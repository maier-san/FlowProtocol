using System.Xml.Serialization;

namespace FlowProtocol.Core
{
   public class Option : Template
   {
      [XmlAttribute] 
      public string Key { get; set;}
      public string OptionText { get; set; }
      
      public Option() : base()
      {
         Key = "";
         OptionText = "";
         Restrictions = new List<Restriction>();
         ToDos = new List<ToDo>();
      }

      public Option(Restriction parent) : this()
      {
         Parent = parent;
      }
   }
}
