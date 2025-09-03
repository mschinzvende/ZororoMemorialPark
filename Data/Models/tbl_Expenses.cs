using System.ComponentModel.DataAnnotations;

namespace ZororoMemorialPark.Data.Models
{
    public class tbl_Expenses
    {
        [Key]
        public int sys_expenseId { get; set; }
        public DateOnly sys_date { get; set; }
        public int sys_supplierId { get; set; }
        public int sys_jobId { get; set; }
        public string sys_notes { get; set; }
        public double sys_amountDue { get; set; }
        public double sys_amountPaid { get; set; }
        public double sys_balanceDue { get; set; }

        public int sys_paymentMethodId { get; set; }

    }
}
