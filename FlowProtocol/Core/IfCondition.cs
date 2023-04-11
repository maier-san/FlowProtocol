namespace FlowProtocol.Core
{
    //If-Bedingung
    public class IfCondition : Command
    {
        public string ConditionTerm => base.Arguments.Trim();
        public Template ConditionalTemplate {get; set;}

        public IfCondition(ReadErrorItem errorTemplate) : base(errorTemplate)
        {
            ConditionalTemplate = new Template();
        }
    }
}