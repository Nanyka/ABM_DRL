using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sugarscape
{
    public class ChooseModel : MonoBehaviour
    {
        [SerializeField] private IntStorage modelIndexStorage;
        private TMP_Dropdown dropdown;

        private void OnEnable()
        {
            dropdown = GetComponent<TMP_Dropdown>();
        }

        public void SetModelIndex(int index)
        {
            modelIndexStorage.SetValue(dropdown.value);
        }
    }
}