using System;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace OptimizeBot.Extensions
{
    public static class UpdateExtensions
    {
        public static User GetUser(this Update update) => update.Type switch
        {
            UpdateType.Message => update.Message!.From!,
            UpdateType.InlineQuery => update.InlineQuery!.From,
            UpdateType.ChosenInlineResult => update.ChosenInlineResult!.From,
            UpdateType.CallbackQuery => update.CallbackQuery!.From,
            _ => throw new NotImplementedException(nameof(update.Type)),
        };

        public static string GetUpdateTypeMessage(this Update update) => update.Type switch
        {
            UpdateType.Message => update.Message!.Text!.Trim(),
            UpdateType.InlineQuery => update.InlineQuery!.Query.Trim(),
            UpdateType.ChosenInlineResult => update.ChosenInlineResult!.ResultId.Trim(),
            UpdateType.CallbackQuery => update.CallbackQuery!.Data!.Trim(),
            _ => throw new NotImplementedException(nameof(update.Type)),
        };

        public static int GetUpdateTypeMessageId(this Update update) => update.Type switch
        {
            UpdateType.Message => update.Message!.MessageId,
            UpdateType.InlineQuery => int.Parse(update.InlineQuery!.Id),
            UpdateType.ChosenInlineResult => int.Parse(update.ChosenInlineResult!.InlineMessageId!),
            UpdateType.CallbackQuery => update.CallbackQuery!.Message!.MessageId,
            _ => throw new NotSupportedException($"Update type <{update.Type}> is not supported"),
        };
    }
}
