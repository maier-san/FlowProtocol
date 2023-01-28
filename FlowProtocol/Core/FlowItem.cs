namespace FlowProtocol.Core
{
    public class FlowItem
    {
        public string SortPath { get; set; }

        public FlowItem()
        {
            SortPath = string.Empty;
        }

        public virtual void ApplyTextOperation(Func<string, string> conv)
        {
        }
    }       
}