namespace FlowProtocol.Core
{
   public class Command
   {
      public string ComandName { get; set; }
      public string Arguments {get; set;}
      // Vorlage für eine Fehlermeldung mit Dateiname, Zeilennummer u.s.w.
      public ReadErrorItem ErrorTemplate {get; set;}

      public Command(ReadErrorItem errorTemplate)
      {
         ComandName = string.Empty;
         Arguments = string.Empty;
         ErrorTemplate =errorTemplate;
      }
   }
}