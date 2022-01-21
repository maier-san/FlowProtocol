namespace FlowProtocol.Core
{
   public class ResultItem
   {
      public string ResultItemText { get; set; }
      public string ResultItemGroup {get; set;}
      public List<string> SubItems {get; set;}

      public ResultItem()
      {
         ResultItemText = string.Empty;;
         ResultItemGroup = string.Empty;
         SubItems = new List<string>();
      }

      // Wendet eine Text-Operation auf die Text-Bestandteile Ergebniseintrags an.
      public void ApplyTextOperation(Func<string, string> conv)
      {
         ResultItemGroup = conv(ResultItemGroup);
         ResultItemText = conv(ResultItemText);
         List<string> convList = new List<string>();
         if (SubItems.Any())
         {
            foreach (var s in SubItems)
            {
               convList.Add(conv(s));
            }
            SubItems.Clear();
            SubItems = convList;
         }
      }
   }
}
