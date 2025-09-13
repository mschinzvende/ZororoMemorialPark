using System.ComponentModel.DataAnnotations;

namespace ZororoMemorialPark.Data.Models
{
    public class tbl_InvoiceItems
    {
        [Key]
        public int sys_itemId { get; set; }
        public int sys_invoiceId { get; set; }
        public string sys_itemName { get; set; }

        public double sys_itemCost { get; set; }
    }
}
