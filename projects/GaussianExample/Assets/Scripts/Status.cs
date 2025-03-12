using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GaussianSplatting.Runtime;
using StartScene;
using UnityEngine;
using UnityEngine.EventSystems;


public class Status : MonoBehaviour
{
    //灵敏度方位旋转
    private static float _rotateSensitivity;

    public static float rotateSensitivity =>
        isInteractive ? _rotateSensitivity : 0f;

    //灵敏度方位移动
    private static float _translateSensitivity;

    public static float translateSensitivity =>
        isInteractive ? _translateSensitivity : 0f;

    // 框选编辑位移灵敏度
    public static float EditTranslateSensitivity = 0.01f;
    
    
    // 基准/最小/最大FoV
    public const float BaseFoV = 30f;
    public const float CameraFovMin = 10f;
    public const float CameraFovMax = 120f;
    // 基准鼠标滚动量
    public const float MouseScrollBase = 120f;
    

    // 检查当前鼠标状态能否与场景交互
    public static bool isInteractive { get; private set; } = true;

    //查看器模式改变时触发回调
    public static event EventHandler<PlayMode> PlayModeChanged;

    private static PlayMode _playMode = PlayMode.None;

    //查看器模式（浏览/选择/编辑）
    public static PlayMode playMode
    {
        get => _playMode;
        private set
        {
            if (_playMode == value) return;
            _playMode = value;
            PlayModeChanged?.Invoke(null, value);
        }
    }

    // note: 当前只有平移功能
    // 编辑模式改变时触发回调
    public static event EventHandler<EditMode> EditModeChanged;

    private static EditMode _editMode = EditMode.None;

    // 编辑模式(平移/旋转/缩放)
    public static EditMode editMode
    {
        get => _editMode;
        private set
        {
            if (_editMode == value) return;
            _editMode = value;
            EditModeChanged?.Invoke(_editMode, value);
        }
    }


    //场景文件保存路径
    public const string SceneFileName = "GaussianAssets";

    public static readonly string SceneFileRootEditor = Path.Join(Application.dataPath, "Resources", SceneFileName);
    public static string SceneFileRootPlayer;
    public const string SceneFileSuffixPlayer = ".dat";
    // 临时文件夹
    public const string TemporaryDir = ".tmp";
    // 存储可编辑文件的colmap相关文件的文件夹
    public const string ColmapDirName = "colmap";

    public const string ChunkFileSuffix = "_chk.bytes";
    public const string PositionFileSuffix = "_pos.bytes";
    public const string ScaleFileSuffix = "_oth.bytes";
    public const string ColorFileSuffix = "_col.bytes";
    public const string SHFileSuffix = "_shs.bytes";
    
    // GaussianEditor脚本及conda环境路径
    public const string CondaEnvName = "GaussianEditor";
    public const string PythonScript = @"\\wsl.localhost\Ubuntu\home\Illli\GaussianEditor\launch.py";

    // 刚切换场景时需要加载的GS资产
    public static readonly List<GaussianSplatAsset> GaussianSplatAssets = new();


    public static void UpdateRotateSensitivity(float value)
    {
        _rotateSensitivity = value;
    }

    public static void UpdateTranslateSensitivity(float value)
    {
        _translateSensitivity = value;
    }

    public static void UpdateIsInteractive(bool value)
    {
        isInteractive = value;
    }

    public static void SwitchSelectMode()
    {
        playMode = PlayMode.Select;
        Debug.Log("select");
    }

    public static void SwitchViewMode()
    {
        playMode = PlayMode.View;
        Debug.Log("view");
    }
    

    public static void EndSelectEdit()
    {
        editMode = EditMode.None;
    }

    public static void StartSelectTranslateEdit()
    {
        editMode = EditMode.Translate;
    }

    private void Awake()
    {
        SceneFileRootPlayer = Path.Join(Application.persistentDataPath, SceneFileName);
        DontDestroyOnLoad(this);
    }

    /// <summary>
    /// 退出编辑器时，清空GS资产列表，删除临时文件夹
    /// </summary>
    private void OnDestroy()
    {
        GaussianSplatAssets.Clear();
        FileHelper.DeletePath(Path.Join(SceneFileRootPlayer, TemporaryDir),true,out _);
    }
}

public enum PlayMode
{
    None,
    View,
    Select,
}

public enum EditMode
{
    None,
    Translate,
    Rotate,
    Scale
}

// 工具函数类
internal static class GsTools
{
    /// <summary>
    /// 根据两个对角坐标创建矩形
    /// </summary>
    /// <param name="from">左上角坐标</param>
    /// <param name="to">右下角坐标</param>
    /// <returns>矩形对象</returns>
    public static Rect FromToRect(Vector2 from, Vector2 to)
    {
        if (from.x > to.x)
            (from.x, to.x) = (to.x, from.x);
        if (from.y > to.y)
            (from.y, to.y) = (to.y, from.y);
        return new Rect(from.x, from.y, to.x - from.x, to.y - from.y);
    }

    /// <summary>
    /// 判断鼠标的位置是否在UI上
    /// </summary>
    /// <returns>鼠标是否在UI上</returns>
    public static bool IsPointerOverUIObject()
    {
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IPHONE)
        eventDataCurrentPosition.position = Input.GetTouch(0).position;
#else
        eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
#endif
        List<RaycastResult> results = new List<RaycastResult>();
        if (EventSystem.current)
        {
            EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        }

        return results.Any(t => t.gameObject.layer == LayerMask.NameToLayer("UI"));
    }

    /// <summary>
    /// 获取已选中GS区域的中心坐标
    /// </summary>
    /// <param name="renderer">对应的GS对象</param>
    /// <returns></returns>
    public static Vector3 GetSelectionCenterLocal(GaussianSplatRenderer renderer)
    {
        if (renderer && renderer.editSelectedSplats > 0)
            return renderer.editSelectedBounds.center;
        return Vector3.zero;
    }

    /// <summary>
    /// 根据相机视角，从鼠标位移得到世界坐标位移
    /// </summary>
    /// <param name="screenDelta">屏幕空间（鼠标）位移</param>
    /// <param name="camera">相机</param>
    /// <returns>世界坐标位移量</returns>
    public static Vector3 ScreenDeltaToWorldDelta(Vector2 screenDelta, Camera camera)
    {
        // 根据摄像机方向计算位移方向
        Vector3 camRight = camera.transform.right;
        Vector3 camUp = camera.transform.up;
        Vector3 moveDir = (camRight * screenDelta.x + camUp * screenDelta.y).normalized;

        // 计算位移量（可根据正交/透视模式调整）
        float moveDistance = screenDelta.magnitude * Status.EditTranslateSensitivity;
        return moveDir * moveDistance;
    }
}