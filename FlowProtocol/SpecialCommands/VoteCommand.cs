using FlowProtocol.Core;

namespace FlowProtocol.SpecialCommands
{
    public class VoteCommand : ISpecialCommand
    {
        public List<ResultItem> RunCommand(Command cmd, Template template, Dictionary<string, string> selectedOptions, Action<ReadErrorItem> addError)
        {
            Restriction? res = CommandHelper.GetRestriction(cmd, "Key", template, addError);
            string groupname = CommandHelper.GetTextParameter(cmd, "GroupName", "Ergebnis", addError, false);
            string drawoption = CommandHelper.GetTextParameter(cmd, "DrawOption", "", addError, true);
            Dictionary<Option, int> votingsum = SetCrossTemplates(template, res, selectedOptions, drawoption);            
            List<ResultItem> result = CreateResultlist(votingsum, groupname);
            return result;
        }

        // Aus den Antworten einer Frage werden Gruppen aus Paar-Vergleichen gemacht.
        // Die Aufteilung in Gruppen erfolgt automatisch.
        private Dictionary<Option, int> SetCrossTemplates(Template template, Restriction? res, Dictionary<string, string> selectedOptions, string drawoption)
        {
            Template t = template;
            Template? orgfollow = template.FollowTemplate;
            Dictionary<Option, int> votingsum = new Dictionary<Option, int>();
            if (res == null) return votingsum;
            foreach (var idx in res.Options)
            {
                votingsum[idx] = 0;
            }
            // Bestimmung von Gruppen mit Vergleichpaaren, die jeweils kein Element doppelt enthalten
            List<List<Tuple<Option, Option>>> compareGroups = CreateCompareGroups(res.Options);
            
            // Gruppen in Fragen umformen und mit den gegebenen Antworten abgleichen:
            bool notfirstrun = false;
            foreach (var idg in compareGroups)
            {
                if (notfirstrun)
                {
                    // Ab dem zweiten Schleifendurchlauf wird eine neue Gruppe eröffnet
                    t.FollowTemplate = new Template();
                    t = t.FollowTemplate;
                }
                notfirstrun = true;
                foreach (var iop in idg)
                {
                    Option i1 = iop.Item1;
                    Option i2 = iop.Item2;

                    // Eingaben zu Paar i1,i2 abfragen:
                    Restriction ncres = new Restriction() { Key = res.Key + "_" + i1.Key + i2.Key, QuestionText = res.QuestionText };
                    ncres.Options.Add(new Option(ncres.Key) { Key = i1.Key, OptionText = i1.OptionText });
                    ncres.Options.Add(new Option(ncres.Key) { Key = i2.Key, OptionText = i2.OptionText });
                    int val = 1;
                    const string drawKey = "dr";
                    if (!string.IsNullOrEmpty(drawoption))
                    {
                        ncres.Options.Add(new Option(ncres.Key) { Key = drawKey, OptionText = drawoption });
                        val = 2;
                    }
                    if (selectedOptions.ContainsKey(ncres.Key))
                    {
                        if (selectedOptions[ncres.Key] == i1.Key) votingsum[i1]+=val;
                        else if (selectedOptions[ncres.Key] == i2.Key) votingsum[i2]+=val;
                        else if (!string.IsNullOrEmpty(drawoption) && selectedOptions[ncres.Key] == drawKey)
                        {
                            votingsum[i1]+=1;
                            votingsum[i2]+=1;
                        }
                    }
                    t.Restrictions.Add(ncres);
                }
            }
            template.Restrictions.Remove(res);
            t.FollowTemplate = orgfollow;
            return votingsum;
        }

