using System.ComponentModel.DataAnnotations;

namespace ZororoMemorialPark.Data.Models
{
    public class tbl_Graves
    {
        [Key]
        public int sys_graveId { get; set; }
        public int sys_stageId { get; set; }

        public int sys_previousStageId { get; set; }
        public int sys_sectionId { get; set; }
        public string sys_plotNumber { get; set; }
        public string sys_graveSaleStatus { get; set; }
       
        public double sys_latitude { get; set; }
        public double sys_longitude { get; set; }

    }
}
