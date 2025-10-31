using System.ComponentModel.DataAnnotations;

namespace ZororoMemorialPark.Data.Models
{
    public class tbl_ProductsAndServices
    {
        [Key]
        public int sys_itemId { get; set; }

        
        public string sys_itemName { get; set; }    // e.g. "Grave Maintenance", "Cement Bag", "Flower Vase"

        public string sys_description { get; set; }

       
        public string sys_itemType { get; set; }     // "Product" or "Service"

      
        public string sys_category { get; set; }     // "Sellable" or "Consumable"

     
        public double sys_unitCost { get; set; }     // Cost to you (for consumables)

        
        public double sys_sellingPrice { get; set; } // Price to customer (for sellables)

        public int sys_stockQuantity { get; set; }  // Only for products
        public string sys_unitOfMeasure { get; set; } // e.g., "Bag", "Piece", "Service"

        

        public bool sys_isActive { get; set; } = true;
    }
}
