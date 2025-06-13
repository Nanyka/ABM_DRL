using System.Collections;
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
        // [SerializeField] private TextMeshPro idText;
        
        // private Agent m_Agent;
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
            m_VisualizeComp = GetComponentInChildren<IVisualizeComp>();
            m_TradeComp = GetComponent<ITradeComp>();
        }
        
        public void Init(int agentId, int x, int y)
        {
            m_Id = agentId;
            // idText.text = agentId.ToString();
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
            
            else StartCoroutine(WaitToAskForActions());
        }

        private IEnumerator  WaitToAskForActions()
        {
            yield return new WaitUntil(() => actionStorage.GetValue() == 0);
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

            foreach (var dir in directions)
            {
                for (int step = 1; step <= gameSettings.visionRange; step++)
                {
                    int nx = m_XCoor + dir.dx * step;
                    int ny = m_YCoor + dir.dy * step;

                    if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                        break;

                    var agentInfo = state.GetAgent(nx, ny);
                    if (agentInfo != null && agentInfo.isOccupied)
                        break;

                    int sugar = state.GetSugar(nx, ny);
                    int spice = state.GetSpice(nx, ny);
                    int sugarAfter = Mathf.Clamp(m_RemainSugar + sugar - gameSettings.metabolismSugar, 0, gameSettings.capacitySugar);
                    int spiceAfter = Mathf.Clamp(m_RemainSpice + spice - gameSettings.metabolismSpice, 0, gameSettings.capacitySpice);

                    float welfare = m_TradeComp.CalculateWelfare(sugarAfter, spiceAfter);
                    if (welfare > bestWelfare)
                    {
                        bestWelfare = welfare;
                        bestAction = dir.action;
                    }
                }
            }

            return bestAction;
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
            if (m_RemainSugar <= 0 || m_RemainSpice <= 0)
            {
                isAlive = false;
                m_VisualizeComp.Visualize(0f);
            }
            else
            {
                m_CurrentMrs = m_TradeComp.CalculateMRS(m_RemainSugar, m_RemainSpice);
            }
        }

        public void UpdateState()
        {
            currentCell = stateStorage.GetValue().GetAgent(m_XCoor,m_YCoor);
            currentCell.UpdateInfo(m_RemainSugar,m_RemainSpice,m_CurrentMrs,isAlive);
        }

        public void AgentReset()
        {
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
    }
}