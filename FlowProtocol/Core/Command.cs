namespace FlowProtocol.Core
{
    public class Command : FlowItem
    {
        public string ComandName { get; set; }
        public string Arguments { get; set; }
        // Vorlage für eine Fehlermeldung mit Dateiname, Zeilennummer u.s.w.
        public ReadErrorItem ErrorTemplate { get; set; }
        public string KeyPath { get; set; }
        
        public Command(ReadErrorItem errorTemplate) : base()
        {            
            ComandName = string.Empty;
            Arguments = string.Empty;
            KeyPath = string.Empty;
            ErrorTemplate = errorTemplate;            
        }

        // Wendet eine Text-Operation auf die Text-Bestandteile des Command an
        public override void ApplyTextOperation(Func<string, string> conv)
        {
            base.ApplyTextOperation(conv);
            Arguments = conv(Arguments);
        }
    }
}