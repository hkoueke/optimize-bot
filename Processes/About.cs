using OptimizeBot.Model;
using OptimizeBot.Utils;
using Stateless;
using System;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using R = OptimizeBot.Properties.Resources;

namespace OptimizeBot.Processes
{
    public class About : Conversation
    {
        private readonly StateMachine<State, Trigger> stateMachine;

        private enum State { Idle, InfoServed }
        private enum Trigger { AskAbout }

        public About(ITelegramBotClient bot, Update update, Service service) : base(bot, update, service)
        {
            //Initialize this process with its own emoji
            serviceDesc = string.Concat(Emojis.Robot, " ", serviceDesc);

            stateMachine = new StateMachine<State, Trigger>(stateAccessor: () => GetSessionState<State>(),
                                                            stateMutator: async s => await UpdateSessionStateAsync(s));
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
            var message = string.Format(R.About, serviceDesc, Environment.NewLine, Constants.DEV_LINK_TELEGRAM);
            await SendEditMessageTextAsync(message);
        }

        public override async Task Process(string data)
        {
            await base.Process(data);

            //Do not serve again.
            if (stateMachine.State == State.InfoServed)
            {
                if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
                    await DeleteMessageAsync(chatId: update.Message.Chat.Id, messageId: update.Message.MessageId);
                return;
            }

            //Fire the machine.
            if (stateMachine.State == State.Idle)
                await stateMachine.FireAsync(Trigger.AskAbout);
        }
    }
}
