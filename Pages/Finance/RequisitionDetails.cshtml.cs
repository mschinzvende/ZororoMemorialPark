using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZororoMemorialPark.Data;
using ZororoMemorialPark.Data.Models;

namespace ZororoMemorialPark.Pages.Finance
{
    public class RequisitionDetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public RequisitionDetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public tbl_Requisitions Requisition { get; set; }

        public List<ItemVM> Items { get; set; } = new();

        [BindProperty]
        public int ItemId { get; set; }

        [BindProperty]
        public double ApprovedAmount { get; set; }

        [BindProperty]
        public string DecisionNotes { get; set; }

        [BindProperty]
        public string PaymentReference { get; set; }

        public class ItemVM
        {
            public int ItemId { get; set; }
            public string ItemName { get; set; }
            public int Quantity { get; set; }
            public double CostPerUnit { get; set; }
            public double TotalCost { get; set; }
            public string Status { get; set; }
            public double? ApprovedAmount { get; set; }
            public string Approver1Notes { get; set; }
            public string Approver2Notes { get; set; }
        }

        private string CurrentUserId => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var req = await _context.tbl_Requisitions
                .FirstOrDefaultAsync(r => r.sys_requisitionId == id);

            if (req == null)
                return NotFound();

            Requisition = req;

            var itemsQuery = _context.tbl_RequisitionItems
                .Where(i => i.sys_requisitionId == id);

            // If current approver is #2, hide items rejected by approver 1
            if (req.sys_currentApproverOrder == 2)
            {
                itemsQuery = itemsQuery.Where(i => i.Status != "Rejected");
            }

            var items = await itemsQuery.ToListAsync();

            Items = items.Select(i => new ItemVM
            {
                ItemId = i.sys_itemId,
                ItemName = i.ItemName,
                Quantity = i.Quantity,
                CostPerUnit = i.CostPerUnit,
                TotalCost = i.TotalCost,
                Status = i.Status,
                ApprovedAmount = i.ApprovedAmount,
                Approver1Notes = i.Approver1Notes,
                Approver2Notes = i.Approver2Notes
            }).ToList();

