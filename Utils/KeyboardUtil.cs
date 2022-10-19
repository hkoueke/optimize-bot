using OptimizeBot.Extensions;
using OptimizeBot.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using Telegram.Bot.Types.ReplyMarkups;
using R = OptimizeBot.Properties.Resources;

namespace OptimizeBot.Utils
{
    public static class KeyboardUtil
    {
        private static int GetRowCount(int itemCount, int buttonsPerRow = 2)
        {
            if (buttonsPerRow <= 0 || buttonsPerRow > 2)
            {
                buttonsPerRow = 2;
            }
            return
                itemCount % buttonsPerRow == 0
                ? itemCount / buttonsPerRow
                : itemCount / buttonsPerRow + 1;
        }

        private static int GetItemCount<T>(ICollection<T> items, bool hasParent = default) => hasParent ? items.Count + 1 : items.Count;

        public static InlineKeyboardMarkup GetInlineKeyboard<T>(IList<T> items, Service? parent = default, int itemsPerRow = 2)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (items.Count == 0) throw new ArgumentException($"{nameof(items)} must contain at least one element");

            //How many Menu items are provided
            var count = GetItemCount(items, parent is null);

            //How many rows keyboard will have
            var rowCount = GetRowCount(count, itemsPerRow);

            //Instantiate row builder
            var rows = new List<IList<InlineKeyboardButton>>(rowCount);

            //Hold number of rows processed
            var processed = 0;

            for (var i = 0; i < count; i++)
            {
                var row = new List<InlineKeyboardButton>(itemsPerRow);
                if (i == count - 1 && parent is not null)
                {
                    string back = string.Format(R.BackButtonText, CultureInfo.CurrentCulture.IsEnglish() ? parent.EnDesc : parent.FrDesc);
                    string backButtonTxt = string.Concat(Emojis.House, " ", back);
                    row.Add(InlineKeyboardButton.WithCallbackData(backButtonTxt, $"{parent.Command}"));
                    rows.Add(row);
                    break;
                }

                for (var j = 0; j < itemsPerRow; j++)
                {
                    switch (items.GetType())
                    {
                        case IList<Service> services:
                        {
                            string service = CultureInfo.CurrentCulture.IsEnglish() ? services[i].EnDesc : services[i].FrDesc;
                            string serviceTxt = string.Concat(Emojis.Arrow_Right, " ", service);
                            row.Add(InlineKeyboardButton.WithCallbackData(serviceTxt, services[i].Command));
                            break;
                        }
                        case IList<Catalog> catalogs:
                        {
                            string provider = catalogs[i].Provider.Name;
                            string providerTxt = string.Concat(Emojis.Arrow_Right, " ", provider);
                            row.Add(InlineKeyboardButton.WithCallbackData(providerTxt, catalogs[i].Provider.ProviderId.ToString()));
                            break;
                        }
                        case IList<KeyValuePair<bool, string>> boolAnswers:
                        {
                            string value = string.Concat(boolAnswers[i].Key ? Emojis.CheckMark_Yes : Emojis.CheckMark_No, " ", boolAnswers[i].Value);
                            row.Add(InlineKeyboardButton.WithCallbackData(value, boolAnswers[i].Key.ToString()));
                            break;
                        }
                        case IList<(string, string)> retries:
                        {
                            row.Add(InlineKeyboardButton.WithCallbackData(retries[i].Item1, retries[i].Item2));
                            break;
                        }
                    }

                    if (row.Count < itemsPerRow)
                    {
                        i++;
                    }
                }

                rows.Insert(processed, row);
                processed++;
            }
            return new InlineKeyboardMarkup(rows);
        }
    }
}
