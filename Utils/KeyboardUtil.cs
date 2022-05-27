using OptimizeBot.Model;
using System;
using System.Collections.Generic;
using Telegram.Bot.Types.ReplyMarkups;
using R = OptimizeBot.Properties.Resources;

namespace OptimizeBot.Utils
{
    public static class KeyboardUtil
    {
        private static int GetRowCount(in int itemCount, int buttonsPerRow = 2)
        {
            if (buttonsPerRow <= 0 || buttonsPerRow > 2) 
                buttonsPerRow = 2;

            return itemCount % buttonsPerRow == 0 
                ? itemCount / buttonsPerRow 
                : itemCount / buttonsPerRow + 1;
        }

        private static int GetItemCount<T>(in ICollection<T> items, in bool hasParent = default) 
            => hasParent ? items.Count + 1 : items.Count;

        public static InlineKeyboardMarkup GetInlineKeyboard<T>(in IList<T> items, in Service parent = default, in int itemsPerRow = 2)
        {
            if (items == null) 
                throw new ArgumentNullException(nameof(items));
            
            if (items.Count == 0) 
                throw new InvalidOperationException($"{nameof(items)} must contain at least one element");

            //How many Menu items are provided
            var count = GetItemCount(items, parent == null);

            //How many rows keyboard will have
            var rowCount = GetRowCount(count, itemsPerRow);

            //Instantiate row builder
            var rows = new List<IList<InlineKeyboardButton>>(rowCount);

            //Hold number of rows processed
            var index = 0;

            for (var i = 0; i < count; i++)
            {
                var row = new List<InlineKeyboardButton>(itemsPerRow);

                if (i == count - 1 && parent != null)
                {
                    var back = string.Format(R.BackButtonText, LangUtil.IsEnglish() ? parent.EnDesc : parent.FrDesc);
                    var backButtonTxt = string.Concat(Emojis.House, " ", back);

                    row.Add(InlineKeyboardButton.WithCallbackData(backButtonTxt, $"{parent.Command}"));
                    rows.Add(row);
                    break;
                }

                for (var j = 0; j < itemsPerRow; j++)
                {
                    switch (items)
                    {
                        case IList<Service> services:
                        {
                            var service = LangUtil.IsEnglish() ? services[i].EnDesc : services[i].FrDesc; 
                            var serviceTxt = string.Concat(Emojis.Arrow_Left, " ", service);
                            row.Add(InlineKeyboardButton.WithCallbackData(serviceTxt, services[i].Command));
                            break;
                        }
                        case IList<Catalog> catalogs:
                        {
                            var provider = catalogs[i].Provider.Name;
                            var providerTxt = string.Concat(Emojis.Arrow_Left, " ", provider);
                            row.Add(InlineKeyboardButton.WithCallbackData(providerTxt, catalogs[i].Provider.ProviderId.ToString()));
                            break;
                        }
                        case IList<KeyValuePair<bool, string>> boolAnswers:
                        {
                            var value = string.Concat(boolAnswers[i].Key ? Emojis.CheckMark_Yes : Emojis.CheckMark_No, " ", boolAnswers[i].Value);
                            row.Add(InlineKeyboardButton.WithCallbackData(value, boolAnswers[i].Key.ToString()));
                            break;
                        }
                        case IList<(string, string)> retries:
                        {
                            row.Add(InlineKeyboardButton.WithCallbackData(retries[i].Item1, retries[i].Item2));
                            break;
                        }
                    }

                    if (row.Count < itemsPerRow) i++;
                }

                rows.Insert(index, row);
                index++;
            }

            return new InlineKeyboardMarkup(rows);
        }
    }
}
