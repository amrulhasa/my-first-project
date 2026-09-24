using System.Collections.Generic;

namespace BDTechMarket.Models.ViewModels
{
    public class ShoppingCartVM
    {
        // Controller er logic er sathe mil rekhe ListCart rakha holo
        public IEnumerable<CartItem> ListCart { get; set; } = new List<CartItem>();
        
        public decimal OrderTotal { get; set; }
        
        public Order Order { get; set; } = new Order();
    }
}