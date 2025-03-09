using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using GaussianSplatting.Editor;
using GaussianSplatting.Editor.Utils;
using GaussianSplatting.Runtime;
using SimpleFileBrowser;
using TMPro;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;


namespace StartScene
{
    public class CreateAssetManager : MonoBehaviour
    {
        // UI组件
        public Button browseInputButton;

        public TMP_InputField inputFileField;

        public TMP_InputField outputFolderField;

        public TMP_InputField assetNameField;

        public Toggle importCameraToggle;

        public TMP_Dropdown qualityDropdown;

        public TMP_Dropdown positionDropdown;

        public TMP_Dropdown scaleDropdown;

        public TMP_Dropdown colorDropdown;

        public TMP_Dropdown shDropdown;

        public Button createAssetButton;

        public Button closeButton;

        // 一些信息显示
        public TextMeshProUGUI fileSizeText;

        public TextMeshProUGUI positionFileSizeText;

        public TextMeshProUGUI scaleFileSizeText;

        public TextMeshProUGUI colorFileSizeText;

        public TextMeshProUGUI shFileSizeText;

        public TextMeshProUGUI assetTotalSizeText;

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
        private string _outputFolder = Status.SceneFileRootPlayer;
        private string _assetName;
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

        private GaussianSplatAsset.VectorFormat formatPos
        {
            get => _formatPos;
            set
            {
                if (value == _formatPos) return;
                _formatPos = value;
                UpdateQualityUI();
            }
        }

        private GaussianSplatAsset.VectorFormat _formatScale;

        private GaussianSplatAsset.VectorFormat formatScale
        {
            get => _formatScale;
            set
            {
                if (value == _formatScale) return;
                _formatScale = value;
                UpdateQualityUI();
            }
        }

        private GaussianSplatAsset.ColorFormat _formatColor;

        private GaussianSplatAsset.ColorFormat formatColor
        {
            get => _formatColor;
            set
            {
                if (value == _formatColor) return;
                _formatColor = value;
                UpdateQualityUI();
            }
        }

        private GaussianSplatAsset.SHFormat _formatSH;

        private GaussianSplatAsset.SHFormat formatSH
        {
            get => _formatSH;
            set
            {
                if (value == _formatSH) return;
                _formatSH = value;
                UpdateQualityUI();
            }
        }

        private bool isUsingChunks =>
            _formatPos != GaussianSplatAsset.VectorFormat.Float32 ||
            _formatScale != GaussianSplatAsset.VectorFormat.Float32 ||
            _formatColor != GaussianSplatAsset.ColorFormat.Float32x4 ||
            _formatSH != GaussianSplatAsset.SHFormat.Float32;

        private string _errorMessage;
        private long _fileSize;
        private int _vertexCount; //点数，使用GaussianFileReader读取

        // 一些常量
        private const string CamerasJson = "cameras.json";


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

            // 更新UI
            UpdateQualityUI();
        }

