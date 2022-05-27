using System.Globalization;

namespace OptimizeBot.Utils
{
    public static class LangUtil
    {
        public static bool IsEnglish()
        {
            //Set Culture for this user
            var uiCulture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            return uiCulture.Equals("en");
        }
    }
}
