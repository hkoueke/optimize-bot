using Newtonsoft.Json;
using OptimizeBot.Model;
using OptimizeBot.Utils;
using Stateless;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using R = OptimizeBot.Properties.Resources;

namespace OptimizeBot.Processes
{
    public sealed class Receipt : Conversation
    {
        private Catalog catalog;
        private readonly ReceiptContext cData;

        private enum State { Idle, AwaitingProvider, AwaitingUtilityProvider, AwaitingTransactionId, Done }
        private enum Trigger
        {
            AskProviderList,
            ProviderSent,
            SingleProviderRefused,
            SingleUtilityProviderRefused,
            UtilityProviderSent,
            TransactionIdSent,
            RetryNewTrxId,
            RetryNewUtilityProvider,
            RetryNewProvider
        }

        private readonly StateMachine<State, Trigger> stateMachine;

        public Receipt(ITelegramBotClient bot, Update update, Service service) : base(bot, update, service)
        {
            //Initialize this process with its own emoji
            serviceDesc = string.Concat(Emojis.Globe, " ", serviceDesc);

            //Initialize ReceiptContext
            if (user.Session.ContextData != null)
                cData = JsonUtil.DeserializeObject<ReceiptContext>(user.Session.ContextData);

            //Initialize state machine
            stateMachine = new StateMachine<State, Trigger>(stateAccessor: () => GetSessionState<State>(),
                                                            stateMutator: async s => await UpdateSessionStateAsync(s));
            //Configure state machine
            stateMachine.Configure(State.Idle)
                        .Permit(Trigger.AskProviderList, State.AwaitingProvider);

            stateMachine.Configure(State.AwaitingProvider)
                        .OnEntryAsync(async () => await SendProviderListAsync())
                        .Permit(Trigger.ProviderSent, State.AwaitingUtilityProvider)
                        .Permit(Trigger.SingleProviderRefused, State.Done)
                        .PermitReentry(Trigger.RetryNewProvider);

            stateMachine.Configure(State.AwaitingUtilityProvider)
                        .OnEntryAsync(async () => await SendUtilitiesListAsync())
                        .Permit(Trigger.UtilityProviderSent, State.AwaitingTransactionId)
                        .Permit(Trigger.SingleUtilityProviderRefused, State.Done)
                        .PermitReentry(Trigger.RetryNewUtilityProvider);

            stateMachine.Configure(State.AwaitingTransactionId)
                        .OnEntryAsync(async () => await AskBillIdAsync())
                        .OnExitAsync(async () => await SendWaitMessageAsync())
                        .OnExitAsync(async () => await GetFileUriAsync()
                            .ContinueWith(async t => await PostDocument(await t), TaskContinuationOptions.OnlyOnRanToCompletion))
                        .Permit(Trigger.TransactionIdSent, State.Done)
                        .PermitReentry(Trigger.RetryNewTrxId);

            stateMachine.Configure(State.Done)
                        .OnEntryAsync(async _ => await AskRetry())
                        .Permit(Trigger.RetryNewTrxId, State.AwaitingTransactionId)
                        .Permit(Trigger.RetryNewProvider, State.AwaitingProvider)
                        .Permit(Trigger.RetryNewUtilityProvider, State.AwaitingUtilityProvider);
        }

        private async Task SendProviderListAsync()
        {
            var hasSingleEntry = service.Catalogs.Count == 1;

            var message = hasSingleEntry
                ? string.Format(R.ServiceSingleProvider, serviceDesc, Environment.NewLine, service.Catalogs[0].Provider.Name)
                : string.Format(R.ServiceSelectProvider, serviceDesc, Environment.NewLine, service.Catalogs.Count);

            var keyboard = hasSingleEntry
                ? KeyboardUtil.GetInlineKeyboard(Constants.YES_NO)
                : KeyboardUtil.GetInlineKeyboard(service.Catalogs, service.Parent, 1);

            //Send the message
            await SendEditMessageTextAsync(message, keyboard);
        }

