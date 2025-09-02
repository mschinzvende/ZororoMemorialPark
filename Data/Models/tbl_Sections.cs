using System.ComponentModel.DataAnnotations;

namespace ZororoMemorialPark.Data.Models
{
    public class tbl_Sections
    {
        [Key]
        public int sys_sectionId { get; set; }
        public string sys_sectionName { get; set; } 
    }
}
