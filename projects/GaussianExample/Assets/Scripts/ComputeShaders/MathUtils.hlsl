#include  "SifakisSVD.hlsl"
// 数学计算功能文件
const float m_sqrt3 = 1.73205080756887729352744634151;
/**
 * 从向量复制值到传入的数组指针中
 */
inline void copy_vector_to_array3(const float3 vec, inout float array[3])
{
    for (int i = 0; i < 3; i++)
    {
        array[i] = vec[i];
    }
}

/**
 * 从矩阵复制值到传入的数组指针中
 */
inline void copy_matrix_to_array3x3(const float3x3 mat, inout float array[3][3])
{
    for (int i = 0; i < 3; i++)
    {
        for (int j = 0; j < 3; j++)
        {
            array[i][j] = mat[i][j];
        }
    }
}

/**
 * 从传入的数组指针复制值到向量中
 */
inline float3 copy_array_to_vector3(inout float array[3])
{
    return float3(array[0], array[1], array[2]);
}

/**
 * 从传入的数组指针复制值到矩阵中
 */
inline float3x3 copy_array_to_matrix3(inout float array[3][3])
{
    return float3x3(array[0][0], array[0][1], array[0][2],
                    array[1][0], array[1][1], array[1][2],
                    array[2][0], array[2][1], array[2][2]);
}

/**
 * 计算2x2行列式(ad-bc)
 * @return 行列式值
 */
inline float determinant_2x2(const float a, const float b, const float c, const float d)
{
    return a * d - b * c;
}


/**
 * 计算3x3行列式
 * @param m 矩阵
 * @return 矩阵的行列式值
 */
inline float determinant_3x3(const float3x3 m)
{
    return m[0][0] * (m[1][1] * m[2][2] - m[1][2] * m[2][1])
        - m[0][1] * (m[1][0] * m[2][2] - m[1][2] * m[2][0])
        + m[0][2] * (m[1][0] * m[2][1] - m[1][1] * m[2][0]);
}

/**
 * 计算3x3行列式
 * @param m 矩阵
 * @return 矩阵的行列式值
 */
inline float determinant_3x3(in float m[3][3])
{
    return m[0][0] * (m[1][1] * m[2][2] - m[1][2] * m[2][1])
        - m[0][1] * (m[1][0] * m[2][2] - m[1][2] * m[2][0])
        + m[0][2] * (m[1][0] * m[2][1] - m[1][1] * m[2][0]);
}


/**
 * 计算伴随矩阵
 * @param m 原矩阵
 * @return 原矩阵的伴随矩阵
 */
inline float3x3 adjoint_3x3(const float3x3 m)
{
    float3x3 adj;

    // 计算每个元素的代数余子式并转置
    adj[0][0] = +determinant_2x2(m[1][1], m[1][2], m[2][1], m[2][2]);
    adj[0][1] = -determinant_2x2(m[1][0], m[1][2], m[2][0], m[2][2]);
    adj[0][2] = +determinant_2x2(m[1][0], m[1][1], m[2][0], m[2][1]);

    adj[1][0] = -determinant_2x2(m[0][1], m[0][2], m[2][1], m[2][2]);
    adj[1][1] = +determinant_2x2(m[0][0], m[0][2], m[2][0], m[2][2]);
    adj[1][2] = -determinant_2x2(m[0][0], m[0][1], m[2][0], m[2][1]);

    adj[2][0] = +determinant_2x2(m[0][1], m[0][2], m[1][1], m[1][2]);
    adj[2][1] = -determinant_2x2(m[0][0], m[0][2], m[1][0], m[1][2]);
    adj[2][2] = +determinant_2x2(m[0][0], m[0][1], m[1][0], m[1][1]);

    return adj;
}

/**
 * 计算伴随矩阵
 * @param m 原矩阵
 * @return 原矩阵的伴随矩阵
 */
