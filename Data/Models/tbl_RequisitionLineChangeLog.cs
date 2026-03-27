// Models/RequisitionLineChangeLog.cs
using System;
using System.ComponentModel.DataAnnotations;

public class tbl_RequisitionLineChangeLog
{
    [Key]
    public int Id { get; set; }

    public int RequisitionLineId { get; set; }
    public tbl_RequisitionLine? RequisitionLine { get; set; }

    public int WorkflowStepId { get; set; }
    public tbl_RequisitionWorkflowStep? WorkflowStep { get; set; }

    public decimal OldQuantity { get; set; }
    public decimal NewQuantity { get; set; }

    public decimal OldUnitCost { get; set; }
    public decimal NewUnitCost { get; set; }

    public string ChangedByUserId { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
