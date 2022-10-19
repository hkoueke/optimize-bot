using OptimizeBot.Contracts.Caching;
using OptimizeBot.Contracts.Messaging;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace OptimizeBot.Services
{
    public abstract class MessagingServiceBase : IMessagingService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ICacheManager _cacheManager;

        protected MessagingServiceBase(ITelegramBotClient botClient, ICacheManager cache) => (_botClient, _cacheManager) = (botClient, cache);

        public async Task DeleteMessageAsync(long chatId, int messageId) => await _botClient.DeleteMessageAsync(chatId, messageId);

        public async Task<Message> SendDocumentAsync(long chatId, InputOnlineFile file, IReplyMarkup? replyMarkup)
        {
            //Show ChatAction.UploadDocument to client device
            await _botClient.SendChatActionAsync(chatId, ChatAction.UploadDocument);
            await Task.Delay(1000);
            Message m = await _botClient.SendDocumentAsync(chatId, file, replyMarkup: replyMarkup ?? new ReplyKeyboardRemove());
            await _cacheManager.IDCache.CacheIdAsync(m.From?.Id!, m.MessageId) ;
            return m;
        }

        public async Task<Message> SendEditMessageTextAsync(long chatId, int messageId, string message, InlineKeyboardMarkup? replyMarkup, ParseMode? parseMode = ParseMode.Html)
        {
            await _botClient.SendChatActionAsync(chatId, ChatAction.Typing);
            await Task.Delay(1000);
            Message m = await _botClient.EditMessageTextAsync(chatId, messageId, message, parseMode, replyMarkup: replyMarkup);
            await _cacheManager.IDCache.CacheIdAsync(m.From?.Id!, m.MessageId);
            return m;
        }

        public async Task<Message> SendTextMessageAsync(long chatId, string message, IReplyMarkup? replyMarkup, ParseMode? parseMode = ParseMode.Html)
        {
            await _botClient.SendChatActionAsync(chatId, ChatAction.Typing);
            await Task.Delay(1000);
            Message m = await _botClient.SendTextMessageAsync(chatId, message, parseMode, replyMarkup: replyMarkup ?? new ReplyKeyboardRemove());
            await _cacheManager.IDCache.CacheIdAsync(m.From?.Id!, m.MessageId);
            return m;
        }
    }
}
