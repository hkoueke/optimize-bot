using Microsoft.EntityFrameworkCore;
using OptimizeBot.Contracts.Persistance;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace OptimizeBot.Repository.Persistence
{
    public abstract class RepositoryBase<T> : IRepositoryBase<T> where T : class
    {
        protected readonly DbContext Context;

        protected RepositoryBase(DbContext context) => Context = context;

        public void Create(T entity) => Context.Set<T>().Add(entity);

        public void Delete(T entity) => Context.Set<T>().Remove(entity);

        public IQueryable<T> FindAll(bool trackChanges = false)
            => trackChanges ? Context.Set<T>() : Context.Set<T>().AsNoTracking();

        public IQueryable<T> FindByCondition(Expression<Func<T, bool>> condition, bool trackChanges = false)
            => trackChanges ? Context.Set<T>().Where(condition) : Context.Set<T>().Where(condition).AsNoTracking();

        public void Update(T entity) => Context.Set<T>().Update(entity);
    }
}
