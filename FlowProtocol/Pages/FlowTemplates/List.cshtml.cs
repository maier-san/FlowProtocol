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
      public List<string> Directiories {get; set;}
      public string TemplatePath { get; set; }
      [BindProperty(SupportsGet = true)]
      public string DetailPath {get; set;}

      public ListModel(IConfiguration configuration)
      {
         TemplatePath = configuration.GetValue<string>("TemplatePath");
         Templates = new List<string>();
      }
      
      public void OnGet(string detailPath)
      {
         string currentPath = TemplatePath;
         if (!string.IsNullOrEmpty(detailPath)) currentPath += "\\" + detailPath;
         DirectoryInfo di = new DirectoryInfo(currentPath);
         Directiories = di.GetDirectories().Select(x => x.Name).ToList<string>();
         Templates = di.GetFiles("*.qfp").Select(x => x.Name.Replace(".qfp", "")).ToList<string>();

         //if (!Templates.Contains("Demo2")) CreateDemo2(TemplatePath);
      }

      
      private void CreateDemo2(string templatepath)
      {
         Template demo = new Template();
         demo.AddRestriction("L1", "Werden Datens�tze im Datenbestand gel�scht?")
             .AddOption("J", "Ja")
               .AddToDo("Es wurde sichergestellt, dass nur die beabsichtigten Datens�tze gel�scht werden.", "Allgemeinen Fall nachbauen, Datens�tze z�hlen")
               .AddRestriction("L2", "Gibt es im Schema Fremdverweise auf die gel�schten Datens�tze?")
               .AddOption("J", "Ja")
                  .AddToDo("Es wurde sichergestellt, dass beim L�schen kleine Blockaden durch Integrit�tsbeziehungen auftreten.", "Allgemeinen Fall nachbauen")
                  .AddToDo("Es wurde sichergestellt, dass beim L�schen keine unbeabsichtigen L�schweitergaben angewendet werden.", "Nach Beziehungen suchen.")
                  .AddToDo("Es wurde sichergestellt, dass nach dem L�schen von Datens�tzen keine verwaisten Datens�tze zur�ckbleiben.", "Allgemeinen Fall nachbauen")
               .EndOption()
               .EndRestriction("N", "Nein")
            .EndOption()
            .EndRestriction("N", "Nein")
            .AddRestriction("G1", "Wird eine Gesch�ftslogik durchlaufen?")
            .AddOption("J", "Ja")
               .AddToDo("Es wurde sichergestellt, dass alle Einschr�nkungskonflikte korrekt verarbeitet werden.")
               .AddToDo("Es wurde sichergestellt, dass ein Abbruch-Konflikt korrekt und transparent verarbeitet wird")
               .AddRestriction("G2", "Werden durch die Gesch�ftslogik Werte berechnet?")
               .AddOption("JR", "Ja, es werden fachlich bedeutsame Werte berechnet.")
                  .AddToDo("Es wurde sichergestellt, dass die durch die GL berechneten Werte in allgemeing�ltigsten Fall korrekt sind.", "Referenzsituation erstellen")
                  .AddToDo("Es wurde sichergestellt, dass die durch die GL berechneten Werte auch in mindestens zwei Sonderf�llen korrekt ermittelt werden.")
                  .AddToDo("Es wurde sichergestellt, dass auch eine Werte-Berechnung durch die GL auch �ber mehrere Datens�tze korrekt funktioniert.")
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
