using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ZororoMemorialPark.Data.Models;
using ZororoMemorialPark.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Shared;
using ZororoMemorialPark.Services;
using System.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Globalization;

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

    public List<SelectListItem> IsPercentageOptions { get; set; }
    public async Task OnGetAsync()
    {
        Agents = await _context.tbl_Agents.ToListAsync();

        IsPercentageOptions = new List<SelectListItem>
    {
        new SelectListItem { Text = "Percentage", Value = "true" },
        new SelectListItem { Text = "Fixed Amount", Value = "false" }
    };
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

    public async Task<IActionResult> OnPostAddPaymentAsync([FromForm] tbl_CommissionPayments payment)
    {
        

        _context.tbl_CommissionPayments.Add(payment);
        await _context.SaveChangesAsync();

        // Update commission status if fully paid
        var commission = await _context.tbl_CommissionsDue.FindAsync(payment.sys_commissionId);
        if (commission != null)
        {
            var totalPaid = await _context.tbl_CommissionPayments
                .Where(p => p.sys_commissionId == commission.sys_commissionId)
                .SumAsync(p => p.sys_amountPaid);

            if (totalPaid >= commission.sys_amount)
            {
                commission.sys_isPaid = true;
                _context.tbl_CommissionsDue.Update(commission);
                await _context.SaveChangesAsync();
            }
        }

        return new JsonResult(new { success = true });
    }

    public async Task<IActionResult> OnGetAgentDetailsAsync(int id)
    {
        var agent = await _context.tbl_Agents.FindAsync(id);

        var commissions = await _context.tbl_CommissionsDue
    .Where(c => c.sys_agentId == id)
    .ToListAsync();

        var result = commissions.Select(c => new
        {
            c.sys_commissionId,
            c.sys_graveId,
            c.sys_amount,
            c.sys_isPaid,
            amountPaid = _context.tbl_CommissionPayments
                .Where(p => p.sys_commissionId == c.sys_commissionId)
                .Sum(p => (decimal?)p.sys_amountPaid) ?? 0
        }).ToList();

        var payments = await _context.tbl_CommissionPayments
            .Where(p => commissions.Select(c => c.sys_commissionId).Contains(p.sys_commissionId))
            .ToListAsync();

        return new JsonResult(new { agent, commissions, payments });
    }





    public async Task<IActionResult> OnPostPayCommissionsAsync(string commissionData, string paymentDate)
    {
        if (string.IsNullOrWhiteSpace(commissionData))
            return new JsonResult(new { success = false, message = "No commissions selected." });

        if (!DateTime.TryParse(paymentDate, out DateTime payDate))
            payDate = DateTime.UtcNow;

        // commissionData format: "id:amount,id:amount"
        var entries = commissionData.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(e =>
                        {
                            var parts = e.Split(':');
                            return (Id: int.Parse(parts[0]), Amount: double.Parse(parts[1]));
                        })
                        .ToList();

        var ids = entries.Select(e => e.Id).ToList();
        var commissions = await _context.tbl_CommissionsDue
                                .Where(c => ids.Contains(c.sys_commissionId))
                                .ToListAsync();

        foreach (var entry in entries)
        {
            var comm = commissions.FirstOrDefault(c => c.sys_commissionId == entry.Id);
            if (comm == null) continue;

            var alreadyPaid = await _context.tbl_CommissionPayments
                                .Where(p => p.sys_commissionId == comm.sys_commissionId)
                                .SumAsync(p => (double?)p.sys_amountPaid) ?? 0.0;

            var remaining = comm.sys_amount - alreadyPaid;
            if (remaining <= 0) continue;

            var payAmount = Math.Min(entry.Amount, remaining);

            _context.tbl_CommissionPayments.Add(new tbl_CommissionPayments
            {
                sys_commissionId = comm.sys_commissionId,
                sys_amountPaid = payAmount,
                sys_paymentDate = payDate
            });

            if (alreadyPaid + payAmount >= comm.sys_amount)
                comm.sys_isPaid = true;
        }

        await _context.SaveChangesAsync();

        var agentId = commissions.First().sys_agentId;
        var agent = await _context.tbl_Agents.FirstOrDefaultAsync(a => a.sys_agentId == agentId);

        var updatedCommissions = await _context.tbl_CommissionsDue
                                    .Where(c => c.sys_agentId == agentId)
                                    .ToListAsync();

        var commissionIdsAll = updatedCommissions.Select(c => c.sys_commissionId).ToList();
        var updatedPayments = await _context.tbl_CommissionPayments
                                    .Where(p => commissionIdsAll.Contains(p.sys_commissionId))
                                    .OrderByDescending(p => p.sys_paymentDate)
                                    .ToListAsync();

        return new JsonResult(new
        {
            success = true,
            message = "Payments recorded.",
            agent,
            commissions = updatedCommissions.Select(c => new {
                c.sys_commissionId,
                c.sys_graveId,
                c.sys_amount,
                c.sys_isPaid
            }),
            payments = updatedPayments.Select(p => new {
                p.sys_commissionPaymentId,
                p.sys_commissionId,
                p.sys_amountPaid,
                paymentDate = p.sys_paymentDate.ToString("yyyy-MM-dd")
            })
        });
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
