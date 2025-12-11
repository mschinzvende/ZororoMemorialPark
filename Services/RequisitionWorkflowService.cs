using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ZororoMemorialPark.Data;

public interface IRequisitionWorkflowService
{
    Task<tbl_RequisitionWorkflowDefinition?> GetWorkflowForRequesterAsync(string requesterUserId);
    Task<tbl_RequisitionWorkflowStep?> GetFirstStepAsync(int workflowDefinitionId);
    Task<tbl_RequisitionWorkflowStep?> GetNextStepAsync(tbl_RequisitionWorkflowStep currentStep);
    Task<string> GenerateRequestNumberAsync();
}

public class RequisitionWorkflowService : IRequisitionWorkflowService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public RequisitionWorkflowService(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<tbl_RequisitionWorkflowDefinition?> GetWorkflowForRequesterAsync(string requesterUserId)
    {
        var user = await _userManager.FindByIdAsync(requesterUserId);
        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);

        var workflow = await _context.tbl_RequisitionWorkflowDefinitions
            .Include(w => w.Steps.OrderBy(s => s.StepOrder))
            .Where(w => w.IsActive && roles.Contains(w.RequesterRole))
            .OrderBy(w => w.Id)
            .FirstOrDefaultAsync();

        return workflow;
    }

    public async Task<tbl_RequisitionWorkflowStep?> GetFirstStepAsync(int workflowDefinitionId)
    {
        return await _context.tbl_RequisitionWorkflowSteps
            .Where(s => s.WorkflowDefinitionId == workflowDefinitionId)
            .OrderBy(s => s.StepOrder)
            .FirstOrDefaultAsync();
    }

    public async Task<tbl_RequisitionWorkflowStep?> GetNextStepAsync(tbl_RequisitionWorkflowStep currentStep)
    {
        return await _context.tbl_RequisitionWorkflowSteps
            .Where(s => s.WorkflowDefinitionId == currentStep.WorkflowDefinitionId
                     && s.StepOrder > currentStep.StepOrder)
            .OrderBy(s => s.StepOrder)
            .FirstOrDefaultAsync();
    }

    public async Task<string> GenerateRequestNumberAsync()
    {
        var today = DateTime.UtcNow.Date;
        var countToday = await _context.tbl_Requisitions.CountAsync(r => r.CreatedAt.Date == today);
        return $"REQ-{today:yyyyMMdd}-{countToday + 1:D4}";
    }
}
