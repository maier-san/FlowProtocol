namespace FlowProtocol.Core
{
   public interface ISpecialCommand
   {
       public List<ResultItem> RunCommand(Command cmd, Template template, Dictionary<string, string> selectedOptions, Action<ReadErrorItem> addError);
   }
}