using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FlowProtocol.Pages
{
   public class InstructionModel : PageModel
   {
      private readonly ILogger<InstructionModel> _logger;

      public InstructionModel(ILogger<InstructionModel> logger)
      {
         _logger = logger;
      }

      public void OnGet()
      {
      }
   }
}