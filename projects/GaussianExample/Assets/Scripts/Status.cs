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
    private static Status _containerInstance;

    //灵敏度方位旋转
    private static float _rotateSensitivity;

    public static float rotateSensitivity =>
        isInteractive >= 0 ? _rotateSensitivity : 0f;

    //灵敏度方位移动
    private static float _translateSensitivity;

    public static float translateSensitivity =>
        isInteractive >= 0 ? _translateSensitivity : 0f;

    // 框选编辑位移/旋转/缩放灵敏度
    public const float EditTranslateSensitivity = 0.01f;
    public const float EditRotateSensitivity = 0.2f;
    public const float EditScaleSensitivity = 0.02f;


    // 基准/最小/最大FoV
    public const float BaseFoV = 30f;
    public const float CameraFovMin = 10f;

    public const float CameraFovMax = 120f;

    // 基准鼠标滚动量
    public const float MouseScrollBase = 120f;


    // 检查当前鼠标状态能否与场景交互
    public static int isInteractive { get; private set; } = 0;

    // 存储当前是否正在模拟
    public static bool IsSimulating = true;

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
            Debug.Log("changed");
            PlayModeChanged?.Invoke(_playMode, value);
        }
    }

    // 编辑模式改变时触发回调
    public static event EventHandler<SelectEditMode> SelectEditModeChanged;

    private static SelectEditMode _selectEditMode = SelectEditMode.None;

    // 编辑模式(平移/旋转/缩放)
    public static SelectEditMode selectEditMode
    {
        get => _selectEditMode;
        private set
        {
            if (_selectEditMode == value) return;
            _selectEditMode = value;
            SelectEditModeChanged?.Invoke(_selectEditMode, value);
        }
    }


    //场景文件保存路径
    public const string SceneFileName = "GaussianAssets";

    public static readonly string SceneFileRootEditor = Path.Join(Application.dataPath, "Resources", SceneFileName);
    public static string SceneFileRootPlayer;

    public const string SceneFileSuffixPlayer = ".dat";

    // 临时文件夹及临时点云文件
    public const string TemporaryDir = ".tmp";
    public const string TemporaryPlyName = "editing";

    // 存储可编辑文件的colmap相关文件的文件夹
    public const string ColmapDir = "colmap";

    // 存储编辑时需要用到的点云文件路径
    public const string AssetPlyFileLocalDir = "ply";

    public const string AssetPlyFileName = "point_clouds.ply";

    // 存储模拟时需要用到的网格文件路径
    public const string AssetMeshFileLocalDir = "mesh";
    public const string AssetMeshFileName = "mesh.txt";

    public const string ChunkFileSuffix = "_chk.bytes";
    public const string PositionFileSuffix = "_pos.bytes";
    public const string ScaleFileSuffix = "_oth.bytes";
    public const string ColorFileSuffix = "_col.bytes";
    public const string SHFileSuffix = "_shs.bytes";

    // GaussianEditor脚本及conda环境路径
    public const string CondaEnvName = "GaussianEditor";
    public const string EditorFolder = @"\\wsl.localhost\Ubuntu\home\Illli\GaussianEditor";
    public const string PythonScript = "launch.py";
    public const string AddConfigPath = "configs/add.yaml";
    public const string DeleteConfigPath = "configs/del-ctn.yaml";
    public const string EditCtnConfigPath = "configs/edit-ctn.yaml"; // for controlnet
    public const string EditN2NConfigPath = "configs/edit-n2n.yaml"; // for instructpix2pix
    public const string CacheDir = "cache";

    // 点云文件更新相关
    public const string PlyUpdateFlag = "[update]";
    public const string EditorPlyUpdateDir = "outputs/temp";


    // 刚切换场景时需要加载的GS资产
    public static readonly List<GaussianSplatAsset> GaussianSplatAssets = new();

    // 因为有一些UI操作只能在主线程进行，所以这里设置一个主线程任务队列
    private static readonly Queue<Action> ExecutionQueue = new();

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
        isInteractive += value ? 1 : -1;
    }

    public static void SwitchSelectMode()
    {
        playMode = PlayMode.Select;
    }

    public static void SwitchViewMode()
    {
        playMode = PlayMode.View;
    }

    public static void SwitchSimulateMode()
    {
        playMode = PlayMode.Simulate;
    }


    public static void EndSelectEdit()
    {
        selectEditMode = SelectEditMode.None;
    }

    public static void StartSelectTranslateEdit()
    {
        selectEditMode = SelectEditMode.Translate;
    }

    public static void StartSelectScaleEdit()
    {
        selectEditMode = SelectEditMode.Scale;
    }

    public static void StartSelectRotateEdit()
    {
        selectEditMode = SelectEditMode.Rotate;
    }

    /// <summary>
    ///  在主线程执行任务
    /// </summary>
    /// <param name="action">任务</param>
    public static void RunOnMainThread(Action action)
    {
        lock (ExecutionQueue)
        {
            ExecutionQueue.Enqueue(action);
        }
    }

    private void Awake()
    {
        if (!_containerInstance)
        {
            _containerInstance = this;
            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(gameObject);
        }

        SceneFileRootPlayer = Path.Join(Application.persistentDataPath, SceneFileName);
    }

    /// <summary>
    /// 退出编辑器时，清空GS资产列表，删除临时文件夹
    /// </summary>
    private void OnDestroy()
    {
        // 只有销毁单例的时候才执行摧毁操作
        if (this != _containerInstance) return;
        // 清空订阅事件
        if (PlayModeChanged != null)
        {
            foreach (Delegate d in PlayModeChanged.GetInvocationList())
            {
                PlayModeChanged -= (EventHandler<PlayMode>)d;
            }
        }

        if (SelectEditModeChanged != null)
        {
            foreach (Delegate d in SelectEditModeChanged.GetInvocationList())
            {
                SelectEditModeChanged -= (EventHandler<SelectEditMode>)d;
            }
        }

        GaussianSplatAssets.Clear();
        FileHelper.DeletePath(Path.Join(SceneFileRootPlayer, TemporaryDir), true, out _);
    }

    /// <summary>
    /// 逐项执行主线程任务队列中的任务
    /// </summary>
    public void Update()
    {
        lock (ExecutionQueue)
        {
            while (ExecutionQueue.Count > 0)
            {
                ExecutionQueue.Dequeue().Invoke();
            }
        }
    }
}

