using System.ComponentModel.DataAnnotations;

namespace ZororoMemorialPark.Data.Models
{
    public class tbl_Beneficiaries
    {
        [Key]
        public int sys_benefitId { get; set; }
        public string sys_benefitFullName { get; set; }
        public DateOnly sys_benefitDOB { get; set; }
        public string sys_benefitRelationship { get; set; }
        public int sys_reservationId { get; set; }
    }
}
