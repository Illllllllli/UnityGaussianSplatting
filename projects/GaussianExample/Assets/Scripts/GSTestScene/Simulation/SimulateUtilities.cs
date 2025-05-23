﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace GSTestScene.Simulation
{
    /// <summary>
    /// 存储物理模拟相关方法
    /// </summary>
    public partial class GaussianSimulator
    {
        /// <summary>
        /// 向上整除
        /// </summary>
        /// <param name="dividend">被除数</param>
        /// <param name="divider">除数</param>
        /// <returns></returns>
        private static int CeilDivide(int dividend, int divider)
        {
            return (dividend + divider - 1) / divider;
        }

        /// <summary>
        /// 获取当前整数的下一个2的幂次
        /// </summary>
        /// <param name="x">当前整数</param>
        /// <returns>返回离x最近的下一个2的幂次</returns>
        private int next_pow_2(int x)
        {
            --x;
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            return ++x;
        }

        /// <summary>
        /// 向命令缓冲区中设定缓冲区参数并更新_lastBuffer
        /// </summary>
        /// <param name="computeShader">计算着色器</param>
        /// <param name="kernalIndex">内核函数索引</param>
        /// <param name="bufferId">缓冲区索引</param>
        /// <param name="buffer">缓冲区</param>
        private void SetShaderComputeBuffer(ComputeShader computeShader, int kernalIndex, int bufferId,
            ComputeBuffer buffer)
        {
            _lastBuffer = buffer;
            _commandBuffer.SetComputeBufferParam(computeShader, kernalIndex, bufferId, buffer);
        }

        /// <summary>
        /// 测试当前buffer的任务是否完成了
        /// </summary>
        /// <param name="computeBuffer">要测试的缓冲区</param>
        /// <param name="func">要执行的方法</param>
        private void TestBufferFinish<T>(ComputeBuffer computeBuffer, Action<NativeArray<T>> func = null)
            where T : struct
        {
            _commandBuffer.RequestAsyncReadback(computeBuffer, request =>
            {
                if (request.hasError) return;
                var values = request.GetData<T>();
                func?.Invoke(values);
            });
        }

        /// <summary>
        /// 向GPU提交_commandBuffer中的任务并同步等待完成
        /// </summary>
        private void SubmitTaskAndSynchronize()
        {
            if (_lastBuffer != null)
            {
                _commandBuffer.RequestAsyncReadback(_lastBuffer, _ => { }); //监视最新的缓冲区
                _commandBuffer.WaitAllAsyncReadbackRequests();
                _lastBuffer = null;
            }

            Graphics.ExecuteCommandBuffer(_commandBuffer);
            _commandBuffer.Clear();
        }

        /// <summary>
        /// 给存储无符号整形数据的ComputeBuffer初始化为固定值
        /// </summary>
        /// <param name="value">值</param>
        /// <param name="length">初始化长度</param>
        /// <param name="offset">偏移量</param>
        /// <param name="computeBuffer">缓冲区</param>
        private static void SetBufferValue(uint value, int length, int offset, ComputeBuffer computeBuffer)
        {
            using NativeArray<uint> array = new NativeArray<uint>(length, Allocator.TempJob);
            FillJob<uint> fillJob = new FillJob<uint> { Data = array, Value = value };
            fillJob.Schedule(length, InnerBatchCount).Complete();
            computeBuffer.SetData(array, 0, offset, length);
        }

        /// <summary>
        /// 按偏移量和长度复制ComputeBuffer
        /// </summary>
        /// <param name="sourceBuffer">数据源缓冲区</param>
        /// <param name="destBuffer">目标缓冲区</param>
        /// <param name="sourceOffset">源缓冲区偏移量（按元素）</param>
        /// <param name="destOffset">目标缓冲区偏移量（按元素）</param>
        /// <param name="elementSize">元素大小（按字节）</param>
        /// <param name="copyLength">拷贝长度（按元素）</param>
        /// <returns></returns>
        private void CopyComputeBuffer(ComputeBuffer sourceBuffer, ComputeBuffer destBuffer, int sourceOffset,
            int destOffset, int elementSize, int copyLength)
        {
            int gridSize = CeilDivide(copyLength, CopyBlockSize);
            // 设置 ComputeShader 参数到 CommandBuffer
            SetShaderComputeBuffer(copyShader, copyBufferKernel, _copySrcBufferId, sourceBuffer);
            SetShaderComputeBuffer(copyShader, copyBufferKernel, _copyDstBufferId, destBuffer);
            _commandBuffer.SetComputeIntParam(copyShader, _copySrcOffsetId, sourceOffset);
            _commandBuffer.SetComputeIntParam(copyShader, _copyDstOffsetId, destOffset);
            _commandBuffer.SetComputeIntParam(copyShader, _copyElementSizeId, elementSize);
            _commandBuffer.SetComputeIntParam(copyShader, _copyLengthId, copyLength);
            // 调度计算任务
            _commandBuffer.DispatchCompute(copyShader, copyBufferKernel, gridSize, 1, 1);
        }


        /// <summary>
        /// 对缓冲区数据进行Blelloch扫描
        /// </summary>
        /// <param name="sourceBuffer">用作各个分区的公共缓冲区</param>
        /// <param name="outBufferOffset">输出缓冲区的偏移量</param>
        /// <param name="inBufferOffset">输入缓冲区的偏移量</param>
        /// <param name="tempBufferOffset">临时缓冲区的偏移量</param>
        /// <param name="elementCount">元素个数</param>
        void SumScanBlelloch(ComputeBuffer sourceBuffer, int outBufferOffset, int inBufferOffset,
            int tempBufferOffset, int elementCount)
        {
            int blockSize = RadixSortBlockSize / 2;
            int maxElementsPerBlock = blockSize * 2;
            int gridSize = CeilDivide(elementCount, maxElementsPerBlock);
            // 共享内存大小（以uint计）
            int sharedMemorySize = maxElementsPerBlock + (maxElementsPerBlock >> BlellochLogNumBanks);
            // 给临时分区置0
            int blockSumsBufferOffset = tempBufferOffset;
            sourceBuffer.SetData(new uint[gridSize], 0, blockSumsBufferOffset, gridSize);
            PreScan(gridSize, outBufferOffset, inBufferOffset, blockSumsBufferOffset, elementCount);
            if (gridSize <= maxElementsPerBlock)
            {
                int dummyBlockSumsOffset = blockSumsBufferOffset + gridSize;
                sourceBuffer.SetData(new uint[1], 0, dummyBlockSumsOffset, 1);
                PreScan(1, blockSumsBufferOffset, blockSumsBufferOffset, dummyBlockSumsOffset, gridSize);
            }
            else
            {
                int inBlockSumsBufferOffset = blockSumsBufferOffset + gridSize;
                CopyComputeBuffer(sourceBuffer, sourceBuffer, blockSumsBufferOffset, inBlockSumsBufferOffset,
                    sizeof(uint), gridSize);
                SumScanBlelloch(sourceBuffer, blockSumsBufferOffset, inBlockSumsBufferOffset,
                    inBlockSumsBufferOffset + gridSize, gridSize);
            }

            AddBlockSums();
            return;

            // 局部方法用于调用核函数
            void PreScan(int gridSize, int outBufferOffset, int inBufferOffset, int blockSumsBufferOffset, int length)
            {
                SetShaderComputeBuffer(radixShader, blellochPreScanKernel, _radixTempBufferId,
                    sourceBuffer);
                _commandBuffer.SetComputeIntParam(radixShader, _blellochOutBufferOffsetId, outBufferOffset);
                _commandBuffer.SetComputeIntParam(radixShader, _blellochInBufferOffsetId, inBufferOffset);
                _commandBuffer.SetComputeIntParam(radixShader, _blellochBlockSumsBufferOffsetId, blockSumsBufferOffset);
                _commandBuffer.SetComputeIntParam(radixShader, _blellochLengthId, length);
                _commandBuffer.SetComputeIntParam(radixShader, _preScanSharedDataSizeId, sharedMemorySize);
                _commandBuffer.DispatchCompute(radixShader, blellochPreScanKernel, gridSize, 1, 1);
            }

            void AddBlockSums()
            {
                SetShaderComputeBuffer(radixShader, blellochAddBlockSumsKernel, _radixTempBufferId,
                    sourceBuffer);
                _commandBuffer.SetComputeIntParam(radixShader, _blellochOutBufferOffsetId, 0);
                _commandBuffer.SetComputeIntParam(radixShader, _blellochInBufferOffsetId, 0);
                _commandBuffer.SetComputeIntParam(radixShader, _blellochBlockSumsBufferOffsetId, blockSumsBufferOffset);
                // 注：这里复用PreScan的blellochLength
                _commandBuffer.SetComputeIntParam(radixShader, _blellochLengthId, elementCount);
                _commandBuffer.DispatchCompute(radixShader, blellochAddBlockSumsKernel, gridSize, 1, 1);
            }
        }

        /// <summary>
        /// 基数排序主方法
        /// </summary>
        /// <param name="keyOutBuffer">键输出缓冲区</param>
        /// <param name="keyInBuffer">键输入缓冲区</param>
        /// <param name="valueOutBuffer">值输出缓冲区</param>
        /// <param name="valueInBuffer">值输入缓冲区</param>
        /// <param name="tempBuffer">临时（排序）缓冲区</param>
        /// <param name="keyInLength">键的数量</param>
        private bool RadixSort<TKey, TValue>(ComputeBuffer keyOutBuffer, ComputeBuffer keyInBuffer,
            ComputeBuffer valueOutBuffer,
            ComputeBuffer valueInBuffer, ComputeBuffer tempBuffer, int keyInLength)
        {
            // tmp_buffer_size->lbvhSortBufferSize
            int maxElementsPerBlock = RadixSortBlockSize;
            int gridSize = CeilDivide(keyInLength, maxElementsPerBlock);
            // 将一个临时块分成4个部分
            int prefixSumsLength = keyInLength;
            int blockSumsLength = 4 * gridSize;
            int scanBlockSumsLength = blockSumsLength;

            int prefixSumOffset = 0;
            int blockSumsOffset = prefixSumsLength;
            int scanBlockSumsOffset = blockSumsLength + blockSumsOffset;
            int scanTempBufferOffset = scanBlockSumsOffset + scanBlockSumsLength;

            int tempBufferSize = (prefixSumsLength + blockSumsLength * 2) + (prefixSumsLength * 4);
            if (_lbvhSortBufferSize == 0)
            {
                _lbvhSortBufferSize = tempBufferSize;
                return true;
            }

            if (tempBufferSize > _lbvhSortBufferSize) return false;

            //给前三个分区置0
            tempBuffer.SetData(new uint[scanTempBufferOffset], 0, 0, scanTempBufferOffset);

            int maskOutLength = maxElementsPerBlock + 1;
            int mergedScanMaskOutLength = maxElementsPerBlock;
            int maskOutSumsLength = 4;
            int scanMaskOutSumsLength = 4;
            // 共享内存大小（以uint计）
            int sharedMemorySize = maskOutLength + mergedScanMaskOutLength + maskOutSumsLength + scanMaskOutSumsLength;
            // 计算键类型大小（以位为单位）
            int bitNum = Marshal.SizeOf<TKey>() * 8;
            // block-wise radix sort (write blocks back to global memory)
            for (int shiftWidth = 0; shiftWidth <= bitNum - 2; shiftWidth += 2)
            {
                RadixSortLocal(shiftWidth);
                // 测试用
                // TestBufferFinish<ulong>(keyInBuffer);
                // SubmitTaskAndSynchronize();

                SumScanBlelloch(tempBuffer, scanBlockSumsOffset, blockSumsOffset, scanTempBufferOffset,
                    blockSumsLength);
                GlobalShuffle(shiftWidth);
            }

            // 复制结果到输出缓冲区
            CopyComputeBuffer(keyInBuffer, keyOutBuffer, 0, 0, Marshal.SizeOf<TKey>(), keyInLength);
            CopyComputeBuffer(valueInBuffer, valueOutBuffer, 0, 0, Marshal.SizeOf<TValue>(), keyInLength);
            return true;

            // 局部方法用于调用核函数
            void RadixSortLocal(int shiftWidth)
            {
                SetShaderComputeBuffer(radixShader, radixSortLocalKernel, _radixKeyInId, keyInBuffer);
                SetShaderComputeBuffer(radixShader, radixSortLocalKernel, _radixKeyOutId, keyOutBuffer);
                SetShaderComputeBuffer(radixShader, radixSortLocalKernel, _radixValueInId, valueInBuffer);
                SetShaderComputeBuffer(radixShader, radixSortLocalKernel, _radixValueOutId,
                    valueOutBuffer);
                SetShaderComputeBuffer(radixShader, radixSortLocalKernel, _radixTempBufferId, tempBuffer);
                _commandBuffer.SetComputeIntParam(radixShader, _gridSizeId, gridSize);
                _commandBuffer.SetComputeIntParam(radixShader, _prefixSumsOffsetId, prefixSumOffset);
                _commandBuffer.SetComputeIntParam(radixShader, _blockSumsOffsetId, blockSumsOffset);
                _commandBuffer.SetComputeIntParam(radixShader, _radixShiftWidthId, shiftWidth);
                _commandBuffer.SetComputeIntParam(radixShader, _radixKeyInLengthId, keyInLength);
                _commandBuffer.SetComputeIntParam(radixShader, _sortLocalSharedMemorySizeId, sharedMemorySize);
                _commandBuffer.DispatchCompute(radixShader, radixSortLocalKernel, gridSize, 1, 1);
            }

            void GlobalShuffle(int shiftWidth)
            {
                SetShaderComputeBuffer(radixShader, radixGlobalShuffleKernel, _radixKeyInId, keyInBuffer);
                SetShaderComputeBuffer(radixShader, radixGlobalShuffleKernel, _radixKeyOutId,
                    keyOutBuffer);
                SetShaderComputeBuffer(radixShader, radixGlobalShuffleKernel, _radixValueInId,
                    valueInBuffer);
                SetShaderComputeBuffer(radixShader, radixGlobalShuffleKernel, _radixValueOutId,
                    valueOutBuffer);
                _commandBuffer.SetComputeIntParam(radixShader, _gridSizeId, gridSize);
                _commandBuffer.SetComputeIntParam(radixShader, _prefixSumsOffsetId, prefixSumOffset);
                _commandBuffer.SetComputeIntParam(radixShader, _scanBlockSumsOffsetId, scanBlockSumsOffset);
                _commandBuffer.SetComputeIntParam(radixShader, _radixShiftWidthId, shiftWidth);
                _commandBuffer.SetComputeIntParam(radixShader, _radixKeyInLengthId, keyInLength);
                _commandBuffer.DispatchCompute(radixShader, radixGlobalShuffleKernel, gridSize, 1, 1);
            }
        }

        /// <summary>
        ///更新选定顶点的缓冲区
        /// </summary>
        private void UpdateSelectVertices()
        {
            int gridSize = CeilDivide(_vertxBuffer.count, SimulateBlockSize);
            // 鼠标没有按下时，清空选定的顶点
            if (!_isMousePressed || float.IsNaN(_controllerVelocity.magnitude))
            {
                SetShaderComputeBuffer(simulateShader, cleanSelectionKernel, _vertSelectedIndicesBufferId,
                    _vertSelectedIndicesBuffer);
                _commandBuffer.DispatchCompute(simulateShader, cleanSelectionKernel, gridSize, 1, 1);
            }
            // 否则，更新选定的顶点
            else
            {
                //todo:由于线程组间不能同步而且没办法把所有顶点塞到一个线程组内，因此正确的做法是先执行计算最小距离的核函数，然后再执行并行选择顶点的函数
                _commandBuffer.SetComputeIntParam(simulateShader, _verticesTotalCountId, _totalVerticesCount);
                _commandBuffer.SetComputeFloatParam(simulateShader, _controllerRadiusId, controllerRadius);
                _commandBuffer.SetComputeVectorParam(simulateShader, _controllerDirectionId, _controllerDirection);
                _commandBuffer.SetComputeVectorParam(simulateShader, _cameraPositionId, _cameraPosition);
                SetShaderComputeBuffer(simulateShader, selectVerticesKernel, _selectedMinDistBufferId,
                    _selectedMinDistBuffer);
                SetShaderComputeBuffer(simulateShader, selectVerticesKernel, _vertxBufferId,
                    _vertxBuffer);
                SetShaderComputeBuffer(simulateShader, selectVerticesKernel, _vertSelectedIndicesBufferId,
                    _vertSelectedIndicesBuffer);
                _commandBuffer.DispatchCompute(simulateShader, selectVerticesKernel, gridSize, 1, 1);
                TestBufferFinish<int>(_vertSelectedIndicesBuffer, arr =>
                {
                    int count = arr.Count(d => d == 1);
                    // Debug.Log(count);
                });
            }


            SubmitTaskAndSynchronize();
        }

        /// <summary>
        /// 初始化协方差矩阵
        /// </summary>
        private void InitializeCovariance()
        {
            int gridSize = CeilDivide(_totalGsCount, SimulateBlockSize);
            SetShaderComputeBuffer(simulateShader, initializeCovarianceKernel, _gsOtherBufferId,
                _gsOtherBuffer);
            SetShaderComputeBuffer(simulateShader, initializeCovarianceKernel, _covBufferId, _covBuffer);
            _commandBuffer.SetComputeIntParam(simulateShader, _gsTotalCountId, _totalGsCount);
            _commandBuffer.DispatchCompute(simulateShader, initializeCovarianceKernel, gridSize, 1, 1);
        }

        /// <summary>
        /// 获取GS局部嵌入四面体
        /// </summary>
        private void GetLocalEmbededTets()
        {
            int gridSize = CeilDivide(_totalGsCount, SimulateBlockSize);
            SetShaderComputeBuffer(simulateShader, getLocalEmbededTetsKernel, _gsPositionBufferId,
                _gsPositionBuffer);
            SetShaderComputeBuffer(simulateShader, getLocalEmbededTetsKernel, _covBufferId, _covBuffer);
            SetShaderComputeBuffer(simulateShader, getLocalEmbededTetsKernel, _localTetXBufferId,
                _localTetXBuffer);
            SetShaderComputeBuffer(simulateShader, getLocalEmbededTetsKernel, _localTetWBufferId,
                _localTetWBuffer);
            _commandBuffer.SetComputeIntParam(simulateShader, _gsTotalCountId, _totalGsCount);
            _commandBuffer.DispatchCompute(simulateShader, getLocalEmbededTetsKernel, gridSize, 1, 1);
        }

        /// <summary>
        /// 获取GS全局嵌入四面体
        /// <param name="gaussianObject">GS物体</param>
        /// <param name="tetId">四面体ID</param>
        /// </summary>
        private void GetGlobalEmbededTet(GaussianObject gaussianObject)
        {
            int gridSize = CeilDivide(_totalGsCount * 4, SimulateBlockSize);
            SetShaderComputeBuffer(simulateShader, getGlobalEmbededTetKernel, _vertXBufferId,
                _vertXBuffer);
            SetShaderComputeBuffer(simulateShader, getGlobalEmbededTetKernel, _gsPositionBufferId,
                _gsPositionBuffer); //test
            SetShaderComputeBuffer(simulateShader, getGlobalEmbededTetKernel, _cellIndicesBufferId,
                _cellIndicesBuffer);
            SetShaderComputeBuffer(simulateShader, getGlobalEmbededTetKernel, _localTetXBufferId,
                _localTetXBuffer);
            SetShaderComputeBuffer(simulateShader, getGlobalEmbededTetKernel, _globalTetIdxBufferId,
                _globalTetIdxBuffer);
            SetShaderComputeBuffer(simulateShader, getGlobalEmbededTetKernel, _globalTetWBufferId,
                _globalTetWBuffer);
            _commandBuffer.SetComputeIntParam(simulateShader, _cellLocalCountId, gaussianObject.cellsCount);
            _commandBuffer.SetComputeIntParam(simulateShader, _cellLocalOffsetId, gaussianObject.CellsOffset);
            _commandBuffer.SetComputeIntParam(simulateShader, _gsLocalCountId, gaussianObject.gsNums);
            _commandBuffer.SetComputeIntParam(simulateShader, _gsLocalOffsetId, gaussianObject.GsOffset);
            _commandBuffer.DispatchCompute(simulateShader, getGlobalEmbededTetKernel, gridSize, 1, 1);
        }

        /// <summary>
        /// 初始化FEM模拟
        /// </summary>
        private void InitializeFemBases()
        {
            int gridSize = CeilDivide(_totalCellsCount, SimulateBlockSize);
            SetShaderComputeBuffer(simulateShader, initFemBasesKernel, _cellDensityBufferId, _cellDensityBuffer);
            SetShaderComputeBuffer(simulateShader, initFemBasesKernel, _cellIndicesBufferId, _cellIndicesBuffer);
            SetShaderComputeBuffer(simulateShader, initFemBasesKernel, _vertXBufferId, _vertXBuffer);
            SetShaderComputeBuffer(simulateShader, initFemBasesKernel, _vertMassBufferId, _vertMassBuffer);
            SetShaderComputeBuffer(simulateShader, initFemBasesKernel, _cellDsInvBufferId, _cellDsInvBuffer);
            SetShaderComputeBuffer(simulateShader, initFemBasesKernel, _cellVolumeInitBufferId, _cellVolumeInitBuffer);
            _commandBuffer.SetComputeIntParam(simulateShader, _cellTotalCountId, _totalCellsCount);
            _commandBuffer.DispatchCompute(simulateShader, initFemBasesKernel, gridSize, 1, 1);
        }

        /// <summary>
        /// 初始化逆质量
        /// </summary>
        private void InitializeInvMass()
        {
            int gridSize = CeilDivide(_totalVerticesCount, SimulateBlockSize);
            SetShaderComputeBuffer(simulateShader, initInvMassKernel, _vertXBufferId, _vertXBuffer);
            SetShaderComputeBuffer(simulateShader, initInvMassKernel, _vertMassBufferId, _vertMassBuffer);
            SetShaderComputeBuffer(simulateShader, initInvMassKernel, _vertInvMassBufferId, _vertInvMassBuffer);
            _commandBuffer.SetComputeIntParam(simulateShader, _verticesTotalCountId, _totalVerticesCount);
            _commandBuffer.SetComputeIntParam(simulateShader, _boundaryId, boundary);
            _commandBuffer.DispatchCompute(simulateShader, initInvMassKernel, gridSize, 1, 1);
        }

        /// <summary>
        /// 初始化刚体系统
        /// </summary>
        private void InitRigid()
        {
            int gridSize = CeilDivide(_totalVerticesCount, SimulateBlockSize);
            SetShaderComputeBuffer(simulateShader, initRigidKernel, _vertXBufferId, _vertXBuffer);
            SetShaderComputeBuffer(simulateShader, initRigidKernel, _rigidMassBufferId, _rigidMassBuffer);
            SetShaderComputeBuffer(simulateShader, initRigidKernel, _rigidMassCenterInitBufferId,
                _rigidMassCenterInitBuffer);
            SetShaderComputeBuffer(simulateShader, initRigidKernel, _rigidVertGroupBufferId, _rigidVertGroupBuffer);
            SetShaderComputeBuffer(simulateShader, initRigidKernel, _vertMassBufferId, _vertMassBuffer);
            _commandBuffer.SetComputeIntParam(simulateShader, _verticesTotalCountId, _totalVerticesCount);
            _commandBuffer.DispatchCompute(simulateShader, initRigidKernel, gridSize, 1, 1);
        }

        /// <summary>
        /// 计算三角面AABB包围盒
        /// </summary>
        private void ComputeTriangleAabbs(float extendedDist = 0)
        {
            int gridSize = CeilDivide(_totalFacesCount, SimulateBlockSize);
            SetShaderComputeBuffer(simulateShader, computeTriangleAabbsKernel, _vertxBufferId, _vertxBuffer);
            SetShaderComputeBuffer(simulateShader, computeTriangleAabbsKernel, _faceIndicesBufferId,
                _faceIndicesBuffer);
            SetShaderComputeBuffer(simulateShader, computeTriangleAabbsKernel, _triangleAabbsBufferId,
                _triangleAabbsBuffer);
            _commandBuffer.SetComputeIntParam(simulateShader, _faceTotalCountId, _totalFacesCount);
            _commandBuffer.SetComputeFloatParam(simulateShader, _collisionDetectionDistId, extendedDist);
            _commandBuffer.DispatchCompute(simulateShader, computeTriangleAabbsKernel, gridSize, 1, 1);
        }

        /// <summary>
        /// 计算Morton码并生成索引
        /// </summary>
        private void ComputeMortonAndIndices()
        {
            int gridSize = CeilDivide(_totalFacesCount, SimulateBlockSize);
            SetShaderComputeBuffer(simulateShader, computeMortonAndIndicesKernel, _partialAabbBufferId,
                _partialAabbBuffer);
            SetShaderComputeBuffer(simulateShader, computeMortonAndIndicesKernel, _triangleAabbsBufferId,
                _triangleAabbsBuffer);
            SetShaderComputeBuffer(simulateShader, computeMortonAndIndicesKernel, _mortonCodeBufferId,
                _mortonCodeBuffer);
            SetShaderComputeBuffer(simulateShader, computeMortonAndIndicesKernel, _indicesBufferId, _indicesBuffer);
            _commandBuffer.SetComputeIntParam(simulateShader, _faceTotalCountId, _totalFacesCount);
            _commandBuffer.DispatchCompute(simulateShader, computeMortonAndIndicesKernel, gridSize, 1, 1);
        }


        /// <summary>
        /// 获取排序后的三角面包围盒
        /// </summary>
        private void GetSortedTriangleAabbs()
        {
            int gridSize = CeilDivide(_totalFacesCount, SimulateBlockSize);
            SetShaderComputeBuffer(simulateShader, getSortedTriangleAabbsKernel, _sortedIndicesBufferId,
                _sortedIndicesBuffer);
            SetShaderComputeBuffer(simulateShader, getSortedTriangleAabbsKernel, _triangleAabbsBufferId,
                _triangleAabbsBuffer);
            SetShaderComputeBuffer(simulateShader, getSortedTriangleAabbsKernel, _sortedTriangleAabbsBufferId,
                _sortedTriangleAabbsBuffer);
            _commandBuffer.SetComputeIntParam(simulateShader, _faceTotalCountId, _totalFacesCount);
            _commandBuffer.DispatchCompute(simulateShader, getSortedTriangleAabbsKernel, gridSize, 1, 1);
        }


        /// <summary>
        /// 初始化BVH的AABB节点
        /// </summary>
        private void ResetAabb(int nodeCount)
        {
            int gridSize = CeilDivide(nodeCount, SimulateBlockSize);
            SetShaderComputeBuffer(simulateShader, resetAABBKernel, _lbvhAabbsBufferId,
                _lbvhAabbsBuffer);
            SetShaderComputeBuffer(simulateShader, resetAABBKernel, _sortedTriangleAabbsBufferId,
                _sortedTriangleAabbsBuffer);
            _commandBuffer.SetComputeIntParam(simulateShader, _faceTotalCountId, _totalFacesCount);
            _commandBuffer.DispatchCompute(simulateShader, resetAABBKernel, gridSize, 1, 1);
        }

        /// <summary>
        /// 构建LBVH内部节点
        /// </summary>
        private void ConstructInternalNodes()
        {
            int gridSize = CeilDivide(_totalFacesCount - 1, SimulateBlockSize);
            SetShaderComputeBuffer(simulateShader, constructInternalNodesKernel, _sortedMortonCodeBufferId,
                _sortedMortonCodeBuffer);
            SetShaderComputeBuffer(simulateShader, constructInternalNodesKernel, _lbvhNodesBufferId,
                _lbvhNodesBuffer);
            _commandBuffer.SetComputeIntParam(simulateShader, _faceTotalCountId, _totalFacesCount);
            _commandBuffer.DispatchCompute(simulateShader, constructInternalNodesKernel, gridSize, 1, 1);
        }

        /// <summary>
        /// 计算LBVH内部的AABB节点
        /// </summary>
        private void ComputeInternalAabbs()
        {
            int gridSize = CeilDivide(_totalFacesCount - 1, SimulateBlockSize);
            SetShaderComputeBuffer(simulateShader, computeInternalAabbsKernel, _lbvhNodesBufferId,
                _lbvhNodesBuffer);
            SetShaderComputeBuffer(simulateShader, computeInternalAabbsKernel, _lbvhAabbsBufferId,
                _lbvhAabbsBuffer);
            SetShaderComputeBuffer(simulateShader, computeInternalAabbsKernel, _faceFlagsBufferId,
                _faceFlagsBuffer);
            _commandBuffer.SetComputeIntParam(simulateShader, _faceTotalCountId, _totalFacesCount);
            _commandBuffer.DispatchCompute(simulateShader, computeInternalAabbsKernel, gridSize, 1, 1);
        }

        /// <summary>
        /// 查询碰撞对
        /// </summary>
        private void QueryCollisionPairs()
        {
            int gridSize = CeilDivide(_totalFacesCount, CullingBlockSize);
            SetShaderComputeBuffer(simulateShader, queryCollisionPairsKernel, _triangleAabbsBufferId,
                _triangleAabbsBuffer);
            SetShaderComputeBuffer(simulateShader, queryCollisionPairsKernel, _lbvhNodesBufferId,
                _lbvhNodesBuffer);
            SetShaderComputeBuffer(simulateShader, queryCollisionPairsKernel, _lbvhAabbsBufferId,
                _lbvhAabbsBuffer);
            SetShaderComputeBuffer(simulateShader, queryCollisionPairsKernel, _sortedIndicesBufferId,
                _sortedIndicesBuffer);
            SetShaderComputeBuffer(simulateShader, queryCollisionPairsKernel, _collisionPairsBufferId,
                _collisionPairsBuffer);
            SetShaderComputeBuffer(simulateShader, queryCollisionPairsKernel, _totalPairsBufferId,
                _totalPairsBuffer);
            _commandBuffer.SetComputeIntParam(simulateShader, _faceTotalCountId, _totalFacesCount);
            _commandBuffer.DispatchCompute(simulateShader, queryCollisionPairsKernel, gridSize, 1, 1);
        }


        /// <summary>
        /// 查询碰撞三角面
        /// </summary>
        private void QueryCollisionTriangles()
        {
            int gridSize = CeilDivide(_totalPairsCount[0], SimulateBlockSize);
            if (gridSize <= 0) return; //可能没有碰撞对
            SetShaderComputeBuffer(simulateShader, queryCollisionTrianglesKernel, _totalPairsBufferId,
                _totalPairsBuffer);
            SetShaderComputeBuffer(simulateShader, queryCollisionTrianglesKernel, _collisionPairsBufferId,
                _collisionPairsBuffer);
            SetShaderComputeBuffer(simulateShader, queryCollisionTrianglesKernel, _vertxBufferId,
                _vertxBuffer);
            SetShaderComputeBuffer(simulateShader, queryCollisionTrianglesKernel, _vertGroupBufferId,
                _vertGroupBuffer);
            SetShaderComputeBuffer(simulateShader, queryCollisionTrianglesKernel, _faceIndicesBufferId,
                _faceIndicesBuffer);
            SetShaderComputeBuffer(simulateShader, queryCollisionTrianglesKernel, _exactCollisionPairsBufferId,
                _exactCollisionPairsBuffer);
            SetShaderComputeBuffer(simulateShader, queryCollisionTrianglesKernel, _totalExactPairsBufferId,
                _totalExactPairsBuffer);
            _commandBuffer.SetComputeFloatParam(simulateShader, _collisionDetectionDistId, collisionMinimalDist);
            _commandBuffer.DispatchCompute(simulateShader, queryCollisionTrianglesKernel, gridSize, 1, 1);
        }

        /// <summary>
        /// 计算轴对齐包围盒的合并结果
        /// </summary>
        private void LaunchAabbReduce(int blocks, int threads)
        {
            Dictionary<int, int> kernelThreads = new Dictionary<int, int>
            {
                { 512, aabbReduce512Kernel }, { 256, aabbReduce256Kernel },
                { 128, aabbReduce128Kernel }, { 64, aabbReduce64Kernel },
                { 32, aabbReduce32Kernel }, { 16, aabbReduce16Kernel },
                { 8, aabbReduce8Kernel }, { 4, aabbReduce4Kernel },
                { 2, aabbReduce2Kernel }, { 1, aabbReduce1Kernel }
            };
            int kernel = kernelThreads[threads];
            SetShaderComputeBuffer(simulateShader, kernel, _triangleAabbsBufferId, _triangleAabbsBuffer);
            SetShaderComputeBuffer(simulateShader, kernel, _partialAabbBufferId, _partialAabbBuffer);
            _commandBuffer.SetComputeIntParam(simulateShader, _aabbGridSizeId, blocks);
            _commandBuffer.DispatchCompute(simulateShader, kernel, blocks, 1, 1);
        }

        /// <summary>
        /// 实际调用GPU应用外力
        /// </summary>
        /// <param name="dt0">步进时间</param>
        private void ApplyExternalForceCompute(float dt0)
        {
            int gridSize = CeilDivide(_totalVerticesCount, SimulateBlockSize);
            SetShaderComputeBuffer(simulateShader, applyExternalForceKernel, _vertInvMassBufferId, _vertInvMassBuffer);
            SetShaderComputeBuffer(simulateShader, applyExternalForceKernel, _vertxBufferId, _vertxBuffer);
            SetShaderComputeBuffer(simulateShader, applyExternalForceKernel, _vertVelocityBufferId,
                _vertVelocityBuffer);
            SetShaderComputeBuffer(simulateShader, applyExternalForceKernel, _vertNewXBufferId, _vertNewXBuffer);
            SetShaderComputeBuffer(simulateShader, applyExternalForceKernel, _vertSelectedIndicesBufferId,
                _vertSelectedIndicesBuffer);
            _commandBuffer.SetComputeVectorParam(simulateShader, _cameraPositionId,
                _cameraPosition.Equals(Vector3.positiveInfinity) ? Vector3.zero : _cameraPosition);
            _commandBuffer.SetComputeVectorParam(simulateShader, _controllerVelocityId,
                _controllerVelocity.Equals(Vector3.positiveInfinity) ? Vector3.zero : _controllerVelocity);
            _commandBuffer.SetComputeVectorParam(simulateShader, _controllerAngleVelocityId,
                _controllerAngularVelocity);
            _commandBuffer.SetComputeIntParam(simulateShader, _verticesTotalCountId, _totalVerticesCount);
            _commandBuffer.SetComputeFloatParam(simulateShader, _dtId, dt0);
            _commandBuffer.SetComputeFloatParam(simulateShader, _gravityId, gravity);
            _commandBuffer.SetComputeFloatParam(simulateShader, _dampingCoefficientId, dampingCoefficient);
            _commandBuffer.SetComputeIntParam(simulateShader, _zUpId, isZUp);
            _commandBuffer.DispatchCompute(simulateShader, applyExternalForceKernel, gridSize, 1, 1);
        }

        /// <summary>
        /// 实际调用GPU求解FEM
        /// </summary>
        /// <param name="dt0"></param>
        private void SolveFemConstraintsCompute(float dt0)
        {
            int gridSize = CeilDivide(_totalCellsCount, SimulateBlockSize / 2);
            SetShaderComputeBuffer(simulateShader, solveFemConstraintsKernel, _cellMuLambdaMultiplierBufferId,
                _cellMuLambdaMultiplierBuffer);
            SetShaderComputeBuffer(simulateShader, solveFemConstraintsKernel, _cellIndicesBufferId, _cellIndicesBuffer);
            SetShaderComputeBuffer(simulateShader, solveFemConstraintsKernel, _vertNewXBufferId, _vertNewXBuffer);
            SetShaderComputeBuffer(simulateShader, solveFemConstraintsKernel, _vertInvMassBufferId, _vertInvMassBuffer);
            SetShaderComputeBuffer(simulateShader, solveFemConstraintsKernel, _cellDsInvBufferId, _cellDsInvBuffer);
            SetShaderComputeBuffer(simulateShader, solveFemConstraintsKernel, _cellVolumeInitBufferId,
                _cellVolumeInitBuffer);
            SetShaderComputeBuffer(simulateShader, solveFemConstraintsKernel, _vertDeltaPosBufferId,
                _vertDeltaPosBuffer);
            SetShaderComputeBuffer(simulateShader, solveFemConstraintsKernel, _rigidVertGroupBufferId,
                _rigidVertGroupBuffer);
            _commandBuffer.SetComputeIntParam(simulateShader, _cellTotalCountId, _totalCellsCount);
            _commandBuffer.SetComputeFloatParam(simulateShader, _dtId, dt0);
            _commandBuffer.DispatchCompute(simulateShader, solveFemConstraintsKernel, gridSize, 1, 1);
        }

        /// <summary>
        /// 实际调用GPU求解三角面碰撞约束
        /// </summary>
        private void SolveTrianglePointDistanceConstraintCompute()
        {
            int gridSize = CeilDivide(_totalExactPairsCount[0], SimulateBlockSize / 2);
            SetShaderComputeBuffer(simulateShader, solveTrianglePointDistanceConstraintKernel,
                _exactCollisionPairsBufferId, _exactCollisionPairsBuffer);
            SetShaderComputeBuffer(simulateShader, solveTrianglePointDistanceConstraintKernel, _vertNewXBufferId,
                _vertNewXBuffer);
            SetShaderComputeBuffer(simulateShader, solveTrianglePointDistanceConstraintKernel, _vertInvMassBufferId,
                _vertInvMassBuffer);
            SetShaderComputeBuffer(simulateShader, solveTrianglePointDistanceConstraintKernel, _vertDeltaPosBufferId,
                _vertDeltaPosBuffer);
            SetShaderComputeBuffer(simulateShader, solveTrianglePointDistanceConstraintKernel, _totalExactPairsBufferId,
                _totalExactPairsBuffer);
            _commandBuffer.SetComputeFloatParam(simulateShader, _collisionDetectionDistId, collisionMinimalDist);
            _commandBuffer.SetComputeFloatParam(simulateShader, _collisionStiffnessId, collisionStiffness);
            _commandBuffer.DispatchCompute(simulateShader, solveTrianglePointDistanceConstraintKernel, gridSize, 1, 1);
        }

        /// <summary>
        /// 实际调用GPU求解步进过程中的顶点位置更新
        /// </summary>
        private void PbdPostSolveCompute()
        {
            int gridSize = CeilDivide(_totalVerticesCount, SimulateBlockSize * 4);
            SetShaderComputeBuffer(simulateShader, pbdPostSolveKernel, _vertDeltaPosBufferId, _vertDeltaPosBuffer);
            SetShaderComputeBuffer(simulateShader, pbdPostSolveKernel, _vertNewXBufferId, _vertNewXBuffer);
            SetShaderComputeBuffer(simulateShader, pbdPostSolveKernel, _vertSelectedIndicesBufferId,
                _vertSelectedIndicesBuffer);
            _commandBuffer.SetComputeIntParam(simulateShader, _verticesTotalCountId, _totalVerticesCount);
            _commandBuffer.DispatchCompute(simulateShader, pbdPostSolveKernel, gridSize, 1, 1);
        }

        /// <summary>
        /// 实际调用GPU将子步长的计算结果应用到顶点位置，完成时间步进
        /// </summary>
        private void PbdAdvanceCompute(float dt0)
        {
            int gridSize = CeilDivide(_totalVerticesCount, SimulateBlockSize);
            SetShaderComputeBuffer(simulateShader, pbdAdvanceKernel, _vertInvMassBufferId, _vertInvMassBuffer);
            SetShaderComputeBuffer(simulateShader, pbdAdvanceKernel, _vertVelocityBufferId, _vertVelocityBuffer);
            SetShaderComputeBuffer(simulateShader, pbdAdvanceKernel, _vertxBufferId, _vertxBuffer);
            SetShaderComputeBuffer(simulateShader, pbdAdvanceKernel, _vertNewXBufferId, _vertNewXBuffer);
            _commandBuffer.SetComputeIntParam(simulateShader, _verticesTotalCountId, _totalVerticesCount);
            _commandBuffer.SetComputeIntParam(simulateShader, _zUpId, isZUp);
            _commandBuffer.SetComputeFloatParam(simulateShader, _dtId, dt0);
            _commandBuffer.SetComputeFloatParam(simulateShader, _groundHeightId, groundHeight);
            _commandBuffer.DispatchCompute(simulateShader, pbdAdvanceKernel, gridSize, 1, 1);
        }

        /// <summary>
        /// 调用GPU更新刚体质心
        /// </summary>
        private void SolveRigidInitMassCenter()
        {
            int gridSize = CeilDivide(_totalVerticesCount, SimulateBlockSize);
            SetShaderComputeBuffer(simulateShader, solveRigidInitMassCenterKernel, _rigidMassCenterBufferId,
                _rigidMassCenterBuffer);
            SetShaderComputeBuffer(simulateShader, solveRigidInitMassCenterKernel, _rigidVertGroupBufferId,
                _rigidVertGroupBuffer);
            SetShaderComputeBuffer(simulateShader, solveRigidInitMassCenterKernel, _vertxBufferId, _vertxBuffer);
            SetShaderComputeBuffer(simulateShader, solveRigidInitMassCenterKernel, _vertMassBufferId, _vertMassBuffer);
            _commandBuffer.SetComputeIntParam(simulateShader, _verticesTotalCountId, _totalVerticesCount);
            _commandBuffer.DispatchCompute(simulateShader, solveRigidInitMassCenterKernel, gridSize, 1, 1);
        }

        /// <summary>
        /// 调用GPU更新刚体角速度矩阵
        /// </summary>
        private void SolveRigidComputeA()
        {
            int gridSize = CeilDivide(_totalVerticesCount, SimulateBlockSize);
            SetShaderComputeBuffer(simulateShader, solveRigidComputeAKernel, _rigidAngleVelocityMatrixBufferId,
                _rigidAngleVelocityMatrixBuffer);
            SetShaderComputeBuffer(simulateShader, solveRigidComputeAKernel, _rigidVertGroupBufferId,
                _rigidVertGroupBuffer);
            SetShaderComputeBuffer(simulateShader, solveRigidComputeAKernel, _rigidMassBufferId,
                _rigidMassBuffer);
            SetShaderComputeBuffer(simulateShader, solveRigidComputeAKernel, _rigidMassCenterBufferId,
                _rigidMassCenterBuffer);
            SetShaderComputeBuffer(simulateShader, solveRigidComputeAKernel, _rigidMassCenterInitBufferId,
                _rigidMassCenterInitBuffer);
            SetShaderComputeBuffer(simulateShader, solveRigidComputeAKernel, _vertxBufferId,
                _vertxBuffer);
            SetShaderComputeBuffer(simulateShader, solveRigidComputeAKernel, _vertXBufferId,
                _vertXBuffer);
            SetShaderComputeBuffer(simulateShader, solveRigidComputeAKernel, _vertMassBufferId,
                _vertMassBuffer);
            _commandBuffer.SetComputeIntParam(simulateShader, _verticesTotalCountId, _totalVerticesCount);
            _commandBuffer.DispatchCompute(simulateShader, solveRigidComputeAKernel, gridSize, 1, 1);
        }

        /// <summary>
        /// 调用GPU更新刚体旋转矩阵
        /// </summary>
        private void SolveRigidComputeR()
        {
            int gridSize = CeilDivide(_gaussianObjects.Count, SimulateBlockSize);
            SetShaderComputeBuffer(simulateShader, solveRigidComputeRKernel, _rigidAngleVelocityMatrixBufferId,
                _rigidAngleVelocityMatrixBuffer);
            SetShaderComputeBuffer(simulateShader, solveRigidComputeRKernel, _rigidRotationMatrixBufferId,
                _rigidRotationMatrixBuffer);
            _commandBuffer.SetComputeIntParam(simulateShader, _verticesTotalCountId, _totalVerticesCount);
            _commandBuffer.SetComputeIntParam(simulateShader, _gsObjectCountId, _gaussianObjects.Count);
            _commandBuffer.DispatchCompute(simulateShader, solveRigidComputeRKernel, gridSize, 1, 1);
        }

        /// <summary>
        /// 调用GPU更新刚体位置
        /// </summary>
        private void SolveRigidUpdateX()
        {
            int gridSize = CeilDivide(_totalVerticesCount, SimulateBlockSize);
            SetShaderComputeBuffer(simulateShader, solveRigidUpdateXKernel, _rigidVertGroupBufferId,
                _rigidVertGroupBuffer);
            SetShaderComputeBuffer(simulateShader, solveRigidUpdateXKernel, _rigidRotationMatrixBufferId,
                _rigidRotationMatrixBuffer);
            SetShaderComputeBuffer(simulateShader, solveRigidUpdateXKernel, _vertxBufferId, _vertxBuffer);
            SetShaderComputeBuffer(simulateShader, solveRigidUpdateXKernel, _vertXBufferId, _vertXBuffer);
            SetShaderComputeBuffer(simulateShader, solveRigidUpdateXKernel, _rigidMassCenterBufferId,
                _rigidMassCenterBuffer);
            SetShaderComputeBuffer(simulateShader, solveRigidUpdateXKernel, _rigidMassCenterInitBufferId,
                _rigidMassCenterInitBuffer);
            SetShaderComputeBuffer(simulateShader, solveRigidUpdateXKernel, _rigidMassBufferId, _rigidMassBuffer);
            _commandBuffer.SetComputeIntParam(simulateShader, _verticesTotalCountId, _totalVerticesCount);
            _commandBuffer.DispatchCompute(simulateShader, solveRigidUpdateXKernel, gridSize, 1, 1);
        }

        /// <summary>
        /// 实际调用GPU计算GS网格插值更新结果
        /// </summary>
        private void ApplyInterpolationCompute()
        {
            int gridSize = CeilDivide(_totalGsCount, SimulateBlockSize);
            // 第一部分
            SetShaderComputeBuffer(simulateShader, applyInterpolationIKernel, _gsPositionBufferId, _gsPositionBuffer);
            SetShaderComputeBuffer(simulateShader, applyInterpolationIKernel, _cellIndicesBufferId, _cellIndicesBuffer);
            SetShaderComputeBuffer(simulateShader, applyInterpolationIKernel, _vertXBufferId, _vertXBuffer);
            SetShaderComputeBuffer(simulateShader, applyInterpolationIKernel, _vertxBufferId, _vertxBuffer);
            SetShaderComputeBuffer(simulateShader, applyInterpolationIKernel, _globalTetIdxBufferId,
                _globalTetIdxBuffer);
            SetShaderComputeBuffer(simulateShader, applyInterpolationIKernel, _globalTetWBufferId, _globalTetWBuffer);
            SetShaderComputeBuffer(simulateShader, applyInterpolationIKernel, _localTetWBufferId, _localTetWBuffer);
            SetShaderComputeBuffer(simulateShader, applyInterpolationIKernel, _applyInterpolationFBufferId,
                _applyInterpolationFBuffer);
            _commandBuffer.SetComputeIntParam(simulateShader, _gsTotalCountId, _totalGsCount);
            _commandBuffer.DispatchCompute(simulateShader, applyInterpolationIKernel, gridSize, 1, 1);
            // 执行并同步
            // TestBufferFinish<float>(_applyInterpolationFBuffer, arr =>
            // {
            //     Debug.Log(arr);
            // });
            SubmitTaskAndSynchronize();
            // 第二部分
            SetShaderComputeBuffer(simulateShader, applyInterpolationIIKernel, _covBufferId, _covBuffer);
            SetShaderComputeBuffer(simulateShader, applyInterpolationIIKernel, _gsOtherBufferId, _gsOtherBuffer);
            SetShaderComputeBuffer(simulateShader, applyInterpolationIIKernel, _globalTetIdxBufferId,
                _globalTetIdxBuffer);
            SetShaderComputeBuffer(simulateShader, applyInterpolationIIKernel, _applyInterpolationFBufferId,
                _applyInterpolationFBuffer);
            _commandBuffer.SetComputeIntParam(simulateShader, _gsTotalCountId, _totalGsCount);
            _commandBuffer.DispatchCompute(simulateShader, applyInterpolationIIKernel, gridSize, 1, 1);
        }
    }


    /// <summary>
    /// 材质属性类。这里的材质不是指渲染材质，而是指物理属性
    /// </summary>
    [Serializable]
    public class MaterialProperty
    {
        // 物体密度(kg/m^3)
        public float density;

        // 杨氏模量（Pa），控制物体的刚度
        public float E;

        // 泊松比，描述材料横向压缩与纵向拉伸的比例
        public float nu;

        // 标记物体是否为刚体（是否可以发生形变）
        public bool isRigid;

        // 物体的初始旋转
        public Quaternion rot = Quaternion.identity;

        public MaterialProperty(float density, float E, float nu, bool isRigid)
        {
            this.density = density;
            this.E = E;
            this.nu = nu;
            this.isRigid = isRigid;
        }
    }
}