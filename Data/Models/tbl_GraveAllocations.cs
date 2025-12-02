using System.ComponentModel.DataAnnotations;

namespace ZororoMemorialPark.Data.Models
{
    public class tbl_GraveAllocations
    {
        [Key]
        public int sys_allocationId { get; set; }
        public int sys_graveId { get; set; }
        public int sys_deceasedId { get; set; }
        public DateOnly sys_allocatedOn { get; set; }
        public int sys_customerId { get; set; }
        public string sys_repatriationSource { get; set; }
        public DateTime sys_dateTimeOfBurial { get; set; }

        public string sys_burialOrder { get; set; }

        public string sys_tombstoneStatus { get; set; } = "Not Installed"; // Not Purchased, Purchased, Installed   
    }
}
 