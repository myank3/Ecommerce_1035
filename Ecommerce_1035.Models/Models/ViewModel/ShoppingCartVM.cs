using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce_1035.Models.Models.ViewModel
{
    public class ShoppingCartVM
    {
        public IEnumerable<ShoppingCart> ListCart { get; set; }
        public OrderHeader OrderHeader { get; set; }
    }
}
