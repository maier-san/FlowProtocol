using System.Text.RegularExpressions;

namespace FlowProtocol.Core
{
    public static class CommandHelper
    {
        // Liest einen Textparameter der Form <name>=... aus den Befehlsargumenten
        public static string GetTextParameter(Command cmd, string name, string defaultvalue, Action<ReadErrorItem> addError, bool optional)
        {
            string ret = defaultvalue;
            Regex reg = new Regex(name + @"\s*=(.*)");
            string arguments = cmd.Arguments.Trim();
            foreach(string argument in arguments.Split(';'))
            {
                Match match = reg.Match(argument);
                if (match.Success)
                {
                    ret = match.Groups[1].Value.Trim();
                    return ret;
                }                            
            }
            if (!optional) CreateError(addError, "S01", $"Befehl ohne {name}-Argument.", cmd);
            return ret;
        }

        // Liest einen Zahlenparameter der Form name=... aus den Befehlsargumenten
        public static int GetIntParameter(Command cmd, string name, int defaultvalue, Action<ReadErrorItem> addError, bool optional)
        {
            string value = GetTextParameter(cmd, name, defaultvalue.ToString(), addError, optional);     
            int ret = defaultvalue;
            bool ok = int.TryParse(value, out ret);
            if (!ok)
            {
                ret = defaultvalue;
                CreateError(addError, "S03", $"Befehl ohne gültiges {name}-Argument.", cmd);
            }
            return ret;
        }            

        // Gibt die Frage zu dem Schlüssel zurück, der in den Argumenten mit <name>=... angegeben ist
        public static  Restriction? GetRestriction(Command cmd, string name, Template template, Action<ReadErrorItem> addError)
        {
            Restriction? res = null;
            Regex regTemplateKey = new Regex(name + @"\s*=\s*([A-Za-z0-9]*)");
            string arguments = cmd.Arguments;
            Match match = regTemplateKey.Match(arguments);
            if (match.Success)
            {
                string key = match.Groups[1].Value.Trim();
                res = template.Restrictions.Find(t => t.Key == key);
                if (res == null) CreateError(addError, "S02", $"Frageschlüssel {key} ({name}-Argument) nicht gefunden.", cmd);                
            }
            else CreateError(addError, "S01", $"Befehl ohne {name}-Argument.", cmd);
            return res;
        }

        public static void CreateError(Action<ReadErrorItem> addError, string errorCode, string errorText, Command cmd)
        {
            ReadErrorItem errorTemplate = cmd.ErrorTemplate;
            errorTemplate.ErrorCode = errorCode;
            errorTemplate.ErrorText = errorText;
            addError(errorTemplate);
        }

        // Liest aus einem Ausdruck "F1=W1; F2=W2" die Variablenzuweisungen aus und gibt diese zurück.
        public static Dictionary<string, string> ReadAssignments(string? varExpression)
        {
            Dictionary<string, string> assignments = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(varExpression))
            {                   
                Regex regSetAssignment = new Regex(@"^([A-Za-z0-9]*)\s*=(.*)");
                foreach(var idx in varExpression.Split(";"))
                {
                    string assignment = idx.Trim();
                    if (regSetAssignment.IsMatch(assignment))
                    {
                        var m = regSetAssignment.Match(assignment);
                        string key = m.Groups[1].Value.Trim();
                        if (!string.IsNullOrWhiteSpace(key))
                        {
                            assignments[key] = m.Groups[2].Value.Trim();
                        }
                    }
                }
            }
            return assignments;
        }
    }
}