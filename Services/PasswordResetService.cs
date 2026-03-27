using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ZororoMemorialPark.Data;
using ZororoMemorialPark.Data.Models;

public class PasswordResetService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public PasswordResetService(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // Generate a 6-digit code
    public async Task<string> GenerateResetCodeAsync(string userId)
    {
        var code = new Random().Next(100000, 999999).ToString();

        var record = new tbl_PasswordResetCode
        {
            UserId = userId,
            Code = code,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            Used = false
        };

        _context.tbl_PasswordResetCodes.Add(record);
        await _context.SaveChangesAsync();

        return code;
    }

    // Validate a reset code
    public async Task<bool> ValidateResetCodeAsync(string userId, string code)
    {
        var record = await _context.tbl_PasswordResetCodes
            .Where(x => x.UserId == userId && x.Code == code && !x.Used && x.ExpiresAt > DateTime.UtcNow)
            .FirstOrDefaultAsync();

        if (record == null)
            return false;

        record.Used = true;
        await _context.SaveChangesAsync();

        return true;
    }
}
