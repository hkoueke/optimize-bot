using System.Threading;
using System.Threading.Tasks;

namespace OptimizeBot.Contracts.Persistance
{
    public interface IRepositoryManager
    {
        IUserRepository UserRepository { get; }
        IServiceRepository ServiceRepository { get; }
        Task<bool> SaveAsync(CancellationToken cancellationToken = default);
    }
}
