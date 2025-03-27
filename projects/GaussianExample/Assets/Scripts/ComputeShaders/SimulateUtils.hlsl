# ifndef _SIMUTILS
# define _SIMUTILS_

// 模拟用功能文件
#include "Packages/org.nesnausk.gaussian-splatting/Shaders/GaussianSplatting.hlsl"// 用于解码和编码uint表示的GS数据
#include "MathUtils.hlsl"
const int log_num_banks = 5;
const int max_collision_pairs = 1000000;


/// 数据存取相关 ///

/**
 * 将原始GS位置列表中uint封装的位置数据转换为float3类型
 * @param buffer 需要读取的缓冲区
 * @param index GS索引
 * @return 解码的GS位置
 */
inline float3 load_gs_pos(const RWStructuredBuffer<uint3> buffer, const int index)
{
    return asfloat(buffer[index]);
}

/**
 * 将新位置存放到GS位置列表的对应位置
 * @param buffer 需要存放的缓冲区
 * @param pos 新的位置(float3)
 * @param index GS索引
 */
inline void store_gs_pos(inout RWStructuredBuffer<uint3> buffer, const float3 pos, const int index)
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
inline float4 load_gs_rotation(const RWStructuredBuffer<uint> buffer, const int index, const int offset = 0)
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
inline void store_gs_rotation(inout RWStructuredBuffer<uint> buffer, const float4 rotation, const int index,
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
inline float3 load_gs_scale(const RWStructuredBuffer<uint> buffer, const int index, const int offset = 1)
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
inline void store_gs_scale(inout RWStructuredBuffer<uint> buffer, const float3 scale, const int index,
                           const int offset = 1)
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
 * 将一个float3x3矩阵存储于float缓冲区对应索引
 * @param mat 矩阵
 * @param buffer 缓冲区
 * @param index 指定索引（大小以float3x3计）
 */
inline void store_matrix_float(const float3x3 mat, inout RWStructuredBuffer<float> buffer, const int index)
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
 * 对浮点缓冲区的原子加法
 * @param float_buffer 以uint存储的浮点缓冲区
 * @param index 索引
 * @param value 增加值
 */
inline void atomic_add_float(inout RWStructuredBuffer<uint> float_buffer, const int index, const float value)
{
    uint current, original;
    do
    {
        current = float_buffer[index];
        const float current_float = asfloat(current); // 二进制转 float
        const float new_float = current_float + value;
        const uint new_uint = asuint(new_float); // float 转二进制
        InterlockedCompareExchange(float_buffer[index], current, new_uint, original);
    }
    while (original != current);
}


/// 莫顿码相关 ///

inline uint conflict_free_offset(const uint n)
{
    return n >> log_num_banks;
}

inline uint get_shared_index(const uint n)
{
    return n + conflict_free_offset(n);
}

/**
 * 某种位扩展操作？
 */
inline uint expand_bits(uint v)
{
    v = (v * 0x00010001u) & 0xFF0000FFu;
    v = (v * 0x00000101u) & 0x0F00F00Fu;
    v = (v * 0x00000011u) & 0xC30C30C3u;
    v = (v * 0x00000005u) & 0x49249249u;
    return v;
}

/**
 * 获取空间位置的莫顿编码。精度可能丢失
 * @param pos 空间位置
 * @param resolution 分辨率
 * @return 近似莫顿编码
 */
inline uint get_morton_code(const float3 pos, const float resolution = 1024.f)
{
    // 截断小数部分
    uint3 code = uint3(min(max(pos * resolution, 0.f), resolution - 1.f));
    code.x = expand_bits(code.x);
    code.y = expand_bits(code.y);
    code.z = expand_bits(code.z);
    return code.x * 4 + code.y * 2 + code.z;
}

/**
 * 计算两个整数二进制表示的最高公共前缀位数
 */
inline int common_upper_bits(const uint lhs, const uint rhs)
{
    const uint xor_val = lhs ^ rhs;
    if (xor_val == 0) return 32;

    const uint debruijn32 = 0x07C4ACDDu;
    const int debruijn32_table[32] = {
        0, 9, 1, 10, 13, 21, 2, 29,
        11, 14, 16, 18, 22, 25, 3, 30,
        8, 12, 20, 28, 15, 17, 24, 7,
        19, 27, 23, 6, 26, 5, 31, 4
    };

    uint v = xor_val | (xor_val >> 1);
    v |= v >> 2;
    v |= v >> 4;
    v |= v >> 8;
    v |= v >> 16;
    const uint r = (v * debruijn32) >> 27;
    return 31 - debruijn32_table[r];
}

/**
 * 计算两个整数二进制表示的最高公共前缀位数(uint2版本)
 * x存储低位，y存储高位
 */
inline int common_upper_bits(const uint2 lhs, const uint2 rhs)
{
    return (lhs.y == rhs.y)
               ? (32 + common_upper_bits(lhs.x, rhs.x))
               : common_upper_bits(lhs.y, rhs.y);
}

/**
 * 通过分析节点编码（例如 Morton 码或其他空间编码），快速确定当前节点在有序数组中的相邻范围
 * @param node_code_buffer Morton码缓冲区
 * @param num_leaves 叶子节点总数
 * @param idx 当前节点索引
 * @return 表示当前节点在某个层次上的相邻范围 [起始索引, 结束索引]
 */
inline uint2 determine_range(const RWStructuredBuffer<uint2> node_code_buffer, const int num_leaves, int idx)
{
    if (idx == 0)
    {
        return uint2(0, num_leaves - 1);
    }
    // 方向判断
    const uint2 self_code = node_code_buffer[idx];
    const int l_delta = common_upper_bits(self_code, node_code_buffer[idx - 1]);
    const int r_delta = common_upper_bits(self_code, node_code_buffer[idx + 1]);
    const int d = (r_delta > l_delta) ? 1 : -1;
    // 范围上限计算
    const int delta_min = min(l_delta, r_delta);
    int l_max = 2;
    int delta = -1;
    int i_tmp = idx + d * l_max;
    if (0 <= i_tmp && i_tmp < num_leaves)
    {
        delta = common_upper_bits(self_code, node_code_buffer[i_tmp]);
    }
    while (delta > delta_min)
    {
        l_max <<= 1;
        i_tmp = idx + d * l_max;
        delta = -1;
        if (0 <= i_tmp && i_tmp < num_leaves)
        {
            delta = common_upper_bits(self_code, node_code_buffer[i_tmp]);
        }
    }

    // 二分法精确边界

    int l = 0;
    int t = l_max >> 1;

    while (t > 0)
    {
        i_tmp = idx + (l + t) * d;
        delta = -1;
        if (0 <= i_tmp && i_tmp < num_leaves)
        {
            delta = common_upper_bits(self_code, node_code_buffer[i_tmp]);
        }
        if (delta > delta_min)
        {
            l += t;
        }
        t >>= 1;
    }
    uint jdx = idx + l * d;
    if (d < 0)
    {
        const uint tmp = idx;
        idx = jdx;
        jdx = tmp;
    }
    return uint2(idx, jdx);
}

/**
 *  在有序编码数组中快速定位分割点
 * @param node_code_buffer Morton码缓冲区
 * @param num_leaves 叶子节点总数
 * @param first 区间索引起点
 * @param last 区间索引终点
 * @return 第一个使得共同前缀层级下降的分割点
 */
inline uint find_split(const RWStructuredBuffer<uint2> node_code_buffer, const int num_leaves, const int first,
                       const int last)
{
    const uint2 first_code = node_code_buffer[first];
    const uint2 last_code = node_code_buffer[last];
    if (all(first_code == last_code))
    {
        return (first + last) >> 1;
    }
    const int delta_node = common_upper_bits(first_code, last_code);

    // binary search...
    int split = first;
    int stride = last - first;
    do
    {
        stride = (stride + 1) >> 1;
        const int middle = split + stride;
        if (middle < last)
        {
            const int delta = common_upper_bits(first_code, node_code_buffer[middle]);
            if (delta > delta_node)
            {
                split = middle;
            }
        }
    }
    while (stride > 1);

    return split;
}

/// 精确碰撞检测的辅助函数 ///
inline void add_collision_pairs(inout RWStructuredBuffer<float3> vert_buffer,
                                inout RWStructuredBuffer<int4> exact_collision_buffer,
                                inout RWStructuredBuffer<int> total_collisions_paiir_buffer,
                                const int p, const int p0, int p1, int p2, const float minimal_dist)
{
    const float3 vert_p = vert_buffer[p];
    const float3 vert_p0 = vert_buffer[p0];
    const float3 vert_p1 = vert_buffer[p1];
    const float3 vert_p2 = vert_buffer[p2];
    if (point_triangle_distance(vert_p, vert_p0, vert_p1, vert_p2) < minimal_dist)
    {
        int pair_idx;
        InterlockedAdd(total_collisions_paiir_buffer[0], 1, pair_idx);
        if (pair_idx < max_collision_pairs)
        {
            float3 d1 = vert_p1 - vert_p0;
            float3 d2 = vert_p2 - vert_p0;
            float3 pp0 = vert_p - vert_p0;
            float3 n = cross(d1, d2);
            if (dot(n, pp0) < 0)
            {
                const int tmp = p1;
                p1 = p2;
                p2 = tmp;
            }
            exact_collision_buffer[pair_idx] = int4(p, p0, p1, p2);
        }
    }
}

# endif
