

namespace Sugarscape
{
    public class AgentForTrainOnly : TradingAgent
    {
        public override void MayBeDie()
        {
            RecordDeltaWelfare(); // from v6.3 and below
            // Debug.Log($"Current accumulate welfare: {m_CurrentDeltaWelfare}");
            if (m_RemainSugar <= 0 || m_RemainSpice <= 0 || m_Age >= gameSettings.maxFertilityAge)
            {
                OnAgentDie(true);
            }
            else
            {
                m_CurrentMrs = m_TradeComp.CalculateMRS(m_RemainSugar, m_RemainSpice);
                if (gameSettings.modelIndex > 0 && gameSettings.modelIndex < 9)
                    m_Agent.AddReward(m_CurrentDeltaWelfare * 0.1f); // v6.x delta-welfare
                else
                    m_Agent.AddReward(gameSettings.surviveReward); // v7.3 (0), v7.4 (9+)
                // Debug.Log($"Agent reward at step {m_Age}: {m_CurrentDeltaWelfare * 0.1f} with metabolism {m_SugarMetabolism}/{m_SpiceMetabolism}");
            }
        }

        public override void OnAgentDie(bool isStarvation)
        {
            isAlive = false;
            m_VisualizeComp.Visualize(0f);
            
            var reward = isStarvation ? gameSettings.deathPunishment : 0f;
            if (gameSettings.modelIndex > 0 && gameSettings.modelIndex < 9)
                reward += m_CurrentDeltaWelfare * 0.1f; // v6.x delta-welfare
            
            m_Agent.AddReward(reward);
            m_CurrentMrs = 0;
            m_Agent.enabled = false;
            Destroy(gameObject);
        }

        public override void ModifyModel(string behaviorName,Unity.InferenceEngine.ModelAsset model)
        {
            m_Agent.SetModel(behaviorName, model);
        }
    }
}