inline float3x3 adjoint_3x3(inout float m[3][3])
{
    float3x3 adj;

    // 计算每个元素的代数余子式并转置
    adj[0][0] = +determinant_2x2(m[1][1], m[1][2], m[2][1], m[2][2]);
    adj[0][1] = -determinant_2x2(m[1][0], m[1][2], m[2][0], m[2][2]);
    adj[0][2] = +determinant_2x2(m[1][0], m[1][1], m[2][0], m[2][1]);

    adj[1][0] = -determinant_2x2(m[0][1], m[0][2], m[2][1], m[2][2]);
    adj[1][1] = +determinant_2x2(m[0][0], m[0][2], m[2][0], m[2][2]);
    adj[1][2] = -determinant_2x2(m[0][0], m[0][1], m[2][0], m[2][1]);

    adj[2][0] = +determinant_2x2(m[0][1], m[0][2], m[1][1], m[1][2]);
    adj[2][1] = -determinant_2x2(m[0][0], m[0][2], m[1][0], m[1][2]);
    adj[2][2] = +determinant_2x2(m[0][0], m[0][1], m[1][0], m[1][1]);

    return adj;
}

/**
 * 计算伴随矩阵
 * @param m 原矩阵
 * @param adj 导出伴随矩阵
 */
inline void adjoint_3x3(inout float m[3][3], inout float adj[3][3])
{
    // 计算每个元素的代数余子式并转置
    adj[0][0] = +determinant_2x2(m[1][1], m[1][2], m[2][1], m[2][2]);
    adj[0][1] = -determinant_2x2(m[1][0], m[1][2], m[2][0], m[2][2]);
    adj[0][2] = +determinant_2x2(m[1][0], m[1][1], m[2][0], m[2][1]);

    adj[1][0] = -determinant_2x2(m[0][1], m[0][2], m[2][1], m[2][2]);
    adj[1][1] = +determinant_2x2(m[0][0], m[0][2], m[2][0], m[2][2]);
    adj[1][2] = -determinant_2x2(m[0][0], m[0][1], m[2][0], m[2][1]);

    adj[2][0] = +determinant_2x2(m[0][1], m[0][2], m[1][1], m[1][2]);
    adj[2][1] = -determinant_2x2(m[0][0], m[0][2], m[1][0], m[1][2]);
    adj[2][2] = +determinant_2x2(m[0][0], m[0][1], m[1][0], m[1][1]);
}

// 主求逆函数
inline float3x3 inverse_3x3(const float3x3 m)
{
    const float det = determinant_3x3(m);
    const float3x3 adj = adjoint_3x3(m);
    return adj / det; // 伴随矩阵除以行列式
}

// 主求逆函数
inline float3x3 inverse_3x3(in float m[3][3])
{
    const float det = determinant_3x3(m);
    const float3x3 inv = adjoint_3x3(m);
    return inv / det; // 伴随矩阵除以行列式
}


// 主求逆函数
inline void inverse_3x3(inout float m[3][3], inout float inv[3][3])
{
    const float det = determinant_3x3(m);
    adjoint_3x3(m, inv);
    for (int i = 0; i < 3; i++) // 伴随矩阵除以行列式
    {
        for (int j = 0; j < 3; j++)
        {
            inv[i][j] /= det;
        }
    }
}

/**
 * 将四元数转换为 3x3 旋转矩阵
 * @param q 四元数
 * @return 四元数对应的旋转矩阵
 */
inline float3x3 build_rotation_matrix(float4 q)
{
    // 确保四元数已归一化
    q = normalize(q);

    float x = q.x;
    float y = q.y;
    float z = q.z;
    float w = q.w;

    float x2 = x + x;
    float y2 = y + y;
    float z2 = z + z;
    float xx = x * x2;
    float xy = x * y2;
    float xz = x * z2;
    float yy = y * y2;
    float yz = y * z2;
    float zz = z * z2;
    float wx = w * x2;
    float wy = w * y2;
    float wz = w * z2;

    float3x3 m;
    m._m00 = 1.0 - (yy + zz);
    m._m01 = xy - wz;
    m._m02 = xz + wy;

    m._m10 = xy + wz;
    m._m11 = 1.0 - (xx + zz);
    m._m12 = yz - wx;

    m._m20 = xz - wy;
    m._m21 = yz + wx;
    m._m22 = 1.0 - (xx + yy);

    return m;
}

