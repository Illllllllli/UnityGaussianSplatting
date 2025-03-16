using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GaussianSplatting.Runtime;
using StartScene;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Object = System.Object;

namespace GSTestScene
{
    public class EditManager : MonoBehaviour
    {
        [Header("Gaussian Splat Asset Renderer")]
        public GameObject gaussianSplats;

        [Header("Edit Panels and Buttons")] public GameObject editPanel;

        public GameObject editModePanel;
        public GameObject addModePanel;
        public GameObject deleteModePanel;
        public Button closeButton;
        public Button editButton;


        [Header("Command Info UI Components")] public GameObject commandInfoPanel;
        public Button clearLogButton;
        public Button cancelEditButton;
        public TextMeshProUGUI editLogTextPrefab;

        public GameObject logContainer;

        //公用部分UI
        [Header("General UI Components")] public TMP_Dropdown switchEditModeDropdown;

        public Slider maxTrainStepsSlider;
        public TextMeshProUGUI maxTrainStepNumber;

        public Slider gsLrSlider;
        public TextMeshProUGUI gsLrNumber;

        public Slider gsFinalLrSlider;
        public TextMeshProUGUI gsFinalLrNumber;

        public Slider gsColorLrSlider;
        public TextMeshProUGUI gsColorLrNumber;

        public Slider gsOpacityLrSlider;
        public TextMeshProUGUI gsOpacityLrNumber;

        public Slider gsScalingLrSlider;
        public TextMeshProUGUI gsScalingLrNumber;

        public Slider gsRotationLrSlider;
        public TextMeshProUGUI gsRotationLrNumber;

        public TMP_InputField scenePromptField;

        //编辑部分UI
        [Header("Edit Mode UI Components")] public TMP_InputField editPromptField;
        public TMP_InputField editSegPromptField;

        public Slider editAnchorInitG0Slider;
        public TextMeshProUGUI editAnchorInitG0Number;

        public Slider editAnchorInitSlider;
        public TextMeshProUGUI editAnchorInitNumber;

        public Slider editAnchorMultiplierSlider;
        public TextMeshProUGUI editAnchorMultiplierNumber;

        public Slider editLambdaAnchorColorSlider;
        public TextMeshProUGUI editLambdaAnchorColorNumber;

        public Slider editLambdaAnchorGeoSlider;
        public TextMeshProUGUI editLambdaAnchorGeoNumber;

        public Slider editLambdaAnchorScaleSlider;
        public TextMeshProUGUI editLambdaAnchorScaleNumber;

        public Slider editLambdaAnchorOpacitySlider;
        public TextMeshProUGUI editLambdaAnchorOpacityNumber;

        public Slider editMaxDensifyPercentSlider;
        public TextMeshProUGUI editMaxDensifyPercentNumber;

        public Slider editDensifyFromIterSlider;
        public TextMeshProUGUI editDensifyFromIterNumber;

        public Slider editDensifyUntilIterSlider;
        public TextMeshProUGUI editDensifUntilIterNumber;

        public Slider editDensificationIntervalSlider;
        public TextMeshProUGUI editDensificationIntervalNumber;

        //新增部分UI
        [Header("Add Mode UI Components")] public TMP_InputField refinePromptField;
        public TMP_InputField addInpaintPromptField;

        //删除部分UI
        [Header("Delete Mode UI Components")] public TMP_InputField deleteInpaintPromptField;
        public TMP_InputField deleteSegPromptField;

        public Toggle fixHoleToggle;

        public Slider inpaintScaleSlider;
        public TextMeshProUGUI inpaintScaleNumber;

        public Slider maskDilateSlider;
        public TextMeshProUGUI maskDilateNumber;


        public Slider deleteAnchorInitG0Slider;
        public TextMeshProUGUI deleteAnchorInitG0Number;

        public Slider deleteAnchorInitSlider;
        public TextMeshProUGUI deleteAnchorInitNumber;

        public Slider deleteAnchorMultiplierSlider;
        public TextMeshProUGUI deleteAnchorMultiplierNumber;

