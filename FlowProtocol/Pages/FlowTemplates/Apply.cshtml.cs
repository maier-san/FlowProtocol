using FlowProtocol.Core;
using FlowProtocol.SpecialCommands;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.RegularExpressions;
using System.Web;

namespace FlowProtocol.Pages.FlowTemplates
{
    public class ApplyModel : PageModel
    {
        public string TemplateBreadcrumb { get; set; }
        public List<string>? TemplateDescription { get; set; }
        public List<Restriction> ShowRestrictions { get; set; }
        public Dictionary<string, List<ResultItem>> ShowResultGroups { get; set; }
        public List<InputItem> ShowInputs { get; set; }
        private string TemplatePath { get; set; }
        private string TemplateDetailPath { get; set; }
        public List<ReadErrorItem> ReadErrors { get; set; }
        private int RecursionCount = 0;

        [BindProperty(SupportsGet = true)]
        public Dictionary<string, string> SelectedOptions { get; set; }
        public List<string> GivenKeys { get; set; }
        public string TemplateBaseURL { get; set; }
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
            TemplateBaseURL = string.Empty;
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
            TemplateBaseURL = this.HttpContext.Request.Scheme + "://" + this.HttpContext.Request.Host + this.HttpContext.Request.Path;
            TemplateDescription = currentTemplate?.Description?.Split(Environment.NewLine).ToList();
            if (currentTemplate != null) ExtractRestrictions(currentTemplate);
            return Page();
        }

