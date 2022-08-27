namespace FlowProtocol.Core
{
   public class ResultItem
   {
      public string ResultItemText { get; set; }
      public string ResultItemGroup {get; set; }
      public List<string> SubItems { get; set; }
      public string CodeBlock { get; set; }

      public ResultItem()
      {
         ResultItemText = string.Empty;
         ResultItemGroup = string.Empty;
         SubItems = new List<string>();
         CodeBlock = string.Empty;
      }

      // Wendet eine Text-Operation auf die Text-Bestandteile Ergebniseintrags an.
      public void ApplyTextOperation(Func<string, string> conv)
      {
         ResultItemGroup = conv(ResultItemGroup);
         ResultItemText = conv(ResultItemText);
         SubItems = ApplyTextOperationToList(SubItems, conv);
         CodeBlock = conv(CodeBlock);
      }

      // Fügt eine Codezeile hinzu
      public void AddCodeLine(string codeline)
      {
         if (CodeBlock == string.Empty) CodeBlock = codeline; else CodeBlock += Environment.NewLine + codeline;
      }

      // Wendet eine TExtoperation auf eine ganze Liste von Strings an.
      private List<string> ApplyTextOperationToList(List<string> currentlist, Func<string, string> conv)
      {
         if (!currentlist.Any()) return currentlist;
         List<string> newlist = new List<string>();         
         foreach (var s in currentlist)
         {
            newlist.Add(conv(s));
         }
         currentlist.Clear();
         return newlist;
      }
   }
}
