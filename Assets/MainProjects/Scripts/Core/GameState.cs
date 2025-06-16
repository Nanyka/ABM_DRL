using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Sugarscape
{
    public class GameState
    {
        public int width;
        public int height;

        // Data layers
        private int[,] sugarLayer;
        private int[,] spiceLayer;
        private AgentInfo[,] agentLayer;

        // Constructor
        public GameState(int width, int height)
        {
            this.width = width;
            this.height = height;
            sugarLayer = new int[width, height];
            spiceLayer = new int[width, height];
            agentLayer = new AgentInfo[width, height];
            for (int i = 0; i < width; i++)
            for (int j = 0; j < height; j++)
                agentLayer[i, j] = new AgentInfo();
        }

        // Accessors and mutators by coordinate
        public int GetSugar(int x, int y) => IsValid(x, y) ? sugarLayer[x, y] : 0;

        public void SetSugar(int x, int y, int value)
        {
            if (IsValid(x, y)) sugarLayer[x, y] = value;
        }

        public int GetSpice(int x, int y) => IsValid(x, y) ? spiceLayer[x, y] : 0;

        public void SetSpice(int x, int y, int value)
        {
            if (IsValid(x, y)) spiceLayer[x, y] = value;
        }

        public AgentInfo GetAgent(int x, int y) => IsValid(x, y) ? agentLayer[x, y] : null;

        public void SetAgent(int x, int y, AgentInfo agent)
        {
            if (IsValid(x, y)) agentLayer[x, y] = agent;
        }

        // Access by layer index: 0=sugar, 1=spice, 2=agents
        public object GetByLayer(int layerIndex, int x, int y)
        {
            switch (layerIndex)
            {
                case 0: return GetSugar(x, y);
                case 1: return GetSpice(x, y);
                case 2: return GetAgent(x, y);
                default: throw new ArgumentOutOfRangeException(nameof(layerIndex));
            }
        }

        public void SetByLayer(int layerIndex, int x, int y, object value)
        {
            switch (layerIndex)
            {
                case 0: SetSugar(x, y, (int)value); break;
                case 1: SetSpice(x, y, (int)value); break;
                case 2: SetAgent(x, y, (AgentInfo)value); break;
                default: throw new ArgumentOutOfRangeException(nameof(layerIndex));
            }
        }

        private bool IsValid(int x, int y) =>
            x >= 0 && x < width && y >= 0 && y < height;

        public int CountAgents()
        {
            // string showAgentLayer = "";
            var count = 0;
            for (int i = 0; i < width; i++)
            for (int j = 0; j < height; j++)
                if (agentLayer[i, j].isOccupied)
                {
                    // showAgentLayer += $"({i},{j}), ";
                    count++;
                }

            // Debug.Log($"Agent count: {count} \n{showAgentLayer}");
            return count;
        }

        public (int totalSugar, int totalSpice) CountResources()
        {
            int totalSugar = 0;
            int totalSpice = 0;
            for (int i = 0; i < width; i++)
            for (int j = 0; j < height; j++)
            {
                totalSugar += GetSugar(i, j);
                totalSpice += GetSpice(i, j);
            }

            return (totalSugar, totalSpice);
        }
    }

    // Supporting agent info structure
    [Serializable]
    public class AgentInfo
    {
        public int remainSugar;
        public int remainSpice;
        public float currentMrs;
        public bool isOccupied;

        public AgentInfo()
        {
            isOccupied = false;
        }

        public void UpdateInfo(int sugar = 0, int spice = 0, float mrs = 0, bool occupied = false)
        {
            remainSugar = sugar;
            remainSpice = spice;
            currentMrs = mrs;
            isOccupied = occupied;
        }

        public override string ToString()
        {
            return $"Sugar: {remainSugar}, Spice: {remainSpice},  Occupied: {isOccupied}";
        }
    }
}