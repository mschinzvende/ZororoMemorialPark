using System.ComponentModel.DataAnnotations;

namespace ZororoMemorialPark.Data.Models
{
    public class tbl_PaymentMethods
    {
        [Key]
        public int sys_paymentMethodId {  get; set; }
        public string sys_paymentMethodName { get; set; }
    }
}
