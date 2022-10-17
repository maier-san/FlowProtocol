using FlowProtocol.Core;
using System.Text.RegularExpressions;

namespace FlowProtocol.SpecialCommands
{
   public class CiteCommand : ISpecialCommand
    {
        public List<ResultItem> RunCommand(Command cmd, Template template, Dictionary<string, string> selectedOptions,
            Dictionary<string, string> globalVars, Action<ReadErrorItem> addError)
        {
            string groupname = CommandHelper.GetTextParameter(cmd, "GroupName", "", addError, false);
            List<ResultItem> result = new List<ResultItem>();
            foreach(var residx in template.Restrictions)
            {
                foreach(var idxOpt in residx.Options)
                {
                    if (selectedOptions.ContainsKey(residx.Key) && selectedOptions[residx.Key]==idxOpt.Key)
                    {
                        ResultItem ri =new ResultItem(){ResultItemText = residx.QuestionText};
                        if (!string.IsNullOrWhiteSpace(groupname)) ri.ResultItemGroup = groupname; 
                        ri.SubItems.Add(idxOpt.OptionText);
                        result.Add(ri);
                    }
                }
            }
            return result;
       }
    }
}