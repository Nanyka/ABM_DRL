using UnityEngine;
using UnityEngine.Events;

namespace Sugarscape
{
    public class GridCell : MonoBehaviour {
        public int sugar;
        public int maxSugar = 5;
        [System.Serializable]
        public class ResourceChangedEvent : UnityEvent<int> {}
        public ResourceChangedEvent OnResourceChanged = new ResourceChangedEvent();

        public void Consume(int amount) {
            sugar = Mathf.Max(sugar - amount, 0);
            OnResourceChanged.Invoke(sugar);
        }
        public void Regrow(int rate) {
            sugar = Mathf.Min(sugar + rate, maxSugar);
            OnResourceChanged.Invoke(sugar);
        }
    }
}