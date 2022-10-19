using OptimizeBot.Contracts.Caching;
using OptimizeBot.Contracts.Messaging;
using OptimizeBot.Contracts.Persistance;
using OptimizeBot.Extensions;
using OptimizeBot.Model;
using System;
using System.Globalization;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using User = OptimizeBot.Model.User;

namespace OptimizeBot.Processes
{
    public abstract class Conversation
    {
        private readonly ICacheManager _cacheManager;
        private readonly IRepositoryManager _repositoryManager;
        private readonly IMessagingService _messagingService;

        protected readonly Update Update;
        protected readonly Service Service;
        //protected User User;
        protected string localizedServiceDescription;

        protected Conversation(ICacheManager cacheManager,
                               IRepositoryManager repositoryManager,
                               IMessagingService messagingService,
                               Update update,
                               Service service)
        {
            Update = update;
            Service = service;
            _cacheManager = cacheManager;
            _repositoryManager = repositoryManager;
            _messagingService = messagingService;

            //Initialize service description field
            localizedServiceDescription = CultureInfo.CurrentCulture.IsEnglish() ? service.EnDesc : service.FrDesc;
        }

        public virtual async Task ProcessAsync(string data)
        {
            User user = await _cacheManager.UserCache.GetOrCacheUserAsync(Update.GetUser());

            //-> Restrict service usage to admins
            if (Service.AdminOnly && !user.IsAdmin)
            {
                Program.Log.Info($"Command <{Service.Command}> is restricted to admins");
                return;
            }

            if (user.Session.Context is null || !user.Session.Context.Equals(Service.Command))
            {
                user.Session.Context = Service.Command;
                user.Session.ContextData = default;
                user.Session.State = default;
                await UpdateSessionAsync(user);
            }
        }

        protected async Task<Message> SendEditMessageTextAsync(string message, InlineKeyboardMarkup? replyMarkup = default)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            // Get the last message sent by the bot and by the User when replying to a message
            //var lastMessageId = await _cacheManager.IDCache.GetOrCreateAsync(update.GetUser().Id);
            int lastMessageId = _cacheManager.IDCache.GetId(Update.GetUser().Id);
            int receivedId = Update.GetUpdateTypeMessageId();

            // If there is no id in cache consider the app was restarted or cache entry dropped.
            // If the cached id is less than the id sent by User reply there is a 'separator' message
            // In both cases, send a regular message and save the message id to cache
            if (lastMessageId == default || lastMessageId < receivedId)
                return await _messagingService.SendTextMessageAsync(Update.GetUser().Id, message, replyMarkup);

            // Send an Edit message to User without caching MessageId since it does not change between two MessageEdit updates
            return await _messagingService.SendEditMessageTextAsync(Update.GetUser().Id, lastMessageId, message, replyMarkup);
        }

        protected async Task SendDocumentAsync(InputOnlineFile file, IReplyMarkup? replyMarkup = default)
            => await _messagingService.SendDocumentAsync(Update.GetUser().Id, file, replyMarkup);

        protected async Task DeleteMessageAsync(long chatId, int messageId)
            => await _messagingService.DeleteMessageAsync(chatId, messageId);

        protected async Task<T> GetSessionStateAsync<T>() where T : struct
        {
            User user = await _cacheManager.UserCache.GetOrCacheUserAsync(Update.GetUser());
            return Enum.Parse<T>(user.Session.State ?? "Idle");
        }

        protected async Task UpdateSessionStateAsync<T>(T state) where T : struct, Enum
        {
            User user = await _cacheManager.UserCache.GetOrCacheUserAsync(Update.GetUser());
            user.Session.State = Enum.GetName(state) ?? "Idle";
            await UpdateSessionAsync(user);
        }

        protected async Task UpdateSessionAsync(User user)
        {
            _repositoryManager.UserRepository.UpdateUser(user);
            await _repositoryManager.SaveAsync()
                                    .ContinueWith(_ => _cacheManager.UserCache.RemoveUser(user.TelegramId), TaskContinuationOptions.OnlyOnRanToCompletion);
        }
    }
}
