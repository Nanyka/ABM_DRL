using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Sugarscape
{
    public class AgentsManager : MonoBehaviour
    {
        [SerializeField] private VoidChannel OnInitiateAgents;
        [SerializeField] private VoidChannel OnSetup;
        [SerializeField] private VoidChannel OnReset;
        [SerializeField] private VoidChannel OnTick;
        [SerializeField] private VoidChannel OnEndStep;
        
        [SerializeField] private StateStorage stateStorage;
        [SerializeField] private GameSettings gameSettings;
        [SerializeField] private IntStorage agentsDoneCount;
        [SerializeField] private IntStorage aliveAgentsCount;
        [SerializeField] private GameObject agentPrefab;
        [SerializeField] private bool isShowId;
        
        private List<IAgentController> agents = new();
        private int remainAgentsAmount;

        private void OnEnable()
        {
            OnInitiateAgents.AddListener(SpawnAgents);
            OnReset.AddListener(ResetAgents);
            OnTick.AddListener(AskAgentsActions);
        }

        private void OnDisable()
        {
            OnInitiateAgents.RemoveListener(SpawnAgents);
            OnReset.RemoveListener(ResetAgents);
            OnTick.RemoveListener(AskAgentsActions);
        }

        private void SpawnAgents()
        {
            int agentIndex = 0;
            for (int i = 0; i < gameSettings.numberOfAgents; i++)
            {
                var xRandom = Random.Range(0,stateStorage.GetValue().width);
                var yRandom = Random.Range(0,stateStorage.GetValue().height);
                var agent = Instantiate(agentPrefab, new Vector3(xRandom, 0, yRandom), 
                    Quaternion.identity, transform);
                agent.name = $"Agent_{agentIndex}";
                
                if (agent.TryGetComponent(out IAgentController aiAgent))
                {
                    aiAgent.Init(agentIndex, xRandom, yRandom, isShowId);
                    agents.Add(aiAgent);
                }
                // Debug.Log($"Spawned agent {agentIndex} at ({xRandom},{yRandom})");
                agentIndex++;
            }

            OnSetup.ExecuteChannel();
        }

        private void ResetAgents()
        {
            agentsDoneCount.SetValue(0);
            remainAgentsAmount = agents.Count(agent => agent.IsAlive() == false);
            foreach (var agent in agents) agent.AgentReset();
            StartCoroutine(WaitForReset());
        }

        private IEnumerator WaitForReset()
        {
            yield return new WaitUntil(() => agentsDoneCount.GetValue() >= remainAgentsAmount);
            foreach (var agent in agents) agent.UpdateState();
            OnEndStep.ExecuteChannel();
        }

        private void AskAgentsActions()
        {
            agentsDoneCount.SetValue(0);
            remainAgentsAmount = agents.Count(agent => agent.IsAlive());
            foreach (var agent in agents) agent.AskForActions();
            StartCoroutine(WaitForAgents());
        }

        private IEnumerator WaitForAgents()
        {
            yield return new WaitUntil(() => agentsDoneCount.GetValue() >= remainAgentsAmount);
            
            foreach (var agent in agents) agent.UpdateState();
            
            var collisions = FindOverlappingAgents(agents);

            foreach (var group in collisions)
            {
                var pos = group[0].GetPosition();
                // Debug.Log($"Found {group.Count} agents at ({pos.Item1},{pos.Item2}):");
                foreach (var agent in group)
                {
                    // var others = group
                    //     .Where(other => other.GetAgentID() != agent.GetAgentID()).ToList();
                    // agent.GetTradeComp().Trade(others[Random.Range(0, others.Count)],true);
                    
                    // 1) Compute the selected agent's MRS
                    float mrsSelected = agent.GetTradeComp().CalculateMRS(agent.RemainSugar(), agent.RemainSpice());

                    IAgentController farthestAgent = null;
                    float maxDiff = float.MinValue;

                    // 2) Loop through all agents (skip the selected one)
                    foreach (var other in group)
                    {
                        if (agent.GetAgentID() == other.GetAgentID())
                            continue;

                        // 3) Compute this agent's MRS
                        float mrsOther = other.GetTradeComp().CalculateMRS(other.RemainSugar(),other.RemainSpice());

                        // 4) Compute absolute difference
                        float diff = Mathf.Abs(mrsOther - mrsSelected);

                        // 5) Track the maximum difference
                        if (diff > maxDiff)
                        {
                            maxDiff = diff;
                            farthestAgent = other;
                        }
                    }
                    agent.GetTradeComp().Trade(farthestAgent);
                    
                    // Debug.Log($"  • Agent ID {agent.GetAgentID()}"); // or any identifying property
                }
            }
            
            aliveAgentsCount.SetValue(agents.Count(agent => agent.IsAlive()));
            
            OnEndStep.ExecuteChannel();
        }

        private List<List<IAgentController>> FindOverlappingAgents(List<IAgentController> agents)
        {
            // var overlappingGroups = agents
            //     .Where(agent => agent.IsAlive())
            //     .GroupBy(a => {
            //         var (x,y) = a.GetPosition();
            //         return (x, y);
            //     })
            //     .Where(g => g.Count() > 1)
            //     .Select(g => g.ToList())
            //     .ToList();
            
            var aliveAgents = agents.Where(a => a.IsAlive()).ToList();
            var overlappingGroups = new List<List<IAgentController>>();
            int tradeRange = gameSettings.tradeRange;

            foreach (var agent in aliveAgents)
            {
                var pos = agent.GetPosition();
                var group = new List<IAgentController>();

                foreach (var other in aliveAgents)
                {
                    if (CalculateChebyshevDistance(pos, other.GetPosition()) <= tradeRange)
                    {
                        group.Add(other);
                    }
                }

                if (group.Count > 1 && !overlappingGroups.Any(g => g.All(group.Contains) && group.All(g.Contains)))
                    overlappingGroups.Add(group);
            }

            return overlappingGroups;
        }
        
        private static int CalculateChebyshevDistance((int, int) p1, (int, int) p2)
        {
            int dx = Mathf.Abs(p1.Item1 - p2.Item1);
            int dy = Mathf.Abs(p1.Item2 - p2.Item2);
            return Mathf.Max(dx, dy);
        }
    }
}