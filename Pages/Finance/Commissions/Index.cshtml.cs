using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ZororoMemorialPark.Data;

namespace ZororoMemorialPark.Pages.Finance.Commissions;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly AgentCommissionService _service;

    public IndexModel(
        ApplicationDbContext context,
        AgentCommissionService service)
    {
        _context = context;
        _service = service;
    }

    public class Row
    {
        public int AgentId { get; set; }

        public string AgentName { get; set; } = "";

        public int Bookings { get; set; }

        public double Earned { get; set; }

        public double Paid { get; set; }

        public double Outstanding { get; set; }

        public bool FullyPaid => Outstanding <= 0;
    }

    public List<Row> Rows { get; set; } = new();

    public double TotalCommission { get; set; }

    public double TotalPaid { get; set; }

    public double Outstanding { get; set; }

    public int AgentCount { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool OutstandingOnly { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public int TotalPages { get; set; }

    private const int PageSize = 10;

    public async Task OnGetAsync()
    {
        // Base Agent Query
        var agentQuery = _context.tbl_Agents
            .AsNoTracking()
            .Where(a => a.sys_isActive);

        if (!string.IsNullOrWhiteSpace(Search))
        {
            agentQuery = agentQuery.Where(a =>
                a.sys_agentName.Contains(Search));
        }

        // Commission Totals Per Agent
        var commissionSummary = await _context.tbl_CommissionsDue
            .AsNoTracking()
            .GroupBy(c => c.sys_agentId)
            .Select(g => new
            {
                AgentId = g.Key,
                Bookings = g.Count(),
                Earned = g.Sum(x => x.sys_amount)
            })
            .ToListAsync();

        // Allocation Totals Per Agent
        var allocationSummary = await (
            from allocation in _context.tbl_CommissionPaymentAllocations.AsNoTracking()
            join commission in _context.tbl_CommissionsDue.AsNoTracking()
                on allocation.sys_commissionId equals commission.sys_commissionId
            group allocation by commission.sys_agentId into g
            select new
            {
                AgentId = g.Key,
                Paid = g.Sum(x => x.sys_amountAllocated)
            })
            .ToListAsync();

        var commissionLookup = commissionSummary
            .ToDictionary(x => x.AgentId);

        var paymentLookup = allocationSummary
            .ToDictionary(x => x.AgentId);

        var rows = await agentQuery
            .OrderBy(a => a.sys_agentName)
            .Select(agent => new Row
            {
                AgentId = agent.sys_agentId,
                AgentName = agent.sys_agentName
            })
            .ToListAsync();

        foreach (var row in rows)
        {
            if (commissionLookup.TryGetValue(row.AgentId, out var commission))
            {
                row.Bookings = commission.Bookings;
                row.Earned = commission.Earned;
            }

            if (paymentLookup.TryGetValue(row.AgentId, out var payment))
            {
                row.Paid = payment.Paid;
            }

            row.Outstanding = row.Earned - row.Paid;
        }

        if (OutstandingOnly)
        {
            rows = rows
                .Where(x => x.Outstanding > 0)
                .ToList();
        }

        TotalCommission = rows.Sum(x => x.Earned);
        TotalPaid = rows.Sum(x => x.Paid);
        Outstanding = rows.Sum(x => x.Outstanding);
        AgentCount = rows.Count(x => x.Outstanding > 0);

        TotalPages = (int)Math.Ceiling(rows.Count / (double)PageSize);

        Rows = rows
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToList();
    }
}