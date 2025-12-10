using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZororoMemorialPark.Data;
using ZororoMemorialPark.Data.Models;

namespace ZororoMemorialPark.Pages.Finance
{
    public class RequisitionsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public RequisitionsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public class ReqRow
        {
            public int Id { get; set; }
            public DateTime Date { get; set; }
            public string Title { get; set; }
            public string RequestedByUserId { get; set; }

            public double TotalRequested { get; set; }
            public double TotalApproved { get; set; }

            public string Status { get; set; }
        }

        public List<ReqRow> Requisitions { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string StatusFilter { get; set; } = "All";

        [BindProperty(SupportsGet = true)]
        public string OwnershipFilter { get; set; } = "All"; // All, Mine

        public List<SelectListItem> StatusOptions { get; set; }
        public List<SelectListItem> OwnershipOptions { get; set; }

        public async Task OnGetAsync()
        {
            StatusOptions = new List<SelectListItem>
            {
                new SelectListItem("All Statuses", "All"),
                new SelectListItem("Pending", "Pending"),
                new SelectListItem("Pending Level 2", "PendingLevel2"),
                new SelectListItem("Partially Approved", "PartiallyApproved"),
                new SelectListItem("Approved", "Approved"),
                new SelectListItem("Rejected", "Rejected"),
                new SelectListItem("Paid", "Paid")
            };

            OwnershipOptions = new List<SelectListItem>
            {
                new SelectListItem("All Requisitions", "All"),
                new SelectListItem("My Requisitions", "Mine")
            };

            var query = _context.tbl_Requisitions.AsQueryable();

            if (StatusFilter != "All")
            {
                query = query.Where(r => r.sys_status == StatusFilter);
            }

            if (OwnershipFilter == "Mine")
            {
                var currentUserId = User?.Identity?.Name;
                if (!string.IsNullOrEmpty(currentUserId))
                {
                    query = query.Where(r => r.sys_requestedByUserId == currentUserId);
                }
                else
                {
                    query = query.Where(r => false);
                }
            }

            var headers = await query
                .OrderByDescending(r => r.sys_requestDate)
                .ToListAsync();

            var ids = headers.Select(h => h.sys_requisitionId).ToList();

            var items = await _context.tbl_RequisitionItems
                .Where(i => ids.Contains(i.sys_requisitionId))
                .ToListAsync();

            Requisitions = headers.Select(h =>
            {
                var theseItems = items.Where(i => i.sys_requisitionId == h.sys_requisitionId).ToList();
                var totalReq = theseItems.Sum(i => i.TotalCost);
                var totalApproved = theseItems
                    .Where(i => i.Status == "Approved" && i.ApprovedAmount.HasValue)
                    .Sum(i => i.ApprovedAmount.Value);

                return new ReqRow
                {
                    Id = h.sys_requisitionId,
                    Date = h.sys_requestDate,
                    Title = h.sys_title,
                    RequestedByUserId = h.sys_requestedByUserId,
                    TotalRequested = totalReq,
                    TotalApproved = totalApproved,
                    Status = h.sys_status
                };
            }).ToList();
        }
    }
}
