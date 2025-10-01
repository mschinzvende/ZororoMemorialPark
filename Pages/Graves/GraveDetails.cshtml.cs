using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;
using ZororoMemorialPark.Data;
using ZororoMemorialPark.Data.Models;
using ZororoMemorialPark.Pages.Jobs;
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
        public List<tbl_Beneficiaries> Beneficiaries { get; set; } = new();
        
        public List<CustomerPaymentDto> AllocationPayments { get; set; } = new();
        public List<CustomerPaymentDto> ReservationPayments { get; set; } = new();
        
        public List<tbl_PaymentMethods> PaymentMethods { get; set; } = new();

        public List<tbl_JobEvaluations> JobEvaluations {  get; set; } = new();





        // Dropdown lists (always initialized)
        public List<tbl_Graves> Graves { get; set; } = new();
        public List<tbl_Suppliers> Suppliers { get; set; } = new();
        public List<tbl_DevelopmentStages> Stages { get; set; } = new();
        
        public List<tbl_jobEvaluationParameters> JobParameters { get; set; } = new();

        public List<tbl_Sections> Sections { get; set; } = new();

        // Job list view model with balances (safe to enumerate in Razor)
        public List<JobViewModel> JobList { get; set; } = new();

        [BindProperty] public tbl_Expenses Expense { get; set; } = new();
        [BindProperty] public tbl_JobEvaluations JobEvaluation { get; set; } = new();

        public double TotalPaidAllocation { get; set; }
        public double TotalPaidReservation { get; set; }
        public double AllocationBalance { get; set; }
        public double ReservationBalance { get; set; }
        public string AllocationPaymentStatus { get; set; }
        public string ReservationPaymentStatus { get; set; }

        
        public class CustomerPaymentDto
        {
            public int sys_incomeId { get; set; }
            public DateOnly sys_date { get; set; }
            public string InvoiceNumber { get; set; }
            public double sys_amount { get; set; }
            public string sys_paymentMethod { get; set; }
            public string sys_notes { get; set; }
        }

        // DTO for returning payments in JSON
        public class PaymentDto
        {
            public DateOnly PaymentDate { get; set; }
            public double AmountPaid { get; set; }
            public double AmountDue { get; set; }
            public double Balance { get; set; }
        }
        // -- lightweight view model used by the razor page --
        public class JobViewModel
        {
            public int JobId { get; set; }
            public int GraveID { get; set; }
            public int SupplierID { get; set; }

            public int SectionID { get; set; }
            public int StageID { get; set; }

            public int EvaluationParameterID { get; set; }

            public string GravePlot { get; set; } = string.Empty;
            public string SupplierName { get; set; } = string.Empty;
            public string StageName { get; set; } = string.Empty;
            public string JobStatus { get; set; } = string.Empty;
            public DateOnly? StartDate { get; set; }
            public DateOnly? EndDate { get; set; }
            public double Balance { get; set; }
        }

        public async Task OnGetAsync()
        {
            try
            {
                await LoadDropdownsAsync();
                // Load grave and related info
                Grave = await _context.tbl_Graves.FindAsync(id);

                Section = await _context.tbl_Sections.FindAsync(Grave.sys_sectionId);
                Stage = await _context.tbl_DevelopmentStages.FindAsync(Grave.sys_stageId);

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

                // Payments from Reservation invoices
                if (Reservation != null)
                {
                    ReservationPayments = await (
                        from gri in _context.tbl_GraveReservationInvoices
                        join inv in _context.tbl_Invoices on gri.sys_invoiceId equals inv.sys_invoiceId
                        join pay in _context.tbl_IncomePayments on inv.sys_invoiceId equals pay.sys_invoiceId
                        where gri.sys_reservationId == Reservation.sys_reservationId
                        select new CustomerPaymentDto
                        {
                            sys_incomeId = pay.sys_incomeId,
                            sys_date = pay.sys_date,
                            InvoiceNumber = inv.sys_invoiceNumber,
                            sys_amount = pay.sys_amount,
                            sys_paymentMethod = pay.sys_paymentMethod,
                            sys_notes = pay.sys_notes
                        }).ToListAsync();

                    
                }

                // Payments from Allocation invoices
                if (Allocation != null)
                {
                    AllocationPayments = await (
                        from gai in _context.tbl_GraveAllocationInvoices
                        join inv in _context.tbl_Invoices on gai.sys_invoiceId equals inv.sys_invoiceId
                        join pay in _context.tbl_IncomePayments on inv.sys_invoiceId equals pay.sys_invoiceId
                        where gai.sys_allocationId == Allocation.sys_allocationId
                        select new CustomerPaymentDto
                        {
                            sys_incomeId = pay.sys_incomeId,
                            sys_date = pay.sys_date,
                            InvoiceNumber = inv.sys_invoiceNumber,
                            sys_amount = pay.sys_amount,
                            sys_paymentMethod = pay.sys_paymentMethod,
                            sys_notes = pay.sys_notes
                        }).ToListAsync();

                }


                //Calculate totals for allocation
                TotalPaidAllocation = AllocationPayments.Sum(CustomerPaymentDto => CustomerPaymentDto.sys_amount );
                AllocationBalance = (Grave?.sys_price ?? 0) - TotalPaidAllocation;

                if (AllocationBalance <= 0)
                {
                    AllocationPaymentStatus = "Fully Paid";
                    AllocationBalance = 0; // avoid showing negative
                }
                else if (TotalPaidAllocation > 0)
                {
                    AllocationPaymentStatus = "Partially Paid";
                }
                else
                {
                    AllocationPaymentStatus = "Unpaid";
                }

                //Calculate totals for reservations
                TotalPaidReservation = ReservationPayments.Sum(CustomerPaymentDto => CustomerPaymentDto.sys_amount);
                ReservationBalance = (Grave?.sys_price ?? 0) - TotalPaidReservation;

                if (ReservationBalance <= 0)
                {
                    ReservationPaymentStatus = "Fully Paid";
                    ReservationBalance = 0; // avoid showing negative
                }
                else if (TotalPaidReservation > 0)
                {
                    ReservationPaymentStatus = "Partially Paid";
                }
                else
                {
                    ReservationPaymentStatus = "Unpaid";
                }


                // Base query for a specific grave
                var baseQuery = _context.tbl_Jobs
                    .Include(j => j.tbl_Grave)
                    .Include(j => j.tbl_Supplier)
                    .Include(j => j.tbl_DevelopmentStage)
                    .Where(j => j.sys_graveId == id)   // ?? filter by graveId
                    .OrderByDescending(j => j.sys_jobId)
                    .AsNoTracking();

                // Fetch jobs for this grave
                var graveJobs = await baseQuery.ToListAsync();

                // Compute balances just for these jobs
                var jobIds = graveJobs.Select(j => j.sys_jobId).ToList();
                var balancesDict = new Dictionary<int, double>();
                if (jobIds.Count > 0)
                {
                    balancesDict = await _context.tbl_Expenses
                        .Where(e => jobIds.Contains(e.sys_jobId))
                        .GroupBy(e => e.sys_jobId)
                        .Select(g => new
                        {
                            JobId = g.Key,
                            Balance = g.Sum(e => (double?)e.sys_amountDue - (double?)e.sys_amountPaid) ?? 0.0
                        })
                        .ToDictionaryAsync(x => x.JobId, x => x.Balance);
                }

                // Build the view model list (only jobs linked to this grave)
                JobList = graveJobs.Select(j => new JobViewModel
                {
                    JobId = j.sys_jobId,
                    GravePlot = j.tbl_Grave?.sys_plotNumber ?? string.Empty,
                    SupplierName = j.tbl_Supplier?.sys_supplierName ?? string.Empty,
                    StageName = j.tbl_DevelopmentStage?.sys_stageName ?? string.Empty,
                    StartDate = j.sys_startDate,
                    EndDate = j.sys_endDate,

                    GraveID = j.tbl_Grave?.sys_graveId ?? 0,
                    SupplierID = j.tbl_Supplier?.sys_supplierId ?? 0,
                    StageID = j.tbl_DevelopmentStage?.sys_stageId ?? 0,
                    SectionID = j.tbl_Grave != null ? j.tbl_Grave.sys_sectionId : 0,
                    JobStatus = j.sys_jobStatus ?? string.Empty,
                    Balance = _context.tbl_Expenses
                        .Where(e => e.sys_jobId == j.sys_jobId)
                        .OrderByDescending(e => e.sys_expenseId)
                        .Select(e => e.sys_balanceDue)
                        .FirstOrDefault()

                }).ToList();


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

                //get the grave

                Grave = await _context.tbl_Graves.FindAsync(graveId);

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

                // Check if there is an existing reservation for this grave
                Reservation = await _context.tbl_GraveReservations.FirstOrDefaultAsync(r => r.sys_graveId == graveId);



                if (Reservation == null) //only do this if there is no reservation already
                {
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



                    // Create Invoice Item

                    var invoiceItem = new tbl_InvoiceItems
                    {
                        sys_invoiceId = invoice.sys_invoiceId,
                        sys_itemName = $"Grave Allocation - Grave ID {Grave.sys_plotNumber}",
                        sys_itemCost = Grave.sys_price
                    };
                    _context.tbl_InvoiceItems.Add(invoiceItem);
                    await _context.SaveChangesAsync();

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

                    //Link grave allocation to invoice via the linker table

                    var linker = new tbl_GraveAllocationInvoices
                    {
                        sys_allocationId = allocation.sys_allocationId,
                        sys_invoiceId = invoice.sys_invoiceId
                    };
                    _context.tbl_GraveAllocationInvoices.Add(linker);
                    await _context.SaveChangesAsync();

                    // Update Grave status
                    Grave.sys_graveSaleStatus = "Occupied";
                    _context.tbl_Graves.Update(Grave);
                    await _context.SaveChangesAsync();
                }

                else
                {
                    // Use existing reservation to get customer
                    var customer = await _context.tbl_Customers.FindAsync(Reservation.sys_customerId);
                    if (customer == null) return BadRequest("Customer not found for existing reservation.");

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
                    Grave.sys_graveSaleStatus = "Occupied";
                    _context.tbl_Graves.Update(Grave);
                    await _context.SaveChangesAsync();
                }
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
        public async Task<IActionResult> OnPostCreateReservationAsync(int graveId, string customerName, 
            string beneficiaryFullName, DateOnly beneficiaryDOB, string beneficiaryRelationship )
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

            
           var b = new tbl_Beneficiaries
             {
                  sys_benefitFullName = beneficiaryFullName ,
                  sys_benefitDOB = beneficiaryDOB,
                  sys_benefitRelationship = beneficiaryRelationship,
                  sys_reservationId = reservation.sys_reservationId
             };
              _context.tbl_Beneficiaries.Add(b);
             await _context.SaveChangesAsync();


            //Create a reservation invoice
            
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
                sys_itemName = $"Grave Reservation - Grave ID {grave.sys_plotNumber}",
                sys_itemCost = grave.sys_price
            };
            _context.tbl_InvoiceItems.Add(invoiceItem);
            await _context.SaveChangesAsync();


            //Link grave reservation to invoice via the linker table

            var linker = new tbl_GraveReservationInvoices
            {
                sys_reservationId = reservation.sys_reservationId,
                sys_invoiceId = invoice.sys_invoiceId
            };
            _context.tbl_GraveReservationInvoices.Add(linker);
            await _context.SaveChangesAsync();

            // Update Grave status
            grave.sys_graveSaleStatus = "Reserved";
            _context.tbl_Graves.Update(grave);
            await _context.SaveChangesAsync();


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

        //Receipt Validation Script

        public async Task<(bool Success, string Message)> ValidateReceiptNumber(int agentId, int receiptNumber)
        {
            var book = await _context.tbl_ReceiptBooks
                .FirstOrDefaultAsync(b => b.sys_agentId == agentId && b.sys_isClosed!= true);

            if (book == null)
                return (false, "Receipt book not found for this agent.");

            if (book.sys_isClosed)
                return (false, "This receipt book is closed.");

            if (receiptNumber < book.sys_startNumber || receiptNumber > book.sys_endNumber)
                return (false, "Receipt number not in the valid range for this book.");

            if (receiptNumber != book.sys_currentNumber + 1)
                return (false, $"Invalid sequence. Next expected receipt is {book.sys_currentNumber + 1}.");

            
            return (true, "Receipt Number is Valid.");
        }



        // Allocation Payment
        public async Task<IActionResult> OnPostAddAllocationPaymentAsync(int allocationId, double amountPaid, int paymentMethodId, int receiptNumber, string notes)
        {

            //Validate Receipt Number

            //get agent id

            //var currentUser = await _userManager.GetUserId(User);

            var theUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

            var theAgent = await _context.tbl_Agents.FirstOrDefaultAsync(a => a.UserId == _userManager.GetUserId(User));

            if (await _userManager.IsInRoleAsync(theUser, "Agent"))
            {
                var ValidationResult = ValidateReceiptNumber(theAgent.sys_agentId, receiptNumber);


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
                    sys_notes = notes ?? "n/a"
                };
                _context.tbl_IncomePayments.Add(payment);
                await _context.SaveChangesAsync();
                return RedirectToPage(new { id = id });
            }

            else
            {
                TempData["ErrorMessage"] = "Only Agents can capture payments";
                return RedirectToPage(new { id = id });
            }
        }

        // Record Reservation Payment
        public async Task<IActionResult> OnPostRecordReservationPaymentAsync(int reservationId, double amountPaid, int paymentMethodId, string notes)
        {
            var reservation = await _context.tbl_GraveReservations.FindAsync(reservationId);
            if (reservation == null) return BadRequest();

            var invoice = await _context.tbl_Invoices.FirstOrDefaultAsync(i => i.sys_customerId == reservation.sys_customerId);
            if (invoice == null) return BadRequest();

            var payment = new tbl_IncomePayments
            {
                sys_invoiceId = invoice.sys_invoiceId,
                sys_date = DateOnly.FromDateTime(DateTime.Now),
                sys_amount = amountPaid,
                sys_paymentMethod = _context.tbl_PaymentMethods.FirstOrDefault(pm => pm.sys_paymentMethodId == paymentMethodId)?.sys_paymentMethodName,
                sys_notes = notes ?? "n/a"
            };
            _context.tbl_IncomePayments.Add(payment);
            await _context.SaveChangesAsync();
            return RedirectToPage(new { id = id });
        }



        
        ///////////////////////////////////////////////////////////
        //Job details Methods/////

        private async Task LoadDropdownsAsync()
        {
            Graves = await _context.tbl_Graves.AsNoTracking().ToListAsync() ?? new();
            Suppliers = await _context.tbl_Suppliers.AsNoTracking().ToListAsync() ?? new();
            Stages = await _context.tbl_DevelopmentStages.AsNoTracking().ToListAsync() ?? new();
            PaymentMethods = await _context.tbl_PaymentMethods.AsNoTracking().ToListAsync() ?? new();
            JobParameters = await _context.tbl_jobEvaluationParameters.AsNoTracking().ToListAsync() ?? new();
            Sections = await _context.tbl_Sections.AsNoTracking().ToListAsync() ?? new();
        }


        // AJAX endpoint
        public JsonResult OnGetGetGravesBySection(int sectionId)
        {
            var graves = _context.tbl_Graves
                .Where(g => g.sys_sectionId == sectionId)
                .Select(g => new { g.sys_graveId, g.sys_plotNumber })
                .ToList();

            return new JsonResult(graves);
        }

        // --- Create/Edit/Delete/Payment/Evaluation handlers ---
        // Note: On failure we reload page state so the view can render safely.
        public async Task<IActionResult> OnPostCreateAsync()
        {
            try
            {
                int graveId = int.Parse(Request.Form["graveId"]);

                var submittedJob = new tbl_Jobs
                {
                    sys_graveId = graveId, // always link to this grave
                    sys_supplierId = Request.Form["sys_supplierId"].ToString() != "" ? int.Parse(Request.Form["sys_supplierId"]) : 0,
                    sys_stageId = Request.Form["sys_stageId"].ToString() != "" ? int.Parse(Request.Form["sys_stageId"]) : 0,
                    sys_startDate = Request.Form["sys_startDate"].ToString() != ""
                                        ? DateOnly.Parse(Request.Form["sys_startDate"])
                                        : DateOnly.FromDateTime(DateTime.Now),
                    sys_endDate = Request.Form["sys_endDate"].ToString() != ""
                                        ? DateOnly.Parse(Request.Form["sys_endDate"])
                                        : DateOnly.FromDateTime(DateTime.Now),
                    sys_jobStatus = Request.Form["sys_status"].ToString()
                };

                // Check for duplicates (per grave + stage + supplier)
                var existingJob = await _context.tbl_Jobs
                    .FirstOrDefaultAsync(j => j.sys_graveId == submittedJob.sys_graveId
                                           && j.sys_stageId == submittedJob.sys_stageId
                                           && j.sys_supplierId == submittedJob.sys_supplierId);

                if (existingJob != null)
                {
                    TempData["ErrorMessage"] = "A job with the same Grave, Stage, and Supplier already exists.";
                    await LoadDropdownsAsync();
                    return RedirectToPage(new { graveId });
                }

                // Basic validation
                if (submittedJob.sys_supplierId == 0 || submittedJob.sys_stageId == 0)
                {
                    TempData["ErrorMessage"] = "Supplier and Stage are required fields.";
                    await LoadDropdownsAsync();
                    return RedirectToPage(new { graveId });
                }

                if (submittedJob.sys_startDate > submittedJob.sys_endDate)
                {
                    TempData["ErrorMessage"] = "End Date must be greater than or equal to Start Date.";
                    return RedirectToPage(new { graveId });
                }

                _context.tbl_Jobs.Add(submittedJob);
                await _context.SaveChangesAsync();

                // Update grave stage if job is completed
                if (submittedJob.sys_jobStatus == "Complete")
                {
                    var graveToUpdate = await _context.tbl_Graves.FindAsync(submittedJob.sys_graveId);
                    if (graveToUpdate != null)
                    {
                        graveToUpdate.sys_stageId = submittedJob.sys_stageId;
                        _context.tbl_Graves.Update(graveToUpdate);
                        await _context.SaveChangesAsync();
                    }
                }

                TempData["SuccessMessage"] = "Job created successfully.";
                await _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Create", $"Created Job ID {submittedJob.sys_jobId}");

                return RedirectToPage(new { graveId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                await LoadDropdownsAsync();
                await _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Error", $"Error in OnPostCreateAsync: {ex.Message}");
                return RedirectToPage();
            }
        }


        public async Task<IActionResult> OnPostEditAsync()
        {
            try
            {
                // Read values from form
                if (!int.TryParse(Request.Form["sys_jobId"], out int jobId) ||
                    !int.TryParse(Request.Form["sys_graveId"], out int graveId) ||
                    !int.TryParse(Request.Form["sys_supplierId"], out int supplierId) ||
                    !int.TryParse(Request.Form["sys_stageId"], out int stageId) ||

                    !DateOnly.TryParse(Request.Form["sys_startDate"], out DateOnly startDate) ||
                    !DateOnly.TryParse(Request.Form["sys_endDate"], out DateOnly endDate))
                {
                    TempData["ErrorMessage"] = "Invalid form input. Please check your values.";
                    return RedirectToPage();
                }

                // Fetch job
                var job = await _context.tbl_Jobs.FirstOrDefaultAsync(j => j.sys_jobId == jobId);
                if (job == null)
                {
                    TempData["ErrorMessage"] = "Job not found.";
                    return RedirectToPage();
                }

                // Validation rules
                if (startDate > endDate)
                {
                    TempData["ErrorMessage"] = "End Date must be greater than or equal to Start Date.";
                    return RedirectToPage();
                }

                var grave = await _context.tbl_Graves.FirstOrDefaultAsync(g => g.sys_graveId == graveId);
                if (grave == null)
                {
                    TempData["ErrorMessage"] = "Selected grave does not exist.";
                    return RedirectToPage();
                }

                // Optional: ensure grave belongs to a section if your design requires
                var sectionId = grave.sys_sectionId;
                if (sectionId <= 0)
                {
                    TempData["ErrorMessage"] = "Selected grave is not assigned to a valid section.";
                    return RedirectToPage();
                }

                var supplierExists = await _context.tbl_Suppliers.AnyAsync(s => s.sys_supplierId == supplierId);
                if (!supplierExists)
                {
                    TempData["ErrorMessage"] = "Selected supplier does not exist.";
                    return RedirectToPage();
                }

                var stageExists = await _context.tbl_DevelopmentStages.AnyAsync(s => s.sys_stageId == stageId);
                if (!stageExists)
                {
                    TempData["ErrorMessage"] = "Selected stage does not exist.";
                    return RedirectToPage();
                }

                // Update job
                job.sys_graveId = graveId;
                job.sys_supplierId = supplierId;
                job.sys_stageId = stageId;
                job.sys_startDate = startDate;
                job.sys_endDate = endDate;
                job.sys_jobStatus = Request.Form["sys_status"];


                await _context.SaveChangesAsync();//check if job status is "Completed" and if so, update the grave status to "Developed"
                if (job.sys_jobStatus == "Complete")
                {
                    var graveToUpdate = await _context.tbl_Graves.FindAsync(job.sys_graveId);
                    if (graveToUpdate != null)
                    {
                        graveToUpdate.sys_stageId = job.sys_stageId;
                        _context.tbl_Graves.Update(graveToUpdate);
                        await _context.SaveChangesAsync();
                    }
                }


                TempData["SuccessMessage"] = "Job updated successfully!";
                _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Edit", $"Edited Job ID {job.sys_jobId}");
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating job: {ex.Message}";
                _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Error", $"Error in OnPostEditAsync: {ex.Message}");
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            try
            {
                int jobid = Int32.Parse(Request.Form["deleteJobId"]);
                var job = await _context.tbl_Jobs.FindAsync(jobid);
                if (job != null)
                {
                    _context.tbl_Jobs.Remove(job);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Job deleted successfully.";

                    _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Delete", $"Deleted Job ID {job.sys_jobId}");
                }
                else
                {
                    TempData["ErrorMessage"] = "Job not found.";
                }
                return RedirectToPage();
            }

            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting job: {ex.Message}";
                _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Error", $"Error in OnPostDeleteAsync: {ex.Message}");
                return RedirectToPage();
            }


        }

        public async Task<JsonResult> OnGetPaymentsAsync(int jobId)
        {
            var payments = await _context.tbl_Expenses
                .Where(p => p.sys_jobId == jobId)
                .OrderBy(p => p.sys_date)
                .Select(p => new PaymentDto
                {
                    PaymentDate = p.sys_date,
                    AmountPaid = p.sys_amountPaid,
                    AmountDue = p.sys_amountDue,
                    Balance = p.sys_balanceDue
                })
                .ToListAsync();

            return new JsonResult(payments);
        }

        public async Task<IActionResult> OnPostAddPaymentAsync(int sys_jobId, int sys_supplierId,
            double AmountDue, double AmountPaid, DateOnly PaymentDate, int sys_paymentMethodId,
            string PaymentNotes)
        {
            var previousPayments = await _context.tbl_Expenses
                .Where(p => p.sys_jobId == sys_jobId)
                .OrderBy(p => p.sys_date)
                .ToListAsync();

            double totalPaid = previousPayments.Sum(p => p.sys_amountPaid);
            double balance = 0;

            if (previousPayments.Count == 0)
            {
                // First payment: take user-entered AmountDue
                balance = AmountDue - AmountPaid;
            }
            else
            {
                // Subsequent payments: AmountDue always 0
                AmountDue = 0;
                var lastBalance = previousPayments.Last().sys_balanceDue;
                balance = lastBalance - AmountPaid;

                if (balance < 0)
                {
                    TempData["ErrorMessage"] = "Payment exceeds the remaining balance.";
                    await LoadDropdownsAsync();
                    return RedirectToPage();
                }
            }

            if (PaymentNotes == null)
            {
                PaymentNotes = "n/a";
            }

            var payment = new tbl_Expenses
            {
                sys_jobId = sys_jobId,
                sys_amountDue = AmountDue,
                sys_amountPaid = AmountPaid,
                sys_balanceDue = balance,
                sys_date = PaymentDate,
                sys_paymentMethodId = sys_paymentMethodId,
                sys_supplierId = sys_supplierId,
                sys_notes = PaymentNotes
            };

            _context.tbl_Expenses.Add(payment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Payment saved successfully!";
            return RedirectToPage();
        }

        public async Task<JsonResult> OnGetEvaluationParametersAsync(int jobId)
        {
            // Get all parameters
            var parameters = await _context.tbl_jobEvaluationParameters
                .Select(p => new
                {
                    p.sys_jobEvaluationParameterId,
                    p.sys_parameterName
                })
                .ToListAsync();

            // Get existing evaluations for this job
            var existing = await _context.tbl_JobEvaluations
                .Where(e => e.sys_jobId == jobId)
                .ToListAsync();

            // Merge
            var result = parameters.Select(p => new
            {
                sys_jobEvaluationParameterId = p.sys_jobEvaluationParameterId,
                sys_parameterName = p.sys_parameterName,
                sys_jobEvaluation = existing
                    .FirstOrDefault(e => e.sys_jobEvaluationParameterId == p.sys_jobEvaluationParameterId)?.sys_jobEvaluation
            });

            return new JsonResult(result);
        }

        public async Task<IActionResult> OnPostAddEvaluationAsync()
        {
            try
            {
                var jobIdStr = Request.Form["jobId"];
                if (string.IsNullOrEmpty(jobIdStr) || !int.TryParse(jobIdStr, out int jobId))
                {
                    TempData["ErrorMessage"] = "Invalid Job ID.";
                    return RedirectToPage();
                }

                // Loop through all form fields that start with "evaluation_"
                foreach (var key in Request.Form.Keys.Where(k => k.StartsWith("evaluation_")))
                {
                    var paramIdStr = key.Replace("evaluation_", "");
                    if (!int.TryParse(paramIdStr, out int paramId))
                        continue;

                    var evaluationText = Request.Form[key];
                    if (string.IsNullOrWhiteSpace(evaluationText))
                        continue; // skip empty entries

                    // Check if evaluation already exists for this job & parameter
                    var existingEval = await _context.tbl_JobEvaluations
                        .FirstOrDefaultAsync(e => e.sys_jobId == jobId && e.sys_jobEvaluationParameterId == paramId);

                    if (existingEval != null)
                    {
                        // Update
                        existingEval.sys_jobEvaluation = evaluationText;
                        _context.tbl_JobEvaluations.Update(existingEval);
                    }
                    else
                    {
                        // Insert
                        var newEval = new tbl_JobEvaluations
                        {
                            sys_jobId = jobId,
                            sys_jobEvaluationParameterId = paramId,
                            sys_jobEvaluation = evaluationText
                        };
                        _context.tbl_JobEvaluations.Add(newEval);
                    }
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Evaluations saved successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error saving evaluations: {ex.Message}";
            }

            return RedirectToPage();
        }


    }


    // DTO for returning payments in JSON
    public class PaymentDto
    {
        public DateOnly PaymentDate { get; set; }
        public double AmountPaid { get; set; }
        public double AmountDue { get; set; }
        public double Balance { get; set; }
    }
    // -- lightweight view model used by the razor page --
    public class JobViewModel
    {
        public int JobId { get; set; }
        public int GraveID { get; set; }
        public int SupplierID { get; set; }

        public int SectionID { get; set; }
        public int StageID { get; set; }

        public int EvaluationParameterID { get; set; }

        public string GravePlot { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public string StageName { get; set; } = string.Empty;
        public string JobStatus { get; set; } = string.Empty;
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public double Balance { get; set; }
    }
}
