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
        [SerializeField] private int modelIndex;
        
        [Header("Configuration")]
        [SerializeField] private Slider numberOfAgent;
        [SerializeField] private Slider hardCodeAgentProp;
        [SerializeField] private Slider regainRate;
        [SerializeField] private Slider metabolismRate;
        [SerializeField] private Slider numberOfEpisode;
        [SerializeField] private Slider randomMap;
        [SerializeField] private Toggle randomState;
        [SerializeField] private TMP_Dropdown chooseModel;
        [SerializeField] private bool allowToConfig;

        public void OnApplyConfig()
        {
            if (allowToConfig == false)
            {
                gameSettings.isPerfectInfo = modelIndex > 2;
                gameSettings.modelIndex = modelIndex + 1;
                chooseModelStorage.SetValue(modelIndex);
                applyConfigChannel.ExecuteChannel();
                return;
            }
            
            gameSettings.numberOfAgents = Mathf.RoundToInt(numberOfAgent.value);
            gameSettings.hardCodeAgentProp = Mathf.RoundToInt(hardCodeAgentProp.value);
            gameSettings.regainRate = Mathf.RoundToInt(regainRate.value);
            gameSettings.metabolismSugar = Mathf.RoundToInt(metabolismRate.value);
            gameSettings.metabolismSpice = Mathf.RoundToInt(metabolismRate.value);
            gameSettings.numberOfEpisode = Mathf.RoundToInt(numberOfEpisode.value);
            gameSettings.scarcity = randomMap.value;
            gameSettings.randomState = randomState.isOn;
            gameSettings.isPerfectInfo = chooseModel.value >2;
            gameSettings.modelIndex = chooseModel.value + 1;
            chooseModelStorage.SetValue(chooseModel.value);
            applyConfigChannel.ExecuteChannel();
        }
    }
}