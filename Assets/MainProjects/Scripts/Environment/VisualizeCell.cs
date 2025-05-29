using UnityEngine;

namespace Sugarscape
{
    public class VisualizeCell : MonoBehaviour, IVisualizeCell
    {
        [Header("Base Colors")] 
        public Color sugarColor = Color.white;
        public Color spiceColor = Color.red;
        
        private Renderer m_Renderer;
        private MaterialPropertyBlock m_Mpb;

        void Awake()
        {
            m_Renderer = GetComponent<Renderer>();
            m_Mpb = new MaterialPropertyBlock();
        }

        public void Visualize(float sugarRatio, float spiceRatio)
        {
            var totalRatio = sugarRatio + spiceRatio;
            m_Renderer.material.color = Color.Lerp(sugarColor, spiceColor,
                totalRatio <= Mathf.Epsilon ? 0.5f : sugarRatio / totalRatio);
        }
    }
}