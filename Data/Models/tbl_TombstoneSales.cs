using System.ComponentModel.DataAnnotations;

namespace ZororoMemorialPark.Data.Models
{
    public class tbl_TombstoneSales
    {
        [Key]
        public int sys_saleId { get; set; }

        public int? sys_graveId { get; set; }                   // null if not attached to any grave
        public int sys_tombstoneId { get; set; }
        public DateOnly sys_saleDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);
        public double sys_salePrice { get; set; }
        public string sys_buyerName { get; set; }
        public string sys_buyerContact { get; set; }
        public string sys_paymentStatus { get; set; } = "Pending";  // Pending, Paid, Completed
        public string sys_installationStatus { get; set; } = "Not Installed"; // Not Installed, Installed, Inspected
    }

}
