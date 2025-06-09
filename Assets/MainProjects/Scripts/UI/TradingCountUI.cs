using System;
using TMPro;
using Unity.VisualScripting;
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

        private void OnEnable()
        {
            OnTick.AddListener(UpdateCount);
            OnReset.AddListener(ResetCount);
        }

        private void OnDisable()
        {
            OnTick.RemoveListener(UpdateCount);
            OnReset.RemoveListener(ResetCount);
        }

        private void UpdateCount()
        {
            tradeCountText.text = $"Trade: {tradeCount.GetValue().ToString()}";
            aliveCountText.text = $"Alive: {aliveAgentsCount.GetValue().ToString()}";
        }

        private void ResetCount()
        {
            tradeCount.SetValue(0);
        }
    }
}
