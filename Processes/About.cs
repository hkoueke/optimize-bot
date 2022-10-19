using OptimizeBot.Contracts.Caching;
using OptimizeBot.Contracts.Messaging;
using OptimizeBot.Contracts.Persistance;
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
    public class About : Conversation
    {
        private readonly StateMachine<State, Trigger> stateMachine;
        private enum State { Idle, InfoServed }
        private enum Trigger { AskAbout }
        public About(ICacheManager cacheManager,
                     IRepositoryManager repositoryManager,
                     IMessagingService messagingService,
                     Update update,
                     Service service) : base(cacheManager, repositoryManager, messagingService, update, service)
        {
            //Initialize this process with its own emoji
            localizedServiceDescription = string.Concat(Emojis.Robot, " ", localizedServiceDescription);
            stateMachine = new StateMachine<State, Trigger>(stateAccessor: async () => await GetSessionStateAsync<State>(),
                                                            stateMutator: async state => await UpdateSessionStateAsync(state));
            stateMachine
                .Configure(State.Idle)
                .Permit(Trigger.AskAbout, State.InfoServed);

            stateMachine
                .Configure(State.InfoServed)
                .Ignore(Trigger.AskAbout)
                .OnEntryAsync(async _ => await ShowAbout());
        }
        private async Task ShowAbout()
        {
            //Format the message to show to the user
            var message = string.Format(R.About, localizedServiceDescription, Environment.NewLine, Constants.DEV_LINK_TELEGRAM);
            await SendEditMessageTextAsync(message);
        }
        public override async Task ProcessAsync(string data)
        {
            await base.ProcessAsync(data);

            //Do not serve again.
            if (stateMachine.State == State.InfoServed)
            {
                if (Update.Type == UpdateType.Message)
                    await DeleteMessageAsync(chatId: Update.Message!.Chat.Id, messageId: Update.Message.MessageId);
                return;
            }

            //Fire the machine.
            if (stateMachine.State == State.Idle)
            {
                await stateMachine.FireAsync(Trigger.AskAbout);
            }
        }
    }
}
