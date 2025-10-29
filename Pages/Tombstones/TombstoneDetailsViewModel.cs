using ZororoMemorialPark.Data.Models;

namespace ZororoMemorialPark.Pages.Tombstones
{
    public class TombstoneDetailsViewModel
    {
        public int GraveId { get; set; }
        public tbl_Tombstones Tombstone { get; set; }
        public tbl_Invoices Invoice { get; set; }
        public List<tbl_TombstoneSales> Payments { get; set; }
        public string PaymentStatus { get; set; }
        public bool IsFullyPaid { get; set; }
    }
}
