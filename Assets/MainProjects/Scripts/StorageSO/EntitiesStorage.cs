using System.Collections.Generic;
using UnityEngine;

namespace Sugarscape
{
    [CreateAssetMenu(fileName = "EntitiesStorage", menuName = "Sugarscape/Storages/EntitiesStorage")]
    public class EntitiesStorage : ScriptableObject
    {
        private List<IAgentController> agents = new();
        
        public void SetAgents(List<IAgentController> agents)
        {
            this.agents = agents;
        }

        public List<IAgentController> GetAgents()
        {
            return agents;
        }
    }
}