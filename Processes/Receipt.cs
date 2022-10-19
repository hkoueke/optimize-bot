using Newtonsoft.Json;
using OptimizeBot.Contracts.Caching;
using OptimizeBot.Contracts.Messaging;
using OptimizeBot.Contracts.Persistance;
using OptimizeBot.Model;
using OptimizeBot.Utils;
using Stateless;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using R = OptimizeBot.Properties.Resources;

namespace OptimizeBot.Processes
{
    public sealed class Receipt : Conversation
    {
        private enum State : byte { Idle, AwaitingProvider, AwaitingUtilityProvider, AwaitingTransactionId, Done }

        private enum Trigger : byte
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
        public Receipt(ICacheManager cacheManager,
                     IRepositoryManager repositoryManager,
                     IMessagingService messagingService,
                     Update update,
                     Service service) : base(cacheManager, repositoryManager, messagingService, update, service)
        {
            //Initialize this process with its own emoji
            localizedServiceDescription = string.Concat(Emojis.Globe, " ", localizedServiceDescription);

            //Initialize ReceiptContext
            if (User.Session.ContextData != null)
            {
                cData = JsonUtil.DeserializeObject<ReceiptContext>(User.Session.ContextData);
            }

            //Initialize state machine
            stateMachine = new StateMachine<State, Trigger>(stateAccessor: () => GetSessionStateAsync<State>(),
                                                            stateMutator: async s => await UpdateSessionStateAsync(s));
            //Configure state machine
            stateMachine
                .Configure(State.Idle)
                .Permit(Trigger.AskProviderList, State.AwaitingProvider);

            stateMachine
                .Configure(State.AwaitingProvider)
                .OnEntryAsync(async () => await SendProviderListAsync())
                .Permit(Trigger.ProviderSent, State.AwaitingUtilityProvider)
                .Permit(Trigger.SingleProviderRefused, State.Done)
                .PermitReentry(Trigger.RetryNewProvider);

            stateMachine
                .Configure(State.AwaitingUtilityProvider)
                .OnEntryAsync(async () => await SendUtilitiesListAsync())
                .Permit(Trigger.UtilityProviderSent, State.AwaitingTransactionId)
                .Permit(Trigger.SingleUtilityProviderRefused, State.Done)
                .PermitReentry(Trigger.RetryNewUtilityProvider);

            stateMachine
                .Configure(State.AwaitingTransactionId)
                .OnEntryAsync(async () => await AskBillIdAsync())
                .OnExitAsync(async () => await SendWaitMessageAsync())
                .OnExitAsync(async () => await GetFileUriAsync().AsTask()
                    .ContinueWith(async t => await PostDocumentAsync(await t), TaskContinuationOptions.OnlyOnRanToCompletion))
                .Permit(Trigger.TransactionIdSent, State.Done)
                .PermitReentry(Trigger.RetryNewTrxId);

            stateMachine
                .Configure(State.Done)
                .OnEntryAsync(async _ => await AskRetryAsync())
                .Permit(Trigger.RetryNewTrxId, State.AwaitingTransactionId)
                .Permit(Trigger.RetryNewProvider, State.AwaitingProvider)
                .Permit(Trigger.RetryNewUtilityProvider, State.AwaitingUtilityProvider);
        }

        private async Task SendProviderListAsync()
        {
            var hasSingleEntry = Service.Catalogs.Count == 1;

            var message = hasSingleEntry
                ? string.Format(R.ServiceSingleProvider, localizedServiceDescription, Environment.NewLine, Service.Catalogs[0].Provider.Name)
                : string.Format(R.ServiceSelectProvider, localizedServiceDescription, Environment.NewLine, Service.Catalogs.Count);

            InlineKeyboardMarkup keyboard = hasSingleEntry
                ? KeyboardUtil.GetInlineKeyboard(Constants.YES_NO)
                : KeyboardUtil.GetInlineKeyboard(Service.Catalogs, Service.Parent, 1);

            //Send the message
            await SendEditMessageTextAsync(message, keyboard);
        }

