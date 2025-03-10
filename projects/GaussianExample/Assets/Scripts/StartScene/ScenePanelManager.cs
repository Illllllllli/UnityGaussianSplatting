using System;
using GaussianSplatting.Runtime;
using GSTestScene;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace StartScene
{
    public class ScenePanelManager : MonoBehaviour
    {
        // 提示信息
        public GameObject sceneNameText;

        public GameObject editableText;

        public GameObject splatCountText;

        public GameObject filePathText;

        // 加载按钮
        public Button loadButton;
        
        // 删除按钮
        public Button deleteButton;

        public TextMeshProUGUI deleteButtonText;

        private bool _checkDelete = false;
        // GS资产
        private GaussianSplatAsset _gaussianSplatAsset;

        public GaussianSplatAsset gaussianSplatAsset
        {
            get => _gaussianSplatAsset;
            set
            {
                _gaussianSplatAsset = value;
                sceneName = value.name;
                editable = value.enableEdit;
                splatCounts = value.splatCount;
                filePath = value.assetDataPath;
                
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
        
        // 场景名称
        private string _filePath;

        private string filePath
        {
            get => _filePath;
            set
            {
                _filePath = value;
                UpdateFilePath(value);
            }
        }

        // 是否可编辑
        private bool _editable;

        private bool editable
        {
            get => _editable;
            set
            {
                _editable = value;
                UpdateEditable(value);
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

        private void UpdateEditable(bool value)
        {
            Debug.Log(value);
            editableText.GetComponent<TextMeshProUGUI>().SetText(value.ToString());
        }

        private void UpdateSplatCount(int value)
        {
            splatCountText.GetComponent<TextMeshProUGUI>().SetText(value.ToString());
        }
        
        private void UpdateFilePath(string value)
        {
            filePathText.GetComponent<TextMeshProUGUI>().SetText(value);
        }

        private void HandleDeleteAsset()
        {
            if (!_checkDelete)
            {
                _checkDelete = true;
                deleteButtonText.SetText("Press again to delete asset");
                deleteButtonText.color=Color.white;
                deleteButton.GetComponent<Image>().color=Color.red;
            }
            else
            {
                //从文件夹中删除资产文件夹
                if (!FileHelper.DeletePath(gaussianSplatAsset.assetDataPath, true, out string errorMessage))
                {
                    MainUIManager.ShowTip(errorMessage);
                }
                else
                {
                    MainUIManager.ShowTip($"Delete asset {gaussianSplatAsset.name} successfully");
                    SceneLoader.ExistingGaussianSplatAssets.Remove(gaussianSplatAsset);
                    Destroy(gameObject);
                }
            }
        }
        

        private void Awake()
        {
            loadButton.onClick.AddListener(()=>SceneLoader.LoadScene(gaussianSplatAsset));
            deleteButton.onClick.AddListener(HandleDeleteAsset);
        }
    }
}