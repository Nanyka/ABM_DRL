using UnityEngine;

namespace Sugarscape
{
    public class VisualizeComp : MonoBehaviour, IVisualizeComp
    {
        [SerializeField] private new string name;

        private Color originalColor = Color.white;
        private Renderer m_Renderer;
        private Material m_Material;

        void Awake()
        {
            m_Renderer = GetComponent<Renderer>();
            m_Material = m_Renderer.material; // per-renderer copy, owned by this component
            originalColor = m_Material.color;
        }

        void OnDestroy()
        {
            // Unity does not free a renderer.material copy with its GameObject; cells and agents are respawned every episode
            if (m_Material != null) Destroy(m_Material);
        }

        public void Visualize(float alpha)
        {
            var color = originalColor;
            color.a = alpha;
            m_Material.color = color;
        }

        public string Name()
        {
            return name;
        }
    }
}
