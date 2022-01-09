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
   }
}
