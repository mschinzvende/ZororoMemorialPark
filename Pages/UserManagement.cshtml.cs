using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using ZororoMemorialPark.Services;

[Authorize(Roles = "Admin")]
public class UserManagementModel : PageModel
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IAppLogger _appLogger;
    public UserManagementModel(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, IAppLogger appLogger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _appLogger = appLogger;
    }

    [TempData]
    public string StatusMessage { get; set; }

    // --- Users list ---
    public List<UserViewModel> Users { get; set; } = new();
    public List<string> AllRoles { get; set; } = new() { "Admin", "Accountant", "Supervisor" };

    // --- Add User ---
    [BindProperty]
    public AddUserInputModel AddInput { get; set; }
    public bool ShowAddModal { get; set; }

    public class AddUserInputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        
       
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }

    // --- Edit User ---
    [BindProperty]
    public EditUserInputModel EditInput { get; set; }

    [BindProperty]
    public string EditUserId { get; set; }
    public bool ShowEditModal { get; set; }

    public class EditUserInputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

    }

    // --- Reset Password ---
    [BindProperty]
    public ResetPasswordInputModel ResetInput { get; set; }
    public bool ShowResetModal { get; set; }

    public class ResetPasswordInputModel
    {
        public string UserId { get; set; }

        
        
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }
    }

    // --- Delete User ---
    [BindProperty, ValidateNever]
    public string DeleteUserId { get; set; }

    // --- ViewModel ---
    public class UserViewModel
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public List<string> Roles { get; set; } = new();
        public bool IsLockedOut { get; set; }
    }

    // --- OnGet ---
    public async Task OnGetAsync()
    {
        try
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

        catch (Exception ex)
        {
            await _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Error", $"Error in UserManagement OnGetAsync: {ex.Message}");
            ModelState.AddModelError(string.Empty, "An error occurred while loading the user management page.");
        }   
        
    }

    // --- Add User ---
    public async Task<IActionResult> OnPostAddUserAsync()
    {
        try
        {
            ModelState.Clear();
            if (!TryValidateModel(AddInput))
            {
                ShowAddModal = true;
                return Page();
            }

            if (!string.IsNullOrEmpty(AddInput.Password))
            {
                if (AddInput.Password.Length < 6)
                {
                    ModelState.AddModelError(nameof(AddInput.Password), "Password must be at least 6 characters.");
                    ShowEditModal = true;
                    return Page();
                }

                var user = new IdentityUser { UserName = AddInput.Email, Email = AddInput.Email };
                var result = await _userManager.CreateAsync(user, AddInput.Password);

                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(string.Empty, error.Description);

                    ShowAddModal = true;
                    return Page();
                }

                if (result.Succeeded)
                {
                    StatusMessage = "User Created successfully!";

                    //send a welcome email
                    _ = Task.Run(async () =>
                    {
                        var emailService = new EmailService();

                        await emailService.SendEmailAsync(
                            AddInput.Email,
                            "Welcome to Zororo Memorial Park",
                            $"<p>Dear User,</p><p>Your account has been created successfully. Your username is {AddInput.Email}.</p> <p>Your Password is {AddInput.Password}</p><p>Thank you!</p>",
                            null
                        );
                    });

                    _appLogger.LogActivityAsync(_userManager.GetUserId(User), "User Created", $"User {AddInput.Email} created successfully.");
                }
            }

            return RedirectToPage();
        }

        catch (Exception ex)
        {
            await _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Error", $"Error in UserManagement OnPostAddUserAsync: {ex.Message}");
            ModelState.AddModelError(string.Empty, "An error occurred while adding the user.");
            ShowAddModal = true;
            return Page();
        }

        
    }

    // --- Edit User ---
    public async Task<IActionResult> OnPostEditUserAsync()
    {
        try
        {
            ModelState.Clear();
            if (!TryValidateModel(EditInput))
            {
                ShowEditModal = true;
                return Page();
            }

            var user = await _userManager.FindByIdAsync(EditUserId);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "User not found.");
                ShowEditModal = true;
                return Page();
            }

            user.Email = EditInput.Email;
            user.UserName = EditInput.Email;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                ShowEditModal = true;
                return Page();
            }

            if (updateResult.Succeeded)
            {
                StatusMessage = "User Updated successfully!";

                _appLogger.LogActivityAsync(_userManager.GetUserId(User), "User Updated", $"User {EditInput.Email} updated successfully.");
            }

            return RedirectToPage();
        }
        catch (Exception ex)
        {
            await _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Error", $"Error in UserManagement OnPostEditUserAsync: {ex.Message}");
            ModelState.AddModelError(string.Empty, "An error occurred while editing the user.");
            ShowEditModal = true;
            return Page();
        }
        
    }

    // --- Reset Password ---
    public async Task<IActionResult> OnPostResetPasswordAsync()
    {
        try
        {
            ModelState.Clear();
            if (!TryValidateModel(ResetInput))
            {
                ShowResetModal = true;
                return Page();
            }

            var user = await _userManager.FindByIdAsync(ResetInput.UserId);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "User not found.");
                ShowResetModal = true;
                return Page();
            }
            if (!string.IsNullOrEmpty(ResetInput.NewPassword))
            {
                if (ResetInput.NewPassword.Length < 6)
                {
                    ModelState.AddModelError(nameof(ResetInput.NewPassword), "Password must be at least 6 characters.");
                    ShowResetModal = true;
                    return Page();
                }
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, ResetInput.NewPassword);

                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(string.Empty, error.Description);

                    ShowResetModal = true;
                    return Page();
                }

                // Fire-and-forget email sending
                _ = Task.Run(async () =>
                {
                   
                        var emailService = new EmailService();
                        await emailService.SendEmailAsync(
                            user.Email,
                            "Password Reset Confirmation",
                            $"<p>Dear {user.Email},</p><p>Your password has been reset successfully. Your new password is <strong>{ResetInput.NewPassword}</strong>.</p><p>Thank you!</p>",
                            null // optional attachment
                        );
                    
                });

                StatusMessage = "Password reset successfully!";
                await _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Password Reset", $"Password for user {user.Email} reset successfully.");
                    return RedirectToPage();
                }
            
                    return RedirectToPage();
        }

        catch (Exception ex)
        {
            await _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Error", $"Error in UserManagement OnPostResetPasswordAsync: {ex.Message}");
            ModelState.AddModelError(string.Empty, "An error occurred while resetting the password.");
            ShowResetModal = true;
            return Page();
        }
        
    }

    // --- Delete User ---
    public async Task<IActionResult> OnPostDeleteUserAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(DeleteUserId))
            {
                ModelState.AddModelError(string.Empty, "User not found.");
                return Page();
            }

            var user = await _userManager.FindByIdAsync(DeleteUserId);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "User not found.");
                return Page();
            }

            await _userManager.DeleteAsync(user);



            StatusMessage = "User Deleted successfully!";

            _appLogger.LogActivityAsync(_userManager.GetUserId(User), "User Deleted", $"User {user.Email} deleted successfully.");

            return RedirectToPage();
        }

        catch (Exception ex)
        {
            await _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Error", $"Error in UserManagement OnPostDeleteUserAsync: {ex.Message}");
            ModelState.AddModelError(string.Empty, "An error occurred while deleting the user.");
            return Page();
        }
            
    }

    // --- Lock/Unlock User ---
    public async Task<IActionResult> OnPostToggleLockAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            if (await _userManager.IsLockedOutAsync(user))
                await _userManager.SetLockoutEndDateAsync(user, null);
            else
                await _userManager.SetLockoutEndDateAsync(user, System.DateTimeOffset.MaxValue);

            _appLogger.LogActivityAsync(_userManager.GetUserId(User), "User Lock Status Changed", $"User {user.Email} lock status toggled.");
            return RedirectToPage();
        }

        catch (Exception ex)
        {
            await _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Error", $"Error in UserManagement OnPostToggleLockAsync: {ex.Message}");
            ModelState.AddModelError(string.Empty, "An error occurred while toggling user lock status.");
            return Page();
        }
    }

    // --- Add/Remove Roles ---
    public async Task<IActionResult> OnPostAddRoleAsync(string userId, string role)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !await _roleManager.RoleExistsAsync(role)) return NotFound();
            if (!await _userManager.IsInRoleAsync(user, role)) await _userManager.AddToRoleAsync(user, role);

            _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Role Added", $"Role {role} added to user {user.Email}.");
            return RedirectToPage();
        }

        catch (Exception ex)
        {
            await _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Error", $"Error in UserManagement OnPostAddRoleAsync: {ex.Message}");
            ModelState.AddModelError(string.Empty, "An error occurred while adding the role.");
            return Page();
        }

    }

    public async Task<IActionResult> OnPostRemoveRoleAsync(string userId, string role)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !await _roleManager.RoleExistsAsync(role)) return NotFound();
            if (await _userManager.IsInRoleAsync(user, role)) await _userManager.RemoveFromRoleAsync(user, role);

            _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Role Removed", $"Role {role} removed from user {user.Email}.");
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            await _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Error", $"Error in UserManagement OnPostRemoveRoleAsync: {ex.Message}");
            ModelState.AddModelError(string.Empty, "An error occurred while removing the role.");
            return Page();
        }
        
    }
}
