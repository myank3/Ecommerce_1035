using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce_1035.Models.Models.ViewModel
{
    public class OrderVM
    {
        public OrderHeader OrderHeader { get; set; }
        public IEnumerable<OrderDetail> OrderDetailList { get; set; }
    }
}