        public Slider deleteLambdaAnchorColorSlider;
        public TextMeshProUGUI deleteLambdaAnchorColorNumber;

        public Slider deleteLambdaAnchorGeoSlider;
        public TextMeshProUGUI deleteLambdaAnchorGeoNumber;

        public Slider deleteLambdaAnchorScaleSlider;
        public TextMeshProUGUI deleteLambdaAnchorScaleNumber;

        public Slider deleteLambdaAnchorOpacitySlider;
        public TextMeshProUGUI deleteLambdaAnchorOpacityNumber;

        public Slider deleteMaxDensifyPercentSlider;
        public TextMeshProUGUI deleteMaxDensifyPercentNumber;

        public Slider deleteDensifyFromIterSlider;
        public TextMeshProUGUI deleteDensifyFromIterNumber;

        public Slider deleteDensifyUntilIterSlider;
        public TextMeshProUGUI deleteDensifUntilIterNumber;

        public Slider deleteDensificationIntervalSlider;
        public TextMeshProUGUI deleteDensificationIntervalNumber;

        private GaussianSplatAsset gaussianSplatAsset => gaussianSplats.GetComponent<GaussianSplatRenderer>().m_Asset;
        private GaussianProfile _profile;
        private WslPythonRunner _wslPythonRunner = new WslPythonRunner();

        private EditMode _editMode = EditMode.Edit;

        private enum EditMode
        {
            Edit,
            Add,
            Delete
        }
        

        private void Start()
        {
            InitializeEditUsageUI();
            InitializeEditModeSwitchUI();
            InitializeEditDetailUI();
            InitializeCommandInfoUI();
        }

        public void HandleEditClick()
        {
            if (gaussianSplatAsset.enableEdit)
            {
                editPanel.SetActive(true);
                Status.UpdateIsInteractive(false);
            }
            else
            {
                MainUIManager.ShowTip(
                    "This asset is not editable because you did not set it when creating. Please try another asset toggled with \"Enable Edit\" option.");
            }
        }


        /// <summary>
        /// 初始化一些基本组件UI响应
        /// </summary>
        private void InitializeEditUsageUI()
        {
            closeButton.onClick.AddListener(() =>
            {
                editPanel.SetActive(false);
                Status.UpdateIsInteractive(true);
            });
            editButton.onClick.AddListener(StartEdit);
        }

        /// <summary>
        /// 初始化切换编辑模式的下拉菜单
        /// </summary>
        private void InitializeEditModeSwitchUI()
        {
            switchEditModeDropdown.options.Clear();
            switchEditModeDropdown.options.AddRange(Enum.GetNames(typeof(EditMode))
                .Select(enumName => new TMP_Dropdown.OptionData(enumName))
                .ToList());

            switchEditModeDropdown.onValueChanged.AddListener(value =>
            {
                editModePanel.SetActive(false);
                addModePanel.SetActive(false);
                deleteModePanel.SetActive(false);

                switch ((EditMode)value)
                {
                    case EditMode.Edit:
                        editModePanel.SetActive(true);
                        break;
                    case EditMode.Add:
                        addModePanel.SetActive(true);
                        break;
                    case EditMode.Delete:
                        deleteModePanel.SetActive(true);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(value), value, null);
                }

                _editMode = (EditMode)value;
            });
        }

