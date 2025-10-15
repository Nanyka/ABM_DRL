using Unity.Sentis;
using UnityEngine;

namespace Sugarscape
{
    [CreateAssetMenu(fileName = "WorkerStorage", menuName = "Sugarscape/Storages/WorkerStorage")]
    public class WorkerStorage : ScriptableObject
    {
        private Worker value;

        private void OnEnable()
        {
            value = null;
        }

        public void SetValue(Worker value)
        {
            this.value = value;
        }

        public Worker GetValue()
        {
            return value;
        }
    }
}