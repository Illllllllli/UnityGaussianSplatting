using System;
using System.Collections;
using System.Linq;
using GaussianSplatting.Editor;
using GaussianSplatting.Runtime;
using GSTestScene.Simulation;
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

        // 右侧属性设置面板
        public GameObject settingPanel;
        public Slider translateSlider;
        public Slider rotateSlider;
        public Slider splatScaleSlider;
        public Slider splatOpacitySlider;
        public TMP_Dropdown renderModeDropdown;

        private GaussianSplatRenderer gsRenderer => gaussianSplats?.GetComponent<GaussianSplatRenderer>();


        // 功能按钮
        public Button viewButton;
        public Button selectButton;
        public Button editButton;
        public Button exportButton;
        public Button editInfoButton;

        public Button simulateButton;

        // 模拟按钮的文本切换
        public TextMeshProUGUI simulateText;

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


        private void InitializeLeftArea()
        {
            //配置按钮功能
            viewButton.onClick.AddListener(Status.SwitchViewMode);
            selectButton.onClick.AddListener(Status.SwitchSelectMode);
            editButton.onClick.AddListener(GetComponent<EditManager>().HandleEditClick);
            exportButton.onClick.AddListener(ExportPlyFile);
            editInfoButton.onClick.AddListener(GetComponent<EditManager>().HandleLogInfoClick);
            simulateButton.onClick.AddListener(HandleSimulateClick);
            // 切换模式时按钮样式调整
            Status.PlayModeChanged += OnPlayModeUpdate;
        }

        private void InitializeBackPanel()
        {
            cancelBackButton.onClick.AddListener(() => SetBackCheckPanelActive(false));
            doBackButton.onClick.AddListener(BackToStartScene);
        }

        private void InitializeSettingPanel()
        {
            // 配置滚动条属性
            Status.UpdateRotateSensitivity(rotateSlider.value);
            Status.UpdateTranslateSensitivity(translateSlider.value);
            rotateSlider.onValueChanged.AddListener(Status.UpdateRotateSensitivity);
            translateSlider.onValueChanged.AddListener(Status.UpdateTranslateSensitivity);
            splatScaleSlider.onValueChanged.AddListener(value =>
            {
                if (gaussianSplats) gsRenderer.m_SplatScale = value;
            });
            splatOpacitySlider.onValueChanged.AddListener(value =>
            {
                if (gaussianSplats) gsRenderer.m_OpacityScale = value;
            });
            // 配置渲染类型下拉菜单属性
            renderModeDropdown.options.Clear();
            renderModeDropdown.options.AddRange(Enum.GetNames(typeof(GaussianSplatRenderer.RenderMode))
                .Select(enumName => new TMP_Dropdown.OptionData(enumName))
                .ToList());
            renderModeDropdown.onValueChanged.AddListener(value =>
            {
                if (gaussianSplats) gsRenderer.m_RenderMode = (GaussianSplatRenderer.RenderMode)value;
            });
            // 配置场景信息实时更新
            GaussianSplatRenderer.onSplatCountChanged += UpdateSplatInfo;
            GaussianSplatRenderer.onEditSselectedSplatsChanged += UpdateSplatInfo;
        }

        //备用
        public Button testButton;

        private void Start()
        {
            // 初始化UI逻辑
            InitializeLeftArea();
            InitializeBackPanel();
            InitializeSettingPanel();
            // 切换到浏览模式
            Status.SwitchViewMode();
            // 初始化场景信息
            if (gaussianSplats)
            {
                UpdateSplatInfo(gsRenderer, EventArgs.Empty);
            }
            testButton.onClick.AddListener(() =>
            {
            });
            
        }

        /// <summary>
        /// 反转右侧属性菜单的激活性
        /// </summary>
        public void ToggleSettingPanelActive()
        {
            settingPanel.SetActive(!settingPanel.activeSelf);
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
        /// 按下物理模拟按钮后的回调
        /// </summary>
        public void HandleSimulateClick()
        {
            if (gsRenderer.asset.enableSimulate)
            {
                if (Status.playMode == PlayMode.Simulate)
                {
                    GetComponent<GaussianSimulator>().ResetSimulate();
                    Status.SwitchViewMode();
                    SetButtonColor(simulateButton, DisableColor);
                    simulateText.SetText("Start Simulate (6)");
                }

                else
                {
                    ShowTip("Press 'Space' to pause the process of simulation");
                    GetComponent<GaussianSimulator>().StartSimulate(true);
                    Status.SwitchSimulateMode();
                    SetButtonColor(simulateButton, EnableColor);
                    simulateText.SetText("Reset (6)");
                }
            }
            else
            {
                ShowTip(
                    "This asset is not simulatable because you did not set it when creating. Please try another asset toggled with \"Enable Simulation\" option.");
            }
        }

        /// <summary>
        /// 实例化一个Tips组件显示Tips.保证任何时候仅存在一个组件
        /// </summary>
        /// <param name="tips">要显示的tips</param>
        /// <param name="unique">是否使用唯一提示组件。若为否则额外生成临时提示组件（只能由用户摧毁）</param>
        /// <returns>生成的组件实例</returns>
        public static GameObject ShowTip(string tips, bool unique = true)
        {
            if (!TipsPanelPrefab || !_mainCanvas) return null;
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

            GameObject tempTipsPanel = Instantiate(TipsPanelPrefab, _mainCanvas.transform);
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
            //重置物体状态
            if (mode != PlayMode.Simulate)
            {
                gameObject.GetComponent<GaussianSimulator>()?.ResetSimulate();
            }
            switch (mode)
            {
                case PlayMode.Select:
                    SetButtonColor(viewButton, DisableColor);
                    SetButtonColor(selectButton, EnableColor);
                    SetButtonColor(editButton, DisableColor);
                    SetButtonColor(simulateButton,DisableColor);
                    break;
                case PlayMode.View:
                    SetButtonColor(viewButton, EnableColor);
                    SetButtonColor(selectButton, DisableColor);
                    SetButtonColor(editButton, DisableColor);
                    SetButtonColor(simulateButton,DisableColor);
                    break;
                case PlayMode.Simulate:
                    SetButtonColor(viewButton, DisableColor);
                    SetButtonColor(selectButton, DisableColor);
                    SetButtonColor(editButton, DisableColor);
                    SetButtonColor(simulateButton,EnableColor);
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
            if (!gaussianSplatRenderer) return;
            splatCountText.SetText(
                $"Splat Count \n Selected / Total : {gaussianSplatRenderer?.editSelectedSplats} / {gaussianSplatRenderer?.splatCount}");
            splatOpacitySlider.value = gaussianSplatRenderer.m_OpacityScale;
            splatScaleSlider.value = gaussianSplatRenderer.m_SplatScale;
            renderModeDropdown.value = (int)gaussianSplatRenderer.m_RenderMode;
        }

        /// <summary>
        /// 导出当前活动的GS为点云文件
        /// </summary>
        public void ExportPlyFile()
        {
            GaussianSplatRendererEditor.ExportPlyFile(gsRenderer, false);
        }
    }
}