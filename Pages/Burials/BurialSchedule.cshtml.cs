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
using ZororoMemorialPark.Services.WhatsApp;
using Microsoft.AspNetCore.Hosting;

namespace ZororoMemorialPark.Pages.Burials
{
    public class BurialScheduleModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notificationService;
        private readonly SystemSettingsService _settingsService;
        private readonly IAppLogger _appLogger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BurialSchedulePdfService _pdfService;
        private readonly WhatsAppNotificationService _whatsApp;
        private readonly IWebHostEnvironment _env;

        public BurialScheduleModel(
            ApplicationDbContext context,
            NotificationService notificationService,
            SystemSettingsService settingsService, IAppLogger appLogger, UserManager<IdentityUser> userManager,
            BurialSchedulePdfService pdfService, WhatsAppNotificationService whatsApp, IWebHostEnvironment env)
        {
            _context = context;
            _notificationService = notificationService;
            _settingsService = settingsService;
            _appLogger = appLogger;
            _userManager = userManager;
            _pdfService = pdfService;
            _whatsApp = whatsApp;
            _env = env;
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

                // ---------------------------------------------------------
                // STEP 1: LOAD BOOKINGS (TODAY + FUTURE ONLY)
                // ---------------------------------------------------------
                var bookingsRaw = await (
                    from b in _context.tbl_BurialBookings
                    join s in _context.tbl_Sections on b.sys_sectionId equals s.sys_sectionId
                    join c in _context.tbl_Customers on b.sys_customerId equals c.sys_customerId
                    where b.sys_requestedBurialDateTime >= today
                          && b.sys_bookingStatus != "Cancelled"
                          && b.sys_bookingStatus != "Rejected"
                          && b.sys_bookingStatus != "Expired"
                    select new
                    {
                        b.sys_bookingId,
                        b.sys_requestedBurialDateTime,
                        b.sys_bookingStatus,
                        b.sys_invoiceId,

                        DeceasedFullName = b.sys_deceasedFirstName + " " + b.sys_deceasedLastName,
                        s.sys_sectionName,
                        c.sys_paymentMadeBy,
                        c.sys_customerPhone
                    }
                ).ToListAsync();

                // ---------------------------------------------------------
                // STEP 2: LOAD INVOICE TOTALS
                // ---------------------------------------------------------
                var invoiceIds = bookingsRaw
                    .Where(x => x.sys_invoiceId.HasValue)
                    .Select(x => x.sys_invoiceId.Value)
                    .Distinct()
                    .ToList();

                var invoiceTotals = await _context.tbl_InvoiceItems
                    .Where(i => invoiceIds.Contains(i.sys_invoiceId))
                    .GroupBy(i => i.sys_invoiceId)
                    .Select(g => new
                    {
                        InvoiceId = g.Key,
                        Total = g.Sum(x => x.sys_itemCost)
                    })
                    .ToDictionaryAsync(x => x.InvoiceId, x => (decimal)x.Total);

                // ---------------------------------------------------------
                // STEP 3: LOAD PAYMENTS (tbl_BurialPayments)
                // ---------------------------------------------------------
                var payments = await _context.tbl_BurialPayments
                    .Where(p => invoiceIds.Contains(p.sys_invoiceId))
                    .GroupBy(p => p.sys_invoiceId)
                    .Select(g => new
                    {
                        InvoiceId = g.Key,
                        Paid = g.Sum(x => x.sys_amountPaid)
                    })
                    .ToDictionaryAsync(x => x.InvoiceId, x => (decimal)x.Paid);

                // ---------------------------------------------------------
                // STEP 4: MAP TO VIEW MODEL
                // ---------------------------------------------------------
                var bookings = bookingsRaw
                    .Select(b => new BurialScheduleVM
                    {
                        BookingId = b.sys_bookingId,

                        BurialDateTime = b.sys_requestedBurialDateTime,
                        BookingStatus = b.sys_bookingStatus,

                        DeceasedFullName = b.DeceasedFullName,
                        SectionName = b.sys_sectionName,
                        CustomerName = b.sys_paymentMadeBy,
                        CustomerNumber = b.sys_customerPhone,

                        InvoiceTotal = b.sys_invoiceId.HasValue && invoiceTotals.ContainsKey(b.sys_invoiceId.Value)
                            ? invoiceTotals[b.sys_invoiceId.Value]
                            : 0,

                        AmountPaid = b.sys_invoiceId.HasValue && payments.ContainsKey(b.sys_invoiceId.Value)
                            ? payments[b.sys_invoiceId.Value]
                            : 0
                    })
                    .OrderBy(b => b.BurialDateTime)
                    .ToList();

                // ---------------------------------------------------------
                // STEP 5: DATE FILTER (OPTIONAL)
                // ---------------------------------------------------------
                if (FilterDate.HasValue)
                {
                    var day = FilterDate.Value.Date;
                    var dayEnd = day.AddDays(1);

                    bookings = bookings
                        .Where(b => b.BurialDateTime >= day && b.BurialDateTime < dayEnd)
                        .ToList();
                }

                // ---------------------------------------------------------
                // STEP 6: ASSIGN PAGE PROPERTIES
                // ---------------------------------------------------------
                AllBurials = bookings;

                TodaysBurials = bookings
                    .Where(b => b.BurialDateTime.Date == today)
                    .ToList();

                UpcomingBurials = bookings
                    .Where(b => b.BurialDateTime.Date > today)
                    .ToList();

                await _appLogger.LogActivityAsync(
                    _userManager.GetUserId(User),
                    "View",
                    "Viewed burial schedule (bookings-based)"
                );
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to load burial schedule.";
                await _appLogger.LogActivityAsync(
                    _userManager.GetUserId(User),
                    "Error",
                    $"Burial schedule error: {ex.Message}"
                );
            }
        }


        public async Task<IActionResult> OnPostSendScheduleAsync(string phoneNumber)
        {
            try
            {
                // reload data (reuse your logic)
                await OnGetAsync();

                var settings = await _settingsService.GetSettingsAsync();

                var logoPath = Path.Combine(_env.ContentRootPath, "wwwroot", settings.LogoPath.TrimStart('/'));

                var pdfBytes = _pdfService.GenerateSchedulePdf(AllBurials, settings, logoPath);

                var fileName = $"BurialSchedule_{DateTime.Now:yyyyMMddHHmm}.pdf";
                var relativePath = $"/documents/{fileName}";
                var fullPath = Path.Combine(_env.WebRootPath, "documents", fileName);

                await System.IO.File.WriteAllBytesAsync(fullPath, pdfBytes);

                var publicUrl = $"{Request.Scheme}://{Request.Host}{relativePath}";
                //var tempurl = "https://zororomemorial.azurewebsites.net/documents/invoice_1097.pdf";
                // SEND WHATSAPP (reuse your NotificationService)
                await _whatsApp.SendPdfMessage(
                    phoneNumber,
                    publicUrl,"send_document",$"BurialSchedule-{DateTime.Now}","Good day","Burial Schedule"
                );

                TempData["SuccessMessage"] = "Schedule sent successfully via WhatsApp.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to send schedule.";
            }

            return RedirectToPage();
        }


        public async Task<IActionResult> OnGetDownloadScheduleAsync()
        {
            try
            {
                // reload data
                await OnGetAsync();

                var settings = await _settingsService.GetSettingsAsync();

                var logoPath = Path.Combine(_env.ContentRootPath, "wwwroot", settings.LogoPath.TrimStart('/'));

                var pdfBytes = _pdfService.GenerateSchedulePdf(AllBurials, settings, logoPath);

                var fileName = $"BurialSchedule_{DateTime.Now:yyyyMMddHHmm}.pdf";

                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Failed to generate schedule PDF.";
                return RedirectToPage();
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
        public int BookingId { get; set; }

        // Booking
        public DateTime BurialDateTime { get; set; }
        public string BookingStatus { get; set; }

        // Deceased (from booking table)
        public string DeceasedFullName { get; set; }

        // Section
        public string SectionName { get; set; }

        // Customer
        public string CustomerName { get; set; }

        public string CustomerNumber { get; set; }

        // FINANCIALS (derived)
        public decimal InvoiceTotal { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal Balance => InvoiceTotal - AmountPaid;

        public bool IsFullyPaid => InvoiceTotal > 0 && Balance <= 0;
    }


}