        /// <summary>
        /// 根据质量等级和GS数量更新UI
        /// </summary>
        private void UpdateQualityUI()
        {
            // 更新下拉列表
            UpdateDropdown(qualityDropdown, quality.ToString());
            UpdateDropdown(positionDropdown, formatPos.ToString());
            UpdateDropdown(scaleDropdown, formatScale.ToString());
            UpdateDropdown(colorDropdown, formatColor.ToString());
            UpdateDropdown(shDropdown, formatSH.ToString());

            bool customize = quality == DataQuality.Custom;
            positionDropdown.interactable = customize;
            scaleDropdown.interactable = customize;
            colorDropdown.interactable = customize;
            shDropdown.interactable = customize;

            //计算文件大小
            long posSize = GaussianSplatAsset.CalcPosDataSize(_vertexCount, formatPos);
            long scaleSize = GaussianSplatAsset.CalcOtherDataSize(_vertexCount, formatScale);
            long colorSize = GaussianSplatAsset.CalcColorDataSize(_vertexCount, formatColor);
            long shSize = GaussianSplatAsset.CalcSHDataSize(_vertexCount, formatSH);
            long chunkSize = isUsingChunks ? GaussianSplatAsset.CalcChunkDataSize(_vertexCount) : 0;
            long totalSize = posSize + scaleSize + colorSize + shSize + chunkSize;
            // 更新子文件大小文本
            positionFileSizeText.SetText(EditorUtility.FormatBytes(posSize));
            scaleFileSizeText.SetText(EditorUtility.FormatBytes(scaleSize));
            colorFileSizeText.SetText(EditorUtility.FormatBytes(colorSize));
            shFileSizeText.SetText(EditorUtility.FormatBytes(shSize));
            assetTotalSizeText.SetText(
                $"{EditorUtility.FormatBytes(totalSize)} - {(double)_fileSize / totalSize:F2}x smaller");
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
        /// 更新文件大小UI。使用到_fileSize和_vertexCount
        /// </summary>
        private void UpdateFileSizeUI()
        {
            fileSizeText.SetText($"{EditorUtility.FormatBytes(_fileSize)} - {_vertexCount:N0} splats");
        }

        /// <summary>
        /// 根据枚举类型初始化单个下拉列表的选项
        /// </summary>
        /// <param name="dropdown">下拉列表组件</param>
        /// <param name="enumType">枚举类型</param>
        private static void InitializeDropdownOption(TMP_Dropdown dropdown, Type enumType)
        {
            dropdown.options.Clear();
            dropdown.options.AddRange(Enum.GetNames(enumType).Select(enumName => new TMP_Dropdown.OptionData(enumName))
                .ToList());
        }

        /// <summary>
        /// 初始化对应下拉列表
        /// </summary>
        private void InitializeQualityUI()
        {
            // 初始化选项
            InitializeDropdownOption(qualityDropdown, typeof(DataQuality));
            InitializeDropdownOption(positionDropdown, typeof(GaussianSplatAsset.VectorFormat));
            InitializeDropdownOption(scaleDropdown, typeof(GaussianSplatAsset.VectorFormat));
            InitializeDropdownOption(colorDropdown, typeof(GaussianSplatAsset.ColorFormat));
            InitializeDropdownOption(shDropdown, typeof(GaussianSplatAsset.SHFormat));

            // 添加回调
            qualityDropdown.onValueChanged.AddListener((newQuality => quality = (DataQuality)newQuality));
            positionDropdown.onValueChanged.AddListener(newQuality =>
                formatPos = (GaussianSplatAsset.VectorFormat)newQuality);
            scaleDropdown.onValueChanged.AddListener(newQuality =>
                formatScale = (GaussianSplatAsset.VectorFormat)newQuality);
            colorDropdown.onValueChanged.AddListener(newQuality =>
                formatColor = (GaussianSplatAsset.ColorFormat)newQuality);
            shDropdown.onValueChanged.AddListener(newQuality => formatSH = (GaussianSplatAsset.SHFormat)newQuality);
        }

        /// <summary>
        /// 初始化文件浏览功能
        /// </summary>
        private void InitializeFileUI()
        {
            // 安卓权限申请
#if UNITY_ANDROID
using UnityEngine.Android;

if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
{
    Permission.RequestUserPermission(Permission.ExternalStorageRead);
}
#endif
            // 文本输入框回调
            inputFileField.onValueChanged.AddListener(value =>
            {
                if (value == _inputFile || string.IsNullOrWhiteSpace(value)) return;
                // 更新文件大小、顶点数量
                _vertexCount = GaussianFileReader.ReadFileHeader(value);
                _fileSize = File.Exists(value) ? new FileInfo(value).Length : 0;
                _inputFile = value;

                // 更新文件相关UI
                UpdateFileSizeUI();
                UpdateQualityUI();
                assetNameField.text =
                    Path.GetFileNameWithoutExtension(FilePickerControl.PathToDisplayString(_inputFile));
            });
            outputFolderField.onValueChanged.AddListener(value =>
            {
                if (FolderNameValidator.ValidateFolderName(value, out _errorMessage))
                {
                    _outputFolder = Path.Join(Status.SceneFileRootPlayer, value);
                }
                else
                {
                    MainUIManager.ShowTip(_errorMessage);
                }
            });
            assetNameField.onValueChanged.AddListener(value =>
            {
                if (FolderNameValidator.ValidateFolderName(value, out _errorMessage))
                {
                    _assetName = value;
                }
                else
                {
                    MainUIManager.ShowTip(_errorMessage);
                }
            });
            // 设置文件浏览过滤条件
            FileBrowser.SetFilters(false, new FileBrowser.Filter("点云文件", ".ply", ".slz"));

            // 文件浏览按钮回调
            browseInputButton.onClick.AddListener(() => StartCoroutine(HandleFileBrowser()));
        }

        /// <summary>
        /// 初始化关闭按钮，导入相机选项和创建资产按钮
        /// </summary>
        private void InitializeOthers()
        {
            closeButton.onClick.AddListener(() => gameObject.SetActive(false));
            importCameraToggle.onValueChanged.AddListener(value => _importCameras = value);
            createAssetButton.onClick.AddListener(() =>
            {
                CreateAsset();
                // 关闭窗口，显示提示
                MainUIManager.ShowTip(_errorMessage ??
                                      $"Successfully Create Asset Data for {_inputFile} at {_outputFolder}");
                SceneLoader.LoadGaussianSplatAssets();
                gameObject.SetActive(false);
            });
        }

        /// <summary>
        /// 配置文件浏览逻辑
        /// </summary>
        /// <param name="input">浏览输入(或是输出)文件夹</param>
        /// <returns></returns>
        private IEnumerator HandleFileBrowser()
        {
            yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, allowMultiSelection: false);
            if (FileBrowser.Success)
            {
                inputFileField.text = FileBrowser.Result[0];
            }
        }

