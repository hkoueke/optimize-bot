using System.Threading.Tasks;

namespace OptimizeBot.Contracts.Caching
{
    public interface IIDCache<T> where T : struct
    {
        T GetId(object key);
        void RemoveId(object key);
        Task<T> CacheIdAsync(object key, T value);
    }
}
