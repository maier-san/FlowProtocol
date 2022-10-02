namespace FlowProtocol.Core
{
    using System.Text.RegularExpressions;

    // Statische Hilfsfunktionen
    public static class CoreLib
    {
        // Wendet eine Textoperation auf eine ganze Liste von Strings an.
        public static List<string> ApplyTextOperationToList(List<string> currentlist, Func<string, string> conv)
        {
            if (!currentlist.Any()) return currentlist;
            List<string> newlist = new List<string>();
            foreach (var s in currentlist)
            {
                newlist.Add(conv(s));
            }
            currentlist.Clear();
            return newlist;
        }

        // Prüft, ob ein Text eine URL ist, evtl in der Form URL|Display-Text und gibt die Bestandteile zurück
        public static bool IsURL(string text, out string url, out string displayText)
        {
            Regex regDisplayURL = new Regex("^(.*)\\|(.*)");
            if (regDisplayURL.IsMatch(text))
            {
                var m = regDisplayURL.Match(text);
                url = m.Groups[1].Value.Trim();
                displayText = m.Groups[2].Value.Trim();
            }
            else
            {
                url = text;
                displayText = text;
            }
            return (url.StartsWith("https://") || url.StartsWith("http://")) && Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute)
               || url.StartsWith("mailto:");
        }

    }
}