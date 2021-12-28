using FlowProtocol.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Xml.Serialization;

namespace FlowProtocol.Pages.FlowTemplates
{
   public class ApplyModel : PageModel
   {
      public string TemplateName { get; set; }
      private string TemplatePath { get; set; }
      private Template? CurrentTemplate { get; set; }
      public List<Restriction> ShowRestrictions { get; set; }
      public List<ToDo> ShowToDos { get; set; }

      [BindProperty(SupportsGet = true)]
      public Dictionary<string, string> SelectedOptions { get; set; }
      public List<string> GivenKeys { get; set; }
      
      public ApplyModel(IConfiguration configuration)
      {
         TemplatePath = configuration.GetValue<string>("TemplatePath");
         TemplateName = "";
         ShowRestrictions = new List<Restriction>();
         ShowToDos = new List<ToDo>();
         SelectedOptions = new Dictionary<string, string>();
         GivenKeys = new List<string>();
      }
      public IActionResult OnGet(string template)
      {
         TemplateName = template;
         LoadTemplate();
         if (CurrentTemplate == null)
         {
            return RedirectToPage("./NoTemplate");
         }
         ExtractRestrictions(CurrentTemplate);
         return Page();
      }

      /// <summary>
      ///   Extrahiert die n�chsten zu beantwortenden Fragen anhand der schon vorhandenen Antworten aus dem Template.
      /// </summary>
      /// <param name="t">Die aktuelle Template-Ebene</param>
      private void ExtractRestrictions(Template t)
      {
         ShowToDos.AddRange(t.ToDos);
         foreach (var r in t.Restrictions)
         {
            if (!SelectedOptions.ContainsKey(r.Key) || !r.Options.Select(x => x.Key).Contains(SelectedOptions[r.Key]))
            {
               // Frage noch unbeantwortet oder ung�ltig beantwortet: auf Seite �bernehmen
               SelectedOptions[r.Key] = "";
               ShowRestrictions.Add(r);
            }
            else
            {
               GivenKeys.Add(r.Key);               
               Option? o = r.Options.Find(x => x.Key == SelectedOptions[r.Key]);
               if (o != null)
               {
                  ExtractRestrictions(o as Template);
               }
            }
         }
      }

      public IActionResult OnPost()
      {
         if (!ModelState.IsValid)
         {            
            return Page();
         }
         return RedirectToPage("./Apply", SelectedOptions);
      }

      private void LoadTemplate()
      {
         string templatefile = TemplatePath + @"\" + TemplateName + ".fpt";
         using (TextReader reader = new StreamReader(templatefile))
         {
            XmlSerializer x = new(typeof(Template));
            CurrentTemplate = x.Deserialize(reader) as Template;
         }
      }
   }
}
