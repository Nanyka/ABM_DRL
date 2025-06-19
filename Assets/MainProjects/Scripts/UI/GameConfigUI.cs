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
        [SerializeField] private int chooseModelIndex;
        
        [Header("Configuration")]
        [SerializeField] private Slider numberOfAgent;
        [SerializeField] private Slider hardCodeAgentProp;
        [SerializeField] private Slider regainRate;
        [SerializeField] private Slider metabolismRate;
        [SerializeField] private Slider numberOfEpisode;
        [SerializeField] private Toggle randomMap;
        [SerializeField] private TMP_Dropdown chooseModel;
        [SerializeField] private bool allowToConfig;

        private void Start()
        {
            if (allowToConfig == false)
            {
                chooseModelStorage.SetValue(chooseModelIndex);
                applyConfigChannel.ExecuteChannel();
            }
        }

        public void OnApplyConfig()
        {
            if (allowToConfig == false) return;
            
            gameSettings.numberOfAgents = Mathf.RoundToInt(numberOfAgent.value);
            gameSettings.hardCodeAgentProp = Mathf.RoundToInt(hardCodeAgentProp.value);
            gameSettings.regainRate = Mathf.RoundToInt(regainRate.value);
            gameSettings.metabolismSugar = Mathf.RoundToInt(metabolismRate.value);
            gameSettings.metabolismSpice = Mathf.RoundToInt(metabolismRate.value);
            gameSettings.numberOfEpisode = Mathf.RoundToInt(numberOfEpisode.value);
            gameSettings.randomMap = randomMap.isOn;
            gameSettings.isPerfectInfo = chooseModel.value >2;
            chooseModelStorage.SetValue(chooseModel.value);
            applyConfigChannel.ExecuteChannel();
        }
    }
}