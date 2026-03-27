using System.ComponentModel.DataAnnotations;

namespace ZororoMemorialPark.Data.Models
{
    public class tbl_CommissionsDue
    {
        [Key]

        public int sys_commissionId { get; set; }
        public int sys_graveId { get; set; }
        public int sys_agentId { get; set; }
        public double sys_amount { get; set; }

        public bool sys_isPaid { get; set; }

        public int sys_bookingId { get; set; }
    }
}
