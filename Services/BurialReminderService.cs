using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZororoMemorialPark.Data;
using ZororoMemorialPark.Services;
using ZororoMemorialPark.Data.Models;

public class BurialReminderService : BackgroundService
{
    private readonly IServiceProvider _provider;
    private readonly ILogger<BurialReminderService> _logger;

    public BurialReminderService(IServiceProvider provider, ILogger<BurialReminderService> logger)
    {
        _provider = provider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Burial Reminder Service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SendRemindersAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in BurialReminderService.");
            }

            await Task.Delay(GetDelayUntilNextRun(), stoppingToken);
        }
    }

    private TimeSpan GetDelayUntilNextRun()
    {
        var now = DateTime.Now;
        var next = DateTime.Today.AddHours(6); // 06:00 daily
        if (now > next)
            next = next.AddDays(1);
        return next - now;
    }

    private async Task SendRemindersAsync()
    {
        using var scope = _provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notify = scope.ServiceProvider.GetRequiredService<NotificationService>();
        var appLogger = scope.ServiceProvider.GetRequiredService<IAppLogger>();

        DateTime tomorrow = DateTime.Today.AddDays(1);

        var burials = await context.tbl_GraveAllocations
            .Where(a => a.sys_dateTimeOfBurial.Date == tomorrow.Date)
            .Join(context.tbl_Deceased,
                a => a.sys_deceasedId,
                d => d.sys_deceasedId,
                (a, d) => new
                {
                    Allocation = a,
                    Deceased = d
                })
            .ToListAsync();

        if (burials.Count == 0)
        {
            _logger.LogInformation("No burials scheduled for tomorrow.");
            return;
        }

        foreach (var b in burials)
        {
            string nokName = b.Deceased.sys_deceasedNextOfKinName;
            string phone = b.Deceased.sys_deceasedNextOfKinPhone;
            string deceasedName = b.Deceased.sys_deceasedFullName;

            string formatted = b.Allocation.sys_dateTimeOfBurial.ToString("dddd, dd MMM yyyy HH:mm");
            string message = $"Reminder: The burial for {deceasedName} is scheduled for {formatted}.";

            if (!string.IsNullOrWhiteSpace(phone))
            {
               // await notify.SendSmsAsync(phone, message);
               //We will send whats app here
            }

            await appLogger.LogActivityAsync(
                "SYSTEM",
                "Burial Reminder",
                $"Reminder sent for {deceasedName} at {formatted} to NOK {nokName} ({phone})");
        }

        _logger.LogInformation("Burial Reminder Service sent {count} reminders.", burials.Count);
    }
}
