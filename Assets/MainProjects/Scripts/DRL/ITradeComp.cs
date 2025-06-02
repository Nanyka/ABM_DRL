namespace Sugarscape
{
    public interface ITradeComp
    {
        public void Init(IAgentController agentController, int metabolismSugar, int metabolismSpice);
        public float CalculateWelfare(float sugarAmt, float spiceAmt);
        public float CalculateMRS(float sugarAmt, float spiceAmt);

        public bool MaybeSellSpice(
            IAgentController buyer,
            float price,
            float welfareSelf,
            float welfareBuyer
        );
        public void Trade(IAgentController other, bool isPrint=false);
        public void Reset();
    }
}