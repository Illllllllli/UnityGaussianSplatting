using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GaussianSplatting.Editor;
using GaussianSplatting.Runtime;
using GSTestScene.Simulation;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace GSTestScene.Simulation
{
    /// <summary>
    /// 存储所有属性和外部输入相关操作
    /// </summary>
    public partial class GaussianSimulator : MonoBehaviour
    {
        public readonly List<GameObject> GaussianSplats = new();
        private readonly List<GaussianSplatAsset> _savedAssets = new(); // 用于停止模拟模式时恢复之前已经编辑过的资产

        private readonly List<GaussianObject> _gaussianObjects = new();

        // 针对每个GS物体的物理材质（虽然只有一个）
        private readonly List<MaterialProperty> _materialProperties=new();

        [Header("Main Camera")]
        // 主相机
        public Camera mainCamera;


        [Header("Simulation Compute Shader")]
        // 模拟用计算着色器主体
        public ComputeShader simulateShader;

        [Header("Simulation Parameters")]
        // 物理模拟的时间步长
        public float frameDt = 7.5e-3f;

        // 子时间步长（Substep），用于XPBD约束求解的微步积分
        public float dt = 1e-3f + 1e-5f;

        // 重力加速度，控制物体在Y轴方向（或Z轴，根据isZUp）的下落速度
        public float gravity = -4.0f;

        // 阻尼系数，用于模拟能量耗散（空气阻力）
        public float dampingCoeffient = 5.0f;

        // XPBD（Extended Position-Based Dynamics）求解器的迭代次数
        public int xpbdRestIter = 25;

        // 碰撞刚度系数
        public float collisionStiffness = 0.1f;

        // 最小碰撞距离，避免穿透（Penetration）的阈值
        public float collisionMinimalDist = 5e-3f;

        // 碰撞检测的迭代间隔次数
        public float collisionDetectionIterInterval = 100;

        // 切换全局坐标系的上方向
        public bool isZUp = false;


        // 边界标志位。但不知道具体干什么用
        public int boundary = 0;

        [Header("Simulate Info")]
        // 初始化时间
        public double initializeMilliSeconds;

        // 计算模拟用掉的时间
        public double totalSimulateMilliSeconds;

        // 统计模拟的总计步数
        public ulong totalSteps;


        // 下面是内部参数

        private const float ThreadCount = 256; // 线程数量（常数,但是为了配合CeilToInt改成浮点数）
        private const int PartialAABBCount = 64; // 部分包围盒的固定数量
        private const int MaxCollisionPairs = 1000000; // 最大碰撞对数量
        private int _totalGsCount; // GS总数

        // 鼠标控制相关参数
        private bool isMousePressed => GetComponent<UserActionListener>().isMousePressed; // 当前鼠标是否按下
        private const float ControllerRadius = 1000f; // 鼠标能控制的顶点半径范围
        private const float ReferenceDepth = 5.0f; // 假设交互固定发生在相机前方5米平面
        private float _lastMouseTime; // 上一次鼠标时间
        private Vector2 _lastMousePos = Vector2.positiveInfinity; // 上一次鼠标位置
        private Vector2 _currentMousePos = Vector2.positiveInfinity; // 本次鼠标位置
        private Vector3 _currentMousePosWorld = Vector3.positiveInfinity; // 本次鼠标位置的世界坐标
        private Vector3 _mouseVelocity = Vector3.positiveInfinity; // 速度
        private readonly Vector3 _mouseAngularVelocity = Vector3.zero; // 角速度，但是感觉没有办法正确检测，因此先固定为0


        // 网格相关基本参数
        // 顶点/边/面/四面体总数
        private int _totalVerticesCount;
        private int _totalEdgesCount;
        private int _totalFacesCount;
        private int _totalCellsCount;

        // 一些网格相关的缓冲区
        // todo:还有一部分没写完
        // 网格数据
        private ComputeBuffer _vertXBuffer; //todo:X和x可能是所谓两层嵌入的不同顶点？
        private ComputeBuffer _vertxBuffer;
        private ComputeBuffer _vertGroupBuffer; //顶点组索引缓冲区

        // 网格索引
        private ComputeBuffer _edgeIndicesBuffer; //边索引缓冲区
        private ComputeBuffer _faceIndicesBuffer; //面索引缓冲区
        private ComputeBuffer _cellIndicesBuffer; //四面体索引缓冲区

        // FEM-PBD数据
        private ComputeBuffer _vertVelocityBuffer; //顶点速度缓冲区
        private ComputeBuffer _vertForceBuffer; //顶点受力缓冲区
        private ComputeBuffer _vertMassBuffer; //顶点质量缓冲区
        private ComputeBuffer _vertInvMassBuffer; //顶点逆质量缓冲区
        private ComputeBuffer _vertNewXBuffer; //顶点新位置缓冲区
        private ComputeBuffer _vertDeltaPosBuffer; //顶点位移增量缓冲区
        private ComputeBuffer _vertSelectedIndicesBuffer; //选中顶点的索引缓冲区
        private ComputeBuffer _rigidVertGroupBuffer; //刚体顶点组索引缓冲区
        private ComputeBuffer _cellMultiplierBuffer; //四面体形变乘数缓冲区
        private ComputeBuffer _cellDsInvBuffer; //四面体逆变形梯度矩阵(3x3)缓冲区
        private ComputeBuffer _cellVolumeInitBuffer; //四面体初始体积缓冲区
        private ComputeBuffer _cellDensityBuffer; //四面体密度缓冲区
        private ComputeBuffer _cellMuBuffer; //四面体剪切模量缓冲区
        private ComputeBuffer _cellLambdaBuffer; //四面体拉梅常数缓冲区

        // 刚体模拟参数
        private ComputeBuffer _rigidMassBuffer; //刚体质量缓冲区
        private ComputeBuffer _rigidMassCenterInitBuffer; //刚体初始质心位置缓冲区
        private ComputeBuffer _rigidMassCenterBuffer; //刚体质心位置缓冲区
        private ComputeBuffer _rigidAngleVelocityMatrixBuffer; //刚体角速度矩阵(3x3)缓冲区
        private ComputeBuffer _rigidRotationMatrixBuffer; //刚体旋转矩阵(3x3)缓冲区

        // 碰撞检测参数
        private ComputeBuffer _triangleAabbsBuffer; //排序前的三角面的轴对齐包围盒缓冲区
        private ComputeBuffer _sortedTriangleAabbsBuffer; //排序后的三角面轴对齐包围盒缓冲区
        private ComputeBuffer _partialAabbBuffer; //部分轴对齐包围盒缓冲区
        private NativeArray<LbvhAABBBoundingBox> _partialAabbData; //部分轴对齐包围盒数据（CPU端）
        private ComputeBuffer _mortonCodeBuffer; //排序前的莫顿编码缓冲区
        private ComputeBuffer _sortedMortonCodeBuffer; //排序后的莫顿编码缓冲区
        private ComputeBuffer _indicesBuffer; //排序前的面片索引缓冲区
        private ComputeBuffer _sortedIndicesBuffer; //排序后的面片索引缓冲区
        private ComputeBuffer _faceFlagsBuffer; //标记每个面片的状态的缓冲区
        private ComputeBuffer _lbvhAabbsBuffer; //LBVH树每个树节点的包围盒缓冲区
        private ComputeBuffer _lbvhNodesBuffer; //LBVH树节点的父子关系缓冲区
        private ComputeBuffer _lbvhSortBuffer; //LBVH的中间排序结果缓冲区

        //碰撞对管理
        private ComputeBuffer _collisionPairsBuffer; //粗略碰撞对缓冲区
        private ComputeBuffer _exactCollisionPairsBuffer; //精确碰撞对缓冲区
        private ComputeBuffer _totalPairsBuffer; //粗略碰撞对数量计数器缓冲区
        private ComputeBuffer _totalExactPairsBuffer; //精确碰撞对数量计数器缓冲区

        // 插值用数据
        private ComputeBuffer _covBuffer; //协方差矩阵缓冲区
        private ComputeBuffer _localTetXBuffer; //本地四面体顶点坐标缓冲区
        private ComputeBuffer _localTetWBuffer; //本地四面体顶点坐标权重缓冲区
        private ComputeBuffer _globalTetIdxBuffer; //全局四面体索引缓冲区
        private ComputeBuffer _globalTetWBuffer; //全局四面体权重缓冲区

        // 着色器属性ID
        private readonly int _controllerPositionId = Shader.PropertyToID("controller_position");
        private readonly int _controllerVelocityId = Shader.PropertyToID("controller_velocity");
        private readonly int _controllerAngleVelocity = Shader.PropertyToID("controller_angle_velocity");
        private readonly int _controllerRadiusId = Shader.PropertyToID("controller_radius");
        private readonly int _vertXBufferId = Shader.PropertyToID("vertices_X");
        private readonly int _vertxBufferId = Shader.PropertyToID("vertices_x");
        private readonly int _vertSelectedIndicesBufferId = Shader.PropertyToID("selected_vertices_ids");

        // 内核函数的索引
        private int selectVerticesKernel => simulateShader.FindKernel("select_vertices");

        private int cleanSelectionKernel => simulateShader.FindKernel("clean_selected_vertices");

        // 其他默认参数
        private const int ShDegree = 3; //sh阶数
        private const bool FastCulling = true; // 快速剔除(?)
        private const float ScalingModifier = 1f; // 缩放修饰符

        /// <summary>
        /// 鼠标按下时的回调
        /// </summary>
        public void MouseDown()
        {
            // 记录时间和位置
            _lastMouseTime = Time.time;
            _lastMousePos = Mouse.current.position.ReadValue();
        }

        /// <summary>
        /// 鼠标松开时的回调
        /// </summary>
        public void MouseUp()
        {
            // 清空所有位置信息
            _lastMousePos = Vector2.positiveInfinity;
            _currentMousePos = Vector2.positiveInfinity;
            _mouseVelocity = Vector3.positiveInfinity;
            _lastMouseTime = 0;
        }

        /// <summary>
        /// 更新鼠标的速度
        /// </summary>
        /// <param name="time">触发时间</param>
        /// <param name="cam">当前相机</param>
        private void UpdateMouseVelocity(float time, Camera cam)
        {
            if (_lastMousePos.Equals(Vector2.positiveInfinity)) return;
            _currentMousePos = Mouse.current.position.ReadValue();
            _currentMousePosWorld = GsTools.GetMouseWorldPos(_currentMousePos, mainCamera, ReferenceDepth);
            //计算屏幕空间速度
            Vector2 screenDelta = _currentMousePos - _lastMousePos;
            float deltaTime = time - _lastMouseTime;
            Vector2 screenVelocity = screenDelta / deltaTime;
            _mouseVelocity = GsTools.ScreenToWorldVelocity(cam, screenVelocity, ReferenceDepth);
            // 更新上次屏幕位置和触发时间为本次统计
            _lastMousePos = _currentMousePos;
            _lastMouseTime = time;
        }


        /// <summary>
        /// 清除所有缓冲区
        /// </summary>
        private void Dispose()
        {
            // todo:补全所有缓冲区
            _vertxBuffer?.Dispose();
            _vertXBuffer?.Dispose();
            _vertGroupBuffer?.Dispose();

            _edgeIndicesBuffer?.Dispose();
            _faceIndicesBuffer?.Dispose();
            _cellIndicesBuffer?.Dispose();

            _vertVelocityBuffer?.Dispose();
            _vertForceBuffer?.Dispose();
            _vertMassBuffer?.Dispose();
            _vertInvMassBuffer?.Dispose();
            _vertNewXBuffer?.Dispose();
            _vertDeltaPosBuffer?.Dispose();
            _vertSelectedIndicesBuffer?.Dispose();
            _rigidVertGroupBuffer?.Dispose();
            _cellMultiplierBuffer?.Dispose();
            _cellDsInvBuffer?.Dispose();
            _cellVolumeInitBuffer?.Dispose();
            _cellDensityBuffer?.Dispose();
            _cellMuBuffer?.Dispose();
            _cellLambdaBuffer?.Dispose();

            _rigidMassBuffer?.Dispose();
            _rigidMassCenterInitBuffer?.Dispose();
            _rigidMassCenterBuffer?.Dispose();
            _rigidAngleVelocityMatrixBuffer?.Dispose();
            _rigidRotationMatrixBuffer?.Dispose();

            _triangleAabbsBuffer?.Dispose();
            _sortedTriangleAabbsBuffer?.Dispose();
            _partialAabbBuffer?.Dispose();
            _partialAabbData.Dispose();
            _mortonCodeBuffer?.Dispose();
            _sortedMortonCodeBuffer?.Dispose();
            _indicesBuffer?.Dispose();
            _sortedIndicesBuffer?.Dispose();
            _faceFlagsBuffer?.Dispose();
            _lbvhAabbsBuffer?.Dispose();
            _lbvhNodesBuffer?.Dispose();
            _lbvhSortBuffer?.Dispose();

            _collisionPairsBuffer?.Dispose();
            _exactCollisionPairsBuffer?.Dispose();
            _totalPairsBuffer?.Dispose();
            _totalExactPairsBuffer?.Dispose();

            _covBuffer?.Dispose();
            _localTetXBuffer?.Dispose();
            _localTetWBuffer?.Dispose();
            _globalTetIdxBuffer?.Dispose();
            _globalTetWBuffer?.Dispose();
        }

        /// <summary>
        /// 开启/继续物理模拟
        /// </summary>
        /// <param name="init">是否需要初始化</param>
        public void StartSimulate(bool init)
        {
            Debug.Log("start simulate");
            if (init)
            {
                using TimerUtil timerUtil = new TimerUtil("Simulation Initialize");
                // 保存已经编辑过的GS资产
                _savedAssets.Clear();
                _savedAssets.AddRange(GaussianSplats.Select(o => o.GetComponent<GaussianSplatRenderer>().m_Asset));
                // 替换新的GS资产到gaussianSplats对象中
                foreach (GameObject gaussianSplat in GaussianSplats)
                {
                    GaussianSplatAsset asset = gaussianSplat.GetComponent<GaussianSplatRenderer>().asset;
                    GaussianSplatAsset newAsset = ScriptableObject.CreateInstance<GaussianSplatAsset>();
                    newAsset.Initialize(asset.splatCount, asset.posFormat, asset.scaleFormat, asset.colorFormat,
                        asset.shFormat, asset.boundsMin, asset.boundsMax, asset.cameras);
                    newAsset.SetAssetFiles(asset.assetDataPath, asset.enableEdit, asset.enableSimulate,
                        asset.chunkData != null ? new ByteAsset(asset.chunkData.GetData<byte>().ToArray()) : null,
                        new ByteAsset(asset.posData.GetData<byte>().ToArray()),
                        new ByteAsset(asset.otherData.GetData<byte>().ToArray()),
                        new ByteAsset(asset.colorData.GetData<byte>().ToArray()),
                        new ByteAsset(asset.shData.GetData<byte>().ToArray()));
                    newAsset.SetDataHash(asset.dataHash);
                    // 替换资产
                    gaussianSplat.GetComponent<GaussianSplatRenderer>().m_Asset = newAsset;
                }

                // 初始化每个物体的参数和公共参数
                GameObject tip = MainUIManager.ShowTip("Preparing for simulation...", false);
                bool initialize = InitializeSimulationParams();
                Destroy(tip);
                if (!initialize) return;
                // 统计初始化时间
                initializeMilliSeconds = timerUtil.GetDeltaTime();
            }

            Status.IsSimulating = true;
        }

        /// <summary>
        /// 暂停物理模拟
        /// </summary>
        public void PauseSimulate()
        {
            Status.IsSimulating = false;
        }

        /// <summary>
        /// 重置物理模拟状态（为初始状态）
        /// </summary>
        public void ResetSimulate()
        {
            // 从已保存的资产恢复到初始状态
            _savedAssets.Select((asset, index) =>
                GaussianSplats[index].GetComponent<GaussianSplatRenderer>().m_Asset = asset);
            Status.IsSimulating = false;
            // 清空统计量
            totalSteps = 0;
            initializeMilliSeconds = 0;
            totalSimulateMilliSeconds = 0;
            _totalGsCount = 0;
            _totalVerticesCount = 0;
            _totalEdgesCount = 0;
            _totalFacesCount = 0;
            _totalCellsCount = 0;
            // 释放公共缓冲区
            Dispose();
            // 释放子物体的缓冲区
            foreach (GaussianObject gaussianObject in _gaussianObjects)
            {
                gaussianObject.Dispose();
            }

            // 清除列表
            _gaussianObjects?.Clear();
            _materialProperties?.Clear();
        }


        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {
            if (Status.IsSimulating)
            {
                using TimerUtil util = new TimerUtil("Simulate");
                //拖拽过程中，更新鼠标速度
                if (isMousePressed)
                {
                    UpdateMouseVelocity(Time.time, mainCamera);
                    Debug.Log($"velocity:{_mouseVelocity}");
                }


                //根据鼠标的位置选择范围内的顶点，更新数据
                UpdateSelectVertices();

                //todo: 剩余步骤

                // 更新总计模拟时间
                totalSimulateMilliSeconds += util.GetDeltaTime();
                // 更新总步数
                totalSteps++;

                // Debug.Log($"total simulate time:{totalSimulateMilliSeconds}ms");
            }
        }

        /// <summary>
        /// 模拟器被销毁时，释放所有缓冲区
        /// </summary>
        private void OnDestroy()
        {
            // 释放公共缓冲区
            Dispose();

            // 释放所有子物体的资源
            foreach (GaussianObject gaussianObject in _gaussianObjects)
            {
                gaussianObject.Dispose();
            }
        }
    }
}



/// <summary>
/// 统计函数运行时间的工具类。
/// 定义时放入计时指针，退出上下文自动增加
/// </summary>
internal class TimerUtil : IDisposable
{
    private readonly string _tag;
    private readonly Stopwatch _timer;

    public TimerUtil(string tag)
    {
        _tag = tag;
        _timer = Stopwatch.StartNew();
    }

    /// <summary>
    /// 获取本轮更新消耗的时间（毫秒）
    /// </summary>
    /// <returns>消耗的毫秒数</returns>
    public double GetDeltaTime()
    {
        return _timer.ElapsedMilliseconds;
    }

    public void Dispose()
    {
        _timer.Stop();
        // Debug.Log($"delta {_tag} time : {_timer.ElapsedMilliseconds}ms");
    }
}