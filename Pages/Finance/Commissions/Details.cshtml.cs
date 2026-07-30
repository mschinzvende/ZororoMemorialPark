using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ZororoMemorialPark.Data;
using ZororoMemorialPark.Data.Models;

namespace ZororoMemorialPark.Pages.Finance.Commissions;

public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(
        ApplicationDbContext context,
        ILogger<DetailsModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty(SupportsGet = true)]
    public int CommissionPage { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int PaymentPage { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public string? CommissionSearch { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? PaymentSearch { get; set; }

    [BindProperty]
    public PaymentInput Payment { get; set; } = new();

    private const int CommissionPageSize = 10;

    private const int PaymentPageSize = 10;

    public int CommissionTotalPages { get; set; }

    public int PaymentTotalPages { get; set; }

    public tbl_Agents Agent { get; set; } = new();

    public double TotalEarned { get; set; }

    public double TotalPaid { get; set; }

    public double Outstanding { get; set; }

    public int BookingCount { get; set; }



    public List<OutstandingCommissionRow> OutstandingCommissions { get; set; } = new();

    public List<PaymentHistoryRow> Payments { get; set; } = new();

    public class PaymentInput
    {
        public DateTime PaymentDate { get; set; } = DateTime.Today;

        public double Amount { get; set; }

        public string? Reference { get; set; }

        public string? Notes { get; set; }
    }

    public class OutstandingCommissionRow
    {
        public int CommissionId { get; set; }

        public int BookingId { get; set; }

        public string DeceasedName { get; set; } = "";

        public string SectionName { get; set; } = "";

        public double Commission { get; set; }

        public double Paid { get; set; }

        public double Balance { get; set; }

        public bool FullyPaid => Balance <= 0;
    }

    public class PaymentHistoryRow
    {
        public int PaymentId { get; set; }

        public DateTime PaymentDate { get; set; }

        public double Amount { get; set; }

        public string Reference { get; set; } = "";

        public string Notes { get; set; } = "";

        public string RecordedBy { get; set; } = "";

        public int AllocatedCommissions { get; set; }

        public double AllocatedAmount { get; set; }
    }

    public class PaymentAllocationRow
    {
        public int CommissionId { get; set; }

        public int BookingId { get; set; }

        public string DeceasedName { get; set; } = "";

        public string SectionName { get; set; } = "";

        public double AmountAllocated { get; set; }
    }

    public async Task<IActionResult> OnGetPaymentAllocationsAsync(int paymentId)
    {
        var data = await
        (
            from allocation in _context.tbl_CommissionPaymentAllocations.AsNoTracking()

            join commission in _context.tbl_CommissionsDue
                on allocation.sys_commissionId equals commission.sys_commissionId

            join booking in _context.tbl_BurialBookings
                on commission.sys_bookingId equals booking.sys_bookingId

            join grave in _context.tbl_Graves
                on commission.sys_graveId equals grave.sys_graveId

            join section in _context.tbl_Sections
                on grave.sys_sectionId equals section.sys_sectionId

            where allocation.sys_commissionPaymentId == paymentId

            select new
            {
                Booking = booking.sys_bookingId,

                Deceased =
                    booking.sys_deceasedFirstName + " " +
                    booking.sys_deceasedLastName,

                Section =
                    section.sys_sectionName,

                allocation.sys_amountAllocated
            }

        ).ToListAsync();

        return new JsonResult(data);
    }

    public async Task<IActionResult> OnGetAsync()
    {
        //--------------------------------------------------
        // Load Agent
        //--------------------------------------------------

        Agent = await _context.tbl_Agents
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.sys_agentId == Id);

        if (Agent == null)
            return NotFound();

        //--------------------------------------------------
        // Allocation Totals
        //--------------------------------------------------

        var allocationTotals =
            from a in _context.tbl_CommissionPaymentAllocations.AsNoTracking()
            group a by a.sys_commissionId into g
            select new
            {
                CommissionId = g.Key,
                Paid = g.Sum(x => (double?)x.sys_amountAllocated) ?? 0
            };

        //--------------------------------------------------
        // SUMMARY
        //--------------------------------------------------

        BookingCount = await _context.tbl_CommissionsDue
            .AsNoTracking()
            .Where(x => x.sys_agentId == Id)
            .CountAsync();

        TotalEarned = await _context.tbl_CommissionsDue
            .AsNoTracking()
            .Where(x => x.sys_agentId == Id)
            .SumAsync(x => (double?)x.sys_amount) ?? 0;

        TotalPaid = await
        (
            from allocation in _context.tbl_CommissionPaymentAllocations.AsNoTracking()

            join commission in _context.tbl_CommissionsDue.AsNoTracking()
                on allocation.sys_commissionId equals commission.sys_commissionId

            where commission.sys_agentId == Id

            select (double?)allocation.sys_amountAllocated

        ).SumAsync() ?? 0;

        Outstanding = TotalEarned - TotalPaid;



        //--------------------------------------------------
        // COMMISSION QUERY
        //--------------------------------------------------
        /*
        var commissionQuery =
            from c in _context.tbl_CommissionsDue.AsNoTracking()

            join booking in _context.tbl_BurialBookings.AsNoTracking()
                on c.sys_bookingId equals booking.sys_bookingId

            join sec in _context.tbl_Sections.AsNoTracking()
                on booking.sys_sectionId equals sec.sys_sectionId
                into sectionJoin

            from section in sectionJoin.DefaultIfEmpty()

            join alloc in allocationTotals
                on c.sys_commissionId equals alloc.CommissionId
                into allocJoin

            from paid in allocJoin.DefaultIfEmpty()

            where c.sys_agentId == Id

            select new OutstandingCommissionRow
            {
                CommissionId = c.sys_commissionId,

                BookingId = booking.sys_bookingId,

                DeceasedName =
                    (booking.sys_deceasedFirstName ?? "") + " " +
                    (booking.sys_deceasedLastName ?? ""),

                SectionName =
                    section == null
                        ? "Unknown Section"
                        : section.sys_sectionName,

                Commission = c.sys_amount,

                Paid = paid == null
                    ? 0
                    : paid.Paid,

                Balance =
                    c.sys_amount -
                    (paid == null
                        ? 0
                        : paid.Paid)
            };
        */

        var commissionQuery =
    from c in _context.tbl_CommissionsDue.AsNoTracking()

    join booking in _context.tbl_BurialBookings.AsNoTracking()
        on c.sys_bookingId equals booking.sys_bookingId

    join sec in _context.tbl_Sections.AsNoTracking()
on booking.sys_sectionId equals sec.sys_sectionId
into sectionJoin

    from section in sectionJoin.DefaultIfEmpty()

    join alloc in allocationTotals
    on c.sys_commissionId equals alloc.CommissionId
    into allocJoin

    from paid in allocJoin.DefaultIfEmpty()

    where c.sys_agentId == Id



    select new OutstandingCommissionRow
    {
        CommissionId = c.sys_commissionId,
        BookingId = booking.sys_bookingId,
        DeceasedName = booking.sys_deceasedFirstName + " " + booking.sys_deceasedLastName,
        SectionName = section == null ? "" : section.sys_sectionName,
        Commission = c.sys_amount,
        Paid = _context.tbl_CommissionPaymentAllocations
    .Where(a => a.sys_commissionId == c.sys_commissionId)
    .Sum(a => (double?)a.sys_amountAllocated) ?? 0,
        Balance =
    c.sys_amount -
    (
        _context.tbl_CommissionPaymentAllocations
            .Where(a => a.sys_commissionId == c.sys_commissionId)
            .Sum(a => (double?)a.sys_amountAllocated) ?? 0
    )


    };
        //--------------------------------------------------
        // SEARCH
        //--------------------------------------------------

        if (!string.IsNullOrWhiteSpace(CommissionSearch))
        {
            var search = CommissionSearch.Trim();

            commissionQuery =
                commissionQuery.Where(x =>

                    x.DeceasedName.Contains(search) ||

                    x.SectionName.Contains(search) ||

                    x.BookingId.ToString().Contains(search));
        }

        //--------------------------------------------------
        // PAGINATION
        //--------------------------------------------------

        var totalCommissionRecords =
            await commissionQuery.CountAsync();

        CommissionTotalPages =
            (int)Math.Ceiling(
                totalCommissionRecords /
                (double)CommissionPageSize);

        OutstandingCommissions =
            await commissionQuery

            .OrderByDescending(x => x.BookingId)

            .Skip(
                (CommissionPage - 1)
                * CommissionPageSize)

            .Take(
                CommissionPageSize)

            .ToListAsync();

        //--------------------------------------------------
        // PAYMENT QUERY
        //--------------------------------------------------

        var paymentQuery =
        (
            from payment in _context.tbl_CommissionPayments.AsNoTracking()

            join allocation in
            (
                from a in _context.tbl_CommissionPaymentAllocations.AsNoTracking()

                group a by a.sys_commissionPaymentId into g

                select new
                {
                    PaymentId = g.Key,

                    Allocated =
                        g.Sum(x => x.sys_amountAllocated),

                    Count =
                        g.Count()
                }

            )

            on payment.sys_commissionPaymentId equals allocation.PaymentId
            into allocationJoin

            from alloc in allocationJoin.DefaultIfEmpty()

            join user in _context.Users
                on payment.sys_recordedByUserId equals user.Id
                into users

            from usr in users.DefaultIfEmpty()

            where payment.sys_agentId == Id

            select new PaymentHistoryRow
            {
                PaymentId =
                    payment.sys_commissionPaymentId,

                PaymentDate =
                    payment.sys_paymentDate,

                Amount =
                    payment.sys_amountPaid,

                Reference =
                    payment.sys_reference ?? "",

                Notes =
                    payment.sys_notes ?? "",

                RecordedBy =
                    usr == null
                        ? ""
                        : usr.Email,

                AllocatedAmount =
                    alloc == null
                        ? 0
                        : alloc.Allocated,

                AllocatedCommissions =
                    alloc == null
                        ? 0
                        : alloc.Count
            }

        );

        //--------------------------------------------------
        // PAYMENT SEARCH
        //--------------------------------------------------

        if (!string.IsNullOrWhiteSpace(PaymentSearch))
        {
            PaymentSearch = PaymentSearch.Trim();

            paymentQuery =
                paymentQuery.Where(x =>

                    x.Reference.Contains(PaymentSearch) ||

                    x.RecordedBy.Contains(PaymentSearch));
        }

        //--------------------------------------------------
        // PAYMENT PAGINATION
        //--------------------------------------------------

        var totalPayments =
            await paymentQuery.CountAsync();

        PaymentTotalPages =
            (int)Math.Ceiling(
                totalPayments /
                (double)PaymentPageSize);

        Payments =
            await paymentQuery

            .OrderByDescending(x => x.PaymentDate)

            .Skip(
                (PaymentPage - 1)
                * PaymentPageSize)

            .Take(
                PaymentPageSize)

            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostRecordPaymentAsync()
    {
        try
        {
            Agent = await _context.tbl_Agents
                .FirstOrDefaultAsync(x => x.sys_agentId == Id);

            if (Agent == null)
            {
                TempData["ErrorMessage"] = "Agent not found.";
                return RedirectToPage(new { Id });
            }

            if (Payment.Amount <= 0)
            {
                TempData["ErrorMessage"] = "Invalid payment amount.";
                return RedirectToPage(new { Id });
            }

            //---------------------------------------------------
            // Calculate Outstanding
            //---------------------------------------------------

            var outstanding = await
            (
                from c in _context.tbl_CommissionsDue

                where c.sys_agentId == Id

                select new
                {
                    Commission = c,

                    Paid =
                        _context.tbl_CommissionPaymentAllocations
                        .Where(x => x.sys_commissionId == c.sys_commissionId)
                        .Sum(x => (double?)x.sys_amountAllocated) ?? 0
                }

            ).ToListAsync();

            var totalOutstanding =
                outstanding.Sum(x =>
                    x.Commission.sys_amount - x.Paid);

            if (Payment.Amount > totalOutstanding)
            {
                TempData["ErrorMessage"] =
                    $"Payment exceeds outstanding balance of ${totalOutstanding:N2}.";

                return RedirectToPage(new { Id });
            }

            //---------------------------------------------------
            // Transaction
            //---------------------------------------------------

            using var transaction =
                await _context.Database.BeginTransactionAsync();

            //---------------------------------------------------
            // Create Payment Header
            //---------------------------------------------------

            var payment = new tbl_CommissionPayments
            {
                sys_agentId = Id,

                sys_paymentDate = Payment.PaymentDate,

                sys_amountPaid = Payment.Amount,

                sys_reference = Payment.Reference,

                sys_notes = Payment.Notes,

                sys_recordedByUserId =
                    User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,

                sys_createdOn = DateTime.Now
            };

            _context.tbl_CommissionPayments.Add(payment);

            await _context.SaveChangesAsync();

            //---------------------------------------------------
            // FIFO Allocation
            //---------------------------------------------------

            double remaining = Payment.Amount;

            var commissions = outstanding
                .OrderBy(x => x.Commission.sys_commissionId)
                .ToList();

            foreach (var item in commissions)
            {
                if (remaining <= 0)
                    break;

                var balance =
                    item.Commission.sys_amount -
                    item.Paid;

                if (balance <= 0)
                    continue;

                var allocation =
                    Math.Min(balance, remaining);

                _context.tbl_CommissionPaymentAllocations.Add(
                    new tbl_CommissionPaymentAllocations
                    {
                        sys_commissionPaymentId =
                            payment.sys_commissionPaymentId,

                        sys_commissionId =
                            item.Commission.sys_commissionId,

                        sys_amountAllocated =
                            allocation
                    });

                remaining -= allocation;

                //------------------------------------------------
                // Mark Commission Paid
                //------------------------------------------------

                if (allocation == balance)
                {
                    var remainingBalance = balance - allocation;

                    //item.Commission.sys_isPaid = remainingBalance <= 0;
                }
            }

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            TempData["SuccessMessage"] =
                "Commission payment captured successfully.";

            return RedirectToPage(new { Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Commission Payment Error");

            TempData["ErrorMessage"] =
                ex.Message;

            return RedirectToPage(new { Id });
        }
    }

}