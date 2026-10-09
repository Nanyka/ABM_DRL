using System;
using System.Collections;
using System.Linq;
using TMPro;
using Unity.MLAgents;

using UnityEngine;
using Random = UnityEngine.Random;

namespace Sugarscape
{
    [RequireComponent(typeof(TradeComp))]
    public class TradingAgent : MonoBehaviour, IAgentController
    {
        // [SerializeField] private IntStorage ActionStorage;
        [SerializeField] private StateStorage stateStorage;
        [SerializeField] protected GameSettings gameSettings;
        [SerializeField] private IntStorage agentsDoneCount;
        [SerializeField] private IntStorage actionStorage;
        [SerializeField] private TextMeshPro idText;
        [SerializeField] private float tickInterval;
        [SerializeField] private bool isRandomState;
        [SerializeField] private bool isWelfareReward;
        
        protected Agent m_Agent;
        private int m_Id;
        private int m_XCoor;
        private int m_YCoor;
        private int m_Vision;
        [SerializeField] protected int m_RemainSugar;
        [SerializeField] protected int m_RemainSpice;
        [SerializeField] private int m_SugarMetabolism;
        [SerializeField] private int m_SpiceMetabolism;
        [SerializeField] private int m_SugarStorage;
        [SerializeField] private int m_SpiceStorage;
        [SerializeField] protected int m_Age;
        private bool m_IsMale;
        private AgentInfo currentCell;
        protected IVisualizeComp m_VisualizeComp;
        protected ITradeComp m_TradeComp;
        protected float m_CurrentMrs;
        private float m_CurrentWelfare;
        protected float m_CurrentDeltaWelfare;
        private float m_MinAge;
        protected bool isAlive = true;

        private void Awake()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            if (!CommunicatorFactory.CommunicatorRegistered)
                CommunicatorFactory.Register<ICommunicator>(RpcCommunicator.Create);
#endif
            m_Agent = GetComponent<Agent>();
            m_VisualizeComp = GetComponentInChildren<IVisualizeComp>();
            m_TradeComp = GetComponent<ITradeComp>();
        }

        public void Init(int agentId, int x, int y, bool isShowId = false)
        {
            m_Id = agentId;
            if (isShowId) idText.text = agentId.ToString();
            else idText.gameObject.SetActive(false);
            
            m_XCoor = x;
            m_YCoor = y;
            m_Age = 0;
            m_IsMale = Random.value > 0.5f;
            isRandomState = gameSettings.randomState;
            
            m_Vision = isRandomState?Random.Range(1, gameSettings.visionRange):gameSettings.visionRange;
            m_SugarMetabolism = isRandomState?Random.Range(1, gameSettings.metabolismSugar + 1):gameSettings.metabolismSugar;
            m_SpiceMetabolism = isRandomState?Random.Range(1, gameSettings.metabolismSpice + 1):gameSettings.metabolismSpice;
            m_SugarStorage = gameSettings.capacitySugar;
            m_SpiceStorage = gameSettings.capacitySpice;
            m_RemainSugar = isRandomState?Random.Range(m_SugarMetabolism * 2, gameSettings.initiatedSugar):gameSettings.initiatedSugar;
            m_RemainSpice = isRandomState?Random.Range(m_SpiceMetabolism * 2, gameSettings.initiatedSpice):gameSettings.initiatedSpice;
            ApplyComplementaryEndowment();
            ApplyComplementaryNeeds();
            isAlive = true;
            m_TradeComp.Init(this, m_SugarMetabolism, m_SpiceMetabolism);
            m_CurrentWelfare = m_TradeComp.CalculateWelfare(m_RemainSugar,m_RemainSpice);
            m_MinAge = (m_RemainSugar*1f / m_SugarMetabolism) + (m_RemainSpice*1f / m_SpiceMetabolism);

            Eat();
        }

        // Gains from trade by construction: half the agents start sugar-rich and spice-poor, the other half the reverse
        private void ApplyComplementaryEndowment()
        {
            if (gameSettings.poorEndowment <= 0) return;

            if (Random.value > 0.5f)
            {
                m_RemainSugar = gameSettings.initiatedSugar;
                m_RemainSpice = gameSettings.poorEndowment;
            }
            else
            {
                m_RemainSugar = gameSettings.poorEndowment;
                m_RemainSpice = gameSettings.initiatedSpice;
            }
        }

