using UnityEngine;

namespace Sugarscape
{
    [CreateAssetMenu(fileName = "Vector3Storage", menuName = "Sugarscape/Storages/Vector3Storage")]
    public class Vector3Storage : ScriptableObject
    {
        private Vector3 value;
        
        public void SetValue(Vector3 value)
        {
            this.value = value;
        }

        public Vector3 GetValue()
        {
            return value;
        }
    }
}