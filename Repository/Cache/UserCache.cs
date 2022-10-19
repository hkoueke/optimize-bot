using OptimizeBot.Contracts.Caching;
using OptimizeBot.Contracts.Persistance;
using OptimizeBot.Extensions;
using OptimizeBot.Objects;
using System.Threading.Tasks;
using TelegramUser = Telegram.Bot.Types.User;
using User = OptimizeBot.Model.User;

namespace OptimizeBot.Repository.Cache
{
    public class UserCache : CacheBase<User>, IUserCache
    {
        private readonly IRepositoryManager _repositoryManager;

        public UserCache(MemoryCacheWithPolicy cache, IRepositoryManager repositoryManager) : base(cache)
            => _repositoryManager = repositoryManager;

        public async Task<User> GetOrCacheUserAsync(TelegramUser telegramUser)
        {
            User? entry = Get(telegramUser.Id);
            if (entry is not null) return entry;

            entry = await _repositoryManager.UserRepository.GetUserAsync(telegramUser.Id, true);

            if (entry is null)
            {
                entry = telegramUser.ToUser();
                _repositoryManager.UserRepository.CreateUser(entry);
                await _repositoryManager.SaveAsync().ContinueWith(_ => Program.Log.Info($"User <{entry.TelegramId}> persisted in database."), TaskContinuationOptions.OnlyOnRanToCompletion);
            }

            return await CacheAsync(telegramUser.Id, entry);
        }

        public void RemoveUser(object key) => Remove(key);
    }
}
