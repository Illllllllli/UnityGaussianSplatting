// SPDX-License-Identifier: MIT

using System;
using System.IO;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;


namespace GaussianSplatting.Runtime
{
    [Serializable]
    public class GaussianSplatAsset : ScriptableObject
    {
        public const int kCurrentVersion = 2023_10_20;
        public const int kChunkSize = 256;
        public const int kTextureWidth = 2048; // allows up to 32M splats on desktop GPU (2k width x 16k height)
        public const int kMaxSplats = 8_600_000; // mostly due to 2GB GPU buffer size limit when exporting a splat (2GB / 248B is just over 8.6M)

        [SerializeField] int m_FormatVersion;
        [SerializeField] int m_SplatCount;
        [SerializeField] Vector3 m_BoundsMin;
        [SerializeField] Vector3 m_BoundsMax;
        [SerializeField] Hash128 m_DataHash;

        public int formatVersion => m_FormatVersion;
        public int splatCount => m_SplatCount;
        public Vector3 boundsMin => m_BoundsMin;
        public Vector3 boundsMax => m_BoundsMax;
        public Hash128 dataHash => m_DataHash;

        // Match VECTOR_FMT_* in HLSL
        public enum VectorFormat
        {
            Float32, // 12 bytes: 32F.32F.32F
            Norm16, // 6 bytes: 16.16.16
            Norm11, // 4 bytes: 11.10.11
            Norm6   // 2 bytes: 6.5.5
        }

        public static int GetVectorSize(VectorFormat fmt)
        {
            return fmt switch
            {
                VectorFormat.Float32 => 12,
                VectorFormat.Norm16 => 6,
                VectorFormat.Norm11 => 4,
                VectorFormat.Norm6 => 2,
                _ => throw new ArgumentOutOfRangeException(nameof(fmt), fmt, null)
            };
        }

        public enum ColorFormat
        {
            Float32x4,
            Float16x4,
            Norm8x4,
            BC7,
        }
        public static int GetColorSize(ColorFormat fmt)
        {
            return fmt switch
            {
                ColorFormat.Float32x4 => 16,
                ColorFormat.Float16x4 => 8,
                ColorFormat.Norm8x4 => 4,
                ColorFormat.BC7 => 1,
                _ => throw new ArgumentOutOfRangeException(nameof(fmt), fmt, null)
            };
        }

        public enum SHFormat
        {
            Float32,
            Float16,
            Norm11,
            Norm6,
            Cluster64k,
            Cluster32k,
            Cluster16k,
            Cluster8k,
            Cluster4k,
        }

        public struct SHTableItemFloat32
        {
            public float3 sh1, sh2, sh3, sh4, sh5, sh6, sh7, sh8, sh9, shA, shB, shC, shD, shE, shF;
            public float3 shPadding; // pad to multiple of 16 bytes
        }
        public struct SHTableItemFloat16
        {
            public half3 sh1, sh2, sh3, sh4, sh5, sh6, sh7, sh8, sh9, shA, shB, shC, shD, shE, shF;
            public half3 shPadding; // pad to multiple of 16 bytes
        }
        public struct SHTableItemNorm11
        {
            public uint sh1, sh2, sh3, sh4, sh5, sh6, sh7, sh8, sh9, shA, shB, shC, shD, shE, shF;
        }
        public struct SHTableItemNorm6
        {
            public ushort sh1, sh2, sh3, sh4, sh5, sh6, sh7, sh8, sh9, shA, shB, shC, shD, shE, shF;
            public ushort shPadding; // pad to multiple of 4 bytes
        }

        public void Initialize(int splats, VectorFormat formatPos, VectorFormat formatScale, ColorFormat formatColor, SHFormat formatSh, Vector3 bMin, Vector3 bMax, CameraInfo[] cameraInfos)
        {
            m_SplatCount = splats;
            m_FormatVersion = kCurrentVersion;
            m_PosFormat = formatPos;
            m_ScaleFormat = formatScale;
            m_ColorFormat = formatColor;
            m_SHFormat = formatSh;
            m_Cameras = cameraInfos;
            m_BoundsMin = bMin;
            m_BoundsMax = bMax;
        }

        public void SetDataHash(Hash128 hash)
        {
            m_DataHash = hash;
        }

