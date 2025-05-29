using UnityEngine;

namespace Sugarscape
{
    [CreateAssetMenu(menuName = "Sugarscape/Config/SugarPatch")]
    public class SugarPatch : ScriptableObject {
        public int clumpSize;
        public float density;
        public Color patchColor;
    }
}