using OptimizeBot.Contracts.Caching;
using OptimizeBot.Objects;
using System.Threading.Tasks;

namespace OptimizeBot.Repository.Cache
{
    public abstract class CacheBase<T> : ICacheBase<T> where T : class
    {
        private readonly MemoryCacheWithPolicy _cache;
        protected CacheBase(MemoryCacheWithPolicy cache) => _cache = cache;
        public void Remove(object key) => _cache.RemoveEntry(key);
        public int Count() => _cache.Count();
        public T Get(object key) => _cache.GetEntry<T>(key);
        public async Task<T> CacheAsync(object key, T value) => await _cache.CacheAsync(key, value);
    }
}
