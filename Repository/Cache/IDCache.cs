using OptimizeBot.Contracts.Caching;
using OptimizeBot.Objects;
using System.Threading.Tasks;

namespace OptimizeBot.Repository.Cache
{
    public class IDCache : CacheBase<Wrapper<int>>, IIDCache<int>
    {
        public IDCache(MemoryCacheWithPolicy cache) : base(cache) { }

        public async Task<int> CacheIdAsync(object key, int value) => await CacheAsync(key, value);

        public int GetId(object key) => Get(key);

        public void RemoveId(object key) => Remove(key);
    }
}