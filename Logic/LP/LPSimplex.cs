using System;
using System.Collections.Generic;
using UnityEngine;

namespace DSPCalculator.Logic.LP
{
    /// <summary>
    /// 两阶段单纯形求解器（替换原 Big-M 单阶段实现）。
    /// 求解: min c'x  s.t.  A x >= b,  x >= 0   （b >= 0；b &lt; 0 的行对可行域无约束，跳过）
    /// 等式化: A x - s + a = b，其中 s 为模型自带的 surplus 列（已含溢出惩罚成本，位于模型列内），
    /// a 为求解器为阶段一追加的人工变量列。
    ///
    /// 为什么抛弃 Big-M：1e7 成本经行变换后目标行是 1e7~1e10 与 1e-9 容差混在一起的数字垃圾；
    /// 蓝buff 等"整列消耗被清零"的情形会制造大量零系数行列 → 严重退化 → 转轴噪声让人工变量
    /// 以微小正值滞留在基中，把实际有解的模型误判为"无解"（DFS 用户能算、LP 弹窗）。
    /// 两阶段法阶段一只有 0/1 成本，且进入变量全程用 Bland 规则（最小下标），保证不会循环。
    /// 另外：旧实现给每行额外加了一列成本 0 的自由 surplus，导致模型自带的 OVERFLOW_PENALTY
    /// 从未生效；本实现不再重复加列。
    /// </summary>
    public static class LPSimplex
    {
        private const double TOL = 1e-9;          // 检验数（reduced cost）判负容差
        private const double PIVOT_EPS = 1e-12;   // 主元绝对值下限
        private const int MAX_ITER = 200000;      // Bland 规则保证有限，这里只是保险

        public static LPResult Solve(LPModel model)
        {
            int m = model.numConstraints;
            int n = model.numVars;

            if (m == 0 || n == 0)
            {
                return new LPResult { feasible = true, bounded = true, objectiveValue = 0, solution = new double[n] };
            }

            // 增广列表布局: 列 [0, n) = 模型变量（含其 surplus），列 [n, n+m) = 人工变量，最后一列 = RHS
            int cols = n + m;
            double[,] T = new double[m + 1, cols + 1]; // 行 0 = 目标行；行 1..m = 约束
            int[] basis = new int[m];

            // 阶段一可行残差判定的问题尺度（需求速率可达数百/千，用绝对容差 1e-6 足够）
            const double FEAS_TOL_ABS = 1e-6;

            for (int i = 0; i < m; i++)
            {
                basis[i] = n + i;
                if (model.b[i] < 0)
                {
                    // A x - s >= b (b<0) 在 x=0,s=0 时显然成立，整行置零视作冗余
                    continue;
                }
                for (int j = 0; j < n; j++)
                {
                    T[i + 1, j] = model.A[i, j];
                }
                T[i + 1, n + i] = 1.0;
                T[i + 1, cols] = model.b[i];
            }

            // ===== 阶段一目标行: min Σ a =====
            // 初始基全是人工变量（c_B 全为 1）：检验数 = c_j − Σ_i T[i,j]。
            // 模型列 c_j=0；人工列 c=1 且自身列为单位向量，检验数恰好为 0（下方显式置 0），
            // 因此人工变量在阶段一不会被选为进入变量。
            for (int j = 0; j <= cols; j++)
            {
                double sum = 0;
                for (int i = 1; i <= m; i++) sum += T[i, j];
                T[0, j] = -sum;
            }
            for (int i = 0; i < m; i++)
            {
                T[0, n + i] = 0;
            }

            // ===== 阶段一单纯形迭代 =====
            bool aborted = false;
            int iter1 = RunSimplex(T, basis, n, m, cols, null, ref aborted);
            if (aborted)
            {
                Utils.logger.LogWarning("LP Simplex: 阶段一超出迭代上限");
            }

            // 可行性判定：基中人工变量残差超过问题尺度容差 → 真无解，收集证据行
            var infeasibleRows = new List<int>();
            var infeasibleResiduals = new List<double>();
            for (int i = 0; i < m; i++)
            {
                if (basis[i] >= n && T[i + 1, cols] > FEAS_TOL_ABS)
                {
                    infeasibleRows.Add(i);
                    infeasibleResiduals.Add(T[i + 1, cols]);
                }
            }
            if (infeasibleRows.Count > 0)
            {
                return new LPResult
                {
                    feasible = false,
                    bounded = true,
                    objectiveValue = 0,
                    solution = new double[n],
                    infeasibleRows = infeasibleRows,
                    infeasibleResiduals = infeasibleResiduals,
                };
            }

            // 把取值为 0 的基人工变量挤出基（退化主元），否则阶段二允许其增长会掩盖真实缺口。
            for (int i = 0; i < m; i++)
            {
                if (basis[i] < n) continue;
                int swapCol = -1;
                for (int j = 0; j < n; j++)
                {
                    if (Math.Abs(T[i + 1, j]) > PIVOT_EPS) { swapCol = j; break; }
                }
                if (swapCol >= 0)
                {
                    Pivot(T, basis, i, swapCol, cols);
                }
                // 找不到可换入列说明该行是冗余行（系数全零、RHS≈0），基中留一个恒为 0 的人工变量无害
            }

            // ===== 阶段二目标行: min c'x（人工列成本视作 +∞，用进入扫描跳过实现） =====
            for (int j = 0; j < cols; j++)
            {
                T[0, j] = j < n ? model.objective[j] : 0.0;
            }
            T[0, cols] = 0.0;
            for (int i = 0; i < m; i++)
            {
                double cB = basis[i] < n ? model.objective[basis[i]] : 0.0;
                if (Math.Abs(cB) > 0)
                {
                    for (int j = 0; j <= cols; j++)
                    {
                        T[0, j] -= cB * T[i + 1, j];
                    }
                }
            }

            bool aborted2 = false;
            int iter2 = RunSimplex(T, basis, n, m, cols, model.objective, ref aborted2);
            if (aborted2)
            {
                Utils.logger.LogWarning("LP Simplex: 阶段二超出迭代上限");
            }
            if (iter2 < 0)
            {
                // RunSimplex 返回 -1 表示无界（本模型理论上 cost>0 不会发生，防御性上报）
                return new LPResult { feasible = true, bounded = false, objectiveValue = double.NegativeInfinity, solution = new double[n] };
            }

            // 提取解与目标值（直接由解重算，避免表格 RHS 的符号歧义）
            double[] solution = new double[n];
            for (int i = 0; i < m; i++)
            {
                if (basis[i] < n)
                {
                    solution[basis[i]] = T[i + 1, cols];
                }
            }
            double objVal = 0;
            for (int j = 0; j < n; j++) objVal += model.objective[j] * solution[j];

            // 后验回代：把解代回模型各行验证 Σ A x >= b。
            // 防护阶段二期间"滞留在基中的零值人工变量"随其他变量转轴而增长的理论边角，
            // 那种情形会把真实缺口伪装成可行解；一旦发现行缺口超容差，如实上报无解并给出证据行。
            var badRows = new List<int>();
            var badResiduals = new List<double>();
            for (int i = 0; i < m; i++)
            {
                if (model.b[i] < 0) continue;
                double lhs = 0;
                for (int j = 0; j < n; j++) lhs += model.A[i, j] * solution[j];
                double gap = model.b[i] - lhs;
                if (gap > FEAS_TOL_ABS)
                {
                    badRows.Add(i);
                    badResiduals.Add(gap);
                }
            }
            if (badRows.Count > 0)
            {
                return new LPResult
                {
                    feasible = false,
                    bounded = true,
                    objectiveValue = 0,
                    solution = solution,
                    infeasibleRows = badRows,
                    infeasibleResiduals = badResiduals,
                };
            }

            return new LPResult
            {
                feasible = true,
                bounded = true,
                objectiveValue = objVal,
                solution = solution,
            };
        }

