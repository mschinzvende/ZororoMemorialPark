using Microsoft.EntityFrameworkCore;
using ZororoMemorialPark.Data;
using ZororoMemorialPark.Services.WhatsApp;

namespace ZororoMemorialPark.Services;

/// <summary>
/// Sends at most one installment reminder per reservation per day. It runs in-process,
/// so it requires the Azure App Service to remain enabled for scheduled notifications.
/// </summary>
public class ReservationInstallmentReminderService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReservationInstallmentReminderService> _logger;

    public ReservationInstallmentReminderService(
        IServiceProvider serviceProvider,
        ILogger<ReservationInstallmentReminderService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Reservation installment reminder service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SendDueRemindersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to send reservation installment reminders.");
            }

            await Task.Delay(GetDelayUntilNextRun(), stoppingToken);
        }
    }

    private static TimeSpan GetDelayUntilNextRun()
    {
        var now = DateTime.Now;
        var next = DateTime.Today.AddHours(8);
        if (now >= next)
        {
            next = next.AddDays(1);
        }

        return next - now;
    }

    private async Task SendDueRemindersAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();
        var whatsApp = scope.ServiceProvider.GetRequiredService<WhatsAppNotificationService>();
        var today = DateOnly.FromDateTime(DateTime.Today);

        var dueReservations = await context.tbl_GraveReservations
            .Where(x => x.sys_nextInstallmentDueDate.HasValue &&
                        x.sys_nextInstallmentDueDate.Value <= today &&
                        x.sys_status != "Cancelled" &&
                        x.sys_status != "Expired" &&
                        x.sys_lastInstallmentReminderSentOn != today)
            .ToListAsync(cancellationToken);

        foreach (var reservation in dueReservations)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var customer = await context.tbl_Customers
                .FirstOrDefaultAsync(x => x.sys_customerId == reservation.sys_customerId, cancellationToken);
            if (customer == null)
            {
                continue;
            }

            var section = reservation.sys_preferredSectionId.HasValue
                ? await context.tbl_Sections.FirstOrDefaultAsync(
                    x => x.sys_sectionId == reservation.sys_preferredSectionId.Value,
                    cancellationToken)
                : null;

            var invoiceIds = await context.tbl_GraveReservationInvoices
                .Where(x => x.sys_reservationId == reservation.sys_reservationId)
                .Select(x => x.sys_invoiceId)
                .ToListAsync(cancellationToken);
            var total = await context.tbl_InvoiceItems
                .Where(x => invoiceIds.Contains(x.sys_invoiceId))
                .SumAsync(x => (double?)x.sys_itemCost, cancellationToken) ?? 0;
            var paid = await context.tbl_IncomePayments
                .Where(x => invoiceIds.Contains(x.sys_invoiceId))
                .SumAsync(x => (double?)x.sys_amount, cancellationToken) ?? 0;
            var balance = Math.Max(0, total - paid);

            if (balance <= 0)
            {
                continue;
            }

            var reference = string.IsNullOrWhiteSpace(reservation.sys_reservationReference)
                ? $"RES-{reservation.sys_reservationId}"
                : reservation.sys_reservationReference;
            var sectionName = section?.sys_sectionName ?? "your selected section";
            var dueDate = reservation.sys_nextInstallmentDueDate?.ToString("dd MMM yyyy") ?? "today";
            var sent = false;

            if (!string.IsNullOrWhiteSpace(customer.sys_customerPhone) && customer.sys_customerPhone != "n/a")
            {
                try
                {
                    var response = await whatsApp.SendReservationInstallmentReminder(
                        customer.sys_customerPhone,
                        reference,
                        customer.sys_paymentMadeBy,
                        sectionName,
                        balance.ToString("N2"),
                        dueDate);
                    sent = response.IsSuccessStatusCode;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "WhatsApp reminder failed for reservation {ReservationReference}.", reference);
                }
            }

            if (!string.IsNullOrWhiteSpace(customer.sys_customerEmail))
            {
                try
                {
                    await notificationService.SendEmailAsync(
                        customer.sys_customerEmail,
                        $"Installment reminder - {reference}",
                        $"<p>Dear {System.Net.WebUtility.HtmlEncode(customer.sys_paymentMadeBy)},</p>" +
                        $"<p>Your installment for reservation <strong>{System.Net.WebUtility.HtmlEncode(reference)}</strong> " +
                        $"in {System.Net.WebUtility.HtmlEncode(sectionName)} is due on <strong>{dueDate}</strong>.</p>" +
                        $"<p>Outstanding balance: <strong>${balance:N2}</strong>.</p>");
                    sent = true;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Email reminder failed for reservation {ReservationReference}.", reference);
                }
            }

            if (sent)
            {
                reservation.sys_lastInstallmentReminderSentOn = today;
                await context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
