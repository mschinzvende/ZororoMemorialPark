using System.ComponentModel.DataAnnotations;

namespace ZororoMemorialPark.Data.Models
{
    public class tbl_DevelopmentStages
    {
        [Key]
        public int sys_stageId { get; set; }
        public string sys_stageName { get; set; }
        
    }
}
