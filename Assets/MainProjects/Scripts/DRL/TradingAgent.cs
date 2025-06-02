using System;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Serialization;
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
        
        private Agent m_Agent;
        private int m_Id;
        private int m_XCoor;
        private int m_YCoor;
        private int m_RemainSugar;
        private int m_RemainSpice;
        private AgentInfo currentCell;
        private IVisualizeComp m_VisualizeComp;
        private ITradeComp m_TradeComp;
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
        
        public void Init(int agentId, int x, int y)
        {
            m_Id = agentId;
            m_XCoor = x;
            m_YCoor = y;
            m_RemainSugar = gameSettings.initiatedSugar;
            m_RemainSpice = gameSettings.initiatedSpice;
            isAlive = true;
            m_TradeComp.Init(this, gameSettings.metabolismSugar, gameSettings.metabolismSpice);

            Eat();
            // agentsDoneCount.SetValue(agentsDoneCount.GetValue() + 1);
        }

        public void AskForActions()
        {
            if (!isAlive) return;
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
            currentCell = stateStorage.GetValue().GetAgent(m_XCoor,m_YCoor);
            if (m_RemainSugar <= 0 || m_RemainSpice <= 0)
            {
                isAlive = false;
                currentCell.isOccupied = false;
                m_VisualizeComp.Visualize(0f);
                m_Agent.AddReward(gameSettings.deathPunishment);
                // Debug.Log($"Agent reward after die: {m_Agent.GetCumulativeReward()}");
                m_Agent.enabled = false;
            }
            else
            {
                UpdateNewCell();
                m_Agent.AddReward(gameSettings.surviveReward);
                // Debug.Log($"Agent reward at step: {m_Agent.GetCumulativeReward()}");
            }
        }

        // public int GetAction()
        // {
        //     return ActionStorage.GetValue();
        // }

        public void Reset()
        {
            m_Agent.enabled = true;
            // Debug.Log($"Agent reward at reset: {m_Agent.GetCumulativeReward()}");
            m_XCoor = Random.Range(0, stateStorage.GetValue().width);
            m_YCoor = Random.Range(0, stateStorage.GetValue().height);
            m_RemainSugar = gameSettings.initiatedSugar;
            m_RemainSpice = gameSettings.initiatedSpice;
            isAlive = true;
            m_VisualizeComp.Visualize(1f);
            Eat();
        }

        public (int, int) GetPosition()
        {
            return (m_XCoor, m_YCoor);
        }

        public int GetAgentID()
        {
            return m_Id;
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

        private void UpdateNewCell()
        {
            currentCell.remainSugar = m_RemainSugar;
            currentCell.remainSpice = m_RemainSpice;
            currentCell.isOccupied = true;
        }
    }
}