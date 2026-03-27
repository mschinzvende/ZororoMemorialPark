using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ZororoMemorialPark.Pages.Requisitions
{
    [AllowAnonymous]
    public class ResultModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public string Status { get; set; }

        [BindProperty(SupportsGet = true)]
        public string RequestNumber { get; set; }

        public void OnGet(string status, string request)
        {
            Status = status;
            RequestNumber = request;
        }
    }
}