using FlowProtocol.Core;
using System.Text.RegularExpressions;

namespace FlowProtocol.SpecialCommands
{
   public class VoteCommand : ISpecialCommand
   {
        public List<ResultItem> RunCommand(Command cmd, ref Template template, Dictionary<string, string> selectedOptions, Action<ReadErrorItem> addError)
        {
            List<ResultItem> ril = new List<ResultItem>();
            Restriction? res = GetRestriction(cmd, ref template, addError);
            return ril;
       }

       private Restriction? GetRestriction(Command cmd, ref Template template, Action<ReadErrorItem> addError)
       {
            Restriction? res = null;
            Regex regTemplateKey = new Regex(@"Key=([A-Za-z0-9]*)");
            string arguments = cmd.Arguments;
            Match match = regTemplateKey.Match(arguments);
            if (match.Success)
            {
                string key = match.Groups[1].Value.Trim();
                res = template.Restrictions.Find(t => t.Key == key);
                if (res == null) CreateError(addError, "V02", $"Frage-Schlüssel {key} nicht gefunden", cmd);                
            }
            else CreateError(addError, "V01", "Vote-Befehl ohne Key-Argument", cmd);
            return res;
       }

       private void CreateError(Action<ReadErrorItem> addError, string errorCode, string errorText, Command cmd)
       {
            ReadErrorItem errorTemplate = cmd.ErrorTemplate;
            errorTemplate.ErrorCode = errorCode;
            errorTemplate.ErrorText = errorText;
            addError(errorTemplate);
       }
   }
}