        /// <summary>
        /// 按参数对文件大小取整倍数
        /// </summary>
        /// <param name="size">文件大小</param>
        /// <param name="multipleOf">整倍数</param>
        /// <returns></returns>
        private static int NextMultipleOf(int size, int multipleOf)
        {
            return (size + multipleOf - 1) / multipleOf * multipleOf;
        }

        /// <summary>
        /// 创建位置数据文件
        /// </summary>
        /// <param name="inputSplats">点云原始数据</param>
        /// <param name="filePath">存储文件路径</param>
        /// <param name="dataHash">hash校验码</param>
        private void CreatePositionsData(NativeArray<InputSplatData> inputSplats, string filePath, ref Hash128 dataHash)
        {
            int dataLen = inputSplats.Length * GaussianSplatAsset.GetVectorSize(_formatPos);
            dataLen = NextMultipleOf(dataLen, 8); // serialized as ulong
            NativeArray<byte> data = new(dataLen, Allocator.TempJob);

            GaussianSplatAssetCreator.CreatePositionsDataJob job = new GaussianSplatAssetCreator.CreatePositionsDataJob
            {
                m_Input = inputSplats,
                m_Format = _formatPos,
                m_FormatSize = GaussianSplatAsset.GetVectorSize(_formatPos),
                m_Output = data
            };
            job.Schedule(inputSplats.Length, 8192).Complete();

            dataHash.Append(data);

            using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            fs.Write(data);

            data.Dispose();
        }

