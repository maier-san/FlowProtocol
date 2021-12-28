using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FlowProtocol.Core;
using System;
using System.IO;
using System.Xml.Serialization;

namespace FlowProtocol.Pages.FlowTemplates
{
   public class ListModel : PageModel
   {
      public List<string> Templates { get; set; }
      public string TemplatePath { get; set; }

      public ListModel(IConfiguration configuration)
      {
         TemplatePath = configuration.GetValue<string>("TemplatePath");
         Templates = new List<string>();
      }
      
      public void OnGet()
      {
         DirectoryInfo di = new DirectoryInfo(TemplatePath);
         Templates = di.GetFiles("*.fpt").Select(x => x.Name.Replace(".fpt", "")).ToList<string>();

         if (!Templates.Contains("Demo2")) CreateDemo2(TemplatePath);
      }

      
      private void CreateDemo2(string templatepath)
      {
         Template demo = new Template();
         demo.AddRestriction("L1", "Werden Datensätze im Datenbestand gelöscht?")
             .AddOption("J", "Ja")
               .AddToDo("Es wurde sichergestellt, dass nur die beabsichtigten Datensätze gelöscht werden.", "Allgemeinen Fall nachbauen, Datensätze zählen")
               .AddRestriction("L2", "Gibt es im Schema Fremdverweise auf die gelöschten Datensätze?")
               .AddOption("J", "Ja")
                  .AddToDo("Es wurde sichergestellt, dass beim Löschen kleine Blockaden durch Integritätsbeziehungen auftreten.", "Allgemeinen Fall nachbauen")
                  .AddToDo("Es wurde sichergestellt, dass beim Löschen keine unbeabsichtigen Löschweitergaben angewendet werden.", "Nach Beziehungen suchen.")
                  .AddToDo("Es wurde sichergestellt, dass nach dem Löschen von Datensätzen keine verwaisten Datensätze zurückbleiben.", "Allgemeinen Fall nachbauen")
               .EndOption()
               .EndRestriction("N", "Nein")
            .EndOption()
            .EndRestriction("N", "Nein")
            .AddRestriction("G1", "Wird eine Geschäftslogik durchlaufen?")
            .AddOption("J", "Ja")
               .AddToDo("Es wurde sichergestellt, dass alle Einschränkungskonflikte korrekt verarbeitet werden.")
               .AddToDo("Es wurde sichergestellt, dass ein Abbruch-Konflikt korrekt und transparent verarbeitet wird")
               .AddRestriction("G2", "Werden durch die Geschäftslogik Werte berechnet?")
               .AddOption("JR", "Ja, es werden fachlich bedeutsame Werte berechnet.")
                  .AddToDo("Es wurde sichergestellt, dass die durch die GL berechneten Werte in allgemeingültigsten Fall korrekt sind.", "Referenzsituation erstellen")
                  .AddToDo("Es wurde sichergestellt, dass die durch die GL berechneten Werte auch in mindestens zwei Sonderfällen korrekt ermittelt werden.")
                  .AddToDo("Es wurde sichergestellt, dass auch eine Werte-Berechnung durch die GL auch über mehrere Datensätze korrekt funktioniert.")
               .EndOption()
               .AddOption("JT", "Ja, es werden aber nur Werte von untergeordneter Bedeutung berechnet.")
                  .AddToDo("Es wurde sichergestellt, dass die durch die GL berechneten Werte der Spezifikation entsprechen.")
               .EndOption()
               .EndRestriction("N", "Nein")
            .EndOption()
            .EndRestriction("N", "Nein");


         string demofile = TemplatePath + @"\Demo2.fpt";
         using (TextWriter writer = new StreamWriter(demofile))
         {
            XmlSerializer x = new XmlSerializer(typeof(Template));
            x.Serialize(writer, demo);
         }
      }
   }
}
