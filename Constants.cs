using System.Collections.Generic;
using R = OptimizeBot.Properties.Resources;

namespace OptimizeBot
{
    public class Constants
    {
        public const string BOT_TOKEN = "YOUR_BOT_TOKEN";
        public const string DEV_LINK_TELEGRAM = @"https://t.me/YOUR_TELEGRAM_USERNAME";
        public static readonly string[] ADMINS = { "TELEGRAM_ID_01","TELEGRAM_ID_02" };
        public const double CACHE_EXPIRY_HOURS = 24;
        public const double CACHE_EXPIRY_DAYS = 7;
        public static readonly List<KeyValuePair<bool, string>> YES_NO = new()
        {
            new KeyValuePair<bool, string>(true, R.Yes),
            new KeyValuePair<bool, string>(false, R.No)
        };
    }
}
