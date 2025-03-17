using GaussianSplatting.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StartScene
{
    public class ScenePanelManager : MonoBehaviour
    {
        // 提示信息
        public TextMeshProUGUI sceneNameText;

        public TextMeshProUGUI editableText;
        
        public TextMeshProUGUI simulatableText;
        
        public TextMeshProUGUI splatCountText;

        public TextMeshProUGUI filePathText;


        // 加载按钮
        public Button loadButton;

        // 删除按钮
        public Button deleteButton;

        public TextMeshProUGUI deleteButtonText;

        private bool _checkDelete;

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
                simulatable = value.enableSimulate;
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

        // 是否可模拟
        private bool _simulatable;

        private bool simulatable
        {
            get => _simulatable;
            set
            {
                _simulatable = value;
                UpdateSimulatable(value);
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
            sceneNameText.SetText(value);
        }

        private void UpdateEditable(bool value)
        {
            editableText.SetText(value.ToString());
        }

        private void UpdateSimulatable(bool value)
        {
            simulatableText.SetText(value.ToString());
        }

        private void UpdateSplatCount(int value)
        {
            splatCountText.SetText(value.ToString());
        }

        private void UpdateFilePath(string value)
        {
            filePathText.SetText(value);
        }

        private void HandleDeleteAsset()
        {
            if (!_checkDelete)
            {
                _checkDelete = true;
                deleteButtonText.SetText("Press again to delete asset");
                deleteButtonText.color = Color.white;
                deleteButton.GetComponent<Image>().color = Color.red;
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
            loadButton.onClick.AddListener(() => SceneLoader.LoadScene(gaussianSplatAsset));
            deleteButton.onClick.AddListener(HandleDeleteAsset);
        }
    }
}