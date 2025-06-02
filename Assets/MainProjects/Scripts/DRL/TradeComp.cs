using System.Collections.Generic;
using UnityEngine;

namespace Sugarscape
{
    public class TradeComp : MonoBehaviour, ITradeComp
    {
        private IAgentController m_AgentController;
        private int m_MetabolismSugar;
        private int m_MetabolismSpice;
        [SerializeField] private List<float> m_Prices = new();
        [SerializeField] private List<int> m_Partners = new();

        public void Init(IAgentController agentController, int metabolismSugar, int metabolismSpice)
        {
            m_AgentController = agentController;
            m_MetabolismSugar = metabolismSugar;
            m_MetabolismSpice = metabolismSpice;
            Reset();
        }

        public float CalculateWelfare(float sugarAmt, float spiceAmt)
        {
            // total metabolism > 0 by design
            float mTotal = m_MetabolismSugar + m_MetabolismSpice;
            // Cobb-Douglas form from GAS p.97
            return Mathf.Pow(sugarAmt, m_MetabolismSugar / mTotal)
                   * Mathf.Pow(spiceAmt, m_MetabolismSpice / mTotal);
        }

        public float CalculateMRS(float sugarAmt, float spiceAmt)
        {
            // Assumes metabolismSugar & metabolismSpice are never zero
            return (spiceAmt / m_MetabolismSpice) / (sugarAmt / m_MetabolismSugar);
        }

        private (int sugarExchanged, int spiceExchanged) CalculateSellSpiceAmount(float price)
        {
            int sugarExchanged;
            int spiceExchanged;

            if (price >= 1f)
            {
                sugarExchanged = 1;
                // cast to int truncates toward zero, same as Python's int()
                spiceExchanged = (int)price;
            }
            else
            {
                sugarExchanged = (int)(1f / price);
                spiceExchanged = 1;
            }

            return (sugarExchanged, spiceExchanged);
        }

        private void SellSpice(IAgentController other, int sugarAmount, int spiceAmount)
        {
            // This agent receives sugarAmount, loses spiceAmount
            m_AgentController.ChangeSugar(sugarAmount);
            other.ChangeSugar(-sugarAmount);
            m_AgentController.ChangeSpice(-spiceAmount);
            m_AgentController.ChangeSpice(spiceAmount);
        }

        public bool MaybeSellSpice(
            IAgentController buyer,
            float price,
            float welfareSelf,
            float welfareBuyer
        )
        {
            // 1) Determine exchange amounts
            var (sugarExchanged, spiceExchanged) = CalculateSellSpiceAmount(price);

            // 2) “Simulate” post-trade holdings
            float selfSugarAfter = m_AgentController.RemainSugar() + sugarExchanged;
            float bSugarAfter = buyer.RemainSugar() - sugarExchanged;
            float selfSpiceAfter = m_AgentController.RemainSpice() - spiceExchanged;
            float bSpiceAfter = buyer.RemainSpice() + spiceExchanged;

            // 3) Check neither would go to zero or negative
            if (selfSugarAfter <= 0f || bSugarAfter <= 0f
                                     || selfSpiceAfter <= 0f || bSpiceAfter <= 0f)
            {
                Debug.Log("Can't trade since one side will be died");
                return false;
            }

            // 4) Criterion #1: Both agents’ welfare strictly increases
            bool bothBetterOff =
                (welfareSelf < CalculateWelfare(selfSugarAfter, selfSpiceAfter))
                && (welfareBuyer < CalculateWelfare(bSugarAfter, bSpiceAfter));

            if (!bothBetterOff)
            {
                Debug.Log("Can't trade since at least one of them worse off");
                return false;
            }

            // 5) Criterion #2: Their MRS would cross (after the hypothetical trade)
            float mrsSelfAfter = CalculateMRS(selfSugarAfter, selfSpiceAfter);
            float mrsBuyerAfter = buyer.GetTradeComp().CalculateMRS(bSugarAfter, bSpiceAfter);

            // In Python: “mrs_not_crossing = self.calculate_MRS(...) > other.calculate_MRS(...)”
            // So require that to be true
            if (!(mrsSelfAfter > mrsBuyerAfter))
            {
                Debug.Log($"Can't trade since MRS crossing: {mrsSelfAfter} vs {mrsBuyerAfter}");
                return false;
            }

            // 6) All criteria met → execute the resource exchange
            Debug.Log($"Amount of sugar: {sugarExchanged} vs spice: {spiceExchanged}");
            SellSpice(buyer, sugarExchanged, spiceExchanged);
            return true;
        }

        public void Trade(IAgentController other, bool isPrint = false)
        {
            if (other == null) return;
            
            // 1) Sanity: both must have > 0 resources
            Debug.Assert(m_AgentController.RemainSugar() > 0f, "This agent’s sugar must be > 0");
            Debug.Assert(m_AgentController.RemainSpice() > 0f, "This agent’s spice must be > 0");
            Debug.Assert(other.RemainSugar() > 0f, "Other agent’s sugar must be > 0");
            Debug.Assert(other.RemainSpice() > 0f, "Other agent’s spice must be > 0");
            
            // 2) Compute MRS for both
            float mrsSelf = CalculateMRS(m_AgentController.RemainSugar(), m_AgentController.RemainSpice());
            float mrsOther = other.GetTradeComp().CalculateMRS(other.RemainSugar(), other.RemainSpice());
            
            if (isPrint)
            {
                Debug.Log($"==> MRS: Agent {m_AgentController.GetAgentID()} ({m_AgentController.RemainSugar()}/" +
                          $"{m_AgentController.RemainSpice()}) " +
                          $"trade with agent {other.GetAgentID()} ({other.RemainSugar()}/{other.RemainSpice()})");
            }

            // 3) If they’re effectively equal, no trade
            if (Mathf.Abs(mrsSelf - mrsOther) < Mathf.Epsilon)
            {
                Debug.Log($"Can't trade since not enough MRS distance: {mrsSelf}/{mrsOther}");
                return;
            }

            // 4) Compute price = sqrt(mrsSelf * mrsOther)
            float price = Mathf.Sqrt(mrsSelf * mrsOther);

            if (isPrint) Debug.Log($"Expected price: {price} ({mrsSelf}/{mrsOther})");

            // 5) Compute each agent’s current welfare
            float welfareSelf = CalculateWelfare(m_AgentController.RemainSugar(), m_AgentController.RemainSpice());
            float welfareOther = other.GetTradeComp().CalculateWelfare(other.RemainSugar(), other.RemainSpice());

            bool sold;
            if (mrsSelf > mrsOther)
            {
                // This agent wants to buy sugar; “other” must sell spice
                sold = MaybeSellSpice(
                    buyer: other,
                    price: price,
                    welfareSelf: welfareSelf,
                    welfareBuyer: welfareOther
                );
                if (!sold)
                    return;
            }
            else
            {
                // Other wants to buy sugar; this agent must sell spice
                sold = other.GetTradeComp().MaybeSellSpice(
                    buyer: m_AgentController,
                    price: price,
                    welfareSelf: welfareOther,
                    welfareBuyer: welfareSelf
                );
                if (!sold)
                    return;
            }

            // 6) Record the successful-trade data
            m_Prices.Add(price);
            m_Partners.Add(other.GetAgentID());

            // 7) Recurse to continue trading until no further beneficial trade
            //    (If you expect very deep recursion, consider converting to a loop.)
            Trade(other);
        }

        public void Reset()
        {
            m_Prices.Clear();
            m_Partners.Clear();
        }
    }
}