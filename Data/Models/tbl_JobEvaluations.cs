using System.ComponentModel.DataAnnotations;

namespace ZororoMemorialPark.Data.Models
{
    public class tbl_JobEvaluations
    {
        [Key]
        public int sys_jobEvaluationId { get; set; }
        public int sys_jobEvaluationParameterId { get; set; }
        public int sys_jobId { get; set; }
        public string sys_jobEvaluation { get; set; }

        public tbl_jobEvaluationParameters tbl_jobEvaluationParameters { get; set; }
        public tbl_Jobs tbl_Jobs { get; set; }
    }
}
