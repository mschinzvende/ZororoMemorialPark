using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ZororoMemorialPark.Data;

namespace ZororoMemorialPark.Pages.Reports
{
    public class ReportsDashModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        private readonly IAppLogger _appLogger;
        public ReportsDashModel(ApplicationDbContext context, UserManager<IdentityUser> userManager, IAppLogger appLogger)
        {
            _context = context;
            _appLogger = appLogger;
            _userManager = userManager;
        }

        

    }
}
