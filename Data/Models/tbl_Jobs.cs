using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZororoMemorialPark.Data.Models
{
    public class tbl_Jobs
    {
        [Key]
        public int sys_jobId { get; set; }

        [ForeignKey("tbl_Grave")]
        public int sys_graveId { get; set; }

        [ForeignKey("tbl_Supplier")]
        public int sys_supplierId { get; set; }

        [ForeignKey("tbl_DevelopmentStage")]
        public int sys_stageId { get; set; }

        public string sys_jobStatus { get; set; }
        public DateOnly sys_startDate { get; set; }
        public DateOnly sys_endDate { get; set; }

        public tbl_Graves tbl_Grave { get; set; }
        public tbl_Suppliers tbl_Supplier { get; set; }
        public tbl_DevelopmentStages tbl_DevelopmentStage { get; set; }
    }
}
