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

        public int sys_bookingId { get; set; }

        public DateTime sys_createdOn { get; set; } = DateTime.Now;

        public string? sys_notes { get; set; }
    }
}
