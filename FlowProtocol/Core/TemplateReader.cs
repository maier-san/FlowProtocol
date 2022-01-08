namespace FlowProtocol.Core
{
    using System;
    using System.IO;
    using System.Text.RegularExpressions;

    public class TemplateReader
   {
       // Liste der einzelnen Template-Ebenen in Abhängigkeit der Einrückung
       Dictionary<int, Template> TemplateList = new Dictionary<int, Template>();
       // Liste der einzelnen Restriction-Ebenen in Abhängigkeit der Einrückung
       Dictionary<int, Restriction> ResttrictionList = new Dictionary<int, Restriction>();
       
       // Liest eine Template-Datei ein
       // filepath = der vollständige Dateipfad
       public Template? ReadTemplate(string filepath)
       {
           if (!File.Exists(filepath)) return null;
           Template main = new Template();
           Dictionary<string, string> assignments = new Dictionary<string, string>();
           TemplateList[-1] = main;
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
                Regex regComment = new Regex("//.*");
                Regex regRestriction = new Regex(@"\?(.*):(.*)");
                Regex regOption = new Regex("#(.*):(.*)");
                Regex regTodo = new Regex(">>(.*)");
                Regex regInsert = new Regex(@"~Include (.*):(.*)");                        
                while (sr.Peek() != -1)
                {
                    string? line = sr.ReadLine();
                    if (!string.IsNullOrWhiteSpace(line))
                    {                       
                        line = line.Replace("\t", "    ");                       
                        int indent = masterindent + line.Length-line.TrimStart().Length;
                        line = ReplaceVariables(line, assignments);
                        string codeline = line.Trim();
                        if (regComment.IsMatch(codeline))
                        {
                            // Nur Kommentar: ignorieren
                        }
                        else if (regRestriction.IsMatch(codeline))
                        {                           
                            Template? parent = GetMatchingParent(indent, TemplateList);
                            if (parent != null)
                            {
                                var m = regRestriction.Match(codeline);
                                Restriction r = new Restriction(){Key = m.Groups[1].Value.Trim(), QuestionText = m.Groups[2].Value.Trim()};
                                parent.Restrictions.Add(r);
                                ResttrictionList[indent] = r;
                            }
                        }
                        else if (regOption.IsMatch(codeline))
                        {
                            Restriction? parent = GetMatchingParent(indent, ResttrictionList);
                            if (parent != null)
                            {
                                var m = regOption.Match(codeline);
                                Option o = new Option(){Key = m.Groups[1].Value.Trim(), OptionText = m.Groups[2].Value.Trim()};
                                parent.Options.Add(o);
                                TemplateList[indent] = o;
                            }
                        }
                        else if (regTodo.IsMatch(codeline))
                        {
                            Template? parent = GetMatchingParent(indent, TemplateList);
                            if (parent != null)
                            {
                                var m = regTodo.Match(codeline);
                                ToDo t = new ToDo(){ToDoText = m.Groups[1].Value.Trim()};
                                parent.ToDos.Add(t);
                            }
                        }
                        else if (regInsert.IsMatch(codeline))
                        {
                            Template? parent = GetMatchingParent(indent, TemplateList);
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

       // Sucht das passende Parent-Objekt anhand der Einrückung aus der Liste heraus
       private T? GetMatchingParent<T>(int indent, Dictionary<int, T> list) where T : class
       {
            int bestMatch = -1;
            T? ret = null;
            foreach(var idx in list)
            {
                if (idx.Key < indent && idx.Key >= bestMatch)
                {
                    ret = idx.Value;
                    bestMatch = idx.Key;
                }
            }
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