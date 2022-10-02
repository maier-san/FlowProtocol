namespace FlowProtocol.Core
{
   public class Restriction : IElementWithHelpLines
   {
      public string Key { get; set; }
      public string QuestionText { get; set; }
      public List<string> HelpLines { get; set; }
      public List<Option> Options { get; set; }
      
      public Restriction()
      {
         Key = string.Empty;
         QuestionText = string.Empty;
         HelpLines = new List<string>();
         Options = new List<Option>();
      }

      // Fügt eine neue Hilfezeile hinzu
      public void AddHelpLine(string helpline)
      {         
         HelpLines.Add(helpline);
      }

      // Wendet eine Text-Operation auf die Text-Bestandteile der Frage an.
      public void ApplyTextOperation(Func<string, string> conv)
      {
         QuestionText = conv(QuestionText);
         HelpLines = CoreLib.ApplyTextOperationToList(HelpLines, conv);         
         foreach (var o in Options)
         {
            o.ApplyTextOperation(conv);
         }
      }
   }
}
