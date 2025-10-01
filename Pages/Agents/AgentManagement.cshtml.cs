using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ZororoMemorialPark.Data.Models;
using ZororoMemorialPark.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Shared;
using ZororoMemorialPark.Services;
using System.Data;

public class AgentManagementModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IAppLogger _appLogger;
    [TempData]
    public string StatusMessage { get; set; }

    [TempData]
    public string ErrorMessage { get; set; }
    public AgentManagementModel(ApplicationDbContext context, UserManager<IdentityUser> userManager, 
        RoleManager<IdentityRole> roleManager, IAppLogger logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _appLogger = logger;

    }

    public IList<tbl_Agents> Agents { get; set; } = new List<tbl_Agents>();

    [BindProperty] public tbl_Agents Agent { get; set; }
    [BindProperty] public tbl_CommissionPayments Payment { get; set; }

    public async Task OnGetAsync()
    {
        Agents = await _context.tbl_Agents.ToListAsync();
    }

    public async Task<IActionResult> OnPostAddAgentAsync()
    {

        //create user account for the agent
        var user = new IdentityUser { UserName = Agent.sys_agentEmail, Email = Agent.sys_agentEmail };

        var randomPassword = GenerateRandomPassword();
        var result = await _userManager.CreateAsync(user, randomPassword);
        if (result.Succeeded)
        {
            StatusMessage = "Agent Created successfully!";

            await _userManager.AddToRoleAsync(user, "Agent");

            //send a welcome email
            _ = Task.Run(async () =>
            {
                var emailService = new EmailService();

                emailService.SendEmail(
                    Agent.sys_agentEmail,
                    "Welcome to Zororo Memorial Park",
                    $"<p>Dear Agent,</p><p>Your account has been created successfully. " +
                    $"Your username is {Agent.sys_agentEmail}.</p> <p>Your Password is {randomPassword}</p><p>Thank you!</p>",
                    null
                );
            });

            Agent.UserId = user.Id;


            _context.tbl_Agents.Add(Agent);
            await _context.SaveChangesAsync();

            _appLogger.LogActivityAsync(_userManager.GetUserId(User), $"User Created Agent {Agent.sys_agentName}", $"Agent User-{Agent.sys_agentEmail} created successfully.");

            return RedirectToPage();
        }

        else
        {
            ErrorMessage = "Failed to create agent. User account creation failed!" + result.ToString();
            return RedirectToPage();
        }
       
    }

    public async Task<IActionResult> OnPostEditAgentAsync()
    {
        
        
        _context.tbl_Agents.Update(Agent);
        await _context.SaveChangesAsync();

        StatusMessage = "Agent updated successfully";
        
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAgentAsync(int id)
    {
        var agent = await _context.tbl_Agents.FindAsync(id);
        if (agent != null)
        {

            var user = await _userManager.FindByIdAsync(agent.UserId);
            if (user != null)
            {
                _context.tbl_Agents.Remove(agent);
                await _context.SaveChangesAsync();
                await _userManager.DeleteAsync(user);
               
            }

        }
        StatusMessage = "Agent deleted successfully!";
        _appLogger.LogActivityAsync(_userManager.GetUserId(User), $"User Deleted Agent {agent?.sys_agentName}", $"Agent User-{agent?.sys_agentEmail} deleted successfully.");
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAddPaymentAsync()
    {
        if (!ModelState.IsValid) return Page();
        _context.tbl_CommissionPayments.Add(Payment);
        await _context.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnGetAgentDetailsAsync(int id)
    {
        try
        {
            var agent = await _context.tbl_Agents.FindAsync(id);
            var commissions = await _context.tbl_CommissionsDue.Where(c => c.sys_agentId == id).ToListAsync();
            var payments = await _context.tbl_CommissionPayments
                                         .Where(p => commissions.Select(c => c.sys_commissionId).Contains(p.sys_commissionId))
                                         .ToListAsync();

            return new JsonResult(new { agent, commissions, payments });
        }

        catch (Exception ex)
        {
            ErrorMessage = "Error loading agent details: " + ex.Message;
            return RedirectToPage();
        }


    }

    public static string GenerateRandomPassword(PasswordOptions opts = null)
    {
        if (opts == null) opts = new PasswordOptions();

        string[] randomChars = new[] {
            "ABCDEFGHJKLMNOPQRSTUVWXYZ",    // uppercase 
            "abcdefghijkmnopqrstuvwxyz",    // lowercase
            "0123456789",                   // digits
            "!@$?_-*"                       // non-alphanumeric
        };

        Random rand = new Random(Environment.TickCount);
        List<char> chars = new List<char>();

        if (opts.RequireUppercase)
            chars.Insert(rand.Next(0, chars.Count),
                randomChars[0][rand.Next(0, randomChars[0].Length)]);

        if (opts.RequireLowercase)
            chars.Insert(rand.Next(0, chars.Count),
                randomChars[1][rand.Next(0, randomChars[1].Length)]);

        if (opts.RequireDigit)
            chars.Insert(rand.Next(0, chars.Count),
                randomChars[2][rand.Next(0, randomChars[2].Length)]);

        if (opts.RequireNonAlphanumeric)
            chars.Insert(rand.Next(0, chars.Count),
                randomChars[3][rand.Next(0, randomChars[3].Length)]);

        for (int i = chars.Count; i < opts.RequiredLength
            || chars.Distinct().Count() < opts.RequiredUniqueChars; i++)
        {
            string rcs = randomChars[rand.Next(0, randomChars.Length)];
            chars.Insert(rand.Next(0, chars.Count),
                rcs[rand.Next(0, rcs.Length)]);
        }

        return new string(chars.ToArray());
    }
}
