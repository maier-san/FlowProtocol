namespace FlowProtocol.Core
{
    // Schnittstelle für Elemente mit Hilfetexte
    public class QueryItem
    {
        public string Section { get; set; }
        public List<string> HelpLines { get; set; }

        public QueryItem()
        {
            HelpLines = new List<string>();
            Section = string.Empty;
        }

        // Fügt eine neue Hilfezeile hinzu
        public void AddHelpLine(string helpline)
        {
            HelpLines.Add(helpline);
        }

        // Prüft, ob ein String eine URL ist. Wird für die Partial-Views benötigt
        public bool IsURL(string text, out string url, out string displayText)
        {
            return CoreLib.IsURL(text, out url, out displayText);
        }

        // Wendet eine Text-Operation auf die Text-Bestandteile der Frage an.
        protected void ApplyTextOperationIntern(Func<string, string> conv)
        {
            HelpLines = CoreLib.ApplyTextOperationToList(HelpLines, conv);
        }
    }
}