using System;
using GaussianSplatting.Editor;
using GaussianSplatting.Runtime;
using StartScene;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = System.Object;

namespace GSTestScene
{
    public class MainUIManager : MonoBehaviour
    {
        public GameObject gaussianSplats;

        // 主面板
        private static GameObject _mainCanvas;
        public GameObject mainCanvas;
        
        // 提示面板
        public GameObject tipsPanelPrefab;

        public static GameObject TipsPanelPrefab;
        // 提示面板实例
        public static GameObject TipsPanel;
        
        // 返回到主菜单相关UI
        public GameObject backCheckPanel;
        public Button doBackButton;
        public Button cancelBackButton;

        private GaussianSplatRenderer gsRenderer => gaussianSplats.GetComponent<GaussianSplatRenderer>();


        // 功能按钮
        public Button viewButton;
        public Button selectButton;
        public Button editButton;
        public Button exportButton;
        public Button editInfoButton;

        // 标识GS数量
        public TextMeshProUGUI splatCountText;

        private static readonly Color DisableColor = Color.white;
        private static readonly Color EnableColor = new Color(0.5f, 0.72f, 0.79f);
        
        private void Awake()
        {
            if (tipsPanelPrefab)
            {
                TipsPanelPrefab = tipsPanelPrefab;
            }

            if (mainCanvas)
            {
                _mainCanvas = mainCanvas;
            }
        }
        
        private void Start()
        {
            //配置按钮功能
            viewButton.onClick.AddListener(Status.SwitchViewMode);
            selectButton.onClick.AddListener(Status.SwitchSelectMode);
            editButton.onClick.AddListener(GetComponent<EditManager>().HandleEditClick);
            exportButton.onClick.AddListener(ExportPlyFile);
            editInfoButton.onClick.AddListener(()=>
            {
                GameObject editInfoPanel = GetComponent<EditManager>().commandInfoPanel;
                editInfoPanel.SetActive(!editInfoPanel.activeSelf);
            });
            
            cancelBackButton.onClick.AddListener(() => SetBackCheckPanelActive(false));
            doBackButton.onClick.AddListener(BackToStartScene);
            //配置滚动条属性
            Slider[] sliders = mainCanvas.GetComponentsInChildren<Slider>();
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
            UpdateSplatInfo(gsRenderer, EventArgs.Empty);
        }

        /// <summary>
        /// 设置返回主菜单确认面板的激活性
        /// </summary>
        /// <param name="active">是否激活</param>
        public void SetBackCheckPanelActive(bool active)
        {
            backCheckPanel.SetActive(active);
        }

        /// <summary>
        /// 跳转到开始界面
        /// </summary>
        private void BackToStartScene()
        {
            Status.UpdateIsInteractive(true);
            Status.GaussianSplatAssets.Clear();
            DestroyImmediate(gaussianSplats);
            SceneManager.LoadScene("Start");
        }
        
        /// <summary>
        /// 实例化一个Tips组件显示Tips.保证任何时候仅存在一个组件
        /// </summary>
        /// <param name="tips">要显示的tips</param>
        /// <param name="unique">是否使用唯一提示组件。若为否则额外生成临时提示组件（只能由用户摧毁）</param>
        /// <returns>生成的组件实例</returns>
        public static GameObject ShowTip(string tips, bool unique=true)
        {
            if(!TipsPanelPrefab||!_mainCanvas)return null;
            if (unique)
            {
                if (TipsPanel)
                {
                    Destroy(TipsPanel);
                }

                TipsPanel = Instantiate(TipsPanelPrefab, _mainCanvas.transform);
                TipsPanel.GetComponent<TipsManager>().SetTipText(tips);
                return TipsPanel;
            }

            GameObject tempTipsPanel= Instantiate(TipsPanelPrefab, _mainCanvas.transform);
            tempTipsPanel.GetComponent<TipsManager>().SetTipText(tips);
            return tempTipsPanel;
        }



        /// <summary>
        /// 当编辑器模式发生改变时更换按键颜色
        /// </summary>
        /// <param name="_"></param>
        /// <param name="mode">新的编辑器模式值</param>
        private void OnPlayModeUpdate(Object _, PlayMode mode)
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
            if (!button) return;
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
            GaussianSplatRendererEditor.ExportPlyFile(gsRenderer, false);
        }
    }
}