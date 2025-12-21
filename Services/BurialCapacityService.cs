using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using ZororoMemorialPark.Data;

public interface IBurialCapacityService
{
    Task<(bool ok, string message)> ValidateAsync(DateTime burialDateTime, int? ignoreBookingId = null);
}

public class BurialCapacityService : IBurialCapacityService
{
    private readonly ApplicationDbContext _db;

    public BurialCapacityService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<(bool ok, string message)> ValidateAsync(DateTime burialDateTime, int? ignoreBookingId = null)
    {
        var settings = await _db.SystemSettings.AsNoTracking().FirstOrDefaultAsync();
        if (settings == null)
            return (false, "System settings not configured.");

        var dayStart = burialDateTime.Date;
        var dayEnd = dayStart.AddDays(1);

        // Count active bookings for the day
        var bookingsQuery = _db.tbl_BurialBookings.AsNoTracking()
            .Where(b => b.sys_requestedBurialDateTime >= dayStart &&
                        b.sys_requestedBurialDateTime < dayEnd &&
                        b.sys_bookingStatus != "Cancelled" &&
                        b.sys_bookingStatus != "Rejected" &&
                        b.sys_bookingStatus != "Expired");

        if (ignoreBookingId.HasValue)
            bookingsQuery = bookingsQuery.Where(b => b.sys_bookingId != ignoreBookingId.Value);

        var bookingCount = await bookingsQuery.CountAsync();

        // Count allocations for the day (these are real burials)
        var allocationCount = await _db.tbl_GraveAllocations.AsNoTracking()
            .Where(a => a.sys_dateTimeOfBurial >= dayStart && a.sys_dateTimeOfBurial < dayEnd)
            .CountAsync();

        var totalScheduled = bookingCount + allocationCount;

        if (totalScheduled >= settings.MaxBurialsPerDay)
            return (false, $"Capacity reached for {dayStart:yyyy-MM-dd}. Max burials per day is {settings.MaxBurialsPerDay}.");

        // Minutes between burials rule (applies to both bookings and allocations)
        var window = TimeSpan.FromMinutes(settings.MinutesBetweenBurials);
        var startWindow = burialDateTime.Subtract(window);
        var endWindow = burialDateTime.Add(window);

        var hasBookingConflict = await _db.tbl_BurialBookings.AsNoTracking()
            .Where(b => b.sys_requestedBurialDateTime >= startWindow &&
                        b.sys_requestedBurialDateTime <= endWindow &&
                        b.sys_bookingStatus != "Cancelled" &&
                        b.sys_bookingStatus != "Rejected" &&
                        b.sys_bookingStatus != "Expired")
            .AnyAsync(b => !ignoreBookingId.HasValue || b.sys_bookingId != ignoreBookingId.Value);

        if (hasBookingConflict)
            return (false, $"Time slot conflict. Must allow at least {settings.MinutesBetweenBurials} minutes between burials.");

        var hasAllocationConflict = await _db.tbl_GraveAllocations.AsNoTracking()
            .Where(a => a.sys_dateTimeOfBurial >= startWindow && a.sys_dateTimeOfBurial <= endWindow)
            .AnyAsync();

        if (hasAllocationConflict)
            return (false, $"Time slot conflict with an existing burial. Must allow at least {settings.MinutesBetweenBurials} minutes between burials.");

        return (true, "OK");
    }
}
