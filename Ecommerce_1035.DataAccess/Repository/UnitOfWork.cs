using Ecommerce_1035.Models.Models; 
using Ecommerce_1035.DataAccess.Repository.IRepository;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Collections.Generic;
using System.Text;
using Ecommerce_1035.DataAccess.Data;

namespace Ecommerce_1035.DataAccess.Repository
{
    public class UnitOfWork : IUnitofWork
    {
        private readonly ApplicationDbContext _context;
        public ICategoryRepository Category { get; private set; }

        public ICoverTypesRepository CoverTypes { get; private set; }

        public IProductRepository Product { get; private set; }

        public IOrderDetailRepository OrderDetail { get; private set; }
        public IOrderHeaderRepository OrderHeader { get; private set; }
        public IShoppingCartRepository ShoppingCart { get; private set; }

        public IApplicationUserRepository ApplicationUser { get; private set; }
        public ICompanyRepository Company { get; private set; }
        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
            Category = new CategoryRepository(_context);
            CoverTypes = new CoverTypesRepository(_context); OrderDetail = new OrderDetailRepository(_context);
            OrderHeader = new OrderHeaderRepository(_context);
            Product = new ProductRepository(_context);
            ShoppingCart = new ShoppingCartRepository(_context);
            ApplicationUser = new ApplicationUserRepository(_context);
            Company = new CompanyRepository(_context);
        }
        public void Save()
        {
            _context.SaveChanges();
        }
    }
}
