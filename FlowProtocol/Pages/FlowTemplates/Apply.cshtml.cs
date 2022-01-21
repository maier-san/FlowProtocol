using FlowProtocol.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Text.RegularExpressions;

namespace FlowProtocol.Pages.FlowTemplates
{
   public class ApplyModel : PageModel
   {
      public string TemplateBreadcrumb {get; set; }
      public List<string>? TemplateDescription {get; set; }      
      public List<Restriction> ShowRestrictions { get; set; }
      public Dictionary<string, List<ResultItem>> ShowResultGroups {get; set;}
      private string TemplatePath { get; set; }
      private string TemplateDetailPath { get; set; }
      private Template? CurrentTemplate { get; set; }
      public List<ReadErrorItem> ReadErrors {get; set;}

      [BindProperty(SupportsGet = true)]
      public Dictionary<string, string> SelectedOptions { get; set; }
      public List<string> GivenKeys { get; set; }
      
      public ApplyModel(IConfiguration configuration)
      {
         TemplatePath = configuration.GetValue<string>("TemplatePath");
         ShowRestrictions = new List<Restriction>();
         ShowResultGroups = new Dictionary<string, List<ResultItem>>();
         SelectedOptions = new Dictionary<string, string>();
         GivenKeys = new List<string>();
         ReadErrors = new List<ReadErrorItem>();
         TemplateDetailPath = string.Empty;
         TemplateBreadcrumb = "Unbekannte Vorlage";
      }
      public IActionResult OnGet(string template)
      {
         string templateFileName = TemplatePath + "\\" + template + ".qfp";
         System.IO.FileInfo fi = new System.IO.FileInfo(templateFileName);
         if (fi == null || fi.DirectoryName == null)
         {
            return RedirectToPage("./NoTemplate");
         }
         TemplateDetailPath = fi.DirectoryName;
         TemplateBreadcrumb = template.Replace("\\", ", ");
         Template? currentTemplate = LoadTemplate(templateFileName);         
         if (currentTemplate == null)
         {
            return RedirectToPage("./NoTemplate");
         }
         TemplateDescription = currentTemplate?.Description?.Split("\n").ToList();
         if (currentTemplate != null) ExtractRestrictions(currentTemplate);
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
         if (!ShowRestrictions.Any() && t.FollowTemplate != null)
         {
            // Alle Fragen sind beantwortet und es gibt ein Folge-Template: ausführen
            ExtractRestrictions(t.FollowTemplate);
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
                case "Implies": RunCmd_Implies(cmd); break;
                case "Include": RunCmd_Include(cmd); break;
                default: AddCommandError($"Der Befehl {cmd.ComandName} ist nicht bekannt und kann nicht ausgeführt werden.", cmd); break;
            }
         }
      }

      // Impiles-Commando auführen
      private void RunCmd_Implies(Command cmd)
      {
         Dictionary<string, string> assignments = ReadAssignments(cmd.Arguments);
         foreach(var ass in assignments)
         {
            SelectedOptions[ass.Key] = ass.Value;
            if (!GivenKeys.Contains(ass.Key)) GivenKeys.Add(ass.Key);
         }
      }

      // Include-Commando ausführen
      private void RunCmd_Include(Command cmd)
      {
         Regex regFileArgument = new Regex(@"^([A-Za-z0-9]*) (.*)");
         string arguments = cmd.Arguments;
         if (regFileArgument.IsMatch(arguments))
         {
            var m = regFileArgument.Match(arguments);                        
            string template= m.Groups[1].Value.Trim();
            string templateFileName = TemplateDetailPath + "\\" + template.Trim().Replace(".qff", string.Empty) + ".qff";
            Dictionary<string, string> assignments = ReadAssignments(m.Groups[2].Value);
            Template? subTemplate = LoadTemplate(templateFileName, assignments);
            if (subTemplate == null)
            {
               AddCommandError($"Die Datei {templateFileName} konnte nicht geladen werden.", cmd);
                return;
            }
            ExtractRestrictions(subTemplate);
         }
      }

      // Fügt einen Fehler beim ausführend eines Commandos hinzu
      private void AddCommandError(string errorText, Command cmd)
      {
         ReadErrorItem errorTemplate = cmd.ErrorTemplate;
         errorTemplate.ErrorText = errorText;
         ReadErrors.Add(errorTemplate);
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

      private Template? LoadTemplate(string templatefile, Dictionary<string, string>? assignments = null)
      {         
         TemplateReader tr = new TemplateReader();
         Template? currentTemplate = tr.ReadTemplate(templatefile, assignments);          
         ReadErrors.AddRange(tr.ReadErrors);
         return currentTemplate;
      }

      public bool IsURL(string text)
      {
         return text.StartsWith("http://") && Uri.IsWellFormedUriString(text, UriKind.RelativeOrAbsolute);
      }
   }
}
