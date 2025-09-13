using System.ComponentModel.DataAnnotations;

namespace ZororoMemorialPark.Data.Models
{
    public class tbl_Invoices
    {
        [Key]
        public int sys_invoiceId { get; set; }
        public DateOnly sys_invoiceDate { get; set; }
        public string sys_invoiceNumber { get; set; }
        public int sys_customerId {  get; set; }
        public DateOnly sys_invoiceDueDate { get; set; }

    }
}