        private async Task SendUtilitiesListAsync()
        {
            try
            {
                var provider = service.Catalogs.SingleOrDefault(ps => ps.ProviderId == cData.ProviderId).Provider;

                //Get this provider's registered utility providers
                var utilities = JsonConvert.DeserializeObject<IList<Utility>>(provider.UtilityProviders);

                if (utilities == null || utilities.Count == 0)
                    throw new InvalidOperationException($"{nameof(utilities)} is null: { utilities is null} isEmpty: {utilities.Count == 0}");

                var message = utilities.Count == 1
                    ? string.Format(R.ServiceSingleProvider, serviceDesc, Environment.NewLine, utilities[0].CompanyName)
                    : string.Format(R.ServiceSelectProvider, serviceDesc, Environment.NewLine, utilities.Count);

                var items = new List<(string, string)>(utilities.Count);
                foreach (var item in utilities) items.Add((item.CompanyName, item.Id));

                var keyboard = utilities.Count == 1
                    ? KeyboardUtil.GetInlineKeyboard(Constants.YES_NO)
                    : KeyboardUtil.GetInlineKeyboard(items: items, service.Parent, itemsPerRow: 1);

                cData.UtilityCount = utilities.Count;
                user.Session.ContextData = JsonConvert.SerializeObject(cData);

                //Send the message
                await UpdateSessionAsync(user)
                    .ContinueWith(async _ => await SendEditMessageTextAsync(message, keyboard), TaskContinuationOptions.OnlyOnRanToCompletion);
            }
            catch (Exception ex)
            {
                Program.log.Error(ex.Message, ex);
            }
        }

        private async Task AskBillIdAsync()
        {
            try
            {
                var catalog = service.Catalogs.SingleOrDefault(c => c.ProviderId == cData.ProviderId);
                var message = string.Format(R.ServiceReceiptEnterTransactionId, serviceDesc, Environment.NewLine, catalog.Provider.Name);

                //Send the message
                await SendEditMessageTextAsync(message);
            }
            catch (Exception ex)
            {
                Program.log.Error(ex.Message, ex);
            }
        }

        private async Task SendWaitMessageAsync()
        {
            var message = string.Format(R.ServiceReceiptPleaseWait, Emojis.Hourglass_Flowing_Sand, Environment.NewLine);
            await SendEditMessageTextAsync(message, replyMarkup: RetryKeyboard());
        }

        private async Task<string> GetFileUriAsync()
        {
            //get the catalog we are interested in
            var catalog = service.Catalogs.SingleOrDefault(c => c.ProviderId == cData.ProviderId);

            if(catalog == null)
                throw new NullReferenceException(nameof(catalog));

            //Get the Configuration object of the provider on the catalog
            var config = catalog.Provider.Config;

            //Get URI
            var address = string.Format(config.ReceiptApiUrl, cData.UtilityProviderId, cData.TrxId);

            using (var client = GetWebClient())
            {
                if (config.ReceiptApiHost != null) client.Headers["Host"] = config.ReceiptApiHost;
                if (config.ReceiptApiReferer != null) client.Headers["Referer"] = config.ReceiptApiReferer;

                try
                {
                    var link = await client.DownloadStringTaskAsync(address);
                    return link;
                }
                catch (WebException ex)
                {
                    Program.log.Error(ex.Message, ex);
                    var message = string.Format(R.ErrorNotDownloaded, Emojis.NoEntry, Environment.NewLine);
                    await SendEditMessageTextAsync(message, replyMarkup: RetryKeyboard());
                    throw;
                }
            }
        }

        private async Task PostDocument(string uri)
        {
            var data = JsonConvert.DeserializeObject<DownloadedLink>(uri);
            var file = new InputOnlineFile(Uri.UnescapeDataString(data.Link));
            await SendDocumentAsync(file);
        }

        private async Task AskRetry() => await SendEditMessageTextAsync(R.Retry, replyMarkup: RetryKeyboard());

        private static InlineKeyboardMarkup RetryKeyboard()
        {
            var items = new List<(string, string)>()
            {
                (string.Format(R.RetryProvider, Emojis.Bank), Enum.GetName(Trigger.RetryNewProvider)),
                (string.Format(R.Home, Emojis.House), "/start")
            };

            return KeyboardUtil.GetInlineKeyboard(items, itemsPerRow: 1);
        }

        private static WebClient GetWebClient()
        {
            return new WebClient()
            {
                Headers = new WebHeaderCollection()
                {
                    @"User-Agent:Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/101.0.4951.67 Safari/537.36",
                    @"Accept:application/json, text/plain, */*",
                    "Accept-Encoding:gzip, deflate",
                    "Accept-Language:fr-FR,fr;q=0.9,en-US;q=0.8,en;q=0.7"
                } 
            };
        }