public enum PlayMode
{
    None,
    View,
    Select,
    Simulate
}

public enum SelectEditMode
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
    /// 获取已选中GS区域的本地中心坐标
    /// </summary>
    /// <param name="renderer">对应的GS对象</param>
    /// <returns></returns>
    public static Vector3 GetSelectionCenterLocal(GaussianSplatRenderer renderer)
    {
        Debug.Log(renderer.editSelectedBounds.extents);
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

        // 计算位移量
        float moveDistance = screenDelta.magnitude * Status.EditTranslateSensitivity;
        return moveDir * moveDistance;
    }

    /// <summary>
    /// 将固定深度上的屏幕坐标转换成世界空间坐标
    /// </summary>
    /// <param name="screenPos">屏幕坐标</param>
    /// <param name="cam">相机</param>
    /// <param name="referenceDepth">固定深度</param>
    /// <returns>世界空间坐标</returns>
    public static Vector3 GetMouseWorldPos(Vector2 screenPos, Camera cam, float referenceDepth)
    {
        // 将屏幕坐标转换为NDC坐标
        Vector3 ndc = new Vector3(
            2.0f * screenPos.x / Screen.width - 1.0f,
            2.0f * screenPos.y / Screen.height - 1.0f,
            referenceDepth // 使用固定深度
        );

        // 反投影到世界空间
        Matrix4x4 viewProjMatrix = cam.projectionMatrix * cam.worldToCameraMatrix;
        Matrix4x4 invViewProj = viewProjMatrix.inverse;
        Vector4 worldPos = invViewProj.MultiplyPoint(ndc);

        return worldPos / worldPos.w;
    }

    /// <summary>
    /// 将屏幕空间是速度转换为世界空间
    /// </summary>
    /// <param name="cam">当前相机</param>
    /// <param name="screenVel">屏幕空间速度</param>
    /// <param name="depth">固定深度</param>
    /// <returns>世界空间速度</returns>
    public static Vector3 ScreenToWorldVelocity(Camera cam, Vector2 screenVel, float depth)
    {
        // 获取相机参数
        float fov = cam.fieldOfView;
        float tanFov = Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);

        // 计算每像素对应的世界单位（在参考深度平面）
        float pixelsPerUnit = Screen.height / (2.0f * depth * tanFov);
        float worldSpeedScale = 1.0f / pixelsPerUnit;

        // 转换到相机坐标系
        Vector3 camSpaceVel = new Vector3(
            screenVel.x * worldSpeedScale,
            screenVel.y * worldSpeedScale,
            0
        );

        // 转换到世界坐标系
        return cam.transform.TransformVector(camSpaceVel);
    }

    /// <summary>
    /// 从四元数生成3x3旋转矩阵
    /// </summary>
    /// <param name="q">四元数</param>
    /// <returns>3x3旋转矩阵</returns>
    public static Matrix4x4 GenerateRotationMatrix(Quaternion q)
    {
        float x = q.x, y = q.y, z = q.z, w = q.w;
        Matrix4x4 matrix = new Matrix4x4
        {
            m00 = 1 - 2 * y * y - 2 * z * z,
            m01 = 2 * x * y - 2 * z * w,
            m02 = 2 * x * z + 2 * y * w,
            m10 = 2 * x * y + 2 * z * w,
            m11 = 1 - 2 * x * x - 2 * z * z,
            m12 = 2 * y * z - 2 * x * w,
            m20 = 2 * x * z - 2 * y * w,
            m21 = 2 * y * z + 2 * x * w,
            m22 = 1 - 2 * x * x - 2 * y * y
        };

        return matrix;
    }
}