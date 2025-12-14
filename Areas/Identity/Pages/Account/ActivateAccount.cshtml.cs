using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

public class ActivateAccountModel : PageModel
{
    private readonly PasswordResetService _resetService;
    private readonly UserManager<IdentityUser> _userManager;

    public ActivateAccountModel(
        PasswordResetService resetService,
        UserManager<IdentityUser> userManager)
    {
        _resetService = resetService;
        _userManager = userManager;
    }

    [BindProperty]
    public InputModel Input { get; set; }

    public string ErrorMessage { get; set; }

    public class InputModel
    {
        public string UserId { get; set; }

        [Required]
        public string Code { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }
    }

    public IActionResult OnGet(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            ErrorMessage = "Invalid activation link.";
            return Page();
        }

        Input = new InputModel
        {
            UserId = userId
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.FindByIdAsync(Input.UserId);
        if (user == null)
        {
            ErrorMessage = "User not found.";
            return Page();
        }

        // Validate activation code
        var valid = await _resetService.ValidateResetCodeAsync(Input.UserId, Input.Code);
        if (!valid)
        {
            ErrorMessage = "Invalid or expired activation code.";
            return Page();
        }

        // Set password
        await _userManager.RemovePasswordAsync(user);
        var result = await _userManager.AddPasswordAsync(user, Input.Password);

        if (!result.Succeeded)
        {
            ErrorMessage = string.Join("<br/>", result.Errors.Select(e => e.Description));
            return Page();
        }

        // Redirect to Login with success message
        TempData["SuccessMessage"] = "Your account has been activated! Please log in.";
        return RedirectToPage("/Account/Login");
    }
}
