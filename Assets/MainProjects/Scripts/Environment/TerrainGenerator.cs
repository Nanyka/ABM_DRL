using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Sugarscape
{
    public class TerrainGenerator : MonoBehaviour, ITerrain, IResourceManager
    {
        public GameObject cellPrefab;
        public TextConfigLoader textConfigLoader;
        
        [SerializeField] private VoidChannel OnGenerateTerrain;
        [SerializeField] private VoidChannel OnInitiateAgents;
        [SerializeField] private VoidChannel OnReset;
        [SerializeField] private GameSettings gameSettings;
        [SerializeField] private StateStorage stateStorage;

        [SerializeField] private bool isRandomize;
        
        private List<GridCell> cells = new();

        private void OnEnable()
        {
            OnGenerateTerrain.AddListener(SetupTerrain);
            OnReset.AddListener(ResetTerrain);
        }

        private void OnDisable()
        {
            OnGenerateTerrain.RemoveListener(SetupTerrain);
            OnReset.RemoveListener(ResetTerrain);
        }

        private void SetupTerrain()
        {
            // Debug.Log($"Max sugar: {textConfigLoader.MaxSugar()}");
            
            for (int x = 0; x < textConfigLoader.Width; x++) {
                for (int y = 0; y < textConfigLoader.Height; y++) {
                    var go = Instantiate(cellPrefab, new Vector3(x, 0, y), Quaternion.identity, transform);
                    go.name = $"Cell_{x}_{y}";

                    if (go.TryGetComponent(out GridCell cell))
                    {
                        if (isRandomize)
                        {
                            var maxSugar = Random.Range(0, textConfigLoader.MaxSugar());
                            var maxSpice = Random.Range(0, textConfigLoader.MaxSpice());
                            cell.Init(x, y, maxSugar, maxSpice);
                        }
                        else cell.Init(x,y,textConfigLoader.GetSugar(x,y),textConfigLoader.GetSpice(x,y));
                        cells.Add(cell);
                    }
                }
            }
            OnInitiateAgents.ExecuteChannel();
        }

        private void ResetTerrain()
        {
            // Debug.Log("Resetting terrain");
            foreach (var cell in cells)
            {
                if (isRandomize)
                {
                    var maxSugar = Random.Range(0, textConfigLoader.MaxSugar());
                    var maxSpice = Random.Range(0, textConfigLoader.MaxSpice());
                    cell.ResetCell(maxSugar, maxSpice);
                }
                else
                {
                    cell.ResetCell(textConfigLoader.GetSugar(cell.m_XCoor, cell.m_YCoor), 
                        textConfigLoader.GetSpice(cell.m_XCoor, cell.m_YCoor));
                }
            }
        }

        public IEnumerable<GridCell> Cells => cells;
        
        public void HandleTick()
        {
            foreach (var cell in cells) cell.Growth();
            
            // var state = stateStorage.GetValue();
            // for (int y = 0; y < state.height; y++)
            // {
            //     for (int x = 0; x < state.width; x++)
            //     {
            //         var sugar = state.GetSugar(x, y);
            //         var spice = state.GetSpice(x, y);
            //         sugar = Mathf.Min(sugar + settings.regainRate, configLoader.GetSugar(x,y));
            //         spice = Mathf.Min(spice + settings.regainRate, configLoader.GetSpice(x,y));
            //         state.SetSugar(x, y,sugar);
            //         state.SetSpice(x,y,spice);
            //     }
            // }
        }
    }
}