using System.ComponentModel.DataAnnotations;

namespace ZororoMemorialPark.Data.Models
{
    public class tbl_TombstoneInstallations
    {
        [Key]
        public int sys_installationId { get; set; }
        public int sys_saleId { get; set; }
        public DateOnly sys_installationDate { get; set; }
        public string sys_installedBy { get; set; }
        public string sys_remarks { get; set; }
        public string sys_status { get; set; } = "Pending"; // Pending, Completed, Verified

        public int sys_graveId { get; set; }
    }

}
