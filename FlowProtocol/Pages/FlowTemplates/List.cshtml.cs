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
         if (!string.IsNullOrEmpty(UserGroup)) currentPath += "\\" + UserGroup;
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
      }
      
      private void AddTemplateArray(DirectoryInfo di)
      {
         string groupName = di.Name;
         if (di.FullName == TemplatePath) groupName = "Favoriten";
         List<TemplateEntry> templatelist = di.GetFiles("*.qfp").Select(x => new TemplateEntry(x, TemplatePath)).ToList();
         ObjectArray<TemplateEntry> templateArray = new ObjectArray<TemplateEntry>();
         templateArray.ReadList(templatelist, 4);
         TemplateGroups[groupName] = templateArray;
      }

      public class TemplateEntry
      {
         public string? TemplateName {get; set;}
         public string? TemplatePath {get; set;}
         public TemplateEntry(FileInfo di, string templatePath)
         {
            TemplateName = di.Name.Replace(".qfp",string.Empty);
            TemplatePath = di.FullName.Replace(templatePath + "\\", string.Empty).Replace(".qfp",string.Empty);
         }
      }
   }   
}
