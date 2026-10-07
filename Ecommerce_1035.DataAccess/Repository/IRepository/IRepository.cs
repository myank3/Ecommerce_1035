using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Ecommerce_1035.DataAccess.Repository.IRepository
{
    public interface IRepository<T> where T : class
    {
        void Add(T entity);
        void Update(T entity);
        T Get(int id);

        IEnumerable<T> GetAll(Expression<Func<T, bool>> filter = null, Func<IQueryable<T>, IOrderedQueryable<T>> orderby = null, string includeProperties = null);

        T FirstOrDefault(Expression<Func<T, bool>> filter = null, string includedProperties = null);
        void Remove(T entity);
        void Remove(int id);
        void RemoveRange(IEnumerable<T> entities);

    }
}
