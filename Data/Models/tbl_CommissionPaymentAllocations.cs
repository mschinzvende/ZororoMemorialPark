using System.ComponentModel.DataAnnotations;

namespace ZororoMemorialPark.Data.Models
{
    public class tbl_CommissionPaymentAllocations
    {
        [Key]
        public int sys_allocationId { get; set; }

        public int sys_commissionPaymentId { get; set; }

        public int sys_commissionId { get; set; }

        public double sys_amountAllocated { get; set; }
    }
}
