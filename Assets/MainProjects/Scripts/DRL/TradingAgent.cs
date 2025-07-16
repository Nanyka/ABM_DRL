using System;
using System.Collections;
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
        [SerializeField] private GameSettings gameSettings;
        [SerializeField] private IntStorage agentsDoneCount;
        [SerializeField] private IntStorage actionStorage;
        [SerializeField] private TextMeshPro idText;
        [SerializeField] private float tickInterval;
        [SerializeField] private bool isRandomState;
        [SerializeField] private bool isWelfareReward;
        
        private Agent m_Agent;
        private int m_Id;
        private int m_XCoor;
        private int m_YCoor;
        private int m_Vision;
        [SerializeField] private int m_RemainSugar;
        [SerializeField] private int m_RemainSpice;
        [SerializeField] private int m_SugarMetabolism;
        [SerializeField] private int m_SpiceMetabolism;
        [SerializeField] private int m_SugarStorage;
        [SerializeField] private int m_SpiceStorage;
        [SerializeField] private int m_Age;
        private bool m_IsMale;
        private AgentInfo currentCell;
        private IVisualizeComp m_VisualizeComp;
        private ITradeComp m_TradeComp;
        private float m_CurrentMrs;
        private float m_CurrentWelfare;
        private float m_CurrentDeltaWelfare;
        private float m_MinAge;
        private bool isAlive = true;

        private void Awake()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            if (!CommunicatorFactory.CommunicatorRegistered)
                CommunicatorFactory.Register<ICommunicator>(RpcCommunicator.Create);
#endif
            // m_SensorComp = GetComponent<SugarscrapeSensorComp>();
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
            // m_SugarStorage = isRandomState?Random.Range(m_SugarMetabolism * 5, gameSettings.capacitySugar): gameSettings.capacitySugar;
            // m_SpiceStorage = isRandomState?Random.Range(m_SpiceMetabolism * 5, gameSettings.capacitySpice): gameSettings.capacitySpice;
            m_RemainSugar = isRandomState?Random.Range(m_SugarMetabolism * 2, gameSettings.initiatedSugar):gameSettings.initiatedSugar;
            m_RemainSpice = isRandomState?Random.Range(m_SpiceMetabolism * 2, gameSettings.initiatedSpice):gameSettings.initiatedSpice;
            isAlive = true;
            m_TradeComp.Init(this, m_SugarMetabolism, m_SpiceMetabolism);
            m_CurrentWelfare = m_TradeComp.CalculateWelfare(m_RemainSugar,m_RemainSpice);
            m_MinAge = (m_RemainSugar*1f / m_SugarMetabolism) + (m_RemainSpice*1f / m_SpiceMetabolism);

            Eat();
        }

        public void AskForActions()
        {
            if (!isAlive) return;
            
            m_TradeComp.BeforeNewStep();
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

        public void MayBeDie()
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
                // m_Agent.AddReward(gameSettings.surviveReward); // for v6.4
                m_Agent.AddReward(m_CurrentDeltaWelfare * 0.1f); // for v6.3

                // Debug.Log($"Agent reward at step {m_Age}: {m_CurrentDeltaWelfare * 0.1f} with metabolism {m_SugarMetabolism}/{m_SpiceMetabolism}");
            }
        }

        public void OnAgentDie(bool isStarvation)
        {
            isAlive = false;
            m_VisualizeComp.Visualize(0f);
            
            var reward = isStarvation ? gameSettings.deathPunishment : 0f; // from v6.3 and below
            // var ageFactor = (m_Age - m_MinAge)/m_MinAge; // for v6.4&5
            // reward *= ageFactor <= 0 ? -ageFactor : 0; // punish if agent can't live longer than minAge, for v6.5
            reward += m_CurrentDeltaWelfare * 0.1f; // for v6.6 & v6.7
            m_Agent.AddReward(reward);
            
            // if (isWelfareReward) m_Agent.AddReward(m_CurrentDeltaWelfare * 0.1f + reward); // adjustment factor is 0.1, for v6.1 & v6.2
            // else m_Agent.AddReward(reward);
            // Debug.Log($"Agent {m_Id} reward after die: {m_Agent.GetCumulativeReward()}");
            m_CurrentMrs = 0;
            m_Agent.enabled = false;
            Destroy(gameObject);
        }

        public void UpdateState()
        {
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

        private void RecordDeltaWelfare()
        {
            var updatedWelfare = m_TradeComp.CalculateWelfare(m_RemainSugar,m_RemainSpice);
            m_CurrentDeltaWelfare = updatedWelfare - m_CurrentWelfare;
            m_CurrentWelfare = updatedWelfare;
        }
    }
}