using System;
using GaussianSplatting.Runtime;
using GSTestScene;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;

namespace StartScene
{
    public class MainUIManager : MonoBehaviour
    {
        // 场景概要面板预制件
        public GameObject scenePanelPrefab;

        // 场景列表滚动视图的内容物框架
        public GameObject sceneListContent;

        // 滚动视图面板
        public GameObject scrollViewContainer;

        // 三个按钮
        public Button createButton;
        public Button loadButton;
        public Button exitButton;

        private void OnEnable()
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
                if(child !=sceneListContent.transform)
                {
                    Destroy(child.gameObject);
                }
            }

            //往滚动视图里面塞面板预制件
            foreach (GaussianSplatAsset gaussianSplatAsset in SceneTool.ExistingGaussianSplatAssets)
            {
                GameObject scenePanel = Instantiate(scenePanelPrefab, sceneListContent.transform);
                ScenePanelManager scenePanelManager = scenePanel.GetComponent<ScenePanelManager>();
                scenePanelManager.gaussianSplatAsset = gaussianSplatAsset;
            }
        }

        // private void OnDestroy()
        // {
        //     createButton.onClick.RemoveAllListeners();
        //     loadButton.onClick.RemoveAllListeners();
        //     exitButton.onClick.RemoveAllListeners();
        // }

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