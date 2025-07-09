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
        [SerializeField] private VoidChannel OnAgentsAct;
        [SerializeField] private VoidChannel OnEndStep;

        [SerializeField] private StateStorage stateStorage;
        [SerializeField] private GameSettings gameSettings;
        [SerializeField] private IntStorage agentsDoneCount;
        [SerializeField] private IntStorage aliveAgentsCount;
        [SerializeField] private EntitiesStorage entitiesStorage;
        [SerializeField] private IntStorage chooseModelStorage;
        [SerializeField] private GameObject hardCodeAgent;
        [SerializeField] private bool isShowId;
        [SerializeField] private GameObject[] drlAgent;

        private List<IAgentController> agents = new();
        private int remainAgentsAmount;

        private void OnEnable()
        {
            OnInitiateAgents.AddListener(SpawnAgents);
            OnReset.AddListener(ResetAgents);
            OnAgentsAct.AddListener(AskAgentsActions);
        }

        private void OnDisable()
        {
            OnInitiateAgents.RemoveListener(SpawnAgents);
            OnReset.RemoveListener(ResetAgents);
            OnAgentsAct.RemoveListener(AskAgentsActions);
        }

        private void SpawnAgents()
        {
            ResetAgentList();

            int agentIndex = 0;
            for (int i = 0; i < gameSettings.numberOfAgents; i++)
            {
                var xRandom = Random.Range(0, stateStorage.GetValue().width);
                var yRandom = Random.Range(0, stateStorage.GetValue().height);
                var spawnAiAgent =
                    agentIndex >= gameSettings.numberOfAgents * gameSettings.hardCodeAgentProp * 1f / 100;
                var agent = Instantiate(spawnAiAgent ? drlAgent[chooseModelStorage.GetValue()] : hardCodeAgent,
                    new Vector3(xRandom, 0, yRandom),
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
            
            UpdateAgentLayer();
            entitiesStorage.SetAgents(agents);
            OnSetup.ExecuteChannel();
        }

        private void ResetAgentList()
        {
            foreach (var agent in agents)
            {
                if (agent.IsAlive())
                    Destroy(agent.GetGameObject());
            }
            agents.Clear();
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
            UpdateAgentLayer();
            OnEndStep.ExecuteChannel();
        }

        private void AskAgentsActions()
        {
            agentsDoneCount.SetValue(0);
            remainAgentsAmount = agents.Count(agent => agent.IsAlive());
            UpdateAgentLayer();
            foreach (var agent in agents) agent.AskForActions();
            StartCoroutine(WaitForAgents());
        }

        private IEnumerator WaitForAgents()
        {
            yield return new WaitUntil(() => agentsDoneCount.GetValue() >= remainAgentsAmount);

            UpdateAgentLayer();
            
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
                        float mrsOther = other.GetTradeComp().CalculateMRS(other.RemainSugar(), other.RemainSpice());

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
                
                // Reproduction: check fertile agents on the same tile
                if (gameSettings.isReproductive)
                {
                    var fertile = group.Where(a => a.IsAlive() &&
                                                   a.GetAge() >= gameSettings.minFertilityAge &&
                                                   a.GetAge() <= gameSettings.maxFertilityAge)
                        .ToList();
                    if (fertile.Count >= 2)
                    {
                        var parentA = fertile[0];
                        var parentB = fertile[1];

                        var newId = agents.Count;
                        var prefab = parentA.IsHardCodeAgent() ? hardCodeAgent : drlAgent[chooseModelStorage.GetValue()];
                        var childObj = Instantiate(prefab, new Vector3(pos.xCoor, 0, pos.yCoor), Quaternion.identity, transform);
                        childObj.name = $"Agent_{newId}";

                        if (childObj.TryGetComponent(out IAgentController child))
                        {
                            child.Init(newId, pos.Item1, pos.Item2, isShowId);

                            int sugarFromA = Mathf.Min(parentA.RemainSugar() / 2, gameSettings.newbornSugar / 2);
                            int sugarFromB = Mathf.Min(parentB.RemainSugar() / 2, gameSettings.newbornSugar / 2);
                            int spiceFromA = Mathf.Min(parentA.RemainSpice() / 2, gameSettings.newbornSpice / 2);
                            int spiceFromB = Mathf.Min(parentB.RemainSpice() / 2, gameSettings.newbornSpice / 2);

                            parentA.ChangeSugar(-sugarFromA);
                            parentB.ChangeSugar(-sugarFromB);
                            parentA.ChangeSpice(-spiceFromA);
                            parentB.ChangeSpice(-spiceFromB);

                            child.ChangeSugar(-child.RemainSugar());
                            child.ChangeSpice(-child.RemainSpice());
                            child.ChangeSugar(sugarFromA + sugarFromB);
                            child.ChangeSpice(spiceFromA + spiceFromB);

                            agents.Add(child);
                            // Debug.Log($"Agent {child.GetAgentID()} has been given birth from {fertile[0].GetAgentID()} and {fertile[1].GetAgentID()}");
                        }
                    }
                }
            }

            var aliveAgents = agents.Where(a => a != null && a.IsAlive());
            aliveAgentsCount.SetValue(aliveAgents.Count());
            entitiesStorage.SetAgents(agents);

            OnEndStep.ExecuteChannel();
        }

        private void UpdateAgentLayer()
        {
            stateStorage.GetValue().ResetAgentLayer();
            foreach (var agent in agents) agent.UpdateState();
        }

        private List<List<IAgentController>> FindOverlappingAgents(List<IAgentController> agents)
        {
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