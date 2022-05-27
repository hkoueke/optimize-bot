using Newtonsoft.Json;
using OptimizeBot.Model;
using OptimizeBot.Utils;
using Stateless;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using R = OptimizeBot.Properties.Resources;

namespace OptimizeBot.Processes
{
    public class CashOut : Conversation
    {
        private string message;
        private readonly CashContext cData;
        private Catalog catalog;

        private readonly StateMachine<State, Trigger> stateMachine;

        private enum State { Idle, AwaitingProvider, AwaitingAmount, Done }

        private enum Trigger { AskProviderList, ProviderSent, SingleProviderRefused, AmountSent, RetryNewAmount, RetryNewProvider }

        public CashOut(ITelegramBotClient botClient, Update update, Service service) : base(botClient, update, service)
        {
            //Initialize this process with its own emoji
            serviceDesc = string.Concat(IsCashOut() ? Emojis.Dollar_Fly : Emojis.Dollar, " ", serviceDesc);

            //Initialize ContextData object
            if (user.Session.ContextData != null)
                cData = JsonUtil.DeserializeObject<CashContext>(user.Session.ContextData);

            //Initialize state machine
            stateMachine = new StateMachine<State, Trigger>(stateAccessor: () => GetSessionState<State>(),
                                                            stateMutator: async s => await UpdateSessionStateAsync(s));
            //Configure state machine
            stateMachine.Configure(State.Idle)
                        .Permit(Trigger.AskProviderList, State.AwaitingProvider);

            stateMachine.Configure(State.AwaitingProvider)
                        .OnEntryAsync(async () => await SendProviderList())
                        .Permit(Trigger.ProviderSent, State.AwaitingAmount)
                        .Permit(Trigger.SingleProviderRefused, State.Done)
                        .PermitReentry(Trigger.RetryNewProvider);

            stateMachine.Configure(State.AwaitingAmount)
                        .OnEntryAsync(async () => await AskAmount())
                        .OnExitAsync(async () => await Task
                           .Run(() => ComputeFee())
                           .ContinueWith(async t => await SendComputeResult(await t), TaskContinuationOptions.OnlyOnRanToCompletion))
                        .Permit(Trigger.AmountSent, State.Done)
                        .PermitReentry(Trigger.RetryNewAmount);

            stateMachine.Configure(State.Done)
                        .Permit(Trigger.RetryNewAmount, State.AwaitingAmount)
                        .Permit(Trigger.RetryNewProvider, State.AwaitingProvider);
        }

        private bool IsCashOut() => GetType().Equals(typeof(CashOut));

        private async Task SendProviderList()
        {
            var hasSingleEntry = service.Catalogs.Count == 1;

            var message = hasSingleEntry
                ? string.Format(R.ServiceSingleProvider, serviceDesc, Environment.NewLine, service.Catalogs[0].Provider.Name)
                : string.Format(R.ServiceSelectProvider, serviceDesc, Environment.NewLine, service.Catalogs.Count);

            var keyboard = hasSingleEntry
                ? KeyboardUtil.GetInlineKeyboard(Constants.YES_NO)
                : KeyboardUtil.GetInlineKeyboard(service.Catalogs, service.Parent, 1);

            //Send the message
            await SendEditMessageTextAsync(message, replyMarkup: keyboard);
        }

        private async Task AskAmount()
        {
            var label = IsCashOut() ? R.ServiceCashOut : R.ServiceCashIn;
            var message = string.Format(R.ServiceCashInOutEnterAmount, serviceDesc, Environment.NewLine, label);
            await SendEditMessageTextAsync(message);
        }

