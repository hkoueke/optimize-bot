using System;
using System.Linq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramUser = Telegram.Bot.Types.User;
using User = OptimizeBot.Model.User;

namespace OptimizeBot.Utils
{
    public static class UpdateUtil
    {
        public static TelegramUser GetSenderFromUpdate(in Update update) => update.Type switch
        {
            UpdateType.Message => update.Message.From,
            UpdateType.InlineQuery => update.InlineQuery.From,
            UpdateType.ChosenInlineResult => update.ChosenInlineResult.From,
            UpdateType.CallbackQuery => update.CallbackQuery.From,
            _ => throw new ArgumentException("Invalid update type", nameof(update))
        };

        public static string GetDataFromUpdate(in Update update) => update.Type switch
        {
            UpdateType.Message => update.Message.Text.Trim(),
            UpdateType.InlineQuery => update.InlineQuery.Query.Trim(),
            UpdateType.ChosenInlineResult => update.ChosenInlineResult.ResultId.Trim(),
            UpdateType.CallbackQuery => update.CallbackQuery.Data.Trim(),
            _ => throw new NotImplementedException(nameof(update.Type)),
        };

        public static int GetMessageIdFromUpdate(in Update update) => update.Type switch
        {
            UpdateType.Message => update.Message.MessageId,
            UpdateType.InlineQuery => int.Parse(update.InlineQuery.Id),
            UpdateType.ChosenInlineResult => int.Parse(update.ChosenInlineResult.InlineMessageId),
            UpdateType.CallbackQuery => update.CallbackQuery.Message.MessageId,
            _ => throw new NotImplementedException(nameof(update.Type)),
        };

        public static User BuildUserFromSender(TelegramUser telegram)
        {
            return new()
            {
                TelegramId = telegram.Id,
                FirstName = telegram.FirstName,
                LastName = telegram.LastName,
                LanguageCode = telegram.LanguageCode,
                Username = telegram.Username,
                IsBot = telegram.IsBot,
                IsAdmin = Constants.ADMINS.Any(s => s.Equals(telegram.Id.ToString())), 
                Session = new()
            };
        }
    }
}
