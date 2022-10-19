using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace OptimizeBot.Objects
{
    public sealed class MemoryCacheWithPolicy
    {
        private readonly MemoryCache _cache;
        private readonly ConcurrentDictionary<object, SemaphoreSlim> _locks;
        private readonly MemoryCacheEntryOptions _options;

        public MemoryCacheWithPolicy(long? sizeLimit, MemoryCacheEntryOptions options)
        {
            _cache = new(new MemoryCacheOptions { SizeLimit = sizeLimit });
            _locks = new();
            _options = options;
        }

        public async Task<T> CacheAsync<T>(object key, T item) where T : class
        {
            if (key is null) throw new ArgumentNullException(nameof(key));
            if (item is null) throw new ArgumentNullException(nameof(item));

            using SemaphoreSlim myLock = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

            try
            {
                await myLock.WaitAsync();
                return _cache.Set(key, item, _options);
            }
            catch (Exception) { throw; }
            finally { myLock.Release(); }
        }
        public T GetEntry<T>(object key) where T : class => _cache.Get<T>(key);
        public void RemoveEntry(object key) => _cache.Remove(key);
        public int SetEntry(object key, int value) => _cache.Set(key, value, _options);
        public int Count() => _cache.Count;
    }
}