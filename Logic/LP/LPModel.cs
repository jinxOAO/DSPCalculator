using System;
using System.Collections.Generic;

namespace DSPCalculator.Logic.LP
{
    /// <summary>
    /// 线性规划模型定义。
    /// 标准形式：min c'x  s.t.  A*x >= b, x >= 0
    /// 通过引入松弛变量和人工变量转化为等式标准形。
    /// </summary>
    public class LPModel
    {
        /// <summary>
        /// 变量数量
        /// </summary>
        public int numVars;
        /// <summary>
        /// 约束数量 (>= 类型的行数)
        /// </summary>
        public int numConstraints;
        /// <summary>
        /// 目标函数系数 c (长度 = numVars)，最小化
        /// </summary>
        public double[] objective;
        /// <summary>
        /// 约束矩阵 A (numConstraints x numVars)
        /// </summary>
        public double[,] A;
        /// <summary>
        /// 约束右端 b (长度 = numConstraints)，A*x >= b
        /// </summary>
        public double[] b;
        /// <summary>
        /// 变量名称（调试用）
        /// </summary>
        public string[] varNames;
        /// <summary>
        /// 约束名称（调试用）
        /// </summary>
        public string[] constraintNames;

        public LPModel(int numVars, int numConstraints)
        {
            this.numVars = numVars;
            this.numConstraints = numConstraints;
            objective = new double[numVars];
            A = new double[numConstraints, numVars];
            b = new double[numConstraints];
            varNames = new string[numVars];
            constraintNames = new string[numConstraints];
        }

        public void SetObjective(int varIdx, double cost)
        {
            objective[varIdx] = cost;
        }

        public void SetConstraintCoeff(int row, int col, double value)
        {
            A[row, col] = value;
        }

        public void AddConstraintCoeff(int row, int col, double value)
        {
            A[row, col] += value;
        }

        public void SetRHS(int row, double value)
        {
            b[row] = value;
        }
    }

    /// <summary>
    /// LP 求解结果
    /// </summary>
    public class LPResult
    {
        public bool feasible;
        public bool bounded;
        public double objectiveValue;
        public double[] solution; // 每个变量的最优值

        /// <summary>
        /// 判定无解时，人工变量残差超过容差的约束行号（对应模型 numConstraints 的索引），供上层输出诊断
        /// </summary>
        public List<int> infeasibleRows;
        /// <summary>
        /// 与 infeasibleRows 对应的人工变量残差值（即该行无法满足的缺口量）
        /// </summary>
        public List<double> infeasibleResiduals;

        public LPResult()
        {
            feasible = false;
            bounded = true;
            objectiveValue = 0;
        }
    }
}
