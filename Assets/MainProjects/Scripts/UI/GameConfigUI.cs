using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sugarscape
{
    public class GameConfigUI : MonoBehaviour
    {
        [SerializeField] private GameSettings gameSettings;
        [SerializeField] private VoidChannel applyConfigChannel;
        
        [Header("Configuration")]
        [SerializeField] private Slider numberOfAgent;
        [SerializeField] private Slider hardCodeAgentProp;
        [SerializeField] private Slider regainRate;
        [SerializeField] private Slider metabolismRate;
        [SerializeField] private Slider numberOfEpisode;
        [SerializeField] private Toggle randomMap;

        public void OnApplyConfig()
        {
            gameSettings.numberOfAgents = Mathf.RoundToInt(numberOfAgent.value);
            gameSettings.hardCodeAgentProp = Mathf.RoundToInt(hardCodeAgentProp.value);
            gameSettings.regainRate = Mathf.RoundToInt(regainRate.value);
            gameSettings.metabolismSugar = Mathf.RoundToInt(metabolismRate.value);
            gameSettings.metabolismSpice = Mathf.RoundToInt(metabolismRate.value);
            gameSettings.numberOfEpisode = Mathf.RoundToInt(numberOfEpisode.value);
            gameSettings.randomMap = randomMap.isOn;
            applyConfigChannel.ExecuteChannel();
        }
    }
}