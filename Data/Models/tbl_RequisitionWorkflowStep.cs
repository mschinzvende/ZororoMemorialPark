// Models/RequisitionWorkflowStep.cs
using System.ComponentModel.DataAnnotations;

public enum WorkflowStepType
{
    Review = 1,
    Approval = 2
}

public class tbl_RequisitionWorkflowStep
{
    [Key]
    public int Id { get; set; }

    public int WorkflowDefinitionId { get; set; }
    public tbl_RequisitionWorkflowDefinition? WorkflowDefinition { get; set; }

    public int StepOrder { get; set; } // 1,2,3...

    public WorkflowStepType StepType { get; set; }

    // The specific user responsible at this step
    [Required]
    public string AssignedToUserId { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? StepName { get; set; }
}
