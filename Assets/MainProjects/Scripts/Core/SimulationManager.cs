using System;
using System.Collections;
using Unity.MLAgents;
using UnityEngine;
using UnityEngine.Serialization;

namespace Sugarscape
{
    public class SimulationManager : MonoBehaviour 
    {
        [Tooltip("Invoked every tickInterval seconds.")]
        [SerializeField] private VoidChannel OnTick;
        [SerializeField] private VoidChannel OnSetup;
        [SerializeField] private VoidChannel OnReset;
        [SerializeField] private VoidChannel OnEndStep;
        [SerializeField] private IntStorage actionStorage;
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
            // Debug.Log("Environment is ready");
        }

        private void CheckEndStep()
        {
            // Debug.Log($"Count agent done: {agentsDoneCount.GetValue()}");
            if (!enabled) return;
            if (Academy.Instance.IsCommunicatorOn) StartOneTick();
            else StartCoroutine(BeginStep());
        }

        private IEnumerator BeginStep()
        {
            // yield return new WaitForSeconds(tickInterval);
            yield return new WaitUntil(() => actionStorage.GetValue() == 0);
            StartOneTick();
        }

        private void StartOneTick()
        {
            actionStorage.SetValue(-1);
            m_ResourceManager.HandleTick();
            if (CheckEndSimulation()) Reset();
            OnTick.ExecuteChannel();
        }

        private void StartSimulation()
        {
            enabled = true;
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