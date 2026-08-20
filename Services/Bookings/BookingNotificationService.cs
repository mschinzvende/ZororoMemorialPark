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

            var bookingType = booking.sys_isTemporary
                ? "temporary burial booking"
                : "burial booking";

            var message =
                $"New {bookingType} created.\n" +
                $"Reference: #{booking.sys_bookingId}\n" +
                $"Deceased: {booking.sys_deceasedFirstName} {booking.sys_deceasedLastName}\n" +
                $"Burial: {burialDate}\n" +
                $"Section: {section?.sys_sectionName ?? "Not specified"}\n" +
                $"Payer: {customer.sys_paymentMadeBy}\n" +
                $"Customer phone: {customer.sys_customerPhone}";

            foreach (var recipient in recipients
                .GroupBy(x => x.PhoneNumber)
                .Select(group => group.First()))
            {
                try
                {
                    var response = await _whatsApp.SendTextMessage(
                        recipient.PhoneNumber!,
                        message);

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
