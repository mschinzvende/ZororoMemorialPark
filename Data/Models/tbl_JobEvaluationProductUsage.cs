using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ZororoMemorialPark.Data.Models
{
    public class tbl_JobEvaluationProductUsage
    {
        [Key]
        public int sys_usageId { get; set; }

        public int sys_jobId { get; set; }
        public int sys_jobEvaluationParameterId { get; set; }
        public int sys_itemId { get; set; }

        public double sys_quantityUsed { get; set; }
        public DateTime sys_dateUsed { get; set; } = DateTime.Now;
    }

}