        // Opposite needs by construction: half the agents burn sugar fast and spice slowly, the other half the reverse,
        // so each type keeps running short of one good and must keep finding the other type
        private void ApplyComplementaryNeeds()
        {
            if (gameSettings.complementaryMetabolism)
            {
                int high = Mathf.Max(gameSettings.metabolismSugar, gameSettings.metabolismSpice);
                int low = Mathf.Min(gameSettings.metabolismSugar, gameSettings.metabolismSpice);
                bool sugarHeavy = Random.value > 0.5f;
                m_SugarMetabolism = sugarHeavy ? high : low;
                m_SpiceMetabolism = sugarHeavy ? low : high;
            }

            if (gameSettings.endowmentDays > 0)
            {
                m_RemainSugar = gameSettings.endowmentDays * m_SugarMetabolism;
                m_RemainSpice = gameSettings.endowmentDays * m_SpiceMetabolism;
            }
        }

        private static readonly (int dx, int dy, int action)[] s_Arms =
        {
            (-1, 0, 1), // left
            (1, 0, 2), // right
            (0, -1, 3), // down
            (0, 1, 4) // up
        };
        private const float k_PartnerMrsGap = 1f; // |ln(MRS_other / MRS_own)| above which a visible agent counts as a complementary trader

        // Scripted rules behind the two optional actions. Forage: step toward the best visible harvest.
        // Seek: step toward the nearest visible trader whose MRS differs clearly from this agent's; with no such
        // trader in view (or no MRS information) it forages, so it is never worse than the forage action
        private int ScriptedAction(bool seekPartner)
        {
            var state = stateStorage.GetValue();
            bool seesMrs = seekPartner && !gameSettings.resourceOnlyObs && !gameSettings.disableNeighborMrs && m_CurrentMrs > 0f;
            int partnerAction = 0;
            int partnerDistance = int.MaxValue;

            float current = m_TradeComp.CalculateWelfare(m_RemainSugar, m_RemainSpice);
            float bestGain = PredictWelfare(state.GetSugar(m_XCoor, m_YCoor), state.GetSpice(m_XCoor, m_YCoor), 1) - current;
            int forageAction = 0;

            foreach (var arm in s_Arms)
            {
                int sugar = 0;
                int spice = 0;
                for (int step = 1; step <= m_Vision; step++)
                {
                    int x = m_XCoor + arm.dx * step;
                    int y = m_YCoor + arm.dy * step;
                    if (x < 0 || x >= state.width || y < 0 || y >= state.height) break;

                    sugar += state.GetSugar(x, y);
                    spice += state.GetSpice(x, y);
                    float gain = PredictWelfare(sugar, spice, step) - current;
                    if (gain > bestGain)
                    {
                        bestGain = gain;
                        forageAction = arm.action;
                    }

                    // an adjacent trader is already within Rule-T range, so only farther ones are worth walking to
                    if (!seesMrs || step == 1 || step >= partnerDistance) continue;
                    var others = state.GetAgents(x, y);
                    if (others != null && others.Any(a => a.isOccupied && a.agentId != m_Id && a.currentMrs > 0f &&
                                                          Mathf.Abs(Mathf.Log(a.currentMrs / m_CurrentMrs)) > k_PartnerMrsGap))
                    {
                        partnerDistance = step;
                        partnerAction = arm.action;
                    }
                }
            }

            bool foundPartner = partnerDistance != int.MaxValue;
            if (seekPartner && Academy.Instance.IsCommunicatorOn)
                Academy.Instance.StatsRecorder.Add("Action/SeekFoundPartner", foundPartner ? 1f : 0f);
            return foundPartner ? partnerAction : forageAction;
        }

        public void AskForActions()
        {
            if (!isAlive) return;
            
            // m_TradeComp.BeforeNewStep();
            if (Academy.Instance.IsCommunicatorOn) m_Agent?.RequestDecision();
            else StartCoroutine(WaitToAskForActions());
        }

        private IEnumerator  WaitToAskForActions()
        {
            // yield return new WaitUntil(() => actionStorage.GetValue() == 0); 
            yield return new WaitForSeconds(tickInterval);
          
            m_Agent?.RequestDecision();
        }

