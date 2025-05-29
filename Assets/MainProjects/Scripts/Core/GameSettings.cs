using UnityEngine;
using UnityEngine.Serialization;

namespace Sugarscape
{
    [CreateAssetMenu(menuName = "Sugarscape/Config/GameSettings")]
    public class GameSettings : ScriptableObject
    {
        public int numberOfAgents;
        public int regainRate;
        public int metabolismSugar;
        public int metabolismSpice;
        public int visionRange;
        public int capacitySugar;
        public int capacitySpice;
        public int initiatedSugar;
        public int initiatedSpice;
    }
}