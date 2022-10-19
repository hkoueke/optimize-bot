using OptimizeBot.Contracts.Caching;
using OptimizeBot.Contracts.Messaging;
using OptimizeBot.Contracts.Persistance;
using OptimizeBot.Extensions;
using OptimizeBot.Model;
using OptimizeBot.Utils;
using Stateless;
using System;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using R = OptimizeBot.Properties.Resources;

namespace OptimizeBot.Processes
{
    public sealed class Start : Conversation
    {
        private readonly StateMachine<State, Trigger> stateMachine;
        private enum State : byte { Idle, MenuServed }
        private enum Trigger : byte { AskMenu }
        public Start(ICacheManager cacheManager,
                     IRepositoryManager repositoryManager,
                     IMessagingService messagingService,
                     Update update,
                     Service service) : base(cacheManager, repositoryManager, messagingService, update, service)
        {
            //Initialize this process with its own emoji
            localizedServiceDescription = string.Concat(Emojis.House, " ", localizedServiceDescription);

            stateMachine = new StateMachine<State, Trigger>(stateAccessor: async() => await GetSessionStateAsync<State>(),
                                                            stateMutator: async s => await UpdateSessionStateAsync(s));
            stateMachine
                .Configure(State.Idle)
                .Permit(Trigger.AskMenu, State.MenuServed);

            stateMachine
                .Configure(State.MenuServed)
                .Ignore(Trigger.AskMenu)
                .OnEntryAsync(_ => ShowMenu());
        }

        private async Task ShowMenu()
        {
            //Build the InlineKeyboardMarkup and send as message to client
            var keyboard = KeyboardUtil.GetInlineKeyboard(Service.Services, Service.Parent, 1);
            //Format the message to show to the user
            var message = string.Format(R.WelcomeUserSelectService,
                                        localizedServiceDescription,
                                        Environment.NewLine,
                                        Update.GetUser().FirstName);
            //Send message
            await SendEditMessageTextAsync(message, replyMarkup: keyboard);
        }
        public override async Task ProcessAsync(string data)
        {
            // Let the base class do preliminary checks and initialization.
            await base.ProcessAsync(data);

            //Do not serve again.
            if (stateMachine.State == State.MenuServed)
            {
                if (Update.Type == UpdateType.Message)
                    await DeleteMessageAsync(chatId: Update.Message!.Chat.Id, messageId: Update.Message.MessageId);
                return;
            }

            //Fire the machine.
            if (stateMachine.State == State.Idle)
                await stateMachine.FireAsync(Trigger.AskMenu);
        }
    }
}