        /// <summary>
        /// 因为将TextAsset改为了自建的ByteAsset，因此Unity没办法自动加载子数据文件。
        /// 所以在放到gsrenderer的时候要确认一下ByteAsset是否正常，没有的话尝试从外部加载。
        /// </summary>
        /// <returns>ByteAsset是否有效</returns>
        public bool CheckByteAsset()
        {
            if (posData.dataSize > 0 && otherData.dataSize > 0 && colorData.dataSize > 0 && shData.dataSize > 0)
            {
                return true;
            }
            m_ChunkData = ByteAsset.CreateByteAssetFromFile(Path.Join(assetDataPath, "_chk.bytes"));
            m_PosData = ByteAsset.CreateByteAssetFromFile(Path.Join(assetDataPath, $"{name}_pos.bytes"));
            m_OtherData= ByteAsset.CreateByteAssetFromFile(Path.Join(assetDataPath, $"{name}_oth.bytes"));
            m_ColorData = ByteAsset.CreateByteAssetFromFile(Path.Join(assetDataPath, $"{name}_col.bytes"));
            m_SHData= ByteAsset.CreateByteAssetFromFile(Path.Join(assetDataPath, $"{name}_shs.bytes"));

            return posData.dataSize > 0 && otherData.dataSize > 0 && colorData.dataSize > 0 && shData.dataSize > 0;
        }
        
        /// <summary>
        /// 创建资产时，设置资产的ByteAsset数据块.
        /// </summary>
        /// <param name="dataPath">资产路径文件夹</param>
        /// <param name="edit">是否可编辑（需要colmap文件）</param>
        /// <param name="dataChunk">压缩数据块</param>
        /// <param name="dataPos">位置数据块</param>
        /// <param name="dataOther">缩放数据块</param>
        /// <param name="dataColor">颜色数据块</param>
        /// <param name="dataSh">sh数据块</param>
        public void SetAssetFiles(string dataPath ,bool edit,ByteAsset  dataChunk, ByteAsset dataPos, ByteAsset dataOther, ByteAsset dataColor, ByteAsset dataSh)
        {
            enableEdit = edit;
            assetDataPath = dataPath;
            m_ChunkData = dataChunk;
            m_PosData = dataPos;
            m_OtherData = dataOther;
            m_ColorData = dataColor;
            m_SHData = dataSh;
        }


        public static int GetOtherSizeNoSHIndex(VectorFormat scaleFormat)
        {
            return 4 + GetVectorSize(scaleFormat);
        }

        public static int GetSHCount(SHFormat fmt, int splatCount)
        {
            return fmt switch
            {
                SHFormat.Float32 => splatCount,
                SHFormat.Float16 => splatCount,
                SHFormat.Norm11 => splatCount,
                SHFormat.Norm6 => splatCount,
                SHFormat.Cluster64k => 64 * 1024,
                SHFormat.Cluster32k => 32 * 1024,
                SHFormat.Cluster16k => 16 * 1024,
                SHFormat.Cluster8k => 8 * 1024,
                SHFormat.Cluster4k => 4 * 1024,
                _ => throw new ArgumentOutOfRangeException(nameof(fmt), fmt, null)
            };
        }

        /// <summary>
        /// 计算颜色纹理所需的大小（存储每个GS的颜色）
        /// </summary>
        /// <param name="splatCount">GS数量</param>
        /// <returns></returns>
        public static (int,int) CalcTextureSize(int splatCount)
        {
            //固定宽度，计算高度
            int width = kTextureWidth;
            int height = math.max(1, (splatCount + width - 1) / width);
            // our swizzle tiles are 16x16, so make texture multiple of that height
            //确保高度是16的倍数
            int blockHeight = 16;
            height = (height + blockHeight - 1) / blockHeight * blockHeight;
            return (width, height);
        }

        /// <summary>
        /// 将自定义的颜色格式映射到Unity内置颜色格式
        /// </summary>
        /// <param name="format">自定义的颜色格式枚举量</param>
        /// <returns>映射的Unity内置颜色格式</returns>
        /// <exception cref="ArgumentOutOfRangeException">未预期的颜色格式</exception>
        public static GraphicsFormat ColorFormatToGraphics(ColorFormat format)
        {
            return format switch
            {
                ColorFormat.Float32x4 => GraphicsFormat.R32G32B32A32_SFloat,
                ColorFormat.Float16x4 => GraphicsFormat.R16G16B16A16_SFloat,
                ColorFormat.Norm8x4 => GraphicsFormat.R8G8B8A8_UNorm,
                ColorFormat.BC7 => GraphicsFormat.RGBA_BC7_UNorm,
                _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
            };
        }

