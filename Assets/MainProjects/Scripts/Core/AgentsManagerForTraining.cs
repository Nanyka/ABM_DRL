using UnityEngine;

namespace Sugarscape
{
    public class AgentsManagerForTraining : AgentsManager
    {
        protected override void SpawnAgents()
        {
            ResetAgentList();

            int agentIndex = 0;
            for (int i = 0; i < gameSettings.numberOfAgents; i++)
            {
                var xRandom = Random.Range(0, stateStorage.GetValue().width);
                var yRandom = Random.Range(0, stateStorage.GetValue().height);
                var spawnAiAgent = agentIndex >= gameSettings.numberOfAgents * gameSettings.hardCodeAgentProp * 1f / 100;
                var agent = Instantiate(drlAgent[0],
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
    }
}