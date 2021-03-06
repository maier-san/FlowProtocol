using FlowProtocol.Core;
using FlowProtocol.SpecialCommands;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Web;
using System.Text.RegularExpressions;

namespace FlowProtocol.Pages.FlowTemplates
{
   public class ApplyModel : PageModel
   {
      public string TemplateBreadcrumb {get; set; }
      public List<string>? TemplateDescription {get; set; }      
      public List<Restriction> ShowRestrictions { get; set; }
      public Dictionary<string, List<ResultItem>> ShowResultGroups {get; set;}
      public List<InputItem> ShowInputs { get; set; }
      private string TemplatePath { get; set; }
      private string TemplateDetailPath { get; set; }
      public List<ReadErrorItem> ReadErrors {get; set;}   
      private int RecursionCount = 0;

      [BindProperty(SupportsGet = true)]
      public Dictionary<string, string> SelectedOptions { get; set; }
      public List<string> GivenKeys { get; set; }
      private Dictionary<string, string> GlobalVars { get; set; }
      
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
         GlobalVars = new Dictionary<string, string>();
         ShowInputs = new List<InputItem>();
      }
      public IActionResult OnGet(string template)
      {
         string templateDec = HttpUtility.UrlDecode(template);
         char separator = Path.DirectorySeparatorChar;
         string templateFileName = TemplatePath + separator + templateDec + ".qfp";
         System.IO.FileInfo fi = new System.IO.FileInfo(templateFileName);
         if (fi == null || fi.DirectoryName == null)
         {
            return RedirectToPage("./NoTemplate");
         }
         TemplateDetailPath = fi.DirectoryName;
         TemplateBreadcrumb = templateDec.Replace(separator.ToString(), ", ");
         Template? currentTemplate = LoadTemplate(templateFileName);         
         if (currentTemplate == null)
         {
            return RedirectToPage("./NoTemplate");
         }
         TemplateDescription = currentTemplate?.Description?.Split(Environment.NewLine).ToList();
         if (currentTemplate != null) ExtractRestrictions(currentTemplate);
         return Page();
      }

      /// <summary>
      ///   Extrahiert die n??chsten zu beantwortenden Fragen anhand der schon vorhandenen Antworten aus dem Template.
      /// </summary>
      /// <param name="t">Die aktuelle Template-Ebene</param>
      private void ExtractRestrictions(Template t)
      {
         RunCommand(t.Commands, ref t);
         AddInputItems(t.InputItems);
         AddResultItems(t.ResultItems);         
         foreach (var r in t.Restrictions)
         {
            r.ApplyTextOperation(ReplaceGlobalVars);
            if (!SelectedOptions.ContainsKey(r.Key))
            {
               // Frage noch unbeantwortet auf Seite ??bernehmen
               SelectedOptions[r.Key] = string.Empty;
               ShowRestrictions.Add(r);
            }
            else
            {
               GivenKeys.Add(r.Key);
               string selectedOption = SelectedOptions[r.Key];
               Option? o = r.Options.Find(x => x.Key == SelectedOptions[r.Key]);
               if (o == null)
               {
                  // Antwort nicht in Liste: Suche nach x-Option
                  o = r.Options.Find(x => x.Key == "x");
               }
               if (o != null)
               {
                  // Antwort gefunden
                  ExtractRestrictions(o as Template);
               }
               else
               {
                  // Antwort unbekannt, keine x-Option gefunden: ignorieren                  
               }
            }
         }
         if (!ShowRestrictions.Any() && t.FollowTemplate != null)
         {
            // Alle Fragen sind beantwortet und es gibt ein Folge-Template: ausf??hren
            ExtractRestrictions(t.FollowTemplate);
         }
      }

      // F??gt die noch offenen erreichbaren Eingaben hinzu
      private void AddInputItems(List<InputItem> inputlist)
      {
         foreach(var q in inputlist)
         {
            q.ApplyTextOperation(ReplaceGlobalVars);
            if (!SelectedOptions.ContainsKey(q.Key))
            {
               // Eingabe noch nicht ausgef??llt: auf Seite ??bernehmen
               SelectedOptions[q.Key] = string.Empty;
               ShowInputs.Add(q);
            }
            else
            {
               GivenKeys.Add(q.Key);               
            }
         }      
      }

      // F??gt die Ergebnispunkte in die Ergebnisgruppen hinzu
      private void AddResultItems(List<ResultItem> resultlist)
      {
         foreach(var item in resultlist)
         {
            item.ApplyTextOperation(ReplaceGlobalVars);            
            if (!ShowResultGroups.ContainsKey(item.ResultItemGroup))
            {
               ShowResultGroups[item.ResultItemGroup] = new List<ResultItem>();
            }
            ShowResultGroups[item.ResultItemGroup].Add(item);
         }
      }

