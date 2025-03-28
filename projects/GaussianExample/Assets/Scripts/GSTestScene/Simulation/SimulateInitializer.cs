using System;
using System.Linq;
using System.Runtime.InteropServices;
using GaussianSplatting.Runtime;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;


namespace GSTestScene.Simulation
{
    /// <summary>
    ///  存放模拟初始化相关方法
    /// </summary>
    public partial class GaussianSimulator
    {
        /// <summary>
        /// 初始化所有GS物体并更新全局相关统计信息（偏移量等）
        /// </summary>
        /// <returns></returns>
        private bool InitializeGaussianObjects()
        {
            // 生成子物体列表
            for (int i = 0; i < GaussianSplats.Count; i++)
            {
                GameObject gaussianSplat = GaussianSplats[i];
                GaussianObject gaussianObject = new GaussianObject();
                MaterialProperty materialProperty = new MaterialProperty();
                GaussianSplatRenderer splatRenderer = gaussianSplat.GetComponent<GaussianSplatRenderer>();
                // 首先更新每个子物体的属性
                if (!gaussianObject.InitializeData(splatRenderer, materialProperty, _totalGsCount,
                        _totalVerticesCount, _totalEdgesCount, _totalFacesCount, _totalCellsCount, i))
                    return false;
                // 然后更新整个GS的总顶点数等统计信息
                _totalGsCount += gaussianObject.gsNums;
                _totalVerticesCount += gaussianObject.verticesCount;
                _totalEdgesCount += gaussianObject.edgesCount;
                _totalFacesCount += gaussianObject.facesCount;
                _totalCellsCount += gaussianObject.cellsCount;
                // 更新到列表中
                _materialProperties.Add(materialProperty);
                _gaussianObjects.Add(gaussianObject);
            }

            return true;
        }

