using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GaussianSplatting.Runtime;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace GSTestScene.Simulation
{
    /// <summary>
    /// 仅用于物理模拟时，是对GaussianSplatRenderer的一层封装
    /// </summary>
    class GaussianObject
    {
        // 引用区
        private GaussianSplatRenderer _renderer; // gs渲染器

        private MaterialProperty _materialProperty; // 材质属性

        // 数据区

        // GS数据
        public int gsNums => _renderer.splatCount; // GS数量
        public int GsOffset; // 相对于全局的GS索引的偏移量

        public NativeArray<uint> gsPositionData => _renderer.asset.posData.GetData<uint>(); //GS 位置数据
        public NativeArray<uint> gsOtherData => _renderer.asset.otherData.GetData<uint>(); //GS 缩放/旋转数据
        public NativeArray<uint> gsSHData => _renderer.asset.shData.GetData<uint>(); // GS SH数据

        // 网格数据
        public int verticesCount => VerticesData.Length; // 顶点数量
        public int edgesCount => EdgesData.Length / 2; //边数量
        public int facesCount => FacesData.Length / 3; //面数量
        public int cellsCount => CellsData.Length / 4; //四面体数量

        public int VerticesOffset; //顶点偏移量
        public int EdgesOffset; //边偏移量
        public int FacesOffset; //面偏移量
        public int CellsOffset; //四面体偏移量

        public NativeArray<Vector3> VerticesData; // 顶点位置
        public NativeArray<int> VerticesGroupData; // 顶点组
        public NativeArray<int> EdgesData; // 边索引
        public NativeArray<int> FacesData; // 面索引
        public NativeArray<int> CellsData; // 四面体索引
        public NativeArray<int> RigidGroupData; // 刚体组

        // 物理属性数据
        public NativeArray<float> DensityData; // 每个顶点的材料密度
        public NativeArray<float> MuData; // 每个顶点的剪切模量
        public NativeArray<float> LambdaData; // 每个顶点的拉梅常数

        // 参数区
        public bool IsBackground; //是否是背景
        public bool isRigid => _materialProperty.isRigid; // 是否是刚体

        // 模拟无关参数
        private const int InnerBatchCount = 64;


        /// <summary>
        /// 初始化子物体的数据和物理属性
        /// </summary>
        /// <param name="renderer">GS渲染器</param>
        /// <param name="materialProperty">物理材质属性</param>
        /// <param name="gsOffset">GS全局偏移</param>
        /// <param name="verticesOffset">顶点索引全局偏移</param>
        /// <param name="edgesOffset">边索引全局偏移</param>
        /// <param name="facesOffset">面索引全局偏移</param>
        /// <param name="cellsOffset">四面体索引全局偏移</param>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool InitializeData(GaussianSplatRenderer renderer, MaterialProperty materialProperty, int gsOffset,
            int verticesOffset, int edgesOffset, int facesOffset, int cellsOffset, int index)

        {
            // 初始化属性
            _renderer = renderer;
            _materialProperty = materialProperty;

            // 初始化偏移量
            GsOffset = gsOffset;
            VerticesOffset = verticesOffset;
            EdgesOffset = edgesOffset;
            FacesOffset = facesOffset;
            CellsOffset = cellsOffset;

            // 判断是否能模拟
            if (_renderer.asset.enableSimulate)
            {
                IsBackground = false;
                // 从文件异步加载网格数据
                string errorMessage = Task.Run(LoadMeshDataFromFile).Result;

                if (!string.IsNullOrWhiteSpace(errorMessage))
                {
                    MainUIManager.ShowTip(errorMessage);
                    return false;
                }

                // 初始化顶点组
                VerticesGroupData = new NativeArray<int>(verticesCount, Allocator.Persistent);
                new FillJob<int> { Data = VerticesGroupData, Value = index }.Schedule(verticesCount, InnerBatchCount)
                    .Complete();

                // 初始化刚体组
                RigidGroupData = new NativeArray<int>(verticesCount, Allocator.Persistent);

                if (isRigid)
                {
                    new FillJob<int> { Data = RigidGroupData, Value = index }.Schedule(verticesCount, InnerBatchCount)
                        .Complete();
                }
                else
                {
                    new FillJob<int> { Data = RigidGroupData, Value = -1 }.Schedule(verticesCount, InnerBatchCount)
                        .Complete();
                }

                // 初始化物理属性
                DensityData = new NativeArray<float>(cellsCount, Allocator.Persistent);
                MuData = new NativeArray<float>(cellsCount, Allocator.Persistent);
                LambdaData = new NativeArray<float>(cellsCount, Allocator.Persistent);

                float density = materialProperty.density;
                float E = materialProperty.E;
                float nu = materialProperty.nu;
                float mu = E / (2 * (1 + nu));
                float lambda = E * nu / ((1 + nu) * (1 - 2 * nu));

                new FillJob<float> { Data = DensityData, Value = density }.Schedule(cellsCount, InnerBatchCount)
                    .Complete();
                new FillJob<float> { Data = MuData, Value = mu }.Schedule(cellsCount, InnerBatchCount).Complete();
                new FillJob<float> { Data = LambdaData, Value = lambda }.Schedule(cellsCount, InnerBatchCount)
                    .Complete();

                // 给网格的边/面/四面体数据索引加上全局顶点偏移量
                new AddIntJob { Data = EdgesData, Value = verticesOffset }
                    .Schedule(EdgesData.Length, InnerBatchCount)
                    .Complete();
                new AddIntJob { Data = FacesData, Value = verticesOffset }
                    .Schedule(FacesData.Length, InnerBatchCount)
                    .Complete();
                new AddIntJob { Data = CellsData, Value = verticesOffset }
                    .Schedule(CellsData.Length, InnerBatchCount)
                    .Complete();
            }
            // 对于不能模拟的物体，设置其为背景即可
            else
            {
                IsBackground = true;
            }


            return true;
        }

        /// <summary>
        /// 根据资产路径加载对应的网格文件
        /// </summary>
        private string LoadMeshDataFromFile()
        {
            // 检查
            if (!_renderer) return "Renderer not initialized";
            if (!_renderer.asset.enableSimulate) return "Asset is not simulatable";
            string meshDataPath = Path.Join(_renderer.asset.assetDataPath, Status.AssetMeshFileLocalDir,
                Status.AssetMeshFileName);
            if (!File.Exists(meshDataPath))
            {
                return $"Mesh File not Found : {meshDataPath}";
            }

            // 读取数据
            using StreamReader reader = new StreamReader(meshDataPath);
            // 读取顶点和单元数量
            string header = reader.ReadLine();
            if (header == null) return "Invalid mesh file : wrong header format";
            string[] headerParts = header.Split(' ');
            int numVertices = int.Parse(headerParts[0]);
            int numCells = int.Parse(headerParts[1]);

            // 初始化 NativeArray
            VerticesData = new NativeArray<Vector3>(numVertices, Allocator.Persistent);
            CellsData = new NativeArray<int>(numCells * 4, Allocator.Persistent);

            // 读取顶点数据
            for (int i = 0; i < numVertices; i++)
            {
                string line = reader.ReadLine();
                if (line == null) return "Invalid mesh file : wrong vertices data";
                string[] parts = line.Split(' ');
                float x = float.Parse(parts[0]);
                float y = float.Parse(parts[1]);
                float z = float.Parse(parts[2]);
                VerticesData[i] = new Vector3(x, y, z);
            }

            // 处理单元、边和面
            var edgesSet = new HashSet<(int, int)>();
            var facesDict = new Dictionary<(int, int, int), (int, int, int)>();

            for (int cellIdx = 0; cellIdx < numCells; cellIdx++)
            {
                string line = reader.ReadLine();
                if (line == null) return "Invalid mesh file : wrong cell data";
                string[] parts = line.Split(' ');
                int v0 = int.Parse(parts[0]);
                int v1 = int.Parse(parts[1]);
                int v2 = int.Parse(parts[2]);
                int v3 = int.Parse(parts[3]);

                // 存储单元数据
                CellsData[cellIdx * 4] = v0;
                CellsData[cellIdx * 4 + 1] = v1;
                CellsData[cellIdx * 4 + 2] = v2;
                CellsData[cellIdx * 4 + 3] = v3;

                // 生成边（去重）
                AddEdge(edgesSet, v0, v1);
                AddEdge(edgesSet, v0, v2);
                AddEdge(edgesSet, v0, v3);
                AddEdge(edgesSet, v1, v2);
                AddEdge(edgesSet, v1, v3);
                AddEdge(edgesSet, v2, v3);

                // 生成面（去重）
                AddFace(facesDict, v0, v1, v2);
                AddFace(facesDict, v0, v1, v3);
                AddFace(facesDict, v0, v2, v3);
                AddFace(facesDict, v1, v2, v3);
            }

            // 5. 填充边和面数据
            EdgesData = new NativeArray<int>(edgesSet.Count * 2, Allocator.Persistent);
            int edgeIdx = 0;
            foreach (var edge in edgesSet)
            {
                EdgesData[edgeIdx++] = edge.Item1;
                EdgesData[edgeIdx++] = edge.Item2;
            }

            FacesData = new NativeArray<int>(facesDict.Count * 3, Allocator.Persistent);
            int faceIdx = 0;
            foreach (var face in facesDict.Values)
            {
                FacesData[faceIdx++] = face.Item1;
                FacesData[faceIdx++] = face.Item2;
                FacesData[faceIdx++] = face.Item3;
            }

            return string.Empty;

            // 辅助方法：添加边（自动排序）
            void AddEdge(HashSet<(int, int)> edges, int a, int b)
            {
                int min = Mathf.Min(a, b);
                int max = Mathf.Max(a, b);
                edges.Add((min, max));
            }

            // 辅助方法：添加面（自动排序并去重）
            void AddFace(Dictionary<(int, int, int), (int, int, int)> faces, int a, int b, int c)
            {
                // 排序顶点索引
                int[] sorted = { a, b, c };
                Array.Sort(sorted);
                var key = (sorted[0], sorted[1], sorted[2]);

                // 保留原始顶点顺序
                var value = (a, b, c);

                if (!faces.TryAdd(key, value))
                    faces.Remove(key); // 内部面移除
            }
        }

        /// <summary>
        /// 释放所有网格数据
        /// </summary>
        public void Dispose()
        {
            VerticesData.Dispose();
            VerticesGroupData.Dispose();
            EdgesData.Dispose();
            FacesData.Dispose();
            CellsData.Dispose();
            RigidGroupData.Dispose();
            DensityData.Dispose();
            MuData.Dispose();
            LambdaData.Dispose();
        }
    }
}

/// <summary>
/// 用于并行填充整个数组
/// </summary>
[BurstCompile]
internal struct FillJob<T> : IJobParallelFor where T : struct
{
    public NativeArray<T> Data;
    public T Value;
    public void Execute(int index) => Data[index] = Value;
}

/// <summary>
/// 用于并行给整个数组添加常数值
/// </summary>
internal struct AddIntJob : IJobParallelFor
{
    public NativeArray<int> Data;
    public int Value;
    public void Execute(int index) => Data[index] += Value;
}