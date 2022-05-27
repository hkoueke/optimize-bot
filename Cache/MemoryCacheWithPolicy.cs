using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace OptimizeBot.Cache
{
    public sealed class MemoryCacheWithPolicy
    {
        private readonly MemoryCache _cache;
        private readonly ConcurrentDictionary<object, SemaphoreSlim> _locks;
        private readonly MemoryCacheEntryOptions _options;

        public MemoryCacheWithPolicy()
        {
            _cache = new(new MemoryCacheOptions { SizeLimit = 1024 });
            _locks = new();
            _options = new MemoryCacheEntryOptions
            {
                Size = 1,
                Priority = CacheItemPriority.High,
                SlidingExpiration = TimeSpan.FromHours(Constants.CACHE_EXPIRY_HOURS),
                AbsoluteExpiration = DateTimeOffset.Now.AddDays(Constants.CACHE_EXPIRY_DAYS)
            };
        }

        public async Task<T> CacheAsync<T>(object key, T item) where T : class
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (key == null)
                throw new ArgumentNullException(nameof(key));

            var myLock = _locks.GetOrAdd(key, v => new SemaphoreSlim(1, 1));
            await myLock.WaitAsync();

            try
            {
                return _cache.Set(key, item, _options);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                myLock.Release();
            }
        }

        public T GetEntry<T>(object key) where T : class => _cache.Get<T>(key);
        public int? GetEntry(object key) => (int?)_cache.Get(key);
        public void RemoveEntry(object key) => _cache.Remove(key);
        public int SetEntry(in object key, in int value) => _cache.Set(key, value, _options);
        public int Count() => _cache.Count;
    }
}