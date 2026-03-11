using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace BDTechMarket.Models
{
    public class ApplicationUser : IdentityUser
    {
        [PersonalData]
        public string? FullName { get; set; }

        [PersonalData]
        public string? Address { get; set; }

        // Navigation property for User's Orders
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}