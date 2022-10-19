using OptimizeBot.Model;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace OptimizeBot.Contracts.Persistance
{
    public interface IServiceRepository
    {
        Task<Service?> GetServiceAsync(Expression<Func<Service, bool>> condition, bool trackChanges = default);
        Task<List<Service>> GetServices(bool trackChanges = default);
        Task<List<Service>> GetServicesByCondition<TKey>(Expression<Func<Service, bool>> condition,
            Expression<Func<Service, TKey>>? orderBy,
            bool trackChanges = default);

    }
}
