using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ZororoMemorialPark.Data;
using ZororoMemorialPark.Data.Models;
using ZororoMemorialPark.Services.WhatsApp;

namespace ZororoMemorialPark.Services.Bookings
{
    public class BurialBookingEngine
    {
        private readonly ApplicationDbContext _db;
        private readonly WhatsAppNotificationService _notificationService;

        public BurialBookingEngine(ApplicationDbContext db, WhatsAppNotificationService notificationService)
        {
            _db = db;
            _notificationService = notificationService;
        }

        // ======================================================
        // SECTIONS WITH AVAILABLE GRAVES
        // ======================================================
        public async Task<List<tbl_Sections>> GetAvailableSectionsAsync()
        {
            return await _db.tbl_Sections
                .AsNoTracking()
                .OrderBy(s => s.sys_sectionName)
                .ToListAsync();
        }

        // ======================================================
        // AVAILABLE TIME SLOTS FOR A DATE
        // ======================================================
        public async Task<(List<string> slots, string message)> GetAvailableSlotsAsync(DateOnly date)
        {
            var settings = await _db.SystemSettings.AsNoTracking().FirstOrDefaultAsync();
            if (settings == null)
                return (new(), "System settings not configured.");

            var dayStart = date.ToDateTime(new TimeOnly(0, 0));
            var dayEnd = dayStart.AddDays(1);

            var bookingCount = await _db.tbl_BurialBookings
                .Where(b =>
                    b.sys_requestedBurialDateTime >= dayStart &&
                    b.sys_requestedBurialDateTime < dayEnd &&
                    b.sys_bookingStatus != "Cancelled" &&
                    b.sys_bookingStatus != "Rejected" &&
                    b.sys_bookingStatus != "Expired")
                .CountAsync();

            var allocationCount = await _db.tbl_GraveAllocations
                .Where(a =>
                    a.sys_dateTimeOfBurial >= dayStart &&
                    a.sys_dateTimeOfBurial < dayEnd)
                .CountAsync();

            if (bookingCount + allocationCount >= settings.MaxBurialsPerDay)
                return (new(), "Daily burial capacity reached.");

            var minutes = Math.Max(5, settings.MinutesBetweenBurials);
            var window = TimeSpan.FromMinutes(minutes);

            var start = date.ToDateTime(new TimeOnly((int)settings.dayStartTime, 0));
            var end = date.ToDateTime(new TimeOnly((int)settings.dayEndTime, 0));

            var allSlots = new List<DateTime>();
            for (var t = start; t <= end; t = t.AddMinutes(minutes))
                allSlots.Add(t);

            var blockedTimes = await _db.tbl_BurialBookings
                .Where(b =>
                    b.sys_requestedBurialDateTime >= dayStart &&
                    b.sys_requestedBurialDateTime < dayEnd &&
                    b.sys_bookingStatus != "Cancelled" &&
                    b.sys_bookingStatus != "Rejected" &&
                    b.sys_bookingStatus != "Expired")
                .Select(b => b.sys_requestedBurialDateTime)
                .Concat(
                    _db.tbl_GraveAllocations
                        .Where(a =>
                            a.sys_dateTimeOfBurial >= dayStart &&
                            a.sys_dateTimeOfBurial < dayEnd)
                        .Select(a => a.sys_dateTimeOfBurial)
                )
                .ToListAsync();

            var available = allSlots
                .Where(s => !blockedTimes.Any(bt => (bt - s).Duration() < window))
                .Select(s => s.ToString("yyyy-MM-ddTHH:mm"))
                .ToList();

            return available.Count == 0
                ? (new(), "No slots available.")
                : (available, "");
        }

        // ======================================================
        // CREATE BOOKING (USED BY UI + WHATSAPP)
        // ======================================================
        public async Task<(bool success, string message, tbl_BurialBookings? booking)>
            CreateBookingAsync(
                int sectionId,
                string slotIso,
                string deceasedFirstName,
                string deceasedLastName,
                string paymentMadeBy,
                string customerPhone,
                int? agentId,
                string? receiptPath,
                bool paymentReceived)
        {
            if (!DateTime.TryParseExact(
                slotIso,
                "yyyy-MM-ddTHH:mm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var selected))
                return (false, "Invalid burial time slot.", null);

            var today = DateOnly.FromDateTime(DateTime.Now);
            var selectedDate = DateOnly.FromDateTime(selected);

            if (selectedDate < today)
                return (false, "Burial date cannot be in the past.", null);

            if (selectedDate > today.AddDays(7))
                return (false, "Burial date cannot exceed 7 days in advance.", null);

            if (selectedDate == today && selected <= DateTime.Now)
                return (false, "Burial time must be in the future.", null);

            var customer = new tbl_Customers
            {
                sys_paymentMadeBy = string.IsNullOrWhiteSpace(paymentMadeBy) ? "n/a" : paymentMadeBy.Trim(),
                sys_customerPhone = string.IsNullOrWhiteSpace(customerPhone) ? "n/a" : customerPhone.Trim()
            };

            _db.tbl_Customers.Add(customer);
            await _db.SaveChangesAsync();

            var booking = new tbl_BurialBookings
            {
                sys_deceasedFirstName = deceasedFirstName.Trim(),
                sys_deceasedLastName = deceasedLastName.Trim(),
                sys_requestedBurialDateTime = selected,
                sys_sectionId = sectionId,
                sys_customerId = customer.sys_customerId,
                sys_agentId = agentId,
                sys_bookingStatus = "PendingPayment",
                sys_createdOn = DateTime.Now,
                sys_expiresOn = selected.AddDays(14),
                sys_receiptPath = receiptPath,
                sys_paymentReceived = paymentReceived
                
            };

            _db.tbl_BurialBookings.Add(booking);
            await _db.SaveChangesAsync();

            //Send booking notification to next of kin
            var bookedSection = await _db.tbl_Sections.AsNoTracking().FirstOrDefaultAsync(s => s.sys_sectionId == booking.sys_sectionId);

            await _notificationService.SendBookingNotification(customer.sys_customerPhone, "booking_notification",
                $"{booking.sys_deceasedFirstName} {booking.sys_deceasedLastName}", booking.sys_requestedBurialDateTime.ToString("f"),
                bookedSection.sys_sectionName, customer.sys_paymentMadeBy);

            return (true, "Booking created.", booking);
        }

        // ======================================================
        // OTP HASH UTILITY
        // ======================================================
        public static string HashOtp(string input)
        {
            using var sha = SHA256.Create();
            return Convert.ToHexString(
                sha.ComputeHash(Encoding.UTF8.GetBytes(input)));
        }
    }
}
