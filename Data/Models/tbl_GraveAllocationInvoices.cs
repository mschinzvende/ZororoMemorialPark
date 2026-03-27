using System.ComponentModel.DataAnnotations;

namespace ZororoMemorialPark.Data.Models
{
    public class tbl_GraveAllocationInvoices
    {
        [Key]
        public int sys_GAInvoicesId { get; set; }
        public int sys_invoiceId { get; set; }
        public int? sys_allocationId { get; set; }
    }
}
