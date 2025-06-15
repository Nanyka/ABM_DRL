using System;
using TMPro;
using Unity.MLAgents.SideChannels;
using UnityEngine;

namespace Sugarscape
{
    public class TradingCountUI : MonoBehaviour
    {
        [SerializeField] private VoidChannel OnTick;
        [SerializeField] private VoidChannel OnReset;
        [SerializeField] private IntStorage tradeCount;
        [SerializeField] private IntStorage aliveAgentsCount;
        [SerializeField] private TextMeshProUGUI tradeCountText;
        [SerializeField] private TextMeshProUGUI aliveCountText;
        
        private MetricSideChannel m_MetricChannel;

        private void Awake()
        {
            m_MetricChannel = new MetricSideChannel();
            SideChannelManager.RegisterSideChannel(m_MetricChannel);
        }

        private void OnEnable()
        {
            OnTick.AddListener(UpdateCount);
            OnReset.AddListener(ResetCount);
        }

        private void OnDisable()
        {
            OnTick.RemoveListener(UpdateCount);
            OnReset.RemoveListener(ResetCount);
            SideChannelManager.UnregisterSideChannel(m_MetricChannel);
        }

        private void UpdateCount()
        {
            tradeCountText.text = $"Trade: {tradeCount.GetValue().ToString()}";
            aliveCountText.text = $"Alive: {aliveAgentsCount.GetValue().ToString()}";
            
            m_MetricChannel.SendMetric("trade_count", tradeCount.GetValue());
        }

        private void ResetCount()
        {
            tradeCount.SetValue(0);
        }
    }
}
