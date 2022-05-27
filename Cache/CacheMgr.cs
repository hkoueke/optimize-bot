using OptimizeBot.Utils;
using System;
using System.Threading.Tasks;

namespace OptimizeBot.Cache
{
    public static class CacheMgr
    {
        private static readonly MemoryCacheWithPolicy objectCache = new();

        private static readonly MemoryCacheWithPolicy numberCache = new();

        public static async Task<T> GetOrCreateAsync<T>(long key, Func<T, bool> predicate, T toCreate) where T : class
        {
            var entry = objectCache.GetEntry<T>(key);
            if (entry != null) return await Task.FromResult(entry);

            entry = await objectCache.CacheAsync(key, await DbUtil.FindOrCreateAsync(predicate, toCreate));
            
            if (entry != null)
                Program.log.Info("Entry cached successfully");

            return entry;        
        }

        public static void RemoveEntry(in object key) => objectCache.RemoveEntry(key);

        public static T GetEntry<T>(in object key) where T : class => objectCache.GetEntry<T>(key);

        public static int? GetEntry(in object key) => numberCache.GetEntry(key);

        public static int SetOrRemoveEntry(in object key, in int? value = default)
        {
            if (!value.HasValue)
            {
                numberCache.RemoveEntry(key);
                return -1;
            }

            if (key == null)
                throw new ArgumentNullException(nameof(key));

            return numberCache.SetEntry(key, value.Value);
        }

        public static int Count() => objectCache.Count();
    }
}