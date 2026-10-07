using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce_1035.DataAccess.Repository.IRepository
{
    public interface IUnitofWork
    {
        ICategoryRepository Category { get; }
        ICoverTypesRepository CoverTypes { get; }
         
        ICompanyRepository Company { get; }
        IShoppingCartRepository ShoppingCart { get; }

        IOrderDetailRepository OrderDetail { get; }
        IOrderHeaderRepository OrderHeader { get; }

        IProductRepository Product { get; }
       
        IApplicationUserRepository ApplicationUser { get; }
        void Save();
    }
}
