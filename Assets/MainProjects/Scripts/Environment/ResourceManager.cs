using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sugarscape
{
    public class ResourceManager : MonoBehaviour
    {
        public VoidChannel OnTick;
        public int regrowRate = 1;
        
        private ITerrain terrain;

        private void Awake()
        {
            terrain = GetComponent<ITerrain>();
        }

        private void OnEnable() {
            OnTick.AddListener(HandleTick);
        }
        
        private void OnDisable() {
            OnTick.RemoveListener(HandleTick);
        }
        
        private void HandleTick() {
            foreach (var cell in terrain.Cells) {
                cell.Regrow(regrowRate);
            }
        }
    }
}