        private double ComputeFee()
        {
            try
            {
                //Get Service for selected provider
                catalog = service.Catalogs.SingleOrDefault(ps => ps.ProviderId == cData.ProviderId);

                //Get pricing lines
                var lines = JsonConvert.DeserializeObject<IList<Line>>(catalog.Pricing.Lines);

                //Get fee for amount
                var line = lines.SingleOrDefault(l => cData.Amount.Value >= l.From && cData.Amount.Value <= l.To);

                return line.Fee < 1 ? cData.Amount.Value * line.Fee : line.Fee;
            }
            catch (Exception ex) when (ex is NullReferenceException or InvalidOperationException)
            {
                Program.log.Error(ex.Message, ex);
                throw;
            }
        }

        private async Task SendComputeResult(double fee)
        {
            //Compose message
            var label = IsCashOut() ? R.ServiceCashOut : R.ServiceCashIn;
            message = string.Format(R.ServiceCashInOutResult, label, cData.Amount, catalog.Provider.Name, fee);

            //Send message
            await SendEditMessageTextAsync(message, RetryKeyboard());
        }

        private InlineKeyboardMarkup RetryKeyboard()
        {
            var items = new List<(string, string)>()
            {
                (string.Format(R.RetryProvider, Emojis.Bank), Enum.GetName(Trigger.RetryNewProvider)),
                (string.Format(R.Home, Emojis.House), "/start")
            };

            if (user.Session.ContextData == null) 
                return null;

            if (cData.Amount.HasValue)
            {
                var itemToInsert = (string.Format(R.RetryAmount, Emojis.Dollar), Enum.GetName(Trigger.RetryNewAmount));
                items.Insert(0, itemToInsert);
            }

            return KeyboardUtil.GetInlineKeyboard(items, itemsPerRow: 1);
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
                        : service.Catalogs.SingleOrDefault(cat => cat.ProviderId == int.Parse(data));

                    var contextData = JsonUtil.SerializeToCashContext(catalog);

                    //Update user context
                    user.Session.ContextData = contextData;

                    await UpdateSessionAsync(user)
                        .ContinueWith(async _ => await stateMachine.FireAsync(Trigger.ProviderSent), TaskContinuationOptions.OnlyOnRanToCompletion);

                    break;
                }
                case State.AwaitingAmount:
                {
                    var amount = 0d;

                    try
                    {
                        //Get alledged amount provided by user
                        amount = double.Parse(data);

                        if (amount <= 0 || amount > double.MaxValue)
                            throw new InvalidOperationException($"{nameof(amount)} must have a value greater than zero");

                        //If amount is not in awaited range, throw
                        if (amount < cData.MinAmount || amount > cData.MaxAmount)
                            throw new ArgumentOutOfRangeException($"[{amount}] is not in range [{cData.MinAmount} - {cData.MaxAmount}]");

                        //Update user session ContextData
                        cData.Amount = amount;
                        user.Session.ContextData = JsonConvert.SerializeObject(cData);

                        await UpdateSessionAsync(user)
                            .ContinueWith(async _ => await stateMachine.FireAsync(Trigger.AmountSent), TaskContinuationOptions.OnlyOnRanToCompletion);
                    }
                    catch (Exception e)
                    {
                        Program.log.Warn(e.Message, e);

                        if (e is FormatException) 
                            message = string.Format(R.WarningInvalidAmountFormat, Emojis.Warning, Environment.NewLine);
                        
                        if (e is ArgumentOutOfRangeException) 
                            message = string.Format(R.WarningAmountOverflow, Emojis.Warning, Environment.NewLine, amount, cData.MinAmount, cData.MaxAmount);

                        if (update.Type == UpdateType.Message)
                            await DeleteMessageAsync(chatId: update.Message.Chat.Id, messageId: update.Message.MessageId);

                        await SendEditMessageTextAsync(message);
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

                    if (retry == Trigger.RetryNewAmount) await stateMachine.FireAsync(Trigger.RetryNewAmount);
                    else await stateMachine.FireAsync(Trigger.RetryNewProvider);

                    break;
                }
                default: throw new InvalidOperationException($"Unknown State: {nameof(stateMachine.State)}");
            }
        }
    }
}