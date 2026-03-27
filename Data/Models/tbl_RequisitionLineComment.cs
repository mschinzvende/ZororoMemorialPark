// Models/RequisitionLineComment.cs
using System;
using System.ComponentModel.DataAnnotations;

public class tbl_RequisitionLineComment
{
    [Key]
    public int Id { get; set; }

    public int RequisitionLineId { get; set; }
    public tbl_RequisitionLine? RequisitionLine { get; set; }

    public int WorkflowStepId { get; set; }
    public tbl_RequisitionWorkflowStep? WorkflowStep { get; set; }

    [Required]
    public string CommentText { get; set; } = string.Empty;

    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
