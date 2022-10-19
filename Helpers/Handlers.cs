using OptimizeBot.Context;
using OptimizeBot.Contracts.Caching;
using OptimizeBot.Contracts.Messaging;
using OptimizeBot.Contracts.Persistance;
using OptimizeBot.Extensions;
using OptimizeBot.Model;
using OptimizeBot.Processes;
using OptimizeBot.Repository.Cache;
using OptimizeBot.Repository.Persistence;
using OptimizeBot.Services;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using User = OptimizeBot.Model.User;

namespace OptimizeBot.Helpers
{
    public static class Handlers
    {
        private static readonly ICacheManager _cacheManager;
        private static readonly IRepositoryManager _repositoryManager;
        private static IMessagingService? _messagingService;

        static Handlers()
        {
            _repositoryManager = new RepositoryManager(new OptimizeBotContext());
            _cacheManager = new CacheManager(Constants.CACHE_DEFAULT_OBJECT, _repositoryManager);
        }

        public static async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
        {
            _messagingService = new MessagingService(bot, _cacheManager);

            var handler = update.Type switch
            {
                UpdateType.Message or
                UpdateType.InlineQuery or
                UpdateType.CallbackQuery or
                UpdateType.ChosenInlineResult => OnUpdateReceivedAsync(update),
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

        private static async Task OnUpdateReceivedAsync(Update update)
        {
            User entry = await _cacheManager.UserCache.GetOrCacheUserAsync(update.GetUser());

            //Set Culture for this user
            if (entry.LanguageCode is not null)
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(entry.LanguageCode);

            //Prepare info for processing
            string data = update.GetUpdateTypeMessage();
            Service? service = await _repositoryManager.ServiceRepository.GetServiceAsync(s => s.Command.Equals(data));
            if (service is null) await _repositoryManager.ServiceRepository.GetServiceAsync(s => s.Command.Equals(entry.Session.Context));
            if (service is null) await _repositoryManager.ServiceRepository.GetServiceAsync(s => s.Parent == null);

            if (service is null)
            {
                Program.Log.Info($"No suitable Service found");
                return;
            }

            Program.Log.Info($"Processing message from user <{update.GetUser().Id}>");

            Conversation conversation = GetConversation(update, service);
            if (conversation is not null) await conversation.ProcessAsync(data);
        }

        public static async Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Program.Log.Error(ErrorMessage, exception);
            await Task.CompletedTask;
        }

        private static Conversation GetConversation(Update update, Service service) => service.Command switch
        {
            "/start" => new Start(_cacheManager, _repositoryManager, _messagingService!, update, service),
            "/cashout" => new CashOut(_cacheManager, _repositoryManager, _messagingService!, update, service),
            "/cashin" => new CashIn(_cacheManager, _repositoryManager, _messagingService!, update, service),
            "/receipt" => new Receipt(_cacheManager, _repositoryManager, _messagingService!, update, service),
            "/about" => new About(_cacheManager, _repositoryManager, _messagingService!, update, service),
            _ => throw new NotImplementedException(nameof(service.Command))
        };
    }
}