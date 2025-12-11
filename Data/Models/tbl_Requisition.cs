
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public enum RequisitionStatus
{
    Draft = 0,
    InProgress = 1,
    Approved = 2,
    Rejected = 3
}

public class tbl_Requisition
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string RequestNumber { get; set; } = string.Empty;

    [ValidateNever]
    public string RequesterUserId { get; set; } = string.Empty;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public RequisitionStatus Status { get; set; } = RequisitionStatus.Draft;

    // Workflow
    public int WorkflowDefinitionId { get; set; }
    public tbl_RequisitionWorkflowDefinition? WorkflowDefinition { get; set; }

    public int? CurrentStepId { get; set; }
    public tbl_RequisitionWorkflowStep? CurrentStep { get; set; }

    public DateTime? FinalizedAt { get; set; }
    public string? FinalizedByUserId { get; set; }

    public ICollection<tbl_RequisitionLine> Lines { get; set; } = new List<tbl_RequisitionLine>();
    public ICollection<tbl_RequisitionHistory> History { get; set; } = new List<tbl_RequisitionHistory>();
}
