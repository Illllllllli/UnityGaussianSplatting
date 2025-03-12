using System;
using GaussianSplatting.Editor;
using GaussianSplatting.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = System.Object;

namespace GSTestScene
{
    public class MainUIManager : MonoBehaviour
    {
        public GameObject gaussianSplats;
        public GameObject uiCanvas;
        private event EventHandler<GaussianSplatRenderer> OnCurrentGsRendererChanged;
        private GaussianSplatRenderer _gsRenderer;
        private GaussianSplatRenderer gsRenderer
        {
            get => _gsRenderer;
            set
            {
                if (_gsRenderer == value) return;
                _gsRenderer = value;
                OnCurrentGsRendererChanged?.Invoke(this, value);
            }
        }

        // 功能按钮
        public Button viewButton;
        public Button selectButton;
        public Button editButton;

        public Button exportButton;

        // 标识GS数量
        public TextMeshProUGUI splatCountText;

        private static readonly Color DisableColor = Color.white;
        private static readonly Color EnableColor = new Color(0.5f, 0.72f, 0.79f);

        // Start is called before the first frame update
        private void Start()
        {
            //绑定gs对象
            gsRenderer = gaussianSplats.GetComponent<GaussianSplatRenderer>();
            //配置按钮功能
            viewButton.onClick.AddListener(Status.SwitchViewMode);
            selectButton.onClick.AddListener(Status.SwitchSelectMode);
            editButton.onClick.AddListener(GetComponent<EditManager>().HandleEditClick);
            exportButton.onClick.AddListener(ExportPlyFile);
            //配置滚动条属性
            Slider[] sliders = uiCanvas.GetComponentsInChildren<Slider>();
            foreach (var slider in sliders)
            {
                switch (slider.gameObject.name)
                {
                    case "RotationSlider":
                        Status.UpdateRotateSensitivity(slider.value);
                        slider.onValueChanged.AddListener(Status.UpdateRotateSensitivity);
                        break;
                    case "TranslateSlider":
                        Status.UpdateTranslateSensitivity(slider.value);
                        slider.onValueChanged.AddListener(Status.UpdateTranslateSensitivity);
                        break;
                }
            }

            // 配置场景信息实时更新
            GaussianSplatRenderer.onSplatCountChanged += UpdateSplatInfo;
            GaussianSplatRenderer.onEditSselectedSplatsChanged += UpdateSplatInfo;
            // 切换模式时按钮样式调整
            Status.PlayModeChanged += OnPlayModeUpdate;
            //切换到浏览模式
            Status.SwitchViewMode();
            //初始化场景信息
            UpdateSplatInfo(gsRenderer,EventArgs.Empty);
        }

        private void OnDestroy()
        {
            Status.PlayModeChanged -= OnPlayModeUpdate;
            GaussianSplatRenderer.onEditSselectedSplatsChanged -= UpdateSplatInfo;
            GaussianSplatRenderer.onSplatCountChanged -= UpdateSplatInfo;
        }

        private void OnPlayModeUpdate(Object _,PlayMode mode)
        {
            
                switch (mode)
                {
                    case PlayMode.Select:
                        SetButtonColor(viewButton, DisableColor);
                        SetButtonColor(selectButton, EnableColor);
                        SetButtonColor(editButton, DisableColor);
                        break;
                    case PlayMode.View:
                        SetButtonColor(viewButton, EnableColor);
                        SetButtonColor(selectButton, DisableColor);
                        SetButtonColor(editButton, DisableColor);
                        break;

                    case PlayMode.None:
                    default:
                        break;
                }
            
        }

        /// <summary>
        /// 更换按钮颜色
        /// </summary>
        /// <param name="button">对应按钮</param>
        /// <param name="color">颜色</param>
        private static void SetButtonColor(Button button, Color color)
        {
            if(!button)return;
            button.GetComponent<Image>().color = color;
        }

        /// <summary>
        /// 更新相关信息(GS数量等)
        /// </summary>
        private void UpdateSplatInfo(Object obj, EventArgs _)
        {
            GaussianSplatRenderer gaussianSplatRenderer = obj as GaussianSplatRenderer;
            splatCountText.SetText(
                $"Splat Count \n Selected / Total : {gaussianSplatRenderer?.editSelectedSplats} / {gaussianSplatRenderer?.splatCount}");
        }

        /// <summary>
        /// 导出当前活动的GS为点云文件
        /// </summary>
        private void ExportPlyFile()
        {
            GaussianSplatRendererEditor.ExportPlyFile(gsRenderer,false);
        }
        
    }
}