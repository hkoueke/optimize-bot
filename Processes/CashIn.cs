using OptimizeBot.Model;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace OptimizeBot.Processes
{
    public sealed class CashIn : CashOut
    {
        public CashIn(ITelegramBotClient bot, Update update, Service service) : base(bot, update, service) { }
    }
}
