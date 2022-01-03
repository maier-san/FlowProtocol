using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FlowProtocol.Core;
using System;
using System.IO;

namespace FlowProtocol.Pages.FlowTemplates
{
   public class ListModel : PageModel
   {
      public List<string> Templates { get; set; }
      public string TemplatePath { get; set; }
            
      public ListModel(IConfiguration configuration)
      {
         TemplatePath = configuration.GetValue<string>("TemplatePath");
         Templates = new List<string>();
      }
      
      public void OnGet()
      {
         string currentPath = TemplatePath;
         ReadDirectory(currentPath);
         Templates.Sort();
      }
      
      private void ReadDirectory(string path)
      {
         DirectoryInfo di = new DirectoryInfo(path);
         Templates.AddRange(di.GetFiles("*.qfp").Select(x => x.FullName.Replace(TemplatePath + "\\", string.Empty).Replace(".qfp", string.Empty)));
         foreach(var idx in di.GetDirectories())
         {
            ReadDirectory(idx.FullName);
         }
      }
   }
}
