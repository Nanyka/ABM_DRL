using UnityEngine;

namespace Sugarscape
{
    public class GameInitiate : MonoBehaviour
    {
        [SerializeField] private VoidChannel OnGenerateTerrain;
        [SerializeField] private TextConfigLoader textConfigLoader;
        [SerializeField] private StateStorage stateStorage;

        private void Start()
        {
            ConfigurateGame();
        }

        private void ConfigurateGame()
        {
            textConfigLoader.Init();
            stateStorage.SetValue(new GameState(textConfigLoader.Width, textConfigLoader.Height));
            OnGenerateTerrain.ExecuteChannel();
        }
    }
}