using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ZororoMemorialPark.Data;
using ZororoMemorialPark.Data.Models;
using ZororoMemorialPark.Services;

namespace ZororoMemorialPark.Pages.Tombstones
{
    public class ManageTombstonesModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly PersistentStaticFileStorage _fileStorage;

        public ManageTombstonesModel(ApplicationDbContext context, PersistentStaticFileStorage fileStorage)
        {
            _context = context;
            _fileStorage = fileStorage;
        }

        public IList<tbl_Tombstones> Tombstones { get; set; } = new List<tbl_Tombstones>();

        [BindProperty]
        public tbl_Tombstones Tombstone { get; set; }

        [BindProperty]
        public IFormFile ImageFile { get; set; }

        public async Task OnGetAsync()
        {
            Tombstones = await _context.tbl_Tombstones.OrderByDescending(t => t.sys_tombstoneId).ToListAsync();
        }

        public async Task<IActionResult> OnPostAddTombstoneAsync()
        {
            

            // handle image upload
            if (ImageFile != null && ImageFile.Length > 0)
            {
                string uploadsFolder = _fileStorage.GetDirectory("uploads", "tombstones");

                string fileName = $"{Guid.NewGuid()}{Path.GetExtension(ImageFile.FileName)}";
                string filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }

                Tombstone.sys_imagePath = "/uploads/tombstones/" + fileName;
            }

            _context.tbl_Tombstones.Add(Tombstone);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Tombstone added successfully!";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditTombstoneAsync()
        {
            var existing = await _context.tbl_Tombstones.FindAsync(Tombstone.sys_tombstoneId);
            if (existing == null) return NotFound();

            existing.sys_tombstoneName = Tombstone.sys_tombstoneName;
            existing.sys_description = Tombstone.sys_description;
            existing.sys_material = Tombstone.sys_material;
            existing.sys_size = Tombstone.sys_size;
            existing.sys_price = Tombstone.sys_price;
            existing.sys_quantityAvailable = Tombstone.sys_quantityAvailable;

            if (ImageFile != null && ImageFile.Length > 0)
            {
                string uploadsFolder = _fileStorage.GetDirectory("uploads", "tombstones");

                string fileName = $"{Guid.NewGuid()}{Path.GetExtension(ImageFile.FileName)}";
                string filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }

                // delete old image
                if (!string.IsNullOrEmpty(existing.sys_imagePath))
                {
                    var oldPath = _fileStorage.GetPersistentFilePathForPublicPath(existing.sys_imagePath);
                    if (!string.IsNullOrEmpty(oldPath) && System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                existing.sys_imagePath = "/uploads/tombstones/" + fileName;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Tombstone updated successfully!";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteTombstoneAsync(int TombstoneId)
        {
            var existing = await _context.tbl_Tombstones.FindAsync(TombstoneId);
            if (existing == null) return NotFound();

            if (!string.IsNullOrEmpty(existing.sys_imagePath))
            {
                var oldPath = _fileStorage.GetPersistentFilePathForPublicPath(existing.sys_imagePath);
                if (!string.IsNullOrEmpty(oldPath) && System.IO.File.Exists(oldPath))
                    System.IO.File.Delete(oldPath);
            }

            _context.tbl_Tombstones.Remove(existing);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Tombstone deleted successfully!";
            return RedirectToPage();
        }

    }
}
