using System.Threading.Tasks;

namespace OptimizeBot.Contracts.Caching
{
    public interface ICacheBase<T>
    {
        T Get(object key);
        void Remove(object key);
        Task<T> CacheAsync(object key, T value);
        int Count();
    }
}
