using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using ZororoMemorialPark.Data;

public class BookingExpiryHostedService : BackgroundService
{
    private readonly IServiceProvider _sp;

    public BookingExpiryHostedService(IServiceProvider sp)
    {
        _sp = sp;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run daily loop
        while (!stoppingToken.IsCancellationRequested)
        {
            // Run at 00:30 local server time (good enough for most deployments)
            var now = DateTime.Now;
            var next = now.Date.AddDays(1).AddMinutes(30);
            var delay = next - now;
            if (delay.TotalMilliseconds > 0)
                await Task.Delay(delay, stoppingToken);

            await ExpireBookings(stoppingToken);
        }
    }

    private async Task ExpireBookings(CancellationToken token)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var now = DateTime.Now;

        var targets = await db.tbl_BurialBookings
            .Where(b => b.sys_requestedBurialDateTime < now &&
                        b.sys_bookingStatus != "Allocated" &&
                        b.sys_bookingStatus != "Cancelled" &&
                        b.sys_bookingStatus != "Rejected" &&
                        b.sys_bookingStatus != "Expired")
            .ToListAsync(token);

        if (targets.Count == 0) return;

        foreach (var b in targets)
            b.sys_bookingStatus = "Expired";

        await db.SaveChangesAsync(token);
    }
}
