using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.IO;

namespace FlowProtocol.Pages.FlowTemplates
{

    public class UserGroupsModel : PageModel
    {        
        public List<string> UserGroups {get; set;}
        public string TemplatePath { get; set; }
                
        public UserGroupsModel(IConfiguration configuration)
        {
            TemplatePath = configuration.GetValue<string>("TemplatePath");
            UserGroups = new List<string>();
        }
        
        public void OnGet()
        {
            DirectoryInfo di = new DirectoryInfo(TemplatePath);
            UserGroups = di.GetDirectories().Select(x => x.Name).ToList();
        }
    } 
}