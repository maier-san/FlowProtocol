using FlowProtocol.Helper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.IO;

namespace FlowProtocol.Pages.FlowTemplates
{

    public class UserGroupsModel : PageModel
    {        
        
        public ObjectArray<string> UserGroupArray {get; set; }        
        public string TemplatePath { get; set; }
                
        public UserGroupsModel(IConfiguration configuration)
        {
            TemplatePath = configuration.GetValue<string>("TemplatePath");
            UserGroupArray = new ObjectArray<string>();
        }
        
        public void OnGet()
        {
            DirectoryInfo di = new DirectoryInfo(TemplatePath);
            List<string> userGroups = di.GetDirectories().Select(x => x.Name).ToList();
            UserGroupArray.ReadList(userGroups);
        }
    } 
}