        /// <summary>
        ///   Extrahiert die nächsten zu beantwortenden Fragen anhand der schon vorhandenen Antworten aus dem Template.
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
                    // Frage noch unbeantwortet auf Seite übernehmen
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
            if (!ShowRestrictions.Any() && !ShowInputs.Any() && t.FollowTemplate != null)
            {
                // Alle Fragen sind beantwortet und es gibt ein Folge-Template: ausführen
                ExtractRestrictions(t.FollowTemplate);
            }
        }

        // Fügt die noch offenen erreichbaren Eingaben hinzu
        private void AddInputItems(List<InputItem> inputlist)
        {
            foreach (var q in inputlist)
            {
                q.ApplyTextOperation(ReplaceGlobalVars);
                if (!SelectedOptions.ContainsKey(q.Key))
                {
                    // Eingabe noch nicht ausgefüllt: auf Seite übernehmen
                    SelectedOptions[q.Key] = string.Empty;
                    ShowInputs.Add(q);
                }
                else
                {
                    GivenKeys.Add(q.Key);
                    if (q.Key.Contains("_"))
                    {
                        // Autonummerierte Input-Keys auch noch als Variable verfügbar machen:
                        string valvar = q.Key.Substring(0, q.Key.IndexOf('_')) + "value";
                        GlobalVars[valvar] = SelectedOptions[q.Key];
                    }
                }
            }
        }

        // Fügt die Ergebnispunkte in die Ergebnisgruppen hinzu
        private void AddResultItems(List<ResultItem> resultlist)
        {
            foreach (var item in resultlist)
            {
                item.ApplyTextOperation(ReplaceGlobalVars);
                if (!ShowResultGroups.ContainsKey(item.ResultItemGroup))
                {
                    ShowResultGroups[item.ResultItemGroup] = new List<ResultItem>();
                }
                ShowResultGroups[item.ResultItemGroup].Add(item);
            }
        }

        // Führt die Laufzeitbefehle aus
        private void RunCommand(List<Command> commandlist, ref Template t)
        {
            foreach (var cmd in commandlist)
            {
                cmd.ApplyTextOperation(ReplaceGlobalVars);
                ISpecialCommand? sc = null;
                switch (cmd.ComandName)
                {
                    case "Implies": RunCmd_Implies(cmd); break;
                    case "Include": RunCmd_Include(cmd); break;
                    case "Set": RunCmd_Set(cmd); break;
                    case "UrlEncode": RunCmd_UrlEncode(cmd); break;
                    case "Calculate": RunCmd_Calculate(cmd); break;
                    case "Round": RunCmd_Round(cmd); break;
                    case "Replace": RunCmd_Replace(cmd); break;
                    case "Random": RunCmd_Random(cmd); break;
                    case "Vote": sc = new VoteCommand(); break;
                    case "Cite": sc = new CiteCommand(); break;
                    default: AddCommandError("C02", $"Der Befehl {cmd.ComandName} ist nicht bekannt und kann nicht ausgeführt werden.", cmd); break;
                }
                if (sc != null)
                {
                    List<ResultItem> erg = sc.RunCommand(cmd, t, SelectedOptions, GlobalVars, ReadErrors.Add);
                    if (erg != null)
                    {
                        t.ResultItems.AddRange(erg);
                    }
                }
            }
        }

        // Impiles-Commando auführen
        private void RunCmd_Implies(Command cmd)
        {
            Dictionary<string, string> assignments = CommandHelper.ReadAssignments(cmd.Arguments);
            foreach (var a in assignments)
            {
                SelectedOptions[a.Key] = a.Value;
                if (!GivenKeys.Contains(a.Key)) GivenKeys.Add(a.Key);
            }
        }

        // Include-Commando ausführen
        private void RunCmd_Include(Command cmd)
        {
            Regex regFileArgument = new Regex(@"^([A-Za-z0-9]*)\s*(.*)");
            string arguments = cmd.Arguments;
            if (regFileArgument.IsMatch(arguments))
            {
                var m = regFileArgument.Match(arguments);
                string template = m.Groups[1].Value.Trim();
                char separator = Path.DirectorySeparatorChar;
                string templateFileName = TemplateDetailPath + separator + template.Trim().Replace(".qff", string.Empty) + ".qff";
                if (!System.IO.File.Exists(templateFileName))
                {
                    // Wenn die Funktionsdatei lokal nicht gefunden wird, dann suche im Shared-Ordner
                    string sharedtemplatefile = TemplatePath + separator + "SharedFunctions" + separator + template.Trim().Replace(".qff", string.Empty) + ".qff";
                    if (System.IO.File.Exists(sharedtemplatefile)) templateFileName = sharedtemplatefile;
                }
                Dictionary<string, string> assignments = CommandHelper.ReadAssignments(m.Groups[2].Value);
                RecursionCount++;
                if (RecursionCount > 100)
                {
                    AddCommandError("C05", $"Der Aufruf der Funktionsdatei {templateFileName} überschreitet das Rekursionsmaximum von 100.", cmd);
                    return;
                }
                Template? subTemplate = LoadTemplate(templateFileName, assignments, cmd.KeyPath);
                if (subTemplate == null)
                {
                    AddCommandError("C03", $"Die Funktionsdatei {templateFileName} konnte nicht geladen werden.", cmd);
                    return;
                }
                ExtractRestrictions(subTemplate);
            }
        }

        // Set-Befehl ausführen
        private void RunCmd_Set(Command cmd)
        {
            string arguments = cmd.Arguments;
            Dictionary<string, string> sets = CommandHelper.ReadAssignments(arguments);
            foreach (var s in sets)
            {
                GlobalVars[s.Key] = s.Value;
            }
            Dictionary<string, int> adds = ReadAddAssignments(arguments);
            foreach (var a in adds)
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

        private void RunCmd_UrlEncode(Command cmd)
        {
            string arguments = cmd.Arguments;
            foreach (var idx in arguments.Split(";"))
            {
                string gvar = idx.Trim(' ');
                if (GlobalVars.ContainsKey(gvar))
                {
                    GlobalVars[gvar] = HttpUtility.UrlEncode(GlobalVars[gvar]).Replace("+", "%20");
                }
            }
        }

        private void RunCmd_Calculate(Command cmd)
        {
            string arguments = cmd.Arguments.Trim();
            Regex regCalcExpression = new Regex(@"^([A-Za-z0-9]*)\s*=\s*(-?[0-9.,]*)\s*([\+\-\*/])\s*(-?[0-9.,]*)");
            if (regCalcExpression.IsMatch(arguments))
            {
                var m = regCalcExpression.Match(arguments);
                string zvar = m.Groups[1].Value.Trim();
                string wert1 = m.Groups[2].Value.Trim();
                string op = m.Groups[3].Value.Trim();
                string wert2 = m.Groups[4].Value.Trim();
                bool w1OK = double.TryParse(wert1, out double dblwert1);
                bool w2OK = double.TryParse(wert2, out double dblwert2);
                double dlbErg = 0;
                bool BerechnungOK = false;
                if (string.IsNullOrEmpty(zvar))
                {
                    AddCommandError("C07", $"Der Ausdruck {zvar} konnte nicht als gültige Zielvariable interpretiert werden.", cmd);
                }
                else if (!w1OK)
                {
                    AddCommandError("C08", $"Der Ausdruck {wert1} konnte nicht als Gleitkommazahl interpretiert werden.", cmd);
                }
                else if (!w2OK)
                {
                    AddCommandError("C08", $"Der Ausdruck {wert2} konnte nicht als Gleitkommazahl interpretiert werden.", cmd);
                }
                else if (op == "+")
                {
                    dlbErg = dblwert1 + dblwert2;
                    BerechnungOK = true;
                }
                else if (op == "-")
                {
                    dlbErg = dblwert1 - dblwert2;
                    BerechnungOK = true;
                }
                else if (op == "*")
                {
                    dlbErg = dblwert1 * dblwert2;
                    BerechnungOK = true;
                }
                else if (op == "/")
                {
                    if (dblwert2 != 0)
                    {
                        dlbErg = dblwert1 / dblwert2;
                        BerechnungOK = true;
                    }
                    else
                    {
                        AddCommandError("C09", $"Division durch 0.", cmd);
                    }
                }
                else
                {
                    AddCommandError("C10", $"Der Ausdruck {op} konnte nicht als zulässiger Operator interpretiert werden.", cmd);
                }
                if (BerechnungOK)
                {
                    GlobalVars[zvar] = dlbErg.ToString();
                }
            }
            else
            {
                AddCommandError("C06", $"Der Ausdruck {arguments} konnte nicht als Berechnungsausdruck interpretiert werden.", cmd);
            }
        }

        private void RunCmd_Round(Command cmd)
        {
            string arguments = cmd.Arguments.Trim();
            Regex regRoundExpression = new Regex(@"^([A-Za-z0-9]*)\s*=\s*(-?[0-9.,]*)\s*\|\s*([0-9]*)");
            if (regRoundExpression.IsMatch(arguments))
            {
                var m = regRoundExpression.Match(arguments);
                string zvar = m.Groups[1].Value.Trim();
                string wert1 = m.Groups[2].Value.Trim();
                string genauigkeit = m.Groups[3].Value.Trim();
                bool w1OK = double.TryParse(wert1, out double dblwert1);
                bool genauigkeitOK = Int32.TryParse(genauigkeit, out int iGenauigkeit);
                double dlbErg = 0;
                bool BerechnungOK = false;
                if (string.IsNullOrEmpty(zvar))
                {
                    AddCommandError("C07", $"Der Ausdruck {zvar} konnte nicht als gültige Zielvariable interpretiert werden.", cmd);
                }
                else if (!w1OK)
                {
                    AddCommandError("C08", $"Der Ausdruck {wert1} konnte nicht als Gleitkommazahl interpretiert werden.", cmd);
                }
                else if (!genauigkeitOK || iGenauigkeit < 0)
                {
                    AddCommandError("C11", $"Der Ausdruck {genauigkeit} konnte nicht als natürliche Zahl interpretiert werden.", cmd);
                }
                else
                {
                    dlbErg = Math.Round(dblwert1, iGenauigkeit);
                    BerechnungOK = true;
                }
                if (BerechnungOK)
                {
                    GlobalVars[zvar] = dlbErg.ToString();
                }
            }
            else
            {
                AddCommandError("C12", $"Der Ausdruck {arguments} konnte nicht als Rundungsausdruck interpretiert werden.", cmd);
            }
        }

        private void RunCmd_Replace(Command cmd)
        {
            string arguments = cmd.Arguments.Trim();
            Regex regReplace = new Regex(@"^([A-Za-z0-9]*)\s*=\s*(.*)\s*\|(.*)->(.*)");
            if (regReplace.IsMatch(arguments))
            {
                var m = regReplace.Match(arguments);
                string zvar = m.Groups[1].Value.Trim();
                string wert = m.Groups[2].Value.Trim();
                string sucheNach = m.Groups[3].Value;
                string ersetzeDurch = m.Groups[4].Value;
                if (string.IsNullOrEmpty(zvar))
                {
                    AddCommandError("C07", $"Der Ausdruck {zvar} konnte nicht als gültige Zielvariable interpretiert werden.", cmd);
                }
                else
                {
                    GlobalVars[zvar] = wert.Replace(sucheNach, ersetzeDurch);
                }
            }
            else
            {
                AddCommandError("C13", $"Der Ausdruck {arguments} konnte nicht als Ersetzungsausdruck interpretiert werden.", cmd);
            }
        }

        private void RunCmd_Random(Command cmd)
        {
            string arguments = cmd.Arguments.Trim();
            Regex regReplace = new Regex(@"^([A-Za-z0-9]*)\s*=\s*(-?[0-9]*)\s*\.\.\s*(-?[0-9]*)");
            if (regReplace.IsMatch(arguments))
            {
                var m = regReplace.Match(arguments);
                string zvar = m.Groups[1].Value.Trim();
                string wertebereichA = m.Groups[2].Value.Trim();
                string wertebereichB = m.Groups[3].Value.Trim();
                bool bOKA = Int32.TryParse(wertebereichA, out int wertA);
                bool bOKB = Int32.TryParse(wertebereichB, out int wertB);


                if (string.IsNullOrEmpty(zvar))
                {
                    AddCommandError("C07", $"Der Ausdruck {zvar} konnte nicht als gültige Zielvariable interpretiert werden.", cmd);
                }
                else if (!bOKA)
                {
                    AddCommandError("C11", $"Der Ausdruck {wertebereichA} konnte nicht als natürliche Zahl interpretiert werden.", cmd);
                }
                else if (!bOKB)
                {
                    AddCommandError("C11", $"Der Ausdruck {wertebereichB} konnte nicht als natürliche Zahl interpretiert werden.", cmd);
                }
                else if (wertA > wertB)
                {
                    AddCommandError("C14", $"Die angegebenen Werte bilden kein gültiges Intervall.", cmd);
                }
                else
                {
                    int rndval = new Random().Next(wertA, wertB + 1);
                    GlobalVars[zvar] = rndval.ToString();
                }
            }
            else
            {
                AddCommandError("C13", $"Der Ausdruck {arguments} konnte nicht als Ersetzungsausdruck interpretiert werden.", cmd);
            }
        }

        // Fügt einen Fehler beim ausführend eines Commandos hinzu
        private void AddCommandError(string errorCode, string errorText, Command cmd)
        {
            ReadErrorItem errorTemplate = cmd.ErrorTemplate;
            errorTemplate.ErrorCode = errorCode;
            errorTemplate.ErrorText = errorText;
            ReadErrors.Add(errorTemplate);
        }

        // Liest aus einem Ausdruck "F1+=W1; F2+=W2" die Variablen-Addier-Zuweisungen aus und gibt diese zurück.
        private Dictionary<string, int> ReadAddAssignments(string? varExpression)
        {
            Dictionary<string, int> assignments = new Dictionary<string, int>();
            if (!string.IsNullOrWhiteSpace(varExpression))
            {
                Regex regAddAssignment = new Regex(@"^([A-Za-z0-9]*)\s*\+=\s*(-?[0-9]*)");
                foreach (var idx in varExpression.Split(";"))
                {
                    string assignment = idx.Trim();
                    if (regAddAssignment.IsMatch(assignment))
                    {
                        var m = regAddAssignment.Match(assignment);

                        int incValue = 0;
                        string key = m.Groups[1].Value.Trim();
                        bool incOK = int.TryParse(m.Groups[2].Value.Trim(), out incValue);
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

        private Template? LoadTemplate(string templatefile, Dictionary<string, string>? assignments = null, string keypath = "")
        {

            TemplateReader tr = new TemplateReader();
            Template? currentTemplate = tr.ReadTemplate(templatefile, assignments, keypath);
            ReadErrors.AddRange(tr.ReadErrors);
            return currentTemplate;
        }

        private string ReplaceGlobalVars(string input)
        {
            if (input.Contains('$'))
            {
                foreach (var v in GlobalVars.OrderByDescending(x => x.Key))
                {
                    input = input.Replace("$" + v.Key, v.Value);
                }
                foreach (var v in SelectedOptions.OrderByDescending(x => x.Key))
                {
                    input = input.Replace("$" + v.Key, v.Value);
                }
                // Systemvariablen
                input = input.Replace("$MyResultURL", this.HttpContext.Request.Scheme + "://" + this.HttpContext.Request.Host + this.HttpContext.Request.Path + this.HttpContext.Request.QueryString);
                input = input.Replace("$MyBaseURL", this.HttpContext.Request.Scheme + "://" + this.HttpContext.Request.Host + this.HttpContext.Request.Path);
                input = input.Replace("$NewGuid", Guid.NewGuid().ToString());
                input = input.Replace("$GetDateTime", $"{DateTime.Now:g}");
                input = input.Replace("$GetDate", $"{DateTime.Now:d}");
                input = input.Replace("$GetTime", $"{DateTime.Now:T}");
                input = input.Replace("$GetYear", $"{DateTime.Now:yyyy}");
                input = input.Replace("$CRLF", "\r\n");
                input = input.Replace("$LF", "\n");
                if (input.Contains("$Chr"))
                {
                    for (int i = 1; i < 255; i++)
                    {
                        input = input.Replace($"$Chr{i:000}", Convert.ToChar(i).ToString());
                    }
                }
            }
            return input;
        }

        // Prüft, ob ein String eine URL ist. Wird für die Partial-Views benötigt
        public bool IsURL(string text, out string url, out string displayText)
        {
            return CoreLib.IsURL(text, out url, out displayText);
        }
    }
}
