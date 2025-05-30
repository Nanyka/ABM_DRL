using System;
using System.Collections;
using UnityEngine;

namespace Sugarscape
{
    public class SimulationManager : MonoBehaviour 
    {
        [Tooltip("Invoked every tickInterval seconds.")]
        [SerializeField] private VoidChannel OnTick;
        [SerializeField] private VoidChannel OnSetup;
        [SerializeField] private VoidChannel OnReset;
        [SerializeField] private VoidChannel OnEndStep;

        [SerializeField] private StateStorage gameState;
        [SerializeField] private IntStorage agentsDoneCount;
        
        public float tickInterval = 1f;
        private float m_Timer;
        private IResourceManager m_ResourceManager;
        private int m_AgentsCount;

        private void Awake()
        {
            m_ResourceManager = GetComponent<IResourceManager>();
        }

        private void OnEnable()
        {
            OnSetup.AddListener(Init);
            OnEndStep.AddListener(CheckEndStep);
        }

        private void OnDisable()
        {
            OnSetup.RemoveListener(Init);
            OnEndStep.RemoveListener(CheckEndStep);
        }

        private void Init()
        {
            StartSimulation();
            StartCoroutine(BeginStep());
            Debug.Log("Environment is ready");
        }

        // void Update() {
        //     m_Timer += Time.deltaTime;
        //     if (m_Timer >= tickInterval)
        //     {
        //         m_Timer = 0f;
        //         BeginStep();
        //     }
        // }

        private IEnumerator BeginStep()
        {
            yield return new WaitForSeconds(tickInterval);
            
            agentsDoneCount.SetValue(0);
            m_ResourceManager.HandleTick();
            m_AgentsCount = gameState.GetValue().CountAgents();
            if (CheckEndSimulation()) Reset();
            OnTick.ExecuteChannel();
        }

        private void CheckEndStep()
        {
            // Debug.Log($"Count agent done: {agentsDoneCount.GetValue()}");
            if (!enabled) return;
            if (agentsDoneCount.GetValue() < m_AgentsCount) return;
            StartCoroutine(BeginStep());
        }

        private void StartSimulation()
        {
            enabled = true;
            // OnInitiateAgents.ExecuteChannel();
        }

        private bool CheckEndSimulation()
        {
            if (!enabled) return false;
            return m_AgentsCount <= 0;
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