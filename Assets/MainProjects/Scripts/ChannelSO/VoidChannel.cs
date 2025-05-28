using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Sugarscape
{
    [CreateAssetMenu(fileName = "VoidChannel", menuName = "Sugarscape/Channels/VoidChannel")]
    public class VoidChannel : ScriptableObject
    {
        private UnityEvent channel = new();
        
        public void AddListener(UnityAction action)
        {
            channel.AddListener(action);
        }

        public void RemoveListener(UnityAction action)
        {
            channel.RemoveListener(action);
        }

        public void ExecuteChannel()
        {
            channel?.Invoke();
        }
    }
}
