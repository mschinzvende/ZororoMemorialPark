using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ZororoMemorialPark.Data;
using ZororoMemorialPark.Services;

namespace ZororoMemorialPark.Pages.Burials
{
    public class EditBurialModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly SystemSettingsService _settings;
        private readonly NotificationService _notify;

        public EditBurialModel(ApplicationDbContext context, SystemSettingsService settings, NotificationService notify)
        {
            _context = context;
            _settings = settings;
            _notify = notify;
        }

        [BindProperty]
        public EditBurialVM Input { get; set; }

        public class EditBurialVM
        {
            public int AllocationId { get; set; }
            public string DeceasedName { get; set; }
            public string PlotNumber { get; set; }
            public string SectionName { get; set; }
            public string DateOfDeath { get; set; }
            public string BurialDate { get; set; }
            public DateTime? ExistingDateTime { get; set; }
            public string NextOfKinEmail { get; set; }
        }

        // -------------------------
        // GET PAGE
        // -------------------------
        public async Task<IActionResult> OnGetAsync(int id)
        {
            try
            {
                var a = await (
                    from alloc in _context.tbl_GraveAllocations
                    join g in _context.tbl_Graves on alloc.sys_graveId equals g.sys_graveId
                    join s in _context.tbl_Sections on g.sys_sectionId equals s.sys_sectionId
                    join d in _context.tbl_Deceased on alloc.sys_deceasedId equals d.sys_deceasedId
                    where alloc.sys_allocationId == id
                    select new EditBurialVM
                    {
                        AllocationId = alloc.sys_allocationId,
                        DeceasedName = d.sys_deceasedFullName,
                        PlotNumber = g.sys_plotNumber,
                        SectionName = s.sys_sectionName,
                        DateOfDeath = d.sys_deceasedDOD.ToString("yyyy-MM-dd"),
                        BurialDate = alloc.sys_dateTimeOfBurial.ToString("yyyy-MM-dd"),
                        ExistingDateTime = alloc.sys_dateTimeOfBurial,
                        NextOfKinEmail = d.sys_deceasedNextOfKinEmail
                    }
                ).FirstOrDefaultAsync();

                if (a == null)
                {
                    TempData["ErrorMessage"] = "Burial record not found.";
                    return RedirectToPage("/Burials/BurialSchedule");
                }

                Input = a;
                return Page();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while loading burial details.";
                return RedirectToPage("/Burials/BurialSchedule");
            }
        }

        // -------------------------
        // POST (SAVE EDIT)
        // -------------------------
        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                var alloc = await _context.tbl_GraveAllocations
                    .FirstOrDefaultAsync(a => a.sys_allocationId == Input.AllocationId);

                if (alloc == null)
                {
                    TempData["ErrorMessage"] = "Record not found.";
                    return RedirectToPage("/Burials/BurialSchedule");
                }

                // Ensure burial time is valid
                if (!DateTime.TryParse(Request.Form["burialDateTime"], out DateTime newBurialDateTime))
                {
                    TempData["ErrorMessage"] = "Invalid burial time selected.";
                    return RedirectToPage(new { id = Input.AllocationId });
                }

                // Update schedule
                alloc.sys_dateTimeOfBurial = newBurialDateTime;
                await _context.SaveChangesAsync();

                // Send NOK Email
                if (!string.IsNullOrWhiteSpace(Input.NextOfKinEmail))
                {
                    try
                    {
                        await _notify.SendEmailAsync(
                            Input.NextOfKinEmail,
                            "Burial Schedule Updated",
                            $"The burial date and time for {Input.DeceasedName} has been updated to {newBurialDateTime:dddd dd MMM yyyy HH:mm}."
                        );
                    }
                    catch
                    {
                        // Optional: Do not block process on email error
                    }
                }

                TempData["SuccessMessage"] = "Burial schedule updated successfully.";
                return RedirectToPage("/Burials/BurialSchedule");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An unexpected error occurred while updating the burial.";
                return RedirectToPage("/Burials/BurialSchedule");
            }
        }

        // -------------------------
        // AJAX: GET AVAILABLE SLOTS
        // -------------------------
        public async Task<JsonResult> OnGetAvailableBurialSlots(DateTime burialDate)
        {
            try
            {
                var cfg = await _settings.GetSettingsAsync();

                var existing = await _context.tbl_GraveAllocations
                    .Where(a => a.sys_dateTimeOfBurial.Date == burialDate.Date)
                    .Select(a => a.sys_dateTimeOfBurial)
                    .ToListAsync();

                if (existing.Count >= cfg.MaxBurialsPerDay)
                {
                    return new JsonResult(new { slots = new List<string>(), message = "Maximum burials already scheduled." });
                }

                var slots = new List<string>();
                DateTime start = burialDate.Date.AddHours(8);
                DateTime end = burialDate.Date.AddHours(17);

                for (DateTime t = start; t <= end; t = t.AddMinutes(cfg.MinutesBetweenBurials))
                {
                    bool clash = existing.Any(e => Math.Abs((e - t).TotalMinutes) < cfg.MinutesBetweenBurials);
                    if (!clash)
                        slots.Add(t.ToString("yyyy-MM-ddTHH:mm"));
                }

                return new JsonResult(new { slots, message = slots.Count == 0 ? "No available slots." : "" });
            }
            catch
            {
                return new JsonResult(new { slots = new List<string>(), message = "Error calculating available slots." });
            }
        }
    }
}
