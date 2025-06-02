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
        [SerializeField] private GameObject agentPrefab;
        [SerializeField] private IntStorage agentsDoneCount;
        
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
                    aiAgent.Init(agentIndex, xRandom, yRandom);
                    agents.Add(aiAgent);
                }
                // Debug.Log($"Spawned agent {agentIndex} at ({xRandom},{yRandom})");
                agentIndex++;
            }

            OnSetup.ExecuteChannel();
        }

        private void ResetAgents()
        {
            foreach (var agent in agents) agent.Reset();
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
                    agent.GetTradeComp().Trade(farthestAgent,true);
                    
                    // Debug.Log($"  • Agent ID {agent.GetAgentID()}"); // or any identifying property
                }
            }
            
            OnEndStep.ExecuteChannel();
        }
        
        public List<List<IAgentController>> FindOverlappingAgents(List<IAgentController> agents)
        {
            var overlappingGroups = agents
                .Where(agent => agent.IsAlive())
                .GroupBy(a => {
                    var (x,y) = a.GetPosition();
                    return (x, y);
                })
                .Where(g => g.Count() > 1)
                .Select(g => g.ToList())
                .ToList();

            return overlappingGroups;
        }
    }
}