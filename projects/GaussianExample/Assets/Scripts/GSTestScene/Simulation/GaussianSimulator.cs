using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GaussianSplatting.Runtime;
using StartScene;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;

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
        private readonly List<MaterialProperty> _materialProperties = new();

        [Header("Main Camera")]
        // 主相机
        public Camera mainCamera;


        [Header("Simulation Compute Shaders")]
        // 模拟用计算着色器主体
        public ComputeShader simulateShader;

        // 基数排序用着色器
        public ComputeShader radixShader;

        // 缓冲区复制用着色器
        public ComputeShader copyShader;

        [Header("Simulation Parameters")]
        // 物理模拟的时间步长
        public float frameDt = 7.5e-3f;

        // 子时间步长（Substep），用于XPBD约束求解的微步积分
        public float dt = 1e-3f + 1e-5f;

        // 重力加速度，控制物体在Y轴方向（或Z轴，根据isZUp）的下落速度
        public float gravity = -4.0f;

        // 阻尼系数，用于模拟能量耗散（空气阻力）
        public float dampingCoefficient = 5.0f;

        // XPBD（Extended Position-Based Dynamics）求解器的迭代次数
        public int xpbdRestIter = 25;

        // 碰撞刚度系数
        public float collisionStiffness = 0.1f;

        // 最小碰撞距离，避免穿透（Penetration）的阈值
        public float collisionMinimalDist = 5e-3f;

        // 碰撞检测的迭代间隔次数
        public float collisionDetectionIterInterval = 100;

        // 切换全局坐标系的上方向
        public int isZUp = 1;


        // 边界标志位。但不知道具体干什么用
        public int boundary = 0;

        [Header("Simulate Info")]
        // 初始化时间
        public double initializeMilliSeconds;

        // 计算模拟用掉的时间
        public double totalSimulateMilliSeconds;

        // 碰撞检测用掉的时间
        public double totalCollisionDetectionMilliSeconds;

        // XPBD用掉的时间
        public double totalXpbdMilliSeconds;

        // FEM求解用掉的时间
        public double totalFemSolveMilliSeconds;

        // 求解碰撞约束用掉的时间
        public double totalCollisionSolveMilliSeconds;

        // 重新将GS嵌入网格所需的时间
        public double totalEmbededMilliSeconds;

        // 统计模拟的总计步数
        public ulong totalSteps;


        // 下面是内部参数
        private CommandBuffer _commandBuffer; // 命令缓冲区
        private ComputeBuffer _lastBuffer; // 标记commandBuffer中最后一个编辑的ComputeBuffer

        private const float Sqrt3 = 1.73205080757f;
        private const int InnerBatchCount = 64; // BurstJob线程数量


        private const int SimulateBlockSize = 256; // 线程数量（常数,但是为了配合CeilToInt改成浮点数）
        private const int CullingBlockSize = 128; // 面剔除线程数量
        private const int RadixSortBlockSize = 128; // RadixSort所用线程块大小
        private const int CopyBlockSize = 1024; // 复制缓冲区所用线程块大小
        private const int BlellochLogNumBanks = 5; // GPU共享内存的划分数的对数
        private const int PartialAABBCount = 64; // 部分包围盒的固定数量
        private const int MaxCollisionPairs = 1000000; // 最大碰撞对数量
        private int _totalGsCount; // GS总数
        private int _lbvhSortBufferSize; // LBVH的中间排序结果缓冲区的大小。经过基数排序后才能确定

        // 鼠标控制相关参数
        private bool isMousePressed => GetComponent<UserActionListener>().isMousePressed; // 当前鼠标是否按下
        private const float ControllerRadius = 1f; // 鼠标能控制的顶点半径范围
        private const float ReferenceDepth = 5.0f; // 假设交互固定发生在相机前方5米平面
        private float _lastMouseTime; // 上一次鼠标时间
        private Vector2 _lastControllerScreenPos = Vector2.positiveInfinity; // 上一次鼠标位置
        private Vector2 _controllerScreenPos = Vector2.positiveInfinity; // 本次鼠标位置
        private Vector3 _controllerPosition = Vector3.positiveInfinity; // 本次鼠标位置的世界坐标
        private Vector3 _controllerVelocity = Vector3.positiveInfinity; // 速度
        private readonly Vector3 _controllerAngularVelocity = Vector3.zero; // 角速度，但是感觉没有办法正确检测，因此先固定为0


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
        // 注意：这里从原版的double改成了float
        private ComputeBuffer _rigidMassBuffer; //刚体质量缓冲区
        private ComputeBuffer _rigidMassCenterInitBuffer; //刚体初始质心位置缓冲区
        private ComputeBuffer _rigidMassCenterBuffer; //刚体质心位置缓冲区
        private ComputeBuffer _rigidAngleVelocityMatrixBuffer; //刚体角速度矩阵(3x3)缓冲区
        private ComputeBuffer _rigidRotationMatrixBuffer; //刚体旋转矩阵(3x3)缓冲区

        // 碰撞检测参数
        private LbvhAABBBoundingBox _aabbGlobal; // 全局的轴对齐包围盒
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
        private int[] _totalPairsCount = new int[1]; //粗略碰撞对数量
        private ComputeBuffer _totalExactPairsBuffer; //精确碰撞对数量计数器缓冲区
        private int[] _totalExactPairsCount = new int[1]; //精确碰撞对数量

        // 插值用数据
        private ComputeBuffer _covBuffer; //协方差矩阵缓冲区
        private ComputeBuffer _localTetXBuffer; //本地四面体顶点坐标缓冲区
        private ComputeBuffer _localTetWBuffer; //本地四面体顶点坐标权重缓冲区
        private ComputeBuffer _globalTetIdxBuffer; //全局四面体索引缓冲区
        private ComputeBuffer _globalTetWBuffer; //全局四面体权重缓冲区

        // GS相关缓冲区。网格插值用,数据类型保留为原始的uint
        private ComputeBuffer _gsPosBuffer; //GS位置缓冲区
        private ComputeBuffer _gsOtherBuffer; //GS缩放和旋转缓冲区

        // 着色器属性ID
        // 通用属性
        private readonly int _gridSizeId = Shader.PropertyToID("grid_size");

        // copyShader
        private readonly int _copySrcBufferId = Shader.PropertyToID("copy_src_buffer");
        private readonly int _copyDstBufferId = Shader.PropertyToID("copy_dst_buffer");
        private readonly int _copySrcOffsetId = Shader.PropertyToID("copy_src_offset");
        private readonly int _copyDstOffsetId = Shader.PropertyToID("copy_dst_offset");
        private readonly int _copyElementSizeId = Shader.PropertyToID("copy_element_size");
        private readonly int _copyLengthId = Shader.PropertyToID("copy_length");

        // radixSortShader
        private readonly int _sortLocalSharedMemorySizeId = Shader.PropertyToID("sort_local_shared_memory_size");
        private readonly int _prefixSumsOffsetId = Shader.PropertyToID("prefix_sums_offset");
        private readonly int _blockSumsOffsetId = Shader.PropertyToID("block_sums_offset");
        private readonly int _scanBlockSumsOffsetId = Shader.PropertyToID("scan_block_sum_offset");
        private readonly int _radixShiftWidthId = Shader.PropertyToID("radix_shift_width");
        private readonly int _radixKeyInLengthId = Shader.PropertyToID("radix_key_in_length");
        private readonly int _radixKeyInId = Shader.PropertyToID("radix_key_in");
        private readonly int _radixKeyOutId = Shader.PropertyToID("radix_key_out");
        private readonly int _radixValueInId = Shader.PropertyToID("radix_value_in");
        private readonly int _radixValueOutId = Shader.PropertyToID("radix_value_out");

        private readonly int _preScanSharedDataSizeId = Shader.PropertyToID("pre_scan_shared_data_size");
        private readonly int _radixTempBufferId = Shader.PropertyToID("radix_temp_buffer");
        private readonly int _blellochOutBufferOffsetId = Shader.PropertyToID("blelloch_out_buffer_offset");
        private readonly int _blellochInBufferOffsetId = Shader.PropertyToID("blelloch_in_buffer_offset");

        private readonly int _blellochBlockSumsBufferOffsetId =
            Shader.PropertyToID("blelloch_block_sums_buffer_offset");

        private readonly int _blellochLengthId = Shader.PropertyToID("blelloch_length");

        // simulateShader
        // 全局物理参数
        private readonly int _gravityId = Shader.PropertyToID("gravity");
        private readonly int _dtId = Shader.PropertyToID("dt");
        private readonly int _dampingCoefficientId = Shader.PropertyToID("damping_coefficient");
        private readonly int _zUpId = Shader.PropertyToID("z_up");

        //控制器
        private readonly int _controllerPositionId = Shader.PropertyToID("controller_position");
        private readonly int _controllerVelocityId = Shader.PropertyToID("controller_velocity");
        private readonly int _controllerAngleVelocityId = Shader.PropertyToID("controller_angle_velocity");
        private readonly int _controllerRadiusId = Shader.PropertyToID("controller_radius");

        //GS
        private readonly int _gsTotalCountId = Shader.PropertyToID("gs_total_count");

        private readonly int _gsPositionBufferId = Shader.PropertyToID("gs_position_buffer");
        private readonly int _gsOtherBufferId = Shader.PropertyToID("gs_other_buffer");

        private readonly int _gsLocalCountId = Shader.PropertyToID("gs_local_count");
        private readonly int _gsLocalOffsetId = Shader.PropertyToID("gs_local_offset");

        //网格
        private readonly int _verticesTotalCountId = Shader.PropertyToID("vertices_total_count");
        private readonly int _faceTotalCountId = Shader.PropertyToID("face_total_count");
        private readonly int _cellTotalCountId = Shader.PropertyToID("cell_total_count");
        private readonly int _boundaryId = Shader.PropertyToID("boundary");

        private readonly int _vertxBufferId = Shader.PropertyToID("vertices_x_buffer");
        private readonly int _vertXBufferId = Shader.PropertyToID("vertices_X_buffer");
        private readonly int _vertGroupBufferId = Shader.PropertyToID("vert_group_buffer");

        private readonly int _edgeIndicesBufferId = Shader.PropertyToID("edge_indices_buffer");
        private readonly int _faceIndicesBufferId = Shader.PropertyToID("face_indices_buffer");
        private readonly int _cellIndicesBufferId = Shader.PropertyToID("cell_indices_buffer");

        private readonly int _vertVelocityBufferId = Shader.PropertyToID("vert_velocity_buffer");
        private readonly int _vertForceBufferId = Shader.PropertyToID("vert_force_buffer");
        private readonly int _vertMassBufferId = Shader.PropertyToID("vert_mass_buffer");
        private readonly int _vertInvMassBufferId = Shader.PropertyToID("vert_inv_mass_buffer");
        private readonly int _vertNewXBufferId = Shader.PropertyToID("vert_new_x_buffer");
        private readonly int _vertDeltaPosBufferId = Shader.PropertyToID("vert_delta_pos_buffer");
        private readonly int _vertSelectedIndicesBufferId = Shader.PropertyToID("vert_selected_indices_buffer");
        private readonly int _rigidVertGroupBufferId = Shader.PropertyToID("rigid_vert_group_buffer");
        private readonly int _cellMultiplierBufferId = Shader.PropertyToID("cell_multiplier_buffer");
        private readonly int _cellDsInvBufferId = Shader.PropertyToID("cell_ds_inv_buffer");
        private readonly int _cellVolumeInitBufferId = Shader.PropertyToID("cell_volume_init_buffer");
        private readonly int _cellDensityBufferId = Shader.PropertyToID("cell_density_buffer");
        private readonly int _cellMuBufferId = Shader.PropertyToID("cell_mu_buffer");
        private readonly int _cellLambdaBufferId = Shader.PropertyToID("cell_lambda_buffer");

        private readonly int _rigidMassBufferId = Shader.PropertyToID("rigid_mass_buffer");
        private readonly int _rigidMassCenterInitBufferId = Shader.PropertyToID("rigid_mass_center_init_buffer");
        private readonly int _rigidMassCenterBufferId = Shader.PropertyToID("rigid_mass_center_buffer");

        private readonly int _rigidAngleVelocityMatrixBufferId =
            Shader.PropertyToID("rigid_angle_velocity_matrix_buffer");

        private readonly int _rigidRotationMatrixBufferId = Shader.PropertyToID("rigid_rotation_matrix_buffer");

        private readonly int _triangleAabbsBufferId = Shader.PropertyToID("triangle_aabbs_buffer");
        private readonly int _sortedTriangleAabbsBufferId = Shader.PropertyToID("sorted_triangle_aabbs_buffer");
        private readonly int _partialAabbBufferId = Shader.PropertyToID("partial_aabb_buffer");
        private readonly int _mortonCodeBufferId = Shader.PropertyToID("morton_code_buffer");
        private readonly int _sortedMortonCodeBufferId = Shader.PropertyToID("sorted_morton_code_buffer");
        private readonly int _indicesBufferId = Shader.PropertyToID("indices_buffer");
        private readonly int _sortedIndicesBufferId = Shader.PropertyToID("sorted_indices_buffer");
        private readonly int _faceFlagsBufferId = Shader.PropertyToID("face_flags_buffer");
        private readonly int _lbvhAabbsBufferId = Shader.PropertyToID("lbvh_aabbs_buffer");
        private readonly int _lbvhNodesBufferId = Shader.PropertyToID("lbvh_nodes_buffer");
        private readonly int _lbvhSortBufferId = Shader.PropertyToID("lbvh_sort_buffer");

        // 碰撞检测额外参数
        private readonly int _collisionDetectionDistId = Shader.PropertyToID("collision_detection_dist");
        private readonly int _aabbGridSizeId = Shader.PropertyToID("aabb_grid_size");

        private readonly int _collisionPairsBufferId = Shader.PropertyToID("collision_pairs_buffer");
        private readonly int _exactCollisionPairsBufferId = Shader.PropertyToID("exact_collision_pairs_buffer");
        private readonly int _totalPairsBufferId = Shader.PropertyToID("total_pairs_buffer");
        private readonly int _totalExactPairsBufferId = Shader.PropertyToID("total_exact_pairs_buffer");

        private readonly int _covBufferId = Shader.PropertyToID("cov_buffer");
        private readonly int _localTetXBufferId = Shader.PropertyToID("local_tet_x_buffer");
        private readonly int _localTetWBufferId = Shader.PropertyToID("local_tet_w_buffer");
        private readonly int _globalTetIdxBufferId = Shader.PropertyToID("global_tet_idx_buffer");
        private readonly int _globalTetWBufferId = Shader.PropertyToID("global_tet_w_buffer");

        private readonly int _cellLocalCountId = Shader.PropertyToID("cell_local_count");
        private readonly int _cellLocalOffsetId = Shader.PropertyToID("cell_local_offset");

        // 内核函数的索引

        // copyShader
        private int copyBufferKernel => copyShader.FindKernel("copy_buffer");

        // radixSortShader
        private int radixSortLocalKernel => radixShader.FindKernel("radix_sort_local");
        private int radixGlobalShuffleKernel => radixShader.FindKernel("radix_global_shuffle");
        private int blellochPreScanKernel => radixShader.FindKernel("radix_blelloch_pre_scan");

        private int blellochAddBlockSumsKernel => radixShader.FindKernel("radix_blelloch_add_block_sums");

        // simulateShader
        private int selectVerticesKernel => simulateShader.FindKernel("select_vertices");
        private int cleanSelectionKernel => simulateShader.FindKernel("clean_selected_vertices");
        private int initializeCovarianceKernel => simulateShader.FindKernel("initialize_covariance");
        private int getLocalEmbededTetsKernel => simulateShader.FindKernel("get_local_embeded_tets");
        private int getGlobalEmbededTetKernel => simulateShader.FindKernel("get_global_embeded_tet");
        private int initFemBasesKernel => simulateShader.FindKernel("init_fem_bases");
        private int initInvMassKernel => simulateShader.FindKernel("init_inv_mass");
        private int initRigidKernel => simulateShader.FindKernel("init_rigid");
        private int computeTriangleAabbsKernel => simulateShader.FindKernel("compute_triangle_aabbs");
        private int computeMortonAndIndicesKernel => simulateShader.FindKernel("compute_morton_and_indices");
        private int getSortedTriangleAabbsKernel => simulateShader.FindKernel("get_sorted_triangle_aabbs");
        private int resetAABBKernel => simulateShader.FindKernel("reset_aabb");
        private int constructInternalNodesKernel => simulateShader.FindKernel("construct_internal_nodes");
        private int computeInternalAabbsKernel => simulateShader.FindKernel("compute_internal_aabbs");
        private int queryCollisionPairsKernel => simulateShader.FindKernel("query_collision_pairs");
        private int queryCollisionTrianglesKernel => simulateShader.FindKernel("query_collision_triangles");
        private int aabbReduce512Kernel => simulateShader.FindKernel("aabb_reduce_512");
        private int aabbReduce256Kernel => simulateShader.FindKernel("aabb_reduce_256");
        private int aabbReduce128Kernel => simulateShader.FindKernel("aabb_reduce_128");
        private int aabbReduce64Kernel => simulateShader.FindKernel("aabb_reduce_64");
        private int aabbReduce32Kernel => simulateShader.FindKernel("aabb_reduce_32");
        private int aabbReduce16Kernel => simulateShader.FindKernel("aabb_reduce_16");
        private int aabbReduce8Kernel => simulateShader.FindKernel("aabb_reduce_8");
        private int aabbReduce4Kernel => simulateShader.FindKernel("aabb_reduce_4");
        private int aabbReduce2Kernel => simulateShader.FindKernel("aabb_reduce_2");
        private int aabbReduce1Kernel => simulateShader.FindKernel("aabb_reduce_1");
        private int applyExternalForceKernel => simulateShader.FindKernel("apply_external_force");

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
            _lastControllerScreenPos = Mouse.current.position.ReadValue();
        }

        /// <summary>
        /// 鼠标松开时的回调
        /// </summary>
        public void MouseUp()
        {
            // 清空所有位置信息
            _lastControllerScreenPos = Vector2.positiveInfinity;
            _controllerScreenPos = Vector2.positiveInfinity;
            _controllerVelocity = Vector3.positiveInfinity;
            _lastMouseTime = 0;
        }

        /// <summary>
        /// 更新鼠标的速度
        /// </summary>
        /// <param name="time">触发时间</param>
        /// <param name="cam">当前相机</param>
        private void UpdateMouseVelocity(float time, Camera cam)
        {
            // todo:更改一下逻辑以获取正确的鼠标深度（通过步进和并行检测来实现简易raycasting)
            if (_lastControllerScreenPos.Equals(Vector2.positiveInfinity)) return;
            _controllerScreenPos = Mouse.current.position.ReadValue();
            _controllerPosition = GsTools.GetMouseWorldPos(_controllerScreenPos, mainCamera, ReferenceDepth);
            //计算屏幕空间速度
            Vector2 screenDelta = _controllerScreenPos - _lastControllerScreenPos;
            float deltaTime = time - _lastMouseTime;
            Vector2 screenVelocity = screenDelta / deltaTime;
            _controllerVelocity = GsTools.ScreenToWorldVelocity(cam, screenVelocity, ReferenceDepth);
            // 更新上次屏幕位置和触发时间为本次统计
            _lastControllerScreenPos = _controllerScreenPos;
            _lastMouseTime = time;
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
                tip.GetComponent<TipsManager>().SetButtonInteractable(false);
                bool success = InitializeSimulationParams();
                if (!success)
                {
                    MainUIManager.ShowTip("Something went wrong, please check your asset file.");
                    return;
                }

                Destroy(tip);
                // 统计初始化时间
                initializeMilliSeconds = timerUtil.GetDeltaTime();
            }

            MainUIManager.ShowTip("Press 'Space' to pause the process of simulation");
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
            totalCollisionDetectionMilliSeconds = 0;
            totalXpbdMilliSeconds = 0;
            totalFemSolveMilliSeconds = 0;
            totalCollisionSolveMilliSeconds = 0;
            totalEmbededMilliSeconds = 0;
            _lbvhSortBufferSize = 0;
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
                using TimerUtil simulateTimer = new TimerUtil("Simulate");
                //拖拽过程中，更新鼠标速度
                if (isMousePressed)
                {
                    UpdateMouseVelocity(Time.time, mainCamera);
                    // Debug.Log($"velocity:{_controllerVelocity}");
                }

                //根据鼠标的位置选择范围内的顶点，更新数据
                UpdateSelectVertices();

                //碰撞检测计数
                int collisionCount = 0;
                // 将一帧长(frame_dt)分割成多个子时间段(dt)
                float dtLeft = frameDt;
                while (dtLeft > 0f)
                {
                    // 每隔 collision_dection_iter_interval 步执行一次碰撞检测，减少计算量
                    if (collisionCount % collisionDetectionIterInterval == 0)
                    {
                        using TimerUtil collisionDetectionTimer = new TimerUtil("Collision Detection");
                        // 执行碰撞检测
                        CollisionDetection();
                        totalCollisionDetectionMilliSeconds += collisionDetectionTimer.GetDeltaTime();
                    }

                    collisionCount++;
                    float dt0 = Mathf.Min(dt, dtLeft);
                    dtLeft -= dt0;

                    // 应用外力（重力/阻尼和控制器外力）
                    using (TimerUtil xpbdTimer = new TimerUtil("Apply External Force"))
                    {
                        ApplyExternalForce(dt0);
                        _cellMultiplierBuffer.SetData(new float[_totalCellsCount]);
                        totalXpbdMilliSeconds += xpbdTimer.GetDeltaTime();
                    }

                    // 进行FEM约束求解和碰撞处理
                    for (int i = 0; i < xpbdRestIter; i++)
                    {
                        using (TimerUtil xpbdTimer = new TimerUtil("Memset"))
                        {
                            _vertDeltaPosBuffer.SetData(new float3[_totalVerticesCount]);
                            totalXpbdMilliSeconds += xpbdTimer.GetDeltaTime();
                        }

                        // FEM求解：处理弹性形变
                        using (TimerUtil femTimer = new TimerUtil("Solve FEM Constraints"))
                        {
                            SolveFemConstraints();
                            totalFemSolveMilliSeconds += femTimer.GetDeltaTime();
                        }

                        // 碰撞约束：解决顶点与三角形面之间的穿透
                        using (TimerUtil collisionSolveTimer =
                               new TimerUtil("Solve Triangle Point Distance Constraint"))
                        {
                            SolveTrianglePointDistanceConstraint();
                            totalCollisionSolveMilliSeconds += collisionSolveTimer.GetDeltaTime();
                        }

                        // 更新位置
                        using (TimerUtil xpbdTimer = new TimerUtil("PBD Post Solve"))
                        {
                            PbdPostSolve();
                            totalXpbdMilliSeconds += xpbdTimer.GetDeltaTime();
                        }
                    }

                    // 将子步长的计算结果应用到顶点位置，完成时间步进
                    using (TimerUtil xpbdTimer = new TimerUtil("PBD Advance"))
                    {
                        PbdAdvance();
                        totalXpbdMilliSeconds += xpbdTimer.GetDeltaTime();
                    }
                }

                // 计算刚体的质心运动（平移）和旋转，更新顶点位置
                using (TimerUtil xpbdTimer = new TimerUtil("Solve Rigid"))
                {
                    SolveRigid();
                    totalXpbdMilliSeconds += xpbdTimer.GetDeltaTime();
                }

                // 将物理顶点位置转换为高斯泼溅的渲染属性（位置、缩放、旋转）。
                // 插值方法：基于四面体权重（tet_w）在全局和局部坐标系间插值。
                using (TimerUtil interpolateTimer = new TimerUtil("Apply Interpolation"))
                {
                    ApplyInterpolation();
                    totalXpbdMilliSeconds += interpolateTimer.GetDeltaTime();
                }

                // 提交任务并同步
                SubmitTaskAndSynchronize();
                // 更新总计模拟时间
                totalSimulateMilliSeconds += simulateTimer.GetDeltaTime();
                // 更新总步数
                totalSteps++;
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