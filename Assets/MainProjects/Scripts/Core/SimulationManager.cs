using System;
using UnityEngine;

namespace Sugarscape
{
    public class SimulationManager : MonoBehaviour {
        // public static SimulationManager Instance { get; private set; }

        [Tooltip("Invoked every tickInterval seconds.")]
        public VoidChannel OnTick;
        public VoidChannel OnSetup;
        public float tickInterval = 1f;
        private float timer;

        private void Start()
        {
            SetupSimulation();
        }

        void Update() {
            timer += Time.deltaTime;
            if (timer >= tickInterval) {
                timer = 0f;
                OnTick.ExecuteChannel();
            }
        }
        
        public void SetupSimulation()
        {
            OnSetup.ExecuteChannel();
        }
        
        public void StartSimulation()
        {
            enabled = true;
        }
        
        public void Pause() => enabled = false;
        
        public void Reset() {
            // TODO: reset environment and agents
            timer = 0f;
        }
    }
}