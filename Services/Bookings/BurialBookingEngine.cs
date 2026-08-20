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
        private readonly BookingNotificationService _bookingNotificationService;

        public BurialBookingEngine(
            ApplicationDbContext db,
            WhatsAppNotificationService notificationService,
            BookingNotificationService bookingNotificationService)
        {
            _db = db;
            _notificationService = notificationService;
            _bookingNotificationService = bookingNotificationService;
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
        public async Task<(List<string> slots, string message)> GetAvailableSlotsAsync(
    DateOnly date,
    int sectionId)
        {
            var settings = await _db.SystemSettings
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (settings == null)
                return (new(), "System settings not configured.");

            var section = await _db.tbl_Sections
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.sys_sectionId == sectionId);

            if (section == null)
                return (new(), "Invalid section.");

            var dayStart = date.ToDateTime(new TimeOnly(0, 0));
            var dayEnd = dayStart.AddDays(1);

            // Overall cemetery capacity
            var bookingCount = await _db.tbl_BurialBookings
                .Where(b =>
                    b.sys_requestedBurialDateTime >= dayStart &&
                    b.sys_requestedBurialDateTime < dayEnd &&
                    b.sys_bookingStatus != "Cancelled" &&
                    b.sys_bookingStatus != "Rejected" &&
                    b.sys_bookingStatus != "Expired")
                .CountAsync();

            if (bookingCount >= settings.MaxBurialsPerDay)
                return (new(), "Daily burial capacity reached.");

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

            // Bookings for THIS section only
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

            var available =
                allSlots
                    .Where(slot =>
                    {
                        var used =
                            sectionBookings.Count(b =>
                                b.Hour == slot.Hour &&
                                b.Minute == slot.Minute);

                        return used < maxConcurrent;
                    })
                    .Select(slot =>
                        slot.ToString("yyyy-MM-ddTHH:mm"))
                    .ToList();

            return available.Count == 0
                ? (new(), "No slots available for this section.")
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

            if (selectedDate > today.AddDays(365))
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

            var sectionName = await _db.tbl_Sections
    .Where(s => s.sys_sectionId == sectionId)
    .Select(s => s.sys_sectionName)
    .FirstOrDefaultAsync();

            bool isTemporary = false;

            if(sectionName == "Zororo Temporary Booking")
            {
                isTemporary = true;
            }

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
                sys_paymentReceived = paymentReceived,
                sys_suspense_amount = 0,
                sys_isTemporary = isTemporary
                
            };

            _db.tbl_BurialBookings.Add(booking);
            await _db.SaveChangesAsync();

            //Send booking notification to next of kin
            var bookedSection = await _db.tbl_Sections.AsNoTracking().FirstOrDefaultAsync(s => s.sys_sectionId == booking.sys_sectionId);

            await _notificationService.SendBookingNotification(customer.sys_customerPhone, "booking_notification",
                $"{booking.sys_deceasedFirstName} {booking.sys_deceasedLastName}", booking.sys_requestedBurialDateTime.ToString("f"),
                bookedSection.sys_sectionName, customer.sys_paymentMadeBy);

            await _bookingNotificationService.NotifyBookingCreatedAsync(
                booking,
                customer,
                bookedSection);

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
