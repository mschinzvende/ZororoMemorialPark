using System.ComponentModel.DataAnnotations;

namespace ZororoMemorialPark.Data.Models
{
    public class tbl_GraveReservationInvoices
    {
        [Key]
        public int sys_GRInvoicesId { get; set; }
        public int sys_invoiceId { get; set; }
        public int sys_reservationId { get; set; }
    }
}
