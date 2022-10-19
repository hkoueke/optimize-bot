using Microsoft.EntityFrameworkCore;
using OptimizeBot.Contracts.Persistance;
using System.Threading;
using System.Threading.Tasks;

namespace OptimizeBot.Repository.Persistence
{
    public class RepositoryManager : IRepositoryManager
    {
        private readonly DbContext _context;
        private IUserRepository? _userRepository;
        private IServiceRepository? _serviceRepository;

        public RepositoryManager(DbContext context) => _context = context;

        public IUserRepository UserRepository => _userRepository ??= new UserRepository(_context);

        public IServiceRepository ServiceRepository => _serviceRepository ??= new ServiceRepository(_context);

        public async Task<bool> SaveAsync(CancellationToken cancellationToken = default)
            => await _context.SaveChangesAsync(cancellationToken) >= 0;
    }
}

