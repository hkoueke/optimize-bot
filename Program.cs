using System;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using System.Threading;
using Telegram.Bot.Extensions.Polling;
using OptimizeBot.Helpers;
using log4net;
using log4net.Config;
using Telegram.Bot.Types.Enums;
using OptimizeBot.Utils;
using OptimizeBot.Model;
using Telegram.Bot.Types;
using System.Globalization;

namespace OptimizeBot
{
    class Program
    {
        private static TelegramBotClient bot;
        public static readonly ILog log = LogManager.GetLogger(typeof(Program));

        public static async Task Main()
        {
            AppDomain.CurrentDomain.UnhandledException += UnHandledExceptionTrap;
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("en-US");
            XmlConfigurator.Configure();

            bot = new(Constants.BOT_TOKEN);
            ReceiverOptions receiverOptions = new()
            {
                AllowedUpdates = new[]
                {
                    UpdateType.Message, UpdateType.InlineQuery,
                    UpdateType.ChosenInlineResult, UpdateType.CallbackQuery
                }
            };

            var cts = new CancellationTokenSource();

            bot.StartReceiving(Handlers.HandleUpdateAsync,
                               Handlers.HandleErrorAsync,
                               receiverOptions,
                               cancellationToken: cts.Token);
            try
            {
                var me = await bot.GetMeAsync();
                Console.Title = me.Username;
                log.Info($"Started listening for messages from @{me.Username}");
                await SetBotCommandsAsync();

                // Send cancellation request to stop bot
                Console.ReadKey();
                cts.Cancel();
            }
            catch (Exception e)
            {
                log.Error(e.Message, e);
                cts.Cancel();
                Environment.Exit(1);
            }
        }

        private static async Task SetBotCommandsAsync()
        {
            var commands = await DbUtil.FindAll<Service, int>(predicate: s => !s.AdminOnly,
                                                              orderBy: o => o.ServiceId);
            if (commands.Any())
            {
                var fr = commands.ConvertAll(s => new BotCommand { Command = s.Command, Description = s.FrDesc });
                var en = commands.ConvertAll(s => new BotCommand { Command = s.Command, Description = s.EnDesc });

                await bot.SetMyCommandsAsync(commands: fr, languageCode: "fr");
                await bot.SetMyCommandsAsync(commands: en, languageCode: "en");
            }
        }
        private static void UnHandledExceptionTrap(object sender, UnhandledExceptionEventArgs e)
        {
            log.Fatal(e.ExceptionObject.ToString());
            Environment.Exit(1);
        }
    }
}