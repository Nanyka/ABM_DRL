using System;
using UnityEngine;

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
        public GameState(int width, int height) {
            this.width = width;
            this.height = height;
            sugarLayer = new int[width, height];
            spiceLayer = new int[width, height];
            agentLayer = new AgentInfo[width, height];
        }

        // Accessors and mutators by coordinate
        public int GetSugar(int x, int y) => IsValid(x,y) ? sugarLayer[x, y] : 0;
        public void SetSugar(int x, int y, int value) {
            if (IsValid(x,y)) sugarLayer[x, y] = value;
        }

        public int GetSpice(int x, int y) => IsValid(x,y) ? spiceLayer[x, y] : 0;
        public void SetSpice(int x, int y, int value) {
            if (IsValid(x,y)) spiceLayer[x, y] = value;
        }

        public AgentInfo GetAgent(int x, int y) => IsValid(x,y) ? agentLayer[x, y] : null;
        public void SetAgent(int x, int y, AgentInfo agent) {
            if (IsValid(x,y)) agentLayer[x, y] = agent;
        }

        // Access by layer index: 0=sugar, 1=spice, 2=agents
        public object GetByLayer(int layerIndex, int x, int y) {
            switch(layerIndex) {
                case 0: return GetSugar(x,y);
                case 1: return GetSpice(x,y);
                case 2: return GetAgent(x,y);
                default: throw new ArgumentOutOfRangeException(nameof(layerIndex));
            }
        }
        
        public void SetByLayer(int layerIndex, int x, int y, object value) {
            switch(layerIndex) {
                case 0: SetSugar(x,y,(int)value); break;
                case 1: SetSpice(x,y,(int)value); break;
                case 2: SetAgent(x,y,(AgentInfo)value); break;
                default: throw new ArgumentOutOfRangeException(nameof(layerIndex));
            }
        }

        private bool IsValid(int x, int y) =>
            x >= 0 && x < width && y >= 0 && y < height;
    }
    
    // Supporting agent info structure
    [Serializable]
    public class AgentInfo {
        public int id;
        public float energy;
        public Vector2Int position;
        // Add other per-agent stats
    }
}