/**
 * 将 3x3 旋转矩阵转换为四元数
 * @param r 旋转矩阵
 * @return 四元数对应的旋转矩阵
 */
inline float4 build_quaternion(const float3x3 r)
{
    float4 q;
    const float trace = r[0][0] + r[1][1] + r[2][2];
    if (trace > 0)
    {
        const float s = 0.5 / sqrt(trace + 1);
        q[0] = 0.25 / s;
        q[1] = (r[2][1] - r[1][2]) * s;
        q[2] = (r[0][2] - r[2][0]) * s;
        q[3] = (r[1][0] - r[0][1]) * s;
    }
    else
    {
        if (r[0][0] > r[1][1] && r[0][0] > r[2][2])
        {
            const float s = 2.0 * sqrt(1.0 + r[0][0] - r[1][1] - r[2][2]);
            q[0] = (r[2][1] - r[1][2]) / s;
            q[1] = 0.25 * s;
            q[2] = (r[0][1] + r[1][0]) / s;
            q[3] = (r[0][2] + r[2][0]) / s;
        }
        else if (r[1][1] > r[2][2])
        {
            const float s = 2.0 * sqrt(1.0 + r[1][1] - r[0][0] - r[2][2]);
            q[0] = (r[0][2] - r[2][0]) / s;
            q[1] = (r[0][1] + r[1][0]) / s;
            q[2] = 0.25 * s;
            q[3] = (r[1][2] + r[2][1]) / s;
        }
        else
        {
            const float s = 2.0 * sqrt(1.0 + r[2][2] - r[0][0] - r[1][1]);
            q[0] = (r[1][0] - r[0][1]) / s;
            q[1] = (r[0][2] + r[2][0]) / s;
            q[2] = (r[1][2] + r[2][1]) / s;
            q[3] = 0.25 * s;
        }
    }
    return normalize(q);
}

/**
 * 根据缩放因子构架缩放矩阵
 * @param scales 缩放因子
 * @return 缩放矩阵
 */
inline float3x3 build_scaling_matrix(float3 scales)
{
    return float3x3(
        scales[0], 0, 0,
        0, scales[1], 0,
        0, 0, scales[2]
    );
}

/**
 * 对3x3对称矩阵进行三对角化处理(豪斯霍尔德变换)
 *  
 * @param A      输入对称3x3矩阵(行优先存储)
 * @param Q      输出豪斯霍尔德变换矩阵(列优先存储)
 * @param d      输出三对角矩阵的主对角线元素
 * @param e      输出三对角矩阵的次对角线元素(最后一位无效)
 */
inline void dsytrd3(const in float A[3][3], inout float Q[3][3], inout float d[3], inout float e[3])
{
    // 初始化变换矩阵为单位矩阵
    Q[0][0] = Q[1][1] = Q[2][2] = 1;
    e[0] = e[1] = e[2] = 0;
    d[0] = d[1] = d[2] = 0;

    // 计算首列非对角线元素的模长
    float h = A[0][1] * A[0][1] + A[0][2] * A[0][2];
    float g = (A[0][1] > 0) ? -sqrt(h) : sqrt(h);

    // 初始化豪斯霍尔德向量
    e[0] = g; // 存储首列消去因子
    const float f = g * A[0][1];
    float3 u = float3(0, A[0][1] - g, A[0][2]); // 豪斯霍尔德向量

    // 计算变换参数
    float omega = h - f;
    if (omega > 0)
    {
        omega = 1.0 / omega;

        // 计算中间向量q
        float3 q = float3(0, 0, 0);
        q.y = omega * (A[1][1] * u.y + A[1][2] * u.z);
        q.z = omega * (A[1][2] * u.y + A[2][2] * u.z);

        // 计算标量修正项K
        float K = 0.5 * omega * omega * (u.y * (A[1][1] * u.y + A[1][2] * u.z) + u.z * (A[1][2] * u.y + A[2][2] * u.z));

        // 修正中间向量
        q.y -= K * u.y;
        q.z -= K * u.z;

        // 更新主对角线元素
        d[0] = A[0][0];
        d[1] = A[1][1] - 2.0 * q.y * u.y;
        d[2] = A[2][2] - 2.0 * q.z * u.z;

        // 更新变换矩阵Q
        for (int j = 1; j < 3; j++)
        {
            float scale = omega * u[j];
            for (int i = 1; i < 3; i++)
            {
                Q[i][j] -= scale * u[i];
            }
        }

        // 更新次对角线元素
        e[2] = A[1][2] - q.y * u.z - u.y * q.z;
    }
    else
    {
        // 直接保留原始对角线元素
        d[0] = A[0][0];
        d[1] = A[1][1];
        d[2] = A[2][2];
        e[2] = A[1][2];
    }
}

