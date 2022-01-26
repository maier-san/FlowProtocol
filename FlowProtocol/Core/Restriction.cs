namespace FlowProtocol.Core
{
   public class Restriction
   {
      public string Key { get; set; }
      public string QuestionText { get; set; }
      public List<Option> Options { get; set; }
      public Restriction()
      {
         Key = string.Empty;
         QuestionText = string.Empty;
         Options = new List<Option>();
      }

      // Wendet eine Text-Operation auf die Text-Bestandteile der Frage an.
      public void ApplyTextOperation(Func<string, string> conv)
      {
         QuestionText = conv(QuestionText);
         foreach (var o in Options)
         {
            o.ApplyTextOperation(conv);
         }
      }
   }
}
