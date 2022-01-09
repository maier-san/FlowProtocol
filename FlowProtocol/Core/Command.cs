namespace FlowProtocol.Core
{
   public class Command
   {
      public string ComandName { get; set; }
      public string Arguments {get; set;}      

      public Command()
      {
         ComandName = string.Empty;;
         Arguments = string.Empty;
      }
   }
}