/**
 * 使用QL算法计算对称3x3矩阵的特征值和特征向量
 *  
 *  @param A 输入对称3x3矩阵
 *  @param Q0 初始特征向量矩阵（通常为单位矩阵）
 *  @param w0 初始特征值估计
 *  @param Q 输出正交特征向量矩阵（列优先存储）
 *  @param w 输出排序后的特征值（升序排列）
 */
inline void dsyevq3(const in float A[3][3], inout float Q0[3][3], inout float w0[3], inout float Q[3][3],
                    inout float w[3])
{
    // 初始化输出
    for (int i = 0; i < 3; i++)
    {
        w[i] = w0[i];
    }
    for (int i = 0; i < 3; i++)
    {
        for (int j = 0; j < 3; j++)
        {
            Q[i][j] = Q0[i][j];
        }
    }
    // 临时存储三对角化结果
    float e[3];
    // 执行三对角化（需实现dsytrd3）
    dsytrd3(A, Q, w, e);


    // QL算法迭代
    for (int l = 0; l < 2; l++)
    {
        for (int n_iter = 0; n_iter < 30; n_iter++)
        {
            // 确定当前需要处理的子矩阵范围
            int m = l;

            {
                for (float g = 0; m < 2 && (abs(e[m]) + g != g); m++)
                {
                    g = abs(w[m]) + abs(w[m + 1]);
                }
            }

            // 计算Givens旋转参数
            float g = (w[l + 1] - w[l]) / (e[l] + e[l]);
            float r = sqrt(g * g + 1.0);
            g = (g > 0)
                    ? w[m] - w[l] + e[l] / (g + r)
                    : w[m] - w[l] + e[l] / (g - r);


            // 应用Givens旋转
            float s = 1.0, c = 1.0, p = 0.0;
            for (int i = m - 1; i >= l; i--)
            {
                const float f = s * e[i];
                const float b = c * e[i];

                // 计算旋转参数
                if (abs(f) > abs(g))
                {
                    c = g / f;
                    r = sqrt(c * c + 1.0);
                    e[i + 1] = f * r;
                    s = 1.0 / r;
                    c *= s;
                }
                else
                {
                    s = f / g;
                    r = sqrt(s * s + 1.0);
                    e[i + 1] = g * r;
                    c = 1.0 / r;
                    s *= c;
                }

                // 更新对角元素
                g = w[i + 1] - p;
                r = (w[i] - g) * s + 2.0 * c * b;
                p = s * r;
                w[i + 1] = g + p;
                g = c * r - b;

                // 更新特征向量
                for (int k = 0; k < 3; k++)
                {
                    const float temp = Q[k][i + 1];
                    Q[k][i + 1] = s * Q[k][i] + c * temp;
                    Q[k][i] = c * Q[k][i] - s * temp;
                }
            }
            w[l] -= p;
            e[l] = g;
            e[m] = 0.0;
        }
    }
}

/**
 * 辅助函数：交换特征向量列并调整特征值顺序
 * @param Q 特征向量矩阵
 * @param a 行标1
 * @param b 行标2
 * @param eigenvalues 特征值向量
 */
