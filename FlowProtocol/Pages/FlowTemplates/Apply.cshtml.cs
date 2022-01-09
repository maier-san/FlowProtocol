using FlowProtocol.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Text.RegularExpressions;

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
      public List<ReadErrorItem> ReadErrors {get; set; }

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
         ReadErrors = new List<ReadErrorItem>();
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
         RunCommand(t.Commands);
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

      // Führt die Laufzeitbefehle aus
      private void RunCommand(List<Command> commandlist)
      {
         foreach(var cmd in commandlist)
         {
            switch (cmd.ComandName)
            {
                case "Implies": RunCmd_Implies(cmd.Arguments); break;
            }
         }
      }

      // Impiled-Commando auführen
      private void RunCmd_Implies(string arguments)
      {
         Dictionary<string, string> assignments = ReadAssignments(arguments);
         foreach(var ass in assignments)
         {
            SelectedOptions[ass.Key] = ass.Value;
            if (!GivenKeys.Contains(ass.Key)) GivenKeys.Add(ass.Key);
         }
      }

      // Liest aus einem Ausdruck "F1=W1; F2=W2" die Variablenzuweisungen aus und gibt diese zurück.
      private Dictionary<string, string> ReadAssignments(string? varExpression)
      {
         Dictionary<string, string> assignments = new Dictionary<string, string>();
         if (!string.IsNullOrWhiteSpace(varExpression))
         {
               Regex regAssignement = new Regex(@"([A-Za-z0-9]*)=(.*)");
               foreach(var idx in varExpression.Split(";"))
               {
                  string assignment = idx.Trim();
                  if (regAssignement.IsMatch(assignment))
                  {
                     var m = regAssignement.Match(assignment);
                     assignments[m.Groups[1].Value.Trim()] = m.Groups[2].Value.Trim();
                  }
               }
         }
         return assignments;
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
         ReadErrors = tr.ReadErrors;
      }

      public bool IsURL(string text)
      {
         return Uri.IsWellFormedUriString(text, UriKind.RelativeOrAbsolute);
      }
   }
}
