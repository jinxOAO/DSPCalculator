using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using UnityEngine;

namespace DSPCalculator.Logic
{
    /// <summary>
    /// 使用约束最小二乘法求解产线问题
    /// </summary>
    public class SolutionLinear
    {
        public UserPreference userPreference;
        public List<ItemTarget> targets;
        
        // 结果存储
        public Dictionary<int, RecipeInfo> recipeInfos; // RecipeInfo字典
        public Dictionary<int, ItemNode> itemNodes; // ItemNode字典
        
        // 内部计算数据
        private Dictionary<int, int> selectedRecipes; // 每个物品对应的主要配方ID
        private List<int> allItems; // 所有相关物品（包括原矿）
        private List<int> constraintItems; // 参与约束的物品（所有非原矿物品）
        private List<int> oreItems; // 原矿物品
        private List<int> allRecipes; // 所有相关配方
        private Dictionary<int, int> itemToIndex; // 物品ID到矩阵行索引的映射（仅针对constraintItems）
        private Dictionary<int, int> recipeToIndex; // 配方ID到矩阵列索引的映射
        
        private const double EPSILON = 1e-10;

        public SolutionLinear()
        {
            userPreference = new UserPreference();
            targets = new List<ItemTarget>();
            recipeInfos = new Dictionary<int, RecipeInfo>();
            itemNodes = new Dictionary<int, ItemNode>();
            selectedRecipes = new Dictionary<int, int>();
            allItems = new List<int>();
            constraintItems = new List<int>();
            oreItems = new List<int>();
            allRecipes = new List<int>();
            itemToIndex = new Dictionary<int, int>();
            recipeToIndex = new Dictionary<int, int>();
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
        /// 递归收集所有相关的物品和配方
        /// </summary>
        private void CollectItemsAndRecipes()
        {
            var processedItems = new HashSet<int>();
            var processedRecipes = new HashSet<int>();
            
            // 从目标物品开始递归收集
            foreach (var target in targets)
            {
                CollectItemRecursive(target.itemId, processedItems, processedRecipes);
            }
            
            // 如果需要求解增产剂，递归收集增产剂的生产配方
            if (userPreference.solveProliferators)
            {
                int[] proliferatorIds = { 1141, 1142, 1143 }; // 增产剂MK.I, MK.II, MK.III
                foreach (int proliferatorId in proliferatorIds)
                {
                    // 对每个增产剂递归收集其生产配方
                    CollectItemRecursive(proliferatorId, processedItems, processedRecipes);
                }
            }
            
            // 确保物品列表无重复（去重）
            allItems = allItems.Distinct().ToList();
            
            // 确保配方列表无重复（去重）
            var originalRecipeCount = allRecipes.Count;
            allRecipes = allRecipes.Distinct().ToList();
            if (originalRecipeCount != allRecipes.Count)
            {
                Debug.LogWarning($"配方列表存在重复项！原始数量：{originalRecipeCount}，去重后：{allRecipes.Count}");
            }
            
            // 分离原矿和非原矿物品
            constraintItems.Clear();
            oreItems.Clear();
            
            foreach (int itemId in allItems)
            {
                if (IsOre(itemId))
                {
                    oreItems.Add(itemId);
                }
                else
                {
                    constraintItems.Add(itemId);
                }
            }
            
            // 验证无重复
            var constraintItemsSet = new HashSet<int>(constraintItems);
            var oreItemsSet = new HashSet<int>(oreItems);
            
            if (constraintItems.Count != constraintItemsSet.Count)
            {
                Debug.LogError($"constraintItems中存在重复项！原始数量：{constraintItems.Count}，去重后：{constraintItemsSet.Count}");
                constraintItems = constraintItemsSet.ToList();
            }
            
            if (oreItems.Count != oreItemsSet.Count)
            {
                Debug.LogError($"oreItems中存在重复项！原始数量：{oreItems.Count}，去重后：{oreItemsSet.Count}");
                oreItems = oreItemsSet.ToList();
            }
            
            // 验证没有交集
            var intersection = constraintItemsSet.Intersect(oreItemsSet).ToList();
            if (intersection.Count > 0)
            {
                Debug.LogError($"constraintItems和oreItems存在交集：{string.Join(",", intersection)}");
            }
            
            // Debug.Log($"收集到 {allItems.Count} 个物品，其中 {constraintItems.Count} 个参与约束的物品，{oreItems.Count} 个原矿，{allRecipes.Count} 个配方。增产剂求解：{userPreference.solveProliferators}");
        }

        /// <summary>
        /// 递归收集单个物品的相关配方和原材料
        /// </summary>
        private void CollectItemRecursive(int itemId, HashSet<int> processedItems, HashSet<int> processedRecipes)
        {
            // 如果已处理过，跳过
            if (processedItems.Contains(itemId))
            {
                return;
            }

            // 如果是原矿，直接加入物品列表
            if (IsOre(itemId))
            {
                if (!allItems.Contains(itemId))
                    allItems.Add(itemId);
                return;
            }

            processedItems.Add(itemId);
            if (!allItems.Contains(itemId))
                allItems.Add(itemId);

            // 确定该物品的主要配方
            int selectedRecipeId = GetPreferredRecipe(itemId);
            if (selectedRecipeId > 0)
            {
                selectedRecipes[itemId] = selectedRecipeId;
                
                // 如果配方未处理过，加入配方列表并递归处理原材料
                if (!processedRecipes.Contains(selectedRecipeId))
                {
                    processedRecipes.Add(selectedRecipeId);
                    allRecipes.Add(selectedRecipeId);
                    
                    var recipe = CalcDB.recipeDict[selectedRecipeId];
                    
                    // 递归处理该配方的所有原材料
                    for (int i = 0; i < recipe.resources.Length; i++)
                    {
                        int resourceId = recipe.resources[i];
                        CollectItemRecursive(resourceId, processedItems, processedRecipes);
                    }
                    
                    // 处理该配方的所有产物（包括副产物）
                    // 注意：主产物应该已经在上面通过processedItems处理了
                    for (int i = 0; i < recipe.products.Length; i++)
                    {
                        int productId = recipe.products[i];
                        if (!processedItems.Contains(productId) && !allItems.Contains(productId))
                        {
                            allItems.Add(productId);
                            // 注意：这里不递归处理副产物的配方，因为副产物不需要主要配方
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 获取物品的首选配方ID
        /// 优先级：临时配方选择(selectedRecipes) > 用户偏好配置 > 默认配方(第0号)
        /// </summary>
        private int GetPreferredRecipe(int itemId)
        {
            // 优先检查临时配方选择（用于配方组合尝试）
            if (selectedRecipes.ContainsKey(itemId))
            {
                return selectedRecipes[itemId];
            }
            
            // 检查用户偏好配置
            if (userPreference.itemConfigs.ContainsKey(itemId) && 
                userPreference.itemConfigs[itemId].recipeID > 0)
            {
                return userPreference.itemConfigs[itemId].recipeID;
            }
            
            // 使用第0号配方（默认配方）
            var recipes = CalcDB.itemDict[itemId].recipes;
            if (recipes != null && recipes.Count > 0)
            {
                return recipes[0].ID;
            }
            
            return 0;
        }

        /// <summary>
        /// 获取单位配方的增产剂消耗（count=1时）
        /// 返回字典：增产剂物品ID -> 消耗数量
        /// </summary>
        private Dictionary<int, double> GetUnitProliferatorUsage(RecipeInfo recipeInfo)
        {
            var result = new Dictionary<int, double>();
            
            if (!userPreference.solveProliferators)
                return result;
                
            // 保存原来的count
            double originalCount = recipeInfo.count;
            
            // 临时设置count为1来获取单位消耗
            recipeInfo.count = 1.0;
            
            try
            {
                int proliferatorId;
                double proliferatorCount;
                recipeInfo.GetProliferatorUsed(out proliferatorId, out proliferatorCount);
                
                if (proliferatorId > 0 && proliferatorCount > 0)
                {
                    result[proliferatorId] = proliferatorCount;
                }
            }
            finally
            {
                // 恢复原来的count
                recipeInfo.count = originalCount;
            }
            
            return result;
        }

        /// <summary>
        /// 使用加权约束最小二乘法求解
        /// 目标：最小化 ||W(Ax - b)||² + λ||x||²
        /// 其中 W 是权重矩阵，目标产物权重高，副产物权重低
        /// </summary>
        private Vector<double> SolveConstrainedLeastSquares(Matrix<double> A, Vector<double> b)
        {
            double lambda = 1e-6; // 正则化参数，用于减少溢出
            
            // 构建权重矩阵W
            var weights = Vector<double>.Build.Dense(constraintItems.Count);
            for (int i = 0; i < constraintItems.Count; i++)
            {
                int itemId = constraintItems[i];
                bool isTargetItem = targets.Any(t => t.itemId == itemId);
                
                if (isTargetItem)
                {
                    weights[i] = 1000.0; // 目标产物高权重
                }
                else
                {
                    weights[i] = 0.001; // 非目标产物低权重，允许溢出
                }
            }
            
            // 构建加权矩阵 WA 和加权目标向量 Wb
            var WA = Matrix<double>.Build.Dense(constraintItems.Count, allRecipes.Count);
            var Wb = Vector<double>.Build.Dense(constraintItems.Count);
            
            for (int i = 0; i < constraintItems.Count; i++)
            {
                Wb[i] = weights[i] * b[i];
                for (int j = 0; j < allRecipes.Count; j++)
                {
                    WA[i, j] = weights[i] * A[i, j];
                }
            }
            
            // Debug.Log("权重分配:");
            for (int i = 0; i < constraintItems.Count; i++)
            {
                int itemId = constraintItems[i];
                bool isTargetItem = targets.Any(t => t.itemId == itemId);
                string itemType = isTargetItem ? "目标产物" : "副产物/中间产物";
                // Debug.Log($"  物品{itemId}({itemType}): 权重={weights[i]}");
            }
            
            try
            {
                // 使用加权正则化的最小二乘法：(WA^T * WA + λI)^(-1) * WA^T * Wb
                var WAtWA = WA.Transpose() * WA;
                var regularizedMatrix = WAtWA + lambda * Matrix<double>.Build.DenseIdentity(WAtWA.RowCount);
                var WAtWb = WA.Transpose() * Wb;
                
                // 求解线性方程组
                var solution = regularizedMatrix.Solve(WAtWb);
                
                // 确保所有配方倍率非负
                for (int i = 0; i < solution.Count; i++)
                {
                    if (solution[i] < 0)
                        solution[i] = 0;
                }
                
                return solution;
            }
            catch (Exception e)
            {
                Debug.LogError($"加权矩阵求解失败: {e.Message}");
                
                // 如果求解失败，尝试使用伪逆
                try
                {
                    var pseudoInverse = WA.PseudoInverse();
                    var solution = pseudoInverse * Wb;
                    
                    // 确保所有配方倍率非负
                    for (int i = 0; i < solution.Count; i++)
                    {
                        if (solution[i] < 0)
                            solution[i] = 0;
                    }
                    
                    return solution;
                }
                catch (Exception e2)
                {
                    Debug.LogError($"加权伪逆求解也失败: {e2.Message}");
                    return Vector<double>.Build.Dense(allRecipes.Count);
                }
            }
        }

        /// <summary>
        /// 使用约束最小二乘法求解
        /// </summary>
        public bool Solve()
        {
            try
            {
                // 完全清理所有数据，从干净状态开始
                ClearAllData();
                
                // 尝试默认配方组合（此时selectedRecipes为空，使用用户偏好和默认配方）
                if (TrySolveWithCurrentRecipes())
                {
                    return true;
                }
                
                // 如果默认配方组合无解，尝试其他配方组合
                // Debug.Log("默认配方组合无解，开始尝试其他配方组合...");
                return TryAlternativeRecipes();
            }
            catch (Exception e)
            {
                Debug.LogError($"线性规划求解失败: {e.Message}\n{e.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// 尝试使用当前配方组合求解
        /// </summary>
        private bool TrySolveWithCurrentRecipes()
        {
            try
            {
                // 显示当前的配方选择状态
                // Debug.Log("=== 开始尝试求解 ===");
                // Debug.Log($"当前selectedRecipes状态：");
                foreach (var kvp in selectedRecipes)
                {
                    string itemName = CalcDB.itemDict.ContainsKey(kvp.Key) ? CalcDB.itemDict[kvp.Key].ID.ToString() : "未知物品";
                    string recipeName = CalcDB.recipeDict.ContainsKey(kvp.Value) ? CalcDB.recipeDict[kvp.Value].ID.ToString() : "未知配方";
                    // Debug.Log($"  物品{kvp.Key}({itemName}) -> 配方{kvp.Value}({recipeName})");
                }

                // 清理旧数据
                ClearSolveData();

                // 1. 收集所有相关的物品和配方
                CollectItemsAndRecipes();

                if (allRecipes.Count == 0)
                {
                    Debug.LogWarning("未找到任何配方需要求解");
                    return false;
                }

                // 显示收集到的配方
                // Debug.Log($"收集到的配方列表：");
                foreach (int recipeId in allRecipes)
                {
                    string recipeName = CalcDB.recipeDict.ContainsKey(recipeId) ? CalcDB.recipeDict[recipeId].ID.ToString() : "未知配方";
                    // Debug.Log($"  配方{recipeId}({recipeName})");
                }

                // 2. 初始化RecipeInfo字典
                InitializeRecipeInfos();

                // 3. 建立索引映射（只对约束物品建立映射）
                BuildIndexMappings();

                // 4. 构建约束矩阵和目标向量
                var constraintMatrix = BuildConstraintMatrix();
                var targetVector = BuildTargetVector();

                // 调试输出
                // PrintDebugInfo(constraintMatrix, targetVector);

                // 5. 使用约束最小二乘法求解
                var solution = SolveConstrainedLeastSquares(constraintMatrix, targetVector);

                // 检查解的有效性
                if (!IsSolutionValid(solution, constraintMatrix, targetVector))
                {
                    // Debug.Log("求解结果无效");
                    return false;
                }

                // 调试输出求解结果
                // PrintSolutionInfo(solution);

                // 6. 处理求解结果
                ProcessSolution(solution);

                // 7. 构建ItemNode字典
                BuildItemNodes();

                // Debug.Log("求解成功！");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"求解过程中出现错误: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 完全清理所有数据（包括selectedRecipes）
        /// </summary>
        private void ClearAllData()
        {
            recipeInfos.Clear();
            itemNodes.Clear();
            selectedRecipes.Clear();
            allItems.Clear();
            constraintItems.Clear();
            oreItems.Clear();
            allRecipes.Clear();
            itemToIndex.Clear();
            recipeToIndex.Clear();
        }

        /// <summary>
        /// 清理求解数据（但保留selectedRecipes的临时配方选择）
        /// </summary>
        private void ClearSolveData()
        {
            recipeInfos.Clear();
            itemNodes.Clear();
            // 注意：不要清理selectedRecipes，因为那是我们要保持的临时配方选择！
            allItems.Clear();
            constraintItems.Clear();
            oreItems.Clear();
            allRecipes.Clear();
            itemToIndex.Clear();
            recipeToIndex.Clear();
        }

        /// <summary>
        /// 建立索引映射
        /// </summary>
        private void BuildIndexMappings()
        {
            for (int i = 0; i < constraintItems.Count; i++)
                itemToIndex[constraintItems[i]] = i;
            for (int i = 0; i < allRecipes.Count; i++)
                recipeToIndex[allRecipes[i]] = i;
        }

        /// <summary>
        /// 检查求解结果是否有效
        /// </summary>
        private bool IsSolutionValid(Vector<double> solution, Matrix<double> constraintMatrix, Vector<double> targetVector)
        {
            // 检查是否有负值（配方倍率不能为负）
            for (int i = 0; i < solution.Count; i++)
            {
                if (solution[i] < -EPSILON)
                {
                    // Debug.Log($"求解结果包含负配方倍率: 配方{allRecipes[i]} = {solution[i]}");
                    return false;
                }
            }

            // 检查约束是否满足
            var result = constraintMatrix * solution;
            double maxError = 0;
            bool allTargetsSatisfied = true;
            
            for (int i = 0; i < constraintItems.Count; i++)
            {
                double actual = result[i];
                double target = targetVector[i];
                
                // 检查该物品是否是目标产物
                bool isTargetItem = targets.Any(t => t.itemId == constraintItems[i]);
                
                if (isTargetItem)
                {
                    // 对于目标产物：实际产出必须大于等于目标需求
                    if (actual < target - 0.01) // 允许小的数值误差
                    {
                        // Debug.Log($"目标物品{constraintItems[i]}产出不足: 目标={target:F3}, 实际={actual:F3}, 缺口={target - actual:F3}");
                        allTargetsSatisfied = false;
                    }
                    else if (Math.Abs(actual - target) <= 0.01)
                    {
                        // Debug.Log($"目标物品{constraintItems[i]}精确满足: 目标={target:F3}, 实际={actual:F3}");
                    }
                    else
                    {
                        // Debug.Log($"目标物品{constraintItems[i]}有超产: 目标={target:F3}, 实际={actual:F3}, 超产={actual - target:F3}");
                    }
                }
                else
                {
                    // 对于非目标产物：允许溢出，但不应该有大缺口
                    if (actual < -0.1)
                    {
                        // Debug.Log($"中间物品{constraintItems[i]}有缺口: 实际={actual:F3} (可能需要原矿补充)");
                        // 注意：不设为失败，因为原矿可以补充
                    }
                    else if (actual > 0.1)
                    {
                        // Debug.Log($"中间物品{constraintItems[i]}有溢出: 实际={actual:F3} (副产物或过量生产)");
                    }
                    else
                    {
                        // Debug.Log($"中间物品{constraintItems[i]}供需平衡: 实际={actual:F3}");
                    }
                }
                
                double error = Math.Abs(actual - target);
                maxError = Math.Max(maxError, error);
            }

            if (!allTargetsSatisfied)
            {
                // Debug.Log("部分目标产物需求未满足");
                return false;
            }

            // Debug.Log($"约束检查通过，最大误差: {maxError:F6}，所有目标产物需求已满足");
            return true;
        }

        /// <summary>
        /// 尝试其他配方组合
        /// </summary>
        private bool TryAlternativeRecipes()
        {
            // 找出所有有多个配方的物品
            var multiRecipeItems = FindMultiRecipeItems();
            
            if (multiRecipeItems.Count == 0)
            {
                // Debug.Log("没有多配方物品可以尝试");
                return false;
            }

            // Debug.Log($"找到 {multiRecipeItems.Count} 个多配方物品，开始尝试不同组合");

            // 尝试不同的配方组合
            return TryRecipeCombinations(multiRecipeItems, 0);
        }

        /// <summary>
        /// 找出所有有多个配方的物品
        /// </summary>
        private List<int> FindMultiRecipeItems()
        {
            var multiRecipeItems = new List<int>();
            
            foreach (var target in targets)
            {
                FindMultiRecipeItemsRecursive(target.itemId, multiRecipeItems, new HashSet<int>());
            }
            
            return multiRecipeItems.Distinct().ToList();
        }

        /// <summary>
        /// 递归查找多配方物品（只包含用户没有明确指定配方的物品）
        /// </summary>
        private void FindMultiRecipeItemsRecursive(int itemId, List<int> multiRecipeItems, HashSet<int> visited)
        {
            if (visited.Contains(itemId) || IsOre(itemId))
                return;
                
            visited.Add(itemId);
            
            // 只有在用户没有明确指定配方的情况下，才考虑该物品的多配方选择
            bool userSpecifiedRecipe = userPreference.itemConfigs.ContainsKey(itemId) && 
                                     userPreference.itemConfigs[itemId].recipeID > 0;
            
            if (!userSpecifiedRecipe)
            {
                var recipes = CalcDB.itemDict[itemId].recipes;
                if (recipes != null && recipes.Count > 1)
                {
                    multiRecipeItems.Add(itemId);
                    // Debug.Log($"物品{itemId}有{recipes.Count}个可选配方，用户未指定，将尝试不同配方");
                }
            }
            else
            {
                // Debug.Log($"物品{itemId}的配方已被用户指定为{userPreference.itemConfigs[itemId].recipeID}，跳过");
            }
            
            // 递归处理当前选择配方的原材料
            int selectedRecipeId = GetPreferredRecipe(itemId);
            if (selectedRecipeId > 0)
            {
                var recipe = CalcDB.recipeDict[selectedRecipeId];
                for (int i = 0; i < recipe.resources.Length; i++)
                {
                    FindMultiRecipeItemsRecursive(recipe.resources[i], multiRecipeItems, visited);
                }
            }
        }

        /// <summary>
        /// 递归尝试不同的配方组合
        /// </summary>
        private bool TryRecipeCombinations(List<int> multiRecipeItems, int currentIndex)
        {
            if (currentIndex >= multiRecipeItems.Count)
            {
                // 已经设置完所有可变配方，尝试求解
                // Debug.Log("所有可变配方已设置完毕，开始求解...");
                return TrySolveWithCurrentRecipes();
            }

            int itemId = multiRecipeItems[currentIndex];
            var recipes = CalcDB.itemDict[itemId].recipes;
            
            // 记录原始状态
            bool hadOriginalSelection = selectedRecipes.ContainsKey(itemId);
            int originalRecipe = hadOriginalSelection ? selectedRecipes[itemId] : 0;
            
            for (int i = 0; i < recipes.Count; i++)
            {
                int newRecipeId = recipes[i].ID;
                
                // 设置新的配方选择
                selectedRecipes[itemId] = newRecipeId;
                
                // Debug.Log($"设置物品{itemId}使用配方{newRecipeId} ({i+1}/{recipes.Count})");
                
                // 递归设置下一个物品的配方
                if (TryRecipeCombinations(multiRecipeItems, currentIndex + 1))
                {
                    // Debug.Log($"找到可行解！最终配方组合中物品{itemId}使用配方{newRecipeId}");
                    return true; // 不需要调用UpdateMainRecipes，因为已经在TrySolveWithCurrentRecipes中处理了
                }
                
                // Debug.Log($"配方{newRecipeId}组合不可行，尝试下一个配方");
            }
            
            // 所有配方都尝试失败，恢复原始状态
            if (hadOriginalSelection)
            {
                selectedRecipes[itemId] = originalRecipe;
            }
            else
            {
                selectedRecipes.Remove(itemId);
            }
            
            // Debug.Log($"物品{itemId}的所有配方组合都无法求解");
            return false;
        }

        /// <summary>
        /// 更新ItemNode的mainRecipe
        /// </summary>
        private void UpdateMainRecipes()
        {
            foreach (var itemKvp in itemNodes)
            {
                int itemId = itemKvp.Key;
                var itemNode = itemKvp.Value;
                
                if (selectedRecipes.ContainsKey(itemId))
                {
                    int recipeId = selectedRecipes[itemId];
                    if (recipeInfos.ContainsKey(recipeId))
                    {
                        itemNode.mainRecipe = recipeInfos[recipeId];
                    }
                }
            }
        }

        /// <summary>
        /// 初始化RecipeInfo字典
        /// </summary>
        private void InitializeRecipeInfos()
        {
            foreach (int recipeId in allRecipes)
            {
                var recipe = CalcDB.recipeDict[recipeId];
                recipeInfos[recipeId] = new RecipeInfo(recipe, userPreference);
            }
        }

        /// <summary>
        /// 构建约束矩阵 A，其中 A * x = b
        /// 矩阵的行表示非原矿物品，列表示配方
        /// 矩阵元素表示配方对物品的影响（正值为产出，负值为消耗）
        /// 原矿物品不参与约束，可以任意消耗
        /// </summary>
        private Matrix<double> BuildConstraintMatrix()
        {
            var matrix = Matrix<double>.Build.Dense(constraintItems.Count, allRecipes.Count);

            for (int i = 0; i < constraintItems.Count; i++)
            {
                int itemId = constraintItems[i];
                
                for (int j = 0; j < allRecipes.Count; j++)
                {
                    int recipeId = allRecipes[j];
                    var recipeInfo = recipeInfos[recipeId];
                    
                    // 计算该配方对该物品的净影响
                    double effect = 0;
                    
                    // 产出：正值
                    if (recipeInfo.productIndices.ContainsKey(itemId))
                    {
                        effect += recipeInfo.GetOutputSpeedByChangedCount(itemId, 1.0);
                    }
                    
                    // 消耗：负值
                    if (recipeInfo.resourceIndices.ContainsKey(itemId))
                    {
                        effect -= recipeInfo.GetInputSpeedByChangedCount(itemId, 1.0);
                    }
                    
                    // 如果启用增产剂求解，添加增产剂消耗
                    if (userPreference.solveProliferators)
                    {
                        var proliferatorUsage = GetUnitProliferatorUsage(recipeInfo);
                        if (proliferatorUsage.ContainsKey(itemId))
                        {
                            effect -= proliferatorUsage[itemId]; // 增产剂消耗为负值
                        }
                    }
                    
                    matrix[i, j] = effect;
                }
            }

            return matrix;
        }

        /// <summary>
        /// 构建目标向量 b
        /// 对于目标产物：设置为目标需求
        /// 对于非目标产物：设置为0（保证供需平衡，允许溢出）
        /// </summary>
        private Vector<double> BuildTargetVector()
        {
            var vector = Vector<double>.Build.Dense(constraintItems.Count);

            for (int i = 0; i < constraintItems.Count; i++)
            {
                int itemId = constraintItems[i];
                double targetSpeed = 0;

                // 查找该物品是否是目标产物
                foreach (var target in targets)
                {
                    if (target.itemId == itemId)
                    {
                        targetSpeed = target.speed;
                        break;
                    }
                }
                
                // 目标产物：设置为目标需求
                // 非目标产物：设置为0（供需平衡，允许溢出）
                vector[i] = targetSpeed;
            }

            return vector;
        }

        /// <summary>
        /// 处理求解结果，更新RecipeInfo的count值
        /// </summary>
        private void ProcessSolution(Vector<double> solution)
        {
            for (int i = 0; i < allRecipes.Count; i++)
            {
                int recipeId = allRecipes[i];
                double count = solution[i];
                
                if (count > EPSILON)
                {
                    recipeInfos[recipeId].count = count;
                }
                else
                {
                    recipeInfos[recipeId].count = 0;
                }
            }
        }

        /// <summary>
        /// 构建ItemNode字典
        /// </summary>
        private void BuildItemNodes()
        {
            // 首先为所有物品创建ItemNode
            foreach (int itemId in allItems)
            {
                itemNodes[itemId] = new ItemNode(itemId, 0, null);
            }

            // 计算每个物品的需求速度和满足速度
            foreach (var recipeKvp in recipeInfos)
            {
                var recipeInfo = recipeKvp.Value;
                if (recipeInfo.count <= EPSILON) continue;

                // 处理产物
                foreach (var productKvp in recipeInfo.productIndices)
                {
                    int productId = productKvp.Key;
                    double outputSpeed = recipeInfo.GetOutputSpeedByChangedCount(productId, recipeInfo.count);
                    
                    if (itemNodes.ContainsKey(productId))
                    {
                        itemNodes[productId].satisfiedSpeed += outputSpeed;
                        
                        // 设置主配方
                        if (selectedRecipes.ContainsKey(productId) && selectedRecipes[productId] == recipeInfo.ID)
                        {
                            itemNodes[productId].mainRecipe = recipeInfo;
                        }
                        else
                        {
                            // 副产物配方
                            if (!itemNodes[productId].byProductRecipes.Contains(recipeInfo))
                                itemNodes[productId].byProductRecipes.Add(recipeInfo);
                        }
                    }
                }

                // 处理原材料
                foreach (var resourceKvp in recipeInfo.resourceIndices)
                {
                    int resourceId = resourceKvp.Key;
                    double inputSpeed = recipeInfo.GetInputSpeedByChangedCount(resourceId, recipeInfo.count);
                    
                    if (itemNodes.ContainsKey(resourceId))
                    {
                        itemNodes[resourceId].needSpeed += inputSpeed;
                    }
                }
                
                // 处理增产剂消耗
                if (userPreference.solveProliferators)
                {
                    int proliferatorId;
                    double proliferatorCount;
                    recipeInfo.GetProliferatorUsed(out proliferatorId, out proliferatorCount);
                    
                    if (proliferatorId > 0 && proliferatorCount > EPSILON)
                    {
                        if (itemNodes.ContainsKey(proliferatorId))
                        {
                            itemNodes[proliferatorId].needSpeed += proliferatorCount;
                        }
                    }
                }
            }

            // 为目标产物添加额外的目标需求
            foreach (var target in targets)
            {
                if (itemNodes.ContainsKey(target.itemId))
                {
                    // 对于目标产物，需要在已有的needSpeed基础上增加目标需求
                    // needSpeed = 其他配方的消耗 + 目标需求
                    itemNodes[target.itemId].needSpeed += target.speed;
                    
                    // Debug.Log($"目标物品{target.itemId}: 配方消耗需求={itemNodes[target.itemId].needSpeed - target.speed:F3}, 目标需求={target.speed:F3}, 总需求={itemNodes[target.itemId].needSpeed:F3}");
                    
                    // 如果实际产出少于总需求，需要从原矿补充（如果是原矿的话）
                    if (itemNodes[target.itemId].satisfiedSpeed < itemNodes[target.itemId].needSpeed && IsOre(target.itemId))
                    {
                        double shortage = itemNodes[target.itemId].needSpeed - itemNodes[target.itemId].satisfiedSpeed;
                        itemNodes[target.itemId].speedFromOre += shortage;
                        itemNodes[target.itemId].satisfiedSpeed = itemNodes[target.itemId].needSpeed;
                    }
                }
            }

            // 处理原矿补充
            foreach (var itemKvp in itemNodes)
            {
                var itemNode = itemKvp.Value;
                if (IsOre(itemNode.itemId))
                {
                    // 如果是原矿且有缺口，从原矿补充
                    if (itemNode.satisfiedSpeed < itemNode.needSpeed)
                    {
                        itemNode.speedFromOre = itemNode.needSpeed - itemNode.satisfiedSpeed;
                        itemNode.satisfiedSpeed = itemNode.needSpeed;
                    }
                }
            }
        }

        /// <summary>
        /// 打印调试信息：约束矩阵和目标向量
        /// </summary>
        private void PrintDebugInfo(Matrix<double> constraintMatrix, Vector<double> targetVector)
        {
            // Debug.Log("=== 线性规划求解调试信息 ===");
            
            // 打印约束物品列表
            // Debug.Log($"约束物品列表 ({constraintItems.Count}个，参与求解):");
            for (int i = 0; i < constraintItems.Count; i++)
            {
                string itemName = CalcDB.itemDict.ContainsKey(constraintItems[i]) ? CalcDB.itemDict[constraintItems[i]].ID.ToString() : "未知物品";
                // Debug.Log($"  [{i}] {constraintItems[i]} - {itemName}");
            }
            
            // 打印原矿物品列表
            // Debug.Log($"原矿物品列表 ({oreItems.Count}个，不参与约束):");
            for (int i = 0; i < oreItems.Count; i++)
            {
                string itemName = CalcDB.itemDict.ContainsKey(oreItems[i]) ? CalcDB.itemDict[oreItems[i]].ID.ToString() : "未知物品";
                // Debug.Log($"  {oreItems[i]} - {itemName}");
            }
            
            // 打印配方列表
            // Debug.Log($"配方列表 ({allRecipes.Count}个):");
            for (int j = 0; j < allRecipes.Count; j++)
            {
                string recipeName = CalcDB.recipeDict.ContainsKey(allRecipes[j]) ? CalcDB.recipeDict[allRecipes[j]].ID.ToString() : "未知配方";
                // Debug.Log($"  [{j}] {allRecipes[j]} - {recipeName}");
            }

            // 打印约束矩阵
            // Debug.Log("约束矩阵 A (行=约束物品, 列=配方):");
            
            // 打印表头
            string header = "物品ID\\配方ID\t";
            for (int j = 0; j < allRecipes.Count; j++)
            {
                header += $"{allRecipes[j]}\t";
            }
            // Debug.Log(header);
            
            // 打印矩阵内容
            for (int i = 0; i < constraintItems.Count; i++)
            {
                string row = $"{constraintItems[i]}\t\t";
                for (int j = 0; j < allRecipes.Count; j++)
                {
                    row += $"{constraintMatrix[i, j]:F3}\t";
                }
                // Debug.Log(row);
            }

            // 打印目标向量
            // Debug.Log("目标向量 b:");
            for (int i = 0; i < constraintItems.Count; i++)
            {
                if (Math.Abs(targetVector[i]) > EPSILON)
                {
                    string itemName = CalcDB.itemDict.ContainsKey(constraintItems[i]) ? CalcDB.itemDict[constraintItems[i]].ID.ToString() : "未知物品";
                    // Debug.Log($"  物品{constraintItems[i]}({itemName}): {targetVector[i]:F3}/min");
                }
            }
        }

        /// <summary>
        /// 打印求解结果信息
        /// </summary>
        private void PrintSolutionInfo(Vector<double> solution)
        {
            // Debug.Log("求解结果 x (配方倍率):");
            for (int j = 0; j < allRecipes.Count; j++)
            {
                if (Math.Abs(solution[j]) > EPSILON)
                {
                    string recipeName = CalcDB.recipeDict.ContainsKey(allRecipes[j]) ? CalcDB.recipeDict[allRecipes[j]].ID.ToString() : "未知配方";
                    // Debug.Log($"  配方{allRecipes[j]}({recipeName}): {solution[j]:F6}倍");
                }
            }
            
            // 验证求解结果
            // Debug.Log("验证求解结果:");
            var constraintMatrix = BuildConstraintMatrix();
            var targetVector = BuildTargetVector();
            var result = constraintMatrix * solution;
            
            for (int i = 0; i < constraintItems.Count; i++)
            {
                double actual = result[i];
                double target = targetVector[i];
                if (Math.Abs(actual) > EPSILON || Math.Abs(target) > EPSILON)
                {
                    string itemName = CalcDB.itemDict.ContainsKey(constraintItems[i]) ? CalcDB.itemDict[constraintItems[i]].ID.ToString() : "未知物品";
                    string status = Math.Abs(actual - target) < 0.01 ? "✓" : "✗";
                    // Debug.Log($"  {status} 物品{constraintItems[i]}({itemName}): 目标={target:F3}, 实际={actual:F3}, 差值={Math.Abs(actual - target):F3}");
                }
            }
        }
    }
} 
