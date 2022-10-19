using OptimizeBot.Contracts.Caching;
using OptimizeBot.Contracts.Messaging;
using OptimizeBot.Contracts.Persistance;
using OptimizeBot.Model;
using Telegram.Bot.Types;

namespace OptimizeBot.Processes
{
    public sealed class CashIn : CashOut
    {
        public CashIn(ICacheManager cacheManager,
                     IRepositoryManager repositoryManager,
                     IMessagingService messagingService,
                     Update update,
                     Service service) : base(cacheManager, repositoryManager, messagingService, update, service) { }
    }
}
