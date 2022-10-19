using System.Threading.Tasks;
using User = OptimizeBot.Model.User;
using TelegramUser = Telegram.Bot.Types.User;

namespace OptimizeBot.Contracts.Caching
{
    public interface IUserCache
    {
        void RemoveUser(object key);
        Task<User> GetOrCacheUserAsync(TelegramUser telegramUser);
    }
}