        /// <summary>
        /// 初始化编辑面板UI响应
        /// </summary>
        private void InitializeEditDetailUI()
        {
            // 公用部分UI
            maxTrainStepsSlider.onValueChanged.AddListener((value =>
            {
                maxTrainStepNumber.SetText($"{Mathf.RoundToInt(value)}");
                deleteDensifyFromIterSlider.maxValue = value;
                editDensifyFromIterSlider.maxValue = value;
                deleteDensifyUntilIterSlider.maxValue = value;
                editDensifyUntilIterSlider.maxValue = value;
            }));
            gsLrSlider.onValueChanged.AddListener(value => gsLrNumber.SetText($"{value:f2}"));
            gsFinalLrSlider.onValueChanged.AddListener(value => gsFinalLrNumber.SetText($"{value:f2}"));
            gsColorLrSlider.onValueChanged.AddListener(value => gsColorLrNumber.SetText($"{value:f2}"));
            gsOpacityLrSlider.onValueChanged.AddListener(value => gsOpacityLrNumber.SetText($"{value:f2}"));
            gsScalingLrSlider.onValueChanged.AddListener(value => gsScalingLrNumber.SetText($"{value:f2}"));
            gsRotationLrSlider.onValueChanged.AddListener(value => gsRotationLrNumber.SetText($"{value:f2}"));
            // 编辑部分UI
            editAnchorInitG0Slider.onValueChanged.AddListener(value => editAnchorInitG0Number.SetText($"{value:f2}"));
            editAnchorInitSlider.onValueChanged.AddListener(value => editAnchorInitNumber.SetText($"{value:f2}"));
            editAnchorMultiplierSlider.onValueChanged.AddListener(value =>
                editAnchorMultiplierNumber.SetText($"{value:f2}"));
            editLambdaAnchorColorSlider.onValueChanged.AddListener(value =>
                editLambdaAnchorColorNumber.SetText($"{value:f2}"));
            editLambdaAnchorGeoSlider.onValueChanged.AddListener(value =>
                editLambdaAnchorGeoNumber.SetText($"{value:f2}"));
            editLambdaAnchorScaleSlider.onValueChanged.AddListener(value =>
                editLambdaAnchorScaleNumber.SetText($"{value:f2}"));
            editLambdaAnchorOpacitySlider.onValueChanged.AddListener(value =>
                editLambdaAnchorOpacityNumber.SetText($"{value:f2}"));
            editMaxDensifyPercentSlider.onValueChanged.AddListener(value =>
                editMaxDensifyPercentNumber.SetText($"{value:f2}"));
            editDensifyFromIterSlider.onValueChanged.AddListener(value =>
                editDensifyFromIterNumber.SetText($"{value:f2}"));
            editDensifyUntilIterSlider.onValueChanged.AddListener(value =>
                editDensifUntilIterNumber.SetText($"{value:f2}"));
            editDensificationIntervalSlider.onValueChanged.AddListener(value =>
                editDensificationIntervalNumber.SetText($"{value:f2}"));
            // 新增部分UI
            // 删除部分UI
            inpaintScaleSlider.onValueChanged.AddListener(value => inpaintScaleNumber.SetText($"{value:f2}"));
            maskDilateSlider.onValueChanged.AddListener(value =>
                maskDilateNumber.SetText($"{Mathf.RoundToInt(value)}"));
            deleteAnchorInitG0Slider.onValueChanged.AddListener(
                value => deleteAnchorInitG0Number.SetText($"{value:f2}"));
            deleteAnchorInitSlider.onValueChanged.AddListener(value => deleteAnchorInitNumber.SetText($"{value:f2}"));
            deleteAnchorMultiplierSlider.onValueChanged.AddListener(value =>
                deleteAnchorMultiplierNumber.SetText($"{value:f2}"));
            deleteLambdaAnchorColorSlider.onValueChanged.AddListener(value =>
                deleteLambdaAnchorColorNumber.SetText($"{value:f2}"));
            deleteLambdaAnchorGeoSlider.onValueChanged.AddListener(value =>
                deleteLambdaAnchorGeoNumber.SetText($"{value:f2}"));
            deleteLambdaAnchorScaleSlider.onValueChanged.AddListener(value =>
                deleteLambdaAnchorScaleNumber.SetText($"{value:f2}"));
            deleteLambdaAnchorOpacitySlider.onValueChanged.AddListener(value =>
                deleteLambdaAnchorOpacityNumber.SetText($"{value:f2}"));
            deleteMaxDensifyPercentSlider.onValueChanged.AddListener(value =>
                deleteMaxDensifyPercentNumber.SetText($"{value:f2}"));
            deleteDensifyFromIterSlider.onValueChanged.AddListener(value =>
                deleteDensifyFromIterNumber.SetText($"{value:f2}"));
            deleteDensifyUntilIterSlider.onValueChanged.AddListener(value =>
                deleteDensifUntilIterNumber.SetText($"{value:f2}"));
            deleteDensificationIntervalSlider.onValueChanged.AddListener(value =>
                deleteDensificationIntervalNumber.SetText($"{value:f2}"));
        }

