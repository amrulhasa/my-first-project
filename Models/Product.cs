using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BDTechMarket.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Product name is required.")]
        [StringLength(150, ErrorMessage = "Product name cannot exceed 150 characters.")]
        [Display(Name = "Product Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Price is required.")]
        [Range(0.01, 999999999.99, ErrorMessage = "Please enter a valid price.")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Stock quantity is required.")]
        [Range(0, 100000, ErrorMessage = "Stock quantity must be between 0 and 100000.")]
        [Display(Name = "Stock Quantity")]
        public int StockQuantity { get; set; }

        [StringLength(50)]
        [Display(Name = "SKU")]
        public string? SKU { get; set; }

        [StringLength(100)]
        public string? Brand { get; set; }
        public int? BrandId { get; set; }

[ForeignKey(nameof(BrandId))]
public virtual Brand? BrandEntity { get; set; }
public virtual ICollection<ProductSpecification> Specifications { get; set; }
    = new List<ProductSpecification>();

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        [Required(ErrorMessage = "Please select a category.")]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public virtual Category? Category { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        [NotMapped]
        [Display(Name = "Product Image")]
        public IFormFile? ImageFile { get; set; }
    }
}