        /// <summary>
        /// 初始化分配所有公共缓冲区
        /// </summary>
        private bool InitializeAllocBuffers()
        {
            try
            {
                //只有位置相关的缓冲区，stride设为sizeof(float3)
                // GS相关缓冲区
                _gsPositionBuffer = new ComputeBuffer(_totalGsCount * 3, sizeof(uint)); // position(3)
                _gsOtherBuffer = new ComputeBuffer(_totalGsCount * 4, sizeof(uint)); // rotation(1) + scale(3)
                // 网格相关缓冲区
                _vertxBuffer = new ComputeBuffer(_totalVerticesCount, sizeof(float) * 3);
                _vertXBuffer = new ComputeBuffer(_totalVerticesCount, sizeof(float) * 3);
                _vertGroupBuffer = new ComputeBuffer(_totalVerticesCount, sizeof(int));

                _edgeIndicesBuffer = new ComputeBuffer(_totalEdgesCount * 2, sizeof(int));
                _faceIndicesBuffer = new ComputeBuffer(_totalFacesCount * 3, sizeof(int));
                _cellIndicesBuffer = new ComputeBuffer(_totalCellsCount * 4, sizeof(int));

                _vertVelocityBuffer = new ComputeBuffer(_totalVerticesCount * 3, sizeof(float));
                _vertForceBuffer = new ComputeBuffer(_totalVerticesCount * 3, sizeof(float));
                _vertMassBuffer = new ComputeBuffer(_totalVerticesCount, sizeof(float));
                _vertMassBuffer.SetData(new float[_totalVerticesCount]);
                _vertInvMassBuffer = new ComputeBuffer(_totalVerticesCount, sizeof(float));
                _vertNewXBuffer = new ComputeBuffer(_totalVerticesCount, sizeof(float) * 3);
                _vertDeltaPosBuffer = new ComputeBuffer(_totalVerticesCount, sizeof(float) * 3);
                _vertSelectedIndicesBuffer = new ComputeBuffer(_totalVerticesCount, sizeof(int));
                _rigidVertGroupBuffer = new ComputeBuffer(_totalVerticesCount, sizeof(int));
                _cellMultiplierBuffer = new ComputeBuffer(_totalCellsCount, sizeof(float));
                _cellDsInvBuffer = new ComputeBuffer(_totalCellsCount * 9, sizeof(float));

                _cellVolumeInitBuffer = new ComputeBuffer(_totalCellsCount, sizeof(float));
                _cellDensityBuffer = new ComputeBuffer(_totalCellsCount, sizeof(float));
                _cellMuBuffer = new ComputeBuffer(_totalCellsCount, sizeof(float));
                _cellLambdaBuffer = new ComputeBuffer(_totalCellsCount, sizeof(float));

                _rigidMassBuffer = new ComputeBuffer(_gaussianObjects.Count, sizeof(float));
                _rigidMassBuffer.SetData(new float[_gaussianObjects.Count]);
                _rigidMassCenterInitBuffer = new ComputeBuffer(_gaussianObjects.Count * 3, sizeof(float));
                _rigidMassCenterInitBuffer.SetData(new float[_gaussianObjects.Count * 3]);
                _rigidMassCenterBuffer = new ComputeBuffer(_gaussianObjects.Count * 3, sizeof(float));
                _rigidMassCenterBuffer.SetData(new float[_gaussianObjects.Count * 3]);
                _rigidAngleVelocityMatrixBuffer = new ComputeBuffer(_gaussianObjects.Count * 9, sizeof(float));
                _rigidRotationMatrixBuffer = new ComputeBuffer(_gaussianObjects.Count * 9, sizeof(float));

                _triangleAabbsBuffer = new ComputeBuffer(_totalFacesCount, Marshal.SizeOf<LbvhAABBBoundingBox>());
                _sortedTriangleAabbsBuffer =
                    new ComputeBuffer(_totalFacesCount, Marshal.SizeOf<LbvhAABBBoundingBox>());
                _partialAabbBuffer =
                    new ComputeBuffer(PartialAABBCount, Marshal.SizeOf<LbvhAABBBoundingBox>());
                _partialAabbData =
                    new NativeArray<LbvhAABBBoundingBox>(PartialAABBCount,
                        Allocator.Persistent);
                _mortonCodeBuffer = new ComputeBuffer(_totalFacesCount, sizeof(ulong));
                _sortedMortonCodeBuffer = new ComputeBuffer(_totalFacesCount, sizeof(ulong));
                _indicesBuffer = new ComputeBuffer(_totalFacesCount, sizeof(int));
                _sortedIndicesBuffer = new ComputeBuffer(_totalFacesCount, sizeof(int));
                _faceFlagsBuffer = new ComputeBuffer(_totalFacesCount, sizeof(int));

                int bvhSize = _totalFacesCount * 2 - 1;
                _lbvhAabbsBuffer = new ComputeBuffer(bvhSize, Marshal.SizeOf<LbvhAABBBoundingBox>());
                _lbvhNodesBuffer = new ComputeBuffer(bvhSize, Marshal.SizeOf<LbvhNode>());
                // 执行Radix排序以更新SortBufferSize
                if (!RadixSort<ulong, int>(_sortedMortonCodeBuffer, _mortonCodeBuffer, _sortedIndicesBuffer,
                        _indicesBuffer, null, _totalFacesCount)) return false;
                _lbvhSortBuffer = new ComputeBuffer(_lbvhSortBufferSize, sizeof(uint));

                _collisionPairsBuffer = new ComputeBuffer(MaxCollisionPairs, Marshal.SizeOf<int2>());
                _totalPairsBuffer = new ComputeBuffer(1, sizeof(int));
                _exactCollisionPairsBuffer = new ComputeBuffer(MaxCollisionPairs, Marshal.SizeOf<int4>());
                _totalExactPairsBuffer = new ComputeBuffer(1, sizeof(int));
                _covBuffer = new ComputeBuffer(_totalGsCount * 9, sizeof(float));
                _localTetXBuffer = new ComputeBuffer(_totalGsCount * 12, sizeof(float));
                _localTetWBuffer = new ComputeBuffer(_totalGsCount * 3, sizeof(float));
                _globalTetIdxBuffer = new ComputeBuffer(_totalGsCount * 4, sizeof(int));
                _globalTetWBuffer = new ComputeBuffer(_totalGsCount * 12, sizeof(float));
                _globalTetWBuffer.SetData(new float[_totalGsCount * 12]);
            }
            catch (Exception e)
            {
                Debug.Log(e);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 初始化填充公共缓冲区（从子物体中获取数据）
        /// </summary>
        /// <returns>是否填充成功</returns>
        private bool InitializeFillBuffers()
        {
            try
            {
                // 传递所需GS数据
                BatchCopyToBuffer(_gsPositionBuffer, o => o.gsPositionData, o => o.GsOffset, 3, true);
                BatchCopyToBuffer(_gsOtherBuffer, o => o.gsOtherData, o => o.GsOffset, 4);
                // 传递所需网格数据
                BatchCopyToBuffer(_vertXBuffer, o => o.VerticesData, o => o.VerticesOffset, 3);
                BatchCopyToBuffer(_vertxBuffer, o => o.VerticesData, o => o.VerticesOffset, 3);
                BatchCopyToBuffer(_vertGroupBuffer, o => o.VerticesGroupData, o => o.VerticesOffset, 1);
                BatchCopyToBuffer(_rigidVertGroupBuffer, o => o.RigidGroupData, o => o.VerticesOffset, 1);
                BatchCopyToBuffer(_edgeIndicesBuffer, o => o.EdgesData, o => o.EdgesOffset, 2);
                BatchCopyToBuffer(_faceIndicesBuffer, o => o.FacesData, o => o.FacesOffset, 3);
                BatchCopyToBuffer(_cellIndicesBuffer, o => o.CellsData, o => o.CellsOffset, 4);
                BatchCopyToBuffer(_cellDensityBuffer, o => o.DensityData, o => o.CellsOffset, 1);
                BatchCopyToBuffer(_cellMuBuffer, o => o.MuData, o => o.CellsOffset, 1);
                BatchCopyToBuffer(_cellLambdaBuffer, o => o.LambdaData, o => o.CellsOffset, 1);
            }
            catch (Exception e)
            {
                Debug.Log(e);
                return false;
            }

            return true;

            //批量拷贝到公共缓冲区
            void BatchCopyToBuffer<TDataType>(
                ComputeBuffer targetBuffer,
                Func<GaussianObject, NativeArray<TDataType>> dataSelector, // 动态选择数据源
                Func<GaussianObject, int> offsetCalculator, // 计算对象在缓冲区的偏移
                int elementStride, // 元素步长(如顶点3=float3)
                bool skipEnd = false // 是否跳过最后一个元素(GS位置数据貌似多了一个元素用于标记末尾)
            ) where TDataType : struct
            {
                // 预合并所有数据到临时数组
                // 这里，由于缓冲区和NativeArray的数据格式可能不一致，所以需要进行空间计算
                using NativeArray<TDataType> mergedData = new NativeArray<TDataType>(
                    targetBuffer.count * targetBuffer.stride / Marshal.SizeOf<TDataType>(),
                    Allocator.Temp,
                    NativeArrayOptions.UninitializedMemory);
                foreach (GaussianObject gaussianObject in _gaussianObjects)
                {
                    if (gaussianObject.IsBackground) continue;

                    // 获取对象数据源
                    NativeArray<TDataType> srcData = dataSelector(gaussianObject);

                    // 计算写入位置
                    int dstStart = offsetCalculator(gaussianObject) * elementStride;
                    int copyLength = srcData.Length;
                    if (skipEnd)
                    {
                        copyLength--;
                    }

                    // 分段拷贝
                    NativeArray<TDataType>.Copy(
                        srcData, 0,
                        mergedData, dstStart,
                        copyLength);
                }

                // 一次性提交GPU缓冲
                targetBuffer.SetData(mergedData);
            }
        }

        /// <summary>
        /// 初始化GS网格插值
        /// </summary>
        /// <returns>是否初始化成功</returns>
        private bool InitializeInterpolation()
        {
            try
            {
                InitializeCovariance();
                GetLocalEmbededTets();
                // TestBufferFinish<float>(_localTetWBuffer);
                SubmitTaskAndSynchronize();
                SetBufferValue(0xffffffff, 4 * _totalGsCount, 0, _globalTetIdxBuffer);
                foreach (var gaussianObject in _gaussianObjects.Where(gaussianObject => !gaussianObject.IsBackground))
                {
                    // 改成一个物体调用一次
                    GetGlobalEmbededTet(gaussianObject);
                    // TestBufferFinish<float>(_globalTetWBuffer);
                    SubmitTaskAndSynchronize();
                }


                return true;
            }
            catch (Exception e)
            {
                Debug.Log(e);
                return false;
            }
        }

        /// <summary>
        /// 初始化PDB-FEM模拟
        /// </summary>
        /// <returns></returns>
        private bool InitializePbdFem()
        {
            try
            {
                InitializeFemBases();
                // TestBufferFinish<float>(_vertMassBuffer);
                SubmitTaskAndSynchronize();
                InitializeInvMass();
                // TestBufferFinish<float>(_vertInvMassBuffer);
                SubmitTaskAndSynchronize();
                return true;
            }
            catch (Exception e)
            {
                Debug.Log(e);
                return false;
            }
        }

        /// <summary>
        /// 初始化刚体模拟
        /// </summary>
        /// <returns></returns>
        private bool InitializeRigid()
        {
            try
            {
                InitRigid();
                // TestBufferFinish<float>(_rigidMassCenterInitBuffer);
                // TestBufferFinish<float>(_rigidMassBuffer);
                SubmitTaskAndSynchronize();
                return true;
            }
            catch (Exception e)
            {
                Debug.Log(e);
                return false;
            }
        }


        /// <summary>
        /// 初始化每个GaussianObject的属性，在此基础上初始化整个Simulator的公共属性
        /// </summary>
        private bool InitializeSimulationParams()
        {
            // 初始化命令缓冲区
            _commandBuffer = new CommandBuffer { name = "Command Buffer" };
            // 初始化所有GS物体
            if (!InitializeGaussianObjects()) return false;
            // 初始化所有必要缓冲区
            if (!InitializeAllocBuffers()) return false;
            // 将每个Object的子数据按偏移复制到公共缓冲区
            if (!InitializeFillBuffers()) return false;
            // gs网格插值初始化
            if (!InitializeInterpolation()) return false;
            // PBD-FEM初始化
            if (!InitializePbdFem()) return false;
            // 刚体初始化
            if (!InitializeRigid()) return false;

            return true;
        }


        /// <summary>
        /// 清除所有缓冲区
        /// </summary>
        private void Dispose()
        {
            _gsPositionBuffer?.Dispose();
            _gsOtherBuffer?.Dispose();

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

            _commandBuffer?.Dispose();
        }
    }
}