        // Erzeugt eine Liste der Paar-Gruppen.
        // In jeder Gruppe sind alle Elemente eines Paares maximal einmal vertreten.
        // Die Routine baut auf dem selbst entwickelten Algorithmus auf, der die Gruppeneinteilung 
        // mittels zyklischer Permutation erstellt.
        private List<List<Tuple<Option, Option>>> CreateCompareGroups(List<Option> options)
        {
            List<List<Tuple<Option, Option>>> result = new List<List<Tuple<Option, Option>>>();
            int ocount = options.Count; // Anzahl der Oprionen
            int rowsPerGroup = 0; // Anzahl der Matrixzeilen pro Gruppe
            int groupCount = 0; // Anzahl der Gruppen
            bool evenmode = (ocount % 2 == 0); // Unterscheidung zwischen gerade und ungerade
            if (evenmode)
            {
                rowsPerGroup = ocount / 2 - 1;
                groupCount = ocount - 1;
            }
            else
            {
                rowsPerGroup = (ocount - 1) / 2;
                groupCount = ocount;
            }
            Option? fieldOne = null; // Einser-Feld: Wird nur im gerade-Fall verwendet und bildet zusammen mit dem HotSeat ein Paar.            
            Option? hotSeat = null; // HotSeat: Das einzelne Feld außerhalb der Matrix für das Element, das im ungerade-Fall nicht in der Gruppe vorkommt.
            Option[,] compareField = new Option[rowsPerGroup, 2]; // Die z x 2-Matrix für die Elementpaare
            int comprow = 0;
            int compcol = 0;
            foreach (var idx in options)
            {
                if (evenmode && fieldOne == null)
                { // Im gerade-Fall wird fieldone mit dem ersten Element befüllt. Es bleibt fest und bildet Paare mit dem HotSeat.
                    fieldOne = idx;
                }
                else if (hotSeat == null)
                { // Der HotSet wird mit dem ersten (ungerade) oder zweiten (gerade) Element befüllt und permutiert mit.
                    hotSeat = idx;
                }
                else
                { //Die Matrix wird zeilenweise mit den restlichen Elementen aufgefüllt.
                    compareField[comprow, compcol] = idx;
                    compcol++;
                    if (compcol > 1)
                    { // Gehe zur nächsten Zeile, Spalte 0
                        compcol = 0;
                        comprow++;
                    }
                }
            }

            // Befülle die einzelnen Gruppen mit Paaren und permutiere die Matrix inklusive HotSeat
            for (int idg = 0; idg < groupCount; idg++)
            {
                // Neue Gruppe hinzufügen. Die Anzahl ergibt sich schon rechnerisch
                List<Tuple<Option, Option>> currentGroup = new List<Tuple<Option, Option>>();
                result.Add(currentGroup);

                if (evenmode && fieldOne != null && hotSeat != null)
                { // Im gerade-Fall bilden fieldOne und HotSet das erste Paar:
                    currentGroup.Add(new Tuple<Option, Option>(fieldOne, hotSeat));
                }
                Option? top1 = hotSeat;
                Option? top2 = hotSeat;
                hotSeat = compareField[0, 1];
                for (int idr = 0; idr < rowsPerGroup; idr++)
                {
                    // Zeile der Matrix als weiteres Paar hinzufügen
                    currentGroup.Add(new Tuple<Option, Option>(compareField[idr, 0], compareField[idr, 1]));
                    // Rechtes Feld der Matrix entsprechend der Permutation ändern. Fallunterscheidung für letzte Zeile
                    if (idr == rowsPerGroup - 1) compareField[idr, 1] = compareField[idr, 0]; else compareField[idr, 1] = compareField[idr + 1, 1];
                    // Linkes Feld in top1 zwischenspeichern, weil man das Feld darunter jetzt noch nicht überschreiben kann.
                    top1 = compareField[idr, 0];
                    if (top1 != null && top2 != null)
                    {
                        // Linkes Feld der Matrix entsprechend der Permutation ändern. Dazu den zuletzt in top2 gespeicherten Wert verwenden.
                        compareField[idr, 0] = top2;
                        // Zwischenspeicher shiften
                        top2 = top1;
                    }
                }
            }
            return result;
        }

        private List<ResultItem> CreateResultlist(Dictionary<Option, int> votingsum, string groupname)
        {
            List<ResultItem> result = new List<ResultItem>();
            int ranking = 0;
            int previousvalue = -1;
            foreach (var idx in votingsum.OrderBy(x => -x.Value))
            {
                if (previousvalue < 0 || idx.Value < previousvalue)
                {
                    ranking++;
                    previousvalue = idx.Value;
                }
                ResultItem ri = new ResultItem()
                {
                    ResultItemGroup = groupname,
                    ResultItemText = $"Platz {ranking} ({idx.Value} Punkte) {idx.Key.OptionText}"
                };
                result.Add(ri);
            }
            return result;
        }
    }
}