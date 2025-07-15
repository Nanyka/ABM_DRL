using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.MLAgents;
using UnityEngine;

namespace Sugarscape
{
    [RequireComponent(typeof(TradeComp))]
    public class OriginalAgent : MonoBehaviour, IAgentController
    {
        // [SerializeField] private IntStorage ActionStorage;
        [SerializeField] private StateStorage stateStorage;
        [SerializeField] private GameSettings gameSettings;
        [SerializeField] private IntStorage agentsDoneCount;
        [SerializeField] private IntStorage actionStorage;
        [SerializeField] private TextMeshPro idText;
        [SerializeField] private float tickInterval;
        [SerializeField] private bool isRandomState;
        
        // private Agent m_Agent;
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
        [SerializeField] private bool m_IsMale;
        private AgentInfo currentCell;
        private IVisualizeComp m_VisualizeComp;
        private ITradeComp m_TradeComp;
        private float m_CurrentMrs;
        [SerializeField] private bool isAlive = true;

        private void Awake()
        {
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
            m_TradeComp.Init(this, m_SugarMetabolism,m_SpiceMetabolism);

            Eat();
        }

        public void AskForActions()
        {
            if (!isAlive) return;
            
            m_TradeComp.BeforeNewStep();
            StartCoroutine(WaitToAskForActions());
        }

        private IEnumerator  WaitToAskForActions()
        {
            // yield return new WaitUntil(() => actionStorage.GetValue() == 0); // run when press Space
            yield return new WaitForSeconds(tickInterval);
            var action = DecideAction();
            Move(action);
        }
        
        private int DecideAction()
        {
            var state = stateStorage.GetValue();
            int width = state.width;
            int height = state.height;

            float bestWelfare = m_TradeComp.CalculateWelfare(m_RemainSugar, m_RemainSpice);
            int bestAction = 0;

            var directions = new (int dx, int dy, int action)[]
            {
                (-1, 0, 1), // left
                (1, 0, 2),  // right
                (0, -1, 3), // down
                (0, 1, 4)   // up
            };

            var visited = new bool[width, height];
            var queue = new Queue<(int x, int y, List<int> path, int dist)>();
            queue.Enqueue((m_XCoor, m_YCoor, new List<int>(), 0));
            visited[m_XCoor, m_YCoor] = true;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                foreach (var dir in directions)
                {
                    int nx = current.x + dir.dx;
                    int ny = current.y + dir.dy;
                    int ndist = current.dist + 1;

                    if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                        continue;
                    if (visited[nx, ny] || ndist > m_Vision)
                        continue;
                    
                    visited[nx, ny] = true;
                    var newPath = new List<int>(current.path) { dir.action };

                    int sugar = state.GetSugar(nx, ny);
                    int spice = state.GetSpice(nx, ny);
                    int sugarAfter = Mathf.Clamp(m_RemainSugar + sugar - m_SugarMetabolism, 0, m_SugarStorage);
                    int spiceAfter = Mathf.Clamp(m_RemainSpice + spice -m_SpiceMetabolism, 0, m_SpiceStorage);

                    float welfare = m_TradeComp.CalculateWelfare(sugarAfter, spiceAfter);
                    if (welfare > bestWelfare)
                    {
                        bestWelfare = welfare;
                        bestAction = newPath[0];
                    }
                    queue.Enqueue((nx, ny, newPath, ndist));
                }
            }

            return bestAction;
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
            if (m_RemainSugar <= 0 || m_RemainSpice <= 0 || m_Age >= gameSettings.maxFertilityAge)
                OnAgentDie(true);
            else
                m_CurrentMrs = m_TradeComp.CalculateMRS(m_RemainSugar, m_RemainSpice);
        }
        
        public void OnAgentDie(bool isStarvation)
        {
            isAlive = false;
            m_VisualizeComp.Visualize(0f);
            Destroy(gameObject);
        }

        public void UpdateState()
        {
            var agentInfo = new AgentInfo(m_Id,m_RemainSugar,m_RemainSpice,m_CurrentMrs,isAlive);
            stateStorage.GetValue().SetByLayer(2,m_XCoor,m_YCoor,agentInfo);
            
            // currentCell = stateStorage.GetValue().GetAgents(m_XCoor,m_YCoor);
            // currentCell.UpdateInfo(m_RemainSugar,m_RemainSpice,m_CurrentMrs,isAlive);
        }

        public void AgentReset()
        {
            m_XCoor = Random.Range(0, stateStorage.GetValue().width);
            m_YCoor = Random.Range(0, stateStorage.GetValue().height);
            m_RemainSugar = isRandomState?Random.Range(gameSettings.initiatedSugar, m_SugarStorage):gameSettings.initiatedSugar;
            m_RemainSpice = isRandomState?Random.Range(gameSettings.initiatedSpice, m_SpiceStorage):gameSettings.initiatedSpice;
            m_Age = 0;
            m_IsMale = Random.value > 0.5f;
            isAlive = true;
            m_VisualizeComp.Visualize(1f);
            m_TradeComp.Reset();
            transform.position = new Vector3(m_XCoor,0,m_YCoor);
            Eat();
            agentsDoneCount.SetValue(agentsDoneCount.GetValue() + 1);
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
            return Mathf.Abs((m_RemainSpice - m_RemainSugar) * 1f / (m_RemainSpice + m_RemainSugar));
        }

        public bool IsHardCodeAgent()
        {
            return true;
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
    }
}