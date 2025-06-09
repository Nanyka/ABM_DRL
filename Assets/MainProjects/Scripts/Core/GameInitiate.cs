using Unity.MLAgents;
using UnityEngine;

namespace Sugarscape
{
    public class GameInitiate : MonoBehaviour
    {
        [SerializeField] private VoidChannel OnGenerateTerrain;
        [SerializeField] private TextConfigLoader textConfigLoader;
        [SerializeField] private StateStorage stateStorage;
        [SerializeField] private GameSettings gameSettings;

        private void Start()
        {
            ConfigurateGame();
        }

        private void ConfigurateGame()
        {
            textConfigLoader.Init();
            stateStorage.SetValue(new GameState(textConfigLoader.Width, textConfigLoader.Height));
            OnGenerateTerrain.ExecuteChannel();

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
                //     $"Number of agents: {numberOfAgents}"
                //     );
            }
        }
    }
}