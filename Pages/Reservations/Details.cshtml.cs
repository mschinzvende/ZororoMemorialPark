using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ZororoMemorialPark.Data;
using ZororoMemorialPark.Data.Models;

namespace ZororoMemorialPark.Pages.Reservations;

[Authorize(Roles = "Manager,OperationsOfficer,FinanceOfficer")]
public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DetailsModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public tbl_GraveReservations Reservation { get; private set; } = new();
    public tbl_Customers Customer { get; private set; } = new();
    public tbl_Sections? PreferredSection { get; private set; }
    public tbl_Graves? LinkedGrave { get; private set; }
    public tbl_Sections? LinkedGraveSection { get; private set; }
    public List<tbl_Beneficiaries> Beneficiaries { get; private set; } = new();
    public List<InvoiceRow> Invoices { get; private set; } = new();
    public List<tbl_IncomePayments> Payments { get; private set; } = new();
    public List<tbl_Graves> AvailableGraves { get; private set; } = new();
    public double Total { get; private set; }
    public double Paid { get; private set; }
    public double Balance => Math.Max(0, Total - Paid);
    public string PaymentStatus => IndexModel.GetDisplayStatus(Reservation.sys_status, Total, Paid);
    public bool CanManageGraveLink => User.IsInRole("Manager") || User.IsInRole("OperationsOfficer");

    public async Task<IActionResult> OnGetAsync(int id)
    {
        return await LoadAsync(id) ? Page() : NotFound();
    }

    public async Task<IActionResult> OnPostLinkGraveAsync(int reservationId, int graveId)
    {
        if (!CanManageGraveLink)
        {
            return Forbid();
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var reservation = await _context.tbl_GraveReservations
            .FirstOrDefaultAsync(x => x.sys_reservationId == reservationId);

        if (reservation == null)
        {
            return NotFound();
        }

        if (reservation.sys_graveId.HasValue)
        {
            TempData["ErrorMessage"] = "A grave is already linked to this reservation.";
            return RedirectToPage(new { id = reservationId });
        }

        if (!reservation.sys_preferredSectionId.HasValue)
        {
            TempData["ErrorMessage"] = "This legacy reservation has no recorded section. Update its details before selecting a grave.";
            return RedirectToPage(new { id = reservationId });
        }

        var grave = await _context.tbl_Graves
            .FirstOrDefaultAsync(x => x.sys_graveId == graveId);

        if (grave == null || grave.sys_graveSaleStatus != "Unsold")
        {
            TempData["ErrorMessage"] = "The selected grave is no longer available.";
            return RedirectToPage(new { id = reservationId });
        }

        if (grave.sys_sectionId != reservation.sys_preferredSectionId.Value)
        {
            TempData["ErrorMessage"] = "A reservation can only be linked to a grave in its selected section.";
            return RedirectToPage(new { id = reservationId });
        }

        var alreadyLinked = await _context.tbl_GraveReservations
            .AnyAsync(x => x.sys_graveId == graveId && x.sys_reservationId != reservationId);

        if (alreadyLinked)
        {
            TempData["ErrorMessage"] = "That grave is already linked to another reservation.";
            return RedirectToPage(new { id = reservationId });
        }

        reservation.sys_graveId = graveId;
        grave.sys_graveSaleStatus = "Reserved";
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["SuccessMessage"] = $"Grave {grave.sys_plotNumber} is now linked to this reservation.";
        return RedirectToPage(new { id = reservationId });
    }

    private async Task<bool> LoadAsync(int id)
    {
        Reservation = await _context.tbl_GraveReservations.AsNoTracking()
            .FirstOrDefaultAsync(x => x.sys_reservationId == id) ?? new tbl_GraveReservations();
        if (Reservation.sys_reservationId == 0)
        {
            return false;
        }

        Customer = await _context.tbl_Customers.AsNoTracking()
            .FirstOrDefaultAsync(x => x.sys_customerId == Reservation.sys_customerId) ?? new tbl_Customers();
        if (Reservation.sys_preferredSectionId.HasValue)
        {
            PreferredSection = await _context.tbl_Sections.AsNoTracking()
                .FirstOrDefaultAsync(x => x.sys_sectionId == Reservation.sys_preferredSectionId.Value);
        }
        if (Reservation.sys_graveId.HasValue)
        {
            LinkedGrave = await _context.tbl_Graves.AsNoTracking()
                .FirstOrDefaultAsync(x => x.sys_graveId == Reservation.sys_graveId.Value);
            if (LinkedGrave != null)
            {
                LinkedGraveSection = await _context.tbl_Sections.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.sys_sectionId == LinkedGrave.sys_sectionId);
            }
        }

        Beneficiaries = await _context.tbl_Beneficiaries.AsNoTracking()
            .Where(x => x.sys_reservationId == id)
            .OrderBy(x => x.sys_benefitFullName)
            .ToListAsync();

        var links = await _context.tbl_GraveReservationInvoices.AsNoTracking()
            .Where(x => x.sys_reservationId == id)
            .ToListAsync();
        var invoiceIds = links.Select(x => x.sys_invoiceId).Distinct().ToList();
        var invoices = await _context.tbl_Invoices.AsNoTracking()
            .Where(x => invoiceIds.Contains(x.sys_invoiceId))
            .OrderByDescending(x => x.sys_invoiceDate)
            .ToListAsync();
        var items = await _context.tbl_InvoiceItems.AsNoTracking()
            .Where(x => invoiceIds.Contains(x.sys_invoiceId))
            .ToListAsync();
        Payments = await _context.tbl_IncomePayments.AsNoTracking()
            .Where(x => invoiceIds.Contains(x.sys_invoiceId))
            .OrderByDescending(x => x.sys_date)
            .ToListAsync();

        Invoices = invoices.Select(invoice =>
        {
            var total = items.Where(x => x.sys_invoiceId == invoice.sys_invoiceId).Sum(x => x.sys_itemCost);
            var paid = Payments.Where(x => x.sys_invoiceId == invoice.sys_invoiceId).Sum(x => x.sys_amount);
            return new InvoiceRow
            {
                InvoiceId = invoice.sys_invoiceId,
                InvoiceDate = invoice.sys_invoiceDate,
                DueDate = invoice.sys_invoiceDueDate,
                Total = total,
                Paid = paid,
                Balance = Math.Max(0, total - paid)
            };
        }).ToList();
        Total = Invoices.Sum(x => x.Total);
        Paid = Invoices.Sum(x => x.Paid);

        if (!Reservation.sys_graveId.HasValue && Reservation.sys_preferredSectionId.HasValue)
        {
            AvailableGraves = await _context.tbl_Graves.AsNoTracking()
                .Where(x => x.sys_sectionId == Reservation.sys_preferredSectionId.Value && x.sys_graveSaleStatus == "Unsold")
                .OrderBy(x => x.sys_plotNumber)
                .ToListAsync();
        }

        return true;
    }

    public class InvoiceRow
    {
        public int InvoiceId { get; set; }
        public DateOnly InvoiceDate { get; set; }
        public DateOnly DueDate { get; set; }
        public double Total { get; set; }
        public double Paid { get; set; }
        public double Balance { get; set; }
    }
}
