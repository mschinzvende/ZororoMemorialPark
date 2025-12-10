using System;
using System.ComponentModel.DataAnnotations;

namespace ZororoMemorialPark.Data.Models
{
    public class tbl_Requisitions
    {
        [Key]
        public int sys_requisitionId { get; set; }

        [Required]
        public string sys_requestedByUserId { get; set; }

        public DateTime sys_requestDate { get; set; } = DateTime.Now;

        [Required]
        [MaxLength(200)]
        public string sys_title { get; set; }

        public string sys_description { get; set; }

        // Overall header status:
        // Pending, PendingLevel2, PartiallyApproved, Approved, Rejected, Paid
        [Required]
        [MaxLength(30)]
        public string sys_status { get; set; } = "Pending";

        public string? sys_rejectReason { get; set; }

        public string? sys_attachmentPath { get; set; }

        // Approval chain
        [Required]
        public string sys_approver1UserId { get; set; }

        public string? sys_approver2UserId { get; set; }

        // 1,2 or null when done
        public int? sys_currentApproverOrder { get; set; } = 1;

        public DateTime? sys_approver1CompletedOn { get; set; }
        public DateTime? sys_approver2CompletedOn { get; set; }

        // Payment fields
        public string? sys_paymentReference { get; set; }
        public DateTime? sys_paymentDate { get; set; }
        public string? sys_paidByUserId { get; set; }
    }
}
