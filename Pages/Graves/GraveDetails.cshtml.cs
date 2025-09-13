using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using ZororoMemorialPark.Data;
using ZororoMemorialPark.Data.Models;
using ZororoMemorialPark.Services;

namespace ZororoMemorialPark.Pages.Graves
{
    public class GraveDetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        private readonly IAppLogger _appLogger;

        public GraveDetailsModel(ApplicationDbContext context, UserManager<IdentityUser> userManager, IAppLogger appLogger)
        {
            _context = context;
            _userManager = userManager;
            _appLogger = appLogger;
        }

        [BindProperty(SupportsGet = true)]
        public int id { get; set; } // Grave ID from route

        // Data for the page
        public tbl_Graves Grave { get; set; }
        public tbl_Sections Section { get; set; }
        public tbl_DevelopmentStages Stage { get; set; }
        public tbl_GraveAllocations Allocation { get; set; }
        public tbl_Deceased Deceased { get; set; }
        public bool AllocationPendingPayment { get; set; }
        public tbl_GraveReservations Reservation { get; set; }
        public tbl_Customers Customer { get; set; }
        public List<tbl_Beneficiaries> Beneficiaries { get; set; }
        public List<JobDto> Jobs { get; set; }
        public List<CustomerPaymentDto> CustomerPayments { get; set; } = new();
        public List<SupplierPaymentDto> SupplierPayments { get; set; } = new();
        public List<tbl_PaymentMethods> PaymentMethods { get; set; } = new();

        public List<tbl_JobEvaluations> JobEvaluations {  get; set; } = new();

        public class JobDto
        {
            public int sys_jobId { get; set; }
            public int sys_supplierId { get; set; }
            public string SupplierName { get; set; }
            public string StageName { get; set; }
            public DateOnly sys_startDate { get; set; }
            public DateOnly sys_endDate { get; set; }
            public string Status { get; set; }
        }

        public class CustomerPaymentDto
        {
            public int sys_incomeId { get; set; }
            public DateOnly sys_date { get; set; }
            public string InvoiceNumber { get; set; }
            public double sys_amount { get; set; }
            public string sys_paymentMethod { get; set; }
            public string sys_notes { get; set; }
        }

        public class SupplierPaymentDto
        {
            public int sys_jobId { get; set; }
            public int sys_supplierId { get; set; }
            public string SupplierName { get; set; }
            public DateOnly sys_date { get; set; }
            public double sys_amountPaid { get; set; }
            public double sys_balanceDue { get; set; }
            public string sys_notes { get; set; }
        }


