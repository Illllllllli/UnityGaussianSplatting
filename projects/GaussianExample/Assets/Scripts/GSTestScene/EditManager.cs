using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Object = System.Object;

namespace GSTestScene
{
    public class EditManager : MonoBehaviour
    {
        public GameObject editPanel;
        public Button closeButton;
        public Button editButton;
        private void Start()
        {
            closeButton.onClick.AddListener(()=>editPanel.SetActive(false));
            editButton.onClick.AddListener(StartEdit);
        }

        public void HandleEditClick()
        {
            editPanel.SetActive(true);
        }

        private async void StartEdit()
        {
            await WslPythonRunner.ExecuteWslCommand($"source activate {Status.CondaEnvName} && python -V", null);
        }
        
        
    }
}

public class WslPythonRunner
{
    private readonly ConcurrentQueue<string> _outputQueue = new ConcurrentQueue<string>();
    

    /// <summary>
    /// 通过环境名称获取Conda环境的python路径
    /// </summary>
    /// <param name="envName">环境名称</param>
    /// <returns>Python路径</returns>
    // public static async Task<string> GetCondaPythonPath(string envName)
    // {
    //     string output = await ExecuteWslCommand($"conda env list", null);
    //     var match = Regex.Match(output, $@"^{envName}\s+\S*(\/.*)$", RegexOptions.Multiline);
    //
    //     return match.Success ? match.Groups[1].Value.Trim() + "/bin/python" : null;
    //     // return match.Success.ToString();
    // }

    /// <summary>
    /// 在wsl环境下运行指令
    /// </summary>
    /// <param name="command">指令</param>
    /// <param name="onFinished">执行完成的回调</param>
    /// <param name="logOutput"></param>
    public static async Task<string> ExecuteWslCommand(string command, Action<bool> onFinished,bool logOutput = true)
    {
        var output = new StringBuilder();
        var tcs = new TaskCompletionSource<bool>();

        using (Process proc = new Process())
        {
            proc.StartInfo = new ProcessStartInfo
            {
                FileName = "wsl",
                Arguments = $"-e bash -c \"source /etc/profile && source ~/.bashrc && conda init bash && {command}; exit\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
            };
            proc.EnableRaisingEvents = true;
            proc.OutputDataReceived += (s, e) => AppendOutput(e.Data);
            proc.ErrorDataReceived += (s, e) => AppendOutput(e.Data);
            proc.Exited += (s, e) => tcs.TrySetResult(true);

            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            await tcs.Task;
        }
        
        onFinished?.Invoke(true);
        return output.ToString();
        
        // 用于在控制台输出临时信息
        void AppendOutput(string data)
        {
            if (string.IsNullOrEmpty(data)) return;
            output.AppendLine(data);
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
    

}