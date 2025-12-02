using System.ComponentModel.DataAnnotations;

namespace ZororoMemorialPark.Data.Models
{
    public class tbl_Sections
    {
        [Key]
        public int sys_sectionId { get; set; }
        public string sys_sectionName { get; set; } 

        public string sys_jobReminder { get; set; }
        public int sys_reminderPeriod { get; set; }
    }
}
