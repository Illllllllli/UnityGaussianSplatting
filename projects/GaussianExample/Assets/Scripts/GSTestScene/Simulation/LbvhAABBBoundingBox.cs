using System.Runtime.InteropServices;
using UnityEngine;

namespace GSTestScene.Simulation
{
    /// <summary>
    /// 碰撞检测包围盒
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct LbvhAABBBoundingBox
    {
        public Vector4 upper;
        public Vector4 lower; 
    }

    /// <summary>
    /// 碰撞检测节点
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)] // 确保内存对齐
    public struct LbvhNode
    {
        public uint ParentIdx; // 对应 parent_idx
        public uint LeftIdx; // 对应 left_idx
        public uint RightIdx; // 对应 right_idx
    }
}
