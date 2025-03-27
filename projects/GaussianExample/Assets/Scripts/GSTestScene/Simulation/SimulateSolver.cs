namespace GSTestScene.Simulation
{
    /// <summary>
    /// 存放物理求解、插值应用相关方法
    /// </summary>
    public partial class GaussianSimulator
    {
        /// <summary>
        /// 应用外力
        /// </summary>
        private void ApplyExternalForce(float dt0)
        {
            ApplyExternalForceCompute(dt0);
            SubmitTaskAndSynchronize();
        }

        /// <summary>
        /// 求解FEM（弹性形变）
        /// </summary>
        private void SolveFemConstraints()
        {
        }

        /// <summary>
        /// 求解碰撞约束
        /// </summary>
        private void SolveTrianglePointDistanceConstraint()
        {
        }

        /// <summary>
        /// 更新顶点位置
        /// </summary>
        private void PbdPostSolve()
        {
        }

        /// <summary>
        /// 将子步长的计算结果应用到顶点位置，完成时间步进
        /// </summary>
        private void PbdAdvance()
        {
        }

        /// <summary>
        /// 求解刚体变换（平移旋转等）
        /// </summary>
        private void SolveRigid()
        {
        }


        /// <summary>
        /// 应用顶点位置变换到GS上
        /// </summary>
        private void ApplyInterpolation()
        {
        }
    }
}