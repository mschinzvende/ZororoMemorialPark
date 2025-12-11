// Models/RequisitionWorkflowDefinition.cs
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class tbl_RequisitionWorkflowDefinition
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    // e.g. map by requester role; you can change to department, cost centre, etc.
    [Required]
    [MaxLength(100)]
    public string RequesterRole { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<tbl_RequisitionWorkflowStep> Steps { get; set; } = new List<tbl_RequisitionWorkflowStep>();
}