        /// <summary>
        /// 初始化编辑日志及控制面板UUI
        /// </summary>
        private void InitializeCommandInfoUI()
        {
            cancelEditButton.onClick.AddListener(TryCancelEdit);
            clearLogButton.onClick.AddListener(ClearLogs);
        }

        /// <summary>
        /// 根据UI内容更新当前模式配置
        /// </summary>
        private void UpdateProfile()
        {
            switch (_editMode)
            {
                case EditMode.Edit:
                    _profile = new EditProfile();
                    UpdateEditProfile();
                    break;
                case EditMode.Add:
                    _profile = new AddProfile();
                    UpdateAddProfile();
                    break;
                case EditMode.Delete:
                    _profile = new DeleteProfile();
                    UpdateDeleteProfile();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            UpdateGeneralProfile();
        }

        /// <summary>
        /// 根据UI内容更新通用配置
        /// </summary>
        private void UpdateGeneralProfile()
        {
            _profile.ScriptPath =
                WslPythonRunner.ConvertToWslPath(Path.Join(Status.EditorFolder, Status.PythonScript));
            string configPath = _editMode switch
            {
                EditMode.Edit => Status.EditN2NConfigPath,
                EditMode.Add => Status.AddConfigPath,
                EditMode.Delete => Status.DeleteConfigPath,
                _ => throw new ArgumentOutOfRangeException()
            };
            _profile.ConfigPath =
                WslPythonRunner.ConvertToWslPath(Path.Join(Status.EditorFolder, configPath));
            _profile.DataSourcePath =
                WslPythonRunner.ConvertToWslPath(Path.Join(gaussianSplatAsset.assetDataPath, Status.ColmapDir));
            _profile.GsSourcePath =
                WslPythonRunner.ConvertToWslPath(Path.Join(gaussianSplatAsset.assetDataPath,
                    Status.AssetPlyFileLocalDir,
                    Status.AssetPlyFileName));
            _profile.MaxTrainSteps = (uint)Mathf.RoundToInt(maxTrainStepsSlider.value);
            _profile.GsLrScaler = gsLrSlider.value;
            _profile.GsFinalLrScaler = gsFinalLrSlider.value;
            _profile.GsColorLrScaler = gsColorLrSlider.value;
            _profile.GsOpacityLrScaler = gsOpacityLrSlider.value;
            _profile.GsScalingLrScaler = gsScalingLrSlider.value;
            _profile.GsRotationLrScaler = gsRotationLrSlider.value;
            _profile.ScenePrompt = scenePromptField.text;
        }

        /// <summary>
        /// 根据UI内容更新编辑模式配置
        /// </summary>
        private void UpdateEditProfile()
        {
            if (_profile is not EditProfile editProfile) return;
            editProfile.EditPrompt = editPromptField.text;
            editProfile.SegPrompt = editSegPromptField.text;
            editProfile.AnchorWeightInitG0 = editAnchorInitG0Slider.value;
            editProfile.AnchorWeightInit = editAnchorInitSlider.value;
            editProfile.AnchorWeightMultiplier = editAnchorMultiplierSlider.value;
            editProfile.LambdaAnchorColor = editLambdaAnchorColorSlider.value;
            editProfile.LambdaAnchorGeo = editLambdaAnchorGeoSlider.value;
            editProfile.LambdaAnchorScale = editLambdaAnchorScaleSlider.value;
            editProfile.LambdaAnchorOpacity = editLambdaAnchorOpacitySlider.value;
            editProfile.MaxDensifyPercent = editMaxDensifyPercentSlider.value;
            editProfile.DensifyFromIter = (uint)Mathf.RoundToInt(editDensifyFromIterSlider.value);
            editProfile.DensifyUntilIter = (uint)Mathf.RoundToInt(editDensifyUntilIterSlider.value);
            editProfile.DensificationInterval = (uint)Mathf.RoundToInt(editDensificationIntervalSlider.value);
        }

        /// <summary>
        /// 根据UI内容更新添加模式配置
        /// </summary>
        private void UpdateAddProfile()
        {
            if (_profile is not AddProfile addProfile) return;
            addProfile.InpaintPrompt = addInpaintPromptField.text;
            addProfile.RefinePrompt = refinePromptField.text;
        }

        /// <summary>
        /// 根据UI内容更新删除模式配置
        /// </summary>
        private void UpdateDeleteProfile()
        {
            if (_profile is not DeleteProfile deleteProfile) return;
            deleteProfile.InpaintPrompt = deleteInpaintPromptField.text;
            deleteProfile.SegPrompt = deleteSegPromptField.text;
            deleteProfile.FixHoles = fixHoleToggle.isOn;
            deleteProfile.InpaintScale = inpaintScaleSlider.value;
            deleteProfile.MaskDilate = (uint)Mathf.RoundToInt(maskDilateSlider.value);
            deleteProfile.AnchorWeightInitG0 = deleteAnchorInitG0Slider.value;
            deleteProfile.AnchorWeightInit = deleteAnchorInitSlider.value;
            deleteProfile.AnchorWeightMultiplier = deleteAnchorMultiplierSlider.value;
            deleteProfile.LambdaAnchorColor = deleteLambdaAnchorColorSlider.value;
            deleteProfile.LambdaAnchorGeo = deleteLambdaAnchorGeoSlider.value;
            deleteProfile.LambdaAnchorScale = deleteLambdaAnchorScaleSlider.value;
            deleteProfile.LambdaAnchorOpacity = deleteLambdaAnchorOpacitySlider.value;
            deleteProfile.MaxDensifyPercent = deleteMaxDensifyPercentSlider.value;
            deleteProfile.DensifyFromIter = (uint)Mathf.RoundToInt(deleteDensifyFromIterSlider.value);
            deleteProfile.DensifyUntilIter = (uint)Mathf.RoundToInt(deleteDensifyUntilIterSlider.value);
            deleteProfile.DensificationInterval = (uint)Mathf.RoundToInt(deleteDensificationIntervalSlider.value);
        }

        /// <summary>
        /// 点击开始编辑按钮的回调
        /// </summary>
        private async void StartEdit()
        {
            UpdateProfile();
            _wslPythonRunner = new WslPythonRunner();
            bool checkProfile = _profile.GetCommand(out string command);
            Debug.Log(command);
            string workPath = WslPythonRunner.ConvertToWslPath(Status.CondaEnvName);
            if (checkProfile)
            {
                // UI和编辑器模式更新
                commandInfoPanel.SetActive(true);
                editPanel.SetActive(false);
                Status.SwitchViewMode();
                MainUIManager.ShowTip("Edit started. Manual edits will not be saved until edit finished.");
                // 调用编辑指令
                await _wslPythonRunner.ExecuteWslCommand(
                    $"source activate {Status.CondaEnvName} && cd {workPath} && python {command}",
                    OnFinished, DataReceived, ErrorReceived);
            }
            else
            {
                MainUIManager.ShowTip(command);
            }

            return;

            void DataReceived(Object _, DataReceivedEventArgs args)
            {
                if (args?.Data == null) return;
                string data = WslPythonRunner.ReplaceIncompatibleCharacters(args.Data);
                UpdateLog(false, data);
                if (data.StartsWith(Status.PlyUpdateFlag))
                {
                    StartCoroutine(UpdateGaussianOnScene());
                }
            }

            void ErrorReceived(Object _, DataReceivedEventArgs args)
            {
                if (args?.Data == null) return;
                string data = WslPythonRunner.ReplaceIncompatibleCharacters(args.Data);
                UpdateLog(true, data);
            }

            void OnFinished(int exitCode)
            {
                string notice = exitCode switch
                {
                    0 => "Edit finished successfully",
                    1 => "Edit was stopped by errors",
                    2 => "Edit was cancelled",
                    _ => throw new ArgumentOutOfRangeException(nameof(exitCode), exitCode, null)
                };
                MainUIManager.ShowTip(notice);
                Status.IsEditing = false;
            }
        }

        /// <summary>
        /// 尝试取消编辑
        /// </summary>
        private void TryCancelEdit()
        {
            _wslPythonRunner.CancelCommand(out string result);
            MainUIManager.ShowTip(result);
        }

        /// <summary>
        /// 在滚动视图更新日志
        /// </summary>
        /// <param name="isError">是否是错误信息</param>
        /// <param name="log">要显示的日志</param>
        private void UpdateLog(bool isError, string log)
        {
            List<Transform> children = logContainer.GetComponentsInChildren<Transform>().Skip(1).ToList();
            // 控制最大日志条目数量100
            if (children.Count >= 100)
            {
                Destroy(children[0].gameObject);
            }

            // 判断当前滚动视图是否在最底端
            ScrollRect scrollRect = logContainer.GetComponentInParent<ScrollRect>();
            bool isAtEnd = scrollRect.verticalNormalizedPosition < 0.05;
            // 添加日志并强制刷新布局
            TextMeshProUGUI text = Instantiate(editLogTextPrefab, logContainer.transform);
            text.SetText(log);
            if (isError)
            {
                text.color = Color.red;
            }

            Canvas.ForceUpdateCanvases();
            // 在下一帧判断是否滚动到底部
            StartCoroutine(ForceScrollToBottom());

            IEnumerator ForceScrollToBottom()
            {
                // 等待渲染完成
                yield return new WaitForEndOfFrame();
                // 自动滚动到底部
                if (isAtEnd)
                {
                    scrollRect.verticalNormalizedPosition = 0;
                }
            }
        }

        /// <summary>
        /// 清除滚动视图中的日志
        /// </summary>
        private void ClearLogs()
        {
            List<Transform> children = logContainer.GetComponentsInChildren<Transform>().Skip(1).ToList();
            foreach (Transform child in children)
            {
                Destroy(child.gameObject);
            }
        }


        /// <summary>
        /// 编辑过程中，实时更新场景中的GS模型
        /// </summary>
        private IEnumerator UpdateGaussianOnScene()
        {
            Debug.Log("update gaussian");

            MainUIManager.ShowTip("Updating Scene...").GetComponent<TipsManager>().SetButtonInteractable(false);
            Status.UpdateIsInteractive(false);
            yield return null;
            try
            {
                UpdateGaussianAsset();
                MainUIManager.ShowTip("Scene updated");
            }
            catch (Exception e)
            {
                MainUIManager.ShowTip($"Update failed : {e}");
                Debug.LogError(e);
            }
            Status.UpdateIsInteractive(true);


            void UpdateGaussianAsset()
            {
                string plyFile =
                    Directory.GetFiles(Path.Join(Status.EditorFolder, Status.EditorPlyUpdateDir), "*.ply")[0];
                string outputDir = Path.Join(Status.SceneFileRootPlayer, Status.TemporaryDir);
                //创建临时新资产
                GaussianSplatAsset tempAsset;
                tempAsset = CreateAssetManager.CreateAsset(plyFile, outputDir,
                    Status.TemporaryPlyName,
                    gaussianSplatAsset.posFormat, gaussianSplatAsset.scaleFormat, gaussianSplatAsset.colorFormat,
                    gaussianSplatAsset.shFormat);
                //替换GSRenderer脚本中的资产引用
                gaussianSplats.GetComponent<GaussianSplatRenderer>().m_Asset = tempAsset;
            }
        }

        private void OnDestroy()
        {
            TryCancelEdit();
        }
    }
}

