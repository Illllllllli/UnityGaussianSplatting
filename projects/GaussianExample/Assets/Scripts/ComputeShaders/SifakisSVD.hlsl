# ifndef _SVD_
# define _SVD_

const int sweeps = 4;
const float four_gamma_squared = 5.82842712474619f; // sqrt(8.) + 3.;
const float sine_pi_over_eight =
    0.3826834323650897f; // .5 * sqrt(2. - sqrt(2.));
const float cosine_pi_over_eight =
    0.9238795325112867f; //.5 * sqrt(2. + sqrt(2.));


// 按位与运算
inline float bitwise_and(const float a, const float b)
{
    return asfloat(asuint(a) & asuint(b));
}

// 按位或运算
inline float bitwise_or(const float a, const float b)
{
    return asfloat(asuint(a) | asuint(b));
}

// 按位取反运算

inline float bitwise_not(const float a)
{
    return asfloat(~asuint(a));
}

// 按位异或运算
inline float bitwise_xor(const float a, const float b)
{
    return asfloat(asuint(a) ^ asuint(b));
}

inline void svd(const float a11,
                const float a12,
                const float a13,
                const float a21,
                const float a22,
                const float a23,
                const float a31,
                const float a32,
                const float a33,
                inout float u11,
                inout float u12,
                inout float u13,
                inout float u21,
                inout float u22,
                inout float u23,
                inout float u31,
                inout float u32,
                inout float u33,
                inout float v11,
                inout float v12,
                inout float v13,
                inout float v21,
                inout float v22,
                inout float v23,
                inout float v31,
                inout float v32,
                inout float v33,
                inout float sigma1,
                inout float sigma2,
                inout float sigma3)
{
    // var
    float Sfour_gamma_squared;
    float Ssine_pi_over_eight;
    float Scosine_pi_over_eight;
    float Sone_half;
    float Sone;
    float Stiny_number;
    float Ssmall_number;
    float Sa11;
    float Sa21;
    float Sa31;
    float Sa12;
    float Sa22;
    float Sa32;
    float Sa13;
    float Sa23;
    float Sa33;

    float Sv11;
    float Sv21;
    float Sv31;
    float Sv12;
    float Sv22;
    float Sv32;
    float Sv13;
    float Sv23;
    float Sv33;
    float Su11;
    float Su21;
    float Su31;
    float Su12;
    float Su22;
    float Su32;
    float Su13;
    float Su23;
    float Su33;
    float Sc;
    float Ss;
    float Sch;
    float Ssh;
    float Stmp1;
    float Stmp2;
    float Stmp3;
    float Stmp4;
    float Stmp5;
    float Sqvs;
    float Sqvvx;
    float Sqvvy;
    float Sqvvz;

    float Ss11;
    float Ss21;
    float Ss31;
    float Ss22;
    float Ss32;
    float Ss33;

    // compute
    Sfour_gamma_squared = four_gamma_squared;
    Ssine_pi_over_eight = sine_pi_over_eight;
    Scosine_pi_over_eight = cosine_pi_over_eight;
    Sone_half = 0.5f;
    Sone = 1.0f;
    Stiny_number = 1.e-20f;
    Ssmall_number = 1.e-12f;

    Sa11 = a11;
    Sa21 = a21;
    Sa31 = a31;
    Sa12 = a12;
    Sa22 = a22;
    Sa32 = a32;
    Sa13 = a13;
    Sa23 = a23;
    Sa33 = a33;

    Sqvs = 1.0f;
    Sqvvx = 0.0f;
    Sqvvy = 0.0f;
    Sqvvz = 0.0f;

    Ss11 = Sa11 * Sa11;
    Stmp1 = Sa21 * Sa21;
    Ss11 = Stmp1 + Ss11;
    Stmp1 = Sa31 * Sa31;
    Ss11 = Stmp1 + Ss11;

    Ss21 = Sa12 * Sa11;
    Stmp1 = Sa22 * Sa21;
    Ss21 = Stmp1 + Ss21;
    Stmp1 = Sa32 * Sa31;
    Ss21 = Stmp1 + Ss21;

    Ss31 = Sa13 * Sa11;
    Stmp1 = Sa23 * Sa21;
    Ss31 = Stmp1 + Ss31;
    Stmp1 = Sa33 * Sa31;
    Ss31 = Stmp1 + Ss31;

    Ss22 = Sa12 * Sa12;
    Stmp1 = Sa22 * Sa22;
    Ss22 = Stmp1 + Ss22;
    Stmp1 = Sa32 * Sa32;
    Ss22 = Stmp1 + Ss22;

    Ss32 = Sa13 * Sa12;
    Stmp1 = Sa23 * Sa22;
    Ss32 = Stmp1 + Ss32;
    Stmp1 = Sa33 * Sa32;
    Ss32 = Stmp1 + Ss32;

    Ss33 = Sa13 * Sa13;
    Stmp1 = Sa23 * Sa23;
    Ss33 = Stmp1 + Ss33;
    Stmp1 = Sa33 * Sa33;
    Ss33 = Stmp1 + Ss33;

    for (int sweep = 0; sweep < sweeps; sweep++)
    {
        Ssh = Ss21 * Sone_half;
        Stmp5 = Ss11 - Ss22;
        Stmp2 = Ssh * Ssh;
        Stmp1 = (Stmp2 >= Stiny_number) ? asfloat(0xffffffff) : asfloat(0);
        Ssh = bitwise_and(Stmp1, Ssh);
        Sch = bitwise_and(Stmp1, Stmp5);
        Stmp2 = bitwise_and(bitwise_not(Stmp1), Sone);
        Sch = bitwise_or(Sch, Stmp2);

        Stmp1 = Ssh * Ssh;
        Stmp2 = Sch * Sch;
        Stmp3 = Stmp1 + Stmp2;
        Stmp4 = rsqrt(Stmp3);
        Ssh = Stmp4 * Ssh;
        Sch = Stmp4 * Sch;

        Stmp1 = Sfour_gamma_squared * Stmp1;
        Stmp1 = (Stmp2 <= Stmp1) ? asfloat(0xffffffff) : asfloat(0);

        Stmp2 = bitwise_and(Ssine_pi_over_eight, Stmp1);
        Ssh = bitwise_and(bitwise_not(Stmp1), Ssh);
        Ssh = bitwise_or(Ssh, Stmp2);
        Stmp2 = bitwise_and(Scosine_pi_over_eight, Stmp1);
        Sch = bitwise_and(bitwise_not(Stmp1), Sch);
        Sch = bitwise_or(Sch, Stmp2);

        Stmp1 = Ssh * Ssh;
        Stmp2 = Sch * Sch;
        Sc = Stmp2 - Stmp1;
        Ss = Sch * Ssh;
        Ss = Ss + Ss;

        Stmp3 = Stmp1 + Stmp2;
        Ss33 = Ss33 * Stmp3;
        Ss31 = Ss31 * Stmp3;
        Ss32 = Ss32 * Stmp3;
        Ss33 = Ss33 * Stmp3;

        Stmp1 = Ss * Ss31;
        Stmp2 = Ss * Ss32;
        Ss31 = Sc * Ss31;
        Ss32 = Sc * Ss32;
        Ss31 = Stmp2 + Ss31;
        Ss32 = Ss32 - Stmp1;

        Stmp2 = Ss * Ss;
        Stmp1 = Ss22 * Stmp2;
        Stmp3 = Ss11 * Stmp2;
        Stmp4 = Sc * Sc;
        Ss11 = Ss11 * Stmp4;
        Ss22 = Ss22 * Stmp4;
        Ss11 = Ss11 + Stmp1;
        Ss22 = Ss22 + Stmp3;
        Stmp4 = Stmp4 - Stmp2;
        Stmp2 = Ss21 + Ss21;
        Ss21 = Ss21 * Stmp4;
        Stmp4 = Sc * Ss;
        Stmp2 = Stmp2 * Stmp4;
        Stmp5 = Stmp5 * Stmp4;
        Ss11 = Ss11 + Stmp2;
        Ss21 = Ss21 - Stmp5;
        Ss22 = Ss22 - Stmp2;

        Stmp1 = Ssh * Sqvvx;
        Stmp2 = Ssh * Sqvvy;
        Stmp3 = Ssh * Sqvvz;
        Ssh = Ssh * Sqvs;

        Sqvs = Sch * Sqvs;
        Sqvvx = Sch * Sqvvx;
        Sqvvy = Sch * Sqvvy;
        Sqvvz = Sch * Sqvvz;

        Sqvvz = Sqvvz + Ssh;
        Sqvs = Sqvs - Stmp3;
        Sqvvx = Sqvvx + Stmp2;
        Sqvvy = Sqvvy - Stmp1;
        Ssh = Ss32 * Sone_half;
        Stmp5 = Ss22 - Ss33;

        Stmp2 = Ssh * Ssh;
        Stmp1 = (Stmp2 >= Stiny_number) ? asfloat(0xffffffff) : asfloat(0);
        Ssh = bitwise_and(Stmp1, Ssh);
        Sch = bitwise_and(Stmp1, Stmp5);
        Stmp2 = bitwise_and(bitwise_not(Stmp1), Sone);
        Sch = bitwise_or(Sch, Stmp2);

        Stmp1 = Ssh * Ssh;
        Stmp2 = Sch * Sch;
        Stmp3 = Stmp1 + Stmp2;
        Stmp4 = rsqrt(Stmp3);
        Ssh = Stmp4 * Ssh;
        Sch = Stmp4 * Sch;

        Stmp1 = Sfour_gamma_squared * Stmp1;
        Stmp1 = (Stmp2 <= Stmp1) ? asfloat(0xffffffff) : asfloat(0);

        Stmp2 = bitwise_and(Ssine_pi_over_eight, Stmp1);
        Ssh = bitwise_and(bitwise_not(Stmp1), Ssh);
        Ssh = bitwise_or(Ssh, Stmp2);
        Stmp2 = bitwise_and(Scosine_pi_over_eight, Stmp1);
        Sch = bitwise_and(bitwise_not(Stmp1), Sch);
        Sch = bitwise_or(Sch, Stmp2);

        Stmp1 = Ssh * Ssh;
        Stmp2 = Sch * Sch;
        Sc = Stmp2 - Stmp1;
        Ss = Sch * Ssh;
        Ss = Ss + Ss;

        Stmp3 = Stmp1 + Stmp2;
        Ss11 = Ss11 * Stmp3;
        Ss21 = Ss21 * Stmp3;
        Ss31 = Ss31 * Stmp3;
        Ss11 = Ss11 * Stmp3;

        Stmp1 = Ss * Ss21;
        Stmp2 = Ss * Ss31;
        Ss21 = Sc * Ss21;
        Ss31 = Sc * Ss31;
        Ss21 = Stmp2 + Ss21;
        Ss31 = Ss31 - Stmp1;

        Stmp2 = Ss * Ss;
        Stmp1 = Ss33 * Stmp2;
        Stmp3 = Ss22 * Stmp2;
        Stmp4 = Sc * Sc;
        Ss22 = Ss22 * Stmp4;
        Ss33 = Ss33 * Stmp4;
        Ss22 = Ss22 + Stmp1;
        Ss33 = Ss33 + Stmp3;
        Stmp4 = Stmp4 - Stmp2;
        Stmp2 = Ss32 + Ss32;
        Ss32 = Ss32 * Stmp4;
        Stmp4 = Sc * Ss;
        Stmp2 = Stmp2 * Stmp4;
        Stmp5 = Stmp5 * Stmp4;
        Ss22 = Ss22 + Stmp2;
        Ss32 = Ss32 - Stmp5;
        Ss33 = Ss33 - Stmp2;

        Stmp1 = Ssh * Sqvvx;
        Stmp2 = Ssh * Sqvvy;
        Stmp3 = Ssh * Sqvvz;
        Ssh = Ssh * Sqvs;

        Sqvs = Sch * Sqvs;
        Sqvvx = Sch * Sqvvx;
        Sqvvy = Sch * Sqvvy;
        Sqvvz = Sch * Sqvvz;

        Sqvvx = Sqvvx + Ssh;
        Sqvs = Sqvs - Stmp1;
        Sqvvy = Sqvvy + Stmp3;
        Sqvvz = Sqvvz - Stmp2;
        Ssh = Ss31 * Sone_half;
        Stmp5 = Ss33 - Ss11;

        Stmp2 = Ssh * Ssh;
        Stmp1 = (Stmp2 >= Stiny_number) ? asfloat(0xffffffff) : asfloat(0);
        Ssh = bitwise_and(Stmp1, Ssh);
        Sch = bitwise_and(Stmp1, Stmp5);
        Stmp2 = bitwise_and(bitwise_not(Stmp1), Sone);
        Sch = bitwise_or(Sch, Stmp2);

        Stmp1 = Ssh * Ssh;
        Stmp2 = Sch * Sch;
        Stmp3 = Stmp1 + Stmp2;
        Stmp4 = rsqrt(Stmp3);
        Ssh = Stmp4 * Ssh;
        Sch = Stmp4 * Sch;

        Stmp1 = Sfour_gamma_squared * Stmp1;
        Stmp1 = (Stmp2 <= Stmp1) ? asfloat(0xffffffff) : asfloat(0);

        Stmp2 = bitwise_and(Ssine_pi_over_eight, Stmp1);
        Ssh = bitwise_and(bitwise_not(Stmp1), Ssh);
        Ssh = bitwise_or(Ssh, Stmp2);
        Stmp2 = bitwise_and(Scosine_pi_over_eight, Stmp1);
        Sch = bitwise_and(bitwise_not(Stmp1), Sch);
        Sch = bitwise_or(Sch, Stmp2);

        Stmp1 = Ssh * Ssh;
        Stmp2 = Sch * Sch;
        Sc = Stmp2 - Stmp1;
        Ss = Sch * Ssh;
        Ss = Ss + Ss;

        Stmp3 = Stmp1 + Stmp2;
        Ss22 = Ss22 * Stmp3;
        Ss32 = Ss32 * Stmp3;
        Ss21 = Ss21 * Stmp3;
        Ss22 = Ss22 * Stmp3;

        Stmp1 = Ss * Ss32;
        Stmp2 = Ss * Ss21;
        Ss32 = Sc * Ss32;
        Ss21 = Sc * Ss21;
        Ss32 = Stmp2 + Ss32;
        Ss21 = Ss21 - Stmp1;

        Stmp2 = Ss * Ss;
        Stmp1 = Ss11 * Stmp2;
        Stmp3 = Ss33 * Stmp2;
        Stmp4 = Sc * Sc;
        Ss33 = Ss33 * Stmp4;
        Ss11 = Ss11 * Stmp4;
        Ss33 = Ss33 + Stmp1;
        Ss11 = Ss11 + Stmp3;
        Stmp4 = Stmp4 - Stmp2;
        Stmp2 = Ss31 + Ss31;
        Ss31 = Ss31 * Stmp4;
        Stmp4 = Sc * Ss;
        Stmp2 = Stmp2 * Stmp4;
        Stmp5 = Stmp5 * Stmp4;
        Ss33 = Ss33 + Stmp2;
        Ss31 = Ss31 - Stmp5;
        Ss11 = Ss11 - Stmp2;

        Stmp1 = Ssh * Sqvvx;
        Stmp2 = Ssh * Sqvvy;
        Stmp3 = Ssh * Sqvvz;
        Ssh = Ssh * Sqvs;

        Sqvs = Sch * Sqvs;
        Sqvvx = Sch * Sqvvx;
        Sqvvy = Sch * Sqvvy;
        Sqvvz = Sch * Sqvvz;

        Sqvvy = Sqvvy + Ssh;
        Sqvs = Sqvs - Stmp2;
        Sqvvz = Sqvvz + Stmp1;
        Sqvvx = Sqvvx - Stmp3;
    }

    Stmp2 = Sqvs * Sqvs;
    Stmp1 = Sqvvx * Sqvvx;
    Stmp2 = Stmp1 + Stmp2;
    Stmp1 = Sqvvy * Sqvvy;
    Stmp2 = Stmp1 + Stmp2;
    Stmp1 = Sqvvz * Sqvvz;
    Stmp2 = Stmp1 + Stmp2;

    Stmp1 = rsqrt(Stmp2);
    Stmp4 = Stmp1 * Sone_half;
    Stmp3 = Stmp1 * Stmp4;
    Stmp3 = Stmp1 * Stmp3;
    Stmp3 = Stmp2 * Stmp3;
    Stmp1 = Stmp1 + Stmp4;
    Stmp1 = Stmp1 - Stmp3;

    Sqvs = Sqvs * Stmp1;
    Sqvvx = Sqvvx * Stmp1;
    Sqvvy = Sqvvy * Stmp1;
    Sqvvz = Sqvvz * Stmp1;

    Stmp1 = Sqvvx * Sqvvx;
    Stmp2 = Sqvvy * Sqvvy;
    Stmp3 = Sqvvz * Sqvvz;
    Sv11 = Sqvs * Sqvs;
    Sv22 = Sv11 - Stmp1;
    Sv33 = Sv22 - Stmp2;
    Sv33 = Sv33 + Stmp3;
    Sv22 = Sv22 + Stmp2;
    Sv22 = Sv22 - Stmp3;
    Sv11 = Sv11 + Stmp1;
    Sv11 = Sv11 - Stmp2;
    Sv11 = Sv11 - Stmp3;
    Stmp1 = Sqvvx + Sqvvx;
    Stmp2 = Sqvvy + Sqvvy;
    Stmp3 = Sqvvz + Sqvvz;
    Sv32 = Sqvs * Stmp1;
    Sv13 = Sqvs * Stmp2;
    Sv21 = Sqvs * Stmp3;
    Stmp1 = Sqvvy * Stmp1;
    Stmp2 = Sqvvz * Stmp2;
    Stmp3 = Sqvvx * Stmp3;
    Sv12 = Stmp1 - Sv21;
    Sv23 = Stmp2 - Sv32;
    Sv31 = Stmp3 - Sv13;
    Sv21 = Stmp1 + Sv21;
    Sv32 = Stmp2 + Sv32;
    Sv13 = Stmp3 + Sv13;
    Stmp2 = Sa12;
    Stmp3 = Sa13;
    Sa12 = Sv12 * Sa11;
    Sa13 = Sv13 * Sa11;
    Sa11 = Sv11 * Sa11;
    Stmp1 = Sv21 * Stmp2;
    Sa11 = Sa11 + Stmp1;
    Stmp1 = Sv31 * Stmp3;
    Sa11 = Sa11 + Stmp1;
    Stmp1 = Sv22 * Stmp2;
    Sa12 = Sa12 + Stmp1;
    Stmp1 = Sv32 * Stmp3;
    Sa12 = Sa12 + Stmp1;
    Stmp1 = Sv23 * Stmp2;
    Sa13 = Sa13 + Stmp1;
    Stmp1 = Sv33 * Stmp3;
    Sa13 = Sa13 + Stmp1;

    Stmp2 = Sa22;
    Stmp3 = Sa23;
    Sa22 = Sv12 * Sa21;
    Sa23 = Sv13 * Sa21;
    Sa21 = Sv11 * Sa21;
    Stmp1 = Sv21 * Stmp2;
    Sa21 = Sa21 + Stmp1;
    Stmp1 = Sv31 * Stmp3;
    Sa21 = Sa21 + Stmp1;
    Stmp1 = Sv22 * Stmp2;
    Sa22 = Sa22 + Stmp1;
    Stmp1 = Sv32 * Stmp3;
    Sa22 = Sa22 + Stmp1;
    Stmp1 = Sv23 * Stmp2;
    Sa23 = Sa23 + Stmp1;
    Stmp1 = Sv33 * Stmp3;
    Sa23 = Sa23 + Stmp1;

    Stmp2 = Sa32;
    Stmp3 = Sa33;
    Sa32 = Sv12 * Sa31;
    Sa33 = Sv13 * Sa31;
    Sa31 = Sv11 * Sa31;
    Stmp1 = Sv21 * Stmp2;
    Sa31 = Sa31 + Stmp1;
    Stmp1 = Sv31 * Stmp3;
    Sa31 = Sa31 + Stmp1;
    Stmp1 = Sv22 * Stmp2;
    Sa32 = Sa32 + Stmp1;
    Stmp1 = Sv32 * Stmp3;
    Sa32 = Sa32 + Stmp1;
    Stmp1 = Sv23 * Stmp2;
    Sa33 = Sa33 + Stmp1;
    Stmp1 = Sv33 * Stmp3;
    Sa33 = Sa33 + Stmp1;

    Stmp1 = Sa11 * Sa11;
    Stmp4 = Sa21 * Sa21;
    Stmp1 = Stmp1 + Stmp4;
    Stmp4 = Sa31 * Sa31;
    Stmp1 = Stmp1 + Stmp4;

    Stmp2 = Sa12 * Sa12;
    Stmp4 = Sa22 * Sa22;
    Stmp2 = Stmp2 + Stmp4;
    Stmp4 = Sa32 * Sa32;
    Stmp2 = Stmp2 + Stmp4;

    Stmp3 = Sa13 * Sa13;
    Stmp4 = Sa23 * Sa23;
    Stmp3 = Stmp3 + Stmp4;
    Stmp4 = Sa33 * Sa33;
    Stmp3 = Stmp3 + Stmp4;

    Stmp4 = (Stmp1 < Stmp2) ? asfloat(0xffffffff) : asfloat(0);

    Stmp5 = bitwise_xor(Sa11, Sa12);
    Stmp5 = bitwise_and(Stmp5, Stmp4);
    Sa11 = bitwise_xor(Sa11, Stmp5);
    Sa12 = bitwise_xor(Sa12, Stmp5);

    Stmp5 = bitwise_xor(Sa21, Sa22);
    Stmp5 = bitwise_and(Stmp5, Stmp4);
    Sa21 = bitwise_xor(Sa21, Stmp5);
    Sa22 = bitwise_xor(Sa22, Stmp5);

    Stmp5 = bitwise_xor(Sa31, Sa32);
    Stmp5 = bitwise_and(Stmp5, Stmp4);
    Sa31 = bitwise_xor(Sa31, Stmp5);
    Sa32 = bitwise_xor(Sa32, Stmp5);

    Stmp5 = bitwise_xor(Sv11, Sv12);
    Stmp5 = bitwise_and(Stmp5, Stmp4);
    Sv11 = bitwise_xor(Sv11, Stmp5);
    Sv12 = bitwise_xor(Sv12, Stmp5);

    Stmp5 = bitwise_xor(Sv21, Sv22);
    Stmp5 = bitwise_and(Stmp5, Stmp4);
    Sv21 = bitwise_xor(Sv21, Stmp5);
    Sv22 = bitwise_xor(Sv22, Stmp5);

    Stmp5 = bitwise_xor(Sv31, Sv32);
    Stmp5 = bitwise_and(Stmp5, Stmp4);
    Sv31 = bitwise_xor(Sv31, Stmp5);
    Sv32 = bitwise_xor(Sv32, Stmp5);

    Stmp5 = bitwise_xor(Stmp1, Stmp2);
    Stmp5 = bitwise_and(Stmp5, Stmp4);
    Stmp1 = bitwise_xor(Stmp1, Stmp5);
    Stmp2 = bitwise_xor(Stmp2, Stmp5);

    Stmp5 = -2.0f;
    Stmp5 = bitwise_and(Stmp5, Stmp4);
    Stmp4 = 1.0f;
    Stmp4 = Stmp4 + Stmp5;

    Sa12 = Sa12 * Stmp4;
    Sa22 = Sa22 * Stmp4;
    Sa32 = Sa32 * Stmp4;

    Sv12 = Sv12 * Stmp4;
    Sv22 = Sv22 * Stmp4;
    Sv32 = Sv32 * Stmp4;
    Stmp4 = (Stmp1 < Stmp3) ? asfloat(0xffffffff) : asfloat(0);

    Stmp5 = bitwise_xor(Sa11, Sa13);
    Stmp5 = bitwise_and(Stmp5, Stmp4);
    Sa11 = bitwise_xor(Sa11, Stmp5);
    Sa13 = bitwise_xor(Sa13, Stmp5);

    Stmp5 = bitwise_xor(Sa21, Sa23);
    Stmp5 = bitwise_and(Stmp5, Stmp4);
    Sa21 = bitwise_xor(Sa21, Stmp5);
    Sa23 = bitwise_xor(Sa23, Stmp5);

    Stmp5 = bitwise_xor(Sa31, Sa33);
    Stmp5 = bitwise_and(Stmp5, Stmp4);
    Sa31 = bitwise_xor(Sa31, Stmp5);
    Sa33 = bitwise_xor(Sa33, Stmp5);

    Stmp5 = bitwise_xor(Sv11, Sv13);
    Stmp5 = bitwise_and(Stmp5, Stmp4);
    Sv11 = bitwise_xor(Sv11, Stmp5);
    Sv13 = bitwise_xor(Sv13, Stmp5);

    Stmp5 = bitwise_xor(Sv21, Sv23);
    Stmp5 = bitwise_and(Stmp5, Stmp4);
    Sv21 = bitwise_xor(Sv21, Stmp5);
    Sv23 = bitwise_xor(Sv23, Stmp5);

    Stmp5 = bitwise_xor(Sv31, Sv33);
    Stmp5 = bitwise_and(Stmp5, Stmp4);
    Sv31 = bitwise_xor(Sv31, Stmp5);
    Sv33 = bitwise_xor(Sv33, Stmp5);

    Stmp5 = bitwise_xor(Stmp1, Stmp3);
    Stmp5 = bitwise_and(Stmp5, Stmp4);
    Stmp1 = bitwise_xor(Stmp1, Stmp5);
    Stmp3 = bitwise_xor(Stmp3, Stmp5);

    Stmp5 = -2.0f;
    Stmp5 = bitwise_and(Stmp5, Stmp4);
    Stmp4 = 1.0f;
    Stmp4 = Stmp4 + Stmp5;

    Sa11 = Sa11 * Stmp4;
    Sa21 = Sa21 * Stmp4;
    Sa31 = Sa31 * Stmp4;

    Sv11 = Sv11 * Stmp4;
    Sv21 = Sv21 * Stmp4;
    Sv31 = Sv31 * Stmp4;
    Stmp4 = (Stmp2 < Stmp3) ? asfloat(0xffffffff) : asfloat(0);

    Stmp5 = bitwise_xor(Sa12, Sa13);
    Stmp5 = bitwise_and(Stmp5, Stmp4);
    Sa12 = bitwise_xor(Sa12, Stmp5);
    Sa13 = bitwise_xor(Sa13, Stmp5);

    Stmp5 = bitwise_xor(Sa22, Sa23);
    Stmp5 = bitwise_and(Stmp5, Stmp4);
    Sa22 = bitwise_xor(Sa22, Stmp5);
    Sa23 = bitwise_xor(Sa23, Stmp5);

    Stmp5 = bitwise_xor(Sa32, Sa33);
    Stmp5 = bitwise_and(Stmp5, Stmp4);
    Sa32 = bitwise_xor(Sa32, Stmp5);
    Sa33 = bitwise_xor(Sa33, Stmp5);

    Stmp5 = bitwise_xor(Sv12, Sv13);
    Stmp5 = bitwise_and(Stmp5, Stmp4);
    Sv12 = bitwise_xor(Sv12, Stmp5);
    Sv13 = bitwise_xor(Sv13, Stmp5);

    Stmp5 = bitwise_xor(Sv22, Sv23);
    Stmp5 = bitwise_and(Stmp5, Stmp4);
    Sv22 = bitwise_xor(Sv22, Stmp5);
    Sv23 = bitwise_xor(Sv23, Stmp5);

    Stmp5 = bitwise_xor(Sv32, Sv33);
    Stmp5 = bitwise_and(Stmp5, Stmp4);
    Sv32 = bitwise_xor(Sv32, Stmp5);
    Sv33 = bitwise_xor(Sv33, Stmp5);

    Stmp5 = bitwise_xor(Stmp2, Stmp3);
    Stmp5 = bitwise_and(Stmp5, Stmp4);
    Stmp2 = bitwise_xor(Stmp2, Stmp5);
    Stmp3 = bitwise_xor(Stmp3, Stmp5);

    Stmp5 = -2.0f;
    Stmp5 = bitwise_and(Stmp5, Stmp4);
    Stmp4 = 1.0f;
    Stmp4 = Stmp4 + Stmp5;

    Sa13 = Sa13 * Stmp4;
    Sa23 = Sa23 * Stmp4;
    Sa33 = Sa33 * Stmp4;

    Sv13 = Sv13 * Stmp4;
    Sv23 = Sv23 * Stmp4;
    Sv33 = Sv33 * Stmp4;
    Su11 = 1.0f;
    Su21 = 0.0f;
    Su31 = 0.0f;
    Su12 = 0.0f;
    Su22 = 1.0f;
    Su32 = 0.0f;
    Su13 = 0.0f;
    Su23 = 0.0f;
    Su33 = 1.0f;
    Ssh = Sa21 * Sa21;
    Ssh = (Ssh >= Ssmall_number) ? asfloat(0xffffffff) : asfloat(0);

    Ssh = bitwise_and(Ssh, Sa21);

    Stmp5 = 0.0f;
    Sch = Stmp5 - Sa11;
    Sch = max(Sch, Sa11);
    Sch = max(Sch, Ssmall_number);
    Stmp5 = (Sa11 >= Stmp5) ? asfloat(0xffffffff) : asfloat(0);

    Stmp1 = Sch * Sch;
    Stmp2 = Ssh * Ssh;
    Stmp2 = Stmp1 + Stmp2;
    Stmp1 = rsqrt(Stmp2);

    Stmp4 = Stmp1 * Sone_half;
    Stmp3 = Stmp1 * Stmp4;
    Stmp3 = Stmp1 * Stmp3;
    Stmp3 = Stmp2 * Stmp3;
    Stmp1 = Stmp1 + Stmp4;
    Stmp1 = Stmp1 - Stmp3;
    Stmp1 = Stmp1 * Stmp2;

    Sch = Sch + Stmp1;

    Stmp1 = bitwise_and(bitwise_not(Stmp5), Ssh);
    Stmp2 = bitwise_and(bitwise_not(Stmp5), Sch);
    Sch = bitwise_and(Stmp5, Sch);
    Ssh = bitwise_and(Stmp5, Ssh);
    Sch = bitwise_or(Sch, Stmp1);
    Ssh = bitwise_or(Ssh, Stmp2);

    Stmp1 = Sch * Sch;
    Stmp2 = Ssh * Ssh;
    Stmp2 = Stmp1 + Stmp2;
    Stmp1 = rsqrt(Stmp2);

    Stmp4 = Stmp1 * Sone_half;
    Stmp3 = Stmp1 * Stmp4;
    Stmp3 = Stmp1 * Stmp3;
    Stmp3 = Stmp2 * Stmp3;
    Stmp1 = Stmp1 + Stmp4;
    Stmp1 = Stmp1 - Stmp3;

    Sch = Sch * Stmp1;
    Ssh = Ssh * Stmp1;

    Sc = Sch * Sch;
    Ss = Ssh * Ssh;
    Sc = Sc - Ss;
    Ss = Ssh * Sch;
    Ss = Ss + Ss;

    Stmp1 = Ss * Sa11;
    Stmp2 = Ss * Sa21;
    Sa11 = Sc * Sa11;
    Sa21 = Sc * Sa21;
    Sa11 = Sa11 + Stmp2;
    Sa21 = Sa21 - Stmp1;

    Stmp1 = Ss * Sa12;
    Stmp2 = Ss * Sa22;
    Sa12 = Sc * Sa12;
    Sa22 = Sc * Sa22;
    Sa12 = Sa12 + Stmp2;
    Sa22 = Sa22 - Stmp1;

    Stmp1 = Ss * Sa13;
    Stmp2 = Ss * Sa23;
    Sa13 = Sc * Sa13;
    Sa23 = Sc * Sa23;
    Sa13 = Sa13 + Stmp2;
    Sa23 = Sa23 - Stmp1;

    Stmp1 = Ss * Su11;
    Stmp2 = Ss * Su12;
    Su11 = Sc * Su11;
    Su12 = Sc * Su12;
    Su11 = Su11 + Stmp2;
    Su12 = Su12 - Stmp1;

    Stmp1 = Ss * Su21;
    Stmp2 = Ss * Su22;
    Su21 = Sc * Su21;
    Su22 = Sc * Su22;
    Su21 = Su21 + Stmp2;
    Su22 = Su22 - Stmp1;

    Stmp1 = Ss * Su31;
    Stmp2 = Ss * Su32;
    Su31 = Sc * Su31;
    Su32 = Sc * Su32;
    Su31 = Su31 + Stmp2;
    Su32 = Su32 - Stmp1;
    Ssh = Sa31 * Sa31;
    Ssh = (Ssh >= Ssmall_number) ? asfloat(0xffffffff) : asfloat(0);

    Ssh = bitwise_and(Ssh, Sa31);

    Stmp5 = 0.0f;
    Sch = Stmp5 - Sa11;
    Sch = max(Sch, Sa11);
    Sch = max(Sch, Ssmall_number);
    Stmp5 = (Sa11 >= Stmp5) ? asfloat(0xffffffff) : asfloat(0);

    Stmp1 = Sch * Sch;
    Stmp2 = Ssh * Ssh;
    Stmp2 = Stmp1 + Stmp2;
    Stmp1 = rsqrt(Stmp2);

    Stmp4 = Stmp1 * Sone_half;
    Stmp3 = Stmp1 * Stmp4;
    Stmp3 = Stmp1 * Stmp3;
    Stmp3 = Stmp2 * Stmp3;
    Stmp1 = Stmp1 + Stmp4;
    Stmp1 = Stmp1 - Stmp3;
    Stmp1 = Stmp1 * Stmp2;

    Sch = Sch + Stmp1;

    Stmp1 = bitwise_and(bitwise_not(Stmp5), Ssh);
    Stmp2 = bitwise_and(bitwise_not(Stmp5), Sch);
    Sch = bitwise_and(Stmp5, Sch);
    Ssh = bitwise_and(Stmp5, Ssh);
    Sch = bitwise_or(Sch, Stmp1);
    Ssh = bitwise_or(Ssh, Stmp2);

    Stmp1 = Sch * Sch;
    Stmp2 = Ssh * Ssh;
    Stmp2 = Stmp1 + Stmp2;
    Stmp1 = rsqrt(Stmp2);

    Stmp4 = Stmp1 * Sone_half;
    Stmp3 = Stmp1 * Stmp4;
    Stmp3 = Stmp1 * Stmp3;
    Stmp3 = Stmp2 * Stmp3;
    Stmp1 = Stmp1 + Stmp4;
    Stmp1 = Stmp1 - Stmp3;

    Sch = Sch * Stmp1;
    Ssh = Ssh * Stmp1;

    Sc = Sch * Sch;
    Ss = Ssh * Ssh;
    Sc = Sc - Ss;
    Ss = Ssh * Sch;
    Ss = Ss + Ss;

    Stmp1 = Ss * Sa11;
    Stmp2 = Ss * Sa31;
    Sa11 = Sc * Sa11;
    Sa31 = Sc * Sa31;
    Sa11 = Sa11 + Stmp2;
    Sa31 = Sa31 - Stmp1;

    Stmp1 = Ss * Sa12;
    Stmp2 = Ss * Sa32;
    Sa12 = Sc * Sa12;
    Sa32 = Sc * Sa32;
    Sa12 = Sa12 + Stmp2;
    Sa32 = Sa32 - Stmp1;

    Stmp1 = Ss * Sa13;
    Stmp2 = Ss * Sa33;
    Sa13 = Sc * Sa13;
    Sa33 = Sc * Sa33;
    Sa13 = Sa13 + Stmp2;
    Sa33 = Sa33 - Stmp1;

    Stmp1 = Ss * Su11;
    Stmp2 = Ss * Su13;
    Su11 = Sc * Su11;
    Su13 = Sc * Su13;
    Su11 = Su11 + Stmp2;
    Su13 = Su13 - Stmp1;

    Stmp1 = Ss * Su21;
    Stmp2 = Ss * Su23;
    Su21 = Sc * Su21;
    Su23 = Sc * Su23;
    Su21 = Su21 + Stmp2;
    Su23 = Su23 - Stmp1;

    Stmp1 = Ss * Su31;
    Stmp2 = Ss * Su33;
    Su31 = Sc * Su31;
    Su33 = Sc * Su33;
    Su31 = Su31 + Stmp2;
    Su33 = Su33 - Stmp1;
    Ssh = Sa32 * Sa32;
    Ssh = (Ssh >= Ssmall_number) ? asfloat(0xffffffff) : asfloat(0);

    Ssh = bitwise_and(Ssh, Sa32);

    Stmp5 = 0.0f;
    Sch = Stmp5 - Sa22;
    Sch = max(Sch, Sa22);
    Sch = max(Sch, Ssmall_number);
    Stmp5 = (Sa22 >= Stmp5) ? asfloat(0xffffffff) : asfloat(0);

    Stmp1 = Sch * Sch;
    Stmp2 = Ssh * Ssh;
    Stmp2 = Stmp1 + Stmp2;
    Stmp1 = rsqrt(Stmp2);

    Stmp4 = Stmp1 * Sone_half;
    Stmp3 = Stmp1 * Stmp4;
    Stmp3 = Stmp1 * Stmp3;
    Stmp3 = Stmp2 * Stmp3;
    Stmp1 = Stmp1 + Stmp4;
    Stmp1 = Stmp1 - Stmp3;
    Stmp1 = Stmp1 * Stmp2;

    Sch = Sch + Stmp1;

    Stmp1 = bitwise_and(bitwise_not(Stmp5), Ssh);
    Stmp2 = bitwise_and(bitwise_not(Stmp5), Sch);
    Sch = bitwise_and(Stmp5, Sch);
    Ssh = bitwise_and(Stmp5, Ssh);
    Sch = bitwise_or(Sch, Stmp1);
    Ssh = bitwise_or(Ssh, Stmp2);

    Stmp1 = Sch * Sch;
    Stmp2 = Ssh * Ssh;
    Stmp2 = Stmp1 + Stmp2;
    Stmp1 = rsqrt(Stmp2);

    Stmp4 = Stmp1 * Sone_half;
    Stmp3 = Stmp1 * Stmp4;
    Stmp3 = Stmp1 * Stmp3;
    Stmp3 = Stmp2 * Stmp3;
    Stmp1 = Stmp1 + Stmp4;
    Stmp1 = Stmp1 - Stmp3;

    Sch = Sch * Stmp1;
    Ssh = Ssh * Stmp1;

    Sc = Sch * Sch;
    Ss = Ssh * Ssh;
    Sc = Sc - Ss;
    Ss = Ssh * Sch;
    Ss = Ss + Ss;

    Stmp1 = Ss * Sa21;
    Stmp2 = Ss * Sa31;
    Sa21 = Sc * Sa21;
    Sa31 = Sc * Sa31;
    Sa21 = Sa21 + Stmp2;
    Sa31 = Sa31 - Stmp1;

    Stmp1 = Ss * Sa22;
    Stmp2 = Ss * Sa32;
    Sa22 = Sc * Sa22;
    Sa32 = Sc * Sa32;
    Sa22 = Sa22 + Stmp2;
    Sa32 = Sa32 - Stmp1;

    Stmp1 = Ss * Sa23;
    Stmp2 = Ss * Sa33;
    Sa23 = Sc * Sa23;
    Sa33 = Sc * Sa33;
    Sa23 = Sa23 + Stmp2;
    Sa33 = Sa33 - Stmp1;

    Stmp1 = Ss * Su12;
    Stmp2 = Ss * Su13;
    Su12 = Sc * Su12;
    Su13 = Sc * Su13;
    Su12 = Su12 + Stmp2;
    Su13 = Su13 - Stmp1;

    Stmp1 = Ss * Su22;
    Stmp2 = Ss * Su23;
    Su22 = Sc * Su22;
    Su23 = Sc * Su23;
    Su22 = Su22 + Stmp2;
    Su23 = Su23 - Stmp1;

    Stmp1 = Ss * Su32;
    Stmp2 = Ss * Su33;
    Su32 = Sc * Su32;
    Su33 = Sc * Su33;
    Su32 = Su32 + Stmp2;
    Su33 = Su33 - Stmp1;
    // end

    u11 = Su11;
    u21 = Su21;
    u31 = Su31;
    u12 = Su12;
    u22 = Su22;
    u32 = Su32;
    u13 = Su13;
    u23 = Su23;
    u33 = Su33;

    v11 = Sv11;
    v21 = Sv21;
    v31 = Sv31;
    v12 = Sv12;
    v22 = Sv22;
    v32 = Sv32;
    v13 = Sv13;
    v23 = Sv23;
    v33 = Sv33;

    sigma1 = Sa11;
    sigma2 = Sa22;
    sigma3 = Sa33;
    // output
}

# endif
