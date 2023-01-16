namespace FlowProtocol.Core
{
    public class Command
    {
        public string ComandName { get; set; }
        public string Arguments { get; set; }
        // Vorlage für eine Fehlermeldung mit Dateiname, Zeilennummer u.s.w.
        public ReadErrorItem ErrorTemplate { get; set; }
        public string KeyPath { get; set; }
        public string SortPath {get; set; }

        public Command(ReadErrorItem errorTemplate)
        {
            ComandName = string.Empty;
            Arguments = string.Empty;
            ErrorTemplate = errorTemplate;
            KeyPath = string.Empty;
            SortPath = string.Empty;
        }

        // Wendet eine Text-Operation auf die Text-Bestandteile des Command an
        public void ApplyTextOperation(Func<string, string> conv)
        {
            Arguments = conv(Arguments);
        }
    }
}