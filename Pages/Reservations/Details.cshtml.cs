using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using ZororoMemorialPark.Data;
using ZororoMemorialPark.Data.Models;
using ZororoMemorialPark.Services;
using ZororoMemorialPark.Services.WhatsApp;

namespace ZororoMemorialPark.Pages.Reservations;

[Authorize(Roles = "Manager,OperationsOfficer,FinanceOfficer")]
public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly NotificationService _notificationService;
    private readonly WhatsAppNotificationService _whatsApp;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(
        ApplicationDbContext context,
        NotificationService notificationService,
        WhatsAppNotificationService whatsApp,
        ILogger<DetailsModel> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _whatsApp = whatsApp;
        _logger = logger;
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
    public List<tbl_Sections> Sections { get; private set; } = new();
    public List<tbl_PaymentMethods> PaymentMethods { get; private set; } = new();
    [BindProperty] public EditReservationInput EditInput { get; set; } = new();
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

    public async Task<IActionResult> OnPostUnlinkGraveAsync(int reservationId)
    {
        if (!CanManageGraveLink)
        {
            return Forbid();
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        var reservation = await _context.tbl_GraveReservations
            .FirstOrDefaultAsync(x => x.sys_reservationId == reservationId);

        if (reservation?.sys_graveId is not int graveId)
        {
            TempData["ErrorMessage"] = "There is no grave linked to this reservation.";
            return RedirectToPage(new { id = reservationId });
        }

        var grave = await _context.tbl_Graves.FirstOrDefaultAsync(x => x.sys_graveId == graveId);
        if (grave == null)
        {
            TempData["ErrorMessage"] = "The linked grave could not be found.";
            return RedirectToPage(new { id = reservationId });
        }

        var hasAllocation = await _context.tbl_GraveAllocations
            .AnyAsync(x => x.sys_graveId == graveId);
        if (hasAllocation)
        {
            TempData["ErrorMessage"] = "This grave cannot be unlinked because it has already been allocated.";
            return RedirectToPage(new { id = reservationId });
        }

        var linkedElsewhere = await _context.tbl_GraveReservations
            .AnyAsync(x => x.sys_graveId == graveId && x.sys_reservationId != reservationId);
        if (linkedElsewhere)
        {
            TempData["ErrorMessage"] = "This grave is linked to another reservation and cannot be released.";
            return RedirectToPage(new { id = reservationId });
        }

        reservation.sys_graveId = null;
        grave.sys_graveSaleStatus = "Unsold";
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["SuccessMessage"] = $"Grave {grave.sys_plotNumber} was unlinked and is available for sale again.";
        return RedirectToPage(new { id = reservationId });
    }

    public async Task<IActionResult> OnPostCapturePaymentAsync(
        int reservationId,
        int invoiceId,
        double amount,
        int paymentMethodId,
        string? notes,
        DateOnly? paymentDate)
    {
        if (amount <= 0)
        {
            TempData["ErrorMessage"] = "Enter a payment amount greater than zero.";
            return RedirectToPage(new { id = reservationId });
        }

        var reservation = await _context.tbl_GraveReservations
            .FirstOrDefaultAsync(x => x.sys_reservationId == reservationId);
        var paymentMethod = await _context.tbl_PaymentMethods.FindAsync(paymentMethodId);
        var isReservationInvoice = await _context.tbl_GraveReservationInvoices
            .AnyAsync(x => x.sys_reservationId == reservationId && x.sys_invoiceId == invoiceId);

        if (reservation == null || paymentMethod == null || !isReservationInvoice)
        {
            TempData["ErrorMessage"] = "The reservation, invoice, or payment method is invalid.";
            return RedirectToPage(new { id = reservationId });
        }

        var total = await _context.tbl_InvoiceItems
            .Where(x => x.sys_invoiceId == invoiceId)
            .SumAsync(x => (double?)x.sys_itemCost) ?? 0;
        var paid = await _context.tbl_IncomePayments
            .Where(x => x.sys_invoiceId == invoiceId)
            .SumAsync(x => (double?)x.sys_amount) ?? 0;
        var balance = Math.Max(0, total - paid);

        if (amount > balance)
        {
            TempData["ErrorMessage"] = $"Payment cannot exceed the outstanding invoice balance of ${balance:N2}.";
            return RedirectToPage(new { id = reservationId });
        }

        _context.tbl_IncomePayments.Add(new tbl_IncomePayments
        {
            sys_invoiceId = invoiceId,
            sys_amount = amount,
            sys_paymentMethod = paymentMethod.sys_paymentMethodName,
            sys_notes = string.IsNullOrWhiteSpace(notes) ? "n/a" : notes.Trim(),
            sys_date = paymentDate ?? DateOnly.FromDateTime(DateTime.Today),
            sys_receiptNumber = invoiceId
        });
        await _context.SaveChangesAsync();

        var customer = await _context.tbl_Customers
            .FirstOrDefaultAsync(x => x.sys_customerId == reservation.sys_customerId);
        var section = reservation.sys_preferredSectionId.HasValue
            ? await _context.tbl_Sections.FirstOrDefaultAsync(x => x.sys_sectionId == reservation.sys_preferredSectionId.Value)
            : null;
        await SendPaymentNotificationAsync(
            reservation,
            customer,
            section?.sys_sectionName ?? "your selected section",
            amount,
            Math.Max(0, balance - amount),
            paymentDate ?? DateOnly.FromDateTime(DateTime.Today));

        TempData["SuccessMessage"] = "Installment payment captured. A customer notification was requested.";
        return RedirectToPage(new { id = reservationId });
    }

    public async Task<IActionResult> OnPostUpdateAsync()
    {
        if (!ModelState.IsValid || EditInput.BeneficiaryDOB == default || EditInput.NextInstallmentDueDate == default)
        {
            if (EditInput.BeneficiaryDOB == default)
            {
                ModelState.AddModelError("EditInput.BeneficiaryDOB", "Enter the beneficiary date of birth.");
            }
            if (EditInput.NextInstallmentDueDate == default)
            {
                ModelState.AddModelError("EditInput.NextInstallmentDueDate", "Enter the next installment due date.");
            }

            await LoadAsync(EditInput.ReservationId);
            return Page();
        }

        var reservation = await _context.tbl_GraveReservations
            .FirstOrDefaultAsync(x => x.sys_reservationId == EditInput.ReservationId);
        var customer = reservation == null
            ? null
            : await _context.tbl_Customers.FirstOrDefaultAsync(x => x.sys_customerId == reservation.sys_customerId);
        var selectedSection = await _context.tbl_Sections
            .FirstOrDefaultAsync(x => x.sys_sectionId == EditInput.SectionId);

        if (reservation == null || customer == null || selectedSection == null)
        {
            TempData["ErrorMessage"] = "The reservation details could not be found.";
            return RedirectToPage(new { id = EditInput.ReservationId });
        }

        if (reservation.sys_graveId.HasValue && reservation.sys_preferredSectionId != EditInput.SectionId)
        {
            TempData["ErrorMessage"] = "The section cannot be changed after a grave has been linked.";
            return RedirectToPage(new { id = reservation.sys_reservationId });
        }

        if (EditInput.AgreedPrice < 0)
        {
            ModelState.AddModelError("EditInput.AgreedPrice", "The agreed price cannot be negative.");
            await LoadAsync(EditInput.ReservationId);
            return Page();
        }

        var allowedStatuses = new[] { "Active", "Expired", "Cancelled" };
        if (!allowedStatuses.Contains(EditInput.Status, StringComparer.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("EditInput.Status", "Select a valid reservation status.");
            await LoadAsync(EditInput.ReservationId);
            return Page();
        }

        var phone = NormalisePhone(EditInput.CustomerPhone);
        if (phone.Length < 8)
        {
            ModelState.AddModelError("EditInput.CustomerPhone", "Enter a valid contact number, for example 263771234567.");
            await LoadAsync(EditInput.ReservationId);
            return Page();
        }

        customer.sys_paymentMadeBy = EditInput.CustomerName.Trim();
        customer.sys_customerPhone = phone;
        customer.sys_customerEmail = string.IsNullOrWhiteSpace(EditInput.CustomerEmail)
            ? null
            : EditInput.CustomerEmail.Trim();

        reservation.sys_preferredSectionId = EditInput.SectionId;
        reservation.sys_agreedPrice = EditInput.AgreedPrice;
        reservation.sys_status = EditInput.Status;
        reservation.sys_nextInstallmentDueDate = EditInput.NextInstallmentDueDate;
        reservation.sys_expiryDate = EditInput.ExpiryDate;
        reservation.sys_lastInstallmentReminderSentOn = null;

        var beneficiary = EditInput.BeneficiaryId.HasValue
            ? await _context.tbl_Beneficiaries.FirstOrDefaultAsync(x =>
                x.sys_benefitId == EditInput.BeneficiaryId.Value && x.sys_reservationId == reservation.sys_reservationId)
            : null;
        if (beneficiary == null)
        {
            beneficiary = new tbl_Beneficiaries { sys_reservationId = reservation.sys_reservationId };
            _context.tbl_Beneficiaries.Add(beneficiary);
        }
        beneficiary.sys_benefitFullName = EditInput.BeneficiaryFullName.Trim();
        beneficiary.sys_benefitDOB = EditInput.BeneficiaryDOB;
        beneficiary.sys_benefitRelationship = EditInput.BeneficiaryRelationship.Trim();

        var invoiceIds = await _context.tbl_GraveReservationInvoices
            .Where(x => x.sys_reservationId == reservation.sys_reservationId)
            .Select(x => x.sys_invoiceId)
            .ToListAsync();
        var invoiceItems = await _context.tbl_InvoiceItems
            .Where(x => invoiceIds.Contains(x.sys_invoiceId))
            .ToListAsync();
        var payments = await _context.tbl_IncomePayments
            .Where(x => invoiceIds.Contains(x.sys_invoiceId))
            .ToListAsync();
        var outstandingInvoices = await _context.tbl_Invoices
            .Where(x => invoiceIds.Contains(x.sys_invoiceId))
            .ToListAsync();

        var totalPaid = payments.Sum(x => x.sys_amount);
        if (EditInput.AgreedPrice < totalPaid)
        {
            ModelState.AddModelError("EditInput.AgreedPrice", "The agreed price cannot be less than payments already captured.");
            await LoadAsync(EditInput.ReservationId);
            return Page();
        }

        var currentInvoiceTotal = invoiceItems.Sum(x => x.sys_itemCost);
        var priceAdjustment = EditInput.AgreedPrice - currentInvoiceTotal;
        if (Math.Abs(priceAdjustment) > 0.0001)
        {
            var adjustmentItem = invoiceItems.OrderBy(x => x.sys_itemId).FirstOrDefault();
            if (adjustmentItem != null && adjustmentItem.sys_itemCost + priceAdjustment >= 0)
            {
                adjustmentItem.sys_itemCost += priceAdjustment;
            }
            else if (outstandingInvoices.FirstOrDefault() is { } invoice)
            {
                _context.tbl_InvoiceItems.Add(new tbl_InvoiceItems
                {
                    sys_invoiceId = invoice.sys_invoiceId,
                    sys_itemName = "Reservation price adjustment",
                    sys_itemCost = priceAdjustment
                });
            }
        }

        foreach (var invoice in outstandingInvoices)
        {
            var invoiceTotal = invoiceItems.Where(x => x.sys_invoiceId == invoice.sys_invoiceId).Sum(x => x.sys_itemCost);
            var invoicePaid = payments.Where(x => x.sys_invoiceId == invoice.sys_invoiceId).Sum(x => x.sys_amount);
            if (invoiceTotal > invoicePaid)
            {
                invoice.sys_invoiceDueDate = EditInput.NextInstallmentDueDate;
            }
        }

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Reservation details and outstanding invoice due dates were updated.";
        return RedirectToPage(new { id = reservation.sys_reservationId });
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
        Sections = await _context.tbl_Sections.AsNoTracking()
            .OrderBy(x => x.sys_sectionName)
            .ToListAsync();
        PaymentMethods = await _context.tbl_PaymentMethods.AsNoTracking()
            .OrderBy(x => x.sys_paymentMethodName)
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

        if (EditInput.ReservationId == 0)
        {
            var beneficiary = Beneficiaries.FirstOrDefault();
            EditInput = new EditReservationInput
            {
                ReservationId = Reservation.sys_reservationId,
                CustomerName = Customer.sys_paymentMadeBy,
                CustomerPhone = Customer.sys_customerPhone,
                CustomerEmail = Customer.sys_customerEmail,
                SectionId = Reservation.sys_preferredSectionId ?? 0,
                AgreedPrice = Reservation.sys_agreedPrice,
                Status = string.IsNullOrWhiteSpace(Reservation.sys_status) ? "Active" : Reservation.sys_status,
                NextInstallmentDueDate = Reservation.sys_nextInstallmentDueDate ?? DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
                ExpiryDate = Reservation.sys_expiryDate,
                BeneficiaryId = beneficiary?.sys_benefitId,
                BeneficiaryFullName = beneficiary?.sys_benefitFullName ?? string.Empty,
                BeneficiaryDOB = beneficiary?.sys_benefitDOB ?? DateOnly.FromDateTime(DateTime.Today),
                BeneficiaryRelationship = beneficiary?.sys_benefitRelationship ?? string.Empty
            };
        }

        return true;
    }

    private async Task SendPaymentNotificationAsync(
        tbl_GraveReservations reservation,
        tbl_Customers? customer,
        string sectionName,
        double paymentAmount,
        double balance,
        DateOnly paymentDate)
    {
        if (customer == null)
        {
            return;
        }

        var reference = string.IsNullOrWhiteSpace(reservation.sys_reservationReference)
            ? $"RES-{reservation.sys_reservationId}"
            : reservation.sys_reservationReference;

        if (!string.IsNullOrWhiteSpace(customer.sys_customerPhone) && customer.sys_customerPhone != "n/a")
        {
            try
            {
                var response = await _whatsApp.SendReservationPaymentNotification(
                    customer.sys_customerPhone,
                    reference,
                    customer.sys_paymentMadeBy,
                    paymentAmount.ToString("N2"),
                    balance.ToString("N2"),
                    paymentDate.ToString("dd MMM yyyy"));
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("WhatsApp payment confirmation failed for reservation {ReservationReference} with status {StatusCode}.", reference, response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "WhatsApp payment confirmation failed for reservation {ReservationReference}.", reference);
            }
        }

        if (!string.IsNullOrWhiteSpace(customer.sys_customerEmail))
        {
            try
            {
                await _notificationService.SendEmailAsync(
                    customer.sys_customerEmail,
                    $"Payment received - {reference}",
                    $"<p>Dear {System.Net.WebUtility.HtmlEncode(customer.sys_paymentMadeBy)},</p>" +
                    $"<p>We received your reservation installment payment of <strong>${paymentAmount:N2}</strong> on {paymentDate:dd MMM yyyy}.</p>" +
                    $"<p>Your outstanding balance for <strong>{System.Net.WebUtility.HtmlEncode(reference)}</strong> is <strong>${balance:N2}</strong>.</p>");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Reservation payment email failed for reservation {ReservationReference}.", reference);
            }
        }
    }

    private static string NormalisePhone(string value) => Regex.Replace(value ?? string.Empty, @"[^0-9]", string.Empty);

    public class EditReservationInput
    {
        public int ReservationId { get; set; }
        [Required, StringLength(150)] public string CustomerName { get; set; } = string.Empty;
        [Required, StringLength(30)] public string CustomerPhone { get; set; } = string.Empty;
        [EmailAddress, StringLength(256)] public string? CustomerEmail { get; set; }
        [Required] public int SectionId { get; set; }
        [Range(0, double.MaxValue)] public double AgreedPrice { get; set; }
        [Required] public string Status { get; set; } = "Active";
        [Required] public DateOnly NextInstallmentDueDate { get; set; }
        public DateOnly? ExpiryDate { get; set; }
        public int? BeneficiaryId { get; set; }
        [Required, StringLength(150)] public string BeneficiaryFullName { get; set; } = string.Empty;
        [Required] public DateOnly BeneficiaryDOB { get; set; }
        [Required, StringLength(100)] public string BeneficiaryRelationship { get; set; } = string.Empty;
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
