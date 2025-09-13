using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZororoMemorialPark.Data.Models
{
    public class tbl_JobEvaluations
    {
        [Key]
        public int sys_jobEvaluationId { get; set; }
        public int sys_jobEvaluationParameterId { get; set; }
        public int sys_jobId { get; set; }
        public string sys_jobEvaluation { get; set; }

        [ForeignKey(nameof(sys_jobEvaluationParameterId))]
        public tbl_jobEvaluationParameters tbl_jobEvaluationParameters { get; set; }

        [ForeignKey(nameof(sys_jobId))]
        public tbl_Jobs tbl_Jobs { get; set; }
    }
}