        private async Task SendUtilitiesListAsync()
        {
            try
            {
                Provider? provider = Service.Catalogs.SingleOrDefault(ps => ps.ProviderId == cData.ProviderId)?.Provider;
                if (provider is null) return;

                //Get this provider's registered utility providers
                IList<Utility> utilities = JsonConvert.DeserializeObject<IList<Utility>>(provider.UtilityProviders);
                if (utilities == null || utilities.Count == 0)
                {
                    throw new InvalidOperationException($"{nameof(utilities)} is null: {utilities is null} isEmpty: {utilities.Count == 0}");
                }

                var message = utilities.Count == 1
                    ? string.Format(R.ServiceSingleProvider, localizedServiceDescription, Environment.NewLine, utilities[0].CompanyName)
                    : string.Format(R.ServiceSelectProvider, localizedServiceDescription, Environment.NewLine, utilities.Count);

                var items = new List<(string, string)>(utilities.Count);
                foreach (var item in utilities)
                {
                    items.Add((item.CompanyName, item.Id));
                }
                var keyboard = utilities.Count == 1
                    ? KeyboardUtil.GetInlineKeyboard(Constants.YES_NO)
                    : KeyboardUtil.GetInlineKeyboard(items: items, Service.Parent, itemsPerRow: 1);

                cData.UtilityCount = utilities.Count;
                User.Session.ContextData = JsonConvert.SerializeObject(cData);

                //Send the message
                await UpdateSessionAsync(User)
                    .ContinueWith(async _ => await SendEditMessageTextAsync(message, keyboard), TaskContinuationOptions.OnlyOnRanToCompletion);
            }
            catch (Exception ex)
            {
                Program.Log.Error(ex.Message, ex);
            }
        }
        private async Task AskBillIdAsync()
        {
            try
            {
                var catalog = Service.Catalogs.FirstOrDefault(c => c.ProviderId == cData.ProviderId);
                var message = string.Format(R.ServiceReceiptEnterTransactionId, localizedServiceDescription, Environment.NewLine, catalog.Provider.Name);
                //Send the message
                await SendEditMessageTextAsync(message);
            }
            catch (Exception ex)
            {
                Program.Log.Error(ex.Message, ex);
            }
        }
        private async Task SendWaitMessageAsync()
        {
            var message = string.Format(R.ServiceReceiptPleaseWait, Emojis.Hourglass_Flowing_Sand, Environment.NewLine);
            await SendEditMessageTextAsync(message, replyMarkup: RetryKeyboard());
        }
        private async ValueTask<string> GetFileUriAsync()
        {
            //get the catalog we are interested in
            var catalog = Service.Catalogs.SingleOrDefault(c => c.ProviderId == cData.ProviderId);
            if (catalog == null)
            {
                throw new NullReferenceException(nameof(catalog));
            }
            //Get the Configuration object of the provider on the catalog
            var config = catalog.Provider.Config;
            //Get URI
            var address = string.Format(config.ReceiptApiUrl, cData.UtilityProviderId, cData.TrxId);

            using var client = GetWebClient();
            if (config.ReceiptApiHost != null)
            {
                client.Headers["Host"] = config.ReceiptApiHost;
            }
            if (config.ReceiptApiReferer != null)
            {
                client.Headers["Referer"] = config.ReceiptApiReferer;
            }
            try
            {
                var link = await client.DownloadStringTaskAsync(address);
                return link;
            }
            catch (WebException ex)
            {
                Program.Log.Error(ex.Message, ex);
                var message = string.Format(R.ErrorNotDownloaded, Emojis.NoEntry, Environment.NewLine);
                await SendEditMessageTextAsync(message, replyMarkup: RetryKeyboard());
                throw;
            }
        }
        private async Task PostDocumentAsync(string uri)
        {
            var data = JsonConvert.DeserializeObject<DownloadedLink>(uri);
            var file = new InputOnlineFile(Uri.UnescapeDataString(data.Link));
            await SendDocumentAsync(file);
        }
        private async Task AskRetryAsync() => await SendEditMessageTextAsync(message: R.Retry, replyMarkup: RetryKeyboard());
        private static InlineKeyboardMarkup RetryKeyboard()
        {
            var items = new List<(string, string)>()
            {
                (string.Format(R.RetryProvider, Emojis.Bank), Enum.GetName(Trigger.RetryNewProvider)!),
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
        public override async Task ProcessAsync(string data)
        {
            await base.ProcessAsync(data);
            var count = Service.Parent.Services.Count(s => s.Command.Equals(data));
            switch (stateMachine.State)
            {
                case State.Idle:
                    {
                        await stateMachine.FireAsync(Trigger.AskProviderList);
                        break;
                    }
                case State.AwaitingProvider:
                    {
                        if (Update.Type == UpdateType.Message)
                        {
                            await DeleteMessageAsync(chatId: Update.Message.Chat.Id, messageId: Update.Message.MessageId);
                            break;
                        }
                        if (count > 0)
                        {
                            break;
                        }
                        if (Service.Catalogs.Count == 1 && !bool.Parse(data))
                        {
                            await stateMachine.FireAsync(Trigger.SingleProviderRefused);
                            break;
                        }
                        //Serialize properties
                        catalog = Service.Catalogs.Count == 1
                            ? Service.Catalogs[0]
                            : Service.Catalogs.FirstOrDefault(ps => ps.ProviderId == int.Parse(data));

                        //Update User context
                        var contextData = JsonConvert.SerializeObject(new ReceiptContext { ProviderId = catalog.ProviderId });
                        User.Session.ContextData = contextData;
                        await UpdateSessionAsync(User)
                            .ContinueWith(async _ => await stateMachine.FireAsync(Trigger.ProviderSent), TaskContinuationOptions.OnlyOnRanToCompletion);
                        break;
                    }
                case State.AwaitingUtilityProvider:
                    {
                        if (Update.Type == UpdateType.Message)
                        {
                            await DeleteMessageAsync(chatId: Update.Message.Chat.Id, messageId: Update.Message.MessageId);
                            break;
                        }
                        if (count > 0)
                        {
                            break;
                        }
                        if (cData.UtilityCount == 1 && !bool.Parse(data))
                        {
                            await stateMachine.FireAsync(Trigger.SingleUtilityProviderRefused);
                            break;
                        }
                        cData.UtilityProviderId = data;
                        User.Session.ContextData = JsonConvert.SerializeObject(cData);
                        await UpdateSessionAsync(User)
                            .ContinueWith(async _ => await stateMachine.FireAsync(Trigger.UtilityProviderSent));
                        break;
                    }
                case State.AwaitingTransactionId:
                    {
                        if (count > 0)
                        {
                            break;
                        }
                        var catalog = Service.Catalogs.SingleOrDefault(c => c.ProviderId == cData.ProviderId);
                        if (catalog == null)
                        {
                            Program.Log.Error($"{nameof(catalog)} is null");
                            break;
                        }
                        try
                        {
                            var errorMessage = string.Format(R.WarningInvalidTrxId,
                                                             Emojis.Warning,
                                                             Environment.NewLine,
                                                             catalog.Provider.Config.TrxIdLength);

                            if (catalog.Provider.Config.TrxIdLength != data.Length)
                            {
                                throw new ArgumentException(errorMessage);
                            }
                            cData.TrxId = data;
                            User.Session.ContextData = JsonConvert.SerializeObject(cData);
                            await UpdateSessionAsync(User).ContinueWith(async _ => await stateMachine.FireAsync(Trigger.TransactionIdSent),
                                                                        TaskContinuationOptions.OnlyOnRanToCompletion);
                        }
                        catch (Exception e)
                        {
                            if (Update.Type == UpdateType.Message)
                            {
                                await DeleteMessageAsync(chatId: Update.Message.Chat.Id, messageId: Update.Message.MessageId);
                            }
                            await SendEditMessageTextAsync(e.Message);
                        }
                        break;
                    }
                case State.Done:
                    {
                        if (Update.Type == UpdateType.Message)
                        {
                            await DeleteMessageAsync(chatId: Update.Message.Chat.Id, messageId: Update.Message.MessageId);
                            break;
                        }
                        if (count > 0)
                        {
                            return;
                        }
                        var retry = Enum.Parse<Trigger>(data);
                        if (retry == Trigger.RetryNewProvider)
                        {
                            await stateMachine.FireAsync(Trigger.RetryNewProvider);
                        }
                        else if (retry == Trigger.RetryNewUtilityProvider)
                        {
                            await stateMachine.FireAsync(Trigger.RetryNewUtilityProvider);
                        }
                        else
                        {
                            await stateMachine.FireAsync(Trigger.RetryNewUtilityProvider);
                        }
                        break;
                    }
                default: throw new InvalidOperationException($"Unknown State: {nameof(stateMachine.State)}");
            }
        }
    }
}
