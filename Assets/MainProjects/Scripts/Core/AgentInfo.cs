using System;

namespace Sugarscape
{
    [Serializable]
    public class AgentInfo
    {
        public int agentId;
        public int remainSugar;
        public int remainSpice;
        public float currentMrs;
        public bool isOccupied;
        public int Age;
        public int SugarMetabolism;
        public int SpiceMetabolism;
        public int SugarCapacity;
        public int SpiceCapacity;

        public AgentInfo(int id,int sugar = 0, int spice = 0, float mrs = 0, bool occupied = false)
        {
            agentId = id;
            remainSugar = sugar;
            remainSpice = spice;
            currentMrs = mrs;
            isOccupied = occupied;
        }

        public AgentInfo() { }

        // public void UpdateInfo(int sugar = 0, int spice = 0, float mrs = 0, bool occupied = false)
        // {
        //     remainSugar = sugar;
        //     remainSpice = spice;
        //     currentMrs = mrs;
        //     isOccupied = occupied;
        // }

        public override string ToString()
        {
            return $"Sugar: {remainSugar}, Spice: {remainSpice},  Occupied: {isOccupied}";
        }
    }
}