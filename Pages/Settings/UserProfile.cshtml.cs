using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ZororoMemorialPark.Pages.Settings
{
    [Authorize]
    public class UserProfileModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public UserProfileModel(UserManager<IdentityUser> userManager,
                                SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [TempData] public string StatusMessage { get; set; }

        // ---------------- PROFILE INPUT ----------------
        [BindProperty]
        public ProfileInputModel Input { get; set; }

        public class ProfileInputModel
        {
            [Required]
            public string FullName { get; set; }

            [Phone]
            public string PhoneNumber { get; set; }

            [Required, EmailAddress]
            public string Email { get; set; }
        }


        // ---------------- LOAD PROFILE ----------------
        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Redirect("/Identity/Account/Login");

            Input = new ProfileInputModel
            {
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                FullName = await GetFullNameAsync(user)
            };

            return Page();
        }


        // ---------------- UPDATE PROFILE ----------------
        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Clear();

            if (!TryValidateModel(Input, nameof(Input)))
                return Page();

            var user = await _userManager.GetUserAsync(User);

            user.Email = Input.Email;
            user.UserName = Input.Email;
            user.PhoneNumber = Input.PhoneNumber;

            await _userManager.UpdateAsync(user);
            await SetFullNameAsync(user, Input.FullName);

            await _signInManager.RefreshSignInAsync(user);

            StatusMessage = "Profile updated successfully.";
            return RedirectToPage();
        }


        // ---------------- CHANGE PASSWORD ----------------
        [BindProperty]
        public PasswordInputModel PasswordInput { get; set; }

        public class PasswordInputModel
        {
            [Required]
            public string CurrentPassword { get; set; }

            [Required, MinLength(6)]
            public string NewPassword { get; set; }
        }

        public async Task<IActionResult> OnPostChangePasswordAsync()
        {
            ModelState.Clear();

            if (!TryValidateModel(PasswordInput, nameof(PasswordInput)))
                return Page();

            var user = await _userManager.GetUserAsync(User);

            var result = await _userManager.ChangePasswordAsync(
                user,
                PasswordInput.CurrentPassword,
                PasswordInput.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var e in result.Errors)
                    ModelState.AddModelError("", e.Description);

                return Page();
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Password updated.";
            return RedirectToPage();
        }


        // ---------------- CLAIM HELPERS ----------------
        private async Task<string> GetFullNameAsync(IdentityUser user)
        {
            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "FullName")?.Value;
        }

        private async Task SetFullNameAsync(IdentityUser user, string fullName)
        {
            var claims = await _userManager.GetClaimsAsync(user);
            var existing = claims.FirstOrDefault(c => c.Type == "FullName");

            if (existing != null)
                await _userManager.RemoveClaimAsync(user, existing);

            await _userManager.AddClaimAsync(user, new Claim("FullName", fullName));
        }
    }
}
