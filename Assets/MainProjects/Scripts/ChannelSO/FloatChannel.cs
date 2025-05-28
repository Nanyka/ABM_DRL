using UnityEngine;
using UnityEngine.Events;

namespace Sugarscape
{
    [CreateAssetMenu(fileName = "FloatChannel", menuName = "Sugarscape/Channels/FloatChannel")]
    public class FloatChannel : ScriptableObject
    {
        private UnityEvent<float> channel = new();
        
        public void AddListener(UnityAction<float> action)
        {
            channel.AddListener(action);
        }

        public void RemoveListener(UnityAction<float> action)
        {
            channel.RemoveListener(action);
        }

        public void RemoveAllListener()
        {
            channel.RemoveAllListeners();
        }

        public void ExecuteChannel(float value)
        {
            channel?.Invoke(value);
        }
    }
}