namespace FlowProtocol.Core
{
    public class Template
    {
        public string? Description { get; set; }
        public List<FlowItem> FlowItems {get; set;}
        public List<QueryItem> QueryItems => FlowItemFilter<QueryItem>();
        public List<Restriction> Restrictions => FlowItemFilter<Restriction>();
        public List<ResultItem> ResultItems => FlowItemFilter<ResultItem>();
        public List<Command> Commands => FlowItemFilter<Command>();
        
        private List<T> FlowItemFilter<T>() where T : FlowItem
        {
            return FlowItems.Where(x => x is T).Cast<T>().ToList();
        }
        
        public Template? FollowTemplate { get; set; }

        public Template()
        {
            FlowItems = new List<FlowItem>();            
        }

        public Template EndOfChain()
        {
            if (FollowTemplate == null) return this; else return FollowTemplate.EndOfChain();
        }        
    }
}
