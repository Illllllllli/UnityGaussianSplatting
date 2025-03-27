using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;

namespace GSTestScene.Simulation
{
    /// <summary>
    /// 碰撞检测包围盒
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct LbvhAABBBoundingBox
    {
        public float4 upper;
        public float4 lower;

        public static LbvhAABBBoundingBox Merge(LbvhAABBBoundingBox a, LbvhAABBBoundingBox b)
        {
            LbvhAABBBoundingBox mergedBox = new LbvhAABBBoundingBox
            {
                lower = Vector4.Min(a.lower, b.lower),
                upper = Vector4.Max(a.upper, b.upper)
            };
            return mergedBox;
        }
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