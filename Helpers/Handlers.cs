using OptimizeBot.Cache;
using OptimizeBot.Model;
using OptimizeBot.Processes;
using OptimizeBot.Utils;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace OptimizeBot.Helpers
{
    public static class Handlers
    {
        public static async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
        {
            var handler = update.Type switch
            {
                UpdateType.Message or
                UpdateType.InlineQuery or
                UpdateType.CallbackQuery or
                UpdateType.ChosenInlineResult => OnUpdateReceivedAsync(bot, update),
                _ => throw new NotSupportedException($"Update type <{update.Type}> is not supported")
            };

            try
            {
                await handler;
            }
            catch (Exception exception)
            {
                await HandleErrorAsync(bot, exception, cancellationToken);
            }
        }

        private static async Task OnUpdateReceivedAsync(ITelegramBotClient bot, Update update)
        {
            var sender = UpdateUtil.GetSenderFromUpdate(update);

            var entry = await CacheMgr.GetOrCreateAsync(sender.Id,
                                                        u => u.TelegramId == sender.Id,
                                                        UpdateUtil.BuildUserFromSender(sender));
            if (entry == null)
                throw new NullReferenceException(nameof(entry));

            //Set Culture for this user
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(sender.LanguageCode);

            //Prepare info for processing
            var data = UpdateUtil.GetDataFromUpdate(update).Trim();

            var service = await DbUtil.FindAsync<Service>(s => s.Command.Equals(data));
            if (service == null) service = await DbUtil.FindAsync<Service>(s => s.Command.Equals(entry.Session.Context));
            if (service == null) service = await DbUtil.FindAsync<Service>(s => s.Parent is null);

            Program.log.Info($"Processing message from user <{sender.Id}>");
            var conversation = GetConversation(bot, update, service);

            if (conversation != null)
                await conversation.Process(data);
        }

        public static async Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };
            Program.log.Error(ErrorMessage, exception);
            await Task.CompletedTask;
        }

        private static Conversation GetConversation(in ITelegramBotClient bot, in Update update, in Service service) => service.Command switch
        {
            "/start" => new Start(bot, update, service),
            "/cashout" => new CashOut(bot, update, service),
            "/cashin" => new CashIn(bot, update, service),
            "/receipt" => new Receipt(bot, update, service),
            "/about" => new About(bot, update, service),
            _ => throw new NotImplementedException(nameof(service.Command))
        };
    }
}