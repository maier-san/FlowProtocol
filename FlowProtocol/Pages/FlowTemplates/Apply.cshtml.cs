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
        private List<QueryItem> ShowQueryItems { get; set; }
        public Dictionary<string, List<ResultItem>> ShowResultGroups { get; set; }
        private string TemplatePath { get; set; }
        private string TemplateDetailPath { get; set; }
        public List<ReadErrorItem> ReadErrors { get; set; }
        private int RecursionCount = 0;

        [BindProperty(SupportsGet = true)]
        public Dictionary<string, string> SelectedOptions { get; set; }
        public List<string> GivenKeys { get; set; }
        public string TemplateBaseURL { get; set; }
        private Dictionary<string, string> GlobalVars { get; set; }
        private List<Tuple<string, Restriction?, InputItem?>> showQueryElements;
        private string CurrentFormat = string.Empty;

        // Die Dreitupel bestehen aus Überschrift, Frage, Eingabeelement, 
        // wobei jeweils nur eines der letzten beiden != null, aber dafür typisiert ist.
        // Überschriften, die gleich sind, wie die vorherige, werden auf leer
        public List<Tuple<string, Restriction?, InputItem?>> ShowQueryElements
        {
            get
            {
                if (!showQueryElements.Any())
                {
                    string currentShowSection = string.Empty;
                    foreach (var q in ShowQueryItems)
                    {
                        string showSection = q.Section;
                        if (showSection == currentShowSection) showSection = string.Empty;
                        showQueryElements.Add(new Tuple<string, Restriction?, InputItem?>(showSection, q as Restriction, q as InputItem));
                        currentShowSection = q.Section;
                    }
                }
                return showQueryElements;
            }
        }

        public ApplyModel(IConfiguration configuration)
        {
            TemplatePath = configuration.GetValue<string>("TemplatePath");
            ShowQueryItems = new List<QueryItem>();
            ShowResultGroups = new Dictionary<string, List<ResultItem>>();
            SelectedOptions = new Dictionary<string, string>();
            GivenKeys = new List<string>();
            ReadErrors = new List<ReadErrorItem>();
            TemplateDetailPath = string.Empty;
            TemplateBreadcrumb = "Unbekannte Vorlage";
            GlobalVars = new Dictionary<string, string>();
            TemplateBaseURL = string.Empty;
            showQueryElements = new List<Tuple<string, Restriction?, InputItem?>>();
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
            if (currentTemplate != null) ExtractQueryItems(currentTemplate);
            return Page();
        }

        /// <summary>
        ///   Extrahiert die nächsten zu beantwortenden Fragen und Eingaben anhand der schon vorhandenen Antworten aus dem Template.
        /// </summary>
        /// <param name="t">Die aktuelle Template-Ebene</param>
        private void ExtractQueryItems(Template t)
        {
            // Zuerst die Spezialbefehle, weil die die Struktur nochmal ändern können
            foreach (var fidx in t.Commands.OrderBy(x => x.SortPath))
            {
                RunByType<Command>(fidx, x => RunSpecialCommand(x, ref t));
            }
            // Dann alles andere (Befehle, Ausgaben und Abfragen)
            foreach (var fidx in t.FlowItems.OrderBy(x => x.SortPath))
            {
                fidx.ApplyTextOperation(ReplaceGlobalVars);
                RunByType<Command>(fidx, RunCommand);
                RunByType<ResultItem>(fidx, AddResultItem);
                RunByType<QueryItem>(fidx, ShowQueryItem);
            }
            if (!ShowQueryItems.Any() && t.FollowTemplate != null)
            {
                // Alle Fragen sind beantwortet und es gibt ein Folge-Template: ausführen
                ExtractQueryItems(t.FollowTemplate);
            }
            if (!ShowQueryItems.Any())
            {
                MakeCodeSummery();
            }
        }

        private void RunByType<T>(FlowItem f, Action<T> H) where T : FlowItem
        {
            if (f is T)
            {
                T? fc = f as T;
                if (fc != null)
                {
                    H(fc);
                }
            }
        }

        private void ShowQueryItem(QueryItem q)
        {
            if (!SelectedOptions.ContainsKey(q.Key))
            {
                // Frage noch unbeantwortet auf Seite übernehmen
                SelectedOptions[q.Key] = string.Empty;
                ShowQueryItems.Add(q);
            }
            else
            {
                GivenKeys.Add(q.Key);
                Restriction? res = q as Restriction;
                if (res != null)
                {
                    string selectedOption = SelectedOptions[res.Key];
                    Option? o = res.Options.Find(x => x.Key == SelectedOptions[res.Key]);
                    if (o == null)
                    {
                        // Antwort nicht in Liste: Suche nach x-Option
                        o = res.Options.Find(x => x.Key == "x");
                    }
                    if (o != null)
                    {
                        // Antwort gefunden
                        ExtractQueryItems(o as Template);
                    }
                    else
                    {
                        // Antwort unbekannt, keine x-Option gefunden: ignorieren                  
                    }
                }
                InputItem? inp = q as InputItem;
                if (inp != null)
                {
                    if (q.Key.Contains("_"))
                    {
                        // Autonummerierte Input-Keys auch noch als Variable verfügbar machen:
                        string valvar = q.Key.Substring(0, q.Key.IndexOf('_')) + "value";
                        GlobalVars[valvar] = SelectedOptions[q.Key];
                    }
                }
            }
        }

        // Kopiert den Code innerhalb jeder Gruppe nach unten und löscht die leeren Ausgaben
        private void MakeCodeSummery()
        {
            foreach (var g in ShowResultGroups)
            {
                ResultItem? csCopyTarget = null;
                List<ResultItem> removelist = new List<ResultItem>();
                foreach (var r in g.Value.OrderByDescending(x => x.SortPath))
                {
                    if (r.CodeBlock.Trim() == "~StartCodeSummary")
                    {
                        csCopyTarget = null;
                        r.CodeBlock = string.Empty;
                        if (string.IsNullOrWhiteSpace(r.ResultItemText)) removelist.Add(r);
                    }
                    if (csCopyTarget != null && !string.IsNullOrWhiteSpace(r.CodeBlock))
                    {
                        csCopyTarget.CodeBlock = r.CodeBlock + "\n" + csCopyTarget.CodeBlock;
                        if (string.IsNullOrWhiteSpace(r.ResultItemText)) removelist.Add(r);
                    }
                    if (r.CodeBlock.Trim() == "~InsertCodeSummary")
                    {
                        csCopyTarget = r;
                        csCopyTarget.CodeBlock = string.Empty;
                    }
                }
                foreach (var r in removelist)
                {
                    g.Value.Remove(r);
                }
            }
        }

        // Fügt die Ergebnispunkte in die Ergebnisgruppen hinzu
        private void AddResultItem(ResultItem item)
        {
            if (!ShowResultGroups.ContainsKey(item.ResultItemGroup))
            {
                ShowResultGroups[item.ResultItemGroup] = new List<ResultItem>();
            }
            ShowResultGroups[item.ResultItemGroup].Add(item);
        }

        // Führt die Laufzeitbefehle aus
        private void RunCommand(Command cmd)
        {
            switch (cmd.ComandName)
            {
                case "Implies": RunCmd_Implies(cmd); break;
                case "Include": RunCmd_Include(cmd); break;
                case "Set": RunCmd_Set(cmd); break;
                case "SetIf": RundCmd_SetIf(cmd); break;
                case "UrlEncode": RunCmd_UrlEncode(cmd); break;
                case "Calculate": RunCmd_Calculate(cmd); break;
                case "Round": RunCmd_Round(cmd); break;
                case "Replace": RunCmd_Replace(cmd); break;
                case "CamelCase": RunCmd_CamelCase(cmd); break;
                case "Random": RunCmd_Random(cmd); break;
                case "SetDateTimeFormat": RundCmd_SetDateTimeFormat(cmd); break;
                case "Vote": break;
                case "Cite": break;
                case "ForEach": break;
                default: AddCommandError("C02", $"Der Befehl {cmd.ComandName} ist nicht bekannt und kann nicht ausgeführt werden.", cmd); break;
            }
        }

        private void RunSpecialCommand(Command cmd, ref Template t)
        {
            ISpecialCommand? sc = null;
            switch (cmd.ComandName)
            {
                case "Vote": sc = new VoteCommand(); break;
                case "Cite": sc = new CiteCommand(); break;
                case "ForEach": sc = new ForEachCommand(TemplateDetailPath); break;
            }
            if (sc != null)
            {
                cmd.ApplyTextOperation(ReplaceGlobalVars);
                List<ResultItem> erg = sc.RunCommand(cmd, t, SelectedOptions, GlobalVars, ReadErrors.Add);
                if (erg != null && erg.Any())
                {
                    t.FlowItems.AddRange(erg);
                }
                // Seed-Wert für den Zufallsgenerator unsichtbar binden
                if (SelectedOptions.ContainsKey("_rseed") && !GivenKeys.Contains("_rseed"))
                {
                    GivenKeys.Add("_rseed");
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
                Template? subTemplate = LoadTemplate(templateFileName, assignments, cmd.KeyPath, cmd.SortPath);
                if (subTemplate == null)
                {
                    AddCommandError("C03", $"Die Funktionsdatei {templateFileName} konnte nicht geladen werden.", cmd);
                    return;
                }
                ExtractQueryItems(subTemplate);
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

        private void RundCmd_SetIf(Command cmd)
        {
            string arguments = cmd.Arguments;
            Regex regSetIfExpression = new Regex(@"^([A-Za-z0-9]*)\s*=\s*(.*)<<<(.*)");
            if (regSetIfExpression.IsMatch(arguments))
            {
                var m = regSetIfExpression.Match(arguments);
                string zvar = m.Groups[1].Value.Trim();
                string wert = m.Groups[2].Value.Trim();
                string cond = m.Groups[3].Value.Trim();
                bool condIsTrue = EvaluateCondition(cond, cmd);
                if (condIsTrue) GlobalVars[zvar] = wert;
            }
        }

        private bool EvaluateCondition(string cond, Command cmd)
        {
            foreach (var disterm in cond.Split("||"))
            {
                bool bdis = EvaluateDisTerm(disterm, cmd);
                if (bdis) return true;
            }
            return false;
        }

        private bool EvaluateDisTerm(string disterm, Command cmd)
        {
            foreach (var conterm in disterm.Split("&&"))
            {
                bool bcon = EvaluateConTerm(conterm, cmd);
                if (!bcon) return false;
            }
            return true;
        }

        private bool EvaluateConTerm(string conterm, Command cmd)
        {
            conterm = conterm.Trim();
            if (conterm == "1" || conterm == "true") return true;
            if (conterm == "0" || conterm == "false") return false;
            bool erg = false;
            if (CheckCompSTerm(conterm, "==", (x, y) => x == y, out erg, cmd)) return erg;
            if (CheckCompSTerm(conterm, "!=", (x, y) => x != y, out erg, cmd)) return erg;
            if (CheckCompDTerm(conterm, "<>", (x, y) => x != y, out erg, cmd)) return erg;
            if (CheckCompDTerm(conterm, "<=", (x, y) => x <= y, out erg, cmd)) return erg;
            if (CheckCompDTerm(conterm, ">=", (x, y) => x >= y, out erg, cmd)) return erg;
            if (CheckCompDTerm(conterm, "<", (x, y) => x < y, out erg, cmd)) return erg;
            if (CheckCompDTerm(conterm, ">", (x, y) => x > y, out erg, cmd)) return erg;            
            AddCommandError("C16", $"Der Ausdruck {conterm} konnte nicht als Vergleichsterm interpretiert werden.", cmd);
            return false;
        }

        private bool CheckCompSTerm(string conterm, string scop, Func<string, string, bool> lcop, out bool erg, Command cmd)
        {
            Regex regCompTerm = new Regex(@"(.*)" + scop + "(.*)");
            if (regCompTerm.IsMatch(conterm))
            {
                var m = regCompTerm.Match(conterm);
                string wert1 = m.Groups[1].Value.Trim();
                string wert2 = m.Groups[2].Value.Trim();
                erg = lcop(wert1, wert2);
                return true;
            }
            erg = false;
            return false;
        }
        private bool CheckCompDTerm(string conterm, string scop, Func<double, double, bool> lcop, out bool erg, Command cmd)
        {
            Regex regCompTerm = new Regex(@"(.*)" + scop + "(.*)");
            if (regCompTerm.IsMatch(conterm))
            {
                var m = regCompTerm.Match(conterm);
                string wert1 = m.Groups[1].Value.Trim();
                string wert2 = m.Groups[2].Value.Trim();

                bool w1OK = double.TryParse(wert1, out double dblwert1);
                bool w2OK = double.TryParse(wert2, out double dblwert2);
                if (!w1OK)
                {
                    AddCommandError("C08", $"Der Ausdruck {wert1} konnte nicht als Gleitkommazahl interpretiert werden.", cmd);
                }
                else if (!w2OK)
                {
                    AddCommandError("C08", $"Der Ausdruck {wert2} konnte nicht als Gleitkommazahl interpretiert werden.", cmd);
                }
                else
                {
                    erg = lcop(dblwert1, dblwert2);
                    return true;
                }
            }
            erg = false;
            return false;
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

        private void RunCmd_CamelCase(Command cmd)
        {
            string arguments = cmd.Arguments.Trim();
            Regex regReplace = new Regex(@"^([A-Za-z0-9]*)\s*=\s*(.*)");
            if (regReplace.IsMatch(arguments))
            {
                var m = regReplace.Match(arguments);
                string zvar = m.Groups[1].Value.Trim();
                string ccwert = m.Groups[2].Value.Trim();
                ccwert = ccwert.Replace("ä", "ae", false, null)
                               .Replace("ö", "oe", false, null)
                               .Replace("ü", "ue", false, null)
                               .Replace("Ä", "Ae", false, null)
                               .Replace("Ö", "Oe", false, null)
                               .Replace("Ü", "Ue", false, null)
                               .Replace("ß", "ss", false, null);
                ccwert = Regex.Replace(ccwert, @"[^\w]", "_");
                ccwert = Regex.Replace(ccwert, @"__*", "_");
                while (ccwert.Contains('_'))
                {
                    int pos = ccwert.IndexOf('_');
                    if (pos + 2 < ccwert.Length)
                    {
                        ccwert = ccwert.Substring(0, pos) + ccwert.Substring(pos + 1, 1).ToUpper() + ccwert.Substring(pos + 2);
                    }
                    else
                    {
                        ccwert = ccwert.Replace("_", "");
                    }
                }
                GlobalVars[zvar] = ccwert;
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

        // Setzt die Datum-Uhrzeit-Formatierung für dem SetDateTimeFormat-Befehl
        private void RundCmd_SetDateTimeFormat(Command cmd)
        {
            string arguments = cmd.Arguments.Trim();
            if (arguments == "default") CurrentFormat = string.Empty; else CurrentFormat = arguments;
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

        private Template? LoadTemplate(string templatefile, Dictionary<string, string>? assignments = null, string keypath = "", string sortpath = "")
        {

            TemplateReader tr = new TemplateReader();
            Template? currentTemplate = tr.ReadTemplate(templatefile, assignments, keypath, sortpath);
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
                input = input.Replace("$GetDateStamp", $"{DateTime.Now:yyyy-MM-dd}");
                input = input.Replace("$GetDateTime", $"{DateTime.Now:g}");
                input = input.Replace("$GetDate", $"{DateTime.Now:d}");
                input = input.Replace("$GetTime", $"{DateTime.Now:T}");
                input = input.Replace("$GetYear", $"{DateTime.Now:yyyy}");
                input = input.Replace("$CRLF", "\r\n");
                input = input.Replace("$LF", "\n");
                if (input.Contains("$GetFDateTime"))
                {
                    string nowstring = $"{DateTime.Now:g}";
                    if (!string.IsNullOrEmpty(CurrentFormat)) nowstring = DateTime.Now.ToString(CurrentFormat);
                    input = input.Replace("$GetFDateTime", nowstring);
                }
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
