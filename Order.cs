using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BDTechMarket.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? ApplicationUser { get; set; }

        [Required]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Full Name is required.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        public string Phone { get; set; } = string.Empty;

        // Email field add করা হলো যাতে ভিউতে এরর না আসে
        [EmailAddress]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Shipping address cannot be empty.")]
        public string Address { get; set; } = string.Empty;

        public string PaymentMethod { get; set; } = "COD";
        public string PaymentStatus { get; set; } = "Pending";
        public string? TransactionId { get; set; }

        public decimal OrderTotal { get; set; }
        public decimal DeliveryFee { get; set; } = 60.00m;

        public string OrderStatus { get; set; } = "Pending";

        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}