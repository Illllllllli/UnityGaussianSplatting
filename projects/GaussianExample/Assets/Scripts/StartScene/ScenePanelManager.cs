using System;
using GaussianSplatting.Runtime;
using GSTestScene;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace StartScene
{
    public class ScenePanelManager : MonoBehaviour
    {
        // 提示信息
        public GameObject sceneNameText;

        public GameObject versionText;

        public GameObject splatCountText;

        // 加载按钮
        public Button loadButton;

        // GS资产
        private GaussianSplatAsset _gaussianSplatAsset;

        public GaussianSplatAsset gaussianSplatAsset
        {
            get => _gaussianSplatAsset;
            set
            {
                _gaussianSplatAsset = value;
                sceneName = value.name;
                version = value.formatVersion;
                splatCounts = value.splatCount;
            }
        }

        // 场景名称
        private string _sceneName;

        private string sceneName
        {
            get => _sceneName;
            set
            {
                if (value == _sceneName) return;
                _sceneName = value;
                UpdateSceneName(value);
            }
        }

        // 资产版本
        private int _version;

        private int version
        {
            get => _version;
            set
            {
                if (value == _version) return;
                _version = value;
                UpdateVersion(value);
            }
        }

        // GS数量
        private int _splatCounts;

        private int splatCounts
        {
            get => _splatCounts;
            set
            {
                if (value == _splatCounts) return;
                _splatCounts = value;
                UpdateSplatCount(value);
            }
        }

        //对应的更新方法
        private void UpdateSceneName(string value)
        {
            sceneNameText.GetComponent<TextMeshProUGUI>().SetText(value);
        }

        private void UpdateVersion(int value)
        {
            versionText.GetComponent<TextMeshProUGUI>().SetText(value.ToString());
        }

        private void UpdateSplatCount(int value)
        {
            splatCountText.GetComponent<TextMeshProUGUI>().SetText(value.ToString());
        }
        

        private void Awake()
        {
            loadButton.onClick.AddListener(()=>SceneLoader.LoadScene(gaussianSplatAsset));
        }
    }
}