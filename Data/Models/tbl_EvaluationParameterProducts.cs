using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ZororoMemorialPark.Data.Models
{
    public class tbl_EvaluationParameterProducts
    {
        [Key]
        public int sys_evalParamProductId { get; set; }

        public int sys_jobEvaluationParameterId { get; set; }  // the parameter (e.g. “Grave Cleaning”)
        public int sys_itemId { get; set; }                 // the product (e.g. “Detergent”)

        public double sys_defaultQty { get; set; } = 1;        // expected usage when performing this parameter

        [ForeignKey(nameof(sys_jobEvaluationParameterId))]
        public tbl_jobEvaluationParameters tbl_jobEvaluationParameters { get; set; }

        [ForeignKey(nameof(sys_itemId))]
        public tbl_ProductsAndServices tbl_ProductsAndServices { get; set; }
    }
}
