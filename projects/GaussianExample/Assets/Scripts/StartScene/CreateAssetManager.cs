using System;
using System.Linq;
using GaussianSplatting.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace StartScene
{
    public class CreateAssetManager : MonoBehaviour
    {
        // UI组件
        public Button browseInputButton;

        public Button browseOutputButton;

        public TMP_InputField inputFileField;

        public TMP_InputField outputFolderField;

        public Toggle importCameraToggle;

        public TMP_Dropdown qualityDropdown;

        public TMP_Dropdown positionDropdown;

        public TMP_Dropdown scaleDropdown;

        public TMP_Dropdown colorDropdown;

        public TMP_Dropdown shDropdown;

        public Button createAssetButton;

        // 文件质量枚举
        enum DataQuality
        {
            VeryHigh,
            High,
            Medium,
            Low,
            VeryLow,
            Custom
        }

        // 一些参数
        private string _inputFile;
        private bool _importCameras = true;
        private string _outputFolder;
        private DataQuality _quality = DataQuality.Medium;

        private DataQuality quality
        {
            get => _quality;
            set
            {
                if (_quality == value) return;
                _quality = value;
                ApplyQualityLevel();
            }
        }

        private GaussianSplatAsset.VectorFormat _formatPos;
        private GaussianSplatAsset.VectorFormat _formatScale;
        private GaussianSplatAsset.ColorFormat _formatColor;
        private GaussianSplatAsset.SHFormat _formatSH;

        private string _errorMessage;

        private bool isUsingChunks =>
            _formatPos != GaussianSplatAsset.VectorFormat.Float32 ||
            _formatScale != GaussianSplatAsset.VectorFormat.Float32 ||
            _formatColor != GaussianSplatAsset.ColorFormat.Float32x4 ||
            _formatSH != GaussianSplatAsset.SHFormat.Float32;

        /// <summary>
        /// 根据整体的资产质量分配子项的资产质量.同时更新UI
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private void ApplyQualityLevel()
        {
            switch (_quality)
            {
                case DataQuality.Custom:
                    break;
                case DataQuality.VeryLow: // 18.62x smaller, 32.27 PSNR
                    _formatPos = GaussianSplatAsset.VectorFormat.Norm11;
                    _formatScale = GaussianSplatAsset.VectorFormat.Norm6;
                    _formatColor = GaussianSplatAsset.ColorFormat.BC7;
                    _formatSH = GaussianSplatAsset.SHFormat.Cluster4k;
                    break;
                case DataQuality.Low: // 14.01x smaller, 35.17 PSNR
                    _formatPos = GaussianSplatAsset.VectorFormat.Norm11;
                    _formatScale = GaussianSplatAsset.VectorFormat.Norm6;
                    _formatColor = GaussianSplatAsset.ColorFormat.Norm8x4;
                    _formatSH = GaussianSplatAsset.SHFormat.Cluster16k;
                    break;
                case DataQuality.Medium: // 5.14x smaller, 47.46 PSNR
                    _formatPos = GaussianSplatAsset.VectorFormat.Norm11;
                    _formatScale = GaussianSplatAsset.VectorFormat.Norm11;
                    _formatColor = GaussianSplatAsset.ColorFormat.Norm8x4;
                    _formatSH = GaussianSplatAsset.SHFormat.Norm6;
                    break;
                case DataQuality.High: // 2.94x smaller, 57.77 PSNR
                    _formatPos = GaussianSplatAsset.VectorFormat.Norm16;
                    _formatScale = GaussianSplatAsset.VectorFormat.Norm16;
                    _formatColor = GaussianSplatAsset.ColorFormat.Float16x4;
                    _formatSH = GaussianSplatAsset.SHFormat.Norm11;
                    break;
                case DataQuality.VeryHigh: // 1.05x smaller
                    _formatPos = GaussianSplatAsset.VectorFormat.Float32;
                    _formatScale = GaussianSplatAsset.VectorFormat.Float32;
                    _formatColor = GaussianSplatAsset.ColorFormat.Float32x4;
                    _formatSH = GaussianSplatAsset.SHFormat.Float32;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            UpdateQualityUI();
        }

        /// <summary>
        /// 根据质量等级更新UI
        /// </summary>
        private void UpdateQualityUI()
        {
            UpdateDropdown(qualityDropdown, quality.ToString());
            UpdateDropdown(positionDropdown, _formatPos.ToString());
            UpdateDropdown(scaleDropdown, _formatScale.ToString());
            UpdateDropdown(colorDropdown, _formatColor.ToString());
            UpdateDropdown(shDropdown, _formatSH.ToString());

            bool customize = quality == DataQuality.Custom;
            positionDropdown.interactable = customize;
            scaleDropdown.interactable = customize;
            colorDropdown.interactable = customize;
            shDropdown.interactable = customize;
        }

        /// <summary>
        /// 根据对应字符串选中下拉列表的对应选项
        /// </summary>
        /// <param name="dropdown"></param>
        /// <param name="text"></param>
        private static void UpdateDropdown(TMP_Dropdown dropdown, string text)
        {
            foreach (var optionData in dropdown.options.Where(optionData => optionData.text.Equals(text)))
            {
                dropdown.value = dropdown.options.IndexOf(optionData);
                dropdown.RefreshShownValue();
                break;
            }
        }

        /// <summary>
        /// 根据枚举类型初始化单个下拉列表的选项
        /// </summary>
        /// <param name="dropdown">下拉列表组件</param>
        /// <param name="enumType">枚举类型</param>
        private static void InitializeDropdown(TMP_Dropdown dropdown, Type enumType)
        {
            dropdown.options.Clear();
            dropdown.options.AddRange(Enum.GetNames(enumType).Select(enumName => new TMP_Dropdown.OptionData(enumName))
                .ToList());
        }

        /// <summary>
        /// 初始化对应下拉列表
        /// </summary>
        private void InitializeDropdown()
        {
            // 初始化选项
            InitializeDropdown(qualityDropdown, typeof(DataQuality));
            InitializeDropdown(positionDropdown, typeof(GaussianSplatAsset.VectorFormat));
            InitializeDropdown(scaleDropdown, typeof(GaussianSplatAsset.VectorFormat));
            InitializeDropdown(colorDropdown, typeof(GaussianSplatAsset.ColorFormat));
            InitializeDropdown(shDropdown, typeof(GaussianSplatAsset.SHFormat));

            // 添加回调
            qualityDropdown.onValueChanged.AddListener((newQuality => quality = (DataQuality)newQuality));
            positionDropdown.onValueChanged.AddListener(newQuality =>
                _formatPos = (GaussianSplatAsset.VectorFormat)newQuality);
            scaleDropdown.onValueChanged.AddListener(newQuality =>
                _formatScale = (GaussianSplatAsset.VectorFormat)newQuality);
            colorDropdown.onValueChanged.AddListener(newQuality =>
                _formatColor = (GaussianSplatAsset.ColorFormat)newQuality);
            shDropdown.onValueChanged.AddListener(newQuality => _formatSH = (GaussianSplatAsset.SHFormat)newQuality);
        }

        private void Start()
        {
            // 初始化下拉列表选项
            InitializeDropdown();
        }
    }
}