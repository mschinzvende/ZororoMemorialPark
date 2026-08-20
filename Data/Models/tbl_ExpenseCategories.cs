using System.ComponentModel.DataAnnotations;

namespace ZororoMemorialPark.Data.Models
{
    public class tbl_ExpenseCategories
    {
        [Key]
        public int sys_expenseCategoryId { get; set; }

        [Required]
        [MaxLength(100)]
        public string sys_expenseCategoryName { get; set; } = string.Empty;

        public int sys_displayOrder { get; set; }
    }
}