        /// <summary>
        /// 创建GS缩放（尺寸）数据文件
        /// </summary>
        /// <param name="inputSplats">点云原始数据</param>
        /// <param name="filePath">存储文件路径</param>
        /// <param name="dataHash">hash校验码</param>
        /// <param name="splatSHIndices">聚类后的SH数据</param>
        private void CreateOtherData(NativeArray<InputSplatData> inputSplats, string filePath, ref Hash128 dataHash,
            NativeArray<int> splatSHIndices)
        {
            int formatSize = GaussianSplatAsset.GetOtherSizeNoSHIndex(_formatScale);
            if (splatSHIndices.IsCreated)
                formatSize += 2;
            int dataLen = inputSplats.Length * formatSize;

            dataLen = NextMultipleOf(dataLen, 8); // serialized as ulong
            NativeArray<byte> data = new(dataLen, Allocator.TempJob);

            GaussianSplatAssetCreator.CreateOtherDataJob job = new GaussianSplatAssetCreator.CreateOtherDataJob
            {
                m_Input = inputSplats,
                m_SplatSHIndices = splatSHIndices,
                m_ScaleFormat = _formatScale,
                m_FormatSize = formatSize,
                m_Output = data
            };
            job.Schedule(inputSplats.Length, 8192).Complete();

            dataHash.Append(data);

            using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            fs.Write(data);

            data.Dispose();
        }

        /// <summary>
        /// 创建颜色数据文件
        /// </summary>
        /// <param name="inputSplats">点云原始数据</param>
        /// <param name="filePath">存储文件路径</param>
        /// <param name="dataHash">hash校验码</param>
        private void CreateColorData(NativeArray<InputSplatData> inputSplats, string filePath, ref Hash128 dataHash)
        {
            var (width, height) = GaussianSplatAsset.CalcTextureSize(inputSplats.Length);
            NativeArray<float4> data = new(width * height, Allocator.TempJob);

            GaussianSplatAssetCreator.CreateColorDataJob job = new GaussianSplatAssetCreator.CreateColorDataJob();
            job.m_Input = inputSplats;
            job.m_Output = data;
            job.Schedule(inputSplats.Length, 8192).Complete();

            dataHash.Append(data);
            dataHash.Append((int)_formatColor);

            GraphicsFormat gfxFormat = GaussianSplatAsset.ColorFormatToGraphics(_formatColor);
            int dstSize = (int)GraphicsFormatUtility.ComputeMipmapSize(width, height, gfxFormat);

            if (GraphicsFormatUtility.IsCompressedFormat(gfxFormat))
            {
                Texture2D tex = new Texture2D(width, height, GraphicsFormat.R32G32B32A32_SFloat,
                    TextureCreationFlags.DontInitializePixels | TextureCreationFlags.DontUploadUponCreate);
                tex.SetPixelData(data, 0);
                EditorUtility.CompressTexture(tex, GraphicsFormatUtility.GetTextureFormat(gfxFormat), 100);
                NativeArray<byte> cmpData = tex.GetPixelData<byte>(0);
                using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                fs.Write(cmpData);

                DestroyImmediate(tex);
            }
            else
            {
                GaussianSplatAssetCreator.ConvertColorJob jobConvert = new GaussianSplatAssetCreator.ConvertColorJob
                {
                    width = width,
                    height = height,
                    inputData = data,
                    format = _formatColor,
                    outputData = new NativeArray<byte>(dstSize, Allocator.TempJob),
                    formatBytesPerPixel = dstSize / width / height
                };
                jobConvert.Schedule(height, 1).Complete();
                using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                fs.Write(jobConvert.outputData);
                jobConvert.outputData.Dispose();
            }

            data.Dispose();
        }

