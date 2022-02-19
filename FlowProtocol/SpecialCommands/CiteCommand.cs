using FlowProtocol.Core;
using System.Text.RegularExpressions;

namespace FlowProtocol.SpecialCommands
{
   public class CiteCommand : ISpecialCommand
    {
        public List<ResultItem> RunCommand(Command cmd, Template template, Dictionary<string, string> selectedOptions, Action<ReadErrorItem> addError)
        {
            string resultgroup = GetResultGroup(cmd);
            List<ResultItem> result = new List<ResultItem>();
            foreach(var residx in template.Restrictions)
            {
                foreach(var idxOpt in residx.Options)
                {
                    if (selectedOptions.ContainsKey(residx.Key) && selectedOptions[residx.Key]==idxOpt.Key)
                    {
                        ResultItem ri =new ResultItem(){ResultItemText = residx.QuestionText};
                        if (!string.IsNullOrWhiteSpace(resultgroup)) ri.ResultItemGroup = resultgroup; 
                        ri.SubItems.Add(idxOpt.OptionText);
                        result.Add(ri);
                    }
                }
            }
            return result;
       }

       private string GetResultGroup(Command cmd)
       {
            string resultgroup = "";
            Regex regResultGroup = new Regex(@"Group=(.*);");
            string arguments = cmd.Arguments + ";";
            Match match = regResultGroup.Match(arguments);
            if (match.Success)
            {
                resultgroup = match.Groups[1].Value.Trim();                
            }
            return resultgroup;
        }
    }
}