        public static long CalcPosDataSize(int splatCount, VectorFormat formatPos)
        {
            return splatCount * GetVectorSize(formatPos);
        }
        public static long CalcOtherDataSize(int splatCount, VectorFormat formatScale)
        {
            return splatCount * GetOtherSizeNoSHIndex(formatScale);
        }
        public static long CalcColorDataSize(int splatCount, ColorFormat formatColor)
        {
            var (width, height) = CalcTextureSize(splatCount);
            return width * height * GetColorSize(formatColor);
        }
        public static long CalcSHDataSize(int splatCount, SHFormat formatSh)
        {
            int shCount = GetSHCount(formatSh, splatCount);
            return formatSh switch
            {
                SHFormat.Float32 => shCount * UnsafeUtility.SizeOf<SHTableItemFloat32>(),
                SHFormat.Float16 => shCount * UnsafeUtility.SizeOf<SHTableItemFloat16>(),
                SHFormat.Norm11 => shCount * UnsafeUtility.SizeOf<SHTableItemNorm11>(),
                SHFormat.Norm6 => shCount * UnsafeUtility.SizeOf<SHTableItemNorm6>(),
                _ => shCount * UnsafeUtility.SizeOf<SHTableItemFloat16>() + splatCount * 2
            };
        }
        public static long CalcChunkDataSize(int splatCount)
        {
            int chunkCount = (splatCount + kChunkSize - 1) / kChunkSize;
            return chunkCount * UnsafeUtility.SizeOf<ChunkInfo>();
        }

        [SerializeField] VectorFormat m_PosFormat = VectorFormat.Norm11;
        [SerializeField] VectorFormat m_ScaleFormat = VectorFormat.Norm11;
        [SerializeField] SHFormat m_SHFormat = SHFormat.Norm11;
        [SerializeField] ColorFormat m_ColorFormat;

        [SerializeField] ByteAsset m_PosData;
        [SerializeField] ByteAsset m_ColorData;
        [SerializeField] ByteAsset m_OtherData;
        [SerializeField] ByteAsset m_SHData;
        // Chunk data is optional (if data formats are fully lossless then there's no chunking)
        [SerializeField] ByteAsset m_ChunkData;

        [SerializeField] CameraInfo[] m_Cameras;

        // 存储对应assetData的数据路径；
        public string assetDataPath;
        public bool enableEdit;

        public VectorFormat posFormat => m_PosFormat;
        public VectorFormat scaleFormat => m_ScaleFormat;
        public SHFormat shFormat => m_SHFormat;
        public ColorFormat colorFormat => m_ColorFormat;

        public ByteAsset posData => m_PosData;
        public ByteAsset colorData => m_ColorData;
        public ByteAsset otherData => m_OtherData;
        public ByteAsset shData => m_SHData;
        public ByteAsset chunkData => m_ChunkData;
        public CameraInfo[] cameras => m_Cameras;

        public struct ChunkInfo
        {
            public uint colR, colG, colB, colA;
            public float2 posX, posY, posZ;
            public uint sclX, sclY, sclZ;
            public uint shR, shG, shB;
        }

        [Serializable]
        public struct CameraInfo
        {
            public Vector3 pos;
            public Vector3 axisX, axisY, axisZ;
            public float fov;
        }
    }
}

/// <summary>
/// 用于替换GSRenderer使用的Textasset
/// </summary>
[Serializable]
public class ByteAsset
{
    private byte[] _bytes;

    public ByteAsset(byte[] data)
    {
        _bytes = data;
    }
    // 属性定义（返回数据总字节数）
    
    /// <summary>
    /// 从文件创建ByteAsset
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>创建的ByteAsset对象</returns>
    public static ByteAsset CreateByteAssetFromFile(string filePath)
    {
        try
        {
            byte[] bytes = File.ReadAllBytes(filePath);
            ByteAsset byteAsset = new ByteAsset(bytes);
            return byteAsset;
        }
        catch
        {
            return new ByteAsset(new byte[]{});
        }

        
    }
    
    public int dataSize => _bytes != null ? _bytes.Length : 0;
    public void SetData(byte[] data)
    {
        _bytes = data;
    }
    // 泛型方法实现
    public NativeArray<T> GetData<T>() where T : struct {
        // 验证数据对齐
        int structSize = UnsafeUtility.SizeOf<T>();
        if (_bytes.Length % structSize != 0) {
            throw new InvalidOperationException($"Data size {_bytes.Length} is not multiple of {structSize}");
        }
    
        // 创建指向原始数据的NativeArray（无需内存分配）
        NativeArray<T> array;
        unsafe {
            void* ptr = UnsafeUtility.AddressOf(ref _bytes[0]);
            array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(
                ptr, _bytes.Length / structSize, Allocator.None);
        }
    
        // 安全句柄（防止数据被GC回收）
        NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, AtomicSafetyHandle.Create());
        return array;
    }

}
