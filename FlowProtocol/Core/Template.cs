namespace FlowProtocol.Core
{
    public class Template
    {
        public string? Description { get; set; }
        public List<QueryItem> QueryItems { get; set; }
        public List<Restriction> Restrictions => QueryItems.Cast<Restriction>().Where(r => r != null).ToList();
        public List<InputItem> InputItems => QueryItems.Cast<InputItem>().Where(r => r != null).ToList();
        public List<ResultItem> ResultItems { get; set; }
        public List<Command> Commands { get; set; }
        public Template? FollowTemplate { get; set; }

        public Template()
        {
            QueryItems = new List<QueryItem>();
            ResultItems = new List<ResultItem>();
            Commands = new List<Command>();
        }

        public Template EndOfChain()
        {
            if (FollowTemplate == null) return this; else return FollowTemplate.EndOfChain();
        }
    }
}