        public async Task OnGetAsync()
        {
            try
            {
                // Load grave and related info
                Grave = await _context.tbl_Graves.FindAsync(id);
                Section = await _context.tbl_Sections.FindAsync(Grave.sys_sectionId);
                Stage = await _context.tbl_DevelopmentStages.FindAsync(Grave.sys_stageId);

                //get job evaluations join evaluation parameters
                JobEvaluations = await _context.tbl_JobEvaluations
                    .Where(je => _context.tbl_Jobs.Any(j => j.sys_jobId == je.sys_jobId && j.sys_graveId == id))
                    .Include(je => je.tbl_jobEvaluationParameters)
                    .ToListAsync();



                // Allocation and Deceased
                Allocation = await _context.tbl_GraveAllocations.FirstOrDefaultAsync(a => a.sys_graveId == id);
                if (Allocation != null)
                {
                    Deceased = await _context.tbl_Deceased.FindAsync(Allocation.sys_deceasedId);

                    
                    // Check if payment exists
                    AllocationPendingPayment = !await _context.tbl_IncomePayments.AnyAsync(p => p.sys_invoiceId == Allocation.sys_allocationId);
                }

                // Reservation + Customer + Beneficiaries
                Reservation = await _context.tbl_GraveReservations.FirstOrDefaultAsync(r => r.sys_graveId == id);
                if (Reservation != null)
                {
                    Customer = await _context.tbl_Customers.FindAsync(Reservation.sys_customerId);
                    Beneficiaries = await _context.tbl_Beneficiaries.Where(b => b.sys_reservationId == Reservation.sys_reservationId).ToListAsync();
                }

                // Jobs for this grave
                Jobs = await _context.tbl_Jobs
                    .Where(j => j.sys_graveId == id)
                    .Select(j => new JobDto
                    {
                        sys_jobId = j.sys_jobId,
                        sys_supplierId = j.sys_supplierId,
                        SupplierName = _context.tbl_Suppliers.FirstOrDefault(s => s.sys_supplierId == j.sys_supplierId).sys_supplierName,
                        StageName = _context.tbl_DevelopmentStages.FirstOrDefault(st => st.sys_stageId == j.sys_stageId).sys_stageName,
                        sys_startDate = j.sys_startDate,
                        sys_endDate = j.sys_endDate,
                        Status = j.sys_jobStatus
                    }).ToListAsync();

                // Customer Payments for this grave
                CustomerPayments = new List<CustomerPaymentDto>();

                if (Customer != null)
                {
                    CustomerPayments = await (from payment in _context.tbl_IncomePayments
                                              join invoice in _context.tbl_Invoices
                                              on payment.sys_invoiceId equals invoice.sys_invoiceId
                                              where invoice.sys_customerId == Customer.sys_customerId
                                              select new CustomerPaymentDto
                                              {
                                                  sys_incomeId = payment.sys_incomeId,
                                                  sys_date = payment.sys_date,
                                                  InvoiceNumber = invoice.sys_invoiceNumber,
                                                  sys_amount = payment.sys_amount,
                                                  sys_paymentMethod = payment.sys_paymentMethod,
                                                  sys_notes = payment.sys_notes
                                              }).ToListAsync();
                }


                // Supplier Payments for this grave (expenses linked to jobs)


                // group supplier payments per job
                // get all job ids for this grave
                var jobIds = await _context.tbl_Jobs
                    .Where(j => j.sys_graveId == Grave.sys_graveId)
                    .Select(j => j.sys_jobId)
                    .ToListAsync();

                // load raw payments
                var rawPayments = await _context.tbl_Expenses
                    .Where(e => jobIds.Contains(e.sys_jobId))
                    .ToListAsync();

                // group in memory so string.Join won’t break
                var grouped = rawPayments
                    .GroupBy(e => new { e.sys_jobId, e.sys_supplierId })
                    .Select(g => new SupplierPaymentDto
                    {
                        sys_jobId = g.Key.sys_jobId,
                        sys_supplierId = g.Key.sys_supplierId,
                        sys_date = g.Max(x => x.sys_date), // most recent payment date
                        sys_amountPaid = g.Sum(x => x.sys_amountPaid),
                        sys_balanceDue = g.Sum(x => x.sys_balanceDue),
                        sys_notes = string.Join("; ", g.Select(x => x.sys_notes)),
                        SupplierName = _context.tbl_Suppliers
                            .Where(s => s.sys_supplierId == g.Key.sys_supplierId)
                            .Select(s => s.sys_supplierName)
                            .FirstOrDefault()
                    })
                    .ToList();

                SupplierPayments = grouped;

                // Payment Methods
                PaymentMethods = await _context.tbl_PaymentMethods.ToListAsync();
            }

            catch (Exception ex)
            {
                // Log the exception (you can use a logging framework here)
               TempData["ErrorMessage"] = "An error occurred while loading the grave details.";
                _appLogger.LogActivityAsync(_userManager.GetUserName(User), "GraveDetails", "OnGetAsync"+ex.Message);
            }

        }

