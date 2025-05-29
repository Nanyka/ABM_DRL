using System;
using UnityEngine;

namespace Sugarscape
{
    public class SimulationManager : MonoBehaviour 
    {
        [Tooltip("Invoked every tickInterval seconds.")]
        [SerializeField] private VoidChannel OnTick;
        [SerializeField] private VoidChannel OnSetup;
        
        public float tickInterval = 1f;
        private float m_Timer;
        private IResourceManager m_ResourceManager;

        private void Awake()
        {
            m_ResourceManager = GetComponent<IResourceManager>();
        }

        private void OnEnable()
        {
            OnSetup.AddListener(Init);
        }

        private void OnDisable()
        {
            OnSetup.RemoveListener(Init);
        }

        private void Init()
        {
            StartSimulation();
            Debug.Log("Simulation is ready");
        }

        void Update() {
            m_Timer += Time.deltaTime;
            if (m_Timer >= tickInterval) 
            {
                m_Timer = 0f;
                m_ResourceManager.HandleTick();
                OnTick.ExecuteChannel();
            }
        }
        
        public void StartSimulation()
        {
            enabled = true;
        }
        
        public void Pause() => enabled = false;
        
        public void Reset() {
            // TODO: reset environment and agents
            m_Timer = 0f;
        }
    }
}