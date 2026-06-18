using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZororoMemorialPark.Data;

namespace ZororoMemorialPark.Pages.Bookings
{
    public class IndexModel : PageModel
    {

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int TotalPages { get; set; }

        public const int PageSize = 20;

        private readonly ApplicationDbContext _db;

        public IndexModel(ApplicationDbContext db)
        {
            _db = db;
        }

        [BindProperty(SupportsGet = true)]
        public string? q { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? status { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? date { get; set; }

        public List<BookingRow> Rows { get; set; } = new();

        public class BookingRow
        {
            public tbl_BurialBookings Booking { get; set; }
            public string SectionName { get; set; }
            public double SectionPrice { get; set; }
            public string AgentName { get; set; } = "-";
        }

        public async Task OnGetAsync()
        {
            var query = _db.tbl_BurialBookings.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                query = query.Where(b =>
                    b.sys_deceasedFirstName.ToLower().Contains(term) ||
                    b.sys_deceasedLastName.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {


                if (status == "Temporary")
                {

                    query = query.Where(b => b.sys_isTemporary == true);
                }

                else
                {
                    query = query.Where(b => b.sys_bookingStatus == status);

                }
            }
            if (date.HasValue)
            {
                var start = date.Value.Date;
                var end = start.AddDays(1);
                query = query.Where(b => b.sys_requestedBurialDateTime >= start && b.sys_requestedBurialDateTime < end);
            }

            var totalRecords = await query.CountAsync();

            TotalPages = (int)Math.Ceiling(
                totalRecords / (double)PageSize);

            var bookings = await query
                .OrderByDescending(b => b.sys_createdOn)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            var sectionIds = bookings.Select(b => b.sys_sectionId).Distinct().ToList();
            var sections = await _db.tbl_Sections.AsNoTracking()
                .Where(s => sectionIds.Contains(s.sys_sectionId))
                .ToDictionaryAsync(s => s.sys_sectionId);

            var agentIds = bookings
    .Where(b => b.sys_agentId.HasValue)
    .Select(b => b.sys_agentId.Value)
    .Distinct()
    .ToList();

            var agents = await _db.tbl_Agents
                .AsNoTracking()
                .Where(a => agentIds.Contains(a.sys_agentId))
                .ToDictionaryAsync(a => a.sys_agentId);


            Rows = bookings.Select(b =>
            {
                sections.TryGetValue(b.sys_sectionId, out var sec);

                string agentName = "-";

                if (b.sys_agentId.HasValue &&
                    agents.TryGetValue(b.sys_agentId.Value, out var agent))
                {
                    agentName = agent.sys_agentName;
                }

                return new BookingRow
                {
                    Booking = b,
                    SectionName = sec?.sys_sectionName ?? "-",
                    SectionPrice = sec?.sys_SecPrice ?? 0,
                    AgentName = agentName
                };
            }).ToList();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var booking = await _db.tbl_BurialBookings
                .FirstOrDefaultAsync(x =>
                    x.sys_bookingId == id);

            if (booking == null)
            {
                TempData["ErrorMessage"] =
                    "Booking not found.";

                return RedirectToPage();
            }

            // Prevent deletion if already invoiced
            if (booking.sys_invoiceId.HasValue)
            {
                TempData["ErrorMessage"] =
                    $"Booking for {booking.sys_deceasedFirstName} {booking.sys_deceasedLastName} has already been invoiced and cannot be deleted.";

                return RedirectToPage();
            }

            var isAllocated = await _db.tbl_GraveAllocations
    .AnyAsync(x => x.sys_bookingId == id);

            if (isAllocated)
            {
                TempData["ErrorMessage"] =
                    $"Booking for {booking.sys_deceasedFirstName} {booking.sys_deceasedLastName} has already been allocated and cannot be deleted.";

                return RedirectToPage();
            }

            _db.tbl_BurialBookings.Remove(booking);

            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Booking deleted successfully.";

            return RedirectToPage();
        }
    }
}
