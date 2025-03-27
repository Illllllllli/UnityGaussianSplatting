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
            // 强制面板立即刷新布局
            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
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
            // 弹出提示信息时，不能和场景交互
            Status.UpdateIsInteractive(false);
        }

        private void OnDestroy()
        {
            Status.UpdateIsInteractive(true);
        }
    }
}