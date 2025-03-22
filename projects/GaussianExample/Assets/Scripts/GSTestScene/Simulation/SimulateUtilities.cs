using System;
using UnityEngine;

namespace GSTestScene.Simulation
{
    /// <summary>
    /// 存储物理模拟相关方法
    /// </summary>
    public partial class GaussianSimulator
    {
        
    }
    
    /// <summary>
    /// 材质属性类。这里的材质不是指渲染材质，而是指物理属性
    /// </summary>
    [Serializable]
    public class MaterialProperty
    {
        // 物体密度
        public float density = 1;

        // 杨氏模量，控制物体的刚度
        public float E = 2;

        // 泊松比，描述材料横向压缩与纵向拉伸的比例
        public float nu = 1;


        // 标记物体是否为刚体（是否可以发生形变）
        public bool isRigid = false;

        // 物体的初始旋转
        public Quaternion rot = Quaternion.identity;
    }
}