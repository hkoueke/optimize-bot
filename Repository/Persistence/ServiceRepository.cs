using Microsoft.EntityFrameworkCore;
using OptimizeBot.Contracts.Persistance;
using OptimizeBot.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;


namespace OptimizeBot.Repository.Persistence
{
    public class ServiceRepository : RepositoryBase<Service>, IServiceRepository
    {
        public ServiceRepository(DbContext context) : base(context)
        {
        }

        public async Task<Service?> GetServiceAsync(Expression<Func<Service, bool>> condition,
                                                   bool trackChanges = false) => await FindByCondition(condition, trackChanges).SingleOrDefaultAsync();

        public async Task<List<Service>> GetServices(bool trackChanges = false) => await FindAll(trackChanges).ToListAsync();

        public async Task<List<Service>> GetServicesByCondition<TKey>(Expression<Func<Service, bool>> condition,
                                                                      Expression<Func<Service, TKey>>? orderBy,
                                                                      bool trackChanges = false)
        {
            IQueryable<Service> result = FindByCondition(condition, trackChanges);
            if (orderBy is not null) result.OrderBy(orderBy);
            return await result.ToListAsync();
        }
    }
}
