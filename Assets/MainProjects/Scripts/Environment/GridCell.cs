using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Sugarscape
{
    public class GridCell : MonoBehaviour
    {
        public int m_XCoor;
        public int m_YCoor;
        
        [SerializeField] private StateStorage stateStorage;
        [SerializeField] private GameSettings settings;
        // [SerializeField] private VoidChannel OnTick;
        
        [SerializeField] private Transform sugarTransform;
        [SerializeField] private Transform spriceTransform;

        private IVisualizeComp[] m_VisualizeComp;

        [SerializeField] private int m_Sugar;
        [SerializeField] private int m_Spice;
        [SerializeField] private int m_MaxSugar;
        [SerializeField] private int m_MaxSpice;

        private void Awake()
        {
            m_VisualizeComp = GetComponentsInChildren<IVisualizeComp>();
        }

        public void Init(int x, int y, int maxSugar, int maxSpice)
        {
            m_XCoor = x;
            m_YCoor = y;
            ResetCell(maxSugar, maxSpice);
            VisualizeChanges();
        }

        public void Growth()
        {
            var state = stateStorage.GetValue();
            m_Sugar = state.GetSugar(m_XCoor,m_YCoor);
            m_Spice = state.GetSpice(m_XCoor, m_YCoor);
            m_Sugar = Mathf.Min(m_Sugar + settings.regainRate, m_MaxSugar);
            m_Spice = Mathf.Min(m_Spice + settings.regainRate, m_MaxSpice);
            UpdateState();
            VisualizeChanges();
        }

        private void UpdateState()
        {
            var state = stateStorage.GetValue();
            state.SetSugar(m_XCoor, m_YCoor,m_Sugar);
            state.SetSpice(m_XCoor, m_YCoor,m_Spice);
        }

        private void VisualizeChanges()
        {
            // var state = stateStorage.GetValue();
            // m_Sugar = state.GetSugar(m_XCoor, m_YCoor);
            // m_Spice = state.GetSpice(m_XCoor, m_YCoor);
            var sugarRatio = (m_MaxSugar != 0) ? m_Sugar * 1f / m_MaxSugar : 0f;
            var spiceRatio = (m_MaxSpice != 0) ? m_Spice * 1f / m_MaxSpice : 0f;
            sugarTransform.localScale = new Vector3(1, m_Sugar*1f/5, 1);
            spriceTransform.localScale = new Vector3(1, m_Spice*1f/5, 1);
            m_VisualizeComp[0].Visualize(sugarRatio);
            m_VisualizeComp[1].Visualize(spiceRatio);
        }

        public void ResetCell(int maxSugar, int maxSpice)
        {
            m_MaxSugar = Mathf.RoundToInt(maxSugar * Random.Range(settings.scarcity,1));
            m_MaxSpice = Mathf.RoundToInt(maxSpice * Random.Range(settings.scarcity,1));
            m_Sugar = Random.Range(0, maxSugar);
            m_Spice = Random.Range(0, maxSpice);
            UpdateState();
        }
        
        public (int sugar, int spice) TotalMapResources(StateStorage stateStorage)
        {
            if (stateStorage == null)
                return (0, 0);

            var state = stateStorage.GetValue();
            if (state == null)
                return (0, 0);

            int totalSugar = 0;
            int totalSpice = 0;
            for (int y = 0; y < state.height; y++)
            {
                for (int x = 0; x < state.width; x++)
                {
                    totalSugar += state.GetSugar(x, y);
                    totalSpice += state.GetSpice(x, y);
                }
            }

            return (totalSugar, totalSpice);
        }
    }
}