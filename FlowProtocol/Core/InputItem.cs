namespace FlowProtocol.Core
{
   public class InputItem
   {
      public string Key { get; set; }
      public string QuestionText { get; set; }

      public InputItem()
      {
         Key = string.Empty;
         QuestionText = string.Empty;
      }
      
      // Wendet eine Text-Operation auf die Text-Bestandteile der Frage an.
      public void ApplyTextOperation(Func<string, string> conv)
      {
         QuestionText = conv(QuestionText);         
      }
   }
}