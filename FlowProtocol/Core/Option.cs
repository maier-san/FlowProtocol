namespace FlowProtocol.Core
{
   public class Option : Template
   {
      public string Key { get; set;}
      public string OptionText { get; set; }
      
      public Option() : base()
      {
         Key = string.Empty;
         OptionText = string.Empty;
         Restrictions = new List<Restriction>();
         ToDos = new List<ToDo>();
      }
   }
}
