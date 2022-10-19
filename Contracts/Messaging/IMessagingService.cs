using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace OptimizeBot.Contracts.Messaging
{
    public interface IMessagingService
    {
        Task<Message> SendEditMessageTextAsync(long chatId, int messageId, string message, InlineKeyboardMarkup? replyMarkup, ParseMode? parseMode = ParseMode.Html);

        Task<Message> SendTextMessageAsync(long chatId, string message, IReplyMarkup? replyMarkup, ParseMode? parseMode = ParseMode.Html);

        Task<Message> SendDocumentAsync(long chatId, InputOnlineFile file, IReplyMarkup? replyMarkup);

        Task DeleteMessageAsync(long chatId, int messageId);
    }
}
