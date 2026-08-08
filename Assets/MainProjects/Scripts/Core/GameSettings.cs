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
        public float scarcity;
        public bool randomMap;
        public bool randomState;
        public int hardCodeAgentProp;
        
        [Header("Training Settings")]
        public bool isPerfectInfo;
        public bool disableNeighborMrs;
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