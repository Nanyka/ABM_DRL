namespace Sugarscape
{
    [System.Serializable]
    public class MetricData
    {
        public float TradeCount;
        public int AliveAgent;
        public float MarketPrice;
        public float Inequality;
        public float AverageWelfare;
        public float CRRatio;
        public float HardCodeAgentPercentage;
        public bool IsEnd;
        public AgentInfo[] Agents;
        public float[] TradePrices; // Rule-T price of each trade executed during this tick
    }
}