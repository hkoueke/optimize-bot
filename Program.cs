using log4net;
using log4net.Config;
using OptimizeBot.Context;
using OptimizeBot.Contracts.Persistance;
using OptimizeBot.Helpers;
using OptimizeBot.Repository.Persistence;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace OptimizeBot
{
    class Program
    {
        private static readonly TelegramBotClient _bot;
        public static readonly ILog Log;
        private static readonly IRepositoryManager _repositoryManager;

        static Program()
        {
            _bot = new(Constants.BOT_TOKEN);
            _repositoryManager = new RepositoryManager(new OptimizeBotContext());
            Log = LogManager.GetLogger(typeof(Program));
        }

        static async Task Main()
        {
            AppDomain.CurrentDomain.UnhandledException += UnHandledExceptionTrap;
            XmlConfigurator.Configure();

            ReceiverOptions receiverOptions = new()
            {
                AllowedUpdates = new[]
                {
                    UpdateType.Message, UpdateType.InlineQuery,
                    UpdateType.ChosenInlineResult, UpdateType.CallbackQuery
                }
            };

            var cts = new CancellationTokenSource();
            _bot.StartReceiving(Handlers.HandleUpdateAsync,
                               Handlers.HandleErrorAsync,
                               receiverOptions,
                               cancellationToken: cts.Token);
            try
            {
                User? me = await _bot.GetMeAsync();
                Console.Title = me?.Username ?? "Bot name is not available";

                Log.Info($"Started listening for messages from @{me?.Username}");
                await SetBotCommandsAsync();

                // Send cancellation request to stop bot
                Console.ReadKey();
                cts.Cancel();
            }
            catch (Exception e)
            {
                Log.Error(e.Message, e);
                cts.Cancel();
                Environment.Exit(1);
            }
        }

        private static async Task SetBotCommandsAsync()
        {
            var commands = await _repositoryManager.ServiceRepository.GetServicesByCondition(condition: s => !s.AdminOnly, orderBy: s => s.ServiceId);

            if (commands.Any())
            {
                var fr = commands.ConvertAll(s => new BotCommand { Command = s.Command, Description = s.FrDesc });
                var en = commands.ConvertAll(s => new BotCommand { Command = s.Command, Description = s.EnDesc });

                await _bot!.SetMyCommandsAsync(commands: fr, languageCode: "fr");
                await _bot!.SetMyCommandsAsync(commands: en, languageCode: "en");
            }
        }

        private static void UnHandledExceptionTrap(object sender, UnhandledExceptionEventArgs e)
        {
            Log.Fatal(e.ExceptionObject.ToString());
            Environment.Exit(1);
        }
    }
}