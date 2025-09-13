using System.ComponentModel.DataAnnotations;

namespace ZororoMemorialPark.Data.Models
{
    public class tbl_Customers
    {
        [Key]
        public int sys_customerId { get; set; }
        public string sys_paymentMadeBy { get; set; }
    }
}