        public void Move(int action)
        {
            // currentCell = stateStorage.GetValue().GetAgent(m_XCoor,m_YCoor);
            // currentCell.isOccupied = false;

            if (SeekAction.Enabled)
            {
                if (Academy.Instance.IsCommunicatorOn)
                {
                    Academy.Instance.StatsRecorder.Add("Action/Forage", action == SeekAction.ForageIndex ? 1f : 0f);
                    Academy.Instance.StatsRecorder.Add("Action/Seek", action == SeekAction.SeekIndex ? 1f : 0f);
                }
                if (action == SeekAction.ForageIndex) action = ScriptedAction(false);
                else if (action == SeekAction.SeekIndex) action = ScriptedAction(true);
            }

            switch (action)
            {
                case 1: m_XCoor = Mathf.Max(0,m_XCoor-1); break;
                case 2: m_XCoor = Mathf.Min(stateStorage.GetValue().width - 1,m_XCoor+1); break;
                case 3: m_YCoor = Mathf.Max(0,m_YCoor-1); break;
                case 4: m_YCoor = Mathf.Min(stateStorage.GetValue().height - 1,m_YCoor+1); break;
            }
            transform.position = new Vector3(m_XCoor,0,m_YCoor);
            Eat();
            m_Age++;
            agentsDoneCount.SetValue(agentsDoneCount.GetValue() + 1);
        }

        public void Eat()
        {
            var state = stateStorage.GetValue();
            var sugar = state.GetSugar(m_XCoor, m_YCoor);
            var spice = state.GetSpice(m_XCoor, m_YCoor);
            ChangeSugar(sugar - m_SugarMetabolism);
            ChangeSpice(spice - m_SpiceMetabolism);
            state.SetSugar(m_XCoor,m_YCoor, 0);
            state.SetSpice(m_XCoor,m_YCoor, 0);
            
            MayBeDie();
        }

        public virtual void MayBeDie()
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
                // Inference-time default; the training reward is selected in AgentForTrainOnly
                m_Agent.AddReward(gameSettings.surviveReward);

