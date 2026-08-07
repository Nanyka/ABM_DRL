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
    }
}