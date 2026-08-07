using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Sugarscape
{
    public class SliderShower : MonoBehaviour
    {
        private TextMeshProUGUI m_SliderText;

        private void Awake()
        {
            m_SliderText = GetComponentInParent<TextMeshProUGUI>();
        }

        private void OnEnable()
        {
            var slider = GetComponentInParent<Slider>();
            slider.onValueChanged.AddListener(OnUpdateSliderValue);
        }

        public void OnUpdateSliderValue(float value)
        {
            m_SliderText.text = Mathf.RoundToInt(value).ToString();
        }
    }
}