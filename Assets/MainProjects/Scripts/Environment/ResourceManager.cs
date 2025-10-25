using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Sugarscape
{
    public class ResourceManager : MonoBehaviour, ITerrain
    {
        public GameObject cellPrefab;
        public TextConfigLoader textConfigLoader;
        
        [SerializeField] private VoidChannel OnGenerateTerrain;
        [SerializeField] private VoidChannel OnInitiateAgents;
        [SerializeField] private VoidChannel OnTick;
        [SerializeField] private VoidChannel OnAgentsAct;
        [SerializeField] private VoidChannel OnReset;
        [SerializeField] private GameSettings gameSettings;
        [SerializeField] private StateStorage stateStorage;
        [SerializeField] private IntStorage regainStorage;
        [SerializeField] private float sugarMultiplier = 1f;
        [SerializeField] private float spiceMultiplier = 1f;
        
        private List<GridCell> cells = new();

        private void OnEnable()
        {
            OnGenerateTerrain.AddListener(SetupTerrain);
            OnTick.AddListener(HandleTick);
            OnReset.AddListener(ResetTerrain);
        }

        private void OnDisable()
        {
            OnGenerateTerrain.RemoveListener(SetupTerrain);
            OnTick.RemoveListener(HandleTick);
            OnReset.RemoveListener(ResetTerrain);
        }

        private void SetupTerrain()
        {
            var maxResource = Mathf.Max(textConfigLoader.MaxSugar(), textConfigLoader.MaxSpice());
            foreach (var cell in cells) Destroy(cell.gameObject);
            cells.Clear();

            var randomMapProportion = Random.Range(0f, 1f) < gameSettings.scarcity;
            
            for (int x = 0; x < textConfigLoader.Width; x++) {
                for (int y = 0; y < textConfigLoader.Height; y++) {
                    var go = Instantiate(cellPrefab, new Vector3(x, 0, y), Quaternion.identity, transform);
                    go.name = $"Cell_{x}_{y}";

                    if (go.TryGetComponent(out GridCell cell))
                    {
                        if (randomMapProportion)
                        {
                            var maxSugar = Random.Range(0, maxResource);
                            var maxSpice = Random.Range(0, maxResource);
                            cell.Init(x, y, maxSugar, maxSpice);
                        }
                        else cell.Init(x,y,Mathf.RoundToInt(textConfigLoader.GetSugar(x,y)*sugarMultiplier),
                            Mathf.RoundToInt(textConfigLoader.GetSpice(x,y)*spiceMultiplier));
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
                cell.ResetCell(textConfigLoader.GetSugar(cell.m_XCoor, cell.m_YCoor), 
                    textConfigLoader.GetSpice(cell.m_XCoor, cell.m_YCoor));
                
                // if (gameSettings.randomMap)
                // {
                //     var maxSugar = Random.Range(0, textConfigLoader.MaxSugar());
                //     var maxSpice = Random.Range(0, textConfigLoader.MaxSpice());
                //     cell.ResetCell(maxSugar, maxSpice);
                // }
                // else
                // {
                //     cell.ResetCell(textConfigLoader.GetSugar(cell.m_XCoor, cell.m_YCoor), 
                //         textConfigLoader.GetSpice(cell.m_XCoor, cell.m_YCoor));
                // }
            }
        }

        public IEnumerable<GridCell> Cells => cells;
        
        private void HandleTick()
        {
            var totalRegain = 0;
            foreach (var cell in cells) totalRegain += cell.Growth();
            regainStorage.SetValue(totalRegain);
            OnAgentsAct.ExecuteChannel();
        }
    }
}