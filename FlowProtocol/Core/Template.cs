namespace FlowProtocol.Core
{
   public class Template
   {
      public string? Description {get; set;}
      public List<Restriction> Restrictions { get; set; }
      public List<ToDo> ToDos { get; set; }      
      
      public Template()
      {          
         Restrictions = new List<Restriction>();
         ToDos = new List<ToDo>();
      }     
   }
}
