using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sugarscape
{
    public class TerrainGenerator : MonoBehaviour, ITerrain
    {
        public VoidChannel OnSetup;
        public GameObject cellPrefab;
        public int width = 20, height = 20;
        
        private List<GridCell> cells = new();

        private void OnEnable()
        {
            OnSetup.AddListener(SetupTerrain);
        }

        private void OnDisable()
        {
            OnSetup.RemoveListener(SetupTerrain);
        }

        private void SetupTerrain() {
            for (int x = 0; x < width; x++) {
                for (int y = 0; y < height; y++) {
                    var go = Instantiate(cellPrefab, new Vector3(x, 0, y), Quaternion.identity, transform);
                    go.name = $"Cell_{x}_{y}";

                    if (go.TryGetComponent(out GridCell cell))
                        cells.Add(cell);
                }
            }
        }

        public IEnumerable<GridCell> Cells => cells;
    }
}