                // Debug.Log($"Agent reward at step {m_Age}: {m_CurrentDeltaWelfare * 0.1f} with metabolism {m_SugarMetabolism}/{m_SpiceMetabolism}");
            }
        }

        public virtual void OnAgentDie(bool isStarvation)
        {
            isAlive = false;
            m_VisualizeComp.Visualize(0f);
            
            var reward = isStarvation ? gameSettings.deathPunishment : 0f;
            m_Agent.AddReward(reward);
            
            // if (isWelfareReward) m_Agent.AddReward(m_CurrentDeltaWelfare * 0.1f + reward); // adjustment factor is 0.1, for v6.1 & v6.2
            // else m_Agent.AddReward(reward);
            // Debug.Log($"Agent {m_Id} reward after die: {m_Agent.GetCumulativeReward()}");
            m_CurrentMrs = 0;
            m_Agent.enabled = false;
            Destroy(gameObject);
        }

        public virtual void ModifyModel(string behaviorName, Unity.InferenceEngine.ModelAsset model) { }

        public void UpdateState()
        {
            if (gameSettings.obsVersion >= 1 && isAlive && m_RemainSugar > 0)
                m_CurrentMrs = m_TradeComp.CalculateMRS(m_RemainSugar, m_RemainSpice); // holdings may have changed through trade
            var agentInfo = new AgentInfo(m_Id,m_RemainSugar,m_RemainSpice,m_CurrentMrs,isAlive);
            stateStorage.GetValue().SetByLayer(2,m_XCoor,m_YCoor,agentInfo);
        }

        // public int GetAction()
        // {
        //     return ActionStorage.GetValue();
        // }

        public void AgentReset()
        {
            m_Agent.enabled = false; // need to reset reward
            m_Agent.enabled = true;
            // Debug.Log($"Agent {m_Id} reward is {m_Agent.GetCumulativeReward()} after reset");

            // Debug.Log($"Agent reward at reset: {m_Agent.GetCumulativeReward()}");
            m_XCoor = Random.Range(0, stateStorage.GetValue().width);
            m_YCoor = Random.Range(0, stateStorage.GetValue().height);
            m_RemainSugar = isRandomState?Random.Range(m_SugarMetabolism, gameSettings.initiatedSugar):gameSettings.initiatedSugar;
            m_RemainSpice = isRandomState?Random.Range(m_SpiceMetabolism, gameSettings.initiatedSpice):gameSettings.initiatedSpice;
            ApplyComplementaryEndowment();
            m_Age = 0;
            m_IsMale = Random.value > 0.5f;
            isAlive = true;
            m_VisualizeComp.Visualize(1f);
            m_TradeComp.Reset();
            transform.position = new Vector3(m_XCoor,0,m_YCoor);
            Eat();
            agentsDoneCount.SetValue(agentsDoneCount.GetValue() + 1);
            // Debug.Log($"Reset agent {agentsDoneCount.GetValue()}");
        }

        public (int, int) GetPosition()
        {
            return (m_XCoor, m_YCoor);
        }

        public int GetAgentID()
        {
            return m_Id;
        }

        public int GetVision()
        {
            return m_Vision;
        }
        
        public float PredictWelfare(int addedSugar, int addedSpice, int steps)
        {
            int sugarAfter = Mathf.Clamp(m_RemainSugar + addedSugar - m_SugarMetabolism*steps, 0,
                m_SugarStorage);
            int spiceAfter = Mathf.Clamp(m_RemainSpice + addedSpice - m_SpiceMetabolism*steps, 0,
                m_SpiceStorage);

            return m_TradeComp.CalculateWelfare(sugarAfter, spiceAfter);
        }

        public bool IsPerfectInfo()
        {
            return gameSettings.isPerfectInfo;
        }

        public bool DisableNeighborMrs()
        {
            return gameSettings.disableNeighborMrs;
        }

        public bool ResourceOnlyObs()
        {
            return gameSettings.resourceOnlyObs;
        }

        public int ObsVersion()
        {
            return gameSettings.obsVersion;
        }

        public bool IsAlive()
        {
            return isAlive;
        }

        public void ChangeSugar(int sugarAmount)
        {
            m_RemainSugar = sugarAmount > 0 ? Mathf.Min(m_RemainSugar + sugarAmount, m_SugarStorage) : 
                Mathf.Max(m_RemainSugar + sugarAmount, 0);
        }

        public void ChangeSpice(int spiceAmount)
        {
            m_RemainSpice = spiceAmount > 0 ? Mathf.Min(m_RemainSpice + spiceAmount, m_SpiceStorage) : 
                Mathf.Max(m_RemainSpice + spiceAmount, 0);
        }

        public ITradeComp GetTradeComp()
        {
            return m_TradeComp;
        }

        public int RemainSugar()
        {
            return m_RemainSugar;
        }

        public int RemainSpice()
        {
            return m_RemainSpice;
        }

        public float CurrentMrs()
        {
            return m_CurrentMrs;
        }

        public float ObserveSugarStarve()
        {
            return m_RemainSugar * 1f / m_SugarMetabolism;
        }

        public float ObserveSpiceStarve()
        {
            return m_RemainSpice * 1f / m_SpiceMetabolism;
        }

        public int SugarStorage()
        {
            return m_SugarStorage;
        }

        public int SpiceStorage()
        {
            return m_SpiceStorage;
        }

        public int SugarMetabolism()
        {
            return m_SugarMetabolism;
        }

        public int SpiceMetabolism()
        {
            return m_SpiceMetabolism;
        }

        public int UsingModel()
        {
            return gameSettings.modelIndex;
        }

        public float SugarSpiceDistance()
        {
            return Mathf.Abs((m_RemainSpice - m_RemainSugar) *1f / (m_RemainSpice + m_RemainSugar));
        }

        public bool IsHardCodeAgent()
        {
            return false;
        }

        public GameObject GetGameObject()
        {
            return gameObject;
        }
        
        public int GetAge()
        {
            return m_Age;
        }

        public bool GetSex()
        {
            return m_IsMale;
        }

        protected void RecordDeltaWelfare()
        {
            var updatedWelfare = m_TradeComp.CalculateWelfare(m_RemainSugar,m_RemainSpice);
            m_CurrentDeltaWelfare = updatedWelfare - m_CurrentWelfare;
            m_CurrentWelfare = updatedWelfare;
        }
    }
}