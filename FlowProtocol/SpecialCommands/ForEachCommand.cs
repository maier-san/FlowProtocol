using System.Text.RegularExpressions;
using FlowProtocol.Core;

namespace FlowProtocol.SpecialCommands
{
    public class ForEachCommand : ISpecialCommand
    {
        public string TemplateDetailPath { get; set; }
        public ForEachCommand(string templateDetailPath)
        {
            TemplateDetailPath = templateDetailPath;
        }

        public List<ResultItem> RunCommand(Command cmd, Template template, Dictionary<string, string> selectedOptions,
            Dictionary<string, string> globalVars, Action<ReadErrorItem> addError)
        {
            Restriction? res = CommandHelper.GetRestriction(cmd, "Key", template, addError);
            string listfilename = CommandHelper.GetTextParameter(cmd, "List", "", addError, false);
            string indexVar = CommandHelper.GetTextParameter(cmd, "IndexVar", "", addError, false);
            int take = CommandHelper.GetIntParameter(cmd, "Take", 0, addError, true);
            int groupBy = CommandHelper.GetIntParameter(cmd, "GroupBy", 0, addError, true);
            string arrayListDiv = CommandHelper.GetTextParameter(cmd, "ArrayList", "", addError, true);
            string groupfilter = CommandHelper.GetTextParameter(cmd, "GroupFilter", "", addError, true);
            var list = ReadList(listfilename, groupfilter, cmd, addError);
            TakeSelection(ref list, take, selectedOptions);
            SetListTemplates(list, template, res, indexVar, groupBy, arrayListDiv, cmd.SortPath);
            return new List<ResultItem>();
        }

        private void SetListTemplates(List<Tuple<string, string>> list, Template template, Restriction? res, string indexVar,
            int groupBy, string arrayListDiv, string sortpath)
        {
            Template t = template;
            Template? orgfollow = template.FollowTemplate;
            if (res == null) return;
            int gcount = 0;
            int lcount = 0;
            if (arrayListDiv == "1") arrayListDiv = ";";
            string elementsortpath = sortpath + lcount.ToString("D6");
            foreach (var idx in list)
            {
                if (groupBy > 0 && gcount >= groupBy)
                {
                    // Ab dem zweiten Schleifendurchlauf wird eine neue Gruppe eröffnet
                    t.FollowTemplate = new Template();
                    t = t.FollowTemplate;
                    gcount = 0;
                }
                lcount++;
                gcount++;

                Func<string, string> repItems = (x => x.Replace("$" + indexVar, idx.Item1));
                if (!string.IsNullOrEmpty(arrayListDiv))
                {
                    // Der Listeneintrag wird in mehrere Elemente aufgeteilt, die Ersetzungsfunktion überschrieben.
                    List<string> itemsplits = idx.Item1.Split(arrayListDiv).ToList();
                    repItems = x =>
                    {
                        int jidx = 0;
                        foreach (var sp in itemsplits)
                        {
                            x = x.Replace("$" + indexVar + jidx.ToString("D1"), sp);
                            jidx++;
                            if (jidx > 9) break;
                        }
                        return x;
                    };
                }

                Restriction q = new Restriction()
                {
                    Key = res.Key + '_' + lcount.ToString(),
                    QuestionText = res.QuestionText,
                    HelpLines = res.HelpLines,
                    SortPath = elementsortpath + res.SortPath
                };
                if (!string.IsNullOrEmpty(idx.Item2)) q.Section = idx.Item2;

                foreach (var oidx in res.Options)
                {
                    Option o = new Option(q.Key)
                    {
                        Key = oidx.Key,
                        OptionText = oidx.OptionText
                    };
                    foreach (var ridx in oidx.ResultItems)
                    {
                        ResultItem r = new ResultItem()
                        {
                            ResultItemGroup = ridx.ResultItemGroup,
                            ResultItemText = ridx.ResultItemText,
                            CodeBlock = ridx.CodeBlock,
                            SortPath = elementsortpath + ridx.SortPath
                        };
                        o.FlowItems.Add(r);
                        foreach (var rsidx in ridx.SubItems)
                        {
                            r.SubItems.Add(rsidx);
                        }
                        r.ApplyTextOperation(repItems);
                    }
                    foreach (var cidx in oidx.Commands)
                    {
                        Command c = new Command(cidx.ErrorTemplate)
                        {
                            ComandName = cidx.ComandName,
                            Arguments = cidx.Arguments,
                            KeyPath = cidx.KeyPath,
                            SortPath = elementsortpath + cidx.SortPath
                        };
                        o.FlowItems.Add(c);
                        c.ApplyTextOperation(repItems);
                    }
                    q.Options.Add(o);
                    q.ApplyTextOperation(repItems);
                }
                t.FlowItems.Add(q);
            }
            template.FlowItems.Remove(res);
            t.FollowTemplate = orgfollow;
        }

        // Wählt <take> Elemente aus der Liste aus.
        private void TakeSelection(ref List<Tuple<string, string>> list, int take, Dictionary<string, string> selectedOptions)
        {
            if (take <= 0 || take >= list.Count) return;
            // Stelle Seed-Wert sicher für wiederholbare Zufallsergebnisse
            int rseed = 100;
            if (selectedOptions.ContainsKey("_rseed"))
            {
                bool ok = Int32.TryParse(selectedOptions["_rseed"], out rseed);
            }
            else
            {
                rseed = new Random().Next();
                selectedOptions["_rseed"] = rseed.ToString();
            }
            Random tr = new Random(rseed);
            while (list.Count > take)
            {
                int remidx = tr.Next(list.Count);
                list.RemoveAt(remidx);
            }
        }

        private List<Tuple<string, string>> ReadList(string listfilename, string groupfilter, Command cmd, Action<ReadErrorItem> addError)
        {
            List<Tuple<string, string>> ret = new List<Tuple<string, string>>();
            char separator = Path.DirectorySeparatorChar;
            string listfilepath = TemplateDetailPath + separator + listfilename.Trim().Replace(".qfl", string.Empty) + ".qfl";
            bool groupfilteraktive = false;
            if (!string.IsNullOrEmpty(groupfilter))
            {
                groupfilteraktive = true;
                groupfilter = "+" + groupfilter + "+";
            }
            if (System.IO.File.Exists(listfilepath))
            {
                using (StreamReader sr = new StreamReader(listfilepath))
                {
                    Regex regComment = new Regex("^//.*");
                    Regex regGroup = new Regex(@"^\[(.*)\]");
                    string group = string.Empty;
                    while (sr.Peek() != -1)
                    {
                        string? line = sr.ReadLine();
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            // Leerzeile: kann ignoriert werden
                        }
                        else if (regComment.IsMatch(line))
                        {
                            // Kommentarzeile: kann ignoriert werden
                        }
                        else if (regGroup.IsMatch(line))
                        {
                            var m = regGroup.Match(line);
                            group = m.Groups[1].Value.Trim();
                        }
                        else
                        {
                            if (!groupfilteraktive || string.IsNullOrEmpty(group) || groupfilter.Contains("+" + group + "+"))
                            {
                                string element = line.Trim();
                                ret.Add(new Tuple<string, string>(element, group));
                            }
                        }
                    }
                }
            }
            else CommandHelper.CreateError(addError, "C15", $"Die Listendatei {listfilepath} konnte nicht gefunden werden.", cmd);
            return ret;
        }
    }
}