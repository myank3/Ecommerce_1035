using Ecommerce_1035.DataAccess.Data;
using Ecommerce_1035.DataAccess.Repository.IRepository;
using Ecommerce_1035.Models.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce_1035.DataAccess.Repository
{
    public class OrderHeaderRepository : Repository<OrderHeader>, IOrderHeaderRepository
    {
        public OrderHeaderRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