public class WslPythonRunner
{
    private Process _wslProcess;
    private TaskCompletionSource<bool> _tcs;

    /// <summary>
    /// 取消正在运行的进程
    /// </summary>
    public bool CancelCommand(out string result)
    {
        try
        {
            if (_wslProcess is { HasExited: false })
            {
                try
                {
                    _wslProcess?.Kill();
                    _tcs?.SetCanceled();
                    result = "Cancel command successfully";
                    return true;
                }

                catch (Exception e)
                {
                    result = $"Cancel command failed : {e}";
                    return false;
                }
            }
        }
        catch (Exception)
        {
            // ignored
        }

        result = "No running command or command has exited";
        return true;
    }

    /// <summary>
    /// 在wsl环境下运行指令
    /// </summary>
    /// <param name="command">指令</param>
    /// <param name="onFinished">执行完成的回调</param>
    /// <param name="dataReceived">有输出到来时的回调</param>
    /// <param name="errorReceived">有错误到来时的回调</param>
    /// <param name="logOutput">是否在控制台输出</param>
    public async Task ExecuteWslCommand(string command, Action<int> onFinished = null,
        Action<Object, DataReceivedEventArgs> dataReceived = null,
        Action<Object, DataReceivedEventArgs> errorReceived = null, bool logOutput = true)
    {
        StringBuilder outputs = new StringBuilder();
        StringBuilder errors = new StringBuilder();
        using (_wslProcess = new Process())
        {
            _tcs = new TaskCompletionSource<bool>();
            _wslProcess.StartInfo = new ProcessStartInfo
            {
                FileName = "wsl",
                Arguments =
                    $"-e bash -c \"source /etc/profile && source ~/.bashrc && conda init bash && {command}; exit\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.Unicode,
            };
            _wslProcess.EnableRaisingEvents = true;

            _wslProcess.OutputDataReceived += (s, e) =>
            {
                ReplaceIncompatibleCharacters(e.Data);
                AppendOutput(outputs, e.Data);
                Status.RunOnMainThread(() => dataReceived?.Invoke(s, e));
            };
            _wslProcess.ErrorDataReceived += (s, e) =>
            {
                AppendOutput(errors, e.Data);
                Status.RunOnMainThread(() => errorReceived?.Invoke(s, e));
            };
            _wslProcess.Exited += (_, _) =>
            {
                _tcs.TrySetResult(true);
                Status.RunOnMainThread(() =>
                {
                    // 判断是否运行出错
                    if (_wslProcess.ExitCode != 0 || errors.ToString().Contains("Traceback"))
                    {
                        onFinished?.Invoke(1);
                    }
                    else
                    {
                        onFinished?.Invoke(0);
                    }
                });
            };
            _wslProcess.Start();
            _wslProcess.BeginOutputReadLine();
            _wslProcess.BeginErrorReadLine();
            try
            {
                await _tcs.Task;
            }
            catch (TaskCanceledException)
            {
                Debug.Log($"Command '{command}' has been canceled.");
                Status.RunOnMainThread(() => onFinished?.Invoke(2));
            }
        }


        // 用于在控制台输出临时信息
        void AppendOutput(StringBuilder stringBuilder, string data)
        {
            data = ReplaceIncompatibleCharacters(data);
            if (string.IsNullOrEmpty(data)) return;
            stringBuilder.Append(data);
            if (logOutput)
            {
                Debug.Log($"[WSL] {data}");
            }
        }
    }

