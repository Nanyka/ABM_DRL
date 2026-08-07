
using UnityEngine;

namespace Sugarscape
{
    [CreateAssetMenu(fileName = "ModelStorage", menuName = "Sugarscape/Storages/ModelStorage")]
    public class ModelStorage : ScriptableObject
    {
        private Unity.InferenceEngine.ModelAsset value;

        private void OnEnable()
        {
            value = null;
        }

        public void SetValue(Unity.InferenceEngine.ModelAsset value)
        {
            this.value = value;
        }

        public Unity.InferenceEngine.ModelAsset GetValue()
        {
            return value;
        }
    }
}