        public override async Task Process(string data)
        {
            await base.Process(data);
            var count = service.Parent.Services.Count(s => s.Command.Equals(data));

            switch (stateMachine.State)
            {
                case State.Idle:
                {
                    await stateMachine.FireAsync(Trigger.AskProviderList);
                    break;
                }
                case State.AwaitingProvider:
                {
                    if (update.Type == UpdateType.Message)
                    {
                        await DeleteMessageAsync(chatId: update.Message.Chat.Id, messageId: update.Message.MessageId);
                        break;
                    }

                    if (count > 0) break;

                    if (service.Catalogs.Count == 1 && !bool.Parse(data))
                    {
                        await stateMachine.FireAsync(Trigger.SingleProviderRefused);
                        break;
                    }

                    //Serialize properties
                    catalog = service.Catalogs.Count == 1
                        ? service.Catalogs[0]
                        : service.Catalogs.SingleOrDefault(ps => ps.ProviderId == int.Parse(data));

                    //Update user context
                    var contextData = JsonConvert.SerializeObject(new ReceiptContext { ProviderId = catalog.ProviderId });
                    user.Session.ContextData = contextData;

                    await UpdateSessionAsync(user)
                        .ContinueWith(async _ => await stateMachine.FireAsync(Trigger.ProviderSent), TaskContinuationOptions.OnlyOnRanToCompletion);

                    break;
                }
                case State.AwaitingUtilityProvider:
                {
                    if (update.Type == UpdateType.Message)
                    {
                        await DeleteMessageAsync(chatId: update.Message.Chat.Id, messageId: update.Message.MessageId);
                        break;
                    }

                    if (count > 0) break;

                    if (cData.UtilityCount == 1 && !bool.Parse(data))
                    {
                        await stateMachine.FireAsync(Trigger.SingleUtilityProviderRefused);
                        break;
                    }

                    cData.UtilityProviderId = data;
                    user.Session.ContextData = JsonConvert.SerializeObject(cData);

                    await UpdateSessionAsync(user)
                        .ContinueWith(async _ => await stateMachine.FireAsync(Trigger.UtilityProviderSent));

                    break;
                }
                case State.AwaitingTransactionId:
                {
                    if (count > 0) break;

                    var catalog = service.Catalogs.SingleOrDefault(c => c.ProviderId == cData.ProviderId);
                    
                    if (catalog == null)
                    {
                        Program.log.Error($"{nameof(catalog)} is null");
                        break;
                    }

                    try
                    {
                        var errorMessage = string.Format(R.WarningInvalidTrxId,
                                                         Emojis.Warning,
                                                         Environment.NewLine,
                                                         catalog.Provider.Config.TrxIdLength);
                        
                        if (catalog.Provider.Config.TrxIdLength != data.Length) 
                            throw new ArgumentException(errorMessage);

                        cData.TrxId = data;
                        user.Session.ContextData = JsonConvert.SerializeObject(cData);

                        await UpdateSessionAsync(user)
                            .ContinueWith(async _ => await stateMachine.FireAsync(Trigger.TransactionIdSent));
                    }
                    catch (Exception e)
                    {
                        if (update.Type == UpdateType.Message)
                            await DeleteMessageAsync(chatId: update.Message.Chat.Id, messageId: update.Message.MessageId);

                        await SendEditMessageTextAsync(e.Message);
                    }
                    break;
                }
                case State.Done:
                {
                    if (update.Type == UpdateType.Message)
                    {
                        await DeleteMessageAsync(chatId: update.Message.Chat.Id, messageId: update.Message.MessageId);
                        break;
                    }

                    if (count > 0) return;

                    var retry = Enum.Parse<Trigger>(data);

                    if (retry == Trigger.RetryNewProvider) 
                        await stateMachine.FireAsync(Trigger.RetryNewProvider);

                    else if (retry == Trigger.RetryNewUtilityProvider) 
                        await stateMachine.FireAsync(Trigger.RetryNewUtilityProvider);

                    else 
                        await stateMachine.FireAsync(Trigger.RetryNewUtilityProvider);

                    break;
                }
                default: throw new InvalidOperationException($"Unknown State: {nameof(stateMachine.State)}");
            }
        }
    }
}
