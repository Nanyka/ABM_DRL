using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sugarscape
{
    public interface IResourceManager
    {
        public void HandleTick();
    }
    
    public class ResourceManager : MonoBehaviour, IResourceManager
    {
        [SerializeField] private StateStorage stateStorage;
        [SerializeField] private GameSettings settings;
        [SerializeField] private TextConfigLoader configLoader;
        
        public void HandleTick() 
        {
            var state = stateStorage.GetValue();
            for (int y = 0; y < state.height; y++)
            {
                for (int x = 0; x < state.width; x++)
                {
                    var sugar = state.GetSugar(x, y);
                    var spice = state.GetSpice(x, y);
                    sugar = Mathf.Min(sugar + settings.regainRate, configLoader.GetSugar(x,y));
                    spice = Mathf.Min(spice + settings.regainRate, configLoader.GetSpice(x,y));
                    state.SetSugar(x, y,sugar);
                    state.SetSpice(x,y,spice);
                }
            }
        }
    }
}