using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce_1035.Models.Models.ViewModel
{
    public class ProductVM
    {
        public Product Product { get; set; }


        public IEnumerable<SelectListItem> CoverTypeList { get; set; }

        public IEnumerable<SelectListItem> CategoryList { get; set; }
    }
}