inline void swap_columns(inout float Q[3][3], int a, int b, inout float eigenvalues[3])
{
    const float tmp_val = eigenvalues[a];
    eigenvalues[a] = eigenvalues[b];
    eigenvalues[b] = tmp_val;

    for (int i = 0; i < 3; i++)
    {
        const float temp = Q[a][i];
        Q[a][i] = Q[b][i];
        Q[b][i] = temp;
    }
}

inline void get_sym_eigen3x3(const in float A[3][3], inout float eigenvalues[3], inout float Q[3][3])
{
    const float m = A[0][0] + A[1][1] + A[2][2]; // 矩阵的迹 (Trace)
    const float dd = A[0][1] * A[0][1]; // A[0][1]^2
    const float ee = A[1][2] * A[1][2]; // A[1][2]^2
    const float ff = A[0][2] * A[0][2]; // A[0][2]^2

    // 计算特征值多项式的系数 c1, c0
    const float c1 = A[0][0] * A[1][1] + A[0][0] * A[2][2] + A[1][1] * A[2][2] - (dd + ee + ff);
    const float c0 = A[2][2] * dd + A[0][0] * ee + A[1][1] * ff - A[0][0] * A[1][1] * A[2][2] - 2.0 * A[0][2] * A[0][1]
        * A[1][2];

    // 计算中间变量 p, q, sqrt_p, phi
    const float p = m * m - 3.0 * c1;
    const float q = m * (p - 1.5 * c1) - 13.5 * c0;
    const float sqrt_p = sqrt(abs(p));
    float phi = 27.0 * (0.25 * c1 * c1 * (p - c1) + c0 * (q + 6.75 * c0));
    phi = (1.0 / 3.0) * atan2(sqrt(abs(phi)), q);

    // 计算特征值
    const float c = sqrt_p * cos(phi);
    const float s = (1.0 / m_sqrt3) * sqrt_p * sin(phi);

    eigenvalues[1] = (1.0 / 3.0) * (m - c);
    eigenvalues[2] = eigenvalues[1] + s;
    eigenvalues[0] = eigenvalues[1] + c;
    eigenvalues[1] = eigenvalues[1] - s;

    // 计算误差容限
    float t = max(abs(eigenvalues[0]), abs(eigenvalues[1]));
    t = max(t, abs(eigenvalues[2]));
    float u = (t < 1.0) ? t : t * t;
    float error = 256.0 * 2.2204460492503131e-16 * u * u;

    // 初始化特征向量矩阵 Q
    for (int i = 0; i < 3; i++)
    {
        for (int j = 0; j < 3; j++)
        {
            Q[i][j] = 0;
        }
    }
    float Q_final[3][3] = {0, 0, 0, 0, 0, 0, 0, 0, 0};
    float eigenvalues_final[3] = {0, 0, 0};

    // 计算第一特征向量
    Q[0][1] = A[0][1] * A[1][2] - A[0][2] * A[1][1];
    Q[1][1] = A[0][2] * A[0][1] - A[1][2] * A[0][0];
    Q[2][1] = A[0][1] * A[0][1];

    Q[0][0] = Q[0][1] + A[0][2] * eigenvalues[0];
    Q[1][0] = Q[1][1] + A[1][2] * eigenvalues[0];
    Q[2][0] = (A[0][0] - eigenvalues[0]) * (A[1][1] - eigenvalues[0]) - Q[2][1];

    float norm = Q[0][0] * Q[0][0] + Q[1][0] * Q[1][0] + Q[2][0] * Q[2][0];
    bool early_ret = false;

    if (norm <= error)
    {
        // todo:调用备用方法 (需实现 dsyevq3)
        dsyevq3(A, Q, eigenvalues, Q_final, eigenvalues_final);
        early_ret = true;
    }
    else
    {
        norm = rsqrt(norm); // 1.0 / sqrt(norm)
        Q[0][0] *= norm;
        Q[1][0] *= norm;
        Q[2][0] *= norm;
    }

    if (!early_ret)
    {
        // 计算第二特征向量
        Q[0][1] = Q[0][1] + A[0][2] * eigenvalues[1];
        Q[1][1] = Q[1][1] + A[1][2] * eigenvalues[1];
        Q[2][1] = (A[0][0] - eigenvalues[1]) * (A[1][1] - eigenvalues[1]) - Q[2][1];
        norm = Q[0][1] * Q[0][1] + Q[1][1] * Q[1][1] + Q[2][1] * Q[2][1];
        if (norm <= error)
        {
            dsyevq3(A, Q, eigenvalues, Q_final, eigenvalues_final);
            early_ret = true;
        }
        else
        {
            norm = rsqrt(norm);
            Q[0][1] *= norm;
            Q[1][1] *= norm;
            Q[2][1] *= norm;
        }

        // 第三特征向量为前两个的叉乘
        Q[0][2] = Q[1][0] * Q[2][1] - Q[2][0] * Q[1][1];
        Q[1][2] = Q[2][0] * Q[0][1] - Q[0][0] * Q[2][1];
        Q[2][2] = Q[0][0] * Q[1][1] - Q[1][0] * Q[0][1];
    }
    // 处理备用方法结果
    if (early_ret)
    {
        Q = Q_final;
        eigenvalues = eigenvalues_final;
    }

    // 按升序排列特征值和特征向量
    if (eigenvalues[1] < eigenvalues[0]) swap_columns(Q, 0, 1, eigenvalues);
    if (eigenvalues[2] < eigenvalues[0]) swap_columns(Q, 0, 2, eigenvalues);
    if (eigenvalues[2] < eigenvalues[1]) swap_columns(Q, 1, 2, eigenvalues);
}


