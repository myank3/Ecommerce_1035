using Ecommerce_1035.DataAccess.Data;
using Ecommerce_1035.DataAccess.Repository.IRepository;
using Ecommerce_1035.Models.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce_1035.DataAccess.Repository
{
    public class OrderDetailRepository : Repository<OrderDetail>, IOrderDetailRepository
    {
        public OrderDetailRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
