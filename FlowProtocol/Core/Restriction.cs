namespace FlowProtocol.Core
{
    public class Restriction : QueryItem
    {
        
        public string QuestionText { get; set; }
        public List<Option> Options { get; set; }

        public Restriction() : base()
        {            
            QuestionText = string.Empty;
            Options = new List<Option>();
        }

        // Wendet eine Text-Operation auf die Text-Bestandteile der Frage an.
        public override void ApplyTextOperation(Func<string, string> conv)
        {
            QuestionText = conv(QuestionText);            
            foreach (var o in Options)
            {
                o.ApplyTextOperation(conv);
            }
            base.ApplyTextOperation(conv);
        }
    }
}
