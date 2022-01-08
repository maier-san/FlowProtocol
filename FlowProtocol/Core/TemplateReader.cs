namespace FlowProtocol.Core
{
    using System;
    using System.Collections;
    using System.IO;
    using System.Text.RegularExpressions;

    public class TemplateReader
    {
        // Stack der übergeordnete Template-Ebenen zusammen mit ihrer Einrückung       
        Stack<Tuple<int, Template>> TemplateStack = new Stack<Tuple<int, Template>>();
        // Stack der übergeordnete Restriction-Ebenen zusammen mit ihrer Einrückung       
        Stack<Tuple<int, Restriction>> ResttrictionStack = new Stack<Tuple<int, Restriction>>();
       
        // Liest eine Template-Datei ein
        // filepath = der vollständige Dateipfad
        public Template? ReadTemplate(string filepath)
        {
            if (!File.Exists(filepath)) return null;
            Template main = new Template();
            Dictionary<string, string> assignments = new Dictionary<string, string>();
            TemplateStack.Push(new Tuple<int, Template>(-1, main));
            ReadTemplatePart(filepath, ref main, 0, assignments);
            return main;
        }

        // Liest eine Template-Datei oder eine Template-Funktionsdatei aus
        // main = die aktuelle Template-Ebene auf der eingelesen wird
        // masterindent = die Einrücktiefe an der aufrufenden Stelle
        // filepath = der vollständige Dateipfad
        private void ReadTemplatePart(string filepath, ref Template main, int masterindent, Dictionary<string, string> assignments)
        {           
            using (StreamReader sr = new StreamReader(filepath))
            {
                Regex regDescription = new Regex("^///(.*)");
                Regex regComment = new Regex("^//.*");
                Regex regRestriction = new Regex(@"^\?(.*):(.*)");
                Regex regOption = new Regex("^#(.*):(.*)");
                Regex regSubItem = new Regex("^>(.*)");
                Regex regTodo = new Regex("^>>(.*)");
                Regex regInsert = new Regex(@"^~Include (.*):(.*)");                        
                ToDo? currentToDo = null;
                while (sr.Peek() != -1)
                {
                    string? line = sr.ReadLine();
                    if (!string.IsNullOrWhiteSpace(line))
                    {                       
                        line = line.Replace("\t", "    ");                       
                        int indent = masterindent + line.Length-line.TrimStart().Length;
                        line = ReplaceVariables(line, assignments);
                        string codeline = line.Trim();
                        if (regDescription.IsMatch(codeline))
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
                            currentToDo = null;
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
                            currentToDo = null;
                        }
                        else if (regTodo.IsMatch(codeline))
                        {
                            Template? parent = GetMatchingParent(indent, TemplateStack);
                            if (parent != null)
                            {
                                var m = regTodo.Match(codeline);
                                ToDo t = new ToDo(){ToDoText = m.Groups[1].Value.Trim()};
                                parent.ToDos.Add(t);
                                currentToDo = t;
                            }
                        }
                        else if (regSubItem.IsMatch(codeline))
                        {
                            if (currentToDo != null)
                            {
                                var m = regSubItem.Match(codeline);
                                currentToDo.SubItems.Add(m.Groups[1].Value.Trim());
                            }
                        }
                        else if (regInsert.IsMatch(codeline))
                        {
                            Template? parent = GetMatchingParent(indent, TemplateStack);
                            if (parent != null)
                            {
                                var m = regInsert.Match(codeline);
                                string flowFunctionFilepath 
                                    = new FileInfo(filepath).DirectoryName + "\\" + m.Groups[1].Value.Trim().Replace(".qff", string.Empty) + ".qff";
                                if (File.Exists(flowFunctionFilepath))
                                {
                                    Dictionary<string, string> subassignments = ReadAssignments(m.Groups[2].Value);
                                    ReadTemplatePart(flowFunctionFilepath, ref parent, indent, subassignments);
                                }
                            }
                        }
                    }
                }    
            }           
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

        // Liest aus einem Ausdruck "$V1=X; $V2=Y" die Variablenzuweisungen aus und gibt diese zurück.
        private Dictionary<string, string> ReadAssignments(string? varExpression)
        {
            Dictionary<string, string> assignments = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(varExpression))
            {
                Regex regAssignement = new Regex(@"\$([A-Za-z0-9]*)=(.*)");
                foreach(var idx in varExpression.Split(";"))
                {
                    string assignment = idx.Trim();
                    if (regAssignement.IsMatch(assignment))
                    {
                        var m = regAssignement.Match(assignment);
                        assignments[m.Groups[1].Value.Trim()] = m.Groups[2].Value.Trim();
                    }
                }
           }
           return assignments;
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