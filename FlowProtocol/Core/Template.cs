using System.Xml.Serialization;

namespace FlowProtocol.Core
{
   public class Template
   {
      public List<Restriction> Restrictions { get; set; }
      public List<ToDo> ToDos { get; set; }
      [XmlIgnore]
      internal Restriction? Parent { get; set; }

      public Template()
      {
         Restrictions = new List<Restriction>();
         ToDos = new List<ToDo>();
      }
      
      public Restriction AddRestriction(string key, string questionText)
      {
         Restriction r = new Restriction(this) { Key = key, QuestionText = questionText };
         Restrictions.Add(r);
         return r;
      }

      public Restriction AddRestriction(string key, string questionText, string helpText)
      {
         Restriction r = AddRestriction(key, questionText);
         r.HelpText = helpText;
         return r;
      }

      public Template AddToDo(string toDoText, string bestPractice)
      {
         ToDo t = new ToDo() { ToDoText = toDoText, BestPractice = bestPractice };
         ToDos.Add(t);
         return this;
      }

      public Template AddToDo(string toDoText)
      {
         return AddToDo(toDoText, "");
      }

      public Restriction EndOption()
      {
         return Parent;
      }      
   }
}
