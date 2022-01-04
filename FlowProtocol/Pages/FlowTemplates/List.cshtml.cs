using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FlowProtocol.Core;
using System;
using System.IO;

namespace FlowProtocol.Pages.FlowTemplates
{
   public class ListModel : PageModel
   {
      public List<TemplateGroup> TemplateGroups {get; set;}
      public string TemplatePath { get; set; }
            
      public ListModel(IConfiguration configuration)
      {
         TemplatePath = configuration.GetValue<string>("TemplatePath");
         TemplateGroups = new List<TemplateGroup>();
      }
      
      public void OnGet()
      {
         string currentPath = TemplatePath;
         ReadDirectory(currentPath);
      }
      
      private void ReadDirectory(string path)
      {
         DirectoryInfo di = new DirectoryInfo(path);
         AddTemplateGroup(di);
         foreach(var idx in di.GetDirectories())
         {
            ReadDirectory(idx.FullName);
         }
      }
      private void AddTemplateGroup(DirectoryInfo di)
      {
         string groupName = di.Name;
         if (di.FullName == TemplatePath) groupName = "Favoriten";
         
         TemplateGroup? group = null;
         TemplateRow? row = null;
         int idx = 0;
         foreach (var fi in di.GetFiles("*.qfp"))
         {
            if (idx == 0) 
            {
               group = new TemplateGroup(groupName);
               TemplateGroups.Add(group);
            }
            if (idx % 3 == 0 && group != null)
            {
               row = new TemplateRow();               
               group.Rows.Add(row);
               row.Entry1 = new TemplateEntry(fi, TemplatePath);                         
            }
            if (idx % 3 == 1 && row != null)
            {
               row.Entry2 = new TemplateEntry(fi, TemplatePath);
            }
            if (idx % 3 == 2 && row != null)
            {
               row.Entry3 = new TemplateEntry(fi, TemplatePath);
            }
            idx++;
         }
      }
   }

   public class TemplateGroup
   {
      public string? GroupName {get; set;}
      public List<TemplateRow> Rows {get; set;}
      public TemplateGroup(string groupName)
      {
         GroupName = groupName;
         Rows = new List<TemplateRow>();
      }
   }
   public class TemplateRow
   {
      public TemplateEntry? Entry1 {get; set;}
      public TemplateEntry? Entry2 {get; set;}
      public TemplateEntry? Entry3 {get; set;}
   }
   public class TemplateEntry
   {
      public string? TemplateName {get; set;}
      public string? TemplatePath {get; set;}
      public TemplateEntry(FileInfo di, string templatePath)
      {
         TemplateName = di.Name.Replace(".qfp",string.Empty);
         TemplatePath = di.FullName.Replace(templatePath + "\\", string.Empty).Replace(".qfp",String.Empty);
      }
   }
}
