using System;
using GaussianSplatting.Runtime;
using GSTestScene;
using UnityEngine;

using Button = UnityEngine.UI.Button;

namespace StartScene
{
    public class MainUIManager : MonoBehaviour
    {
        // 主面板
        public GameObject mainCanvas;
        private static GameObject _mainCanvas; 
        
        // 场景概要面板预制件
        public GameObject scenePanelPrefab;

        // 场景列表滚动视图的内容物框架
        public GameObject sceneListContent;

        // 滚动视图面板
        public GameObject scrollViewContainer;
        
        // 创建新资产面板
        public GameObject createAssetPanel;
        
        // 提示面板
        public GameObject tipsPanelPrefab;

        public static GameObject TipsPanelPrefab;
        // 提示面板实例
        public static GameObject TipsPanel;

        // 三个按钮
        public Button createButton;
        public Button loadButton;
        public Button exitButton;

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
            createButton.onClick.AddListener(HandleClickCreateButton);
            loadButton.onClick.AddListener(HandleClickLoadButton);
            exitButton.onClick.AddListener(ExitGame);
        }

        /// <summary>
        /// 点击创建场景按钮后的回调
        /// </summary>
        private void HandleClickCreateButton()
        {
            createAssetPanel.SetActive(true);
        }

        /// <summary>
        /// 点击加载场景后的按钮
        /// </summary>
        private void HandleClickLoadButton()
        {
            scrollViewContainer.SetActive(true);
            //清空滚动视图的残留物
            // 清空现有内容
            foreach (Transform child in sceneListContent.GetComponentsInChildren<Transform>())
            {
                if (child != sceneListContent.transform)
                {
                    Destroy(child.gameObject);
                }
            }

            //往滚动视图里面塞面板预制件
            foreach (GaussianSplatAsset gaussianSplatAsset in SceneLoader.ExistingGaussianSplatAssets)
            {
                GameObject scenePanel = Instantiate(scenePanelPrefab, sceneListContent.transform);
                ScenePanelManager scenePanelManager = scenePanel.GetComponent<ScenePanelManager>();
                scenePanelManager.gaussianSplatAsset = gaussianSplatAsset;
            }
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

            GameObject  tempTipsPanel= Instantiate(TipsPanelPrefab, _mainCanvas.transform);
            tempTipsPanel.GetComponent<TipsManager>().SetTipText(tips);
            return tempTipsPanel;

        }


        /// <summary>
        /// 退出游戏
        /// </summary>
        private static void ExitGame()
        {
            Application.Quit();
#if UNITY_EDITOR
            // 在Unity编辑器中，此调用没有效果
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}