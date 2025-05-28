using UnityEngine;

namespace Sugarscape
{
    [CreateAssetMenu(menuName = "Sugarscape/Environment/SugarPatch")]
    public class SugarPatch : ScriptableObject {
        public int clumpSize;
        public float density;
        public Color patchColor;
    }
}