using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StartScene
{
    public class TipsManager : MonoBehaviour
    {
        public TextMeshProUGUI tipText;
        public Button closeButton;
        public void SetTipText(string text)
        {
            tipText.SetText(text);
        }

        public void SetButtonInteractable(bool active)
        {
            closeButton.interactable = active;
        }

        private void Close()
        {
            Destroy(gameObject);
        }

        private void Start()
        {
            closeButton.onClick.AddListener(Close);
        }
    }
}
