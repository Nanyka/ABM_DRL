using UnityEngine;

namespace Sugarscape
{
    [CreateAssetMenu(fileName = "BooleanStorage", menuName = "Sugarscape/Storages/BooleanStorage")]
    public class BooleanStorage : ScriptableObject
    {
        private bool value;
        
        public void SetValue(bool value)
        {
            this.value = value;
        }

        public bool GetValue()
        {
            return value;
        }
    }    
}
