using System.ComponentModel.DataAnnotations;

namespace ZororoMemorialPark.Data.Models
{
    public class tbl_Tombstones
    {
        [Key]
        public int sys_tombstoneId { get; set; }

        public string sys_tombstoneName { get; set; }          // e.g. “Classic Marble A1”
        public string sys_description { get; set; }
        public string sys_material { get; set; }                // e.g. “Granite”, “Marble”
        public string sys_size { get; set; }                    // e.g. “1.2m x 0.8m”
        public double sys_price { get; set; }
        public int sys_quantityAvailable { get; set; }          // stock count
        public bool sys_isCustomizable { get; set; }            // optional, e.g. engraving
        public string sys_imagePath { get; set; }               // display on UI

        public string sys_status { get; set; } = "Available";   // Available, Low Stock, Out of Stock
    }

}
