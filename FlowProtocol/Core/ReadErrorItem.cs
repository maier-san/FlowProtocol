namespace FlowProtocol.Core
{
   // Bildet eine Fehler beim einlesen der Datei ab
   public class ReadErrorItem
   {
      public string ErrorCode {get; set; }
      public string ErrorText { get; set; }
      public string FilePath { get; set; }
      public int LineNumber { get; set; }
      public string Codeline { get; set; }

      public ReadErrorItem()
      {
         ErrorCode = "000";
         ErrorText = "Unbekannter Fehler";
         FilePath = string.Empty;
         LineNumber = 0;
         Codeline = string.Empty;
      }
   }
}