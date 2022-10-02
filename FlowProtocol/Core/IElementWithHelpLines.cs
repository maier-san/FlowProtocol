namespace FlowProtocol.Core
{
    // Schnittstelle für Elemente mit Hilfetexte
    public interface IElementWithHelpLines
    {
        public List<string> HelpLines { get; set; }
        public void AddHelpLine(string helpline);
    }
}