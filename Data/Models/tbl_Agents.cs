using System.ComponentModel.DataAnnotations;

namespace ZororoMemorialPark.Data.Models
{
    public class tbl_Agents
    {
        [Key]
        public int sys_agentId { get; set; }
        public string sys_agentName { get; set; }
        public string sys_agentContactPerson { get; set; }
        public string sys_agentPhone { get; set; }
        public string sys_agentEmail { get; set; }
        public string sys_agentAddress { get; set; }
        public string UserId { get; set; }

        public int sys_commission { get; set; }
        public bool sys_isPercentage { get; set; }

        public string? sys_whatsAppPinHash { get; set; }

        public bool sys_whatsAppPinSet { get; set; }

        public bool sys_isActive { get; set; } = true;


    }
}
