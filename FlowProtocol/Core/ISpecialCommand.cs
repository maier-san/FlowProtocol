namespace FlowProtocol.Core
{
   public interface ISpecialCommand
   {
       public List<ResultItem> RunCommand(Command cmd, ref Template template, Dictionary<string, string> selectedOptions, Action<ReadErrorItem> addError);
   }
}