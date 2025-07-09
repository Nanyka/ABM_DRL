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
        [SerializeField] private VoidChannel applyConfigChannel;
        [SerializeField] private VoidChannel OnTick;
        [SerializeField] private VoidChannel OnSetup;
        [SerializeField] private VoidChannel OnReset;
        [SerializeField] private VoidChannel OnEndStep;
        [SerializeField] private IntStorage actionStorage;
        [SerializeField] private StateStorage gameState;
        [SerializeField] private GameSettings gameSettings;
        
        [SerializeField] private int m_Timer;

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
            StartOneTick();
            // Debug.Log("Environment is ready");
        }

        private void CheckEndStep()
        {
            // Debug.Log($"Count agent done: {agentsDoneCount.GetValue()}");
            if (!enabled) return;
            StartOneTick();
        }

        private void StartOneTick()
        {
            actionStorage.SetValue(-1);
            if (CheckEndSimulation() || m_Timer >= gameSettings.numberOfEpisode) Reset();
            else
            {
                m_Timer += 1;
                OnTick.ExecuteChannel();
            }
        }

        private void StartSimulation()
        {
            enabled = true;
        }

        private bool CheckEndSimulation()
        {
            if (!enabled) return false;
            // Debug.Log($"Number of agents: {gameState.GetValue().CountAgents()}");
            return gameState.GetValue().CountAgents() <= 0;
        }
        
        public void Pause() => enabled = false;
        
        public void Reset() {
            m_Timer = 0;
            enabled = true;
            if (Academy.Instance.IsCommunicatorOn)
                applyConfigChannel.ExecuteChannel();
                // OnReset.ExecuteChannel();
            else
                StartCoroutine(BeginNewSimulation());
        }

        private IEnumerator BeginNewSimulation()
        {
            yield return new WaitUntil(() => actionStorage.GetValue() == 0);
            Debug.Log("Simulation started");
            applyConfigChannel.ExecuteChannel();
            // OnReset.ExecuteChannel();
        }
    }
}