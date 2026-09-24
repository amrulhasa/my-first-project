using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BDTechMarket.Models
{
    /// <summary>
    /// Category Model: Marketplace-er product classification handle kore.
    /// Enables organized browsing and better SEO for the tech market.
    /// </summary>
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Category name is mandatory.")]
        [StringLength(100, ErrorMessage = "Category name cannot exceed 100 characters.")]
        [Display(Name = "Category Name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// DisplayOrder: UI-te kemon serial-e category gulo dekhabe sheta control korar jonno.
        /// </summary>
        [Display(Name = "Display Order")]
        [Range(1, 100, ErrorMessage = "Display order must be between 1 and 100.")]
        public int DisplayOrder { get; set; } = 0;

        /// <summary>
        /// Navigation property representing the products linked to this category.
        /// Initialized as a list to prevent null reference errors during data entry.
        /// </summary>
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}