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
         ResultItems = new List<ResultItem>();
      }

      // Wendet eine Text-Operation auf die Text-Bestandteile der Frage an.
      public void ApplyTextOperation(Func<string, string> conv)
      {
         OptionText = conv(OptionText);
      }
   }
}
