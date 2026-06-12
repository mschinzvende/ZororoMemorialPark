using System.ComponentModel.DataAnnotations;

namespace ZororoMemorialPark.Data.Models
{
    public class tbl_Deceased
    {
        [Key]
        public int sys_deceasedId {  get; set; }
        public string sys_deceasedFullName { get; set; }
        public string sys_deceasedGender { get; set; }
        public string sys_deceasedNextOfKinAddress { get; set; }
        public string? sys_burialOrderNo { get; set; }
        public DateOnly sys_deceasedDOD { get; set; }
        public string? sys_minister { get; set; }
        public string? sys_undertaker { get; set; }
        public string? sys_depth { get; set; }
        public string sys_deceasedNextOfKinName { get; set; }
        public string sys_deceasedNextOfKinPhone { get; set; }
        public string sys_deceasedNationality { get; set; }


    }
}
