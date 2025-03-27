using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;

namespace GSTestScene.Simulation
{
    /// <summary>
    /// 存放碰撞检测相关方法
    /// </summary>
    public partial class GaussianSimulator
    {
        /// <summary>
        /// 计算全局的AABB包围盒
        /// </summary>
        private void GetGlobalAabb()
        {
            const int maxThreads = SimulateBlockSize;
            int threads = (_totalFacesCount < maxThreads * 2) ? next_pow_2((_totalFacesCount + 1) / 2) : maxThreads;
            int blocks = Mathf.Min((_totalFacesCount + (threads * 2 - 1)) / (threads * 2), PartialAABBCount);
            LaunchAabbReduce(blocks, threads);
            SubmitTaskAndSynchronize();
            LbvhAABBBoundingBox[] boundingBoxes = new LbvhAABBBoundingBox[PartialAABBCount];
            _partialAabbBuffer.GetData(boundingBoxes);
            _partialAabbData.CopyFrom(boundingBoxes);
            for (int i = 1; i < blocks; i++)
            {
                _partialAabbData[0] = LbvhAABBBoundingBox.Merge(_partialAabbData[0], _partialAabbData[i]);
            }
            _aabbGlobal = _partialAabbData[0];
            _partialAabbBuffer.SetData(_partialAabbData, 0, 0, 1);
        }

        /// <summary>
        /// 构建BVH包围盒
        /// </summary>
        private void ConstructBvh()
        {
            ComputeTriangleAabbs();
            SubmitTaskAndSynchronize();
            int objectCount = _totalFacesCount;
            int internalNodesCount = objectCount - 1;
            int nodesCount = objectCount * 2 - 1;
            GetGlobalAabb();
            {
                ComputeMortonAndIndices();
                SubmitTaskAndSynchronize();
                RadixSort<ulong, int>(_sortedMortonCodeBuffer, _mortonCodeBuffer, _sortedIndicesBuffer, _indicesBuffer,
                    _lbvhSortBuffer, _lbvhSortBufferSize);
                SubmitTaskAndSynchronize();
                GetSortedTriangleAabbs();
                SubmitTaskAndSynchronize();
            }

            ResetAabb(nodesCount);
            SubmitTaskAndSynchronize();
            SetBufferValue(0xffffffff, Marshal.SizeOf<LbvhNode>() * nodesCount / sizeof(uint), 0, _lbvhNodesBuffer);
            ConstructInternalNodes();
            SubmitTaskAndSynchronize();

            SetBufferValue(0, internalNodesCount, 0, _faceFlagsBuffer);
            ComputeInternalAabbs();
            SubmitTaskAndSynchronize();
        }

        /// <summary>
        /// 粗粒度的碰撞检测
        /// </summary>
        private void BoardPhaseCulling()
        {
            SetBufferValue(0, 1, 0, _totalPairsBuffer);
            ComputeTriangleAabbs(collisionMinimalDist * Sqrt3);
            SubmitTaskAndSynchronize();
            QueryCollisionPairs();
            SubmitTaskAndSynchronize();
            _totalPairsBuffer.GetData(_totalPairsCount);
            _totalPairsCount[0] = Mathf.Min(_totalPairsCount[0], MaxCollisionPairs);
        }

        /// <summary>
        /// 细粒度的碰撞检测
        /// </summary>
        private void NarrowPhaseDetection()
        {
            SetBufferValue(0, 1, 0, _totalExactPairsBuffer);
            QueryCollisionTriangles();
            SubmitTaskAndSynchronize();
            _totalExactPairsBuffer.GetData(_totalExactPairsCount);
            _totalExactPairsCount[0] = Mathf.Min(_totalExactPairsCount[0], MaxCollisionPairs);
        }

        /// <summary>
        /// 碰撞检测主方法
        /// </summary>
        private void CollisionDetection()
        {
            ConstructBvh();
            BoardPhaseCulling();
            NarrowPhaseDetection();
        }
    }
}