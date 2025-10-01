using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ZororoMemorialPark.Data;
using ZororoMemorialPark.Data.Models;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace ZororoMemorialPark.Pages.Graves
{
    public class DevelopmentStagesModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IAppLogger _appLogger;

        public DevelopmentStagesModel(ApplicationDbContext context, UserManager<IdentityUser> userManager, IAppLogger appLogger)
        {
            _context = context;
            _userManager = userManager;
            _appLogger = appLogger;
        }

        public IList<tbl_DevelopmentStages> Stages { get; set; } = new List<tbl_DevelopmentStages>();
        public PaginatedList<tbl_DevelopmentStages> PaginatedStages { get; set; }

        public async Task OnGetAsync(int pageIndex = 1)
        {
            var query = _context.tbl_DevelopmentStages.OrderBy(s => s.sys_stageName);
            PaginatedStages = await PaginatedList<tbl_DevelopmentStages>.CreateAsync(query, pageIndex, 5);
            await _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Access", "Accessed Development Stages page");
        }

        // CREATE
        public async Task<IActionResult> OnPostCreateAsync()
        {
            var stageName = Request.Form["stageName"].ToString().Trim();
            var stageOrder = int.TryParse(Request.Form["stageOrder"], out int order) ? order : 0;
            if (string.IsNullOrEmpty(stageName))
            {
                TempData["ErrorMessage"] = "Stage name cannot be empty.";
                return RedirectToPage();
            }

            var exists = await _context.tbl_DevelopmentStages.AnyAsync(s => s.sys_stageName.ToLower() == stageName.ToLower());
            if (exists)
            {
                TempData["ErrorMessage"] = "A stage with this name already exists.";
                return RedirectToPage();
            }

            var orderExists = await _context.tbl_DevelopmentStages.AnyAsync(o=>o.sys_stageOrder ==stageOrder);

            if (orderExists)
            {
                TempData["ErrorMessage"] = "A stage with this order already exists.";
                return RedirectToPage();
            }

            var stage = new tbl_DevelopmentStages { sys_stageName = stageName, sys_stageOrder = stageOrder };
            _context.tbl_DevelopmentStages.Add(stage);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Stage created successfully.";
            await _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Create", $"Created stage: {stageName}");

            return RedirectToPage();
        }

        // EDIT
        public async Task<IActionResult> OnPostEditAsync()
        {
            var idStr = Request.Form["sys_stageId"].ToString();
            var name = Request.Form["sys_stageName"].ToString().Trim();

            var stageOrder = int.TryParse(Request.Form["sys_stageOrder"], out int order) ? order : 0;


            if (!int.TryParse(idStr, out int id))
            {
                TempData["ErrorMessage"] = "Invalid stage ID.";
                return RedirectToPage();
            }
            if (string.IsNullOrEmpty(name))
            {
                TempData["ErrorMessage"] = "Stage name cannot be empty.";
                return RedirectToPage();
            }

            var stage = await _context.tbl_DevelopmentStages.FindAsync(id);
            if (stage == null)
            {
                TempData["ErrorMessage"] = "Stage not found.";
                return RedirectToPage();
            }

            var duplicate = await _context.tbl_DevelopmentStages
                .AnyAsync(s => s.sys_stageName.ToLower() == name.ToLower() && s.sys_stageId != id);
            if (duplicate)
            {
                TempData["ErrorMessage"] = "A stage with this name already exists.";
                return RedirectToPage();
            }

            var orderExists = await _context.tbl_DevelopmentStages.AnyAsync(o => o.sys_stageOrder == stageOrder);

            if (orderExists)
            {
                TempData["ErrorMessage"] = "A stage with this order already exists.";
                return RedirectToPage();
            }

            stage.sys_stageOrder = stageOrder;
            stage.sys_stageName = name;
            _context.tbl_DevelopmentStages.Update(stage);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Stage updated successfully.";
            await _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Edit", $"Edited stage: {name}");

            return RedirectToPage();
        }

        // DELETE
        public async Task<IActionResult> OnPostDeleteAsync()
        {
            var idStr = Request.Form["sys_stageId"].ToString();
            if (!int.TryParse(idStr, out int id))
            {
                TempData["ErrorMessage"] = "Invalid stage ID.";
                return RedirectToPage();
            }

            var stage = await _context.tbl_DevelopmentStages.FindAsync(id);
            if (stage == null)
            {
                TempData["ErrorMessage"] = "Stage not found.";
                return RedirectToPage();
            }

            _context.tbl_DevelopmentStages.Remove(stage);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Stage deleted successfully.";
            await _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Delete", $"Deleted stage: {stage.sys_stageName}");

            return RedirectToPage();
        }
    }
}