        /// <summary>
        /// 单纯形主循环。进入变量用 Bland 规则（检验数为负的最小列下标），平局离基也用 Bland，
        /// 保证退化情形不会循环。phase2Objective 非空时表示阶段二：人工列禁止换入。
        /// 返回迭代次数；判定无界返回 -1。
        /// </summary>
        private static int RunSimplex(double[,] T, int[] basis, int n, int m, int cols, double[] phase2Objective, ref bool aborted)
        {
            int iter = 0;
            while (iter < MAX_ITER)
            {
                iter++;

                int enterCol = -1;
                int scanLimit = phase2Objective != null ? n : T.GetLength(1) - 1; // 阶段二跳过人工列
                for (int j = 0; j < scanLimit; j++)
                {
                    if (T[0, j] < -TOL)
                    {
                        enterCol = j;
                        break; // Bland: 取最小下标
                    }
                }
                if (enterCol < 0)
                    return iter; // 最优

                int leaveRow = -1;
                double minRatio = double.PositiveInfinity;
                for (int i = 0; i < m; i++)
                {
                    if (T[i + 1, enterCol] > PIVOT_EPS)
                    {
                        double ratio = T[i + 1, cols] / T[i + 1, enterCol];
                        if (ratio < minRatio - 1e-12)
                        {
                            minRatio = ratio;
                            leaveRow = i;
                        }
                        else if (Math.Abs(ratio - minRatio) <= 1e-12 && leaveRow >= 0 && basis[i] < basis[leaveRow])
                        {
                            leaveRow = i; // Bland 平局: 基变量下标小者
                        }
                    }
                }

                if (leaveRow < 0)
                {
                    return -1; // 无界
                }

                Pivot(T, basis, leaveRow, enterCol, cols);
            }
            aborted = true;
            return iter;
        }

        private static void Pivot(double[,] T, int[] basis, int leaveRow, int enterCol, int cols)
        {
            int r = leaveRow + 1;
            double pivotVal = T[r, enterCol];
            for (int j = 0; j <= cols; j++)
            {
                T[r, j] /= pivotVal;
            }
            int rowCount = T.GetLength(0);
            for (int i = 0; i < rowCount; i++)
            {
                if (i == r) continue;
                double factor = T[i, enterCol];
                if (Math.Abs(factor) > PIVOT_EPS)
                {
                    for (int j = 0; j <= cols; j++)
                    {
                        T[i, j] -= factor * T[r, j];
                    }
                }
            }
            basis[leaveRow] = enterCol;
        }
    }
}
