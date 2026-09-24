using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BDTechMarket.Models
{
    public class CartItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }
        
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Range(1, 100)]
        public int Count { get; set; }

        // Logic-based properties
        [NotMapped]
        public decimal Price => Product?.Price ?? 0;

        [NotMapped]
        public decimal Total => Count * Price;
    }
}