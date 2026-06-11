using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ZororoMemorialPark.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using ZororoMemorialPark.Data.Models;
using Microsoft.AspNetCore.Identity;
using static System.Collections.Specialized.BitVector32;
using System;

namespace ZororoMemorialPark.Pages.Graves
{
    public class GraveSectionsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
       
        private readonly IAppLogger _appLogger;


        public GraveSectionsModel(ApplicationDbContext context,UserManager<IdentityUser> userManager, IAppLogger appLogger)
        {
            _context = context;
            _appLogger = appLogger;
            _userManager = userManager;
        }

        public IList<tbl_Sections> Sections { get; set; } = new List<tbl_Sections>();
        public PaginatedList<tbl_Sections> PaginatedSections { get; set; }

        public async Task OnGetAsync(int pageIndex = 1)
        {
            try
            {
                Sections = await _context.tbl_Sections.AsNoTracking().ToListAsync();

                int pageSize = 5; // ?? Adjust how many rows per page
                var query = _context.tbl_Sections.OrderBy(s => s.sys_sectionName);
                PaginatedSections = await PaginatedList<tbl_Sections>.CreateAsync(query, pageIndex, pageSize);
                // Log the access
                var userId = _userManager.GetUserId(User);
                await _appLogger.LogActivityAsync(userId, "Access", "Accessed Grave Sections page");


            }
            catch (Exception ex)
            {
                // Log the exception if logging fails
                Console.WriteLine("Logging failed: " + ex.Message);
                Sections = await _context.tbl_Sections.AsNoTracking().ToListAsync();
            }
        }

        // CREATE
        public async Task<IActionResult> OnPostCreateAsync()
        {
            try
            {
                var sectionName = Request.Form["sectionName"].ToString().Trim();
                var sectionPrice = double.Parse(Request.Form["sectionPrice"]);
                var jobReminder = Request.Form["jobReminder"].ToString().Trim()??"N/A";
                var reminderPeriod = Int32.Parse(Request.Form["reminderPeriod"].ToString().Trim());
                var lastGraveNo = Int32.Parse(Request.Form["sys_lastGraveNo"].ToString().Trim());
                var maxConcurrentBurials = Int32.Parse(Request.Form["sys_maxConcurrentBurialsPerSlot"].ToString().Trim());



                if (string.IsNullOrEmpty(sectionName))
                {
                    TempData["ErrorMessage"] = "Section name cannot be empty.";
                    return RedirectToPage();
                }

                // Check for duplicates
                var existingSection = await _context.tbl_Sections
                    .FirstOrDefaultAsync(s => s.sys_sectionName.ToLower() == sectionName.ToLower());
                if (existingSection != null)
                {
                    TempData["ErrorMessage"] = "A section with this name already exists.";
                    return RedirectToPage();
                }
                var section = new tbl_Sections
                {
                    sys_sectionName = sectionName,
                    sys_SecPrice = sectionPrice,
                    sys_jobReminder = jobReminder,
                    sys_reminderPeriod = reminderPeriod,
                    sys_lastGraveNo = lastGraveNo,
                    sys_maxConcurrentBurialsPerSlot = maxConcurrentBurials
                };
                _context.tbl_Sections.Add(section);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Section created successfully.";

                //Save the log

                _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Create", $"Created section: {section.sys_sectionName}");

                return RedirectToPage();
            }
            catch (System.Exception ex)
            {
                TempData["ErrorMessage"] = "Error creating section: " + ex.Message;
                _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Error", $"Error creating section: {ex.Message}");

                return RedirectToPage();

            }
            
        }

       
       

        // POST EDIT
        public async Task<IActionResult> OnPostEditAsync()
        {
            try
            {
                var sectionIdStr = Request.Form["sys_sectionId"].ToString();
                var sectionPrice = double.Parse(Request.Form["sys_sectionPrice"]);
                var sectionName = Request.Form["sys_sectionName"].ToString().Trim();
                var jobReminder = Request.Form["sys_jobReminder"].ToString().Trim();
                var reminderPeriod = Int32.Parse(Request.Form["sys_reminderPeriod"].ToString().Trim());
                var lastGraveNo = Int32.Parse(Request.Form["sys_lastGraveNo"].ToString().Trim());
                var maxConcurrentBurials = Int32.Parse(Request.Form["sys_maxConcurrentBurialsPerSlot"].ToString().Trim());
                if (!int.TryParse(sectionIdStr, out int sectionId))
                {
                    TempData["ErrorMessage"] = "Invalid section ID.";
                    return RedirectToPage();
                }
                if (string.IsNullOrEmpty(sectionName))
                {
                    TempData["ErrorMessage"] = "Section name cannot be empty.";
                    return RedirectToPage();
                }
                var section = await _context.tbl_Sections.FindAsync(sectionId);
                if (section == null)
                {
                    TempData["ErrorMessage"] = "Section not found.";
                    return RedirectToPage();
                }
                // Check for duplicates
                var existingSection = await _context.tbl_Sections
                    .FirstOrDefaultAsync(s => s.sys_sectionName.ToLower() == sectionName.ToLower() && s.sys_sectionId != sectionId);
                if (existingSection != null)
                {
                    TempData["ErrorMessage"] = "A section with this name already exists.";
                    return RedirectToPage();
                }

                section.sys_sectionName = sectionName;
                section.sys_jobReminder = jobReminder;
                section.sys_reminderPeriod = reminderPeriod;
                section.sys_SecPrice = sectionPrice;
                section.sys_lastGraveNo = lastGraveNo;
                section.sys_maxConcurrentBurialsPerSlot=maxConcurrentBurials;
               

                _context.tbl_Sections.Update(section);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Section updated successfully.";
                _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Edit", $"Edited section: {section.sys_sectionName}");
                return RedirectToPage();

            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error editing section: " + ex.Message;
                _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Error", $"Error editing section: {ex.Message}");
                return RedirectToPage();
            }
              
        }

        

        // POST DELETE
        public async Task<IActionResult> OnPostDeleteAsync()
        {
            try
            {
                var sectionIdStr = Request.Form["sys_sectionId"].ToString();
                if (!int.TryParse(sectionIdStr, out int id))
                {
                    TempData["ErrorMessage"] = "Invalid section ID.";
                    return RedirectToPage();
                }
                var section = await _context.tbl_Sections.FindAsync(id);
                if (section == null) return NotFound();

                _context.tbl_Sections.Remove(section);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Section deleted successfully.";
                _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Delete", $"Deleted section: {section.sys_sectionName}");


                return RedirectToPage();
            }
            catch (System.Exception ex)
            {
                TempData["ErrorMessage"] = "Error deleting section: " + ex.Message;
                _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Error", $"Error deleting section: {ex.Message}");
                return RedirectToPage();
            }
            


        }
    }

    public class PaginatedList<T> : List<T>
    {
        public int PageIndex { get; private set; }
        public int TotalPages { get; private set; }

        public PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
        {
            PageIndex = pageIndex;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);

            AddRange(items);
        }

        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;

        public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int pageIndex, int pageSize)
        {
            var count = await source.CountAsync();
            var items = await source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PaginatedList<T>(items, count, pageIndex, pageSize);
        }
    }

}
