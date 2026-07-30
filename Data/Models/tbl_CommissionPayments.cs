using System.ComponentModel.DataAnnotations;

namespace ZororoMemorialPark.Data.Models
{
    public class tbl_CommissionPayments
    {
        [Key]
        public int sys_commissionPaymentId { get; set; }

        public int sys_agentId { get; set; }

        public DateTime sys_paymentDate { get; set; }

        public double sys_amountPaid { get; set; }

        [MaxLength(100)]
        public string? sys_reference { get; set; }

        [MaxLength(500)]
        public string? sys_notes { get; set; }

        public string? sys_recordedByUserId { get; set; }

        public DateTime sys_createdOn { get; set; } = DateTime.Now;

    }
}
