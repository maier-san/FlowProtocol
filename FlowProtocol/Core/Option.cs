namespace FlowProtocol.Core
{
   public class Option : Template
   {
      public string Key { get; set;}
      public string UniqueKey => RestrictionKey + "_" + Key;
      public string OptionText { get; set; }
      private string RestrictionKey = string.Empty;

      public Option(string restrictionKey) : base()
      {
         Key = string.Empty;
         OptionText = string.Empty;
         RestrictionKey = restrictionKey;
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
