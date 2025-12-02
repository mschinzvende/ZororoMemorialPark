using System.ComponentModel.DataAnnotations;

namespace ZororoMemorialPark.Data.Models
{
    public class tbl_IncomePayments
    {
        [Key]
        public int sys_incomeId { get; set; }
        public int sys_invoiceId { get; set; }
        public DateOnly sys_date { get; set; }
        public double sys_amount { get; set; }
        public string sys_paymentMethod { get; set; }
        public string sys_notes { get; set; }

        public int sys_receiptNumber { get; set; }
    }
}
