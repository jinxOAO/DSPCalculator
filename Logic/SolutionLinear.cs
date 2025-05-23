using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra.Solvers;
using UnityEngine;

namespace DSPCalculator.Logic
{
    /// <summary>
    /// 使用线性规划方法求解产线问题
    /// </summary>
    public class SolutionLinear
    {
        public UserPreference userPreference;
        public List<ItemTarget> targets;
        public Dictionary<int, double> itemSpeeds; // 每个物品的净产出速度（正数为产出，负数为消耗）
        public Dictionary<int, double> recipeMultipliers; // 每个配方的使用倍率
        public Dictionary<int, double> proliferatorCount; // 增产剂使用量
        public Dictionary<int, double> proliferatorCountSelfSprayed; // 自喷涂的增产剂使用量

        // 用于存储计算过程中的中间数据
        private Dictionary<int, List<NormalizedRecipe>> itemToRecipes; // 每个物品可以被哪些配方生产
        private Dictionary<int, bool> isItemProcessed; // 记录物品是否已经被处理过
        private List<int> allItems; // 所有涉及到的物品ID
        private List<NormalizedRecipe> allRecipes; // 所有可能用到的配方

        // 求解参数
        private const double EPSILON = 1e-10; // 数值精度
        private const int MAX_ITERATIONS = 1000; // 最大迭代次数

        public SolutionLinear()
        {
            userPreference = new UserPreference();
            targets = new List<ItemTarget>();
            itemSpeeds = new Dictionary<int, double>();
            recipeMultipliers = new Dictionary<int, double>();
            proliferatorCount = new Dictionary<int, double>();
            proliferatorCountSelfSprayed = new Dictionary<int, double>();
            
            itemToRecipes = new Dictionary<int, List<NormalizedRecipe>>();
            isItemProcessed = new Dictionary<int, bool>();
            allItems = new List<int>();
            allRecipes = new List<NormalizedRecipe>();
        }

        /// <summary>
        /// 从SolutionTree复制目标和用户配置
        /// </summary>
        public void CopyFromTree(SolutionTree tree)
        {
            userPreference = tree.userPreference.DeepCopy();
            targets = new List<ItemTarget>();
            foreach (var target in tree.targets)
            {
                targets.Add(new ItemTarget(target.itemId, target.speed));
            }
        }

