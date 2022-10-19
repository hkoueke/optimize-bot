using System;
using System.Globalization;

namespace OptimizeBot.Extensions
{
    public static class LanguageExtensions
    {
        public static bool IsEnglish(this CultureInfo cultureInfo) 
            => cultureInfo.TwoLetterISOLanguageName.Equals("en", StringComparison.OrdinalIgnoreCase);

    }
}
