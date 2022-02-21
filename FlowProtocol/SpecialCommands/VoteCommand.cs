using FlowProtocol.Core;

namespace FlowProtocol.SpecialCommands
{
   public class VoteCommand : ISpecialCommand
   {
        public List<ResultItem> RunCommand(Command cmd, Template template, Dictionary<string, string> selectedOptions, Action<ReadErrorItem> addError)
        {
            Restriction? res = CommandHelper.GetRestriction(cmd, "Key", template, addError);
            int groupsize = CommandHelper.GetIntParameter(cmd, "GroupSize", 3, addError, false);
            string groupname = CommandHelper.GetTextParameter(cmd, "GroupName", "Ergebnis", addError, false);
            Dictionary<Option, int> votingsum = SetCrossTemplates(template, res, groupsize, selectedOptions);
            List<ResultItem> result = CreateResultlist(votingsum, groupname);
            return result;
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
                       ncres.Options.Add(new Option(ncres.Key){Key = i1.Key, OptionText = i1.OptionText});
                       ncres.Options.Add(new Option(ncres.Key){Key = i2.Key, OptionText = i2.OptionText});                    
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
   }
}