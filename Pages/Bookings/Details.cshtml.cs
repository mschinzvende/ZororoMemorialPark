using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ZororoMemorialPark.Data.Models;
using ZororoMemorialPark.Data;
using static System.Collections.Specialized.BitVector32;
using ZororoMemorialPark.Services;
using System.Globalization;
using static ZororoMemorialPark.Pages.Graves.GraveDetailsModel;
using Microsoft.AspNetCore.Identity;
using ZororoMemorialPark.Services.WhatsApp;

namespace ZororoMemorialPark.Pages.Bookings
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly IAppLogger _appLogger;

        private readonly WhatsAppNotificationService _whatsApp;

        public DetailsModel(ApplicationDbContext db, UserManager<IdentityUser> userManager, IAppLogger appLogger, WhatsAppNotificationService whatsApp)
        {
            _db = db;
            _userManager = userManager;
            _appLogger = appLogger;
            _whatsApp = whatsApp;
        }



        [BindProperty(SupportsGet = true)]
        public int id { get; set; }


        // Discount
        [BindProperty] public double DiscountAmount { get; set; }
        [BindProperty] public string DiscountReason { get; set; }

        //Get information from the agent bot
        public tbl_Agents? BookingAgent { get; set; }

        public string? ReceiptImageUrl { get; set; }

        public bool PaymentReceived { get; set; }


        // Permissions
        public bool CanOverride => User.IsInRole("Manager");

        public bool IsAlreadyAllocated { get; set; } = new();


        //customer fields

        [BindProperty] public string DeceasedFirstName { get; set; }
        [BindProperty] public string DeceasedLastName { get; set; }

        [BindProperty] public string PaymentMadeBy { get; set; }
        [BindProperty] public string CustomerPhone { get; set; }

        public tbl_BurialBookings Booking { get; set; }
        public tbl_Customers Customers { get; set; }
        public tbl_Sections Section { get; set; }
        public double SectionPrice => Section?.sys_SecPrice ?? 0;

        public List<tbl_Sections> Sections { get; set; } = new();

        public List<tbl_PaymentMethods> PaymentMethods { get; set; } = new();

        public double InvoiceTotal { get; set; }
        public double PaidTotal { get; set; }
        public double Balance => InvoiceTotal - PaidTotal;

        public bool IsPaidInFull => InvoiceTotal > 0 && PaidTotal >= InvoiceTotal;

        public List<tbl_BurialPayments> Payments { get; set; } = new();

        public bool IsAgent { get; set; }
        public int? CurrentAgentId { get; set; }

        public System.Collections.Generic.List<tbl_ReceiptBooks> ReceiptBooks { get; set; } = new();

        // Payment input
        [BindProperty] public int ReceiptBookId { get; set; }
        [BindProperty] public double Amount { get; set; }
        [BindProperty] public string Method { get; set; } = "Cash";

        [BindProperty] public int AgentId { get; set; }

        [BindProperty] public int ReceiptNumber { get; set; }

        // Allocate input
        [BindProperty] public int GraveId { get; set; }

        // Deceased fields (only created at allocation)
        [BindProperty] public string DeceasedFullName { get; set; }
        [BindProperty] public DateOnly DeceasedDOB { get; set; }
        [BindProperty] public DateOnly DeceasedDOD { get; set; }
        [BindProperty] public string Gender { get; set; }
        [BindProperty] public string NextOfKinName { get; set; }
        [BindProperty] public string NextOfKinAddress { get; set; }
        [BindProperty] public string NextOfKinEmail { get; set; } = "n/a";
        [BindProperty] public string NextOfKinPhone { get; set; }
        [BindProperty] public string MaritalStatus { get; set; }
        [BindProperty] public string NameOfSpouse { get; set; } = "n/a";
        [BindProperty] public string Nationality { get; set; }
        [BindProperty] public string sys_repatriationSource { get; set; }

        // Override (partial payment)
        [BindProperty] public bool OverridePartialPayment { get; set; }
        [BindProperty] public string OverrideReason { get; set; }

        public bool SectionLocked => Booking?.sys_invoiceId != null;

        public List<PaymentViewRow> PaymentRows { get; set; } = new();

        public List<tbl_Graves> AvailableGraves { get; set; } = new();

        public List<tbl_Agents> Agents { get; set; } = new();


        [BindProperty] public IFormFile? BurialOrder { get; set; }
        [BindProperty] public DateOnly burialDate { get; set; }
        [BindProperty] public string burialDateTime { get; set; }

        public bool AgentRecordExists { get; set; }
        public List<ReceiptBookView> AvailableBooks { get; set; } = new();
        public tbl_ReceiptBooks AgentReceiptBook { get; set; } = new();


        public async Task<IActionResult> OnPostAllocateGraveAsync()
        {
            await LoadAsync();
            if (Booking == null) return RedirectToPage("Index");

            // ----- SERVER DATE VALIDATION -----
            if (DeceasedDOB > DateOnly.FromDateTime(DateTime.Today))
            {
                TempData["Err"] = "DOB cannot be in the future.";
                return RedirectToPage(new { id });
            }

            if (DeceasedDOD < DeceasedDOB)
            {
                TempData["Err"] = "DOD cannot be before DOB.";
                return RedirectToPage(new { id });
            }



            // ----- PAYMENT CHECK -----
            if (!IsPaidInFull)
            {
                if (!OverridePartialPayment || !CanOverride)
                {
                    TempData["Err"] = "Invoice not fully paid. Override not authorized.";
                    return RedirectToPage(new { id });
                }

                if (string.IsNullOrWhiteSpace(OverrideReason))
                {
                    TempData["Err"] = "Override reason is required.";
                    return RedirectToPage(new { id });
                }
            }


            // ----- SAVE BURIAL ORDER -----
            string burialOrderPath = null;
            if (BurialOrder != null)
            {
                var dir = Path.Combine("wwwroot", "uploads", "burial-orders");
                Directory.CreateDirectory(dir);
                var file = $"{Guid.NewGuid()}{Path.GetExtension(BurialOrder.FileName)}";
                var full = Path.Combine(dir, file);
                using var fs = new FileStream(full, FileMode.Create);
                await BurialOrder.CopyToAsync(fs);
                burialOrderPath = $"/uploads/burial-orders/{file}";
            }

            using var tx = await _db.Database.BeginTransactionAsync();

            var deceased = new tbl_Deceased
            {
                sys_deceasedFullName = DeceasedFullName,
                sys_deceasedDOB = DeceasedDOB,
                sys_deceasedDOD = DeceasedDOD,
                sys_deceasedGender = Gender,
                sys_deceasedNationality = Nationality,
                sys_deceasedNextOfKinName = NextOfKinName,
                sys_deceasedNextOfKinPhone = NextOfKinPhone,
                sys_deceasedNextOfKinEmail = NextOfKinEmail,
                sys_deceasedNextOfKinAddress = NextOfKinAddress,
                sys_deceasedMaritalStatus = MaritalStatus,
                sys_deceasedNameOfSpouse = NameOfSpouse
            };

            _db.tbl_Deceased.Add(deceased);
            await _db.SaveChangesAsync();

            var alloc = new tbl_GraveAllocations
            {
                sys_graveId = GraveId,
                sys_deceasedId = deceased.sys_deceasedId,
                sys_customerId = Booking.sys_customerId,
                sys_dateTimeOfBurial = Booking.sys_requestedBurialDateTime,
                sys_allocatedOn = DateOnly.FromDateTime(DateTime.Now),
                sys_burialOrder = burialOrderPath,
                sys_bookingId = Booking.sys_bookingId,
                sys_repatriationSource = sys_repatriationSource
                
            };

            _db.tbl_GraveAllocations.Add(alloc);

            Booking.sys_bookingStatus = "Allocated";
            var grave = await _db.tbl_Graves.FindAsync(GraveId);
            grave.sys_graveSaleStatus = "Occupied";

            await _db.SaveChangesAsync();

            // ---------- FINALIZE COMMISSION ----------
            var commissions = await _db.tbl_CommissionsDue
                .Where(c => c.sys_bookingId == Booking.sys_bookingId && c.sys_graveId == null)
                .ToListAsync();

            foreach (var c in commissions)
            {
                c.sys_graveId = GraveId;
            }

            await _db.SaveChangesAsync();

            // Finalize Linking Allocation Invoices

            var linkerInvoices = await _db.tbl_GraveAllocationInvoices
                .Where(a => a.sys_invoiceId == Booking.sys_invoiceId && a.sys_allocationId == null)
                .ToListAsync();

            foreach (var link in linkerInvoices)
            {
                link.sys_allocationId = alloc.sys_allocationId;
            }

            await _db.SaveChangesAsync();

            await tx.CommitAsync();

            TempData["Ok"] = "Grave allocated successfully.";
            return RedirectToPage(new { id });
        }


        public async Task<IActionResult> OnPostApplyDiscountAsync()
        {
            await LoadAsync();
            if (Booking == null || !Booking.sys_invoiceId.HasValue)
                return RedirectToPage("Index");

            if (!CanOverride)
            {
                TempData["Err"] = "You are not authorized to apply discounts.";
                return RedirectToPage(new { id });
            }

            if (DiscountAmount <= 0)
            {
                TempData["Err"] = "Discount amount must be greater than zero.";
                return RedirectToPage(new { id });
            }

            if (string.IsNullOrWhiteSpace(DiscountReason))
            {
                TempData["Err"] = "Discount reason is required.";
                return RedirectToPage(new { id });
            }

            var items = await _db.tbl_InvoiceItems
                .Where(i => i.sys_invoiceId == Booking.sys_invoiceId.Value)
                .ToListAsync();

            var total = items.Sum(i => i.sys_itemCost);

            if (DiscountAmount >= total)
            {
                TempData["Err"] = "Discount cannot exceed invoice total.";
                return RedirectToPage(new { id });
            }

            _db.tbl_InvoiceItems.Add(new tbl_InvoiceItems
            {
                sys_invoiceId = Booking.sys_invoiceId.Value,
                sys_itemName = $"Discount – {DiscountReason}",
                sys_itemCost = -DiscountAmount
            });

            await _db.SaveChangesAsync();

            TempData["Ok"] = "Discount applied successfully.";
            return RedirectToPage(new { id });
        }


        public async Task<IActionResult> OnPostUpdateBookingDetailsAsync()
        {
            try
            {
                await LoadAsync();
                if (Booking == null) return RedirectToPage("Index");

                if (string.IsNullOrWhiteSpace(DeceasedFirstName) ||
                    string.IsNullOrWhiteSpace(DeceasedLastName))
                {
                    TempData["Err"] = "Deceased name is required.";
                    return RedirectToPage(new { id });
                }

                // Update booking (deceased names)
                Booking.sys_deceasedFirstName = DeceasedFirstName.Trim();
                Booking.sys_deceasedLastName = DeceasedLastName.Trim();

                

                // Update customer
                var customer = await _db.tbl_Customers
                    .FirstOrDefaultAsync(c => c.sys_customerId == Booking.sys_customerId);

                if (customer != null)
                {
                    customer.sys_paymentMadeBy = string.IsNullOrWhiteSpace(PaymentMadeBy)
                        ? "n/a"
                        : PaymentMadeBy.Trim();

                    customer.sys_customerPhone = string.IsNullOrWhiteSpace(CustomerPhone)
                        ? "n/a"
                        : CustomerPhone.Trim();
                }

                await _db.SaveChangesAsync();

                
                TempData["Ok"] = "Booking details updated successfully.";

                var bookedSection = await _db.tbl_Sections.AsNoTracking().FirstOrDefaultAsync(s => s.sys_sectionId == Booking.sys_sectionId);


                var response = await _whatsApp.SendBookingNotification(customer.sys_customerPhone, "burial_booking_modification",
                $"{Booking.sys_deceasedFirstName} {Booking.sys_deceasedLastName}", Booking.sys_requestedBurialDateTime.ToString("f"),
                bookedSection.sys_sectionName, customer.sys_paymentMadeBy);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Ok"] = "Booking Notification Sent to Customer";
                }


                return RedirectToPage(new { id });
            }
            catch (Exception ex)
            {

                TempData["Err"] = $"Unexpected Error: {ex.Message}";
                return RedirectToPage(new { id });
            }
        }


        public async Task<IActionResult> OnPostReverseInvoiceAsync()
        {
            await LoadAsync();

            if (Booking == null || !Booking.sys_invoiceId.HasValue)
                return RedirectToPage("Index");

            // -----------------------------
            // PERMISSION CHECK
            // -----------------------------
            if (!User.IsInRole("Manager"))
            {
                TempData["Err"] = "You are not authorized to reverse invoices.";
                return RedirectToPage(new { id });
            }

            // -----------------------------
            // CHECK PAYMENTS
            // -----------------------------
            var hasPayments = await _db.tbl_BurialPayments
                .AnyAsync(p => p.sys_invoiceId == Booking.sys_invoiceId.Value);

            if (hasPayments)
            {
                TempData["Err"] = "Cannot reverse invoice with existing payments.";
                return RedirectToPage(new { id });
            }

            // -----------------------------
            // CHECK ALLOCATION
            // -----------------------------
            var isAllocated = await _db.tbl_GraveAllocations
                .AnyAsync(a => a.sys_bookingId == Booking.sys_bookingId);

            if (isAllocated)
            {
                TempData["Err"] = "Cannot reverse invoice after grave allocation.";
                return RedirectToPage(new { id });
            }

            using var tx = await _db.Database.BeginTransactionAsync();

            // -----------------------------
            // DELETE INVOICE ITEMS
            // -----------------------------
            var items = await _db.tbl_InvoiceItems
                .Where(i => i.sys_invoiceId == Booking.sys_invoiceId.Value)
                .ToListAsync();

            _db.tbl_InvoiceItems.RemoveRange(items);

            // -----------------------------
            // DELETE LINKER
            // -----------------------------
            var linkers = await _db.tbl_GraveAllocationInvoices
                .Where(l => l.sys_invoiceId == Booking.sys_invoiceId.Value)
                .ToListAsync();

            _db.tbl_GraveAllocationInvoices.RemoveRange(linkers);

            // -----------------------------
            // DELETE INVOICE
            // -----------------------------
            var invoice = await _db.tbl_Invoices
                .FirstOrDefaultAsync(i => i.sys_invoiceId == Booking.sys_invoiceId.Value);

            if (invoice != null)
                _db.tbl_Invoices.Remove(invoice);

            // -----------------------------
            // RESET BOOKING
            // -----------------------------
            Booking.sys_invoiceId = null;
            Booking.sys_bookingStatus = "PendingPayment";

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            TempData["Ok"] = "Invoice reversed successfully.";

            return RedirectToPage(new { id });
        }
        public async Task<IActionResult> OnGetAsync()
        {
            await LoadAsync();
            if (Booking == null) return RedirectToPage("Index");
            return Page();
        }

        private async Task LoadAsync()
        {

           Agents = await _db.tbl_Agents
                    .AsNoTracking()
                    .ToListAsync();

            Booking = await _db.tbl_BurialBookings.FirstOrDefaultAsync(b => b.sys_bookingId == id);
            if (Booking == null) return;


            // WhatsApp Booking Information
            if (Booking.sys_agentId > 0)
            {
                BookingAgent = await _db.tbl_Agents
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a =>
                        a.sys_agentId == Booking.sys_agentId);
            }

            PaymentReceived = Booking.sys_paymentReceived;

            if (!string.IsNullOrWhiteSpace(
                    Booking.sys_receiptPath))
            {
                ReceiptImageUrl =
                    $"/receipts/{Booking.sys_receiptPath}";
            }




            Customers = await _db.tbl_Customers
                .FirstOrDefaultAsync(c => c.sys_customerId == Booking.sys_customerId);

            if (Customers == null) return;
            


            IsAlreadyAllocated = await _db.tbl_GraveAllocations
   .AnyAsync(a => a.sys_bookingId == Booking.sys_bookingId);
            Section = await _db.tbl_Sections.AsNoTracking().FirstOrDefaultAsync(s => s.sys_sectionId == Booking.sys_sectionId);

            // Only show sections that still have available graves
            Sections = await _db.tbl_Sections.AsNoTracking()
                .Where(s => _db.tbl_Graves.Any(g =>
                    g.sys_sectionId == s.sys_sectionId &&
                    g.sys_graveSaleStatus == "Unsold"))
                .OrderBy(s => s.sys_sectionName)
                .ToListAsync();

            PaymentMethods = await _db.tbl_PaymentMethods.ToListAsync();

            AvailableGraves = await _db.tbl_Graves.AsNoTracking()
                    .Where(g =>
                        g.sys_sectionId == Booking.sys_sectionId &&
                        g.sys_graveSaleStatus == "Unsold")
                    .OrderBy(g => g.sys_plotNumber)
                    .ToListAsync();

            


            if (Booking.sys_invoiceId.HasValue)
            {
                var payments = await (
                    from p in _db.tbl_BurialPayments.AsNoTracking()
                    join rb in _db.tbl_ReceiptBooks.AsNoTracking()
                        on p.sys_receiptBookId equals rb.sys_receiptBookId
                    
                    where p.sys_invoiceId == Booking.sys_invoiceId.Value
                    orderby p.sys_paymentDate
                    select new PaymentViewRow
                    {
                        Date = p.sys_paymentDate,
                        ReceiptBookNumber = rb.sys_bookNumber,
                        ReceiptNumber = p.sys_receiptNumber,
                        AgentName = rb.issuedTo,
                        Method = p.sys_paymentMethod,
                        Amount = p.sys_amountPaid
                    }
                ).ToListAsync();

                PaymentRows = payments;

                InvoiceTotal = await _db.tbl_InvoiceItems
                    .Where(i => i.sys_invoiceId == Booking.sys_invoiceId.Value)
                    .SumAsync(i => (double?)i.sys_itemCost) ?? 0;

                PaidTotal = PaymentRows.Sum(x => x.Amount);
            }



            // Get ReceiptBooks
            
                ReceiptBooks = await _db.tbl_ReceiptBooks.AsNoTracking()
                    .Where(r => !r.sys_isClosed)
                    .OrderByDescending(r => r.sys_issueDate)
                    .ToListAsync();
            

            // Totals
            if (Booking.sys_invoiceId.HasValue)
            {
                InvoiceTotal = await _db.tbl_InvoiceItems.AsNoTracking()
                    .Where(i => i.sys_invoiceId == Booking.sys_invoiceId.Value)
                    .SumAsync(i => (double?)i.sys_itemCost) ?? 0;

                PaidTotal = await _db.tbl_BurialPayments.AsNoTracking()
                    .Where(p => p.sys_invoiceId == Booking.sys_invoiceId.Value)
                    .SumAsync(p => (double?)p.sys_amountPaid) ?? 0;
            }
        }


        //change date and time of burial
        public async Task<IActionResult> OnPostChangeDateTimeAsync()
        {
            await LoadAsync();
            if (Booking == null) return RedirectToPage("Index");

            if (string.IsNullOrWhiteSpace(burialDateTime))
            {
                TempData["Err"] = "Please select a burial time slot.";
                return RedirectToPage(new { id });
            }

            if (!DateTime.TryParseExact(
                burialDateTime,
                "yyyy-MM-ddTHH:mm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var selected))
            {
                TempData["Err"] = "Invalid burial time slot.";
                return RedirectToPage(new { id });
            }

            var now = DateTime.Now;
            var today = DateOnly.FromDateTime(now);
            var selectedDate = DateOnly.FromDateTime(selected);

            if (selectedDate < today)
            {
                TempData["Err"] = "Burial date cannot be in the past.";
                return RedirectToPage(new { id });
            }

            if (selectedDate > today.AddDays(7))
            {
                TempData["Err"] = "Burial date cannot exceed 7 days in advance.";
                return RedirectToPage(new { id });
            }

            if (selectedDate == today && selected <= now)
            {
                TempData["Err"] = "For today, burial time must be later than the current time.";
                return RedirectToPage(new { id });
            }

            var settings = await _db.SystemSettings.FirstAsync();
            var minutes = Math.Max(5, settings.MinutesBetweenBurials);
            var window = TimeSpan.FromMinutes(minutes);

            var dayStart = selected.Date;
            var dayEnd = dayStart.AddDays(1);

            var minTime = selected.AddMinutes(-minutes);
            var maxTime = selected.AddMinutes(minutes);

            var conflicting = await _db.tbl_BurialBookings
                .AsNoTracking()
                .AnyAsync(b =>
                    b.sys_bookingId != Booking.sys_bookingId &&
                    b.sys_requestedBurialDateTime >= minTime &&
                    b.sys_requestedBurialDateTime <= maxTime &&
                    b.sys_bookingStatus != "Cancelled" &&
                    b.sys_bookingStatus != "Rejected" &&
                    b.sys_bookingStatus != "Expired");


            if (conflicting)
            {
                TempData["Err"] = "Selected burial slot is no longer available.";
                return RedirectToPage(new { id });
            }

            Booking.sys_requestedBurialDateTime = selected;
            await _db.SaveChangesAsync();

            TempData["Ok"] = "Burial date and time updated.";


            var bookedSection = await _db.tbl_Sections.AsNoTracking().FirstOrDefaultAsync(s => s.sys_sectionId == Booking.sys_sectionId);
            var customer = await _db.tbl_Customers.AsNoTracking().FirstOrDefaultAsync(c => c.sys_customerId == Booking.sys_customerId);


            var response = await _whatsApp.SendBookingNotification(customer.sys_customerPhone, "burial_booking_modification",
            $"{Booking.sys_deceasedFirstName} {Booking.sys_deceasedLastName}", Booking.sys_requestedBurialDateTime.ToString("f"),
            bookedSection.sys_sectionName, customer.sys_paymentMadeBy);

            if (response.IsSuccessStatusCode)
            {
                TempData["Ok"] = "Booking Notification Sent to Customer";
            }
            return RedirectToPage(new { id });
        }




        // Lock section change after invoice: allow section edit only when no invoice
        public async Task<IActionResult> OnPostChangeSectionAsync(int newSectionId)
        {
            await LoadAsync();
            if (Booking == null) return RedirectToPage("Index");

            if (Booking.sys_invoiceId.HasValue)
            {
                TempData["Err"] = "Section is locked because an invoice has already been raised.";
                return RedirectToPage(new { id });
            }

            // Ensure new section has available graves
            var hasAvailable = await _db.tbl_Graves.AnyAsync(g =>
                g.sys_sectionId == newSectionId && g.sys_graveSaleStatus == "Unsold");

            if (!hasAvailable)
            {
                TempData["Err"] = "Selected section has no available graves.";
                return RedirectToPage(new { id });
            }

            Booking.sys_sectionId = newSectionId;
            await _db.SaveChangesAsync();

            TempData["Ok"] = "Section updated.";


            var bookedSection = await _db.tbl_Sections.AsNoTracking().FirstOrDefaultAsync(s => s.sys_sectionId == Booking.sys_sectionId);
            var customer = await _db.tbl_Customers.AsNoTracking().FirstOrDefaultAsync(c => c.sys_customerId == Booking.sys_customerId);


            var response = await _whatsApp.SendBookingNotification(customer.sys_customerPhone, "burial_booking_modification",
            $"{Booking.sys_deceasedFirstName} {Booking.sys_deceasedLastName}", Booking.sys_requestedBurialDateTime.ToString("f"),
            bookedSection.sys_sectionName, customer.sys_paymentMadeBy);

            if (response.IsSuccessStatusCode)
            {
                TempData["Ok"] = "Booking Notification Sent to Customer";
            }
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostRaiseInvoiceAsync()
        {
            await LoadAsync();
            if (Booking == null) return RedirectToPage("Index");

            if (Booking.sys_invoiceId.HasValue)
                return RedirectToPage(new { id });

            // Still ensure section has available graves at invoicing stage
            var hasAvailable = await _db.tbl_Graves.AnyAsync(g =>
                g.sys_sectionId == Booking.sys_sectionId && g.sys_graveSaleStatus == "Unsold");

            if (!hasAvailable)
            {
                TempData["Err"] = "Cannot raise invoice. Selected section has no available graves.";
                return RedirectToPage(new { id });
            }

            using var tx = await _db.Database.BeginTransactionAsync();

            var inv = new tbl_Invoices
            {
                sys_invoiceDate = DateOnly.FromDateTime(DateTime.Now),
                sys_customerId = Booking.sys_customerId,
                sys_invoiceDueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(3))
            };
            _db.tbl_Invoices.Add(inv);
            await _db.SaveChangesAsync();

            var sec = await _db.tbl_Sections.AsNoTracking().FirstAsync(s => s.sys_sectionId == Booking.sys_sectionId);

            _db.tbl_InvoiceItems.Add(new tbl_InvoiceItems
            {
                sys_invoiceId = inv.sys_invoiceId,
                sys_itemName = $"Burial Fee – {sec.sys_sectionName}",
                sys_itemCost = sec.sys_SecPrice
            });

            Booking.sys_invoiceId = inv.sys_invoiceId;
            Booking.sys_bookingStatus = "Invoiced";

            await _db.SaveChangesAsync();

            //Link the invoice to GraveAllocation Invoices table

            var linker = new tbl_GraveAllocationInvoices
            {
                sys_allocationId = null,
                sys_invoiceId = inv.sys_invoiceId
            };
            _db.tbl_GraveAllocationInvoices.Add(linker);
            await _db.SaveChangesAsync();

            await tx.CommitAsync();

            TempData["Ok"] = "Invoice raised successfully. Section is now locked.";
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostCapturePaymentAsync()
        {
            await LoadAsync();
            if (Booking == null) return RedirectToPage("Index");

            if (!Booking.sys_invoiceId.HasValue)
            {
                TempData["Err"] = "Raise an invoice before capturing payment.";
                return RedirectToPage(new { id });
            }

            var balance = InvoiceTotal - PaidTotal;

            if (balance <= 0)
            {
                TempData["Err"] = "Invoice is already fully paid.";
                return RedirectToPage(new { id });
            }

            if (Amount <= 0)
            {
                TempData["Err"] = "Amount must be greater than zero.";
                return RedirectToPage(new { id });
            }

            if (Amount > balance)
            {
                TempData["Err"] = $"Payment exceeds outstanding balance. Maximum allowed is {balance}.";
                return RedirectToPage(new { id });
            }


            if (ReceiptBookId <= 0)
            {
                TempData["Err"] = "Select a receipt book.";
                return RedirectToPage(new { id });
            }

            // Ensure selected receipt book is valid for agent/non-agent
            
            
                var ok = await _db.tbl_ReceiptBooks.AsNoTracking()
                    .AnyAsync(r => r.sys_receiptBookId == ReceiptBookId && !r.sys_isClosed);

                if (!ok)
                {
                    TempData["Err"] = "Invalid receipt book selection.";
                    return RedirectToPage(new { id });
                }
            

            using var tx = await _db.Database.BeginTransactionAsync();

            var book = await _db.tbl_ReceiptBooks.FirstAsync(r => r.sys_receiptBookId == ReceiptBookId);

            if (book.sys_isClosed)
            {
                await tx.RollbackAsync();
                TempData["Err"] = "Receipt book is closed.";
                return RedirectToPage(new { id });
            }

            var nextReceipt = ReceiptNumber + 1;
            if (nextReceipt < book.sys_startNumber) nextReceipt = book.sys_startNumber;

            if (nextReceipt > book.sys_endNumber)
            {
                book.sys_isClosed = true;
                await _db.SaveChangesAsync();
                await tx.RollbackAsync();
                TempData["Err"] = "Receipt book exhausted and has been closed.";
                return RedirectToPage(new { id });
            }

            book.sys_currentNumber = ReceiptNumber;
            if (book.sys_currentNumber >= book.sys_endNumber)
                book.sys_isClosed = true;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            _db.tbl_BurialPayments.Add(new tbl_BurialPayments
            {
                sys_invoiceId = Booking.sys_invoiceId.Value,
                sys_receiptBookId = book.sys_receiptBookId,
                sys_receiptNumber = ReceiptNumber,
                sys_amountPaid = Amount,
                sys_paymentMethod = string.IsNullOrWhiteSpace(Method) ? "Cash" : Method.Trim(),
                sys_capturedByUserId = userId
            });

            await _db.SaveChangesAsync();

            _db.tbl_IncomePayments.Add(new tbl_IncomePayments
            {
                sys_invoiceId = Booking.sys_invoiceId.Value,
                sys_date = DateOnly.FromDateTime(DateTime.Now),
                sys_receiptNumber = ReceiptNumber,
                sys_amount = Amount,
                sys_paymentMethod = string.IsNullOrWhiteSpace(Method) ? "Cash" : Method.Trim(),


            });

            await _db.SaveChangesAsync();

            await tx.CommitAsync();

            // refresh totals and update booking status
            await LoadAsync();

            var paidFull = InvoiceTotal <= 0 ? true : PaidTotal >= InvoiceTotal;
            Booking.sys_bookingStatus = paidFull ? "Paid in Full" : "Partialy Paid";

            await _db.SaveChangesAsync();


            //Calculate the agent commission 

            // ---------- COMMISSION LOGIC ----------
            if (AgentId!= 0)
            {
                var agent = await _db.tbl_Agents.FindAsync(AgentId);

                if (agent != null)
                {
                    double commissionAmount = agent.sys_isPercentage
                        ? (Amount * agent.sys_commission / 100.0)
                        : agent.sys_commission;

                    if (commissionAmount > 0)
                    {
                        // Grave not known yet ? link commission to BOOKING
                        _db.tbl_CommissionsDue.Add(new tbl_CommissionsDue
                        {
                            sys_bookingId = Booking.sys_bookingId,   // IMPORTANT
                            sys_agentId = agent.sys_agentId,
                            sys_amount = commissionAmount,
                            sys_isPaid = false
                            
                        });

                        await _db.SaveChangesAsync();
                    }
                }
            }


            TempData["Ok"] = $"Payment captured. Receipt #{nextReceipt} issued.";



            return RedirectToPage(new { id });
        }

   


        public async Task<IActionResult> OnGetAvailableBurialSlotsAsync(string burialDate)
        {
            if (!DateOnly.TryParseExact(burialDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                return new JsonResult(new { slots = Array.Empty<string>(), message = "Invalid date." });

            var settings = await _db.SystemSettings.AsNoTracking().FirstOrDefaultAsync();
            if (settings == null)
                return new JsonResult(new { slots = Array.Empty<string>(), message = "System settings not configured." });

            // Daily capacity check (bookings + allocations)
            var dayStart = date.ToDateTime(new TimeOnly(0, 0));
            var dayEnd = dayStart.AddDays(1);

            var activeBookingCount = await _db.tbl_BurialBookings.AsNoTracking()
                .Where(b => b.sys_requestedBurialDateTime >= dayStart &&
                            b.sys_requestedBurialDateTime < dayEnd &&
                            b.sys_bookingStatus != "Cancelled" &&
                            b.sys_bookingStatus != "Rejected" &&
                            b.sys_bookingStatus != "Expired")
                .CountAsync();

            var allocationCount = await _db.tbl_GraveAllocations.AsNoTracking()
                .Where(a => a.sys_dateTimeOfBurial >= dayStart &&
                            a.sys_dateTimeOfBurial < dayEnd)
                .CountAsync();

            if (activeBookingCount + allocationCount >= settings.MaxBurialsPerDay)
                return new JsonResult(new { slots = Array.Empty<string>(), message = "Daily capacity reached." });

            // Build slots 08:00–17:00 stepping by MinutesBetweenBurials
            var minutes = Math.Max(5, settings.MinutesBetweenBurials);

            var start = date.ToDateTime(new TimeOnly(8, 0));
            var end = date.ToDateTime(new TimeOnly(17, 0));

            var allSlots = new List<DateTime>();
            for (var t = start; t <= end; t = t.AddMinutes(minutes))
                allSlots.Add(t);

            // Pull existing scheduled times (bookings + allocations)
            var bookedTimes = await _db.tbl_BurialBookings.AsNoTracking()
                .Where(b => b.sys_requestedBurialDateTime >= dayStart &&
                            b.sys_requestedBurialDateTime < dayEnd &&
                            b.sys_bookingStatus != "Cancelled" &&
                            b.sys_bookingStatus != "Rejected" &&
                            b.sys_bookingStatus != "Expired")
                .Select(b => b.sys_requestedBurialDateTime)
                .ToListAsync();

            var allocatedTimes = await _db.tbl_GraveAllocations.AsNoTracking()
                .Where(a => a.sys_dateTimeOfBurial >= dayStart &&
                            a.sys_dateTimeOfBurial < dayEnd)
                .Select(a => a.sys_dateTimeOfBurial)
                .ToListAsync();

            var blockedTimes = bookedTimes.Concat(allocatedTimes).ToList();
            var window = TimeSpan.FromMinutes(minutes);

            var available = allSlots
                .Where(s => !blockedTimes.Any(bt => (bt - s).Duration() < window))
                .Select(s => s.ToString("yyyy-MM-ddTHH:mm"))
                .ToList();

            if (available.Count == 0)
                return new JsonResult(new { slots = Array.Empty<string>(), message = "No slots available." });

            return new JsonResult(new { slots = available, message = "" });
        }


        public async Task<IActionResult> OnGetDownloadInvoiceAsync()
        {
            await LoadAsync();

            if (Booking == null || !Booking.sys_invoiceId.HasValue)
                return RedirectToPage("Index");

            var invoice = await _db.tbl_Invoices.FindAsync(Booking.sys_invoiceId.Value);

            var items = await _db.tbl_InvoiceItems
                .Where(i => i.sys_invoiceId == invoice.sys_invoiceId)
                .ToListAsync();

            var payments = await _db.tbl_BurialPayments
                .Where(p => p.sys_invoiceId == invoice.sys_invoiceId)
                .OrderBy(p => p.sys_paymentDate)
                .ToListAsync();

            var settings = await _db.SystemSettings.FirstAsync();

            // Optional logo path
            var logoPath = string.IsNullOrEmpty(settings.LogoPath)
                ? null
                : Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", settings.LogoPath.TrimStart('/'));

            var pdfService = HttpContext.RequestServices.GetRequiredService<InvoicePdfService>();

            var pdfBytes = pdfService.GenerateInvoicePdf(
                invoice,
                settings,
                Booking,
                items,
                payments,
                logoPath
            );

            return File(
                pdfBytes,
                "application/pdf",
                $"Invoice_{invoice.sys_invoiceId}.pdf"
            );
        }


    }

    public class PaymentViewRow
    {
        public DateTime Date { get; set; }
        public string ReceiptBookNumber { get; set; }
        public int ReceiptNumber { get; set; }
        public string AgentName { get; set; }
        public string Method { get; set; }
        public double Amount { get; set; }
    }

}
