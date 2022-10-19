using Newtonsoft.Json;
using OptimizeBot.Model;
using System.Collections.Generic;
using System.Linq;

namespace OptimizeBot.Utils
{
    public static class JsonUtil
    {
        public static string SerializeToCashContext(in Catalog catalog)
        {
            var lines = JsonConvert.DeserializeObject<IList<Line>>(catalog.Pricing.Lines);
            double minAmt = lines.Min(l => l.From);
            double maxAmt = lines.Max(l => l.To);
            var pId = catalog.ProviderId;
            return JsonConvert.SerializeObject(new CashContext(pId, minAmt, maxAmt));
        }
        public static T DeserializeObject<T>(in string contextData) where T : class
            => JsonConvert.DeserializeObject<T>(contextData);
    }
}
