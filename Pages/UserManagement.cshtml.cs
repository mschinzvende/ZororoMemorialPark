using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

[Authorize(Roles = "Admin")]
public class UserManagementModel : PageModel
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UserManagementModel(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public bool ShowModal { get; set; } // Flag to reopen modal on errors
    public List<UserViewModel> Users { get; set; } = new();
    public List<string> AllRoles { get; set; } = new() { "Admin", "Accountant", "Supervisor" };


    [BindProperty]
    public InputModel Input { get; set; }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MinLength(6)]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }



    public class UserViewModel
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public List<string> Roles { get; set; } = new();
        public bool IsLockedOut { get; set; }
    }




    public async Task OnGetAsync()
    {
        Users.Clear();
        var allUsers = _userManager.Users;
        foreach (var user in allUsers)
        {
            var roles = await _userManager.GetRolesAsync(user);
            Users.Add(new UserViewModel
            {
                Id = user.Id,
                Email = user.Email,
                Roles = new List<string>(roles),
                IsLockedOut = await _userManager.IsLockedOutAsync(user)
            });
        }
    }

    public async Task<IActionResult> OnPostAddUserAsync()
    {
        if (!ModelState.IsValid)
        {
            ShowModal = true; // Keep modal open if validation fails
            return Page();
        }

        var user = new IdentityUser { UserName = Input.Email, Email = Input.Email };
        var result = await _userManager.CreateAsync(user, Input.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            ShowModal = true; // Keep modal open on creation errors
            return Page();
        }

        // Success - reset form
        
        return RedirectToPage(); // reload page to show new user list
    }

    public async Task<IActionResult> OnPostAddRoleAsync(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || !await _roleManager.RoleExistsAsync(role))
            return NotFound();

        if (!await _userManager.IsInRoleAsync(user, role))
            await _userManager.AddToRoleAsync(user, role);

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemoveRoleAsync(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || !await _roleManager.RoleExistsAsync(role))
            return NotFound();

        if (await _userManager.IsInRoleAsync(user, role))
            await _userManager.RemoveFromRoleAsync(user, role);

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleLockAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound();

        if (await _userManager.IsLockedOutAsync(user))
        {
            await _userManager.SetLockoutEndDateAsync(user, null); // unlock
        }
        else
        {
            await _userManager.SetLockoutEndDateAsync(user, System.DateTimeOffset.MaxValue); // lock
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostResetPasswordAsync(string userId, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound();

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);
        }

        return RedirectToPage();
    }
}
