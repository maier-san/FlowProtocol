namespace FlowProtocol.Core
{
    public class InputItem : QueryItem
    {        
        public string QuestionText { get; set; }

        public InputItem() : base()
        {
            QuestionText = string.Empty;
        }

        // Wendet eine Text-Operation auf die Text-Bestandteile der Frage an.
        public override void ApplyTextOperation(Func<string, string> conv)
        {
            QuestionText = conv(QuestionText);
            base.ApplyTextOperation(conv);
        }
    }
}