using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BDTechMarket.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Category name is mandatory.")]
        [StringLength(100, ErrorMessage = "Category name cannot exceed 100 characters.")]
        [Display(Name = "Category Name")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Display Order")]
        [Range(1, 100, ErrorMessage = "Display order must be between 1 and 100.")]
        public int DisplayOrder { get; set; } = 1;

        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}