        /// <summary>
        /// 创建SH数据文件
        /// </summary>
        /// <param name="inputSplats">点云原始数据</param>
        /// <param name="filePath">存储文件路径</param>
        /// <param name="dataHash">hash校验码</param>
        /// <param name="splatSHIndices">聚类后的SH数据</param>
        private void CreateSHData(NativeArray<InputSplatData> inputSplats, string filePath, ref Hash128 dataHash,
            NativeArray<GaussianSplatAsset.SHTableItemFloat16> clusteredSHs)
        {
            if (clusteredSHs.IsCreated)
            {
                GaussianSplatAssetCreator.EmitSimpleDataFile(clusteredSHs, filePath, ref dataHash);
            }
            else
            {
                int dataLen = (int)GaussianSplatAsset.CalcSHDataSize(inputSplats.Length, _formatSH);
                NativeArray<byte> data = new(dataLen, Allocator.TempJob);
                GaussianSplatAssetCreator.CreateSHDataJob job = new GaussianSplatAssetCreator.CreateSHDataJob
                {
                    m_Input = inputSplats,
                    m_Format = _formatSH,
                    m_Output = data
                };
                job.Schedule(inputSplats.Length, 8192).Complete();
                GaussianSplatAssetCreator.EmitSimpleDataFile(data, filePath, ref dataHash);
                data.Dispose();
            }
        }

        
        /// <summary>
        /// 从文件创建ByteAsset
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>创建的ByteAsset对象</returns>
        public static ByteAsset CreateByteAssetFromFile(string filePath)
        {
            byte[] bytes = File.ReadAllBytes(filePath);
            ByteAsset byteAsset = new ByteAsset(bytes);
            return byteAsset;
        }

        /// <summary>
        /// 根据参数创建资产。在persistentDataPath创建二进制文件。
        /// </summary>
        private unsafe GaussianSplatAsset CreateAsset()
        {
            _errorMessage = null;
            // 路径检查
            if (string.IsNullOrWhiteSpace(_inputFile))
            {
                _errorMessage = $"Select input PLY/SPZ file";
                return null;
            }

            if (string.IsNullOrWhiteSpace(_outputFolder))
            {
                _errorMessage = $"Invalid output folder '{_outputFolder}'";
                return null;
            }

            // 创建文件夹
            Directory.CreateDirectory(_outputFolder);
            // 创建提示信息
            TipsManager infoTips = MainUIManager.ShowTip("Creating Asset...").GetComponent<TipsManager>();
            infoTips.SetButtonInteractable(false);
            //导入相机参数（如有）
            GaussianSplatAsset.CameraInfo[] cameras =
                GaussianSplatAssetCreator.LoadJsonCamerasFile(_inputFile, _importCameras);
            // 读取数据
            using NativeArray<InputSplatData> inputSplats = LoadInputSplatFile(_inputFile);
            if (inputSplats.Length == 0)
            {
                Destroy(infoTips.gameObject);
                return null;
            }

            // 并行计算场景边界范围
            float3 boundsMin, boundsMax;
            var boundsJob = new GaussianSplatAssetCreator.CalcBoundsJob
            {
                m_BoundsMin = &boundsMin,
                m_BoundsMax = &boundsMax,
                m_SplatData = inputSplats
            };
            boundsJob.Schedule().Complete();

            // 更新提示信息
            infoTips.SetTipText("Morton reordering");
            // 对点云数据执行Morton排序以提高后续渲染等计算效率
            GaussianSplatAssetCreator.ReorderMorton(inputSplats, boundsMin, boundsMax);

            // SH聚类
            NativeArray<int> splatSHIndices = default;
            NativeArray<GaussianSplatAsset.SHTableItemFloat16> clusteredSHs = default;
            if (formatSH >= GaussianSplatAsset.SHFormat.Cluster64k)
            {
                infoTips.SetTipText("Cluster SHs");
                GaussianSplatAssetCreator.ClusterSHs(inputSplats, formatSH, out clusteredSHs, out splatSHIndices);
            }

            infoTips.SetTipText("Creating data objects");
            // 初始化GS asset
            GaussianSplatAsset asset = ScriptableObject.CreateInstance<GaussianSplatAsset>();
            asset.Initialize(inputSplats.Length, _formatPos, _formatScale, _formatColor, _formatSH, boundsMin,
                boundsMax, cameras);
            asset.name = _assetName;

            //生成Hash码用于校验
            var dataHash = new Hash128((uint)asset.splatCount, (uint)asset.formatVersion, 0, 0);
            //创建存储不同数据的二进制文件并保存到对应文件夹
            string chunkPath = $"{_outputFolder}/{_assetName}_chk.bytes";
            string posPath = $"{_outputFolder}/{_assetName}_pos.bytes";
            string otherPath = $"{_outputFolder}/{_assetName}_oth.bytes";
            string colorPath = $"{_outputFolder}/{_assetName}_col.bytes";
            string shPath = $"{_outputFolder}/{_assetName}_shs.bytes";

            bool useChunks = isUsingChunks;
            if (useChunks)
            {
                GaussianSplatAssetCreator.CreateChunkData(inputSplats, chunkPath, ref dataHash);
            }

            CreatePositionsData(inputSplats, posPath, ref dataHash);
            CreateOtherData(inputSplats, otherPath, ref dataHash, splatSHIndices);
            CreateColorData(inputSplats, colorPath, ref dataHash);
            CreateSHData(inputSplats, shPath, ref dataHash, clusteredSHs);
            asset.SetDataHash(dataHash);
            // 丢弃临时数据
            splatSHIndices.Dispose();
            clusteredSHs.Dispose();

            // 绑定子数据文件和主文件
            infoTips.SetTipText("Setup data onto asset");

            asset.SetAssetFiles(useChunks ? CreateByteAssetFromFile(chunkPath) : null,
                CreateByteAssetFromFile(posPath),
                CreateByteAssetFromFile(otherPath),
                CreateByteAssetFromFile(colorPath),
                CreateByteAssetFromFile(shPath));
            // 保存主文件
            infoTips.SetTipText("Saving Asset File");

            var assetDataPath = $"{_outputFolder}/{_assetName}{Status.SceneFileSuffixPlayer}";
            BinaryFormatter formatter = new BinaryFormatter();
            // 由于scriptableObject没有被标记为可序列化，因此需要使用自定义数据类实现外部存储
            GaussianSplatAssetData assetData =
                new GaussianSplatAssetData(asset, useChunks, chunkPath, posPath, otherPath, colorPath, shPath);
            using (FileStream stream = new FileStream(assetDataPath, FileMode.Create))
            {
                formatter.Serialize(stream, assetData);
            }

            // 摧毁提示组件
            Destroy(infoTips.gameObject);
            return asset;
        }

