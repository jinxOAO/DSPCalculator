using DSPCalculator.BP;
using DSPCalculator.UI;
using JetBrains.Annotations;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Factorization;
using MathNet.Numerics.LinearAlgebra.Solvers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DSPCalculator.Logic
{
    /// <summary>
    /// 主体逻辑
    /// </summary>
    public class SolutionTree
    {
        //public int targetItem { get; private set; } // 需要计算的最终产物
        //public double targetSpeed; // 最终产物需要的速度（/s)
        //public double displaySpeedRatio; // 允许玩家随时修改targetSpeed而不重新计算整个树，则通过最后所有显示都追加一个系数来实现

        public UserPreference userPreference; // 用户规定的，比如：某些物品需要用特定配方生产、某些物品的配方需要几级增产剂等等

        public List<ItemTarget> targets;
        public List<ItemNode> root;
        public Dictionary<int, ItemNode> itemNodes; // 用于储存节点状态与信息，并不代表最终的树里有这个节点。一旦这里存储了一个solved的节点，这个物品就永远安全了，用它作为原材料不用担心它成环
        // itemNodes还用于在最终量化计算阶段存储对应产物的所有产出和需求量

        public Dictionary<int, RecipeInfo> recipeInfos; // 用于储存配方信息，相同id的配方在这里共享同一个recipeInfo，在量化计算阶段存储着该配方的所有需求倍率总和

        public List<ItemNode> nodeStack; // 用于解决过程中存储暂存节点

        public Dictionary<int, double> proliferatorCount; // 用于存储所有增产剂的使用量（但目前不会链式求解增产剂）
        public Dictionary<int, double> proliferatorCountSelfSprayed; // 如果是自喷涂过的增产剂需求量

        public bool tempNotShrinkRoot; // 在主产物首次发现溢出时(tempNotShrinkRoot为false)，直接暴力修改targetSpeed然后重新计算，修改后置tempNotShrinkRoot为true。

        public bool unsolved { get { return nodeStack.Count > 0; } }

        public List<BpBeltInfo> beltsAvailable;
        public List<BpSorterInfo> sortersAvailable;

        public SolutionTree() 
        {
            targets = new List<ItemTarget>();
            root = new List<ItemNode>();
            itemNodes = new Dictionary<int, ItemNode>();
            recipeInfos = new Dictionary<int, RecipeInfo>();
            nodeStack = new List<ItemNode>();
            proliferatorCount = new Dictionary<int, double>();
            proliferatorCountSelfSprayed = new Dictionary<int, double>();
            userPreference = new UserPreference();
            tempNotShrinkRoot = false;
        }

        public void ClearTree()
        {
            root.Clear();
            itemNodes.Clear();
            recipeInfos.Clear();
            nodeStack.Clear();
            proliferatorCount.Clear();
            proliferatorCountSelfSprayed.Clear();
        }

        public void ClearUserPreference()
        {
            userPreference = new UserPreference();
        }

        public bool SetTargetItem0AndBeginSolve(int targetItem)
        {
            ClearTree();
            if (targets.Count <= 0)
                targets.Add(new ItemTarget(targetItem, 360));
            else
                targets[0].itemId = targetItem;
            userPreference.ClearWhenChangeTarget();
            tempNotShrinkRoot = false;
            return Solve();
        }

        public bool ChangeTargetSpeed0AndSolve(double targetSpeed)
        {
            if (targets.Count <= 0)
                targets.Add(new ItemTarget(0, 360));
            else
                targets[0].speed = targetSpeed;
            ClearTree();
            userPreference.ClearWhenChangeTarget();
            tempNotShrinkRoot = false;
            return Solve();
        }

        public bool SetTargetAndSolve(int index, int targetItem, double targetSpeed)
        {
            if(AddOrUpdateTarget(index,targetItem,targetSpeed))
            {
                ClearTree();
                userPreference.ClearWhenChangeTarget();
                tempNotShrinkRoot = false;
                return Solve();
            }
            else
            {
                return false;
            }
        }

        public bool AddOrUpdateTarget(int index, int targetItem, double targetSpeed)
        {
            if (targets.Count > index)
            {
                targets[index].itemId = targetItem;
                targets[index].speed = targetSpeed;
            }
            else if (index == targets.Count)
            {
                targets.Add(new ItemTarget(targetItem, targetSpeed));
            }
            else
            {
                Utils.logger.LogWarning("试图新增目标产物，但是新增的index不在targets列表末尾。");
                return false;
            }
            return true;
        }
        
        public void MergeDuplicateTargets()
        {
            if(targets.Count > 1)
            {
                Dictionary<int, int> targetsIndices = new Dictionary<int, int>();
                List<ItemTarget> newTargets = new List<ItemTarget>();
                for (int i = 0; i < targets.Count; i++)
                {
                    int itemId = targets[i].itemId;
                    double speed = targets[i].speed;
                    if (!targetsIndices.ContainsKey(itemId))
                    {
                        targetsIndices[itemId] = newTargets.Count;
                        newTargets.Add(new ItemTarget(itemId, speed));
                    }
                    else
                    {
                        int index = targetsIndices[itemId];
                        newTargets[index].speed += speed;
                    }
                }
                targets = newTargets;
            }
        }


        public bool ReSolve(double forceSpeed)
        {
            ClearTree();
            if(forceSpeed > 0 && targets.Count > 0)
            {
                targets[0].speed = forceSpeed;
            }
            tempNotShrinkRoot = false;
            return Solve();
        }

        public bool ReSolveForRootShrinking(double newTargetSpeed)
        {
            ClearTree();
            targets[0].speed = newTargetSpeed;
            //this.targetSpeed = newTargetSpeed;
            tempNotShrinkRoot = true;
            return Solve();
        }

        public bool Solve()
        {
            MergeDuplicateTargets();
            if (targets.Count > 0)
            {
                RefreshBlueprintDicts(); // 根据userPreference生成后续节点蓝图生成所需的相关字典信息

                // 如果需要计算增产剂生产线，还需要解决增产剂的路线
                if(userPreference.solveProliferators)
                {
                    for (int i = 0; i < CalcDB.proliferatorItemIds.Count; i++)
                    {
                        ItemNode p = new ItemNode(CalcDB.proliferatorItemIds[i], 0, this);
                        Push(p);
                    }
                }
                // 将root放在栈顶（其实放在栈底也没影响，但是这会使得：当root就是增产剂时，且增产剂并入产线计算，那么在Calc的时候，root本身只能作为signNode，且其与itemNodes里面存储的同Id的ItemNode不是同一个对象）
                //root = new ItemNode(targetItem, targetSpeed, this);
                //ItemNode node = root;
                //Push(node);

                root.Clear();
                for (int i = 0; i < targets.Count; i++)
                {
                    if (targets[i].itemId > 0)
                    {
                        ItemNode node = new ItemNode(targets[i].itemId, targets[i].speed, this);
                        root.Add(node);
                        Push(node);
                    }
                }

                if (root.Count <= 0)
                    return false;

                if (SolvePath())
                {
                    //TestLog();
                    CalcTree();
                    double recalcRatio = RemoveOverflow();
                    if(recalcRatio>0)
                    {
                        return ReSolveForRootShrinking(targets[0].speed * recalcRatio);
                    }
                    CalcProliferator();

                    if(userPreference.solveProliferators) // 如果需要单独计算增产剂
                    {
                        CalcProliferatorInProductLine();
                    }

                    return true;
                }
                // TestLog2();
            }
            return false;
        }

        /// <summary>
        /// 解决物品合成路径（找到一个无环有向图）
        /// </summary>
        public bool SolvePath()
        {
            Dictionary<int,int> rootItems = new Dictionary<int, int>();
            for (int i = 0; i < targets.Count; i++)
            {
                rootItems[targets[i].itemId] = 1;
            }
            while (unsolved)
            {
                // 出栈！对这个节点进行处理。注意：出栈时这个节点虽然已经连接入
                ItemNode cur = Pop();
                int itemId = cur.itemId;
                // 开始处理这个item 选择合理的配方 或者 此物品已经解决过路线，不再寻找一次了

                // 1. 如果这个节点已经被处理过了，那么要么他已经解决，unsolvedCount是0，如果不是0，则代表着出现了环路，需要更换配方
                if(itemNodes.ContainsKey(itemId))
                {
                    int unsolvedCount = itemNodes[itemId].unsolvedCount;

                    if(unsolvedCount <= 0)
                    {
                        if (cur.parents.Count > 0)
                        {
                            cur.parents[0].SolveOne();
                        }
                        else if (rootItems.ContainsKey(cur.itemId)) // 多需求配方
                        {
                            itemNodes[cur.itemId].needSpeed += cur.needSpeed;
                        }
                        else if (CalcDB.proliferatorItemIds.Contains(cur.itemId) && cur.needSpeed == 0 || itemNodes[cur.itemId].needSpeed == 0) // 这是可预见的，在将增产剂生产并入当前产线时可能会产生的情况
                        {
                            itemNodes[cur.itemId].needSpeed += cur.needSpeed;
                        }
                        else
                        {
                            Debug.LogWarning($"cur的parent为0，这不正常，出现在itemId={itemId}");
                        }

                        // 暂时保持所有子节点只有一个父，最后再进行合并！！这种情况下，只有
                        //// 将记录中的node的parents加上cur的parent（因为cur和记录中的node是同一个物品的节点，需要合并成同一个）
                        //if (!itemNodes[itemId].parents.Contains(cur.parents[0]))
                        //    itemNodes[itemId].parents.Add(cur.parents[0]);
                        
                        // DEBUG
                        if (DSPCalculatorPlugin.developerMode)
                        {
                            int parentItemId = cur.parents[0].itemId;
                            for (int i = 0; i < itemNodes[itemId].parents.Count; i++)
                            {
                                int parentItemIdInTree = itemNodes[itemId].parents[i].itemId;
                                if(parentItemIdInTree == parentItemId && itemNodes[itemId].parents[i] != cur.parents[0])
                                {
                                    Debug.LogWarning($"在解决itemId={itemId}时，引用了同一个父级itemId={parentItemId}，但是不同的节点实例。");
                                }
                            }
                        }
                    }
                    else // 处理环路！！！！
                    {
                        bool canContinue = false; // 只有在成功更换了配方之后，才会将这个置为true
                        while (cur.parents.Count > 0) // 当cur还有父级节点时，也许还有机会修正并且改到无环的配方
                        {
                            // 如果需求的itemId是一个存在于itemNodes里面且未解决的节点，说明出现了环路。这时候要改变父级的配方（全试过之后还不能的话，就改变再上一级，递归进行）

                            // 首先，从itemNodes中移除所有[“当前父级节点”的子节点]。已经solved的节点可以不用移除(方便后续得知他是solved过的item)，但是要从该节点的parents里面删除当前父节点(防止后续访问到这个本该消失的父节点)
                            ItemNode parent = cur.parents[0];
                            for (int i = parent.children.Count - 1; i >= 0; i--) // 逆序来
                            {
                                ItemNode child = parent.children[i];
                                if (itemNodes.ContainsKey(child.itemId) && itemNodes[child.itemId] == child)
                                {
                                    if (child.unsolvedCount > 0)
                                        itemNodes.Remove(child.itemId);
                                    else
                                        child.parents.Remove(parent);
                                }
                                // 然后记得清除栈中的这些节点！从栈尾清除！
                                for (int j = nodeStack.Count - 1; j >= 0; j--)
                                {
                                    if (nodeStack[j] == child)
                                    {
                                        nodeStack.RemoveAt(j);
                                        break;
                                    }
                                }
                            }
                            // 清理子节点的连接
                            parent.children.Clear();

                            // parent不再挂载任何子节点后，尝试更换recipeId到下一个（前提是用户没有锁定该Id，如果锁定了该Id，则直接进入到此Node不可解，去继续向上解决父级Node的阶段）
                            if (userPreference.itemConfigs.ContainsKey(parent.itemId) && userPreference.itemConfigs[parent.itemId].recipeID > 0)
                            {
                                cur = parent;
                                continue; // 这里不可解，去尝试父节点解决
                            }
                            else
                            {
                                int recipeIndexOfParentItem = CalcDB.itemDict[parent.itemId].recipes.IndexOf(parent.mainRecipe.recipeNorm);
                                if(recipeIndexOfParentItem < CalcDB.itemDict[parent.itemId].recipes.Count - 1) // 如果不是最后一个配方，说明还可以更改
                                {
                                    // --------------------------------------------------------------------------------------------------------------------------------------------------------
                                    cur = parent; // 开始处理parent节点（当做cur进行，重新进行选取recipe后的配置）
                                    NormalizedRecipe recipe = CalcDB.itemDict[parent.itemId].recipes[recipeIndexOfParentItem + 1]; // 设定为下一个配方
                                    cur.TryRecipe(recipe); // 这里已经把cur的unsolvedCount设置好了

                                    // 然后将所有该配方的净原材料入栈，准备后续处理
                                    for (int i = 0; i < recipe.resources.Length; i++)
                                    {
                                        if (recipe.resourceCounts[i] > 0) // 等于0的原材料不属于净原材料
                                        {
                                            ItemNode nodeResource = new ItemNode(recipe.resources[i], 0, this); // 先不在意速度需求，这个在建立完成树之后才进行初始化和计算
                                            cur.AddChild(nodeResource); // 这里也同时把子节点的parent设置成了cur，是双向链接
                                            Push(nodeResource); // 压入栈
                                        }
                                    }
                                    canContinue = true;
                                    break;
                                    // --------------------------------------------------------------------------------------------------------------------------------------------------------
                                }
                                else // 尝试过最后一个配方了，这里不可解
                                {
                                    cur = parent;
                                    continue; // 这里不可解，去尝试父节点解决
                                }
                            }
                        }
                        if (canContinue) // 说明处理完环路的结尾必定是将一个新的配方的所有原材料节点压入栈，所以如果不是因为到顶终止（无法解决成环问题），那么必然要立刻进入下一个循环
                        { 
                            continue; 
                        }
                        else // 说明一路到了顶，到了最初的需求产物，都没能找到不成环的路线，这时要提示玩家，存在环路（并不一定不可解，但是环路接起来好麻烦？），是否清除用户配置
                        {
                            UIRealtimeTip.Popup("存在环路警告".Translate());
                            return false;
                        }
                    }

                    // DEBUG
                    if (DSPCalculatorPlugin.developerMode)
                    {
                        if (unsolvedCount < 0)
                            Debug.LogWarning($"unsolvedCount 出现了负数，这是一个不正常现象，出现在itemId={itemId}");
                        if (cur.parents.Count != 1)
                            Debug.LogWarning($"cur的parent不为1，这不正常，出现在itemId={itemId}");
                    }
                }
                // 2. 如果这个节点尚未被处理过，则选取配方（或视为原矿）
                else
                {
                    // 将此物品node加入到处理过的节点记录中
                    itemNodes[itemId] = cur;

                    bool isOre = CalcDB.itemDict[itemId].defaultAsOre || CalcDB.itemDict[itemId].recipes.Count == 0;
                    if (userPreference.itemConfigs.ContainsKey(itemId)) // 查询用户是否指定了该物品的处理规则，是否视为原矿
                    {
                        isOre = userPreference.itemConfigs[itemId].consideredAsOre || isOre;
                        if (userPreference.itemConfigs[itemId].forceNotOre && CalcDB.itemDict[itemId].recipes.Count > 0)
                            isOre = false;
                    }
                    if (isOre) // 如果是原矿
                    {
                        cur.unsolvedCount = 0;
                        if(cur.parents.Count > 0)
                            cur.parents[0].SolveOne();
                    }
                    else
                    {
                        int recipeId = CalcDB.itemDict[itemId].recipes[0].ID; // 默认取这个物品0号位的配方
                        if (userPreference.itemConfigs.ContainsKey(itemId) && userPreference.itemConfigs[itemId].recipeID > 0) // 配方查询用哪个配方：用户是否指定了该物品的处理规则，如果有则读取用户设置
                        {
                            recipeId = userPreference.itemConfigs[itemId].recipeID;
                        }
                        NormalizedRecipe recipe = CalcDB.recipeDict[recipeId]; // 根据上述结果，取配方
                        cur.TryRecipe(recipe); // 这里已经把cur的unsolvedCount设置好了

                        // 然后将所有该配方的净原材料入栈，准备后续处理
                        for (int i = 0; i < recipe.resources.Length; i++)
                        {
                            if (recipe.resourceCounts[i] > 0) // 等于0的原材料不属于净原材料
                            {
                                ItemNode nodeResource = new ItemNode(recipe.resources[i], 0, this); // 先不在意速度需求，这个在建立完成树之后才进行初始化和计算
                                cur.AddChild(nodeResource); // 这里也同时把子节点的parent设置成了cur，是双向链接
                                Push(nodeResource); // 压入栈
                            }
                        }
                    }
                }
            }
            return true;
            // Debug.Log("Solved");
        }


        /// <summary>
        /// 在准备好的有向图的基础上，计算各个节点需要的生产速度
        /// </summary>
        public void CalcTree(int proliferatorId = -1, double addedSpeed = 0)
        {
            Stack<ItemNode> stack = new Stack<ItemNode>();
            if (proliferatorId <= 0) // 如果是默认参数，代表是为了计算root而非增产剂
            {
                if(root.Count != targets.Count)
                {
                    Utils.logger.LogError("Error when ClacTree, root count does not match the targes count.");
                    return;
                }
                for (int i = 0; i < root.Count; i++)
                {
                    root[i].needSpeed = targets[i].speed;
                    stack.Push(root[i]);
                }
            }
            else
            {
                if(itemNodes.ContainsKey(proliferatorId))
                {
                    itemNodes[proliferatorId].needSpeed += addedSpeed;
                    stack.Push(itemNodes[proliferatorId]);
                }
                else
                {
                    ItemNode pNode = new ItemNode(proliferatorId, addedSpeed, this);
                    stack.Push(pNode);
                }
            }


            while(stack.Any())
            {
                ItemNode oriNode = stack.Pop();
                int itemId = oriNode.itemId;

                ItemNode sharedItemNode = itemNodes[itemId]; // 实际是要在这个上面操作的

                if(sharedItemNode.children.Count == 0) // 说明视为原矿
                {
                    if (sharedItemNode.satisfiedSpeed < sharedItemNode.needSpeed)
                    {
                        sharedItemNode.speedFromOre += sharedItemNode.needSpeed - sharedItemNode.satisfiedSpeed; // 将原矿补充数量记录
                        sharedItemNode.satisfiedSpeed = sharedItemNode.needSpeed;
                    }
                }
                else
                {
                    if(sharedItemNode.mainRecipe != null)
                    {
                        RecipeInfo recipeInfo = sharedItemNode.mainRecipe;
                        int recipeId = recipeInfo.ID;
                        double unsatisfiedSpeed = sharedItemNode.needSpeed - sharedItemNode.satisfiedSpeed;
                        if (unsatisfiedSpeed > 0.0001f) // 仍未满足
                        {
                            if (!recipeInfos.ContainsKey(recipeId))
                            {
                                recipeInfos[recipeId] = recipeInfo;
                            }
                            double addedCount = recipeInfos[recipeId].AddCountByOutputNeed(itemId, unsatisfiedSpeed); // 所有操作都要在recipeInfos的字典中进行

                            // 如果该配方有多种产物，记得在配方增加后，将所有的产物新增速度都加入itemNodes对应的item中。并且记录此配方会生成该item（记录在itemNode的byProductRecipes里，如果与mainRecipe不同的话）
                            foreach (var item in recipeInfo.productIndices)
                            {
                                int productId = item.Key;
                                double addedSatisfiedSpeed = recipeInfo.GetOutputSpeedByChangedCount(productId, addedCount);
                                if (!itemNodes.ContainsKey(productId))
                                {
                                    itemNodes[productId] = new ItemNode(productId, 0, this);
                                }
                                itemNodes[productId].satisfiedSpeed += addedSatisfiedSpeed;
                                // 还要记录一下，这个配方会生成该目标产物，到时候计算溢出的时候可能会用到
                                if (itemNodes[productId].mainRecipe == null || itemNodes[productId].mainRecipe.ID != recipeId)
                                {
                                    bool alreadyMarked = false;
                                    for (int i = 0; i< itemNodes[productId].byProductRecipes.Count; i++)
                                    {
                                        if (itemNodes[productId].byProductRecipes[i].ID == recipeId)
                                        {
                                            alreadyMarked = true;
                                            break;
                                        }
                                    }
                                    if (!alreadyMarked)
                                        itemNodes[productId].byProductRecipes.Add(recipeInfo);
                                }
                            }
                            // 对于该配方的每个需求物品，要新增额外的需求速度。这些物品应该和sharedItemNode的children是对应的
                            //for (int i = 0; i < sharedItemNode.children.Count; i++)
                            //{
                            //    int resourceId = sharedItemNode.children[i].itemId;
                            //    if (DSPCalculatorPlugin.developerMode)
                            //    {
                            //        if (!recipeInfo.resourceIndices.ContainsKey(resourceId))
                            //        {
                            //            Debug.LogWarning($"计算速度时，itemNode的children和配方的resources出现了不对应，配方为{sharedItemNode.mainRecipe.ID}，主产物为{sharedItemNode.itemId}，丢失的resource为{resourceId}");
                            //        }
                            //    }
                            //    itemNodes[sharedItemNode.children[i].itemId].needSpeed += recipeInfo.GetInputSpeedByChangedCount(resourceId, addedCount); // 新增需求速度
                            //    stack.Push(sharedItemNode.children[i]); // 入栈
                            //}
                            foreach (var item in recipeInfo.resourceIndices)
                            {
                                int resourceId = item.Key;
                                itemNodes[resourceId].needSpeed += recipeInfo.GetInputSpeedByChangedCount(resourceId, addedCount);
                                stack.Push(itemNodes[resourceId]); // 入栈
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning("有子节点，但是却没有产出此物的配方，这种情况不应出现！");
                    }
                }

            }
            //foreach (var item in itemNodes)
            //{
            //    Debug.Log($"item {LDB.items.Select(item.Value.itemId).name} have {item.Value.satisfiedSpeed} and need {item.Value.needSpeed}");
            //}
        }

        /// <summary>
        /// 用于去除不必要的溢出量
        /// </summary>
        /// <param name="proliferatorId"></param>
        /// <returns>正常返回0，如果返回了一个正数，则证明root产物有溢出，可以暴力将目标速度乘系数并重新计算，返回的就是这个系数</returns>
        public double RemoveOverflow(int proliferatorId = -1)
        {
            Dictionary<int, ItemNode> visitingNodes = new Dictionary<int, ItemNode>(); // 用于排查环
            Dictionary<int, int> neverShrinkThisBecauseLoop = new Dictionary<int, int>(); // 一旦检测到环，将交叉点录入这个字典，并且不再检查
            if(tempNotShrinkRoot) // 说明经过了一次暴力乘系数
            {
                root[0].needSpeed = root[0].satisfiedSpeed; // 那么将needSpeed修正
            }
            //if (proliferatorId <= 0)
            //{
            //    ItemNode signRootNode = new ItemNode(root.itemId, root.needSpeed, this); // 专门用于路径成环过程的标志。
            //    stack.Push(signRootNode);
            //}
            //else
            //{
            //    ItemNode pNode = new ItemNode(proliferatorId,0, this);
            //    stack.Push(pNode);
            //}
            foreach (var nodeData in itemNodes)
            {
                Stack<ItemNode> stack = new Stack<ItemNode>();
                if (!visitingNodes.ContainsKey(nodeData.Key))
                {
                    ItemNode signNode = new ItemNode(nodeData.Key, 0, this);
                    stack.Push(signNode);

                    while (stack.Any())
                    {
                        ItemNode oriSignNode = stack.Pop();
                        int itemId = oriSignNode.itemId;
                        ItemNode sharedNode = itemNodes[itemId];

                        // 先判断是否成环
                        // 如果只处理mainRecipe应该是不会成环的！！！！

                        // 判断是不是原矿
                        bool isOre = CalcDB.itemDict[itemId].defaultAsOre || CalcDB.itemDict[itemId].recipes.Count == 0;
                        if (userPreference.itemConfigs.ContainsKey(itemId))
                        {
                            isOre = userPreference.itemConfigs[itemId].consideredAsOre || isOre;
                            if (userPreference.itemConfigs[itemId].forceNotOre && CalcDB.itemDict[itemId].recipes.Count > 0)
                                isOre = false;
                        }
                        if (isOre)
                        {
                            oriSignNode.unsolvedCount = 0;

                            if (oriSignNode.parents.Count > 0)
                                oriSignNode.parents[0].SolveOne();

                            if (sharedNode.satisfiedSpeed < sharedNode.needSpeed)
                            {
                                sharedNode.satisfiedSpeed = sharedNode.needSpeed;
                            }
                            else if (sharedNode.satisfiedSpeed > sharedNode.needSpeed) // 如果原矿有溢出
                            {
                                double overflowSpeed = sharedNode.satisfiedSpeed - sharedNode.needSpeed;
                                if (sharedNode.speedFromOre > overflowSpeed) // 如果从原矿来的部分比溢出量还多，那说明可以将所有溢出部分当做原矿来的部分，直接移除
                                {
                                    sharedNode.speedFromOre -= overflowSpeed;
                                    sharedNode.satisfiedSpeed -= overflowSpeed;
                                }
                                else // 否则，只能移除原矿来源的部分，其他的部分都是来自于配方产出
                                {
                                    sharedNode.satisfiedSpeed -= sharedNode.speedFromOre;
                                    sharedNode.speedFromOre = 0;
                                }
                            }
                            continue;
                        }
                        else
                        {
                            double overflowSpeed = sharedNode.satisfiedSpeed - sharedNode.needSpeed;
                            if (overflowSpeed > 0.0001f) // 如果有溢出
                            {
                                if (DSPCalculatorPlugin.developerMode)
                                {
                                    Debug.Log($"检测到溢出 id {LDB.items.Select(itemId).name}, 溢出量{overflowSpeed}/min");
                                }

                                // 只判断生成此物品的主要配方可不可以被削减
                                RecipeInfo sharedRecipeInfo = null;
                                if (visitingNodes.ContainsKey(itemId))
                                {
                                    if (visitingNodes[itemId].unsolvedCount > 0) // 说明成环
                                    {
                                        while (stack.Any() && visitingNodes.ContainsKey(stack.Last().itemId) && visitingNodes[stack.Last().itemId].unsolvedCount > 0) // 将所有栈中与环有关的节点清除
                                        {
                                            stack.Pop();
                                        }
                                        // 这些成环节点在visitingNodes中保留，不再能shrink
                                        continue; // 进入下一个配方的尝试shrink
                                    }
                                }
                                visitingNodes[itemId] = oriSignNode; // 表示处理过这个节点

                                // 首先判断mainRecipe (r==-1的时候)
                                if (sharedNode.mainRecipe != null)
                                    sharedRecipeInfo = recipeInfos[sharedNode.mainRecipe.ID];
                                else
                                    continue;

                                if (sharedRecipeInfo == null)
                                    continue;

                                // 接下来开始对选定配方进行处理
                                double minShrinkCount = sharedRecipeInfo.count;
                                foreach (var item in sharedRecipeInfo.productIndices) // 对于每个产物，看看他是不是溢出，最终取最小的可缩减数目
                                {
                                    int productId = item.Key;
                                    if (itemNodes.ContainsKey(productId)) // 不再itemNodes里面的产出物应该是不曾需求过，因此不会影响minShrinkCount
                                    {
                                        double relatedOverflowSpd = itemNodes[productId].satisfiedSpeed - itemNodes[productId].needSpeed;
                                        if (itemNodes[productId].IsOre(userPreference)) // 如果产物是原材料，无论如何不影响mainrecipe的削减，因为缺的部分都能补上
                                        {

                                        }
                                        else if (relatedOverflowSpd > 0.0001f)
                                        {
                                            minShrinkCount = Math.Min(minShrinkCount, sharedRecipeInfo.CalcCountByOutputSpeed(productId, relatedOverflowSpd));
                                        }
                                        else
                                        {
                                            minShrinkCount = 0;
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        // Debug.LogWarning($"在计算溢出配方{sharedRecipeInfo.ID}时不包含{productId}");
                                    }
                                }
                                if(minShrinkCount >= sharedRecipeInfo.count - 0.0001f) // 当前溢出的产物是主产物，且为了缩减溢出，会把主配方削减干净，那么禁止削减（因为这样相当于用的不是主配方了）
                                {
                                    minShrinkCount = 0;
                                }
                                if(root.Count == 1 && root[0].itemId == itemId) // 如果是单产物
                                {
                                    if (!tempNotShrinkRoot)
                                    {
                                        double ratio = sharedNode.needSpeed / sharedNode.satisfiedSpeed;
                                        // ReSolveForRootShrinking(targetSpeed * ratio);
                                        return ratio;
                                    }
                                }

                                if (minShrinkCount > 0.001f) // 如果有可削减的数量，进行处理
                                {
                                    oriSignNode.SetUnsolvedCountByRecipe(sharedRecipeInfo.recipeNorm); // 此处只是设置一下unsolvedCount，这里不要用TryRecipe，可能扰乱正常solution

                                    if (DSPCalculatorPlugin.developerMode)
                                    {
                                        Debug.Log($"该溢出可以削减 {minShrinkCount}x");
                                    }

                                    double addedCount = -minShrinkCount;
                                    sharedRecipeInfo.count += addedCount;
                                    foreach (var item in sharedRecipeInfo.productIndices) // 对所有该配方的产物进行削减，注意处理产物被视作原矿的情况（将不足部分补齐），因为当初产物如果是原矿，则无视了他到底移除不溢出、溢出多少
                                    {
                                        int productId = item.Key;
                                        double addedSatisfiedSpeed = sharedRecipeInfo.GetOutputSpeedByChangedCount(productId, addedCount);
                                        if (!itemNodes.ContainsKey(productId)) // 这种情况应该不存在才对
                                        {
                                            Debug.LogWarning("在削减溢出配方数量时，出现了配方产物不在字典中的情况，该情况不应该发生。");
                                            itemNodes[productId] = new ItemNode(productId, 0, this);
                                        }
                                        itemNodes[productId].satisfiedSpeed += addedSatisfiedSpeed;
                                        if (DSPCalculatorPlugin.developerMode)
                                        {
                                            Debug.Log($"削减了 {LDB.items.Select(productId).name}, 削减量{addedSatisfiedSpeed}， 现在它satisfied{itemNodes[productId].satisfiedSpeed}");
                                        }
                                        if (itemNodes[productId].satisfiedSpeed < itemNodes[productId].needSpeed)
                                        {
                                            if (itemNodes[productId].IsOre(userPreference))
                                            {
                                                double gap = itemNodes[productId].needSpeed - itemNodes[productId].satisfiedSpeed;
                                                itemNodes[productId].speedFromOre += gap;
                                                itemNodes[productId].satisfiedSpeed = itemNodes[productId].needSpeed;
                                            }
                                            else
                                            {
                                                Debug.LogWarning($"在削减溢出配方数量时，出现了配方id={sharedRecipeInfo.ID}产物{productId}被过度削减的情况（且该产物并未被视作原矿），该情况不应该发生。");
                                            }
                                        }

                                    }
                                    // 对于该配方的每个原材料，削减对应的需求
                                    foreach (var item in sharedRecipeInfo.resourceIndices)
                                    {
                                        int resourceId = item.Key;
                                        double addedNeedSpeed = sharedRecipeInfo.GetInputSpeedByChangedCount(resourceId, addedCount); // 必须要双负，以防止GetInputSpeedByChangedCount不能返回负数
                                        itemNodes[resourceId].needSpeed += addedNeedSpeed; // 这个resourceId应该不可能不存在

                                        if (DSPCalculatorPlugin.developerMode)
                                        {
                                            Debug.Log($"削减了 {LDB.items.Select(resourceId).name}, 削减量{addedNeedSpeed}， need{itemNodes[resourceId].needSpeed}");
                                        }
                                        // 同时加入signNode最为子节点
                                        ItemNode signRelatedNode = new ItemNode(resourceId, 0, this);
                                        oriSignNode.AddChild(signRelatedNode);
                                        oriSignNode.unsolvedCount++; // 这里单独计数，每添加一个child且放入了stack都要计数一个unsolved
                                        stack.Push(signRelatedNode);

                                        // 对于如果需求是原矿，并且有来自于原矿的部分，可以削减，别的就不检查了防止成环
                                        if (itemNodes[resourceId].speedFromOre > 0) // 不需要判断是否是原矿，直接看speedFromOre是不是大于零就可以，因为如果不大于0，即使是原矿也不能处理
                                        {
                                            if (itemNodes[resourceId].speedFromOre > -addedNeedSpeed)
                                            {
                                                itemNodes[resourceId].satisfiedSpeed -= -addedNeedSpeed;
                                                itemNodes[resourceId].speedFromOre -= -addedNeedSpeed;
                                            }
                                            else
                                            {
                                                itemNodes[resourceId].satisfiedSpeed -= itemNodes[resourceId].speedFromOre;
                                                itemNodes[resourceId].speedFromOre = 0;
                                            }
                                        }
                                    }
                                }
                            }

                            //// 然后将其所有的children加入
                            //if (sharedNode.children != null)
                            //{
                            //    for (int i = 0; i < sharedNode.children.Count; i++)
                            //    {
                            //        ItemNode signChildNode = new ItemNode(sharedNode.children[i].itemId, 0, this);
                            //        oriSignNode.AddChild(signChildNode);
                            //        oriSignNode.unsolvedCount++; // 这里单独计数，每添加一个child且放入了stack都要计数一个unsolved
                            //        stack.Push(signChildNode);
                            //    }
                            //}
                        }
                    }
                }
            }
            return 0;
        }

        public void CalcProliferator()
        {
            proliferatorCount.Clear();
            proliferatorCountSelfSprayed.Clear();
            foreach (var data in recipeInfos)
            {
                int id;
                double count;
                data.Value.GetProliferatorUsed(out id, out count);
                if(id > 0)
                {
                    if (!proliferatorCount.ContainsKey(id))
                        proliferatorCount[id] = count;
                    else
                        proliferatorCount[id] += count;
                }
            }
            // 将增产剂的自喷涂后用量记录
            for (int i = 0; i < CalcDB.proliferatorItemIds.Count; i++)
            {
                int itemId = CalcDB.proliferatorItemIds[i];
                if(proliferatorCount.ContainsKey(itemId))
                {
                    int oriCount = LDB.items.Select(itemId).HpMax;
                    int ability = CalcDB.proliferatorAbilitiesMap[itemId];
                    int proliferatedCount = (int)(oriCount * (1.0 + Utils.GetIncMilli(ability, userPreference)));
                    if (proliferatedCount - 1 > oriCount)
                        proliferatorCountSelfSprayed[itemId] = proliferatorCount[itemId] * oriCount / (proliferatedCount - 1);
                    else
                        proliferatorCountSelfSprayed[itemId] = proliferatorCount[itemId];
                }
            }
        }


       

        public void RefreshBlueprintDicts()
        {
            beltsAvailable = new List<BpBeltInfo>();
            sortersAvailable = new List<BpSorterInfo>();

            for (int i = 0; i < BpDB.beltsAscending.Count; i++)
            {
                if (GameMain.history.ItemUnlocked(BpDB.beltsAscending[i].itemId) || !userPreference.bpBeltTechLimit || i == 0) // 第一个最慢的带子总被视为可用的
                    beltsAvailable.Add(BpDB.beltsAscending[i]);
            }
            for (int i = 0; i < BpDB.sortersAscending.Count; i++)
            {
                if (GameMain.history.ItemUnlocked(BpDB.sortersAscending[i].itemId) || !userPreference.bpSorterTechLimit || i == 0) // 第一个最慢的爪子总是被视为可用的
                    sortersAvailable.Add(BpDB.sortersAscending[i]);
            }
        }

        public ItemNode Pop()
        {
            if(nodeStack.Count > 0)
            {
                int index = nodeStack.Count - 1;
                ItemNode node = nodeStack[index];
                nodeStack.RemoveAt(index);
                return node;
            }
            return null;
        }

        public void Push(ItemNode node)
        {
            nodeStack.Add(node);
        }


        public ItemNode StackTail()
        {
            if (nodeStack.Count > 0)
            {
                return nodeStack[nodeStack.Count - 1];
            }

            return null;
        }

        /// <summary>
        /// 求解增产剂的生产线
        /// </summary>
        /// <returns></returns>
        public bool CalcProliferatorInProductLine()
        {
            // 非齐次线性方程组求解
            // 首先建立solution，求解每1个各类增产剂产出，需要消耗多少个自喷涂的各类增产剂，并记录
            double[,] consumeRatio = { { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 } }; // 每种增产剂生产过程中需要消耗的各类增产剂的数量
            for (int i = 0; i < CalcDB.proliferatorItemIds.Count; i++)
            {
                int itemId = CalcDB.proliferatorItemIds[i];
                SolutionTree solutionProlif = new SolutionTree();
                solutionProlif.userPreference = userPreference.ShallowCopy();
                solutionProlif.userPreference.solveProliferators = false; // 必须要！否则无限递归了
                //solutionProlif.targetSpeed = 1;
                //solutionProlif.SetTargetItem0AndBeginSolve(itemId);
                solutionProlif.SetTargetAndSolve(0, itemId, 1);
                foreach (var pConsume in solutionProlif.proliferatorCountSelfSprayed)
                {
                    int index = CalcDB.proliferatorItemIds.IndexOf(pConsume.Key);
                    if(index >= 0 && index < 3)
                    {
                        consumeRatio[i, index] = pConsume.Value;
                    }
                }
            }

            // 对于消耗本体大于产出的情况视为无解
            for (int i = 0; i < 3; i++)
            {
                if (consumeRatio[i, i] >= 1 && proliferatorCountSelfSprayed.ContainsKey(CalcDB.proliferatorItemIds[i]))
                {
                    UIRealtimeTip.Popup("增产剂生产消耗比产出多警告".Translate());
                    return false;
                }
            }

            // 构建线性方程组
            double[,] coefficients = new double[3, 3];
            double[] constants = new double[3];
            for (int i = 0; i < 3; i++)
            {
                int proliferatorId = CalcDB.proliferatorItemIds[i];
                for (int j = 0; j < 3; j++)
                {
                    if (i == j)
                        coefficients[i, j] = (1 - consumeRatio[j, i]);
                    else
                        coefficients[i, j] = -consumeRatio[j, i];
                    
                }

                if (proliferatorCountSelfSprayed.ContainsKey(proliferatorId))
                {
                    constants[i] = proliferatorCountSelfSprayed[proliferatorId];
                }
                else
                {
                    constants[i] = 0;
                }
            }


            // 解方程
            Vector<double> result = null;
            try
            {
                var solverCoef = Matrix<double>.Build.DenseOfArray(coefficients);
                var solverConstants = Vector<double>.Build.DenseOfArray(constants);
                result = solverCoef.Solve(solverConstants);
            }
            catch (Exception)
            {
                Debug.LogWarning("在解方程组时出现错误，该情况不应该发生。");
                UIRealtimeTip.Popup("求解出错警告".Translate());
                return false;
            }

            if(result == null)
            {
                Debug.LogWarning("在解方程组时出现错误，结果为null，该情况不应该发生。");
                UIRealtimeTip.Popup("求解出错警告".Translate());
                return false;
            }

            for (int i = 0; i < result.Count; i++)
            {
                if (result[i] < 0)
                {
                    Debug.LogWarning($"在解方程组时，增产剂{i}需求数量为负数。");
                    UIRealtimeTip.Popup("求解出错警告".Translate());
                    return false;
                }
            }

            // 到此，解没问题
            for (int i = 0; i < result.Count; i++)
            {
                if (result[i] > 0)
                {
                    int proliferatorId = CalcDB.proliferatorItemIds[i];
                    CalcTree(proliferatorId, result[i]);
                }
            }
            // 再次执行一遍去除溢出任务
            double recalcRatio = RemoveOverflow();
            if (recalcRatio > 0)
            {
                return ReSolveForRootShrinking(targets[0].speed * recalcRatio);
            }

            return true;

        }

        /// <summary>
        /// 由于目标产物暴力乘了系数而进行的免溢出计算，则这个目标产物不视为溢出
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public bool CanShowOverflow(int itemId)
        {
            if (root.Count == 1 && itemId == root[0].itemId && tempNotShrinkRoot)
                return false;
            else
                return true;
        }

        //public void TestLog()
        //{
        //    if (DSPCalculatorPlugin.developerMode)
        //    {
        //        List<ItemNode> stack = new List<ItemNode>();
        //        List<int> levelStack = new List<int>();
        //        stack.Add(root);
        //        levelStack.Add(0);
        //        while(stack.Count > 0)
        //        {
        //            ItemNode curNode = stack[stack.Count - 1];
        //            stack.RemoveAt(stack.Count - 1);
        //            int curLevel = levelStack[levelStack.Count - 1];
        //            levelStack.RemoveAt(levelStack.Count - 1);

        //            string log = "";
        //            for (int i = 0; i < curLevel; i++)
        //            {
        //                log += "|  ";
        //            }
        //            log += $"|--- {LDB.items.Select(curNode.itemId).name}";
        //            Debug.Log(log);

        //            //if (!itemNodes.ContainsKey(curNode.itemId))
        //            //    Debug.LogWarning("Not contains");
        //            //if(curNode.unsolvedCount != 0)
        //            //{
        //            //    Debug.LogWarning($"not zero {curNode.itemId} is {curNode.unsolvedCount}");
        //            //}
        //            //else
        //            //{
        //            //    Debug.Log("ok");
        //            //}

        //            for (int i = 0; i < curNode.children.Count; i++)
        //            {
        //                stack.Add(curNode.children[i]);
        //                levelStack.Add(curLevel+1);
        //            }
        //        }
        //    }
        //}

        //public void TestLog2()
        //{
        //    if (DSPCalculatorPlugin.developerMode)
        //    {
        //        List<ItemNode> stack = new List<ItemNode>();
        //        stack.Add(root);
        //        Dictionary<int, ItemNode> visitedNodes = new Dictionary<int,ItemNode>();
        //        while (stack.Count > 0)
        //        {
        //            ItemNode oriNode = stack[stack.Count - 1];
        //            ItemNode curNode = itemNodes[oriNode.itemId];
        //            if(!visitedNodes.ContainsKey(curNode.itemId))
        //            {
        //                visitedNodes.Add(curNode.itemId, curNode);

        //                // 输出
        //                string log = "";
        //                if (curNode.mainRecipe != null)
        //                {
        //                    log = $"{LDB.items.Select(curNode.itemId).name} 需求{curNode.needSpeed}  产出{curNode.satisfiedSpeed}    主要配方：{curNode.mainRecipe.recipeNorm.oriProto.name}  + {curNode.mainRecipe.GetOutputSpeedByChangedCount(curNode.itemId, curNode.mainRecipe.count)}\n";
        //                    for (int i = 0; i < curNode.byProductRecipes.Count; i++)
        //                    {
        //                        RecipeInfo rInfo = curNode.byProductRecipes[i];
        //                        log += $"    来自配方{rInfo.recipeNorm.oriProto.name} +{rInfo.GetOutputSpeedByChangedCount(curNode.itemId, rInfo.count)}\n";
        //                    }
        //                }
        //                else
        //                {
        //                    log = $"{LDB.items.Select(curNode.itemId).name} 原矿 需求{curNode.needSpeed}  产出{curNode.satisfiedSpeed}\n";
        //                }
        //                Debug.Log(log);
        //            }
        //            stack.RemoveAt(stack.Count - 1);

        //            //string log = "";
        //            //log += $"|--- {LDB.items.Select(curNode.itemId).name}";
        //            //Debug.Log(log);

        //            //if (!itemNodes.ContainsKey(curNode.itemId))
        //            //    Debug.LogWarning("Not contains");
        //            //if(curNode.unsolvedCount != 0)
        //            //{
        //            //    Debug.LogWarning($"not zero {curNode.itemId} is {curNode.unsolvedCount}");
        //            //}
        //            //else
        //            //{
        //            //    Debug.Log("ok");
        //            //}

        //            for (int i = 0; i < curNode.children.Count; i++)
        //            {
        //                stack.Add(curNode.children[i]);
        //            }
        //        }
        //    }
        //}

    }


    public class ItemTarget
    {
        public int itemId;
        public double speed;

        public ItemTarget(int itemId, double speed)
        {
            this.itemId = itemId;
            this.speed = speed;
        }
    }

    // 以下情况的附产物溢出问题，不进行解决。
    // 举例: D的配方：1D←1C，还有0.1D+0.2C+1B←1A；C的配方是1C←1B。
    // 在需求1D的情况下，默认采用了1D←1C←1B←1A的路线，其中末端1B←1A实际上采用了0.1D+0.2C+1B←1A这一配方，会产出0.1D和0.2C的副产物，这其实整个路线可以优化，但是处理这种类似的情况可能很复杂？因此对这种成环的副产物溢出，不做处理。
    // 如何处理不成环的同时，跳过成环的副产物溢出情况呢？对于每个溢出的item，挨个判断所有生成它的recipe，对每个recipe，如果它哪怕有一个产出是不溢出的，该recipe不能被修改。如果该recipe所有产物都溢出（当然，如果只有一个产物，就是本体这个溢出的item，那么显然也符合所有产物都溢出的情况），则按照最小溢出比例，cut该recipe的数量，然后继续向下处理该recipe的原材料溢出，直到原矿。遇到环就立刻停止，回到成环的那个点的父亲处，对下一个孩子的溢出继续判断。
}
