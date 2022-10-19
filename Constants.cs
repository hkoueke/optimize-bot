using Microsoft.Extensions.Caching.Memory;
using OptimizeBot.Objects;
using System;
using System.Collections.Generic;
using R = OptimizeBot.Properties.Resources;

namespace OptimizeBot
{
    public class Constants
    {
        public const string BOT_TOKEN = "YOUR_BOT_TOKEN";
        public const string DEV_LINK_TELEGRAM = @"https://t.me/RKV237";
        public static readonly string[] ADMINS = { "TELEGRAM_ID_01", "TELEGRAM_ID_02" };
        public static readonly List<KeyValuePair<bool, string>> YES_NO = new()
        {
            new KeyValuePair<bool, string>(true, R.Yes),
            new KeyValuePair<bool, string>(false, R.No)
        };
        public static readonly MemoryCacheWithPolicy CACHE_DEFAULT_OBJECT = new (1024, new()
        {
            Size = 256,
            Priority = CacheItemPriority.High,
            AbsoluteExpiration = DateTimeOffset.Now.AddDays(1),
            SlidingExpiration = TimeSpan.FromHours(1)
        });
    }
}
