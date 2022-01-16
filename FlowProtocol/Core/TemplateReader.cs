namespace FlowProtocol.Core
{
    using System;
    using System.Collections;
    using System.IO;
    using System.Text.RegularExpressions;

    public class TemplateReader
    {
        // Stack der übergeordnete Template-Ebenen zusammen mit ihrer Einrückung       
        private Stack<Tuple<int, Template>> TemplateStack = new Stack<Tuple<int, Template>>();
        // Stack der übergeordnete Restriction-Ebenen zusammen mit ihrer Einrückung       
        private Stack<Tuple<int, Restriction>> ResttrictionStack = new Stack<Tuple<int, Restriction>>();
        // Fehler beim Einlesen
        public List<ReadErrorItem> ReadErrors {get; private set;}
       
        public TemplateReader()
        {
            ReadErrors = new List<ReadErrorItem>();
        }

        // Liest eine Template-Datei ein
        // filepath = der vollständige Dateipfad
        public Template? ReadTemplate(string filepath, Dictionary<string, string>? assignments = null)
        {
            ReadErrors.Clear();
            if (!File.Exists(filepath)) 
            {
                AddReadError("Vorlagendatei nicht gefunden.", filepath, 0, string.Empty);
                return null;
            }
            Template main = new Template();
            if (assignments == null)
            {
                assignments = new Dictionary<string, string>();
            }
            TemplateStack.Push(new Tuple<int, Template>(-1, main));
            using (StreamReader sr = new StreamReader(filepath))
            {
                Regex regDescription = new Regex("^///(.*)");
                Regex regComment = new Regex("^//.*");
                Regex regRestriction = new Regex(@"^\?(.*):(.*)");
                Regex regOption = new Regex("^#(.*):(.*)");
                Regex regSubItem = new Regex("^>(.*)");
                Regex regGroupedResultItem = new Regex("^>>(.*)>>(.*)");
                Regex regResultItem = new Regex("^>>(.*)");
                Regex regCommand = new Regex(@"^~([A-Za-z0-9]*) (.*)");                      
                ResultItem? currentResultItem = null;
                int linenumber = 0;
                while (sr.Peek() != -1)
                {
                    string? line = sr.ReadLine();
                    linenumber++;
                    if (!string.IsNullOrWhiteSpace(line))
                    {                       
                        line = line.Replace("\t", "    ");                       
                        int indent = line.Length-line.TrimStart().Length;
                        line = ReplaceVariables(line, assignments);
                        string codeline = line.Trim();
                        if (string.IsNullOrWhiteSpace(codeline))
                        {
                            // Leerzeile: ignorieren
                        }
                        else if (regDescription.IsMatch(codeline))
                        {                            
                            if (!(main is Option))
                            { // Beschreibung nur für das Hauptelement hinzufügen und ansonsten ignorieren
                                var m = regDescription.Match(codeline);
                                string descriptionLine = m.Groups[1].Value.Trim();
                                if (!string.IsNullOrWhiteSpace(descriptionLine))
                                {
                                    if (!string.IsNullOrWhiteSpace(main.Description)) 
                                    {
                                        main.Description += "\n" + descriptionLine;
                                    }
                                    else 
                                    {
                                        main.Description = descriptionLine;
                                    }
                                }
                            }
                            else AddReadError("Beschreibungskommentar auf untergeordneter Ebene wird ignoriert.", filepath, linenumber, codeline);
                        }
                        else if (regComment.IsMatch(codeline))
                        {
                            // Nur Kommentar: ignorieren
                        }
                        else if (regRestriction.IsMatch(codeline))
                        {                           
                            Template? parent = GetMatchingParent(indent, TemplateStack);
                            if (parent != null)
                            {
                                var m = regRestriction.Match(codeline);
                                Restriction r = new Restriction(){Key = m.Groups[1].Value.Trim(), QuestionText = m.Groups[2].Value.Trim()};
                                parent.Restrictions.Add(r);
                                ResttrictionStack.Push(new Tuple<int, Restriction>(indent, r));
                            }
                            else AddReadError("Frage kann nicht zugeordnet werden.", filepath, linenumber, codeline);
                            currentResultItem = null;
                        }
                        else if (regOption.IsMatch(codeline))
                        {
                            Restriction? parent = GetMatchingParent(indent, ResttrictionStack);
                            if (parent != null)
                            {
                                var m = regOption.Match(codeline);
                                Option o = new Option(){Key = m.Groups[1].Value.Trim(), OptionText = m.Groups[2].Value.Trim()};
                                parent.Options.Add(o);
                                TemplateStack.Push(new Tuple<int, Template>(indent, o));
                            }
                            else AddReadError("Antwort kann nicht zugeordnet werden.", filepath, linenumber, codeline);
                            currentResultItem = null;
                        }
                        else if (regGroupedResultItem.IsMatch(codeline))
                        {
                            Template? parent = GetMatchingParent(indent, TemplateStack);
                            if (parent != null)
                            {
                                var m = regGroupedResultItem.Match(codeline);
                                ResultItem t = new ResultItem(){ResultItemGroup = m.Groups[1].Value.Trim(), ResultItemText = m.Groups[2].Value.Trim()};
                                parent.ResultItems.Add(t);
                                currentResultItem = t;
                            }
                            else AddReadError("Gruppierter Ausgabeeintrag kann nicht zugeordnet werden.", filepath, linenumber, codeline);
                        }
                        else if (regResultItem.IsMatch(codeline))
                        {
                            Template? parent = GetMatchingParent(indent, TemplateStack);
                            if (parent != null)
                            {
                                var m = regResultItem.Match(codeline);
                                ResultItem t = new ResultItem(){ResultItemText = m.Groups[1].Value.Trim()};
                                parent.ResultItems.Add(t);
                                currentResultItem = t;
                            }
                            else AddReadError("Ausgabeeintrag kann nicht zugeordnet werden.", filepath, linenumber, codeline);
                        }
                        else if (regSubItem.IsMatch(codeline))
                        {
                            if (currentResultItem != null)
                            {
                                var m = regSubItem.Match(codeline);
                                currentResultItem.SubItems.Add(m.Groups[1].Value.Trim());
                            }
                            else AddReadError("Ergänzungseintrag kann keinen Ausgabeeintrag zugeordnet werden.", filepath, linenumber, codeline);
                        }
                        else if (regCommand.IsMatch(codeline))
                        {
                            Template? parent = GetMatchingParent(indent, TemplateStack);
                            if (parent != null)
                            {
                                ReadErrorItem errortemplate =  new ReadErrorItem()
                                {
                                    ErrorText = "Beim Ausführend eines Kommandos ist ein Fehler aufgetreten", 
                                    FilePath = filepath,
                                    LineNumber = linenumber,
                                    Codeline = codeline.Trim()
                                };
                                var m = regCommand.Match(codeline);
                                Command c = new Command(errortemplate){ComandName = m.Groups[1].Value.Trim(), Arguments = m.Groups[2].Value.Trim()};
                                parent.Commands.Add(c);
                            }
                            else AddReadError("Befehl kann nicht zugeordnet werden.", filepath, linenumber, codeline);                            
                            currentResultItem = null;
                        }
                        else AddReadError("Zeile nicht interpretierbar", filepath, linenumber, codeline);                        
                    }
                }    
            }     
            return main;      
        }

        // Fügt einen Einlesefehler hinzu
        private void AddReadError(string errorText, string filepath, int linenumber, string codeline)
        {
            ReadErrorItem rei =  new ReadErrorItem()
                {
                    ErrorText = errorText, 
                    FilePath = filepath,
                    LineNumber = linenumber,
                    Codeline = codeline.Trim()
                };
            ReadErrors.Add(rei);
        }

        private T? GetMatchingParent<T>(int indent, Stack<Tuple<int, T>> list) where T : class
        {
            T? ret = null;
            Tuple<int, T> p = list.Peek();
            while (list.Any() && p.Item1 >= indent) 
            {
                list.Pop();
                p = list.Peek();
            }
            if (list.Any()) ret = p.Item2;
            return ret;
        }

        // Ersetzt die Variablen durch die Werte.
        private string ReplaceVariables(string codeline, Dictionary<string, string> assignments)
        {
            if (string.IsNullOrWhiteSpace(codeline)) return codeline;
            string ret = codeline;
            foreach(var idx in assignments)
            {
                ret = ret.Replace("$" + idx.Key, idx.Value);
            }
            return ret;
        }
    }
}