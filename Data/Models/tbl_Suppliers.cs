using System.ComponentModel.DataAnnotations;

namespace ZororoMemorialPark.Data.Models
{
    public class tbl_Suppliers
    {   
        [Key]
        public int sys_supplierId { get; set; }
        public string sys_supplierName { get; set; }

        public string sys_supplierAddress { get; set; } = "Zororo";
        public string sys_supplierPhone { get; set; } = "263000000000";
        public string sys_supplierEmail { get; set; } = "supplier@mail.com";
       
    }
}
