using OptimizeBot.Contracts.Caching;
using OptimizeBot.Contracts.Persistance;
using OptimizeBot.Objects;

namespace OptimizeBot.Repository.Cache
{
    public class CacheManager : ICacheManager
    {
        private IUserCache? _userCache;
        private IIDCache<int>? _idCache;

        private readonly MemoryCacheWithPolicy _cache;
        private readonly IRepositoryManager _repositoryManager;

        public CacheManager(MemoryCacheWithPolicy cache, IRepositoryManager repositoryManager)
        {
            _cache = cache;
            _repositoryManager = repositoryManager;
        }

        public IUserCache UserCache => _userCache ??= new UserCache(_cache, _repositoryManager);

        public IIDCache<int> IDCache => _idCache ??= new IDCache(_cache);
    }
}
