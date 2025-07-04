using System;
using Unity.MLAgents;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Sugarscape
{
    public class GameInitiate : MonoBehaviour
    {
        [SerializeField] private VoidChannel OnApplyConfig;
        [SerializeField] private VoidChannel OnGenerateTerrain;
        [SerializeField] private TextConfigLoader textConfigLoader;
        [SerializeField] private StateStorage stateStorage;
        [SerializeField] private GameSettings gameSettings;
        [SerializeField] private int randomSeed;
        
        private bool isInitialized;

        private void OnEnable()
        {
            OnApplyConfig.AddListener(ConfigurateGame);
        }

        private void OnDisable()
        {
            OnApplyConfig.RemoveListener(ConfigurateGame);
        }

        private void Start()
        {
            if (Academy.Instance.IsCommunicatorOn)
                ConfigurateGame();
        }

        private void ConfigurateGame()
        {
            if (isInitialized)
            {
                OnGenerateTerrain.ExecuteChannel();
                return;
            }
            
            isInitialized = true;
            Random.InitState(randomSeed);
            textConfigLoader.Init();
            stateStorage.SetValue(new GameState(textConfigLoader.Width, textConfigLoader.Height));
            OnGenerateTerrain.ExecuteChannel();
            gameSettings.maxFertilityAge = gameSettings.isReproduction == false ? 1000 : 100;

            if (Academy.Instance.IsCommunicatorOn)
            {
                var regainRate = Academy.Instance.EnvironmentParameters.GetWithDefault("regain_rate", 1);
                var visionRange = Academy.Instance.EnvironmentParameters.GetWithDefault("vision_range", 1);
                var tradeRange = Academy.Instance.EnvironmentParameters.GetWithDefault("trade_range", 1);
                var capacitySugar = Academy.Instance.EnvironmentParameters.GetWithDefault("capacity_sugar", 5);
                var capacitySpices = Academy.Instance.EnvironmentParameters.GetWithDefault("capacity_spices", 5);
                var initiatedSugar = Academy.Instance.EnvironmentParameters.GetWithDefault("initiated_sugar", 5);
                var initiatedSpice = Academy.Instance.EnvironmentParameters.GetWithDefault("initiated_spice", 5);
                var metabolismSugar = Academy.Instance.EnvironmentParameters.GetWithDefault("metabolism_sugar", 2);
                var metabolismSpice = Academy.Instance.EnvironmentParameters.GetWithDefault("metabolism_spice", 2);
                var scarcity = Academy.Instance.EnvironmentParameters.GetWithDefault("scarcity", 0.5f);
                var numberOfEpisode = Academy.Instance.EnvironmentParameters.GetWithDefault("number_of_episode", 100);
                var numberOfAgents = Academy.Instance.EnvironmentParameters.GetWithDefault("number_of_agents", 30);
                var randomMap = Academy.Instance.EnvironmentParameters.GetWithDefault("random_map", 0);
                var isRandomState = Academy.Instance.EnvironmentParameters.GetWithDefault("is_random_state", 0);
                var isPerfectInfo = Academy.Instance.EnvironmentParameters.GetWithDefault("is_perfect_info", 0);
                var modelIndex = Academy.Instance.EnvironmentParameters.GetWithDefault("model_index", 0);

                gameSettings.regainRate = Mathf.RoundToInt(regainRate);
                gameSettings.visionRange = Mathf.RoundToInt(visionRange);
                gameSettings.tradeRange = Mathf.RoundToInt(tradeRange);
                gameSettings.capacitySugar = Mathf.RoundToInt(capacitySugar);
                gameSettings.capacitySpice = Mathf.RoundToInt(capacitySpices);
                gameSettings.initiatedSugar = Mathf.RoundToInt(initiatedSugar);
                gameSettings.initiatedSpice = Mathf.RoundToInt(initiatedSpice);
                gameSettings.metabolismSugar = Mathf.RoundToInt(metabolismSugar);
                gameSettings.metabolismSpice = Mathf.RoundToInt(metabolismSpice);
                gameSettings.scarcity = scarcity;
                gameSettings.numberOfEpisode = Mathf.RoundToInt(numberOfEpisode);
                gameSettings.numberOfAgents = Mathf.RoundToInt(numberOfAgents);
                gameSettings.randomMap = Mathf.Abs(randomMap) > Mathf.Epsilon;
                gameSettings.randomState = Mathf.Abs(randomMap) > Mathf.Epsilon;
                gameSettings.isPerfectInfo = Mathf.Abs(isPerfectInfo) > Mathf.Epsilon;
                gameSettings.modelIndex = Mathf.RoundToInt(modelIndex);

                // Debug.Log($"Regain rate: {regainRate}\n" +
                //     $"Vision range: {visionRange}\n" + 
                //     $"Capacity sugar: {capacitySugar}\n" + 
                //     $"Capacity spice: {capacitySpices}\n" + 
                //     $"Initiated sugar: {initiatedSugar}\n" +
                //     $"Initiated spice: {initiatedSpice}\n" +
                //     $"Metabolism sugar: {metabolismSugar}\n" +
                //     $"Metabolism spice: {metabolismSpice}\n" +
                //     $"Scarcity: {scarcity}\n" +
                //     $"Number of episodes: {numberOfEpisode}\n" +
                //     $"Number of agents: {numberOfAgents}\n" +
                //     $"Random map: {randomMap}"
                //     );
            }
        }
    }
}