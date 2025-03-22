using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using GaussianSplatting.Runtime;
using GSTestScene.Simulation;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

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
                _vertxBuffer = new ComputeBuffer(_totalVerticesCount, sizeof(float) * 3);
                _vertXBuffer = new ComputeBuffer(_totalVerticesCount, sizeof(float) * 3);
                _vertGroupBuffer = new ComputeBuffer(_totalVerticesCount, sizeof(int));

                _edgeIndicesBuffer = new ComputeBuffer(_totalEdgesCount, sizeof(int) * 2);
                _faceIndicesBuffer = new ComputeBuffer(_totalFacesCount, sizeof(int) * 3);
                _cellIndicesBuffer = new ComputeBuffer(_totalCellsCount, sizeof(int) * 4);

                _vertVelocityBuffer = new ComputeBuffer(_totalVerticesCount, sizeof(float) * 3);
                _vertForceBuffer = new ComputeBuffer(_totalVerticesCount, sizeof(float) * 3);
                _vertMassBuffer = new ComputeBuffer(_totalVerticesCount, sizeof(float));
                _vertInvMassBuffer = new ComputeBuffer(_totalVerticesCount, sizeof(float));
                _vertNewXBuffer = new ComputeBuffer(_totalVerticesCount, sizeof(float) * 3);
                _vertDeltaPosBuffer = new ComputeBuffer(_totalVerticesCount, sizeof(float) * 3);
                _vertSelectedIndicesBuffer = new ComputeBuffer(_totalVerticesCount, sizeof(int));
                _rigidVertGroupBuffer = new ComputeBuffer(_totalVerticesCount, sizeof(int));
                _cellMultiplierBuffer = new ComputeBuffer(_totalCellsCount, sizeof(float));
                _cellDsInvBuffer = new ComputeBuffer(_totalCellsCount, sizeof(float) * 9);
                _cellVolumeInitBuffer = new ComputeBuffer(_totalCellsCount, sizeof(float));
                _cellDensityBuffer = new ComputeBuffer(_totalCellsCount, sizeof(float));
                _cellMuBuffer = new ComputeBuffer(_totalCellsCount, sizeof(float));
                _cellLambdaBuffer = new ComputeBuffer(_totalCellsCount, sizeof(float));

                _rigidMassBuffer = new ComputeBuffer(_gaussianObjects.Count, sizeof(double));
                _rigidMassCenterInitBuffer = new ComputeBuffer(_gaussianObjects.Count, sizeof(double) * 3);
                _rigidMassCenterBuffer = new ComputeBuffer(_gaussianObjects.Count, sizeof(double) * 3);
                _rigidAngleVelocityMatrixBuffer = new ComputeBuffer(_gaussianObjects.Count, sizeof(double) * 9);
                _rigidRotationMatrixBuffer = new ComputeBuffer(_gaussianObjects.Count, sizeof(double) * 9);

                _triangleAabbsBuffer = new ComputeBuffer(_totalFacesCount, Marshal.SizeOf<LbvhAABBBoundingBox>());
                _sortedTriangleAabbsBuffer =
                    new ComputeBuffer(_totalFacesCount, Marshal.SizeOf<LbvhAABBBoundingBox>());
                _partialAabbBuffer =
                    new ComputeBuffer(PartialAABBCount, Marshal.SizeOf<LbvhAABBBoundingBox>());
                _partialAabbData =
                    new NativeArray<LbvhAABBBoundingBox>(Marshal.SizeOf<LbvhAABBBoundingBox>() * PartialAABBCount,
                        Allocator.Persistent);
                _mortonCodeBuffer = new ComputeBuffer(_totalFacesCount, sizeof(ulong));
                _sortedMortonCodeBuffer = new ComputeBuffer(_totalFacesCount, sizeof(ulong));
                _indicesBuffer = new ComputeBuffer(_totalFacesCount, sizeof(int));
                _sortedIndicesBuffer = new ComputeBuffer(_totalFacesCount, sizeof(int));
                _faceFlagsBuffer = new ComputeBuffer(_totalFacesCount, sizeof(int));

                _collisionPairsBuffer = new ComputeBuffer(MaxCollisionPairs, Marshal.SizeOf<int2>());
                _totalPairsBuffer = new ComputeBuffer(1, sizeof(int));
                _exactCollisionPairsBuffer = new ComputeBuffer(MaxCollisionPairs, Marshal.SizeOf<int4>());
                _totalExactPairsBuffer = new ComputeBuffer(1, sizeof(int));

                _covBuffer = new ComputeBuffer(_totalGsCount, sizeof(float) * 9);
                _localTetXBuffer = new ComputeBuffer(_totalGsCount, sizeof(float) * 12);
                _localTetWBuffer = new ComputeBuffer(_totalGsCount, sizeof(float) * 3);
                _globalTetIdxBuffer = new ComputeBuffer(_totalGsCount, sizeof(int) * 4);
                _globalTetWBuffer = new ComputeBuffer(_totalGsCount, sizeof(float) * 12);
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
                // 传递所需数据
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
                int elementStride // 元素步长(如顶点3=float3)
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
        /// 初始化每个GaussianObject的属性，在此基础上初始化整个Simulator的公共属性
        /// </summary>
        private bool InitializeSimulationParams()
        {
            // 初始化所有GS物体
            if (!InitializeGaussianObjects()) return false;
            // 初始化所有必要缓冲区
            if (!InitializeAllocBuffers()) return false;
            // todo: 执行Radix排序

            // todo:将每个Object的子数据按偏移复制到公共缓冲区
            if (!InitializeFillBuffers()) return false;
            // todo:gs网格插值初始化

            // todo:PBD-FEM初始化

            // todo:刚体初始化
            return true;
        }

        /// <summary>
        ///更新选定顶点的缓冲区
        /// </summary>
        private void UpdateSelectVertices()
        {
            int threadGroups = Mathf.CeilToInt(_vertxBuffer.count / ThreadCount);
            // 鼠标没有按下时，清空选定的顶点
            if (_currentMousePos == Vector2.positiveInfinity)
            {
                simulateShader.SetBuffer(cleanSelectionKernel, _vertSelectedIndicesBufferId,
                    _vertSelectedIndicesBuffer);
                simulateShader.Dispatch(cleanSelectionKernel, threadGroups, 1, 1);
            }
            // 否则，更新选定的顶点
            else
            {
                simulateShader.SetFloat(_controllerRadiusId, ControllerRadius);
                simulateShader.SetVector(_controllerPositionId, _currentMousePosWorld);
                simulateShader.SetBuffer(selectVerticesKernel, _vertxBufferId, _vertxBuffer);
                simulateShader.SetBuffer(selectVerticesKernel, _vertSelectedIndicesBufferId,
                    _vertSelectedIndicesBuffer);
                simulateShader.Dispatch(selectVerticesKernel, threadGroups, 1, 1);
            }
        }
    }
}