/**
 * 计算矩阵 A 的 SVD 分解
 * @param A 3x3矩阵
 * @param U 左奇异向量矩阵
 * @param sigma 奇异值矩阵
 * @param V 右奇异向量矩阵
 */
inline void svd3x3(const in float A[3][3], inout float U[3][3], inout float sigma[3][3], inout float V[3][3])
{
    for (int i = 0; i < 3; i++)
    {
        for (int j = 0; j < 3; j++)
            sigma[i][j] = 0;
    }
    SVD::svd(
        A[0][0], A[0][1], A[0][2], A[1][0], A[1][1], A[1][2], A[2][0], A[2][1], A[2][2],
        U[0][0], U[0][1], U[0][2], U[1][0], U[1][1], U[1][2], U[2][0], U[2][1], U[2][2],
        V[0][0], V[0][1], V[0][2], V[1][0], V[1][1], V[1][2], V[2][0], V[2][1], V[2][2],
        sigma[0][0], sigma[1][1], sigma[2][2]
    );
}

/**
* 在标准 SVD 分解的基础上，确保：
    1. U 和 V 的行列式为正（即它们是旋转矩阵，属于 SO(3) 群）。
    2. 通过调整符号保持分解的正确性。
 * @param A 3x3矩阵
 * @param U 左奇异向量矩阵
 * @param sigma 奇异值矩阵
 * @param V 右奇异向量矩阵
 */
inline void ssvd3x3(const in float A[3][3], inout float U[3][3], inout float sigma[3][3], inout float V[3][3])
{
    svd3x3(A, U, sigma, V);
    // 修正后的代码（适配二维数组）
    if (determinant_3x3(U) < 0)
    {
        // 修正 U 的第三列符号
        for (int i = 0; i < 3; i++)
        {
            U[i][2] *= -1.0f; // 二维数组索引 [i][2]
        }
        sigma[2][2] = -sigma[2][2]; // 修正 sigma 的最后一个奇异值[1](@ref)
    }

    if (determinant_3x3(V) < 0)
    {
        // 修正 V 的第三列符号
        for (int i = 0; i < 3; i++)
        {
            V[i][2] *= -1.0f; // 二维数组索引 [i][2]
        }
        sigma[2][2] = -sigma[2][2]; // 修正 sigma 的最后一个奇异值[1](@ref)
    }
}