        /// <summary>
        /// 从文件读入点云原始数据
        /// </summary>
        /// <param name="filePath">点云文件路径</param>
        /// <returns></returns>
        private NativeArray<InputSplatData> LoadInputSplatFile(string filePath)
        {
            NativeArray<InputSplatData> data = default;
            if (!File.Exists(filePath))
            {
                _errorMessage = $"Did not find {filePath} file";
                return data;
            }

            try
            {
                GaussianFileReader.ReadFile(filePath, out data);
            }
            catch (Exception ex)
            {
                _errorMessage = ex.Message;
            }

            return data;
        }

        private void Start()
        {
            // 初始化
            InitializeQualityUI();
            InitializeFileUI();
            InitializeOthers();

            _outputFolder = Path.Join(Status.SceneFileRootPlayer, outputFolderField.text);
        }
    }

    

    public static class FolderNameValidator
    {
        // 验证文件夹名称是否合法（单层）
        public static bool ValidateFolderName(string folderName, out string errorMessage)
        {
            errorMessage = "";

            // 空值检查
            if (string.IsNullOrWhiteSpace(folderName))
            {
                errorMessage = "Folder name cannot be empty";
                return false;
            }

            // 检查路径分隔符
            if (folderName.Contains(Path.DirectorySeparatorChar.ToString()) ||
                folderName.Contains(Path.AltDirectorySeparatorChar.ToString()))
            {
                errorMessage =
                    $"Name cannot contain path separators ({Path.DirectorySeparatorChar} or {Path.AltDirectorySeparatorChar})";
                return false;
            }

            // 检查相对路径符号
            if (folderName.Trim() == "." || folderName.Trim() == "..")
            {
                errorMessage = "Cannot use '.' or '..' as name";
                return false;
            }

            // 检查非法字符
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                if (folderName.Contains(c.ToString()))
                {
                    errorMessage = $"Name contains invalid character: '{c}'";
                    return false;
                }
            }

            return true;
        }
    }
}