        /// <summary>
        /// 收集所有可能用到的物品和配方
        /// </summary>
        private void CollectItemsAndRecipes()
        {
            Queue<int> itemsToProcess = new Queue<int>();
            
            // 首先加入目标产物
            foreach (var target in targets)
            {
                if (!isItemProcessed.ContainsKey(target.itemId))
                {
                    itemsToProcess.Enqueue(target.itemId);
                    isItemProcessed[target.itemId] = false;
                }
            }

            // 处理每个物品，找出其相关配方和原材料
            while (itemsToProcess.Count > 0)
            {
                int itemId = itemsToProcess.Dequeue();
                if (isItemProcessed[itemId]) continue;

                // 标记物品为已处理
                isItemProcessed[itemId] = true;
                allItems.Add(itemId);

                // 如果是原矿，跳过配方收集
                if (IsOre(itemId)) continue;

                // 收集可用的配方
                var recipes = new List<NormalizedRecipe>();
                if (userPreference.itemConfigs.ContainsKey(itemId) && 
                    userPreference.itemConfigs[itemId].recipeID > 0)
                {
                    // 用户指定了配方
                    var recipe = CalcDB.recipeDict[userPreference.itemConfigs[itemId].recipeID];
                    recipes.Add(recipe);
                }
                else
                {
                    // 使用所有可用配方
                    recipes.AddRange(CalcDB.itemDict[itemId].recipes);
                }

                itemToRecipes[itemId] = recipes;

                // 将配方加入总配方列表
                foreach (var recipe in recipes)
                {
                    if (!allRecipes.Contains(recipe))
                    {
                        allRecipes.Add(recipe);
                        // 将配方中的所有物品加入待处理队列
                        for (int i = 0; i < recipe.resources.Length; i++)
                        {
                            int resourceId = recipe.resources[i];
                            if (!isItemProcessed.ContainsKey(resourceId))
                            {
                                itemsToProcess.Enqueue(resourceId);
                                isItemProcessed[resourceId] = false;
                            }
                        }
                        for (int i = 0; i < recipe.products.Length; i++)
                        {
                            int productId = recipe.products[i];
                            if (!isItemProcessed.ContainsKey(productId))
                            {
                                itemsToProcess.Enqueue(productId);
                                isItemProcessed[productId] = false;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 检查物品是否为原矿
        /// </summary>
        private bool IsOre(int itemId)
        {
            bool isOre = CalcDB.itemDict[itemId].defaultAsOre || CalcDB.itemDict[itemId].recipes.Count == 0;
            if (userPreference.itemConfigs.ContainsKey(itemId))
            {
                isOre = userPreference.itemConfigs[itemId].consideredAsOre || isOre;
                if (userPreference.itemConfigs[itemId].forceNotOre && CalcDB.itemDict[itemId].recipes.Count > 0)
                    isOre = false;
            }
            return isOre;
        }

        /// <summary>
        /// 使用线性规划求解产线
        /// </summary>
        public bool Solve()
        {
            try
            {
                // 清理旧数据
                itemSpeeds.Clear();
                recipeMultipliers.Clear();
                itemToRecipes.Clear();
                isItemProcessed.Clear();
                allItems.Clear();
                allRecipes.Clear();

                // 收集所有相关的物品和配方
                CollectItemsAndRecipes();

                // 构建线性规划矩阵
                var matrix = Matrix<double>.Build.Dense(allItems.Count, allRecipes.Count);
                var vector = Vector<double>.Build.Dense(allItems.Count);

                // 填充矩阵
                for (int i = 0; i < allItems.Count; i++)
                {
                    int itemId = allItems[i];
                    
                    // 设置目标需求
                    double targetSpeed = 0;
                    foreach (var target in targets)
                    {
                        if (target.itemId == itemId)
                        {
                            targetSpeed += target.speed;
                        }
                    }
                    vector[i] = targetSpeed;

                    // 填充每个配方对该物品的影响
                    for (int j = 0; j < allRecipes.Count; j++)
                    {
                        var recipe = allRecipes[j];
                        
                        // 检查产物
                        for (int k = 0; k < recipe.products.Length; k++)
                        {
                            if (recipe.products[k] == itemId)
                            {
                                matrix[i, j] += recipe.productCounts[k] * (60.0 / recipe.time);
                            }
                        }
                        
                        // 检查原材料
                        for (int k = 0; k < recipe.resources.Length; k++)
                        {
                            if (recipe.resources[k] == itemId)
                            {
                                matrix[i, j] -= recipe.resourceCounts[k] * (60.0 / recipe.time);
                            }
                        }
                    }
                }

                // 添加非负约束（所有配方使用量必须大于等于0）
                var constraintRows = allRecipes.Count;
                var totalRows = allItems.Count + constraintRows;
                var augmentedMatrix = Matrix<double>.Build.Dense(totalRows, allRecipes.Count);
                var augmentedVector = Vector<double>.Build.Dense(totalRows);

                // 复制原始方程
                for (int i = 0; i < allItems.Count; i++)
                {
                    for (int j = 0; j < allRecipes.Count; j++)
                    {
                        augmentedMatrix[i, j] = matrix[i, j];
                    }
                    augmentedVector[i] = vector[i];
                }

                // 添加非负约束
                for (int i = 0; i < constraintRows; i++)
                {
                    augmentedMatrix[allItems.Count + i, i] = 1.0;
                    augmentedVector[allItems.Count + i] = 0.0;
                }

                // 使用LSQR（最小二乘法）求解器
                var solver = new LSQR();
                var solution = Vector<double>.Build.Dense(allRecipes.Count);
                
                // 设置求解器参数
                solver.Tolerance = EPSILON;
                solver.MaxIterations = MAX_ITERATIONS;
                
                // 求解最小二乘问题
                solver.Solve(augmentedMatrix, augmentedVector, solution);

                // 验证解的有效性
                var residual = augmentedMatrix.Multiply(solution).Subtract(augmentedVector);
                if (residual.L2Norm() > EPSILON * Math.Sqrt(totalRows))
                {
                    Debug.LogWarning("线性规划求解可能不准确，残差较大");
                }

                // 保存结果
                for (int i = 0; i < allRecipes.Count; i++)
                {
                    if (solution[i] > EPSILON) // 只保存正值
                    {
                        recipeMultipliers[allRecipes[i].ID] = solution[i];
                    }
                }

                // 计算每个物品的净产出速度
                foreach (int itemId in allItems)
                {
                    double speed = 0;
                    
                    // 计算所有配方对该物品的影响
                    foreach (var pair in recipeMultipliers)
                    {
                        var recipe = CalcDB.recipeDict[pair.Key];
                        double multiplier = pair.Value;

                        // 加上产出
                        for (int i = 0; i < recipe.products.Length; i++)
                        {
                            if (recipe.products[i] == itemId)
                            {
                                speed += recipe.productCounts[i] * multiplier * (60.0 / recipe.time);
                            }
                        }

                        // 减去消耗
                        for (int i = 0; i < recipe.resources.Length; i++)
                        {
                            if (recipe.resources[i] == itemId)
                            {
                                speed -= recipe.resourceCounts[i] * multiplier * (60.0 / recipe.time);
                            }
                        }
                    }

                    if (Math.Abs(speed) > EPSILON)
                    {
                        itemSpeeds[itemId] = speed;
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"线性规划求解失败: {e.Message}");
                return false;
            }
        }
    }
} 