using System.ComponentModel.DataAnnotations;

namespace ZororoMemorialPark.Data.Models
{
    public class tbl_jobEvaluationParameters
    {
        [Key]
        public int sys_jobEvaluationParameterId { get; set; }
        public string sys_parameterName { get; set; }
    }
}
