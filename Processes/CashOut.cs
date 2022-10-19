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
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using R = OptimizeBot.Properties.Resources;

namespace OptimizeBot.Processes
{
    public class CashOut : Conversation
    {
        private readonly CashContext cData;

        private readonly StateMachine<State, Trigger> stateMachine;
        private enum State : byte { Idle, AwaitingProvider, AwaitingAmount, Done }
        private enum Trigger : byte { AskProviderList, ProviderSent, SingleProviderRefused, AmountSent, RetryNewAmount, RetryNewProvider }
        public CashOut(ICacheManager cacheManager,
                     IRepositoryManager repositoryManager,
                     IMessagingService messagingService,
                     Update update,
                     Service service) : base(cacheManager, repositoryManager, messagingService, update, service)
        {
            //Initialize this process with its own emoji
            localizedServiceDescription = string.Concat(IsCashOut() ? Emojis.Dollar_Fly : Emojis.Dollar, " ", localizedServiceDescription);

            //Initialize ContextData object
            if (User.Session.ContextData != null)
            {
                cData = JsonUtil.DeserializeObject<CashContext>(User.Session.ContextData);
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
                .OnEntryAsync(async () => await SendProviderList())
                .Permit(Trigger.ProviderSent, State.AwaitingAmount)
                .Permit(Trigger.SingleProviderRefused, State.Done)
                .PermitReentry(Trigger.RetryNewProvider);

            stateMachine
                .Configure(State.AwaitingAmount)
                .OnEntryAsync(async () => await AskAmount())
                .OnExitAsync(async () => await Task
                    .Run(() => ComputeFee())
                    .ContinueWith(async t => await SendComputeResult(await t), TaskContinuationOptions.OnlyOnRanToCompletion))
                .Permit(Trigger.AmountSent, State.Done)
                .PermitReentry(Trigger.RetryNewAmount);

            stateMachine
                .Configure(State.Done)
                .Permit(Trigger.RetryNewAmount, State.AwaitingAmount)
                .Permit(Trigger.RetryNewProvider, State.AwaitingProvider);

            cData = new(0, 0d, 0d);
        }
        private bool IsCashOut() => GetType().Equals(typeof(CashOut));
        private async Task SendProviderList()
        {
            var hasSingleEntry = Service.Catalogs.Count == 1;

            var message = hasSingleEntry
                ? string.Format(R.ServiceSingleProvider, localizedServiceDescription, Environment.NewLine, Service.Catalogs[0].Provider.Name)
                : string.Format(R.ServiceSelectProvider, localizedServiceDescription, Environment.NewLine, Service.Catalogs.Count);

            var keyboard = hasSingleEntry
                ? KeyboardUtil.GetInlineKeyboard(Constants.YES_NO)
                : KeyboardUtil.GetInlineKeyboard(Service.Catalogs, Service.Parent, 1);

            //Send the message
            await SendEditMessageTextAsync(message, replyMarkup: keyboard);
        }
        private async Task AskAmount()
        {
            var label = IsCashOut() ? R.ServiceCashOut : R.ServiceCashIn;
            var message = string.Format(R.ServiceCashInOutEnterAmount, localizedServiceDescription, Environment.NewLine, label);
            await SendEditMessageTextAsync(message);
        }
        private double ComputeFee()
        {
            try
            {
                //Get Service for selected provider
                Catalog? catalog = Service.Catalogs.FirstOrDefault(cat => cat.ProviderId == cData.ProviderId);

                if (catalog == null)
                {
                    throw new NullReferenceException(nameof(catalog));
                }

                //Get pricing lines
                var lines = JsonConvert.DeserializeObject<IList<Line>>(catalog.Pricing.Lines);

                //Get fee for amount
                var line = lines.FirstOrDefault(l => cData.Amount.Value >= l.From && cData.Amount.Value <= l.To);

                //Return fee
                return line.Fee < 1 ? cData.Amount.Value * line.Fee : line.Fee;
            }
            catch (Exception ex) when (ex is NullReferenceException or InvalidOperationException)
            {
                Program.Log.Error(ex.Message, ex);
                throw;
            }
        }
        private async Task SendComputeResult(double fee)
        {
            //Compose message
            var label = IsCashOut() ? R.ServiceCashOut : R.ServiceCashIn;
            string message = string.Format(R.ServiceCashInOutResult, label, cData.Amount, catalog.Provider.Name, fee);

            //Send message
            await SendEditMessageTextAsync(message, RetryKeyboard());
        }
        private InlineKeyboardMarkup? RetryKeyboard()
        {
            var items = new List<(string, string)>()
            {
                (string.Format(R.RetryProvider, Emojis.Bank), Trigger.RetryNewProvider.ToString()),
                (string.Format(R.Home, Emojis.House), "/start")
            };

            if (User.Session.ContextData == null)
            {
                return null;
            }

            if (cData.Amount.HasValue)
            {
                var itemToInsert = (string.Format(R.RetryAmount, Emojis.Dollar), Trigger.RetryNewAmount.ToString());
                items.Insert(0, itemToInsert);
            }

            return KeyboardUtil.GetInlineKeyboard(items, itemsPerRow: 1);
        }
        public override async Task ProcessAsync(string data)
        {
            await base.ProcessAsync(data);
            var count = Service.Parent?.Services.Count(s => s.Command.Equals(data)) ?? 0;
            var message = string.Empty;

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

                    if (count > 0) break;

                    if (Service.Catalogs.Count == 1 && !bool.Parse(data))
                    {
                        await stateMachine.FireAsync(Trigger.SingleProviderRefused);
                        break;
                    }

                    //Serialize properties
                    catalog = Service.Catalogs.Count == 1
                        ? Service.Catalogs[0]
                        : Service.Catalogs.SingleOrDefault(cat => cat.ProviderId == int.Parse(data));

                    var contextData = JsonUtil.SerializeToCashContext(catalog);

                    //Update User context
                    User.Session.ContextData = contextData;
                    await UpdateSessionAsync(User).ContinueWith(async _ => await stateMachine.FireAsync(Trigger.ProviderSent), TaskContinuationOptions.OnlyOnRanToCompletion);

                    break;
                }
                case State.AwaitingAmount:
                {
                    var amount = 0d;
                    try
                    {
                        //Get alledged amount provided by User
                        amount = double.Parse(data);
                        if (amount <= 0 || amount > double.MaxValue)
                        {
                            throw new InvalidOperationException($"{nameof(amount)} must have a value greater than zero");
                        }
                        //If amount is not in awaited range, throw
                        if (amount < cData.MinAmount || amount > cData.MaxAmount)
                        {
                            throw new ArgumentOutOfRangeException($"[{amount}] is not in range [{cData.MinAmount} - {cData.MaxAmount}]");
                        }

                        //Update User session ContextData
                        cData.Amount = amount;
                        User.Session.ContextData = JsonConvert.SerializeObject(cData);

                        await UpdateSessionAsync(User).ContinueWith(async _ => await stateMachine.FireAsync(Trigger.AmountSent), TaskContinuationOptions.OnlyOnRanToCompletion);
                    }
                    catch (Exception e)
                    {
                        Program.Log.Warn(e.Message, e);
                        if (e is FormatException)
                            message = string.Format(R.WarningInvalidAmountFormat, Emojis.Warning, Environment.NewLine);

                        if (e is ArgumentOutOfRangeException)
                            message = string.Format(R.WarningAmountOverflow, Emojis.Warning, Environment.NewLine, amount, cData.MinAmount, cData.MaxAmount);

                        if (Update.Type == UpdateType.Message)
                            await DeleteMessageAsync(chatId: Update.Message.Chat.Id, messageId: Update.Message.MessageId);

                        await SendEditMessageTextAsync(message);
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
                        return;

                    var retry = Enum.Parse<Trigger>(data);

                    if (retry == Trigger.RetryNewAmount)
                        await stateMachine.FireAsync(Trigger.RetryNewAmount);

                    else
                        await stateMachine.FireAsync(Trigger.RetryNewProvider);

                    break;
                }
                default: throw new InvalidOperationException($"Unknown State: {nameof(stateMachine.State)}");
            }
        }
    }
}