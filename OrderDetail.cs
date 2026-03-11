using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BDTechMarket.Models
{
    /// <summary>
    /// OrderDetail: Prottekta order-er individual product items track kore.
    /// Order place korar somoy product-er "Price Snapshot" ekhane save hoy.
    /// </summary>
    public class OrderDetail
    {
        [Key]
        public int Id { get; set; }

        // Relationship with the parent Order
        [Required]
        public int OrderId { get; set; }
        
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        // Relationship with the Product
        [Required]
        public int ProductId { get; set; }
        
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        // Mapping Count property to 'Quantity' column in DB
        [Required]
        [Column("Quantity")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int Count { get; set; }

        // Important: Price snapshot at the time of purchase
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
    }
}