using UnityEngine;

namespace Sugarscape
{
    [CreateAssetMenu(fileName = "StateStorage", menuName = "Sugarscape/Storages/StateStorage")]
    public class StateStorage : ScriptableObject
    {
        private GameState value;
        
        public void SetValue(GameState value)
        {
            this.value = value;
        }

        public GameState GetValue()
        {
            return value;
        }
    }
}