# ifndef  _LBVH_
# define _LBVH_

typedef struct
{
    float3 upper;
    float3 lower;
} lbvh_aabb;


typedef struct
{
    uint parent_idx;
    uint left_idx;
    uint right_idx;
} lbvh_node;


/**
 * 计算一个aabb包围盒的中心点
 * @return 中心点坐标
 */
inline float3 get_aabb_center(const lbvh_aabb aabb)
{
    return (aabb.upper + aabb.lower) / 2;
}

/**
 * 合并两个包围盒
 * @param a 包围盒a
 * @param b 包围盒b
 * @return 合并后的包围盒
 */
inline lbvh_aabb merge_aabb(const lbvh_aabb a, const lbvh_aabb b)
{
    lbvh_aabb merged_aabb;
    merged_aabb.upper = max(a.upper, b.upper);
    merged_aabb.lower = min(a.lower, b.lower);
    return merged_aabb;
}

inline bool intersects_aabb(const lbvh_aabb a, const lbvh_aabb b)
{
    return !(any(a.lower > b.upper) || any(a.upper < b.lower));
}

# endif
