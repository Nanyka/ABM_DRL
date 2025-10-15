using Unity.Sentis;
using UnityEngine;

namespace Sugarscape
{
    [CreateAssetMenu(fileName = "ModelStorage", menuName = "Sugarscape/Storages/ModelStorage")]
    public class ModelStorage : ScriptableObject
    {
        private ModelAsset value;

        private void OnEnable()
        {
            value = null;
        }

        public void SetValue(ModelAsset value)
        {
            this.value = value;
        }

        public ModelAsset GetValue()
        {
            return value;
        }
    }
}