using Ecommerce_1035.Models.Models;
using Ecommerce_1035.DataAccess.Repository.IRepository;
using Ecommerce_1035.Models.Models;
using System;
using System.Collections.Generic;
using System.Text;
using Ecommerce_1035.DataAccess.Data;

namespace Ecommerce_1035.DataAccess.Repository
{
    public class CompanyRepository : Repository<Company>, ICompanyRepository
    {
        public CompanyRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
