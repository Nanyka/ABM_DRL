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
        public bool isPerfectInfo;
        
        [Header("Training Settings")]
        public int numberOfEpisode;
        public int numberOfAgents;
        public float surviveReward;
        public float deathPunishment;
    }
}