using System.ComponentModel.DataAnnotations;

namespace ZororoMemorialPark.Data.Models
{
    public class tbl_InventoryAuditTrail
    {
        [Key]
        public int sys_auditId { get; set; }
        public int sys_itemId { get; set; }
        public int sys_jobId { get; set; }
        public string sys_action { get; set; }
        public double sys_beforeQty { get; set; }
        public double sys_afterQty { get; set; }
        public double sys_changeQty { get; set; }

        public DateTime sys_date { get; set; }
    }
}
