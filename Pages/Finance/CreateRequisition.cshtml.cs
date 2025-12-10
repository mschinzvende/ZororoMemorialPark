using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;
using ZororoMemorialPark.Data;
using ZororoMemorialPark.Data.Models;

namespace ZororoMemorialPark.Pages.Finance
{
    public class CreateRequisitionModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public CreateRequisitionModel(ApplicationDbContext context, UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [BindProperty]
        public HeaderInput Input { get; set; } = new();

        [BindProperty]
        public List<ItemInput> Items { get; set; } = new() { new ItemInput() };

        [BindProperty]
        public IFormFile? Attachment { get; set; }

        [BindProperty]
        [Required]
        public string Approver1UserId { get; set; }

        [BindProperty]
        public string? Approver2UserId { get; set; }

        public List<SelectListItem> RequisitionApproverOptions { get; set; } = new();
        public List<SelectListItem> RequisitionReviewerOptions { get; set; } = new();


        public class HeaderInput
        {
            [Required]
            [MaxLength(200)]
            public string Title { get; set; }

            public string Description { get; set; }
        }

        public class ItemInput
        {
            [Required]
            public string ItemName { get; set; }

            [Range(1, int.MaxValue)]
            public int Quantity { get; set; } = 1;

            [Range(0.01, double.MaxValue)]
            public double CostPerUnit { get; set; }
        }

        public async Task OnGetAsync()
        {
            // TODO: Populate from your users table (Identity or z_Users)
            // For now, keeps list empty so page still compiles.
            var RequisitionApprovers = await _userManager.GetUsersInRoleAsync("RequisitionApprover");
            RequisitionApproverOptions = RequisitionApprovers.Select(u => new SelectListItem
            {
                Value = u.Id,
                Text = u.UserName ?? u.UserName   // adjust to your field
            })
    .ToList();

            var RequisitionReviewers = await _userManager.GetUsersInRoleAsync("RequisitionReviewer");
            RequisitionReviewerOptions = RequisitionReviewers.Select(u => new SelectListItem
            {
                Value = u.Id,
                Text = u.UserName ?? u.UserName   // adjust to your field
            })
    .ToList();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                // Rebuild approver options
                await OnGetAsync();

                return Page();
            }

            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
            {
                ModelState.AddModelError(string.Empty, "Unable to identify current user.");
                await OnGetAsync();
                
                return Page();
            }

            if (Items == null || Items.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Please add at least one item.");
                await OnGetAsync();
                return Page();
            }

            string? attachmentPath = null;

            if (Attachment != null && Attachment.Length > 0)
            {
                var uploadsFolder = Path.Combine("wwwroot", "uploads", "requisitions");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}_{Attachment.FileName}";
                var fullPath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await Attachment.CopyToAsync(stream);
                }

                attachmentPath = $"/uploads/requisitions/{fileName}";
            }

            var header = new tbl_Requisitions
            {
                sys_requestedByUserId = currentUserId,
                sys_requestDate = DateTime.Now,
                sys_title = Input.Title,
                sys_description = Input.Description,
                sys_status = "Pending",
                sys_attachmentPath = attachmentPath,
                sys_approver1UserId = Approver1UserId,
                sys_approver2UserId = Approver2UserId,
                sys_currentApproverOrder = 1
            };

            _context.tbl_Requisitions.Add(header);
            await _context.SaveChangesAsync();

            foreach (var item in Items)
            {
                var total = item.Quantity * item.CostPerUnit;

                var dbItem = new tbl_RequisitionItems
                {
                    sys_requisitionId = header.sys_requisitionId,
                    ItemName = item.ItemName,
                    Quantity = item.Quantity,
                    CostPerUnit = item.CostPerUnit,
                    TotalCost = total,
                    Status = "Pending"
                };

                _context.tbl_RequisitionItems.Add(dbItem);
            }

            await _context.SaveChangesAsync();

            // TODO: Send email to Approver1 (hook into your notification service here)

            TempData["SuccessMessage"] = "Requisition submitted successfully.";
            return RedirectToPage("./RequisitionDetails", new { id = header.sys_requisitionId });
        }
    }
}
