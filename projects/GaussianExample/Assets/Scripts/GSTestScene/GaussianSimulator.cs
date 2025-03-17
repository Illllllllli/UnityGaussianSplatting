using System;
using GaussianSplatting.Runtime;
using UnityEngine;

namespace GSTestScene
{
    /// <summary>
    /// 在GS渲染器的基础上尝试进行物理模拟
    /// </summary>
    public class GaussianSimulator : MonoBehaviour
    {
        public GaussianSplatRenderer gaussianSplatRenderer;
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
        public int XPBDRestIter = 25;
        // 碰撞刚度系数
        public float collisionStiffness = 0.1f;
        // 最小碰撞距离，避免穿透（Penetration）的阈值
        public float collisionMinimalDist = 5e-3f;
        // 碰撞检测的迭代间隔次数
        public float collisionDetectionIterInterval = 100;
        // 切换全局坐标系的上方向
        public bool isZUp = false;
        // 地面高度，限制物体下落的Y坐标
        public float groundHeight = 0.0f;
        // 全局缩放系数，统一调整场景中所有物体的尺寸
        public float globalScale = 1.0f;
        // 全局旋转，应用于所有物体的初始方向
        public Quaternion globalRotation;
        // 全局位置偏移，将所有物体平移指定距离
        public Vector3 globalOffset;
        // 每个物体的局部位置偏移
        public Vector3[] objectOffsets;
        // 边界标志位。但不知道具体干什么用
        public int boundary = 0;
        // Start is called before the first frame update
        
        /// <summary>
        /// 开启/继续物理模拟
        /// </summary>
        public void StartSimulate()
        {
            Debug.Log("start simulate");
            
        }
        
        /// <summary>
        /// 暂停物理模拟
        /// </summary>
        public void PauseSimulate()
        {
            
        }

        /// <summary>
        /// 重置物理模拟状态（为初始状态）
        /// </summary>
        public void ResetSimulate()
        {
            
        }
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}

/// <summary>
/// 材质属性类。这里的材质不是指渲染材质，而是指物理属性
/// </summary>
[Serializable]
public class MaterialProperty {
    // 物体密度
    public float density;
    // 杨氏模量，控制物体的刚度
    public float E;
    // 泊松比，描述材料横向压缩与纵向拉伸的比例
    public float nu;
    // 物体局部缩放系数
    public float scale = 1.0f;
    // 标记物体是否为刚体（是否可以发生形变）
    public bool isRigid = false;
    // 物体的初始旋转
    public Quaternion rot = Quaternion.identity;
}