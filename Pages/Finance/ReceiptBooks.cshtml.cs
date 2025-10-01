using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ZororoMemorialPark.Data;
using ZororoMemorialPark.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace ZororoMemorialPark.Pages.Finance
{
    public class ReceiptBooksModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public ReceiptBooksModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<tbl_ReceiptBooks> ReceiptBooks { get; set; } = new List<tbl_ReceiptBooks>();
        public IList<tbl_Agents> Agents { get; set; } = new List<tbl_Agents>();

        public async Task OnGetAsync()
        {
            ReceiptBooks = await _context.tbl_ReceiptBooks
                //.Include(r => r.Agent)
                .OrderByDescending(r => r.sys_issueDate)
                .ToListAsync();

            Agents = await _context.tbl_Agents.OrderBy(a => a.sys_agentName).ToListAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync(tbl_ReceiptBooks book)
        {
            if (book.sys_startNumber >= book.sys_endNumber)
            {
                TempData["ErrorMessage"] = "Start number must be less than end number.";
                return RedirectToPage();
            }

            // Check overlapping ranges for agent
            var overlap = await _context.tbl_ReceiptBooks.AnyAsync(r =>
                r.sys_agentId == book.sys_agentId &&
                !(book.sys_endNumber < r.sys_startNumber || book.sys_startNumber > r.sys_endNumber));

            if (overlap)
            {
                TempData["ErrorMessage"] = "This receipt range overlaps with an existing book.";
                return RedirectToPage();
            }

            book.sys_currentNumber = book.sys_startNumber - 1;
            book.sys_isClosed = false;

            _context.tbl_ReceiptBooks.Add(book);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Receipt book added successfully.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditAsync(tbl_ReceiptBooks book)
        {
            var existing = await _context.tbl_ReceiptBooks.FindAsync(book.sys_receiptBookId);
            if (existing == null)
                return NotFound();

            if (!existing.sys_isClosed)
            {
                existing.sys_bookNumber = book.sys_bookNumber;
                existing.sys_startNumber = book.sys_startNumber;
                existing.sys_endNumber = book.sys_endNumber;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Receipt book updated successfully.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int sys_receiptBookId)
        {
            var book = await _context.tbl_ReceiptBooks.FindAsync(sys_receiptBookId);
            if (book == null)
                return NotFound();

            _context.tbl_ReceiptBooks.Remove(book);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Receipt book deleted.";
            return RedirectToPage();
        }
    }
}
