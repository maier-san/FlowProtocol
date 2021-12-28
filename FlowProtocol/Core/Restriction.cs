using System.Xml.Serialization;

namespace FlowProtocol.Core
{
   public class Restriction
   {
      [XmlAttribute]
      public string Key { get; set; }
      public string QuestionText { get; set; }
      public string HelpText { get; set; }
      public List<Option> Options { get; set; }
      [XmlIgnore]
      private Template? Parent { get; set; }
      public Restriction()
      {
         Key = "";
         QuestionText = "";
         HelpText = "";
         Options = new List<Option>();
         Parent = null;
      }

      public Restriction(Template parent) : this()
      {         
         Parent = parent;
      }
      public Option AddOption(string key, string optionText)
      {
         Option o = new Option(this) { Key = key, OptionText = optionText };
         Options.Add(o);
         return o;

      }
      public Template EndRestriction()
      {
         return Parent;
      }
      public Template EndRestriction(string key, string optionText)
      {
         AddOption(key, optionText);
         return EndRestriction();
      }
   }
}
