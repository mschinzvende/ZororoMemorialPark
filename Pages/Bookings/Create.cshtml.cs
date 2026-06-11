using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ZororoMemorialPark.Data.Models;
using ZororoMemorialPark.Data;
using ZororoMemorialPark.Services.WhatsApp;
using static System.Collections.Specialized.BitVector32;

namespace ZororoMemorialPark.Pages.Bookings
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly WhatsAppNotificationService _whatsApp;

        public CreateModel(ApplicationDbContext db, WhatsAppNotificationService whatsApp)
        {
            _db = db;
            _whatsApp = whatsApp;
        }

        public List<tbl_Sections> Sections { get; set; } = new();

        [BindProperty] public int SectionId { get; set; }

        // Date-first UI pieces
        [BindProperty] public DateOnly burialDate { get; set; }
        [BindProperty] public string burialDateTime { get; set; } // selected slot (ISO string)

        [BindProperty] public string DeceasedFirstName { get; set; }
        [BindProperty] public string DeceasedLastName { get; set; }

        [BindProperty] public string PaymentMadeBy { get; set; }
        [BindProperty] public string CustomerPhone { get; set; }

        public async Task OnGetAsync()
        {
            // Only show sections that still have available graves 
            Sections = await _db.tbl_Sections.AsNoTracking()
                .OrderBy(s => s.sys_sectionName)
                .ToListAsync();
        }

        // Slots API: called from JS after date change
        public async Task<IActionResult> OnGetAvailableBurialSlotsAsync(
    string burialDate,
    int sectionId)
        {
            if (!DateOnly.TryParseExact(
                burialDate,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date))
            {
                return new JsonResult(new
                {
                    slots = Array.Empty<string>(),
                    message = "Invalid date."
                });
            }

            var settings = await _db.SystemSettings
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (settings == null)
            {
                return new JsonResult(new
                {
                    slots = Array.Empty<string>(),
                    message = "System settings not configured."
                });
            }
            Console.WriteLine($"SECTION ID RECEIVED = {sectionId}");
            var section = await _db.tbl_Sections
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.sys_sectionId == sectionId);

            if (section == null)
            {
                return new JsonResult(new
                {
                    slots = Array.Empty<string>(),
                    message = "Invalid section."
                });
            }

            var dayStart = date.ToDateTime(
                new TimeOnly(0, 0));

            var dayEnd = dayStart.AddDays(1);

            // Overall cemetery capacity
            var activeBookingCount =
                await _db.tbl_BurialBookings
                    .AsNoTracking()
                    .Where(b =>
                        b.sys_requestedBurialDateTime >= dayStart &&
                        b.sys_requestedBurialDateTime < dayEnd &&
                        b.sys_bookingStatus != "Cancelled" &&
                        b.sys_bookingStatus != "Rejected" &&
                        b.sys_bookingStatus != "Expired")
                    .CountAsync();

           

            if (activeBookingCount  >=
                settings.MaxBurialsPerDay)
            {
                return new JsonResult(new
                {
                    slots = Array.Empty<string>(),
                    message = "Daily capacity reached. Try a new date."
                });
            }

            var minutes =
                Math.Max(
                    5,
                    settings.MinutesBetweenBurials);

            var start = date.ToDateTime(
                new TimeOnly(
                    (int)settings.dayStartTime,
                    0));

            var end = date.ToDateTime(
                new TimeOnly(
                    (int)settings.dayEndTime,
                    0));

            var allSlots = new List<DateTime>();

            for (var t = start;
                 t <= end;
                 t = t.AddMinutes(minutes))
            {
                allSlots.Add(t);
            }

            // Section-specific bookings
            var sectionBookings =
                await _db.tbl_BurialBookings
                    .AsNoTracking()
                    .Where(b =>
                        b.sys_sectionId == sectionId &&
                        b.sys_requestedBurialDateTime >= dayStart &&
                        b.sys_requestedBurialDateTime < dayEnd &&
                        b.sys_bookingStatus != "Cancelled" &&
                        b.sys_bookingStatus != "Rejected" &&
                        b.sys_bookingStatus != "Expired")
                    .Select(b =>
                        b.sys_requestedBurialDateTime)
                    .ToListAsync();


            var maxConcurrent =
                Math.Max(
                    1,
                    (int)section.sys_maxConcurrentBurialsPerSlot);



            ////////DEBUG
            
            Console.WriteLine(
            $"SECTION={section.sys_sectionName}");

            Console.WriteLine(
                $"MAX CONCURRENT={section.sys_maxConcurrentBurialsPerSlot}");

            Console.WriteLine(
                $"BOOKINGS={sectionBookings.Count}");

            foreach (var booking in sectionBookings)
            {
                Console.WriteLine(
                    $"BOOKING={booking:yyyy-MM-dd HH:mm:ss}");
            }
            ///


            var available = allSlots
                .Where(slot =>
                {
                    var used = sectionBookings.Count(b =>
                        b.Hour == slot.Hour &&
                        b.Minute == slot.Minute);

                    return used < maxConcurrent;
                })
                .Select(slot =>
                    slot.ToString("yyyy-MM-ddTHH:mm"))
                .ToList();


            if (available.Count == 0)
            {
                return new JsonResult(new
                {
                    slots = Array.Empty<string>(),
                    message =
                        "No slots available for this section."
                });
            }

            return new JsonResult(new
            {
                slots = available,
                message = ""
            });
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await OnGetAsync(); // repopulate sections on validation errors

            // -----------------------------
            // SECTION VALIDATION
            // -----------------------------
            if (SectionId <= 0)
            {
                ModelState.AddModelError("", "Section is required.");
                return Page();
            }

            //We now dont care if the section has graves

            /*var hasAvailable = await _db.tbl_Graves.AnyAsync(g =>
                g.sys_sectionId == SectionId &&
                g.sys_graveSaleStatus == "Unsold");

            if (!hasAvailable)
            {
                ModelState.AddModelError("", "Selected section has no available graves.");
                return Page();
            }*/

            // -----------------------------
            // TIME SLOT PARSING
            // -----------------------------
            if (string.IsNullOrWhiteSpace(burialDateTime))
            {
                ModelState.AddModelError("", "Please select a burial time slot.");
                return Page();
            }

            if (!DateTime.TryParseExact(
                    burialDateTime,
                    "yyyy-MM-ddTHH:mm",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var selected))
            {
                ModelState.AddModelError("", "Invalid burial time slot.");
                return Page();
            }

            // -----------------------------
            // DECEASED NAME VALIDATION
            // -----------------------------
            if (string.IsNullOrWhiteSpace(DeceasedFirstName) ||
                string.IsNullOrWhiteSpace(DeceasedLastName))
            {
                ModelState.AddModelError("", "Deceased name and surname are required.");
                return Page();
            }

            // -----------------------------
            // DATE & TIME RULES
            // -----------------------------
            var now = DateTime.Now;
            var today = DateOnly.FromDateTime(now);
            var selectedDate = DateOnly.FromDateTime(selected);

            // Cannot book in the past (date)
            if (selectedDate < today)
            {
                ModelState.AddModelError("", "Burial date cannot be in the past.");
                return Page();
            }

            // Cannot exceed 7 days in advance
            if (selectedDate > today.AddDays(365))
            {
                ModelState.AddModelError("", "Burial date cannot be more than 7 days in advance.");
                return Page();
            }

            // If booking is TODAY ? time must be in the future
            if (selectedDate == today && selected <= now)
            {
                ModelState.AddModelError("", "For today, burial time must be later than the current time.");
                return Page();
            }

            // -----------------------------
            // CREATE CUSTOMER
            // -----------------------------
            var customer = new tbl_Customers
            {
                sys_paymentMadeBy = string.IsNullOrWhiteSpace(PaymentMadeBy)
                    ? "n/a"
                    : PaymentMadeBy.Trim(),
                sys_customerPhone = string.IsNullOrWhiteSpace(CustomerPhone)
                    ? "n/a"
                    : CustomerPhone.Trim()
            };

            _db.tbl_Customers.Add(customer);
            await _db.SaveChangesAsync();

            // -----------------------------
            // AGENT DETECTION
            // -----------------------------
            int? agentId = null;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!string.IsNullOrWhiteSpace(userId))
            {
                var agent = await _db.tbl_Agents
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.UserId == userId);

                if (agent != null)
                    agentId = agent.sys_agentId;
            }

            // -----------------------------
            // SYSTEM SETTINGS
            // -----------------------------
            var settings = await _db.SystemSettings.AsNoTracking().FirstOrDefaultAsync();
            if (settings == null)
            {
                ModelState.AddModelError("", "System settings not configured.");
                return Page();
            }

            var minutes = Math.Max(5, settings.MinutesBetweenBurials);
            var window = TimeSpan.FromMinutes(minutes);

            var dayStart = selected.Date;
            var dayEnd = dayStart.AddDays(1);

            // -----------------------------
            // SLOT AVAILABILITY CHECK
            // -----------------------------
            var scheduledTimes = await _db.tbl_BurialBookings.AsNoTracking()
                .Where(b =>
                    b.sys_requestedBurialDateTime >= dayStart &&
                    b.sys_requestedBurialDateTime < dayEnd &&
                    b.sys_bookingStatus != "Cancelled" &&
                    b.sys_bookingStatus != "Rejected" &&
                    b.sys_bookingStatus != "Expired")
                .Select(b => b.sys_requestedBurialDateTime)
                .ToListAsync();

            var allocatedTimes = await _db.tbl_GraveAllocations.AsNoTracking()
                .Where(a =>
                    a.sys_dateTimeOfBurial >= dayStart &&
                    a.sys_dateTimeOfBurial < dayEnd)
                .Select(a => a.sys_dateTimeOfBurial)
                .ToListAsync();

           /* if (scheduledTimes.Concat(allocatedTimes)
                .Any(t => Math.Abs((t - selected).TotalMinutes) < minutes))
            {
                ModelState.AddModelError("", "Selected slot is no longer available. Please choose another slot.");
                return Page();
            }*/

            if (scheduledTimes.Count + allocatedTimes.Count >= settings.MaxBurialsPerDay)
            {
                ModelState.AddModelError("", "Daily burial capacity has been reached for this date. Please try another date.");
                return Page();
            }

            // -----------------------------
            // CREATE BOOKING
            // -----------------------------
            var booking = new tbl_BurialBookings
            {
                sys_deceasedFirstName = DeceasedFirstName.Trim(),
                sys_deceasedLastName = DeceasedLastName.Trim(),
                sys_requestedBurialDateTime = selected,
                sys_sectionId = SectionId,
                sys_customerId = customer.sys_customerId,
                sys_agentId = agentId,
                sys_bookingStatus = "PendingPayment",
                sys_createdOn = DateTime.Now,
                sys_expiresOn = selected.AddDays(14)
            };

            _db.tbl_BurialBookings.Add(booking);
            await _db.SaveChangesAsync();

            //send whatsapp notification

            var bookedSection  = await _db.tbl_Sections.AsNoTracking().FirstOrDefaultAsync(s => s.sys_sectionId == booking.sys_sectionId);
           // var customer = await _db.tbl_Customers.AsNoTracking().FirstOrDefaultAsync(c => c.sys_customerId == booking.sys_customerId);
            
            var response = await _whatsApp.SendBookingNotification(customer.sys_customerPhone, "booking_notification",
                $"{booking.sys_deceasedFirstName} {booking.sys_deceasedLastName}", booking.sys_requestedBurialDateTime.ToString("f"),
                bookedSection.sys_sectionName, customer.sys_paymentMadeBy);

            if (response.IsSuccessStatusCode)
            {
                TempData["Ok"] = "Booking Notification Sent to Customer";
            }

            else
            {
                TempData["Err"] = $"Notification Failed to Send: {response.ReasonPhrase}";
            }

            return RedirectToPage("Details", new { id = booking.sys_bookingId });
        }

    }
}
