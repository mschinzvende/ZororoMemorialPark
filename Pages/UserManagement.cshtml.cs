using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ZororoMemorialPark.Services;

[Authorize(Roles = "Manager")]
public class UserManagementModel : PageModel
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IAppLogger _logger;
    private readonly SystemSettingsService _settings;
    private readonly NotificationService _notify;

    public UserManagementModel(
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IAppLogger logger, SystemSettingsService settings, NotificationService notify)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
        _settings = settings;
        _notify = notify;
    }

    [TempData] public string StatusMessage { get; set; }

    public List<UserViewModel> Users { get; set; } = new();
    public List<string> AllRoles { get; } = new() { "Manager", "FinanceOfficer", "OperationsOfficer", "Agent"};


    // --------------------- ADD USER ---------------------
    [BindProperty]
    public AddUserInput AddInput { get; set; }
    public bool ShowAddModal { get; set; }

    public class AddUserInput
    {
        [Required] public string FullName { get; set; }
        [Phone] public string PhoneNumber { get; set; }
        [Required, EmailAddress] public string Email { get; set; }
        [Required, MinLength(6)] public string Password { get; set; }
    }


    // --------------------- EDIT USER ---------------------
    [BindProperty]
    public EditUserInput EditInput { get; set; }

    [BindProperty, ValidateNever]
    public string EditUserId { get; set; }

    public bool ShowEditModal { get; set; }

    public class EditUserInput
    {
        [Required] public string FullName { get; set; }
        [Phone] public string PhoneNumber { get; set; }
        [Required, EmailAddress] public string Email { get; set; }
    }


    // --------------------- RESET PASSWORD ---------------------
    [BindProperty]
    public ResetPasswordInput ResetInput { get; set; }

    public bool ShowResetModal { get; set; }

    public class ResetPasswordInput
    {
        public string UserId { get; set; }
        [Required, MinLength(6)] public string NewPassword { get; set; }
    }


    // --------------------- DELETE USER ---------------------
    [BindProperty, ValidateNever]
    public string DeleteUserId { get; set; }


    // --------------------- ROLES ---------------------
    [BindProperty, ValidateNever]
    public string RolesUserId { get; set; }

    [BindProperty, ValidateNever]
    public List<string> ModalSelectedRoles { get; set; } = new();

    public bool ShowRolesModal { get; set; }


    // --------------------- VIEW MODEL ---------------------
    public class UserViewModel
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public List<string> Roles { get; set; }
        public bool IsLockedOut { get; set; }
    }


    // ---------------------- HELPERS ----------------------

    private void SafeLog(string action, string msg)
    {
        _ = Task.Run(async () =>
        {
            try { await _logger.LogActivityAsync(_userManager.GetUserId(User), action, msg); }
            catch { /* swallow */ }
        });
    }

    private async Task<string> GetFullNameAsync(IdentityUser u)
    {
        var claims = await _userManager.GetClaimsAsync(u);
        return claims.FirstOrDefault(c => c.Type == "FullName")?.Value;
    }

    private async Task SetFullNameAsync(IdentityUser u, string fullName)
    {
        var claims = await _userManager.GetClaimsAsync(u);
        var existing = claims.FirstOrDefault(c => c.Type == "FullName");

        if (existing != null)
            await _userManager.RemoveClaimAsync(u, existing);

        await _userManager.AddClaimAsync(u, new Claim("FullName", fullName));
    }

    private async Task LoadUsersAsync()
    {
        Users.Clear();
        foreach (var u in _userManager.Users)
        {
            Users.Add(new UserViewModel
            {
                Id = u.Id,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                FullName = await GetFullNameAsync(u),
                Roles = (await _userManager.GetRolesAsync(u)).ToList(),
                IsLockedOut = await _userManager.IsLockedOutAsync(u)
            });
        }
    }


    // --------------------- GET ---------------------
    public async Task OnGetAsync() => await LoadUsersAsync();


    // --------------------- ADD USER ---------------------
    public async Task<IActionResult> OnPostAddUserAsync()
    {
        // Clear stale ModelState entries from other modals
        ModelState.Clear();

        if (!TryValidateModel(AddInput, nameof(AddInput)))
        {
            ShowAddModal = true;
            await LoadUsersAsync();
            return Page();
        }

        var user = new IdentityUser
        {
            UserName = AddInput.Email,
            Email = AddInput.Email,
            PhoneNumber = AddInput.PhoneNumber
        };

        var create = await _userManager.CreateAsync(user, AddInput.Password);

        if (!create.Succeeded)
        {
            foreach (var e in create.Errors)
                ModelState.AddModelError("", e.Description);

            ShowAddModal = true;
            await LoadUsersAsync();
            return Page();
        }

        await SetFullNameAsync(user, AddInput.FullName);

        SafeLog("User Created", AddInput.Email);
        TempData["SuccessMessage"]= "User created successfully!";

        //use the notification service to send user the email

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            try
            {
                string baseUrl = $"{Request.Scheme}://{Request.Host}";
                string loginUrl = $"{baseUrl}/Identity/Account/Login";

                string subject = "Your Zororo Memorial Park System Account";

                string body = $@"
            <p>Dear {AddInput.FullName},</p>

            <p>Your user account for <strong>Zororo Memorial Park Management System</strong> has been successfully created.</p>

            <p><strong>Login Email:</strong> {user.Email}<br/>
               <strong>Temporary Password:</strong> {AddInput.Password}</p>

            <p>You can log in using the link below:</p>

            <p>
                <a href=""{loginUrl}"" 
                   style=""background-color:#006A46;color:white;padding:10px 18px;
                          text-decoration:none;border-radius:5px;font-weight:bold;"">
                   Click here to login
                </a>
            </p>

            <p>Please log in and change your password immediately for security reasons.</p>

            <br/>
            <p>Regards,<br/>Zororo Memorial Park Admin</p>
        ";

                await _notify.SendEmailAsync(user.Email, subject, body);
            }
            catch (Exception ex)
            {
                // Don't block the process on email failure
                Console.WriteLine("Email notification error: " + ex.Message);
            }
        }




        return RedirectToPage();
    }



    // --------------------- EDIT USER ---------------------
    public async Task<IActionResult> OnPostEditUserAsync()
    {
        ModelState.Clear();

        if (!TryValidateModel(EditInput, nameof(EditInput)))
        {
            ShowEditModal = true;
            await LoadUsersAsync();
            return Page();
        }

        var user = await _userManager.FindByIdAsync(EditUserId);
        if (user == null)
        {
            ModelState.AddModelError("", "User not found.");
            ShowEditModal = true;
            await LoadUsersAsync();
            return Page();
        }

        user.Email = EditInput.Email;
        user.UserName = EditInput.Email;
        user.PhoneNumber = EditInput.PhoneNumber;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors)
                ModelState.AddModelError("", e.Description);

            ShowEditModal = true;
            await LoadUsersAsync();
            return Page();
        }

        await SetFullNameAsync(user, EditInput.FullName);

        SafeLog("User Updated", EditInput.Email);
        TempData["SuccessMessage"] = "User updated successfully!";
        return RedirectToPage();
    }



    // --------------------- RESET PASSWORD ---------------------
    public async Task<IActionResult> OnPostResetPasswordAsync()
    {
        ModelState.Clear();

        if (!TryValidateModel(ResetInput, nameof(ResetInput)))
        {
            ShowResetModal = true;
            await LoadUsersAsync();
            return Page();
        }

        var user = await _userManager.FindByIdAsync(ResetInput.UserId);
        if (user == null)
        {
            ModelState.AddModelError("", "User not found.");
            ShowResetModal = true;
            await LoadUsersAsync();
            return Page();
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, ResetInput.NewPassword);

        if (!result.Succeeded)
        {
            foreach (var e in result.Errors)
                ModelState.AddModelError("", e.Description);

            ShowResetModal = true;
            await LoadUsersAsync();
            return Page();
        }


        // ============= SEND PASSWORD RESET EMAIL =============
        
        try
        {
            string baseUrl = $"{Request.Scheme}://{Request.Host}";
            string loginUrl = $"{baseUrl}/Identity/Account/Login";

            string subject = "Your Password Has Been Reset • Zororo Memorial Park";

            string body = $@"
        <p>Dear {user.UserName},</p>

        <p>Your password for the <strong>Zororo Memorial Park Management System</strong> has been successfully reset.</p>

        <p><strong>Login Email:</strong> {user.Email}<br/>
        <strong>New Password:</strong> {ResetInput.NewPassword}</p>

        <p>
            <a href=""{loginUrl}"" 
               style=""background-color:#006A46;color:white;padding:10px 18px;
                      text-decoration:none;border-radius:5px;font-weight:bold;"">
                Click here to login
            </a>
        </p>

        <p>If you did not request this password reset, please contact the system administrator immediately.</p>

        <p>Regards,<br/>Zororo Memorial Park Admin</p>
    ";

            // Send email normally (NOT inside Task.Run)
            await _notify.SendEmailAsync(user.Email, subject, body);

            TempData["SuccessMessage"] = "Password has been reset and an email notification was sent.";
        }
        catch (Exception ex)
        {
            // Show the REAL error on the page
            TempData["ErrorMessage"] = "Email sending failed: " + ex.Message;
        }




        SafeLog("Password Reset", user.Email);

        
        return RedirectToPage();
    }




    // --------------------- DELETE USER ---------------------
    public async Task<IActionResult> OnPostDeleteUserAsync()
    {
        var user = await _userManager.FindByIdAsync(DeleteUserId);
        if (user == null)
        {
            ModelState.AddModelError("", "User not found.");
            await LoadUsersAsync();
            return Page();
        }

        await _userManager.DeleteAsync(user);

        SafeLog("User Deleted", user.Email);
        TempData["SuccessMessage"] = "User deleted successfully!";
        return RedirectToPage();
    }


    // --------------------- UPDATE ROLES ---------------------
    public async Task<IActionResult> OnPostUpdateRolesAsync()
    {
        var user = await _userManager.FindByIdAsync(RolesUserId);
        if (user == null)
        {
            ShowRolesModal = true;
            ModelState.AddModelError("", "User not found.");
            await LoadUsersAsync();
            return Page();
        }

        var current = await _userManager.GetRolesAsync(user);
        var selected = ModalSelectedRoles ?? new();

        var toAdd = selected.Except(current);
        var toRemove = current.Except(selected);

        foreach (var r in toAdd) await _userManager.AddToRoleAsync(user, r);
        foreach (var r in toRemove) await _userManager.RemoveFromRoleAsync(user, r);

        SafeLog("Roles Updated", user.Email);

        TempData["SuccessMessage"] = "Roles updated successfully!";
        return RedirectToPage();
    }


    // --------------------- LOCK/UNLOCK ---------------------
    public async Task<IActionResult> OnPostToggleLockAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user != null)
        {
            if (await _userManager.IsLockedOutAsync(user))
                await _userManager.SetLockoutEndDateAsync(user, null);
            else
                await _userManager.SetLockoutEndDateAsync(user, System.DateTimeOffset.MaxValue);

            SafeLog("Lock Toggle", user.Email);
        }

        return RedirectToPage();
    }
}
