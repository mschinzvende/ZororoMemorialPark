using System.ComponentModel.DataAnnotations;

namespace ZororoMemorialPark.Data.Models
{
    public class tbl_CommissionPayments
    {
        [Key]
        public int sys_paymentId { get; set; }
        public int sys_commissionId { get; set; }
        public DateTime sys_paymentDate { get; set; }
        public double sys_amountPaid { get; set; }
    }
}
