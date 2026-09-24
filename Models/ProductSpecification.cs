using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BDTechMarket.Models
{
    public class ProductSpecification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public virtual Product? Product { get; set; }

        [Required(ErrorMessage = "Specification name is required.")]
        [StringLength(
            100,
            ErrorMessage = "Specification name cannot exceed 100 characters.")]
        [Display(Name = "Specification Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Specification value is required.")]
        [StringLength(
            1000,
            ErrorMessage = "Specification value cannot exceed 1000 characters.")]
        [Display(Name = "Value")]
        public string Value { get; set; } = string.Empty;

        [Range(
            1,
            1000,
            ErrorMessage = "Display order must be between 1 and 1000.")]
        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; } = 1;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }
    }
}