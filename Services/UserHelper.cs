using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

public class UserHelper
{
    private readonly UserManager<IdentityUser> _userManager;

    public UserHelper(UserManager<IdentityUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<string?> GetFullNameByIdAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return null;

        var claims = await _userManager.GetClaimsAsync(user);
        return claims.FirstOrDefault(c => c.Type == "FullName")?.Value;
    }

    public async Task<string?> GetUserNameByIdAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user?.UserName;
    }
}
