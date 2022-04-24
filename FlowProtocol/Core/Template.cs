namespace FlowProtocol.Core
{
   public class Template
   {
      public string? Description { get; set; }
      public List<Restriction> Restrictions { get; set; }
      public List<ResultItem> ResultItems { get; set; }
      public List<Command> Commands { get; set; }
      public List<InputItem> InputItems { get; set; }
      public Template? FollowTemplate { get; set; }
      
      public Template()
      {          
         Restrictions = new List<Restriction>();
         ResultItems = new List<ResultItem>();
         Commands = new List<Command>();
         InputItems = new List<InputItem>();
      }

      public Template EndOfChain()
      {
         if (FollowTemplate == null) return this; else return FollowTemplate.EndOfChain();
      }
   }
}
