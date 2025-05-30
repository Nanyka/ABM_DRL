using System.Collections.Generic;
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
        
        [SerializeField] private StateStorage stateStorage;
        [SerializeField] private GameSettings gameSettings;
        [SerializeField] private GameObject agentPrefab;
        
        private List<IAgentController> agents = new();

        private void OnEnable()
        {
            OnInitiateAgents.AddListener(SpawnAgents);
            OnReset.AddListener(ResetAgents);
        }

        private void OnDisable()
        {
            OnInitiateAgents.RemoveListener(SpawnAgents);
            OnReset.RemoveListener(ResetAgents);
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
                
                agentIndex++;
            }
            
            OnSetup.ExecuteChannel();
        }

        private void ResetAgents()
        {
            foreach (var agent in agents) agent.Reset();
        }
    }
}