    /// <summary>
    /// 将windows路径转换为wsl路径
    /// </summary>
    /// <param name="windowsPath"></param>
    /// <returns></returns>
    public static string ConvertToWslPath(string windowsPath)
    {
        return windowsPath.Replace(@"\\wsl.localhost\Ubuntu", "").Replace("\\", "/")
            .Replace("C:", "/mnt/c")
            .Replace("D:", "/mnt/d")
            .Replace(" ", "\\ ");
    }

    /// <summary>
    /// 替换输出中Unity默认字体没有的字符
    /// </summary>
    /// <param name="input">原始输入字符串</param>
    /// <returns>替换后的字符串</returns>
    public static string ReplaceIncompatibleCharacters(string input)
    {
        // 正则表达式匹配 U+2588 至 U+2590 范围，排除允许的字符
        string pattern = @"[\u001B\u2588-\u2590]";
        string replaced = Regex.Replace(input, pattern, match =>
        {
            char c = match.Value[0];
            // 允许保留的字符：█（2588）、▌（258C）、▐（2590）
            if (c is '\u2588' or '\u258C' or '\u2590')
            {
                return c.ToString();
            }

            return c == '\u001B' ? "" : "█"; // 替换为可用字符（或自定义符号如 □）
        });
        return replaced;
    }
}