// Models/RequisitionHistory.cs
using System;
using System.ComponentModel.DataAnnotations;

public class tbl_RequisitionHistory
{
    [Key]
    public int Id { get; set; }

    public int RequisitionId { get; set; }
    public tbl_Requisition? Requisition { get; set; }

    public int? WorkflowStepId { get; set; }
    public tbl_RequisitionWorkflowStep? WorkflowStep { get; set; }

    [Required]
    public string Action { get; set; } = string.Empty; // "Submitted", "Reviewed", "Approved", "Rejected"

    public string? Notes { get; set; }

    public string PerformedByUserId { get; set; } = string.Empty;
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
}
