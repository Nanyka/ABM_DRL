using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Sugarscape
{
    public class GridCell : MonoBehaviour
    {
        // [SerializeField] private TextConfigLoader configLoader;
        // [SerializeField] private GameSettings settings;
        [SerializeField] private StateStorage stateStorage;
        [SerializeField] private VoidChannel OnTick;

        private IVisualizeCell m_VisualizeCell;

        private int m_Sugar;
        private int m_Spice;
        private int m_MaxSugar;
        private int m_MaxSpice;
        private int m_XCoor;
        private int m_YCoor;

        private void Awake()
        {
            m_VisualizeCell = GetComponentInChildren<IVisualizeCell>();
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

        public void Consume()
        {
            // m_Sugar = Mathf.Max(m_Sugar - settings.metabolismSugar, 0);
            // m_Spice = Mathf.Max(m_Spice - settings.metabolismSpice, 0);
        }

        public void Regrow()
        {
            // m_Sugar = Mathf.Min(m_Sugar + settings.regainRate, m_MaxSugar);
            // m_Spice = Mathf.Min(m_Spice + settings.regainRate, m_MaxSpice);
            VisualizeChanges();
        }

        private void VisualizeChanges()
        {
            var state = stateStorage.GetValue();
            var sugarRatio = (m_MaxSugar != 0) ? state.GetSugar(m_XCoor,m_YCoor) * 1f / m_MaxSugar : 0f;
            var spiceRatio = (m_MaxSpice != 0) ? state.GetSpice(m_XCoor,m_YCoor) * 1f / m_MaxSpice : 0f;
            transform.localScale = new Vector3(1, Mathf.Max(sugarRatio, spiceRatio), 1);
            m_VisualizeCell.Visualize(sugarRatio, spiceRatio);
        }
    }
}