        // Allocate Grave
        public async Task<IActionResult> OnPostAllocateGraveAsync(int graveId, string deceasedFullName, DateOnly deceasedDOB,
            DateOnly deceasedDOD, string deceasedGender, string nationality, string maritalStatus, string nameOfSpouse,
            string nextOfKinFullName, string nextOfKinAddress, string nextOfKinEmail, string nextOfKinPhone,
            string paymentBy, DateTime burialDateTime, string repatriationSource)
        {
            try
            {
                if (graveId == 0) return BadRequest();

                // Create Deceased
                var deceased = new tbl_Deceased
                {
                    sys_deceasedFullName = deceasedFullName,
                    sys_deceasedDOB = deceasedDOB,
                    sys_deceasedDOD = deceasedDOD,
                    sys_deceasedGender = deceasedGender,
                    sys_deceasedNationality = nationality,
                    sys_deceasedMaritalStatus = maritalStatus,
                    sys_deceasedNameOfSpouse = nameOfSpouse ?? "n/a",
                    sys_deceasedNextOfKinName = nextOfKinFullName ?? "n/a",
                    sys_deceasedNextOfKinAddress = nextOfKinAddress ?? "n/a",
                    sys_deceasedNextOfKinEmail = nextOfKinEmail ?? "n/a",
                    sys_deceasedNextOfKinPhone = nextOfKinPhone ?? "n/a"

                };
                _context.tbl_Deceased.Add(deceased);
                await _context.SaveChangesAsync();

                // Create Customer
                var customer = new tbl_Customers { sys_paymentMadeBy = paymentBy };
                _context.tbl_Customers.Add(customer);
                await _context.SaveChangesAsync();

                // Create Invoice
                var invoice = new tbl_Invoices
                {

                    sys_invoiceDate = DateOnly.FromDateTime(DateTime.Now),
                    sys_invoiceNumber = $"INV-{DateTime.Now.Ticks}",
                    sys_customerId = customer.sys_customerId,
                    sys_invoiceDueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(7))
                };
                _context.tbl_Invoices.Add(invoice);
                await _context.SaveChangesAsync();


                //get the grave

                var grave = await _context.tbl_Graves.FindAsync(graveId);
                // Create Invoice Item

                var invoiceItem = new tbl_InvoiceItems
                {
                    sys_invoiceId = invoice.sys_invoiceId,
                    sys_itemName = $"Grave Allocation - Grave ID {grave.sys_plotNumber}",
                    sys_itemCost = grave.sys_price
                };

                // Create Allocation
                var allocation = new tbl_GraveAllocations
                {
                    sys_graveId = graveId,
                    sys_deceasedId = deceased.sys_deceasedId,
                    sys_customerId = customer.sys_customerId,
                    sys_allocatedOn = DateOnly.FromDateTime(DateTime.Now),
                    sys_repatriationSource = repatriationSource ?? "n/a",
                    sys_dateTimeOfBurial = burialDateTime
                };
                _context.tbl_GraveAllocations.Add(allocation);
                await _context.SaveChangesAsync();

                // Update Grave status
                grave.sys_graveSaleStatus = "Occupied";
                _context.tbl_Graves.Update(grave);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Grave allocated successfully.";
                _appLogger.LogActivityAsync(_userManager.GetUserName(User), "GraveDetails", $"OnPostAllocateGraveAsync - Grave ID {graveId} allocated.");
                return RedirectToPage(new { id = graveId });
            }
            catch (Exception ex)
            {
                // Log the exception (you can use a logging framework here)
                TempData["ErrorMessage"] = "An error occurred while allocating the grave.";
                _appLogger.LogActivityAsync(_userManager.GetUserName(User), "GraveDetails", "OnPostAllocateGraveAsync" + ex.Message);
                return RedirectToPage(new { id = graveId });

            }
        }


        public async Task<JsonResult> OnGetAvailableBurialSlots(DateTime burialDate)
        {
            try
            {
                // Load existing burials
                var existing = await _context.tbl_GraveAllocations
                    .Where(a => a.sys_dateTimeOfBurial.Date == burialDate.Date)
                    .Select(a => a.sys_dateTimeOfBurial)
                    .OrderBy(a => a)
                    .ToListAsync();

                // Max per day
                if (existing.Count >= Globals.maxBurialsAllowed)
                {
                    return new JsonResult(new { slots = new List<string>(), message = "Max burials reached for this day." });
                }

                // Build slots based on global period
                DateTime start = burialDate.Date.Add(Globals.BurialStartTime.ToTimeSpan());
                DateTime end = burialDate.Date.Add(Globals.BurialEndTime.ToTimeSpan());

                var slots = new List<string>();

                for (DateTime t = start; t <= end; t = t.AddMinutes(Globals.minMinutesApart))
                {
                    bool clash = existing.Any(e => Math.Abs((e - t).TotalMinutes) < Globals.minMinutesApart);
                    if (!clash)
                    {
                        slots.Add(t.ToString("yyyy-MM-ddTHH:mm"));
                    }
                }

                return new JsonResult(new { slots, message = slots.Count == 0 ? "No valid slots left." : "" });
            }
            catch (Exception ex)
            {
                // Log the exception (you can use a logging framework here)
                _appLogger.LogActivityAsync(_userManager.GetUserName(User), "GraveDetails", "OnGetAvailableBurialSlots" + ex.Message);
                return new JsonResult(new { slots = new List<string>(), message = "Error calculating slots." });
            }


        }



        // Create Reservation
        public async Task<IActionResult> OnPostCreateReservationAsync(int graveId, string customerName, string beneficiaries)
        {
            var customer = new tbl_Customers { sys_paymentMadeBy = customerName };
            _context.tbl_Customers.Add(customer);
            await _context.SaveChangesAsync();

            var reservation = new tbl_GraveReservations
            {
                sys_graveId = graveId,
                sys_customerId = customer.sys_customerId
            };
            _context.tbl_GraveReservations.Add(reservation);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(beneficiaries))
            {
                var lines = beneficiaries.Split('\n');
                foreach (var line in lines)
                {
                    var parts = line.Split('|');
                    if (parts.Length == 3)
                    {
                        var b = new tbl_Beneficiaries
                        {
                            sys_benefitFullName = parts[0],
                            sys_benefitDOB = DateOnly.Parse(parts[1]),
                            sys_benefitRelationship = parts[2],
                            sys_reservationId = reservation.sys_reservationId
                        };
                        _context.tbl_Beneficiaries.Add(b);
                    }
                }
                await _context.SaveChangesAsync();
            }

            return RedirectToPage(new { id = graveId });
        }

        // Supplier Payment
        public async Task<IActionResult> OnPostAddSupplierPaymentAsync(int jobId, int supplierId, double amountDue, double amountPaid, double balanceDue, int paymentMethodId, string notes)
        {
            var expense = new tbl_Expenses
            {
                sys_jobId = jobId,
                sys_supplierId = supplierId,
                sys_amountDue = amountDue,
                sys_amountPaid = amountPaid,
                sys_balanceDue = balanceDue,
                sys_date = DateOnly.FromDateTime(DateTime.Now),
                sys_paymentMethodId = paymentMethodId,
                sys_notes = notes
            };
            _context.tbl_Expenses.Add(expense);
            await _context.SaveChangesAsync();
            return RedirectToPage(new { id = id });
        }

        // Allocation Payment
        public async Task<IActionResult> OnPostAddAllocationPaymentAsync(int allocationId, double amountPaid, int paymentMethodId, string notes)
        {
            var allocation = await _context.tbl_GraveAllocations.FindAsync(allocationId);
            if (allocation == null) return BadRequest();

            var invoice = await _context.tbl_Invoices.FirstOrDefaultAsync(i => i.sys_customerId == allocation.sys_customerId);
            if (invoice == null) return BadRequest();

            var payment = new tbl_IncomePayments
            {
                sys_invoiceId = invoice.sys_invoiceId,
                sys_date = DateOnly.FromDateTime(DateTime.Now),
                sys_amount = amountPaid,
                sys_paymentMethod = _context.tbl_PaymentMethods.FirstOrDefault(pm => pm.sys_paymentMethodId == paymentMethodId)?.sys_paymentMethodName,
                sys_notes = notes
            };
            _context.tbl_IncomePayments.Add(payment);
            await _context.SaveChangesAsync();
            return RedirectToPage(new { id = id });
        }
    }

   
}
