using UnityEngine;

namespace Sugarscape
{
    public class VisualizeComp : MonoBehaviour, IVisualizeComp
    {
        [SerializeField] private new string name;
        
        private Color originalColor = Color.white;
        private Renderer m_Renderer;

        void Awake()
        {
            m_Renderer = GetComponent<Renderer>();
            originalColor = m_Renderer.material.color;
        }

        public void Visualize(float alpha)
        {
            var color = originalColor;
            color.a = alpha;
            m_Renderer.material.color = color;
        }

        public string Name()
        {
            return name;
        }
    }
}