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
        
        private Agent m_Agent;
        private int m_Id;
        private int m_XCoor;
        private int m_YCoor;
        [SerializeField] private int m_RemainSugar;
        [SerializeField] private int m_RemainSpice;
        private AgentInfo currentCell;
        private IVisualizeComp m_VisualizeComp;
        private ITradeComp m_TradeComp;
        private float m_CurrentMrs;
        [SerializeField] private bool isAlive = true;

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
            m_RemainSugar = Random.Range(gameSettings.initiatedSugar, gameSettings.capacitySugar);
            m_RemainSpice = Random.Range(gameSettings.initiatedSpice, gameSettings.capacitySpice);
            isAlive = true;
            m_TradeComp.Init(this, gameSettings.metabolismSugar, gameSettings.metabolismSpice);

            Eat();
        }

        public void AskForActions()
        {
            if (!isAlive) return;
            
            if (Academy.Instance.IsCommunicatorOn) m_Agent?.RequestDecision();
            else StartCoroutine(WaitToAskForActions());
        }

        private IEnumerator  WaitToAskForActions()
        {
            // yield return new WaitUntil(() => actionStorage.GetValue() == 0); // run when press Space
            yield return new WaitForSeconds(tickInterval);
            m_Agent?.RequestDecision();
        }

        public void Move(int action)
        {
            currentCell = stateStorage.GetValue().GetAgent(m_XCoor,m_YCoor);
            currentCell.isOccupied = false;
            
            switch (action)
            {
                case 1: m_XCoor = Mathf.Max(0,m_XCoor-1); break;
                case 2: m_XCoor = Mathf.Min(stateStorage.GetValue().width - 1,m_XCoor+1); break;
                case 3: m_YCoor = Mathf.Max(0,m_YCoor-1); break;
                case 4: m_YCoor = Mathf.Min(stateStorage.GetValue().height - 1,m_YCoor+1); break;
            }
            transform.position = new Vector3(m_XCoor,0,m_YCoor);
            Eat();
            agentsDoneCount.SetValue(agentsDoneCount.GetValue() + 1);
        }

        public void Eat()
        {
            var state = stateStorage.GetValue();
            var sugar = state.GetSugar(m_XCoor, m_YCoor);
            var spice = state.GetSpice(m_XCoor, m_YCoor);
            ChangeSugar(sugar - gameSettings.metabolismSugar);
            ChangeSpice(spice - gameSettings.metabolismSpice);
            state.SetSugar(m_XCoor,m_YCoor, 0);
            state.SetSpice(m_XCoor,m_YCoor, 0);
            
            MayBeDie();
        }

        public void MayBeDie()
        {
            // currentCell = stateStorage.GetValue().GetAgent(m_XCoor,m_YCoor);
            if (m_RemainSugar <= 0 || m_RemainSpice <= 0)
            {
                isAlive = false;
                // currentCell.UpdateInfo(occupied:isAlive);
                m_VisualizeComp.Visualize(0f);
                m_Agent.AddReward(gameSettings.deathPunishment);
                // Debug.Log($"Agent reward after die: {m_Agent.GetCumulativeReward()} with remain sugar: {m_RemainSugar} and remain spice: {m_RemainSpice}");
                m_Agent.enabled = false;
            }
            else
            {
                // UpdateNewCell();
                m_CurrentMrs = m_TradeComp.CalculateMRS(m_RemainSugar, m_RemainSpice);
                // currentCell.UpdateInfo(m_RemainSugar,m_RemainSpice,currentMrs,isAlive);
                m_Agent.AddReward(gameSettings.surviveReward);
                // Debug.Log($"Agent reward at step: {m_Agent.GetCumulativeReward()}");
            }
        }

        public void UpdateState()
        {
            currentCell = stateStorage.GetValue().GetAgent(m_XCoor,m_YCoor);
            currentCell.UpdateInfo(m_RemainSugar,m_RemainSpice,m_CurrentMrs,isAlive);
        }

        // public int GetAction()
        // {
        //     return ActionStorage.GetValue();
        // }

        public void AgentReset()
        {
            m_Agent.enabled = true;
            // Debug.Log($"Agent reward at reset: {m_Agent.GetCumulativeReward()}");
            m_XCoor = Random.Range(0, stateStorage.GetValue().width);
            m_YCoor = Random.Range(0, stateStorage.GetValue().height);
            m_RemainSugar = gameSettings.initiatedSugar;
            m_RemainSpice = gameSettings.initiatedSpice;
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
            return gameSettings.visionRange;
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
            m_RemainSugar = sugarAmount > 0 ? Mathf.Min(m_RemainSugar + sugarAmount, gameSettings.capacitySugar) : 
                Mathf.Max(m_RemainSugar + sugarAmount, 0);
        }

        public void ChangeSpice(int spiceAmount)
        {
            m_RemainSpice = spiceAmount > 0 ? Mathf.Min(m_RemainSpice + spiceAmount, gameSettings.capacitySpice) : 
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

        public float ObserveSugarStave()
        {
            return m_RemainSugar * 1f / gameSettings.metabolismSugar;
        }

        public float ObserveSpiceStave()
        {
            return m_RemainSpice * 1f / gameSettings.metabolismSpice;
        }

        // private void UpdateNewCell()
        // {
        //     currentCell.remainSugar = m_RemainSugar;
        //     currentCell.remainSpice = m_RemainSpice;
        //     currentCell.isOccupied = true;
        // }
    }
}