using System.ComponentModel.DataAnnotations;

namespace ZororoMemorialPark.Data.Models
{
    public class tbl_Deceased
    {
        [Key]
        public int sys_deceasedId {  get; set; }
        public string sys_deceasedFullName { get; set; }
        public DateOnly sys_deceasedDOB { get; set; }
        public DateOnly sys_deceasedDOD { get; set; }
        public string sys_deceasedGender { get; set; }
        public string sys_deceasedNextOfKinName { get; set; }
        public string sys_deceasedNextOfKinAddress { get; set; }
        public string sys_deceasedNextOfKinEmail {  get; set; }
        public string sys_deceasedNextOfKinPhone { get; set; }
        public string sys_deceasedMaritalStatus { get; set; }
        public string sys_deceasedNameOfSpouse { get; set; }
        public string sys_deceasedNationality { get; set; }
    }
}
