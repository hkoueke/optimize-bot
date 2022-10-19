using OptimizeBot.Contracts.Caching;
using Telegram.Bot;

namespace OptimizeBot.Services
{
    public class MessagingService : MessagingServiceBase
    {
        public MessagingService(ITelegramBotClient botClient, ICacheManager cache) : base(botClient, cache)
        {
        }
    }
}
