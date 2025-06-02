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

        private int m_Sugar;
        private int m_Spice;
        private int m_MaxSugar;
        private int m_MaxSpice;
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
            m_Sugar = Random.Range(0, maxSugar);
            m_Spice = Random.Range(0, maxSpice);
            VisualizeChanges();
        }

        // public void Consume()
        // {
        //     // m_Sugar = Mathf.Max(m_Sugar - settings.metabolismSugar, 0);
        //     // m_Spice = Mathf.Max(m_Spice - settings.metabolismSpice, 0);
        // }
        //
        // public void Regrow()
        // {
        //     // m_Sugar = Mathf.Min(m_Sugar + settings.regainRate, m_MaxSugar);
        //     // m_Spice = Mathf.Min(m_Spice + settings.regainRate, m_MaxSpice);
        //     VisualizeChanges();
        // }

        private void VisualizeChanges()
        {
            var state = stateStorage.GetValue();
            var sugarRatio = (m_MaxSugar != 0) ? state.GetSugar(m_XCoor,m_YCoor) * 1f / m_MaxSugar : 0f;
            var spiceRatio = (m_MaxSpice != 0) ? state.GetSpice(m_XCoor,m_YCoor) * 1f / m_MaxSpice : 0f;
            sugarTransform.localScale = new Vector3(1, sugarRatio, 1);
            spriceTransform.localScale = new Vector3(1, spiceRatio, 1);
            m_VisualizeComp[0].Visualize(sugarRatio);
            m_VisualizeComp[1].Visualize(spiceRatio);
        }
    }
}