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
                query = query.Where(b => b.sys_bookingStatus == status);

            if (date.HasValue)
            {
                var start = date.Value.Date;
                var end = start.AddDays(1);
                query = query.Where(b => b.sys_requestedBurialDateTime >= start && b.sys_requestedBurialDateTime < end);
            }

            var bookings = await query
                .OrderByDescending(b => b.sys_createdOn)
                .Take(500)
                .ToListAsync();

            var sectionIds = bookings.Select(b => b.sys_sectionId).Distinct().ToList();
            var sections = await _db.tbl_Sections.AsNoTracking()
                .Where(s => sectionIds.Contains(s.sys_sectionId))
                .ToDictionaryAsync(s => s.sys_sectionId);

            Rows = bookings.Select(b =>
            {
                sections.TryGetValue(b.sys_sectionId, out var sec);
                return new BookingRow
                {
                    Booking = b,
                    SectionName = sec?.sys_sectionName ?? "-",
                    SectionPrice = sec?.sys_SecPrice ?? 0
                };
            }).ToList();
        }
    }
}
