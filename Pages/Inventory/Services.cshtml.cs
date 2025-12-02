using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using System;
using ZororoMemorialPark.Data.Models;
using ZororoMemorialPark.Data;
using Microsoft.EntityFrameworkCore;


namespace ZororoMemorialPark.Pages.Inventory
{
    public class ServicesModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public ServicesModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<tbl_ProductsAndServices> Services { get; set; }

    public async Task OnGetAsync()
    {
        Services = await _context.tbl_ProductsAndServices
            .Where(x => x.sys_itemType == "Service")
            .OrderBy(x => x.sys_itemName)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync(tbl_ProductsAndServices service)
    {
        service.sys_itemType = "Service";
        service.sys_isActive = true;
        service.sys_unitOfMeasure = "n/a";
        service.sys_status = "n/a";

        _context.tbl_ProductsAndServices.Add(service);
        await _context.SaveChangesAsync();

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEditAsync(tbl_ProductsAndServices service)
    {
        var existing = await _context.tbl_ProductsAndServices
            .FindAsync(service.sys_itemId);

        if (existing == null)
            return NotFound();

        existing.sys_itemName = service.sys_itemName;
        existing.sys_description = service.sys_description;
        existing.sys_category = service.sys_category;
        existing.sys_sellingPrice = service.sys_sellingPrice;

        await _context.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleStatusAsync(int id)
    {
        var service = await _context.tbl_ProductsAndServices.FindAsync(id);
        if (service == null) return NotFound();

        service.sys_isActive = !service.sys_isActive;
        await _context.SaveChangesAsync();

        return RedirectToPage();
    }
}
    }