      // F??hrt die Laufzeitbefehle aus
      private void RunCommand(List<Command> commandlist, ref Template t)
      {
         foreach(var cmd in commandlist)
         {
            cmd.ApplyTextOperation(ReplaceGlobalVars);
            ISpecialCommand? sc = null;
            switch (cmd.ComandName)
            {
                case "Implies": RunCmd_Implies(cmd); break;
                case "Include": RunCmd_Include(cmd); break;
                case "Set": RunCmd_Set(cmd); break;
                case "Vote": sc = new VoteCommand(); break;
                case "Cite": sc = new CiteCommand(); break;
                default: AddCommandError("C02", $"Der Befehl {cmd.ComandName} ist nicht bekannt und kann nicht ausgef??hrt werden.", cmd); break;
            }
            if (sc != null)
            {
               List<ResultItem> erg = sc.RunCommand(cmd, t, SelectedOptions, ReadErrors.Add);
               if (erg != null)
               {
                  t.ResultItems.AddRange(erg);
               }
            }
         }
      }

      // Impiles-Commando auf??hren
      private void RunCmd_Implies(Command cmd)
      {
         Dictionary<string, string> assignments = CommandHelper.ReadAssignments(cmd.Arguments);
         foreach(var a in assignments)
         {
            SelectedOptions[a.Key] = a.Value;
            if (!GivenKeys.Contains(a.Key)) GivenKeys.Add(a.Key);
         }
      }

      // Include-Commando ausf??hren
      private void RunCmd_Include(Command cmd)
      {
         Regex regFileArgument = new Regex(@"^([A-Za-z0-9]*)\s*(.*)");
         string arguments = cmd.Arguments;
         if (regFileArgument.IsMatch(arguments))
         {
            var m = regFileArgument.Match(arguments);                        
            string template= m.Groups[1].Value.Trim();
            char separator = Path.DirectorySeparatorChar;
            string templateFileName = TemplateDetailPath + separator + template.Trim().Replace(".qff", string.Empty) + ".qff";
            Dictionary<string, string> assignments = CommandHelper.ReadAssignments(m.Groups[2].Value);
            RecursionCount++;
            if (RecursionCount > 100)
            {
               AddCommandError("C05", $"Der Aufruf der Funktionsdatei {templateFileName} ??berschreitet das Rekursionsmaximum von 100.", cmd);
               return;
            }            
            Template? subTemplate = LoadTemplate(templateFileName, assignments);
            if (subTemplate == null)
            {
               AddCommandError("C03", $"Die Funktionsdatei {templateFileName} konnte nicht geladen werden.", cmd);
               return;
            }
            ExtractRestrictions(subTemplate);                        
         }
      }

      // Set-Befehl ausf??hren
      private void RunCmd_Set(Command cmd)
      {         
         string arguments = cmd.Arguments;
         Dictionary<string, string> sets = CommandHelper.ReadAssignments(arguments);         
         foreach(var s in sets)
         {
            GlobalVars[s.Key] = s.Value;
         }
         Dictionary<string, int> adds = ReadAddAssignments(arguments);
         foreach(var a in adds)
         {
            bool baseOK = true;
            int baseValue = 0;
            if (GlobalVars.ContainsKey(a.Key))
            {
               baseOK = int.TryParse(GlobalVars[a.Key], out baseValue);
            }
            if (baseOK)
            {
               GlobalVars[a.Key] = (baseValue + a.Value).ToString();
            }
            else
            {
                AddCommandError("C04", $"Der Wert der Variablen ${a.Key} konnte nicht als ganze Zahl interpretiert werden.", cmd);
            }
         }
      }

      // F??gt einen Fehler beim ausf??hrend eines Commandos hinzu
      private void AddCommandError(string errorCode, string errorText, Command cmd)
      {
         ReadErrorItem errorTemplate = cmd.ErrorTemplate;
         errorTemplate.ErrorCode = errorCode;
         errorTemplate.ErrorText = errorText;
         ReadErrors.Add(errorTemplate);
      }      

      // Liest aus einem Ausdruck "F1+=W1; F2+=W2" die Variablen-Addier-Zuweisungen aus und gibt diese zur??ck.
      private Dictionary<string, int> ReadAddAssignments(string? varExpression)
      {
         Dictionary<string, int> assignments = new Dictionary<string, int>();
         if (!string.IsNullOrWhiteSpace(varExpression))
         {
            Regex regAddAssignment = new Regex(@"^([A-Za-z0-9]*)\s*\+=\s*([0-9]*)");                         
            foreach(var idx in varExpression.Split(";"))
            {
               string assignment = idx.Trim();
               if (regAddAssignment.IsMatch(assignment))
               {
                  var m = regAddAssignment.Match(assignment);                     
                  
                  int incValue = 0;
                  string key = m.Groups[1].Value.Trim();
                  bool incOK = int.TryParse(m.Groups[2].Value.Trim(), out  incValue);                  
                  if (!string.IsNullOrWhiteSpace(key) && incOK)
                  {
                     assignments[key] = incValue;
                  }
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

      private string ReplaceGlobalVars(string input)
      {
         foreach (var v in GlobalVars)
         {
            input = input.Replace("$" + v.Key, v.Value);
         }
         foreach (var v in SelectedOptions)
         {
            input = input.Replace("$" + v.Key, v.Value);
         }
         return input;
      }

      public bool IsURL(string text)
      {
         return (text.StartsWith("https://") || text.StartsWith("http://")) && Uri.IsWellFormedUriString(text, UriKind.RelativeOrAbsolute);
      }
   }
}
