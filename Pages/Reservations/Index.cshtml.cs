using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ZororoMemorialPark.Data;
using ZororoMemorialPark.Data.Models;
using ZororoMemorialPark.Services;
using ZororoMemorialPark.Services.WhatsApp;

namespace ZororoMemorialPark.Pages.Reservations;

[Authorize(Roles = "Manager,OperationsOfficer,FinanceOfficer")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly NotificationService _notificationService;
    private readonly WhatsAppNotificationService _whatsApp;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        ApplicationDbContext context,
        NotificationService notificationService,
        WhatsAppNotificationService whatsApp,
        ILogger<IndexModel> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _whatsApp = whatsApp;
        _logger = logger;
    }

    [BindProperty]
    public CreateReservationInput Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; } = "All";

    [BindProperty(SupportsGet = true)]
    public int? SectionFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? GraveLinkFilter { get; set; } = "All";

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public const int PageSize = 20;
    public int TotalPages { get; set; } = 1;
    public int TotalRecords { get; set; }
    public bool ShowCreateModal { get; private set; }

    public List<tbl_Sections> Sections { get; set; } = new();
    public List<ReservationRow> Reservations { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadPageAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (Input.BeneficiaryDOB == default)
        {
            ModelState.AddModelError("Input.BeneficiaryDOB", "Enter the beneficiary date of birth.");
        }

        if (Input.NextInstallmentDueDate == default)
        {
            ModelState.AddModelError("Input.NextInstallmentDueDate", "Enter the next installment due date.");
        }

        if (!ModelState.IsValid)
        {
            ShowCreateModal = true;
            await LoadPageAsync();
            return Page();
        }

        var section = await _context.tbl_Sections
            .FirstOrDefaultAsync(x => x.sys_sectionId == Input.SectionId);

        if (section == null)
        {
            ShowCreateModal = true;
            ModelState.AddModelError("Input.SectionId", "Select a valid section.");
            await LoadPageAsync();
            return Page();
        }

        var phone = NormalisePhone(Input.CustomerPhone);
        if (phone.Length < 8)
        {
            ShowCreateModal = true;
            ModelState.AddModelError("Input.CustomerPhone", "Enter a valid contact number, for example 263771234567.");
            await LoadPageAsync();
            return Page();
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        if (Input.NextInstallmentDueDate < today)
        {
            ShowCreateModal = true;
            ModelState.AddModelError("Input.NextInstallmentDueDate", "The next installment date cannot be in the past.");
            await LoadPageAsync();
            return Page();
        }

        if (Input.ExpiryDate.HasValue && Input.ExpiryDate < today)
        {
            ShowCreateModal = true;
            ModelState.AddModelError("Input.ExpiryDate", "The reservation expiry date cannot be in the past.");
            await LoadPageAsync();
            return Page();
        }

        tbl_GraveReservations reservation;
        await using (var transaction = await _context.Database.BeginTransactionAsync())
        {
            var customer = new tbl_Customers
            {
                sys_paymentMadeBy = Input.CustomerName.Trim(),
                sys_customerPhone = phone,
                sys_customerEmail = string.IsNullOrWhiteSpace(Input.CustomerEmail)
                    ? null
                    : Input.CustomerEmail.Trim()
            };
            _context.tbl_Customers.Add(customer);
            await _context.SaveChangesAsync();

            reservation = new tbl_GraveReservations
            {
                sys_customerId = customer.sys_customerId,
                sys_reservedOn = today,
                sys_preferredSectionId = section.sys_sectionId,
                sys_reservationReference = CreateReference(),
                sys_agreedPrice = section.sys_SecPrice,
                sys_status = "Active",
                sys_nextInstallmentDueDate = Input.NextInstallmentDueDate,
                sys_expiryDate = Input.ExpiryDate
            };
            _context.tbl_GraveReservations.Add(reservation);
            await _context.SaveChangesAsync();

            _context.tbl_Beneficiaries.Add(new tbl_Beneficiaries
            {
                sys_benefitFullName = Input.BeneficiaryFullName.Trim(),
                sys_benefitDOB = Input.BeneficiaryDOB,
                sys_benefitRelationship = Input.BeneficiaryRelationship.Trim(),
                sys_reservationId = reservation.sys_reservationId
            });

            var invoice = new tbl_Invoices
            {
                sys_invoiceDate = today,
                sys_customerId = customer.sys_customerId,
                sys_invoiceDueDate = Input.NextInstallmentDueDate
            };
            _context.tbl_Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            _context.tbl_InvoiceItems.Add(new tbl_InvoiceItems
            {
                sys_invoiceId = invoice.sys_invoiceId,
                sys_itemName = $"Grave reservation - {section.sys_sectionName}",
                sys_itemCost = reservation.sys_agreedPrice
            });

            _context.tbl_GraveReservationInvoices.Add(new tbl_GraveReservationInvoices
            {
                sys_reservationId = reservation.sys_reservationId,
                sys_invoiceId = invoice.sys_invoiceId
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        TempData["SuccessMessage"] = $"Reservation {reservation.sys_reservationReference} was created. You can link an available grave later.";
        await SendCreationNotificationsAsync(reservation, Input, section, phone);

        return RedirectToPage("Details", new { id = reservation.sys_reservationId });
    }

    private async Task LoadPageAsync()
    {
        StatusFilter = string.IsNullOrWhiteSpace(StatusFilter) ? "All" : StatusFilter;
        GraveLinkFilter = string.IsNullOrWhiteSpace(GraveLinkFilter) ? "All" : GraveLinkFilter;

        Sections = await _context.tbl_Sections
            .AsNoTracking()
            .OrderBy(x => x.sys_sectionName)
            .ToListAsync();

        if (Input.NextInstallmentDueDate == default)
        {
            Input.NextInstallmentDueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7));
        }

        var reservationQuery = _context.tbl_GraveReservations.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var term = SearchTerm.Trim();
            var matchingCustomerIds = _context.tbl_Customers.AsNoTracking()
                .Where(x => x.sys_paymentMadeBy.Contains(term) ||
                            x.sys_customerPhone.Contains(term) ||
                            (x.sys_customerEmail != null && x.sys_customerEmail.Contains(term)))
                .Select(x => x.sys_customerId);
            var matchingSectionIds = _context.tbl_Sections.AsNoTracking()
                .Where(x => x.sys_sectionName.Contains(term))
                .Select(x => x.sys_sectionId);
            var matchingGraveIds = _context.tbl_Graves.AsNoTracking()
                .Where(x => x.sys_plotNumber.Contains(term))
                .Select(x => x.sys_graveId);

            reservationQuery = reservationQuery.Where(x =>
                x.sys_reservationReference.Contains(term) ||
                matchingCustomerIds.Contains(x.sys_customerId) ||
                (x.sys_preferredSectionId.HasValue && matchingSectionIds.Contains(x.sys_preferredSectionId.Value)) ||
                (x.sys_graveId.HasValue && matchingGraveIds.Contains(x.sys_graveId.Value)));
        }

        if (!string.Equals(StatusFilter, "All", StringComparison.OrdinalIgnoreCase))
        {
            reservationQuery = reservationQuery.Where(x => x.sys_status == StatusFilter);
        }

        if (SectionFilter.HasValue)
        {
            reservationQuery = reservationQuery.Where(x => x.sys_preferredSectionId == SectionFilter.Value);
        }

        if (string.Equals(GraveLinkFilter, "Linked", StringComparison.OrdinalIgnoreCase))
        {
            reservationQuery = reservationQuery.Where(x => x.sys_graveId.HasValue);
        }
        else if (string.Equals(GraveLinkFilter, "Unlinked", StringComparison.OrdinalIgnoreCase))
        {
            reservationQuery = reservationQuery.Where(x => !x.sys_graveId.HasValue);
        }

        TotalRecords = await reservationQuery.CountAsync();
        TotalPages = Math.Max(1, (int)Math.Ceiling(TotalRecords / (double)PageSize));
        PageNumber = Math.Clamp(PageNumber, 1, TotalPages);

        var reservations = await reservationQuery
            .OrderByDescending(x => x.sys_reservedOn)
            .ThenByDescending(x => x.sys_reservationId)
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        var customerIds = reservations.Select(x => x.sys_customerId).Distinct().ToList();
        var sectionIds = reservations.Where(x => x.sys_preferredSectionId.HasValue)
            .Select(x => x.sys_preferredSectionId!.Value).Distinct().ToList();
        var graveIds = reservations.Where(x => x.sys_graveId.HasValue)
            .Select(x => x.sys_graveId!.Value).Distinct().ToList();
        var reservationIds = reservations.Select(x => x.sys_reservationId).ToList();

        var customers = await _context.tbl_Customers.AsNoTracking()
            .Where(x => customerIds.Contains(x.sys_customerId))
            .ToDictionaryAsync(x => x.sys_customerId);
        var sections = await _context.tbl_Sections.AsNoTracking()
            .Where(x => sectionIds.Contains(x.sys_sectionId))
            .ToDictionaryAsync(x => x.sys_sectionId);
        var graves = await _context.tbl_Graves.AsNoTracking()
            .Where(x => graveIds.Contains(x.sys_graveId))
            .ToDictionaryAsync(x => x.sys_graveId);

        var links = await _context.tbl_GraveReservationInvoices.AsNoTracking()
            .Where(x => reservationIds.Contains(x.sys_reservationId))
            .ToListAsync();
        var invoiceIds = links.Select(x => x.sys_invoiceId).Distinct().ToList();
        var totals = await _context.tbl_InvoiceItems.AsNoTracking()
            .Where(x => invoiceIds.Contains(x.sys_invoiceId))
            .GroupBy(x => x.sys_invoiceId)
            .Select(x => new { InvoiceId = x.Key, Amount = x.Sum(i => i.sys_itemCost) })
            .ToDictionaryAsync(x => x.InvoiceId, x => x.Amount);
        var payments = await _context.tbl_IncomePayments.AsNoTracking()
            .Where(x => invoiceIds.Contains(x.sys_invoiceId))
            .GroupBy(x => x.sys_invoiceId)
            .Select(x => new { InvoiceId = x.Key, Amount = x.Sum(i => i.sys_amount) })
            .ToDictionaryAsync(x => x.InvoiceId, x => x.Amount);

        Reservations = reservations.Select(reservation =>
        {
            customers.TryGetValue(reservation.sys_customerId, out var customer);
            sections.TryGetValue(reservation.sys_preferredSectionId ?? 0, out var section);
            graves.TryGetValue(reservation.sys_graveId ?? 0, out var grave);
            var linkedInvoiceIds = links.Where(x => x.sys_reservationId == reservation.sys_reservationId)
                .Select(x => x.sys_invoiceId);
            var total = linkedInvoiceIds.Sum(invoiceId => totals.GetValueOrDefault(invoiceId));
            var paid = linkedInvoiceIds.Sum(invoiceId => payments.GetValueOrDefault(invoiceId));

            return new ReservationRow
            {
                Id = reservation.sys_reservationId,
                Reference = string.IsNullOrWhiteSpace(reservation.sys_reservationReference)
                    ? $"RES-{reservation.sys_reservationId}"
                    : reservation.sys_reservationReference,
                CustomerName = customer?.sys_paymentMadeBy ?? "Customer",
                CustomerPhone = customer?.sys_customerPhone ?? "-",
                SectionName = section?.sys_sectionName ?? "Not recorded",
                GraveLabel = grave == null ? null : $"{grave.sys_plotNumber}",
                ReservedOn = reservation.sys_reservedOn,
                NextInstallmentDueDate = reservation.sys_nextInstallmentDueDate,
                Status = GetDisplayStatus(reservation.sys_status, total, paid),
                AgreedPrice = reservation.sys_agreedPrice,
                Balance = Math.Max(0, total - paid)
            };
        }).ToList();
    }

    private async Task SendCreationNotificationsAsync(
        tbl_GraveReservations reservation,
        CreateReservationInput input,
        tbl_Sections section,
        string phone)
    {
        var dueDate = reservation.sys_nextInstallmentDueDate?.ToString("dd MMM yyyy") ?? "Not scheduled";
        var message = $"Reservation {reservation.sys_reservationReference} has been created for {section.sys_sectionName}. " +
                      $"Agreed price: ${reservation.sys_agreedPrice:N2}. Next installment due: {dueDate}.";

        try
        {
            var response = await _whatsApp.SendReservationNotification(
                phone,
                reservation.sys_reservationReference,
                input.CustomerName.Trim(),
                section.sys_sectionName,
                reservation.sys_agreedPrice.ToString("N2"),
                dueDate);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("WhatsApp reservation notification failed for reservation {ReservationId} with status {StatusCode}.",
                    reservation.sys_reservationId, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WhatsApp reservation notification could not be sent for reservation {ReservationId}.", reservation.sys_reservationId);
        }

        if (!string.IsNullOrWhiteSpace(input.CustomerEmail))
        {
            try
            {
                await _notificationService.SendEmailAsync(
                    input.CustomerEmail.Trim(),
                    $"Reservation confirmation - {reservation.sys_reservationReference}",
                    $"<p>Dear {System.Net.WebUtility.HtmlEncode(input.CustomerName.Trim())},</p><p>{System.Net.WebUtility.HtmlEncode(message)}</p>");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Reservation confirmation email could not be sent for reservation {ReservationId}.", reservation.sys_reservationId);
            }
        }
    }

    private static string CreateReference() =>
        $"RES-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

    private static string NormalisePhone(string value) => Regex.Replace(value ?? string.Empty, @"[^0-9]", string.Empty);

    public static string GetDisplayStatus(string? storedStatus, double total, double paid)
    {
        if (string.Equals(storedStatus, "Cancelled", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(storedStatus, "Expired", StringComparison.OrdinalIgnoreCase))
        {
            return storedStatus;
        }

        if (total > 0 && paid >= total) return "Fully paid";
        if (paid > 0) return "Partially paid";
        return "Active";
    }

    public class CreateReservationInput
    {
        [Required, Display(Name = "Section")]
        public int SectionId { get; set; }

        [Required, StringLength(150), Display(Name = "Customer name")]
        public string CustomerName { get; set; } = string.Empty;

        [Required, StringLength(30), Display(Name = "WhatsApp / phone number")]
        public string CustomerPhone { get; set; } = string.Empty;

        [EmailAddress, StringLength(256), Display(Name = "Email address")]
        public string? CustomerEmail { get; set; }

        [Required, StringLength(150), Display(Name = "Beneficiary full name")]
        public string BeneficiaryFullName { get; set; } = string.Empty;

        [Required, Display(Name = "Beneficiary date of birth")]
        public DateOnly BeneficiaryDOB { get; set; }

        [Required, StringLength(100), Display(Name = "Relationship")]
        public string BeneficiaryRelationship { get; set; } = string.Empty;

        [Required, Display(Name = "Next installment due")]
        public DateOnly NextInstallmentDueDate { get; set; }

        [Display(Name = "Reservation expiry")]
        public DateOnly? ExpiryDate { get; set; }
    }

    public class ReservationRow
    {
        public int Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string SectionName { get; set; } = string.Empty;
        public string? GraveLabel { get; set; }
        public DateOnly ReservedOn { get; set; }
        public DateOnly? NextInstallmentDueDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public double AgreedPrice { get; set; }
        public double Balance { get; set; }
    }
}
