using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Sugarscape
{
    public class GridCell : MonoBehaviour
    {
        [SerializeField] private StateStorage stateStorage;
        [SerializeField] private VoidChannel OnTick;
        
        [SerializeField] private Transform sugarTransform;
        [SerializeField] private Transform spriceTransform;

        private IVisualizeComp[] m_VisualizeComp;

        [SerializeField] private int m_Sugar;
        [SerializeField] private int m_Spice;
        [SerializeField] private int m_MaxSugar;
        [SerializeField] private int m_MaxSpice;
        private int m_XCoor;
        private int m_YCoor;

        private void Awake()
        {
            m_VisualizeComp = GetComponentsInChildren<IVisualizeComp>();
        }

        private void OnEnable()
        {
            OnTick.AddListener(VisualizeChanges);
        }

        private void OnDisable()
        {
            OnTick.RemoveListener(VisualizeChanges);
        }

        public void Init(int x, int y, int maxSugar, int maxSpice)
        {
            m_XCoor = x;
            m_YCoor = y;
            m_MaxSugar = maxSugar;
            m_MaxSpice = maxSpice;
            VisualizeChanges();
        }

        private void VisualizeChanges()
        {
            var state = stateStorage.GetValue();
            m_Sugar = state.GetSugar(m_XCoor, m_YCoor);
            m_Spice = state.GetSpice(m_XCoor, m_YCoor);
            var sugarRatio = (m_MaxSugar != 0) ? m_Sugar * 1f / m_MaxSugar : 0f;
            var spiceRatio = (m_MaxSpice != 0) ? m_Spice * 1f / m_MaxSpice : 0f;
            sugarTransform.localScale = new Vector3(1, m_Sugar*1f/5, 1);
            spriceTransform.localScale = new Vector3(1, m_Spice*1f/5, 1);
            m_VisualizeComp[0].Visualize(sugarRatio);
            m_VisualizeComp[1].Visualize(spiceRatio);
        }
    }
}