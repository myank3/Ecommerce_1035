using Ecommerce_1035.Models.Models;
using Ecommerce_1035.Models.Models; 
using Ecommerce_1035.DataAccess.Repository.IRepository;
 
using System;
using System.Collections.Generic;
using System.Text;
using Ecommerce_1035.DataAccess.Data;

namespace Ecommerce_1035.DataAccess.Repository
{
    public class CoverTypesRepository : Repository<CoverTypes>, ICoverTypesRepository
    {
        public CoverTypesRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
