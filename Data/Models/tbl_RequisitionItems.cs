using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZororoMemorialPark.Data.Models
{
    public class tbl_RequisitionItems
    {
        [Key]
        public int sys_itemId { get; set; }

        [Required]
        public int sys_requisitionId { get; set; }

        [ForeignKey(nameof(sys_requisitionId))]
        public tbl_Requisitions Requisition { get; set; }

        [Required]
        [MaxLength(200)]
        public string ItemName { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;

        [Range(0.01, double.MaxValue)]
        public double CostPerUnit { get; set; }

        public double TotalCost { get; set; }

        // Pending, Approved, Rejected
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending";

        public double? ApprovedAmount { get; set; }

        public string? Approver1Notes { get; set; }
        public bool ReviewedByApprover1 { get; set; }

        public string? Approver2Notes { get; set; }
        public bool ReviewedByApprover2 { get; set; }
    }
}
