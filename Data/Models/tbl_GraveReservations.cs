using System.ComponentModel.DataAnnotations;

namespace ZororoMemorialPark.Data.Models
{
    public class tbl_GraveReservations
    {
        [Key]
        public int sys_reservationId { get; set; }
        public int sys_graveId { get; set; }
        public int sys_customerId { get; set; }

        public DateOnly sys_reservedOn { get; set; }



    }
}
