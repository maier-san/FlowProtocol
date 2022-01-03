namespace FlowProtocol.Core
{
   public class Restriction
   {
      public string Key { get; set; }
      public string QuestionText { get; set; }
      public string HelpText { get; set; }
      public List<Option> Options { get; set; }
      public Restriction()
      {
         Key = string.Empty;
         QuestionText = string.Empty;
         HelpText = string.Empty;
         Options = new List<Option>();
      }
   }
}
