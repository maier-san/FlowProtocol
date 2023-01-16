namespace FlowProtocol.Core
{
    public class ResultItem
    {
        public string ResultItemText { get; set; }
        public string ResultItemGroup { get; set; }
        public List<string> SubItems { get; set; }
        public string CodeBlock { get; set; }
        public string SortPath { get; set; }

        public ResultItem()
        {
            ResultItemText = string.Empty;
            ResultItemGroup = string.Empty;
            SubItems = new List<string>();
            CodeBlock = string.Empty;
            SortPath = string.Empty;
        }

        // Wendet eine Text-Operation auf die Text-Bestandteile Ergebniseintrags an.
        public void ApplyTextOperation(Func<string, string> conv)
        {
            ResultItemGroup = conv(ResultItemGroup);
            ResultItemText = conv(ResultItemText);
            SubItems = CoreLib.ApplyTextOperationToList(SubItems, conv);
            CodeBlock = conv(CodeBlock);
        }

        // Fügt eine Codezeile hinzu
        public void AddCodeLine(string codeline)
        {
            // Hier nicht mit Environment.newline arbeiten, da sonst beim ausschneiden und kopieren des Codes unerwünschte Leerzeilen eingefügt werden.
            if (CodeBlock == string.Empty) CodeBlock = codeline; else CodeBlock += "\n" + codeline;
        }
    }
}
