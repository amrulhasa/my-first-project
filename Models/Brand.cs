using System.ComponentModel.DataAnnotations;

namespace BDTechMarket.Models
{
    public class Brand
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Brand name is required.")]
        [StringLength(
            100,
            ErrorMessage = "Brand name cannot exceed 100 characters."
        )]
        [Display(Name = "Brand Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Logo URL")]
        public string? LogoUrl { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public int DisplayOrder { get; set; } = 1;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<Product> Products { get; set; }
            = new List<Product>();
    }
}