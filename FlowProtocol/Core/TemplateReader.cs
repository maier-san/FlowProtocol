namespace FlowProtocol.Core
{
    using System;
    using System.IO;
    using System.Text.RegularExpressions;

    public class TemplateReader
   {
       Dictionary<int, Template> TemplateList = new Dictionary<int, Template>();
       Dictionary<int, Restriction> ResttrictionList = new Dictionary<int, Restriction>();
       
       public Template? ReadTemplate(string filepath)
       {
           if (!File.Exists(filepath)) return null;
           Template main = new Template();
           using (StreamReader sr = new StreamReader(filepath))
           {
               Regex regRestriction = new Regex(@"\?(.*):(.*)");
               Regex regOption = new Regex("#(.*):(.*)");
               Regex regTodo = new Regex(">>(.*)");               
               TemplateList[-1] = main; 
               while (sr.Peek() != -1)
               {
                   string? line = sr.ReadLine();
                   if (!string.IsNullOrWhiteSpace(line))
                   {                       
                       int indent = line.Length-line.TrimStart().Length;
                       string codeline = line.Trim();
                       if (regRestriction.IsMatch(codeline))
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
                   }
               }
           }
           return main;
       }

       // Sucht das passende Parent-Objekt anhand der Einrückung aus der Liste heraus
       T? GetMatchingParent<T>(int indent, Dictionary<int, T> list) where T : class
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
   }
}