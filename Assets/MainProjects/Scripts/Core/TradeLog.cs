using System.Collections.Generic;

namespace Sugarscape
{
    // Per-tick record of the Rule-T price of every executed trade.
    // Only filled while a statistics receiver is connected, so training runs do not accumulate it.
    public static class TradeLog
    {
        private static readonly List<float> s_Prices = new();

        public static bool Enabled { get; set; }

        public static void Record(float price)
        {
            if (Enabled) s_Prices.Add(price);
        }

        public static float[] Drain()
        {
            var prices = s_Prices.ToArray();
            s_Prices.Clear();
            return prices;
        }

        public static void Clear() => s_Prices.Clear();
    }
}