            return Page();
        }

        // Approve item for current approver
        public async Task<IActionResult> OnPostApproveItemAsync(int id)
        {
            var req = await _context.tbl_Requisitions.FindAsync(id);
            if (req == null) return NotFound();

            var item = await _context.tbl_RequisitionItems
                .FirstOrDefaultAsync(i => i.sys_itemId == ItemId && i.sys_requisitionId == id);
            if (item == null) return NotFound();

            if (ApprovedAmount <= 0 || ApprovedAmount > item.TotalCost)
            {
                TempData["ErrorMessage"] = "Approved amount must be > 0 and <= item total.";
                return RedirectToPage(new { id });
            }

            // Approver 1
            if (req.sys_currentApproverOrder == 1 &&
                string.Equals(req.sys_approver1UserId, CurrentUserId, StringComparison.OrdinalIgnoreCase))
            {
                item.Status = "Approved";
                item.ApprovedAmount = ApprovedAmount;
                item.Approver1Notes = DecisionNotes;
                item.ReviewedByApprover1 = true;
            }
            // Approver 2
            else if (req.sys_currentApproverOrder == 2 &&
                     !string.IsNullOrEmpty(req.sys_approver2UserId) &&
                     string.Equals(req.sys_approver2UserId, CurrentUserId, StringComparison.OrdinalIgnoreCase))
            {
                item.Status = "Approved";
                item.ApprovedAmount = ApprovedAmount;
                item.Approver2Notes = DecisionNotes;
                item.ReviewedByApprover2 = true;
            }
            else
            {
                TempData["ErrorMessage"] = "You are not allowed to approve this requisition at this stage.";
                return RedirectToPage(new { id });
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Item approved.";
            return RedirectToPage(new { id });
        }

        // Reject item for current approver
        public async Task<IActionResult> OnPostRejectItemAsync(int id)
        {
            var req = await _context.tbl_Requisitions.FindAsync(id);
            if (req == null) return NotFound();

            var item = await _context.tbl_RequisitionItems
                .FirstOrDefaultAsync(i => i.sys_itemId == ItemId && i.sys_requisitionId == id);
            if (item == null) return NotFound();

            if (string.IsNullOrWhiteSpace(DecisionNotes))
            {
                TempData["ErrorMessage"] = "Please provide a reason for rejection.";
                return RedirectToPage(new { id });
            }

            // Approver 1
            if (req.sys_currentApproverOrder == 1 &&
                string.Equals(req.sys_approver1UserId, CurrentUserId, StringComparison.OrdinalIgnoreCase))
            {
                item.Status = "Rejected";
                item.ApprovedAmount = null;
                item.Approver1Notes = DecisionNotes;
                item.ReviewedByApprover1 = true;
            }
            // Approver 2
            else if (req.sys_currentApproverOrder == 2 &&
                     !string.IsNullOrEmpty(req.sys_approver2UserId) &&
                     string.Equals(req.sys_approver2UserId, CurrentUserId, StringComparison.OrdinalIgnoreCase))
            {
                item.Status = "Rejected";
                item.Approver2Notes = DecisionNotes;
                item.ReviewedByApprover2 = true;
            }
            else
            {
                TempData["ErrorMessage"] = "You are not allowed to reject this requisition at this stage.";
                return RedirectToPage(new { id });
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Item rejected.";
            return RedirectToPage(new { id });
        }

        // Approver 1 / 2 finalizes review
        public async Task<IActionResult> OnPostFinalizeReviewAsync(int id)
        {
            var req = await _context.tbl_Requisitions
                .FirstOrDefaultAsync(r => r.sys_requisitionId == id);

            if (req == null) return NotFound();

            var items = await _context.tbl_RequisitionItems
                .Where(i => i.sys_requisitionId == id)
                .ToListAsync();

            // Approver 1 finalizing
            if (req.sys_currentApproverOrder == 1 &&
                string.Equals(req.sys_approver1UserId, CurrentUserId, StringComparison.OrdinalIgnoreCase))
            {
                req.sys_approver1CompletedOn = DateTime.Now;

                if (!string.IsNullOrEmpty(req.sys_approver2UserId))
                {
                    // Move to level 2 (only approved items will be visible there)
                    req.sys_currentApproverOrder = 2;
                    req.sys_status = "PendingLevel2";
                }
                else
                {
                    // Final statuses based on items
                    UpdateHeaderStatusFromItems(req, items);
                    req.sys_currentApproverOrder = null;
                }
            }
            // Approver 2 finalizing
            else if (req.sys_currentApproverOrder == 2 &&
                     !string.IsNullOrEmpty(req.sys_approver2UserId) &&
                     string.Equals(req.sys_approver2UserId, CurrentUserId, StringComparison.OrdinalIgnoreCase))
            {
                req.sys_approver2CompletedOn = DateTime.Now;
                UpdateHeaderStatusFromItems(req, items);
                req.sys_currentApproverOrder = null;
            }
            else
            {
                TempData["ErrorMessage"] = "You are not allowed to finalize this requisition at this stage.";
                return RedirectToPage(new { id });
            }

            await _context.SaveChangesAsync();

            // TODO: send notification to requester about status change

            TempData["SuccessMessage"] = "Requisition review finalized.";
            return RedirectToPage(new { id });
        }

        // Mark as paid
        public async Task<IActionResult> OnPostMarkPaidAsync(int id)
        {
            var req = await _context.tbl_Requisitions
                .FirstOrDefaultAsync(r => r.sys_requisitionId == id);
            if (req == null) return NotFound();

            if (req.sys_status != "Approved" && req.sys_status != "PartiallyApproved")
            {
                TempData["ErrorMessage"] = "Only approved requisitions can be marked as paid.";
                return RedirectToPage(new { id });
            }

            if (string.IsNullOrWhiteSpace(PaymentReference))
            {
                TempData["ErrorMessage"] = "Please provide a payment reference.";
                return RedirectToPage(new { id });
            }

            req.sys_status = "Paid";
            req.sys_paymentReference = PaymentReference;
            req.sys_paymentDate = DateTime.Now;
            req.sys_paidByUserId = CurrentUserId;

            await _context.SaveChangesAsync();

            // TODO: notify requester that requisition has been paid

            TempData["SuccessMessage"] = "Requisition marked as paid.";
            return RedirectToPage(new { id });
        }

        private static void UpdateHeaderStatusFromItems(tbl_Requisitions req, List<tbl_RequisitionItems> items)
        {
            var anyApproved = items.Any(i => i.Status == "Approved");
            var anyRejected = items.Any(i => i.Status == "Rejected");
            var allApproved = items.All(i => i.Status == "Approved");
            var allRejected = items.All(i => i.Status == "Rejected");

            if (allApproved)
                req.sys_status = "Approved";
            else if (allRejected)
                req.sys_status = "Rejected";
            else if (anyApproved && anyRejected)
                req.sys_status = "PartiallyApproved";
            else
                req.sys_status = "Pending";
        }
    }
}
