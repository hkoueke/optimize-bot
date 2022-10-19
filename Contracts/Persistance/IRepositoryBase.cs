using System;
using System.Linq;
using System.Linq.Expressions;

namespace OptimizeBot.Contracts.Persistance
{
    public interface IRepositoryBase<T>
    {
        IQueryable<T> FindAll(bool trackChanges = default);
        IQueryable<T> FindByCondition(Expression<Func<T, bool>> condition, bool trackChanges = default);
        void Create(T entity);
        void Update(T entity);
        void Delete(T entity);
    }
}
