namespace OptimizeBot.Contracts.Caching
{
    public interface ICacheManager
    {
        IUserCache UserCache { get; }
        IIDCache<int> IDCache { get; }
    }
}
