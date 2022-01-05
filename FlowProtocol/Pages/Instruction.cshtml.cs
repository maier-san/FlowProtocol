using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FlowProtocol.Pages
{
   public class InstructionModel : PageModel
   {      
      public string TemplatePath { get; set; }

      public InstructionModel(IConfiguration configuration)
      {         
         TemplatePath = configuration.GetValue<string>("TemplatePath");
      }

      public void OnGet()
      {
      }
   }
}