using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Sugarscape
{
    public class GameConfigUI : MonoBehaviour
    {
        [SerializeField] private GameSettings gameSettings;
        [SerializeField] private VoidChannel applyConfigChannel;
        [SerializeField] private IntStorage chooseModelStorage;
        [SerializeField] private VoidChannel OnNewSimulation;
        
        [Header("Configuration")]
        [SerializeField] private Slider numberOfAgent;
        [SerializeField] private Slider hardCodeAgentProp;
        [SerializeField] private Slider regainRate;
        [SerializeField] private Slider metabolismRate;
        [SerializeField] private Slider storageCapacity;
        [SerializeField] private Slider numberOfEpisode;
        [SerializeField] private Slider randomMap;
        [SerializeField] private Toggle randomState;
        [SerializeField] private TMP_Dropdown chooseModel;
        [SerializeField] private Toggle isReproductive;
        [SerializeField] private Slider numberOfSimulation;
        [SerializeField] private bool allowToConfig;
        
        private int m_CountSimulation;

        private void OnEnable()
        {
            OnNewSimulation.AddListener(StartNewEpisode);
        }

        private void OnDisable()
        {
            OnNewSimulation.RemoveListener(StartNewEpisode);
        }

        private void StartNewEpisode()
        {
            if (++m_CountSimulation < numberOfSimulation.value)
                OnApplyConfig();
        }

        private void Start()
        {
            Init();
        }

        private void Init()
        {
            OnSetReproduction(isReproductive.isOn);
        }

        public void OnApplyConfig()
        {
            if (allowToConfig == false)
            {
                chooseModelStorage.SetValue(gameSettings.modelIndex);
                applyConfigChannel.ExecuteChannel();
                return;
            }
            
            gameSettings.numberOfAgents = Mathf.RoundToInt(numberOfAgent.value);
            gameSettings.hardCodeAgentProp = Mathf.RoundToInt(hardCodeAgentProp.value);
            gameSettings.regainRate = Mathf.RoundToInt(regainRate.value);
            gameSettings.metabolismSugar = Mathf.RoundToInt(metabolismRate.value);
            gameSettings.metabolismSpice = Mathf.RoundToInt(metabolismRate.value);
            gameSettings.capacitySugar = Mathf.RoundToInt(storageCapacity.value);
            gameSettings.capacitySpice = Mathf.RoundToInt(storageCapacity.value);
            gameSettings.numberOfEpisode = Mathf.RoundToInt(numberOfEpisode.value);
            gameSettings.scarcity = randomMap.value;
            gameSettings.randomState = randomState.isOn;
            gameSettings.isReproductive = isReproductive.isOn;
            gameSettings.maxFertilityAge = isReproductive.isOn ? 100 : 1000;
            gameSettings.isPerfectInfo = chooseModel.value >1;
            gameSettings.modelIndex = chooseModel.value;
            
            chooseModelStorage.SetValue(chooseModel.value);
            applyConfigChannel.ExecuteChannel();
        }

        public void OnSetReproduction(bool isOn)
        {
            numberOfEpisode.maxValue = isOn ? 1000 : 500;
        }
    }
}