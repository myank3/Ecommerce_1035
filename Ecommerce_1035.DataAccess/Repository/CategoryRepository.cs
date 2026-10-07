using Ecommerce_1035.Models.Models;
using Ecommerce_1035.DataAccess.Repository.IRepository; 
using System;
using System.Collections.Generic;
using System.Text;
using Ecommerce_1035.DataAccess.Data;

namespace Ecommerce_1035.DataAccess.Repository
{
    public class CategoryRepository : Repository<Category>, ICategoryRepository
    {
        public CategoryRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
