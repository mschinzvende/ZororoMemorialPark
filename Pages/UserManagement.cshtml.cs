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
using System.Net;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ZororoMemorialPark.Data.Models;
using ZororoMemorialPark.Data;
using ZororoMemorialPark.Services.WhatsApp;

[Authorize(Roles = "Manager")]
public class UserManagementModel : PageModel
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IAppLogger _logger;
    private readonly SystemSettingsService _settings;
    private readonly NotificationService _notify;
    private readonly PasswordResetService _passwordReset;
    private readonly ApplicationDbContext _context;
    private readonly WhatsAppNotificationService _wa;

    public UserManagementModel(
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IAppLogger logger, SystemSettingsService settings, 
        NotificationService notify, PasswordResetService passwordReset, 
        ApplicationDbContext context, WhatsAppNotificationService wa)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
        _settings = settings;
        _notify = notify;
        _passwordReset = passwordReset;
        _context = context;
        _wa = wa;
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
        ModelState.ClearValidationState(nameof(AddInput));
        TryValidateModel(AddInput, nameof(AddInput));


        

        var user = new IdentityUser
        {
            UserName = AddInput.Email,
            Email = AddInput.Email,
            PhoneNumber = AddInput.PhoneNumber
        };

        var create = await _userManager.CreateAsync(user);
        if (!create.Succeeded)
        {
            foreach (var e in create.Errors)
                ModelState.AddModelError("", e.Description);

            ShowAddModal = true;
            await LoadUsersAsync();
            return Page();
        }

       

        // Generate 6-digit activation code
        var activationCode = await _passwordReset.GenerateResetCodeAsync(user.Id);

        // Build activation URL
        string baseUrl = $"{Request.Scheme}://{Request.Host}";
        string activationUrl = $"{baseUrl}/Identity/Account/ActivateAccount?userId={user.Id}";

        // Send email
        try
        {
            string subject = "Your Zororo Memorial Park Account Activation";

            string body = $@"
<p>Dear {AddInput.FullName},</p>

<p>Your user account for the 
<strong>Zororo Memorial Park Management System</strong> has been created.</p>

<p>Please activate your account using the code below:</p>

<h2 style=""color:#006A46;"">{activationCode}</h2>

<p>This code expires in <strong>15 minutes</strong>.</p>

<p>Click the link below to activate and set your password:</p>

<p>
    <a href=""{activationUrl}""
        style=""background-color:#006A46;color:white;padding:10px 18px;
               text-decoration:none;border-radius:5px;font-weight:bold;"">
        Activate Account
    </a>
</p>

<p>If you did not request this account, please contact the administrator.</p>

<br/>
<p>Regards,<br/>Zororo Memorial Park Admin</p>
";

            await _notify.SendEmailAsync(user.Email, subject, body);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Email sending error: " + ex.Message);
        }

        // Save full name claim
        await SetFullNameAsync(user, AddInput.FullName);

        SafeLog("User Created", AddInput.Email);

        TempData["SuccessMessage"] = "User created successfully and activation email sent.";

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
        var user = await _userManager.FindByIdAsync(ResetInput.UserId);
        if (user == null)
        {
            ModelState.AddModelError("", "User not found.");
            ShowResetModal = true;
            await LoadUsersAsync();
            return Page();
        }

        // Generate a 6-digit reset code
        var code = await _passwordReset.GenerateResetCodeAsync(user.Id);

        // Build reset page URL
        string baseUrl = $"{Request.Scheme}://{Request.Host}";
        string resetUrl = $"{baseUrl}/Identity/Account/ResetPassword?userId={user.Id}";

        string subject = "Password Reset Code • Zororo Memorial Park";

        string body = $@"
<p>Dear {user.UserName},</p>

<p>You requested to reset your password for the 
<strong>Zororo Memorial Park Management System</strong>.</p>

<p>Your password reset code is:</p>

<h2 style=""color:#006A46;"">{code}</h2>

<p>This code expires in <strong>15 minutes</strong>.</p>

<p>Please click the link below to enter your code and set a new password:</p>

<p>
    <a href=""{resetUrl}"" 
       style=""background-color:#006A46;color:white;padding:10px 18px;
              text-decoration:none;border-radius:5px;font-weight:bold;display:inline-block;"">
        Enter Reset Code
    </a>
</p>

<p>If you did not request this action, please contact the administrator immediately.</p>

<p>Regards,<br/>Zororo Memorial Park Admin</p>
";

        await _notify.SendEmailAsync(user.Email, subject, body);
        TempData["SuccessMessage"] = "Reset code generated and email sent.";

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

        var toAdd = selected.Except(current).ToList();
        var toRemove = current.Except(selected).ToList();

        foreach (var r in toAdd)
            await _userManager.AddToRoleAsync(user, r);

        foreach (var r in toRemove)
            await _userManager.RemoveFromRoleAsync(user, r);

        // ----------------------------------
        // AGENT ROLE ADDED
        // ----------------------------------

        if (toAdd.Contains("Agent"))
        {
            var existingAgent =
                await _context.tbl_Agents
                    .FirstOrDefaultAsync(a =>
                        a.UserId == user.Id);

            if (existingAgent == null)
            {
                var agent = new tbl_Agents
                {
                    UserId = user.Id,
                    sys_agentName = user.UserName ?? "Agent",
                    sys_agentContactPerson = user.UserName ?? "",
                    sys_agentEmail = user.Email ?? "",
                    sys_agentPhone = user.PhoneNumber ?? "",
                    sys_agentAddress = "N/A",
                    sys_commission = 0,
                    sys_isPercentage = true,
                    sys_isActive = true
                    
                };

                _context.tbl_Agents.Add(agent);

                await _context.SaveChangesAsync();

                if (!string.IsNullOrWhiteSpace(agent.sys_agentPhone))
                {
                    await _wa.SendTextMessage(
                        agent.sys_agentPhone,
                """
You have been added as a Zororo Memorial Agent.

To activate WhatsApp booking:

1. Send any message to our WhatsApp number.
2. Create your PIN.
3. Login and start booking graves.
""");
                }

                SafeLog(
                    "Agent Record Created",
                    user.Email);
            }
            else
            {
                existingAgent.sys_isActive = true;



                await _context.SaveChangesAsync();

                await _wa.SendTextMessage(
    existingAgent.sys_agentPhone,
    existingAgent.sys_isActive
        ? "Your Zororo agent account has been enabled."
        : "Your Zororo agent account has been disabled. You can no longer access the WhatsApp booking service.");

                SafeLog(
                    "Agent Reactivated",
                    user.Email);
            }
        }

        // ----------------------------------
        // AGENT ROLE REMOVED
        // ----------------------------------

        if (toRemove.Contains("Agent"))
        {
            var existingAgent =
                await _context.tbl_Agents
                    .FirstOrDefaultAsync(a =>
                        a.UserId == user.Id);

            if (existingAgent != null)
            {
                existingAgent.sys_isActive = false;

                await _context.SaveChangesAsync();

                await _wa.SendTextMessage(
existingAgent.sys_agentPhone,
existingAgent.sys_isActive
? "Your Zororo agent account has been enabled."
: "Your Zororo agent account has been disabled. You can no longer access the WhatsApp booking service.");

                SafeLog(
                    "Agent Disabled",
                    user.Email);
            }
        }

        SafeLog(
            "Roles Updated",
            user.Email);

        TempData["SuccessMessage"] =
            "Roles updated successfully!";

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
