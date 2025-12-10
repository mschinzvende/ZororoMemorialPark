using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ZororoMemorialPark.Data;
using ZororoMemorialPark.Data.Models;
using ZororoMemorialPark.Services;

namespace ZororoMemorialPark.Pages.Burials
{
    public class BurialScheduleModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notificationService;
        private readonly SystemSettingsService _settingsService;
        private readonly IAppLogger _appLogger;
        private readonly UserManager<IdentityUser> _userManager;

        public BurialScheduleModel(
            ApplicationDbContext context,
            NotificationService notificationService,
            SystemSettingsService settingsService, IAppLogger appLogger, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _notificationService = notificationService;
            _settingsService = settingsService;
            _appLogger = appLogger;
            _userManager = userManager;
        }

        // Filter
        [BindProperty(SupportsGet = true)]
        public DateTime? FilterDate { get; set; }

        // Lists
        public List<BurialScheduleVM> TodaysBurials { get; set; } = new();
        public List<BurialScheduleVM> UpcomingBurials { get; set; } = new();
        public List<BurialScheduleVM> AllBurials { get; set; } = new();

        // ---------------------------------------------------------
        // GET PAGE
        // ---------------------------------------------------------
        public async Task OnGetAsync()
        {
            try
            {
                var today = DateTime.Today;

                // Step 1 – fetch without filtering
                var raw = await (
                    from a in _context.tbl_GraveAllocations
                    join g in _context.tbl_Graves on a.sys_graveId equals g.sys_graveId
                    join s in _context.tbl_Sections on g.sys_sectionId equals s.sys_sectionId
                    join d in _context.tbl_Deceased on a.sys_deceasedId equals d.sys_deceasedId
                    join c in _context.tbl_Customers on a.sys_customerId equals c.sys_customerId
                    select new
                    {
                        a.sys_allocationId,
                        g.sys_graveId,
                        g.sys_plotNumber,
                        s.sys_sectionName,
                        d.sys_deceasedFullName,
                        d.sys_deceasedNextOfKinName,
                        d.sys_deceasedNextOfKinPhone,
                        d.sys_deceasedNextOfKinEmail,
                        c.sys_paymentMadeBy,
                        a.sys_repatriationSource,
                        a.sys_dateTimeOfBurial
                    }
                ).ToListAsync();

                // Step 2 – convert in memory
                var burials = raw
                    .Where(x => x.sys_dateTimeOfBurial > DateTime.MinValue)
                    .Select(x => new BurialScheduleVM
                    {
                        AllocationId = x.sys_allocationId,
                        GraveId = x.sys_graveId,
                        PlotNumber = x.sys_plotNumber,
                        SectionName = x.sys_sectionName,
                        DeceasedName = x.sys_deceasedFullName,
                        NextOfKinName = x.sys_deceasedNextOfKinName,
                        NextOfKinContact = x.sys_deceasedNextOfKinPhone,
                        NextOfKinEmail = x.sys_deceasedNextOfKinEmail,
                        CustomerName = x.sys_paymentMadeBy,
                        RepatriationSource = x.sys_repatriationSource,
                        BurialDateTime = x.sys_dateTimeOfBurial
                    })
                    .ToList();

                // Step 3 – filtering
                DateTime defaultFrom = today.AddDays(-7);
                DateTime defaultTo = today.AddDays(30);

                if (FilterDate.HasValue)
                {
                    var day = FilterDate.Value.Date;
                    var dayEnd = day.AddDays(1);

                    burials = burials
                        .Where(b => b.BurialDateTime >= day && b.BurialDateTime < dayEnd)
                        .ToList();
                }
                else
                {
                    burials = burials
                        .Where(b => b.BurialDateTime >= defaultFrom && b.BurialDateTime <= defaultTo)
                        .ToList();
                }

                // Step 4 – assign model
                AllBurials = burials.OrderBy(b => b.BurialDateTime).ToList();

                TodaysBurials = AllBurials
                    .Where(b => b.BurialDateTime.Date == today)
                    .ToList();

                UpcomingBurials = AllBurials
                    .Where(b => b.BurialDateTime.Date > today)
                    .Take(100)
                    .ToList();

                await _appLogger.LogActivityAsync(
                    _userManager.GetUserId(User),
                    "View",
                    "Viewed burial schedule"
                );
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred loading burial schedule.";
                _appLogger.LogActivityAsync(
                    _userManager.GetUserId(User),
                    "Error",
                    $"Error loading burial schedule: {ex.Message}"
                ).Wait();

            }
        }

       
        // ---------------------------------------------------------
        // SEND NOK NOTIFICATION
        // ---------------------------------------------------------
        public async Task<IActionResult> OnPostNotifyAsync(int id)
        {
            try
            {
                var b = await (
                    from a in _context.tbl_GraveAllocations
                    join g in _context.tbl_Graves on a.sys_graveId equals g.sys_graveId
                    join s in _context.tbl_Sections on g.sys_sectionId equals s.sys_sectionId
                    join d in _context.tbl_Deceased on a.sys_deceasedId equals d.sys_deceasedId
                    where a.sys_allocationId == id
                    select new
                    {
                        d.sys_deceasedFullName,
                        d.sys_deceasedNextOfKinName,
                        d.sys_deceasedNextOfKinPhone,
                        d.sys_deceasedNextOfKinEmail,
                        g.sys_plotNumber,
                        s.sys_sectionName,
                        a.sys_dateTimeOfBurial
                    }
                ).FirstOrDefaultAsync();

                if (b == null)
                {
                    TempData["ErrorMessage"] = "Burial record not found.";
                    return RedirectToPage();
                }

                await _notificationService.SendNextOfKinNotificationAsync(
                    b.sys_deceasedNextOfKinEmail,
                    b.sys_deceasedNextOfKinPhone,
                    b.sys_deceasedFullName,
                    b.sys_dateTimeOfBurial,
                    b.sys_plotNumber,
                    b.sys_sectionName
                );

                TempData["SuccessMessage"] = "Notification sent to next of kin.";
                await _appLogger.LogActivityAsync(
                    _userManager.GetUserId(User),
                    "Notify",
                    $"Sent burial notification for allocation ID {id} to next of kin."
                );
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to send notification.";
                await _appLogger.LogActivityAsync(
                    _userManager.GetUserId(User),
                    "Error",
                    $"Error sending burial notification for allocation ID {id}: {ex.Message}"
                );
                return RedirectToPage();
            }
        }
    }

    public class BurialScheduleVM
    {
        public int AllocationId { get; set; }
        public int GraveId { get; set; }
        public string PlotNumber { get; set; }
        public string SectionName { get; set; }
        public string DeceasedName { get; set; }
        public string NextOfKinName { get; set; }
        public string NextOfKinContact { get; set; }
        public string NextOfKinEmail { get; set; }
        public string CustomerName { get; set; }
        public string RepatriationSource { get; set; }
        public DateTime BurialDateTime { get; set; }
    }
}
