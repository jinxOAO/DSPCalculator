using DSPCalculator.Compatibility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DSPCalculator.Logic.LP
{
    /// <summary>
    /// 线性规划生产计算器。
    /// 负责：
    /// 1. 从 CalcDB + UserPreference 构建 LP 模型（含所有 mod 适配）
    /// 2. 调用 Simplex 求解
    /// 3. 将结果回填到 SolutionTree 的 recipeInfos/itemNodes 结构中，
    ///    使其与现有 UI 和 Bp（蓝图生成）接口完全兼容。
    /// </summary>
    public class LPProductionCalculator
    {
        private SolutionTree solutionTree;
        private UserPreference pref;

        // LP 内部索引
        private List<int> candidateRecipeIds;       // 参与 LP 的配方 ID 列表 (变量)
        private List<int> constrainedItemIds;       // 有约束的物品 ID 列表 (约束行)
        private Dictionary<int, int> recipeVarIdx;  // recipeId -> 变量索引
        private Dictionary<int, int> itemConstraintIdx; // itemId -> 约束行索引

        // 溢出变量: 每个约束物品有一个 surplus 变量，索引 = candidateRecipeIds.Count + itemConstraintIdx[itemId]
        private int totalVars;
        private int totalConstraints;

        // 溢出惩罚权重（可调节）
        private const double OVERFLOW_PENALTY = 100.0;

        // 求解失败原因：NoModel 表示无需建模型（如需求全为原矿），不打扰玩家；
        // Infeasible / Unbounded 属于真正的求解出错，需弹出玩家可见的报错提示。
        public enum LPFailReason { None, NoModel, Infeasible, Unbounded }
        public LPFailReason failReason = LPFailReason.None;

        public LPProductionCalculator(SolutionTree solutionTree)
        {
            this.solutionTree = solutionTree;
            this.pref = solutionTree.userPreference;
        }

        /// <summary>
        /// 主入口：构建模型、求解、回填结果
        /// </summary>
        public bool Solve()
        {
            failReason = LPFailReason.None;
            if (!BuildModel())
            {
                failReason = LPFailReason.NoModel; // 需求全为原矿等无需建模的情况，不算求解出错
                if (lpDiag) LpDiagEvent("BuildModel=false (NoModel：需求全为原矿/无可收集配方/无目标)");
                return false;
            }

            if (lpDiag)
                LpDiagEvent($"BuildModel OK: 配方变量={candidateRecipeIds.Count} 约束物品={constrainedItemIds.Count}");

            LPModel model = ConstructLPModel();
            if (lpDiag) LpDiagDumpModel(model, "model built");
            LPResult result = LPSimplex.Solve(model);

            if (!result.feasible)
            {
                failReason = LPFailReason.Infeasible;
                Debug.LogWarning("LP求解：无可行解，可能配方设定存在矛盾");
                if (lpDiag) LpDiagDumpModel(model, "INFEASIBLE 失败现场");
                LogInfeasibleDiagnostics(result);
                return false;
            }
            if (!result.bounded)
            {
                failReason = LPFailReason.Unbounded;
                Debug.LogWarning("LP求解：目标函数无界");
                if (lpDiag) LpDiagDumpModel(model, "UNBOUNDED 失败现场");
                return false;
            }

            PopulateSolutionTree(result);
            if (lpDiag)
            {
                var sbX = new System.Text.StringBuilder("[LPDIAG] LP 成功, 使用中的配方 xR: ");
                for (int r = 0; r < candidateRecipeIds.Count; r++)
                {
                    if (result.solution != null && r < result.solution.Length && result.solution[r] > 1e-8)
                        sbX.Append($"{LpDiagRecipeName(CalcDB.recipeDict[candidateRecipeIds[r]])}={result.solution[r]:0.####}, ");
                }
                LpDiagEvent(sbX.ToString());

                // 增产剂节点终态（与 DFS 侧同格式，便于对数）
                var sbTerm = new System.Text.StringBuilder("[LPDIAG] LP 增产剂节点终态:");
                foreach (int pid in CalcDB.proliferatorItemIds)
                {
                    if (solutionTree.itemNodes.ContainsKey(pid))
                    {
                        var nd = solutionTree.itemNodes[pid];
                        string mainR = nd.mainRecipe != null && nd.mainRecipe.recipeNorm != null ? LpDiagRecipeName(nd.mainRecipe.recipeNorm) : "无";
                        sbTerm.Append($" [{LpDiagItemName(pid)}] need={nd.needSpeed:0.####} satisfied={nd.satisfiedSpeed:0.####} fromOre={nd.speedFromOre:0.####} main配方={mainR} count={nd.mainRecipe?.count ?? 0:0.####};");
                    }
                    else sbTerm.Append($" [{LpDiagItemName(pid)}] 无节点;");
                }
                LpDiagEvent(sbTerm.ToString());
            }
            return true;
        }

        #region Model Building

        /// <summary>
        /// 确定参与 LP 的配方集合和约束物品集合
        /// </summary>
        private bool BuildModel()
        {
            candidateRecipeIds = new List<int>();
            constrainedItemIds = new List<int>();
            recipeVarIdx = new Dictionary<int, int>();
            itemConstraintIdx = new Dictionary<int, int>();

            // 1. 收集目标物品
            HashSet<int> targetItems = new HashSet<int>();
            foreach (var t in solutionTree.targets)
            {
                if (t.itemId > 0)
                    targetItems.Add(t.itemId);
            }
            if (targetItems.Count == 0)
                return false;

            // 如果 solveProliferators，将增产剂也加入目标
            if (pref.solveProliferators)
            {
                foreach (int pId in CalcDB.proliferatorItemIds)
                    targetItems.Add(pId);
            }

            // 2. BFS 从目标出发，收集所有可达的配方和物品
            HashSet<int> visitedItems = new HashSet<int>();
            HashSet<int> visitedRecipes = new HashSet<int>();
            Queue<int> itemQueue = new Queue<int>();

            foreach (int t in targetItems)
            {
                if (!itemQueue.Contains(t))
                    itemQueue.Enqueue(t);
                visitedItems.Add(t);
            }

            while (itemQueue.Count > 0)
            {
                int itemId = itemQueue.Dequeue();

                // 增产剂并线时，增产剂必须被生产出来，不能因为被判为原矿而跳过其配方收集
                bool isProlifMerged = pref.solveProliferators && CalcDB.proliferatorItemIds.Contains(itemId);

                // 跳过原矿
                if (IsRawOre(itemId) && !isProlifMerged)
                    continue;

                // 跳过已完成标记
                if (pref.finishedRecipes.ContainsKey(itemId))
                    continue;

                if (!CalcDB.itemDict.ContainsKey(itemId))
                    continue;

                List<NormalizedRecipe> recipes = CalcDB.itemDict[itemId].recipes;
                if (recipes == null || recipes.Count == 0)
                    continue;

                // 如果用户指定了配方，只用指定的
                int forcedRecipeId = -1;
                if (pref.itemConfigs.ContainsKey(itemId) && pref.itemConfigs[itemId].recipeID > 0)
                {
                    forcedRecipeId = pref.itemConfigs[itemId].recipeID;
                }

                foreach (var recipe in recipes)
                {
                    if (forcedRecipeId > 0 && recipe.ID != forcedRecipeId)
                        continue;

                    if (visitedRecipes.Add(recipe.ID))
                    {
                        // 将配方的所有净原材料加入队列
                        for (int i = 0; i < recipe.resources.Length; i++)
                        {
                            if (recipe.resourceCounts[i] > 0)
                            {
                                int resId = recipe.resources[i];
                                if (!visitedItems.Contains(resId))
                                {
                                    visitedItems.Add(resId);
                                    itemQueue.Enqueue(resId);
                                }
                            }
                        }
                        // 将配方的所有净产物加入（用于副产物约束）
                        for (int i = 0; i < recipe.products.Length; i++)
                        {
                            if (recipe.productCounts[i] > 0)
                            {
                                int prodId = recipe.products[i];
                                if (!visitedItems.Contains(prodId))
                                {
                                    visitedItems.Add(prodId);
                                    itemQueue.Enqueue(prodId);
                                }
                            }
                        }
                    }
                }
            }

            // 3. 构建变量索引
            foreach (int rId in visitedRecipes)
            {
                recipeVarIdx[rId] = candidateRecipeIds.Count;
                candidateRecipeIds.Add(rId);
            }

            // 4. 构建约束行索引（非原矿、非 finished 的物品）
            foreach (int itemId in visitedItems)
            {
                if (IsRawOre(itemId))
                    continue;
                if (pref.finishedRecipes.ContainsKey(itemId))
                    continue;
                if (!itemConstraintIdx.ContainsKey(itemId))
                {
                    itemConstraintIdx[itemId] = constrainedItemIds.Count;
                    constrainedItemIds.Add(itemId);
                }
            }

            // 4b. 增产剂并线：强制为每种增产剂建立约束行（即便被判定为原矿），
            // 这样所有配方对增产剂的消耗才有对应的约束来承载，LP 会自动生产足量增产剂。
            if (pref.solveProliferators)
            {
                for (int i = 0; i < CalcDB.proliferatorItemIds.Count; i++)
                {
                    int pId = CalcDB.proliferatorItemIds[i];
                    if (!itemConstraintIdx.ContainsKey(pId))
                    {
                        itemConstraintIdx[pId] = constrainedItemIds.Count;
                        constrainedItemIds.Add(pId);
                    }
                }
            }

            totalVars = candidateRecipeIds.Count + constrainedItemIds.Count; // recipe vars + surplus vars
            totalConstraints = constrainedItemIds.Count;

            if (candidateRecipeIds.Count == 0 || constrainedItemIds.Count == 0)
                return false;

            return true;
        }

        /// <summary>
        /// 判断物品是否视为原矿（外部无限供应）——与 SolutionTree.SolvePath 中的逻辑一致
        /// </summary>
        private bool IsRawOre(int itemId)
        {
            if (!CalcDB.itemDict.ContainsKey(itemId))
                return true; // 无任何配方能生产

            bool isOre = CalcDB.itemDict[itemId].defaultAsOre || CalcDB.itemDict[itemId].recipes.Count == 0;

            if (pref.itemConfigs.ContainsKey(itemId))
            {
                isOre = pref.itemConfigs[itemId].consideredAsOre || isOre;
                if (pref.itemConfigs[itemId].forceNotOre && CalcDB.itemDict[itemId].recipes.Count > 0)
                    isOre = false;
            }
            return isOre;
        }

        /// <summary>
        /// 计算配方的有效净产出/消耗速率（items/s per unit count），已包含所有 mod 适配
        /// </summary>
        private void GetEffectiveRates(NormalizedRecipe recipe, out Dictionary<int, double> outputRates, out Dictionary<int, double> inputRates)
        {
            outputRates = new Dictionary<int, double>();
            inputRates = new Dictionary<int, double>();

            double time = recipe.time;
            if (time < 0.001) time = 0.001;

            // 确定该配方在此上下文的 mod 修正因子
            double bonusFactor = GetBonusFactor(recipe);
            double diracBonus = GetDiracBonus(recipe);
            double proliferatorBonus = GetProliferatorBonus(recipe);
            
            // 增产剂自喷涂特例（对齐 DFS CalcProliferator）：该配方若是增产剂生产线且用户未另行设置喷涂，
            // 其产出按"喷涂增产后每轮 gross 个"计（不叠加 bonusFactor/dirac 等其他加成，与 DFS 折减公式一致）
            bool selfSpray = GetSelfSprayModel(recipe, out int selfSprayProlif, out int selfSprayGross);
            
            // 产出速率 = productCounts[j] / time * bonusFactor * (1 + dirac + proliferator)
            for (int j = 0; j < recipe.products.Length; j++)
            {
                if (recipe.productCounts[j] > 0)
                {
                    int itemId = recipe.products[j];
                    double rate;
                    if (selfSpray && itemId == selfSprayProlif)
                        rate = (double)selfSprayGross / time;
                    else
                        rate = (double)recipe.productCounts[j] / time * bonusFactor * (1.0 + diracBonus + proliferatorBonus);
                    if (outputRates.ContainsKey(itemId))
                        outputRates[itemId] += rate;
                    else
                        outputRates[itemId] = rate;
                }
            }

            // 劣质加工适配: 对于 itemId==1501 特殊处理
            if (pref.inferior && recipe.products.Length > 0 && recipe.products[0] == 1501 && !ShouldUseIA(recipe))
            {
                // inferior: 产出 N 消耗 N+1, 即需要多执行一次配方来产出同样数量
                // 实际上相当于产出率 *= N/(N+1)
                if (outputRates.ContainsKey(1501))
                {
                    double N = recipe.productCounts[0];
                    outputRates[1501] *= N / (N + 1);
                }
            }

            // 消耗速率 = resourceCounts[j] / time
            for (int j = 0; j < recipe.resources.Length; j++)
            {
                if (recipe.resourceCounts[j] > 0)
                {
                    int itemId = recipe.resources[j];
                    double rate = (double)recipe.resourceCounts[j] / time;

                    // 使用 oriProto 的原始产出数来计算返还量（与 RecipeInfo 逻辑一致）
                    double rawOutput = recipe.oriProto.ResultCounts != null && recipe.oriProto.ResultCounts.Length > 0
                        ? recipe.oriProto.ResultCounts[0] : recipe.productCounts.Length > 0 ? recipe.productCounts[0] : 1;

                    // bluebuff 适配: resources[0] 返还一份产量（使用 oriProto 原始产量）
                    if (pref.bluebuff && IsBluebuffEligible(recipe) && j == 0 && recipe.resources[0] == itemId)
                    {
                        double prolifMultiplier = 1.0;
                        if (IsIncrementMode(recipe))
                        {
                            int incLvl = GetEffectiveIncLevel(recipe);
                            prolifMultiplier = 1.0 + (incLvl > 0 ? Utils.GetIncMilli(incLvl, pref) : 0);
                        }
                        double returnRate = rawOutput / time * prolifMultiplier;
                        rate = Math.Max(0, rate - returnRate);
                    }

                    // energyBurst 适配
                    if (pref.energyBurst && recipe.products.Length > 0)
                    {
                        int rocketId = recipe.products[0];
                        int returnIndex = -1;
                        if ((rocketId >= 9488 && rocketId <= 9490) || (rocketId == 1503 && CompatManager.GB))
                            returnIndex = 2;
                        else if (rocketId >= 9491 && rocketId <= 9492 || rocketId == 9510 || rocketId == 1503)
                            returnIndex = 1;

                        if (returnIndex > 0 && j == returnIndex && returnIndex < recipe.resources.Length)
                        {
                            double prolifMultiplier = 1.0;
                            if (IsIncrementMode(recipe))
                            {
                                int incLvl = GetEffectiveIncLevel(recipe);
                                prolifMultiplier = 1.0 + (incLvl > 0 ? Utils.GetIncMilli(incLvl, pref) : 0);
                            }
                            double returnRate = 2.0 * rawOutput / time * prolifMultiplier;
                            rate = Math.Max(0, rate - returnRate);
                        }
                    }

                    if (rate > 0.0001)
                    {
                        if (inputRates.ContainsKey(itemId))
                            inputRates[itemId] += rate;
                        else
                            inputRates[itemId] = rate;
                    }
                }
            }
        }

        #region Mod Adaptation Helpers

        private double GetBonusFactor(NormalizedRecipe recipe)
        {
            if (CompatManager.GB && recipe.ID == CalcDB.dfSmelterId && !ShouldUseIA(recipe))
                return 2.0;
            // 实际应该查 assemblerItemId == dfSmelterId... 
            // 但由于 LP 阶段不决定具体用哪个工厂，这里检查 recipe 是否能用 2319
            // 更精确：只有当配方 type 为 Smelt 且 GB 开启时
            if (CompatManager.GB && recipe.type == (int)ERecipeType.Smelt && !ShouldUseIA(recipe))
            {
                int assemblerId = GetPreferredAssemblerItemId(recipe);
                if (assemblerId == CalcDB.dfSmelterId)
                    return 2.0;
            }
            return 1.0;
        }

        private double GetDiracBonus(NormalizedRecipe recipe)
        {
            if (pref.dirac && recipe.type == (int)ERecipeType.Particle
                && recipe.products.Length > 0 && recipe.products[0] == 1122)
                return 0.5;
            return 0.0;
        }

        private double GetProliferatorBonus(NormalizedRecipe recipe)
        {
            // MMS 特化加成
            if (CompatManager.MMS && ShouldUseIA(recipe))
            {
                int specType = pref.globalIAType;
                int specLevel = GetSpecLevelForLP(recipe, specType);
                if (specLevel > 0)
                {
                    if (specType == 3) return 0.25;
                    else if (specType == 4 || specType == 5) return 0.25 * specLevel;
                }
            }

            // 增产剂加成
            int incLevel = GetEffectiveIncLevel(recipe);
            if (incLevel > 0 && recipe.productive)
            {
                return Utils.GetIncMilli(incLevel, pref);
            }
            return 0.0;
        }

        /// <summary>
        /// 复用 RecipeInfo.GetProliferatorUsed 计算某配方每单位执行(count=1)对增产剂的消耗速率，
        /// 保证与 DFS 路径完全一致的 mod 适配（蓝buff/能量迸发/MMS特化/GB 等）。
        /// 返回 (prolifId, perUnitRate)。prolifId<=0 表示该配方不消耗增产剂。
        /// </summary>
        private void GetProliferatorUsage(NormalizedRecipe recipe, out int prolifId, out double perUnitRate)
        {
            prolifId = 0;
            perUnitRate = 0.0;
            if (!pref.solveProliferators)
                return;
            // count=1 时 GetProliferatorUsed 返回的量即为每单位执行的消耗系数（该式为 count 的线性函数）
            RecipeInfo probe = new RecipeInfo(recipe, pref);
            probe.count = 1.0;
            probe.GetProliferatorUsed(out prolifId, out perUnitRate);
            if (prolifId <= 0 || perUnitRate <= 1e-12)
            {
                prolifId = 0;
                perUnitRate = 0.0;
            }
        }

        /// <summary>
        /// 增产剂自喷涂建模（与 DFS CalcProliferator 的折减公式采用相同的公式与触发条件）：
        /// 并入产线时，增产剂生产线总是被视为用它自己喷涂（与用户的配方设置无关）：
        /// 每轮产出 = (int)(HpMax × (1 + 自身 ability 增幅))，其中喷涂自身产品消耗 1 剂增产剂，
        /// 净外卖 = 每轮产出 − 1。仅当满足 DFS 触发条件（每轮产出 − 1 > HpMax）时生效。
        /// 若用户已显式给该增产剂配方设置了喷涂（GetProliferatorUsage 已有消耗项），通用建模
        /// 已包含加成与消耗，本特例不再叠加，避免重复计数。
        /// </summary>
        private bool GetSelfSprayModel(NormalizedRecipe recipe, out int prolifItemId, out int grossPerRun)
        {
            prolifItemId = 0;
            grossPerRun = 0;
            if (!pref.solveProliferators)
                return false;
            for (int j = 0; j < recipe.products.Length; j++)
            {
                int pid = recipe.products[j];
                if (!CalcDB.proliferatorAbilitiesMap.ContainsKey(pid))
                    continue;
                // 用户已设置该增产剂配方喷涂时不叠加
                GetProliferatorUsage(recipe, out int uId, out double uRate);
                if (uId > 0 && uRate > 0.0)
                    return false;
                ItemProto proto = LDB.items.Select(pid);
                if (proto == null)
                    return false;
                int oriCount = proto.HpMax;
                int ability = CalcDB.proliferatorAbilitiesMap[pid];
                int gross = (int)(oriCount * (1.0 + Utils.GetIncMilli(ability, pref)));
                if (gross - 1 > oriCount) // 与 DFS 折减公式一致的不等式条件
                {
                    prolifItemId = pid;
                    grossPerRun = gross;
                    return true;
                }
                return false;
            }
            return false;
        }

        private bool ShouldUseIA(NormalizedRecipe recipe)
        {
            if (!CompatManager.MMS) return false;
            // 简化判定：如果全局使用 IA 且没有强制非 IA 设施
            if (pref.globalUseIA)
            {
                if (pref.recipeConfigs.ContainsKey(recipe.ID))
                {
                    var config = pref.recipeConfigs[recipe.ID];
                    if (config.forceUseIA) return true;
                    if (config.assemblerItemId > 0) return false;
                }
                return true;
            }
            if (pref.recipeConfigs.ContainsKey(recipe.ID))
            {
                var config = pref.recipeConfigs[recipe.ID];
                if (config.forceUseIA && config.IAType >= 0) return true;
            }
            return false;
        }

        private bool IsBluebuffEligible(NormalizedRecipe recipe)
        {
            int type = recipe.type;
            bool typeMatch = (type == (int)ERecipeType.Assemble || type == 9 || type == 10 || type == 12 || ShouldUseIA(recipe));
            if (!typeMatch) return false;
            if (recipe.resources.Length <= 1) return false;
            if (recipe.products.Length == 0) return false;
            int mainProduct = recipe.products[0];
            if (mainProduct == 1803 || mainProduct == 6006) return false;
            return true;
        }

        private bool IsEnergyBurstEligible(NormalizedRecipe recipe, int rocketId)
        {
            if (rocketId >= 9488 && rocketId <= 9492) return true;
            if (rocketId == 9510 || rocketId == 1503) return true;
            if (CompatManager.GB && rocketId == 1503) return true;
            return false;
        }

        /// <summary>
        /// 判断配方是否处于"增产模式"（而非加速模式）
        /// </summary>
        private bool IsIncrementMode(NormalizedRecipe recipe)
        {
            if (!recipe.productive) return false;
            if (pref.recipeConfigs.ContainsKey(recipe.ID) && pref.recipeConfigs[recipe.ID].forceIncMode >= 0)
                return pref.recipeConfigs[recipe.ID].forceIncMode == 1;
            return pref.globalIsInc;
        }

        private int GetSpecLevelForLP(NormalizedRecipe recipe, int specType)
        {
            if (!CompatManager.MMS || specType <= 0) return 0;
            switch (specType)
            {
                case 1: if (recipe.type == (int)ERecipeType.Smelt) return 1; break;
                case 2: if (recipe.type == (int)ERecipeType.Chemical || recipe.type == (int)ERecipeType.Refine || recipe.type == 16) return 1; break;
                case 3:
                    if (ContainsItem(recipe, 1121) || ContainsItem(recipe, 1122)) return 1;
                    break;
                case 4:
                    if (ProductContains(recipe, 1303) || ProductContains(recipe, 1305) || ProductContains(recipe, 9486)) return 2;
                    if (ResourceContains(recipe, 1303) || ResourceContains(recipe, 1305) || ResourceContains(recipe, 9486)) return 1;
                    break;
                case 5: return 4; // 简化，需要 itemProto.isAmmo 检查
            }
            return 0;
        }

        private int GetEffectiveIncLevel(NormalizedRecipe recipe)
        {
            if (pref.recipeConfigs.ContainsKey(recipe.ID) && pref.recipeConfigs[recipe.ID].incLevel >= 0)
                return pref.recipeConfigs[recipe.ID].incLevel;
            return pref.globalIncLevel;
        }

        private int GetPreferredAssemblerItemId(NormalizedRecipe recipe)
        {
            if (pref.recipeConfigs.ContainsKey(recipe.ID) && pref.recipeConfigs[recipe.ID].assemblerItemId > 0)
                return pref.recipeConfigs[recipe.ID].assemblerItemId;
            if (pref.globalAssemblerIdByType.ContainsKey(recipe.type) && pref.globalAssemblerIdByType[recipe.type] > 0)
                return pref.globalAssemblerIdByType[recipe.type];
            if (CalcDB.assemblerListByType.ContainsKey(recipe.type) && CalcDB.assemblerListByType[recipe.type].Count > 0)
                return CalcDB.assemblerListByType[recipe.type][0].ID;
            return -1;
        }

        private bool ContainsItem(NormalizedRecipe r, int itemId)
        {
            foreach (int p in r.products) if (p == itemId) return true;
            foreach (int res in r.resources) if (res == itemId) return true;
            return false;
        }
        private bool ProductContains(NormalizedRecipe r, int itemId)
        {
            foreach (int p in r.products) if (p == itemId) return true;
            return false;
        }
        private bool ResourceContains(NormalizedRecipe r, int itemId)
        {
            foreach (int res in r.resources) if (res == itemId) return true;
            return false;
        }

        #endregion

        /// <summary>
        /// 构建 LPModel 对象
        /// </summary>
        private LPModel ConstructLPModel()
        {
            LPModel model = new LPModel(totalVars, totalConstraints);

            // 目标函数系数
            for (int r = 0; r < candidateRecipeIds.Count; r++)
            {
                // 成本 = 配方每单位执行的"代价"，用 assemblerCount 做权重
                // 简化为 1/time (执行越慢越贵)
                NormalizedRecipe recipe = CalcDB.recipeDict[candidateRecipeIds[r]];
                double cost = 1.0 / recipe.time;
                // 使用 IA 的配方能耗为 0，cost 降低
                if (ShouldUseIA(recipe))
                    cost *= 0.01; // 极低成本（IA 不占工厂位）
                model.SetObjective(r, cost);
            }
            // 溢出变量成本
            for (int i = 0; i < constrainedItemIds.Count; i++)
            {
                model.SetObjective(candidateRecipeIds.Count + i, OVERFLOW_PENALTY);
            }

            // 约束矩阵: 对于每个物品 i: Σ_r (netRate_r_i * x_r) - surplus_i >= demand_i
            for (int row = 0; row < constrainedItemIds.Count; row++)
            {
                int itemId = constrainedItemIds[row];

                // 右端：目标需求
                double demand = GetItemDemand(itemId);
                model.SetRHS(row, demand);

                // 配方变量系数
                for (int r = 0; r < candidateRecipeIds.Count; r++)
                {
                    NormalizedRecipe recipe = CalcDB.recipeDict[candidateRecipeIds[r]];
                    GetEffectiveRates(recipe, out var outputs, out var inputs);

                    double netRate = 0;
                    if (outputs.ContainsKey(itemId)) netRate += outputs[itemId];
                    if (inputs.ContainsKey(itemId)) netRate -= inputs[itemId];

                    if (Math.Abs(netRate) > 1e-10)
                    {
                        model.SetConstraintCoeff(row, r, netRate);
                    }
                }

                // 溢出变量系数 = -1
                model.SetConstraintCoeff(row, candidateRecipeIds.Count + row, -1.0);
            }

            // 增产剂并线：为每个消耗增产剂的配方，在其对应增产剂约束行上追加负向消耗系数。
            // 这样 Σ(增产剂净产出) - Σ(各配方消耗) - surplus >= 0，LP 会自动生产足量增产剂，
            // 并将增产剂生产节点回填到 itemNodes，使 UI 与 Bp（含黑盒 coater 串联）无需改动即可工作。
            if (pref.solveProliferators)
            {
                for (int r = 0; r < candidateRecipeIds.Count; r++)
                {
                    NormalizedRecipe recipe = CalcDB.recipeDict[candidateRecipeIds[r]];
                    GetProliferatorUsage(recipe, out int prolifId, out double perUnitRate);
                    if (prolifId > 0 && perUnitRate > 0.0 && itemConstraintIdx.ContainsKey(prolifId))
                    {
                        int prow = itemConstraintIdx[prolifId];
                        model.AddConstraintCoeff(prow, r, -perUnitRate);
                    }
                    // 增产剂自喷涂：产线每轮额外消耗 1 剂喷涂自身产品（产出侧已在 GetEffectiveRates
                    // 按增产后 gross 计，此处扣 1 后净外卖 = gross − 1，与 DFS 折减公式等价）
                    else if (GetSelfSprayModel(recipe, out int spId, out _) && itemConstraintIdx.ContainsKey(spId))
                    {
                        double tt = recipe.time < 0.001 ? 0.001 : recipe.time;
                        model.AddConstraintCoeff(itemConstraintIdx[spId], r, -1.0 / tt);
                    }
                }
            }

            return model;
        }

        private double GetItemDemand(int itemId)
        {
            foreach (var t in solutionTree.targets)
            {
                if (t.itemId == itemId)
                    return t.speed;
            }
            if (pref.solveProliferators && CalcDB.proliferatorItemIds.Contains(itemId))
                return 0; // 增产剂目标为 0（由 LP 内部满足）
            return 0;
        }

        #endregion

        #region Solution Extraction

        /// <summary>
        /// 将 LP 求解结果回填到 SolutionTree 的标准数据结构中。
        /// 这是 Bp 蓝图生成和 UI 显示的关键接口。
        /// </summary>
        private void PopulateSolutionTree(LPResult result)
        {
            solutionTree.ClearTree();

            // 1. 设置 root 节点
            for (int i = 0; i < solutionTree.targets.Count; i++)
            {
                var t = solutionTree.targets[i];
                if (t.itemId > 0)
                {
                    ItemNode rootNode = GetOrCreateItemNode(t.itemId);
                    rootNode.needSpeed = t.speed;
                    solutionTree.root.Add(rootNode);
                }
            }

            // 2. 第一遍：建立 recipeInfos、累计各节点的产出与需求，并登记每个物品的生产配方候选。
            // mainRecipe 的选取挪到第二遍（所有 needSpeed 累计完成后）进行，
            // 因为"有效专线"判定依赖其他物品的实际需求是否已被消耗链覆盖。
            Dictionary<int, List<KeyValuePair<RecipeInfo, double>>> producersByItem = new Dictionary<int, List<KeyValuePair<RecipeInfo, double>>>();

            for (int r = 0; r < candidateRecipeIds.Count; r++)
            {
                int recipeId = candidateRecipeIds[r];
                double xR = result.solution[r];
                if (xR < 1e-8) continue; // 配方未被使用

                NormalizedRecipe recipe = CalcDB.recipeDict[recipeId];
                RecipeInfo info = new RecipeInfo(recipe, pref);
                info.count = xR;
                solutionTree.recipeInfos[recipeId] = info;

                GetEffectiveRates(recipe, out var outputs, out var inputs);

                foreach (var kv in outputs)
                {
                    int prodItemId = kv.Key;
                    double prodRate = kv.Value * xR;
                    ItemNode node = GetOrCreateItemNode(prodItemId);
                    node.satisfiedSpeed += prodRate;

                    if (!producersByItem.ContainsKey(prodItemId))
                        producersByItem[prodItemId] = new List<KeyValuePair<RecipeInfo, double>>();
                    producersByItem[prodItemId].Add(new KeyValuePair<RecipeInfo, double>(info, prodRate));
                }

                foreach (var kv in inputs)
                {
                    int resItemId = kv.Key;
                    double consRate = kv.Value * xR;
                    ItemNode node = GetOrCreateItemNode(resItemId);
                    node.needSpeed += consRate;
                }
            }

            // 2b. 增产剂并线：把各配方"喷涂消耗"的增产剂累加到增产剂节点的 needSpeed。
            // 配方资源表(items)里并不包含增产剂（增产剂是通过喷涂施加的），所以若不显式累加，
            // 增产剂节点的产出会远大于需求，被 Bp/UI 误判为大量溢出。
            if (pref.solveProliferators)
            {
                for (int r = 0; r < candidateRecipeIds.Count; r++)
                {
                    double xR = result.solution[r];
                    if (xR < 1e-8) continue;
                    NormalizedRecipe recipe = CalcDB.recipeDict[candidateRecipeIds[r]];
                    GetProliferatorUsage(recipe, out int prolifId, out double perUnitRate);
                    if (prolifId > 0 && perUnitRate > 0.0)
                    {
                        ItemNode pNode = GetOrCreateItemNode(prolifId);
                        pNode.needSpeed += perUnitRate * xR;
                    }
                    else if (GetSelfSprayModel(recipe, out int spId, out _))
                    {
                        // 自喷涂消耗：与约束矩阵中 −1/time 同项，保证增产剂节点产需平衡
                        double tt = recipe.time < 0.001 ? 0.001 : recipe.time;
                        ItemNode pNode = GetOrCreateItemNode(spId);
                        pNode.needSpeed += xR / tt;
                    }
                }
            }

            // 3. 第二遍：为每个物品按优先级选取 mainRecipe，其余生产配方记为副产物。
            // 优先级（仅 LP 回填，DFS 路径不经过这里）：
            //   1) 用户强制指定的配方（itemConfigs[X].recipeID 且实际被使用）；
            //   2) "专线"：只产出该物品的配方；以及"有效专线"——虽是多产物配方，但该配方除它以外的
            //      其余产物在本方案中实际需求均为 0（不是目标，也不被任何使用中的配方净消耗；
            //      蓝buff 全额返还导致消耗计 0 的情况已体现在 needSpeed 里），等价于只产出它，
            //      与专线合并为一档；档内按对该物品的产出贡献最大者当选；
            //   3) 其余多产物配方（它们通常已经/将会作为其他物品的主线显示，例如原油处理之于精炼油），
            //      优先级最低，仅当前两档无候选时才当选（按贡献最大，与旧行为一致）。
            // 被判定为原矿的物品保持既有规则：生产它的配方一律只记副产物、永不设 mainRecipe
            // （否则原矿节点会因带上 mainRecipe+children 而跳过步骤6的缺口计算，参见氢气 1120 的历史修复）。
            foreach (var kv in producersByItem)
            {
                int itemId = kv.Key;
                ItemNode node = GetOrCreateItemNode(itemId);
                List<KeyValuePair<RecipeInfo, double>> candidates = kv.Value;

                if (IsRawOre(itemId))
                {
                    foreach (var c in candidates)
                    {
                        if (!node.byProductRecipes.Contains(c.Key))
                            node.byProductRecipes.Add(c.Key);
                    }
                    continue;
                }

                RecipeInfo chosen = ChooseMainRecipeForItem(itemId, candidates);
                node.mainRecipe = chosen;
                foreach (var c in candidates)
                {
                    if (c.Key != chosen && !node.byProductRecipes.Contains(c.Key))
                        node.byProductRecipes.Add(c.Key);
                }
            }

            // 4. 为每个有 mainRecipe 的节点建立 children 关系（Bp 依赖）
            // 必须遍历快照：配方资源表里的输入项可能还没有对应节点（典型场景：蓝buff 把某原料消耗清零后
            // 其生产配方 xR=0 不再被使用，第一遍没建该节点），下方 GetOrCreateItemNode 会向 itemNodes
            // 插入新 key，直接遍历字典会抛 Collection was modified。新建的子节点 mainRecipe==null，
            // 本就不该在本步继续展开，因此快照语义与原意一致。
            var mainRecipeNodes = new List<KeyValuePair<int, ItemNode>>(solutionTree.itemNodes);
            foreach (var kv in mainRecipeNodes)
            {
                ItemNode node = kv.Value;
                if (node.mainRecipe == null) continue;

                RecipeInfo recipe = node.mainRecipe;
                foreach (var resKv in recipe.resourceIndices)
                {
                    int resId = resKv.Key;
                    ItemNode childNode = GetOrCreateItemNode(resId);
                    if (!node.children.Contains(childNode))
                        node.children.Add(childNode);
                    if (!childNode.parents.Contains(node))
                        childNode.parents.Add(node);
                }
            }

            // 5. 对目标节点，如果 satisfiedSpeed < needSpeed 则差额为原矿供给
            foreach (var node in solutionTree.root)
            {
                if (node.satisfiedSpeed < node.needSpeed)
                {
                    node.speedFromOre = node.needSpeed - node.satisfiedSpeed;
                    node.satisfiedSpeed = node.needSpeed;
                }
            }

            // 6. 对所有非目标节点，如果产出 < 需求，差额视为原矿
            foreach (var kv in solutionTree.itemNodes)
            {
                ItemNode node = kv.Value;
                if (node.children.Count == 0 && node.mainRecipe == null)
                {
                    // 这是原矿节点
                    if (node.needSpeed > node.satisfiedSpeed)
                    {
                        node.speedFromOre = node.needSpeed - node.satisfiedSpeed;
                        node.satisfiedSpeed = node.needSpeed;
                    }
                }
            }

            // 7. 增产剂用量将在 SolutionTree.SolveLinear() 中通过 CalcProliferator() 统一计算
        }

        /// <summary>
        /// 无解时输出逐行诊断：哪些约束行的需求无法满足（残差多少），
        /// 以及模型中本可服务该物品、却因强制配方/原矿标记/成环被排除的生产配方。
        /// 让玩家/开发者能看到"凭什么无解"，而不是只有一句笼统报错。
        /// </summary>
        private void LogInfeasibleDiagnostics(LPResult result)
        {
            if (result.infeasibleRows == null || result.infeasibleRows.Count == 0)
                return;
            foreach (int row in result.infeasibleRows)
            {
                if (row < 0 || row >= constrainedItemIds.Count)
                    continue;
                int itemId = constrainedItemIds[row];
                double demand = GetItemDemand(itemId);
                double residual = row < result.infeasibleResiduals.Count ? result.infeasibleResiduals[row] : 0;

                ItemProto itemProto = itemId > 0 ? LDB.items.Select(itemId) : null;
                string itemName = itemProto != null ? itemProto.name : itemId.ToString();

                // 模型内产出该物品的配方，及其实际净系数
                var producerDesc = new List<string>();
                for (int r = 0; r < candidateRecipeIds.Count; r++)
                {
                    NormalizedRecipe recipe = CalcDB.recipeDict[candidateRecipeIds[r]];
                    GetEffectiveRates(recipe, out var outputs, out var inputs);
                    double net = 0;
                    if (outputs.ContainsKey(itemId)) net += outputs[itemId];
                    if (inputs.ContainsKey(itemId)) net -= inputs[itemId];
                    if (net > 1e-10)
                        producerDesc.Add($"{recipe.oriProto?.name ?? recipe.ID.ToString()}(净产率 {net:0.####}/s)");
                }

                // 模型外本可生产它却被排除的配方（用于定位强制配方/原矿标记类矛盾）
                var excludedDesc = new List<string>();
                if (CalcDB.itemDict.ContainsKey(itemId))
                {
                    foreach (var recipe in CalcDB.itemDict[itemId].recipes)
                    {
                        if (!recipeVarIdx.ContainsKey(recipe.ID))
                            excludedDesc.Add(recipe.oriProto?.name ?? recipe.ID.ToString());
                    }
                }

                Utils.logger.LogError($"LP无解诊断: 物品[{itemName}] 目标需求 {demand:0.####}/s, 无法满足的残差 {residual:0.####}/s; " +
                    $"模型内可生产它的配方: [{(producerDesc.Count > 0 ? string.Join(", ", producerDesc) : "无")}; " +
                    $"被排除在模型外的配方: {(excludedDesc.Count > 0 ? string.Join(", ", excludedDesc) : "无")}]");
            }
        }

        private ItemNode GetOrCreateItemNode(int itemId)
        {
            if (!solutionTree.itemNodes.ContainsKey(itemId))
            {
                solutionTree.itemNodes[itemId] = new ItemNode(itemId, 0, solutionTree);
            }
            return solutionTree.itemNodes[itemId];
        }

        /// <summary>
        /// LP 回填时按优先级选取某物品的主配方（仅 LP 使用，DFS 不经过）。
        /// 候选均来自实际被使用的配方（xR>0），且必须在全部 needSpeed 累计完成后调用，
        /// 因为"有效专线"判定依赖各物品的实际需求已被累计完毕（含增产剂并线）。
        /// </summary>
        private RecipeInfo ChooseMainRecipeForItem(int itemId, List<KeyValuePair<RecipeInfo, double>> candidates)
        {
            // 1. 用户强制指定且确实被使用的配方优先
            if (pref.itemConfigs.ContainsKey(itemId) && pref.itemConfigs[itemId].recipeID > 0)
            {
                int fid = pref.itemConfigs[itemId].recipeID;
                foreach (var c in candidates)
                {
                    if (c.Key.ID == fid)
                        return c.Key;
                }
            }

            // 2. 专线（含有效专线）一档优先于其余多产物配方；同档内按产出贡献最大者当选
            RecipeInfo best = null;
            bool bestDedicated = false;
            double bestContribution = -1.0;
            foreach (var c in candidates)
            {
                bool dedicated = IsEffectivelyDedicated(c.Key.recipeNorm, itemId);
                if ((dedicated && !bestDedicated) || (dedicated == bestDedicated && c.Value > bestContribution))
                {
                    best = c.Key;
                    bestDedicated = dedicated;
                    bestContribution = c.Value;
                }
            }
            return best;
        }

        /// <summary>
        /// "有效专线"判定：配方全部（净）产物中除 itemId 外，其余产物在本方案中实际需求均为 0
        /// （不是目标产物，也不被任何使用中的配方净消耗）。蓝buff 类全额返还使消耗按 0 累计，
        /// 已体现在 needSpeed 中；返还超出消耗的部分在 GetEffectiveRates 里被截为 0，不会被视为产出需求。
        /// 只产出 itemId 的单产物配方自然满足本判定。
        /// </summary>
        private bool IsEffectivelyDedicated(NormalizedRecipe recipe, int itemId)
        {
            for (int j = 0; j < recipe.products.Length; j++)
            {
                if (recipe.productCounts[j] <= 0)
                    continue; // 净产出为 0 的产物不计（原料/产物相同物品已在标准化时消除）
                int prodId = recipe.products[j];
                if (prodId == itemId)
                    continue;
                double need = solutionTree.itemNodes.ContainsKey(prodId) ? solutionTree.itemNodes[prodId].needSpeed : 0;
                if (need > 1e-9)
                    return false;
            }
            return true;
        }

        #region [临时诊断] LP 求解全量日志 —— 问题排查完毕后整区删除
        public static bool lpDiag = true; // [临时诊断] 删除时连同所有 lpDiag 判断与本区域一起移除

        private void LpDiagEvent(string msg)
        {
            Utils.logger.LogInfo("[LPDIAG] " + msg);
        }

        /// <summary>
        /// 全模型快照：目标与偏好、强制配方及是否入模、每个配方变量的成本/有效净速率/返还削减明细、
        /// 每条约束行的需求与全部非零系数。失败现场与建模完成后各打一份。
        /// </summary>
        private void LpDiagDumpModel(LPModel model, string stage)
        {
            if (!lpDiag) return;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("[LPDIAG] ========== " + stage + " ==========");

            sb.Append("[LPDIAG] targets:");
            for (int i = 0; i < solutionTree.targets.Count; i++)
            {
                var t = solutionTree.targets[i];
                if (t.itemId > 0)
                    sb.Append($" {LpDiagItemName(t.itemId)}({t.itemId})@{t.speed:0.####}/s;");
            }
            sb.AppendLine();

            int forcedCount = 0;
            foreach (var ic in pref.itemConfigs.Values)
                if (ic.recipeID > 0) forcedCount++;
            sb.AppendLine($"[LPDIAG] prefs: bluebuff={pref.bluebuff} energyBurst={pref.energyBurst} dirac={pref.dirac} inferior={pref.inferior} " +
                $"solveProlif={pref.solveProliferators} globalUseIA={pref.globalUseIA} globalInc={pref.globalIsInc}/lvl{pref.globalIncLevel} " +
                $"forced={forcedCount} finished={pref.finishedRecipes.Count} itemConfigs={pref.itemConfigs.Count}");

            // 原矿标记（consideredAsOre / forceNotOre）
            var oreFlags = new System.Text.StringBuilder();
            foreach (var ic in pref.itemConfigs.Values)
            {
                if (ic.consideredAsOre) oreFlags.Append($" [视为原矿]{LpDiagItemName(ic.ID)}({ic.ID})");
                if (ic.forceNotOre) oreFlags.Append($" [强制非原矿]{LpDiagItemName(ic.ID)}({ic.ID})");
            }
            if (oreFlags.Length > 0) sb.AppendLine("[LPDIAG] oreFlags:" + oreFlags);

            // 强制配方是否在模型里（排查"强制配方被吞"最直接的证据）
            foreach (var ic in pref.itemConfigs.Values)
            {
                if (ic.recipeID > 0)
                {
                    bool inModel = recipeVarIdx.ContainsKey(ic.recipeID);
                    string rname = CalcDB.recipeDict.ContainsKey(ic.recipeID) ? LpDiagRecipeName(CalcDB.recipeDict[ic.recipeID]) : ("配方" + ic.recipeID);
                    string producerItem = "";
                    if (CalcDB.recipeDict.ContainsKey(ic.recipeID))
                    {
                        var nr = CalcDB.recipeDict[ic.recipeID];
                        producerItem = " 产物:[" + string.Join(",", SysLpDiagProducts(nr)) + "]";
                    }
                    sb.AppendLine($"[LPDIAG] 强制: 物品 {LpDiagItemName(ic.ID)}({ic.ID}) -> {rname} [{(inModel ? "已入模" : "未入模!!")}]{producerItem}" +
                        $" 该物品IsRawOre={IsRawOre(ic.ID)}");
                }
            }

            sb.AppendLine($"[LPDIAG] model: vars={model.numVars}(recipe {candidateRecipeIds.Count} + surplus {constrainedItemIds.Count}) rows={model.numConstraints}");

            // ===== 配方变量 =====
            for (int r = 0; r < candidateRecipeIds.Count; r++)
            {
                NormalizedRecipe recipe = CalcDB.recipeDict[candidateRecipeIds[r]];
                GetEffectiveRates(recipe, out var outputs, out var inputs);

                var rates = new System.Text.StringBuilder();
                foreach (var o in outputs) rates.Append($" +{LpDiagItemName(o.Key)}:{o.Value:0.####}");
                foreach (var q in inputs) rates.Append($" -{LpDiagItemName(q.Key)}:{q.Value:0.####}");

                // 返还削减明细：原始消耗 vs 有效消耗（蓝buff/能量迸发的作用直观呈现）
                var reduced = new System.Text.StringBuilder();
                double tt = recipe.time < 0.001 ? 0.001 : recipe.time;
                for (int j = 0; j < recipe.resources.Length; j++)
                {
                    if (recipe.resourceCounts[j] <= 0) continue;
                    double raw = recipe.resourceCounts[j] / tt;
                    double eff = inputs.ContainsKey(recipe.resources[j]) ? inputs[recipe.resources[j]] : 0.0;
                    if (raw - eff > 1e-9)
                        reduced.Append($" [{LpDiagItemName(recipe.resources[j])} 原始{raw:0.####}->有效{eff:0.####}]");
                }

                sb.AppendLine($"[LPDIAG]  x{r} {LpDiagRecipeName(recipe)}(id{recipe.ID}) cost={model.objective[r]:0.####} time={recipe.time:0.###}" +
                    $" 净速率:{rates}{(reduced.Length > 0 ? " 返还:" + reduced : "")}" +
                    $" 工厂={(ShouldUseIA(recipe) ? "IA" : "普通")} incMode={(IsIncrementMode(recipe) ? GetEffectiveIncLevel(recipe) : 0)}" +
                    (GetSelfSprayModel(recipe, out int spId2, out int spGross2) ? $" 自喷涂[{LpDiagItemName(spId2)} 每轮{spGross2} 自耗1]" : ""));
            }

            // ===== 约束行 =====
            for (int i = 0; i < constrainedItemIds.Count; i++)
            {
                int itemId = constrainedItemIds[i];
                double demand = GetItemDemand(itemId);
                var coefs = new System.Text.StringBuilder();
                for (int r = 0; r < candidateRecipeIds.Count; r++)
                {
                    double v = model.A[i, r];
                    if (System.Math.Abs(v) > 1e-12) coefs.Append($" x{r}:{v:0.####}");
                }
                sb.AppendLine($"[LPDIAG] row{i} {LpDiagItemName(itemId)}({itemId}) demand={demand:0.####} 系数[{coefs.ToString().Trim()}] surplusX={candidateRecipeIds.Count + i}");
            }

            Utils.logger.LogInfo(sb.ToString());
        }

        private System.Collections.Generic.IEnumerable<string> SysLpDiagProducts(NormalizedRecipe recipe)
        {
            for (int j = 0; j < recipe.products.Length; j++)
            {
                if (recipe.productCounts[j] > 0)
                    yield return LpDiagItemName(recipe.products[j]) + "*" + recipe.productCounts[j];
            }
        }

        private static string LpDiagItemName(int itemId)
        {
            var p = LDB.items.Select(itemId);
            return p != null ? p.name : ("物品" + itemId);
        }

        private static string LpDiagRecipeName(NormalizedRecipe r)
        {
            string n = r.oriProto != null ? r.oriProto.name : null;
            return string.IsNullOrEmpty(n) ? ("配方" + r.ID) : n;
        }
        #endregion

        #endregion
    }
}
