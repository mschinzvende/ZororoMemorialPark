using System.ComponentModel.DataAnnotations;

namespace ZororoMemorialPark.Data.Models
{
    public class tbl_Expenses
    {
        [Key]
        public int sys_expenseId { get; set; }
        public DateOnly sys_date { get; set; }
        // Construction costs can still be linked to a supplier and grave-development job,
        // while daily operating expenses can be captured independently.
        public int? sys_supplierId { get; set; }
        public int? sys_jobId { get; set; }
        // Daily operating expenses are paid against a line that has completed
        // the requisition approval workflow. Construction payments continue to
        // be governed by their job-payment workflow.
        public int? sys_requisitionId { get; set; }
        public int? sys_requisitionLineId { get; set; }
        public string sys_expenseCategory { get; set; } = "General";
        public string sys_payeeName { get; set; } = string.Empty;
        public string sys_reference { get; set; } = string.Empty;
        public string sys_notes { get; set; } = string.Empty;
        public double sys_amountDue { get; set; }
        public double sys_amountPaid { get; set; }
        public double sys_balanceDue { get; set; }

        public int sys_paymentMethodId { get; set; }

    }
}
