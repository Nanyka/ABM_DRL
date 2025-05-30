using System;
using UnityEngine;

namespace Sugarscape
{
    public class SimulationManager : MonoBehaviour 
    {
        [Tooltip("Invoked every tickInterval seconds.")]
        [SerializeField] private VoidChannel OnTick;
        [SerializeField] private VoidChannel OnSetup;
        [SerializeField] private VoidChannel OnReset;
        [SerializeField] private StateStorage gameState;
        
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
            Debug.Log("Environment is ready");
        }

        void Update() {
            m_Timer += Time.deltaTime;
            if (m_Timer >= tickInterval) 
            {
                m_Timer = 0f;
                m_ResourceManager.HandleTick();
                if (CheckEndSimulation()) Reset();
                OnTick.ExecuteChannel();
            }
        }

        private void StartSimulation()
        {
            enabled = true;
            // OnInitiateAgents.ExecuteChannel();
        }

        private bool CheckEndSimulation()
        {
            if (!enabled) return false;
            return gameState.GetValue().CountAgents() <= 0;
        }
        
        public void Pause() => enabled = false;
        
        public void Reset() {
            // TODO: reset environment and agents
            m_Timer = 0f;
            enabled = true;
            OnReset.ExecuteChannel();
        }
    }
}