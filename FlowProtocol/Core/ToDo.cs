namespace FlowProtocol.Core
{
   public class ToDo
   {
      public string ToDoText { get; set; }
      public List<string> SubItems {get; set;}

      public ToDo()
      {
         ToDoText = "";
         SubItems = new List<string>();
      }
   }
}
