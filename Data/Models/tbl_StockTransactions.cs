using System.ComponentModel.DataAnnotations;

namespace ZororoMemorialPark.Data.Models
{
    public class tbl_StockTransactions
    {
        [Key]
        public int sys_transactionId { get; set; }

        public int sys_itemId { get; set; } // FK → tbl_ProductsAndServices

        
        public DateTime sys_transactionDate { get; set; } = DateTime.Now;

       
        public string sys_transactionType { get; set; }
        // Possible values:
        // "Purchase" – when new stock is received
        // "Consumption" – when used internally (e.g., construction)
        // "Sale" – when sold to a customer
        // "Adjustment" – manual correction

        [Required]
        [Range(0.01, double.MaxValue)]
        public double sys_quantity { get; set; }

        public double sys_unitCost { get; set; } // For purchases or average costing
        

        public string sys_referenceNumber { get; set; } // e.g., InvoiceNo, JobNo, or GRNNo

       

        public string sys_notes { get; set; }

        // Optional status
        
    }
}
