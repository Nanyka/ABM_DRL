using System;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Serialization;

namespace Sugarscape
{
    public class TradingAgent : MonoBehaviour, IAgentController
    {
        [SerializeField] private VoidChannel OnSpacePressed;        
        [SerializeField] private IntStorage ActionStorage;
        [SerializeField] private StateStorage stateStorage;
        [SerializeField] private GameSettings gameSettings;
        
        private SugarscrapeSensorComp m_SensorComp;
        private Agent m_Agent;
        private int m_Id;
        private int m_XCoor;
        private int m_YCoor;
        private int m_RemainSugar;
        private int m_RemainSpice;
        [SerializeField] private AgentInfo currentCell;
        private bool isAlive = true;

        private void Awake()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            if (!CommunicatorFactory.CommunicatorRegistered)
                CommunicatorFactory.Register<ICommunicator>(RpcCommunicator.Create);
#endif
            m_SensorComp = GetComponent<SugarscrapeSensorComp>();
            m_Agent = GetComponent<Agent>();
        }

        private void OnEnable()
        {
            OnSpacePressed.AddListener(AskForActions);
        }

        private void OnDisable()
        {
            OnSpacePressed.RemoveListener(AskForActions);
        }
        
        public void Init(int agentId, int x, int y)
        {
            m_Id = agentId;
            m_XCoor = x;
            m_YCoor = y;
            m_RemainSugar = gameSettings.metabolismSugar;
            m_RemainSpice = gameSettings.metabolismSpice;
            isAlive = true;
            
            Eat();
        }

        private void AskForActions()
        {
            if (!isAlive)
            {
                Debug.Log($"Agent {m_Id} is death");
                return;
            }
            
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
        }

        public void Eat()
        {
            var state = stateStorage.GetValue();
            var sugar = state.GetSugar(m_XCoor, m_YCoor);
            var spice = state.GetSpice(m_XCoor, m_YCoor);
            m_RemainSugar = Mathf.Min(m_RemainSugar + sugar, gameSettings.capacitySugar);
            m_RemainSpice = Mathf.Min(m_RemainSpice + spice , gameSettings.capacitySpice);
            m_RemainSugar = Mathf.Max(m_RemainSugar - gameSettings.metabolismSugar, 0);
            m_RemainSpice = Mathf.Max(m_RemainSpice - gameSettings.metabolismSpice, 0);
            state.SetSugar(m_XCoor,m_YCoor, 0);
            state.SetSpice(m_XCoor,m_YCoor, 0);
            
            UpdateNewCell();
            // Debug.Log($"Id: {m_Id}, {currentCell}");
        }

        public void MayBeDie()
        {
            if (currentCell.remainSugar <= 0 || currentCell.remainSpice <= 0)
            {
                isAlive = false;
                currentCell.isOccupied = false;
            }
        }

        public int GetAction()
        {
            return ActionStorage.GetValue();
        }
        
        private void UpdateNewCell()
        {
            currentCell = stateStorage.GetValue().GetAgent(m_XCoor,m_YCoor);
            currentCell.remainSugar = m_RemainSugar;
            currentCell.remainSpice = m_RemainSpice;
            currentCell.isOccupied = true;
        }
    }
}