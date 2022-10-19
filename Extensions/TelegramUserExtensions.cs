using OptimizeBot.Model;
using System.Linq;
using TelegramUser = Telegram.Bot.Types.User;

namespace OptimizeBot.Extensions
{
    public static class TelegramUserExtensions
    {
        public static User ToUser(this TelegramUser telegramUser) => new()
        {
            TelegramId = telegramUser.Id,
            FirstName = telegramUser.FirstName,
            LastName = telegramUser.LastName,
            LanguageCode = telegramUser.LanguageCode,
            Username = telegramUser.Username,
            IsBot = telegramUser.IsBot,
            IsAdmin = Constants.ADMINS.Any(s => s.Equals(telegramUser.Id.ToString())),
            Session = new()
        };
    }
}
