using FlowProtocol.Core;
using System.Text.RegularExpressions;

namespace FlowProtocol.SpecialCommands
{
   public class VoteCommand : ISpecialCommand
   {
        public List<ResultItem> RunCommand(Command cmd, Template template, Dictionary<string, string> selectedOptions, Action<ReadErrorItem> addError)
        {
            Restriction? res = GetRestriction(cmd, template, addError);
            int groupsize = GetIntParameter(cmd, "GroupSize", 3, addError);
            string groupname = GetTextParameter(cmd, "GroupName", "Ergebnis");
            Dictionary<Option, int> votingsum = SetCrossTemplates(template, res, groupsize, selectedOptions);
            List<ResultItem> result = CreateResultlist(votingsum, groupname);
            return result;
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
                if (res == null) CreateError(addError, "V02", $"Frageschlüssel {key} nicht gefunden.", cmd);                
            }
            else CreateError(addError, "V01", "Vote-Befehl ohne Key-Argument.", cmd);
            return res;
        }

        private int GetIntParameter(Command cmd, string name, int defaultvalue, Action<ReadErrorItem> addError)
        {
            int ret = defaultvalue;
            Regex reg = new Regex(name + "=([0-9]*)");
            string arguments = cmd.Arguments;
            Match match = reg.Match(arguments);
            if (match.Success)
            {
                string value = match.Groups[1].Value.Trim();
                bool ok = int.TryParse(value, out ret);
                if (!ok)
                {
                    ret = defaultvalue;
                    CreateError(addError, "V03", $"Vote-Befehl ohne gültiges {name}-Argument.", cmd);
                }
            }
            return ret;
        }    

        private string GetTextParameter(Command cmd, string name, string defaultvalue)
        {
            string ret = defaultvalue;
            Regex reg = new Regex(name + "=(.*);");
            string arguments = cmd.Arguments;
            Match match = reg.Match(arguments);
            if (match.Success)
            {
                ret = match.Groups[1].Value.Trim();
            }
            return ret;
        }

       private Dictionary<Option, int> SetCrossTemplates(Template template, Restriction? res, int groupsize, Dictionary<string, string> selectedOptions)
       {           
           Template t = template;
           Template? orgfollow = template.FollowTemplate;
           Dictionary<Option, int> votingsum = new Dictionary<Option, int>();
           if (res == null) return votingsum;
           int groupcount = 0;
           foreach (var idx in res.Options)
           {
               votingsum[idx] = 0;
           }
           foreach (var i1 in res.Options)
           {
               foreach(var i2 in res.Options)
               {
                   if (string.Compare(i1.Key, i2.Key) < 0)
                   {
                       Restriction ncres = new Restriction(){Key = res.Key + "_" + i1.Key + i2.Key, QuestionText = res.QuestionText};
                       ncres.Options.Add(new Option(){Key = i1.Key, OptionText = i1.OptionText});
                       ncres.Options.Add(new Option(){Key = i2.Key, OptionText = i2.OptionText});                    
                       if (selectedOptions.ContainsKey(ncres.Key))
                       {
                            Option? winner = null;
                            if (selectedOptions[ncres.Key] == i1.Key) winner = i1;
                            else if (selectedOptions[ncres.Key] == i2.Key) winner = i2;
                            if (winner != null) votingsum[winner]++;
                       }
                       if (groupcount >= groupsize)
                       {
                           t.FollowTemplate = new Template();
                           t = t.FollowTemplate;
                           groupcount = 0;
                       }
                       groupcount++;
                       t.Restrictions.Add(ncres);
                   }
               }
           }
           template.Restrictions.Remove(res);
           t.FollowTemplate = orgfollow;
           return votingsum;
       }

       private List<ResultItem> CreateResultlist(Dictionary<Option, int> votingsum, string groupname)
       {
            List<ResultItem> result = new List<ResultItem>();
            int ranking = 0;
            int previousvalue = -1;
            foreach(var idx in votingsum.OrderBy(x => -x.Value))
            {
                if (previousvalue < 0 || idx.Value < previousvalue)
                {
                    ranking++;
                    previousvalue = idx.Value;
                }
                ResultItem ri = new ResultItem()
                {
                        ResultItemGroup = groupname, 
                        ResultItemText = $"Platz {ranking} ({idx.Value} Punkte) {idx.Key.OptionText}"
                };
                result.Add(ri);
            }
            return result;
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