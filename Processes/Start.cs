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
    public sealed class Start : Conversation
    {
        private readonly StateMachine<State, Trigger> stateMachine;
        private enum State { Idle, MenuServed }
        private enum Trigger { AskMenu }

        public Start(ITelegramBotClient botClient, Update update, Service service) : base(botClient, update, service)
        {
            //Initialize this process with its own emoji
            serviceDesc = string.Concat(Emojis.House, " ", serviceDesc);

            stateMachine = new StateMachine<State, Trigger>(stateAccessor: () => GetSessionState<State>(),
                                                            stateMutator: async s => await UpdateSessionStateAsync(s));
            stateMachine
                .Configure(State.Idle)
                .Permit(Trigger.AskMenu, State.MenuServed);

            stateMachine
                .Configure(State.MenuServed)
                .Ignore(Trigger.AskMenu)
                .OnEntryAsync(t => ShowMenu());
        }
       
        private async Task ShowMenu()
        {
            //Build the InlineKeyboardMarkup and send as message to client
            var keyboard = KeyboardUtil.GetInlineKeyboard(service.Services, service.Parent, 1);

            //Format the message to show to the user
            var message = string.Format(R.WelcomeUserSelectService, serviceDesc, Environment.NewLine, UpdateUtil.GetSenderFromUpdate(update).FirstName);

            //Send message
            await SendEditMessageTextAsync(message, replyMarkup: keyboard);
        }

        public override async Task Process(string data)
        {
            // Let the base class do preliminary checks and initialization.
            await base.Process(data);

            //Do not serve again.
            if (stateMachine.State == State.MenuServed)
            {
                if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
                    await DeleteMessageAsync(chatId: update.Message.Chat.Id, messageId: update.Message.MessageId);
                return;
            }

            //Fire the machine.
            if (stateMachine.State == State.Idle) 
                await stateMachine.FireAsync(Trigger.AskMenu);
        }
    }
}