// Models/RequisitionLine.cs
using System;
using System.ComponentModel.DataAnnotations;

public class tbl_RequisitionLine
{
    [Key]
    public int Id { get; set; }

    public int RequisitionId { get; set; }
    public tbl_Requisition? Requisition { get; set; }

    [Required]
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    // Original values (what requester submitted)
    public decimal OriginalQuantity { get; set; }
    public decimal OriginalUnitCost { get; set; }
    public decimal OriginalTotal => OriginalQuantity * OriginalUnitCost;

    // Current values (after reviews/approvals)
    public decimal CurrentQuantity { get; set; }
    public decimal CurrentUnitCost { get; set; }
    public decimal CurrentTotal => CurrentQuantity * CurrentUnitCost;

    // Latest comment on this line (for quick display)
    public string? LatestComment { get; set; }
}
