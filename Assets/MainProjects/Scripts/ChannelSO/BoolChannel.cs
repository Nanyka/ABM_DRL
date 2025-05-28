using UnityEngine;
using UnityEngine.Events;

namespace Sugarscape
{
    [CreateAssetMenu(fileName = "BoolChannel", menuName = "Sugarscape/Channels/BoolChannel")]
    public class BoolChannel : ScriptableObject
    {
        private UnityEvent<bool> channel = new();
        
        public void AddListener(UnityAction<bool> action)
        {
            channel.AddListener(action);
        }

        public void RemoveListener(UnityAction<bool> action)
        {
            channel.RemoveListener(action);
        }

        public void ExecuteChannel(bool value)
        {
            channel?.Invoke(value);
        }
    }
}