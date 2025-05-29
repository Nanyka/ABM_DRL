using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Sugarscape
{
    public class TerrainGenerator : MonoBehaviour, ITerrain
    {
        // public StateStorage stateStorage;
        public GameObject cellPrefab;
        public TextConfigLoader textConfigLoader;
        
        [SerializeField] private VoidChannel OnGenerateTerrain;
        [SerializeField] private VoidChannel OnInitiateAgents;
        
        private List<GridCell> cells = new();

        private void OnEnable()
        {
            OnGenerateTerrain.AddListener(SetupTerrain);
        }

        private void OnDisable()
        {
            OnGenerateTerrain.RemoveListener(SetupTerrain);
        }

        private void SetupTerrain() 
        {
            // textConfigLoader.Init();
            // stateStorage.SetValue(new GameState(textConfigLoader.Width, textConfigLoader.Height)); // First state
            for (int x = 0; x < textConfigLoader.Width; x++) {
                for (int y = 0; y < textConfigLoader.Height; y++) {
                    var go = Instantiate(cellPrefab, new Vector3(x, 0, y), Quaternion.identity, transform);
                    go.name = $"Cell_{x}_{y}";

                    if (go.TryGetComponent(out GridCell cell))
                    {
                        cell.Init(x,y,textConfigLoader.GetSugar(x,y),textConfigLoader.GetSpice(x,y));
                        cells.Add(cell);
                    }
                }
            }
            OnInitiateAgents.ExecuteChannel();
        }

        public IEnumerable<GridCell> Cells => cells;
    }
}