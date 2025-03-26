// 模拟用功能文件
#include "Packages/org.nesnausk.gaussian-splatting/Shaders/GaussianSplatting.hlsl"// 用于解码和编码uint表示的GS数据


/**
 * 将原始GS位置列表中uint封装的位置数据转换为float3类型
 * @param buffer 需要读取的缓冲区
 * @param index GS索引
 * @return 解码的GS位置
 */
inline float3 load_gs_pos(RWStructuredBuffer<uint3> buffer, const int index)
{
    return asfloat(buffer[index]);
}

/**
 * 将新位置存放到GS位置列表的对应位置
 * @param buffer 需要存放的缓冲区
 * @param pos 新的位置(float3)
 * @param index GS索引
 */
inline void store_gs_pos(RWStructuredBuffer<uint3> buffer, const float3 pos, const int index)
{
    buffer[index] = asuint(pos);
}

/**
 * 将原始GS位置列表中uint封装的旋转数据转换为float4类型
 * @param buffer 需要读取的缓冲区
 * @param index GS索引
 * @return 解码的GS旋转
 * @param offset 每组数据的组内偏移量
 */
inline float4 load_gs_rotation(RWStructuredBuffer<uint> buffer, const int index, const int offset = 0)
{
    const int start_index = index * 4 + offset; //一组数据4个uint，第一个是旋转，后面是缩放
    return DecodeRotation(DecodePacked_10_10_10_2(buffer[start_index]));
}

/**
 * 将新旋转存放到GS其他数据列表的对应位置
 * @param buffer 需要存放的缓冲区
 * @param rotation 新的旋转(float4)
 * @param index GS索引
 * @param offset 每组数据的组内偏移量
 */
inline void store_gs_rotation(RWStructuredBuffer<uint> buffer, const float4 rotation, const int index,
                              const int offset = 0)
{
    const int start_index = index * 4 + offset;
    buffer[start_index] = EncodeQuatToNorm10(PackSmallest3Rotation(rotation));
}

/**
 * 将原始GS位置列表中uint封装的缩放数据转换为float4类型
 * @param buffer 需要读取的缓冲区
 * @param index GS索引
 * @param offset 每组数据的组内偏移量
 * @return 解码的GS缩放
 */
inline float3 load_gs_scale(RWStructuredBuffer<uint> buffer, const int index, const int offset = 1)
{
    const int start_index = index * 4 + offset; //一组数据4个uint，第一个是旋转，后面是缩放
    return asfloat(uint3(buffer[start_index], buffer[start_index + 1],
                         buffer[start_index + 2]));
}

/**
 * 将新缩放存放到GS其他数据列表的对应位置
 * @param buffer 需要存放的缓冲区
 * @param scale 新的缩放(float3)
 * @param index GS索引
 * @param offset 每组数据的组内偏移量
 */
inline void store_gs_scale(RWStructuredBuffer<uint> buffer, const float3 scale, const int index, const int offset = 1)
{
    const int start_index = index * 4 + 1;
    const uint3 scale_uint3 = asuint(scale);
    buffer[start_index] = scale_uint3[0];
    buffer[start_index + 1] = scale_uint3[1];
    buffer[start_index + 2] = scale_uint3[2];
}

inline float3x3 load_matrix_float(const RWStructuredBuffer<float> buffer, const int index)
{
    float3x3 result;
    const int start_index = index * 9;

    result[0][0] = buffer[start_index + 0];
    result[0][1] = buffer[start_index + 1];
    result[0][2] = buffer[start_index + 2];
    result[1][0] = buffer[start_index + 3];
    result[1][1] = buffer[start_index + 4];
    result[1][2] = buffer[start_index + 5];
    result[2][0] = buffer[start_index + 6];
    result[2][1] = buffer[start_index + 7];
    result[2][2] = buffer[start_index + 8];

    return result;
}

/**
 * 将一个float3x3矩阵按行优先存储于float缓冲区对应索引
 * @param mat 矩阵
 * @param buffer 缓冲区
 * @param index 指定索引（大小以float3x3计）
 */
inline void store_matrix_float(const float3x3 mat, RWStructuredBuffer<float> buffer, const int index)
{
    const int start_index = index * 9;
    for (int i = 0; i < 3; i++)
    {
        for (int j = 0; j < 3; j++)
        {
            buffer[start_index + i * 3 + j] = mat[i][j];
        }
    }
}

/**
 * 将一个double3x3矩阵按行优先存储于double缓冲区对应索引
 * @param mat 矩阵
 * @param buffer 缓冲区
 * @param index 指定索引（大小以float3x3计）
 */
inline void store_matrix_double(const double3x3 mat, RWStructuredBuffer<double> buffer, const int index)
{
    const int start_index = index * 9;
    for (int i = 0; i < 3; i++)
    {
        for (int j = 0; j < 3; j++)
        {
            buffer[start_index + i * 3 + j] = mat[i][j];
        }
    }
}

inline void atom_add_float(RWStructuredBuffer<>)