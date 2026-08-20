using Microsoft.EntityFrameworkCore;
using ZororoMemorialPark.Data;
using ZororoMemorialPark.Data.Models;
using ZororoMemorialPark.Services.WhatsApp;

namespace ZororoMemorialPark.Services.Bookings
{
    public class BookingNotificationService
    {
        private readonly ApplicationDbContext _db;
        private readonly WhatsAppNotificationService _whatsApp;
        private readonly ILogger<BookingNotificationService> _logger;

        public BookingNotificationService(
            ApplicationDbContext db,
            WhatsAppNotificationService whatsApp,
            ILogger<BookingNotificationService> logger)
        {
            _db = db;
            _whatsApp = whatsApp;
            _logger = logger;
        }

        public async Task NotifyBookingCreatedAsync(
            tbl_BurialBookings booking,
            tbl_Customers customer,
            tbl_Sections? section)
        {
            var recipients = await (
                from recipient in _db.tbl_BookingNotificationRecipients.AsNoTracking()
                join user in _db.Users.AsNoTracking() on recipient.UserId equals user.Id
                where !string.IsNullOrWhiteSpace(user.PhoneNumber)
                select new
                {
                    user.UserName,
                    user.PhoneNumber
                })
                .ToListAsync();

            if (recipients.Count == 0)
                return;

            var burialDate = booking.sys_isTemporary
                ? "To be advised"
                : booking.sys_requestedBurialDateTime.ToString("f");

            foreach (var recipient in recipients
                .GroupBy(x => x.PhoneNumber)
                .Select(group => group.First()))
            {
                try
                {
                    var response = await _whatsApp.SendInternalBookingNotification(
                        recipient.PhoneNumber!,
                        $"#{booking.sys_bookingId}",
                        $"{booking.sys_deceasedFirstName} {booking.sys_deceasedLastName}",
                        burialDate,
                        section?.sys_sectionName ?? "Not specified",
                        customer.sys_paymentMadeBy,
                        customer.sys_customerPhone);

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogWarning(
                            "Booking notification for booking {BookingId} was not sent to user {UserName}. HTTP {StatusCode}",
                            booking.sys_bookingId,
                            recipient.UserName,
                            response.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Booking notification for booking {BookingId} could not be sent to user {UserName}",
                        booking.sys_bookingId,
                        recipient.UserName);
                }
            }
        }
    }
}
