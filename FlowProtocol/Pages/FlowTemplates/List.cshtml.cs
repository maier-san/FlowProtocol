using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FlowProtocol.Core;
using FlowProtocol.Helper;
using System;
using System.IO;

namespace FlowProtocol.Pages.FlowTemplates
{
   public class ListModel : PageModel
   {
      public string? UserGroup {get; set;}
      public Dictionary<string, ObjectArray<TemplateEntry>> TemplateGroups {get; set;}
      public string TemplatePath { get; set; }
            
      public ListModel(IConfiguration configuration)
      {
         TemplatePath = configuration.GetValue<string>("TemplatePath");         
         TemplateGroups = new Dictionary<string, ObjectArray<TemplateEntry>>();
      }
      
      public void OnGet(string userGroup)
      {
         string currentPath = TemplatePath;
         UserGroup = userGroup;
         char separator = Path.DirectorySeparatorChar;
         if (!string.IsNullOrEmpty(UserGroup)) currentPath += separator + UserGroup;
         ReadDirectory(currentPath);
      }
      
      private void ReadDirectory(string path)
      {
         DirectoryInfo di = new DirectoryInfo(path);
         AddTemplateArray(di);
         foreach(var idx in di.GetDirectories())
         {
            ReadDirectory(idx.FullName);
         }
         TemplateGroups.OrderBy(x=>x.Key);
      }
      
      private void AddTemplateArray(DirectoryInfo di)
      {
         string groupName = di.Name;
         if (di.FullName == TemplatePath) groupName = "Favoriten";
         List<TemplateEntry> templatelist = di.GetFiles("*.qfp").Select(x => new TemplateEntry(x, TemplatePath)).OrderBy(x=>x.TemplateName).ToList();
         ObjectArray<TemplateEntry> templateArray = new ObjectArray<TemplateEntry>();
         templateArray.ReadList(templatelist);
         TemplateGroups[groupName] = templateArray;
      }

      public class TemplateEntry
      {
         public string? TemplateName {get; set;}
         public string? TemplatePath {get; set;}
         public TemplateEntry(FileInfo di, string templatePath)
         {
            TemplateName = di.Name.Replace(".qfp",string.Empty);
            char separator = Path.DirectorySeparatorChar;
            TemplatePath = di.FullName.Replace(templatePath + separator, string.Empty).Replace(".qfp",string.Empty);
         }
      }
   }   
}
