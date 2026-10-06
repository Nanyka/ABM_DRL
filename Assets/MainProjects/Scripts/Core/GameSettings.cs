using UnityEngine;
using UnityEngine.Serialization;

namespace Sugarscape
{
    [CreateAssetMenu(menuName = "Sugarscape/Config/GameSettings")]
    public class GameSettings : ScriptableObject
    {
        [Header("Simulation Settings")]
        public int regainRate;
        public int metabolismSugar;
        public int metabolismSpice;
        public int visionRange;
        public int tradeRange;
        public int capacitySugar;
        public int capacitySpice;
        public int initiatedSugar;
        public int initiatedSpice;
        public int poorEndowment; // > 0: each agent starts rich in one random good (initiatedSugar/Spice) and holds only this much of the other
        public float resourceMultiplier = 1f; // scales every cell's sugar/spice capacity; 0 = pure exchange economy
        public float scarcity;
        public bool randomMap;
        public bool randomState;
        public int hardCodeAgentProp;
        public bool ruleMAllowCoLocation; // let rule-based agents enter occupied cells, as DRL agents can
        
        [Header("Training Settings")]
        public bool isPerfectInfo;
        public bool disableNeighborMrs;
        public int obsVersion; // 0 = legacy layout the published models were trained on; 1 = layout fixes; 2 = fixes + relative neighbour MRS
        public bool resourceOnlyObs; // zero every trade-related observation: own MRS, neighbours' MRS, neighbour presence
        public int modelIndex;
        public int numberOfEpisode;
        public int numberOfAgents;
        public float surviveReward;
        public float deathPunishment;
        
        [Header("Reproduction Settings")]
        public bool isReproductive;
        public int minFertilityAge;
        public int maxFertilityAge;
        public int newbornSugar;
        public int newbornSpice;
    }
}