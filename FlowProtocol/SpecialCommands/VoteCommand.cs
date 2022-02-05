using FlowProtocol.Core;
using System.Text.RegularExpressions;

namespace FlowProtocol.SpecialCommands
{
   public class VoteCommand : ISpecialCommand
   {
        public List<ResultItem> RunCommand(Command cmd, Template template, Dictionary<string, string> selectedOptions, Action<ReadErrorItem> addError)
        {
            List<ResultItem> ril = new List<ResultItem>();
            Restriction? res = GetRestriction(cmd, template, addError);
            SetCrossTemplates(template, res);
            
            return ril;
       }

       private Restriction? GetRestriction(Command cmd, Template template, Action<ReadErrorItem> addError)
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

       private void SetCrossTemplates(Template template, Restriction? res)
       {
           if (res == null) return;
           Template t = template;
           Template? orgfollow = template.FollowTemplate;
           bool createfollowtemplate = false;
           foreach (var i1 in res.Options)
           {
               foreach(var i2 in res.Options)
               {
                   if (string.Compare(i1.Key, i2.Key) < 0)
                   {
                       Restriction ncres = new Restriction(){Key = i1.Key + "_" + i2.Key, QuestionText = res.QuestionText};
                       ncres.Options.Add(new Option(){Key = i1.Key, OptionText = i1.OptionText});
                       ncres.Options.Add(new Option(){Key = i2.Key, OptionText = i2.OptionText});
                       if (createfollowtemplate)
                       {
                           t.FollowTemplate = new Template();
                           t = t.FollowTemplate;
                       }
                       t.Restrictions.Add(ncres);
                       createfollowtemplate = true;
                   }
               }
           }
           template.Restrictions.Remove(res);
           t.FollowTemplate = orgfollow;
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