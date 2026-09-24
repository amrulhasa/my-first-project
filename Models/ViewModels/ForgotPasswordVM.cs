using System.ComponentModel.DataAnnotations;

namespace BDTechMarket.Models.ViewModels
{
    public class ForgotPasswordVM
    {
        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;
    }
}