using OptimizeBot.Cache;
using OptimizeBot.Model;
using OptimizeBot.Utils;
using System;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramUser = Telegram.Bot.Types.User;
using User = OptimizeBot.Model.User;

namespace OptimizeBot.Processes
{
    public abstract class Conversation
    {
        private readonly ITelegramBotClient bot;
        protected readonly Update update;
        protected readonly Service service;
        protected readonly TelegramUser sender;
        protected string serviceDesc;
        protected readonly User user;

        protected Conversation(ITelegramBotClient bot, Update update, Service service)
        {
            (this.bot, this.update, this.service) = (bot, update, service);

            //Initialize TelegramUser field
            sender = UpdateUtil.GetSenderFromUpdate(update);
            user = CacheMgr.GetEntry<User>(sender.Id);

            //Initialize service description field
            serviceDesc = LangUtil.IsEnglish() ? service.EnDesc : service.FrDesc;
        }

        public virtual async Task Process(string data)
        {
            //-> Restrict service usage to admins
            if (service.AdminOnly && !user.IsAdmin)
                throw new InvalidOperationException($"Command <{ service.Command }> is restricted to admins");

            if (user.Session.Context == null || !user.Session.Context.Equals(service.Command))
            {
                user.Session.Context = service.Command;
                user.Session.ContextData = default;
                user.Session.State = default;
                await UpdateSessionAsync(user);
            }
        }

        protected async Task SendEditMessageTextAsync(string message, InlineKeyboardMarkup replyMarkup = default, ParseMode? parseMode = ParseMode.Html)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            // Get the last message sent by the bot and by the user when replying to a message
            var lastMessageId = CacheMgr.GetEntry(sender.Id);
            var sentId = UpdateUtil.GetMessageIdFromUpdate(update);

            // If there is no id in cache consider the app was restarted or cache entry dropped.
            // If the cached id is less than the id sent by user reply there is a 'separator' message
            // In both cases, send a regular message and save the message id to cache
            if (!lastMessageId.HasValue || lastMessageId.Value < sentId)
            {
                await SendTextMessageAsync(message, replyMarkup, parseMode)
                    .ContinueWith(async t => CacheMgr.SetOrRemoveEntry(sender.Id, (await t).MessageId), TaskContinuationOptions.OnlyOnRanToCompletion);
                return;
            }

            // Send an Edit message to user without caching MessageId since it does not change between two MessageEdit updates
            await bot.EditMessageTextAsync(sender.Id, lastMessageId.Value, message, parseMode, replyMarkup: replyMarkup);
        }

        private async Task<Message> SendTextMessageAsync(string message, IReplyMarkup replyMarkup = default, ParseMode? parseMode = ParseMode.Html)
        {
            await bot.SendChatActionAsync(chatId: sender.Id, chatAction: ChatAction.Typing);
            return await bot.SendTextMessageAsync(sender.Id, message, parseMode, replyMarkup: replyMarkup ?? new ReplyKeyboardRemove());
        }

        protected async Task SendDocumentAsync(InputOnlineFile file, IReplyMarkup replyMarkup = default)
        {
            //Show ChatAction.UploadDocument to client device
            await bot.SendChatActionAsync(chatId: sender.Id, chatAction: ChatAction.UploadDocument);

            //Send the downloaded file to user and delete lastMessageId from Cache
            //This will force the app to send a new message under the document
            await bot
                .SendDocumentAsync(sender.Id, file, replyMarkup: replyMarkup ?? new ReplyKeyboardRemove())
                .ContinueWith(_ => CacheMgr.SetOrRemoveEntry(sender.Id), TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        protected async Task DeleteMessageAsync(long chatId, int messageId) => await bot.DeleteMessageAsync(chatId, messageId);

        protected TEnum GetSessionState<TEnum>() where TEnum : struct
            => string.IsNullOrEmpty(user.Session.State) ? Enum.Parse<TEnum>("Idle") : Enum.Parse<TEnum>(user.Session.State);

        protected async Task UpdateSessionStateAsync<TEnum>(TEnum state) where TEnum : struct, Enum
        {
            user.Session.State = Enum.GetName(state);
            await UpdateSessionAsync(user);
        }

        protected static async Task UpdateSessionAsync(User userToUpdate)
        {
            await DbUtil.UpdateOrInsertAsync(userToUpdate)
                        .ContinueWith(async t => CacheMgr.RemoveEntry((await t).TelegramId), TaskContinuationOptions.OnlyOnRanToCompletion);
        }
    }
}
