using FlowProtocol.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;

namespace FlowProtocol.Pages.FlowTemplates
{
   public class ApplyModel : PageModel
   {
      public string TemplateName { get; set; }
      public string TemplateBreadcrumb => TemplateName.Replace("\\", ", ");
      public List<string>? TemplateDescription => CurrentTemplate?.Description?.Split("\n").ToList();      
      public List<Restriction> ShowRestrictions { get; set; }
      public Dictionary<string, List<ResultItem>> ShowResultGroups {get; set;}
      private string TemplatePath { get; set; }
      private Template? CurrentTemplate { get; set; }

      [BindProperty(SupportsGet = true)]
      public Dictionary<string, string> SelectedOptions { get; set; }
      public List<string> GivenKeys { get; set; }
      
      public ApplyModel(IConfiguration configuration)
      {
         TemplatePath = configuration.GetValue<string>("TemplatePath");
         TemplateName = string.Empty;
         ShowRestrictions = new List<Restriction>();
         ShowResultGroups = new Dictionary<string, List<ResultItem>>();
         SelectedOptions = new Dictionary<string, string>();
         GivenKeys = new List<string>();
      }
      public IActionResult OnGet(string template)
      {
         TemplateName = template;
         LoadTemplate();
         if (CurrentTemplate == null)
         {
            return RedirectToPage("/NoTemplate");
         }
         ExtractRestrictions(CurrentTemplate);
         return Page();
      }

      /// <summary>
      ///   Extrahiert die nächsten zu beantwortenden Fragen anhand der schon vorhandenen Antworten aus dem Template.
      /// </summary>
      /// <param name="t">Die aktuelle Template-Ebene</param>
      private void ExtractRestrictions(Template t)
      {
         AddResultItems(t.ResultItems);
         foreach (var r in t.Restrictions)
         {
            if (!SelectedOptions.ContainsKey(r.Key) || !r.Options.Select(x => x.Key).Contains(SelectedOptions[r.Key]))
            {
               // Frage noch unbeantwortet oder ungültig beantwortet: auf Seite übernehmen
               SelectedOptions[r.Key] = string.Empty;
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

      // Fügt die Ergebnispunkte in die Ergebnisgruppen hinzu
      private void AddResultItems(List<ResultItem> resultlist)
      {
         foreach(var item in resultlist)
         {
            if (!ShowResultGroups.ContainsKey(item.ResultItemGroup))
            {
               ShowResultGroups[item.ResultItemGroup] = new List<ResultItem>();
            }
            ShowResultGroups[item.ResultItemGroup].Add(item);
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
         string templatefile = TemplatePath + @"\" + TemplateName + ".qfp";
         TemplateReader tr = new TemplateReader();
         CurrentTemplate = tr.ReadTemplate(templatefile);
      }

      public bool IsURL(string text)
      {
         return Uri.IsWellFormedUriString(text, UriKind.RelativeOrAbsolute);
      }
   }
}
