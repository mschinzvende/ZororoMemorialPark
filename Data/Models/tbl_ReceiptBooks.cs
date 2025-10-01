using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZororoMemorialPark.Data.Models
{
    public class tbl_ReceiptBooks
    {
        [Key]
        public int sys_receiptBookId { get; set; }

        public int sys_agentId { get; set; }   // which agent the book belongs to

        public string sys_bookNumber { get; set; } // identifier for the book (e.g. "RB-2025-01")

        public int sys_startNumber { get; set; }   // first receipt number in the book
        public int sys_endNumber { get; set; }     // last receipt number in the book
        public int sys_currentNumber { get; set; } // last used receipt number (updated on each payment)

        public DateTime sys_issueDate { get; set; }
        public bool sys_isClosed { get; set; }     // mark book as finished when all numbers used

        // Navigation property

        [ForeignKey("sys_agentId")]
        public tbl_Agents? Agent { get; set; }
    }

}
