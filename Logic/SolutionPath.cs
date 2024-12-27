using JetBrains.Annotations;
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
        public int targetItem { get; private set; } // 需要计算的最终产物
        public float targetSpeed; // 最终产物需要的速度（/s)
        public float displaySpeedRatio; // 允许玩家随时修改targetSpeed而不重新计算整个树，则通过最后所有显示都追加一个系数来实现

        public UserPreference userPreference; // 用户规定的，比如：某些物品需要用特定配方生产、某些物品的配方需要几级增产剂等等

        public ItemNode root;
        public Dictionary<int, ItemNode> itemNodes; // 用于储存节点状态与信息，并不代表最终的树里有这个节点。一旦这里存储了一个solved的节点，这个物品就永远安全了，用它作为原材料不用担心它成环
        public Dictionary<int, RecipeInfo> recipeInfos; // 用于储存配方信息

        public List<ItemNode> nodeStack; // 用于解决过程中存储暂存节点
        public bool unsolved { get { return nodeStack.Count > 0; } }


        public SolutionTree() 
        {
            itemNodes = new Dictionary<int, ItemNode>();
            recipeInfos = new Dictionary<int, RecipeInfo>();
            nodeStack = new List<ItemNode>();
            userPreference = new UserPreference();
            targetSpeed = 60;
            displaySpeedRatio = 1;
        }

        public void ClearTree()
        {
            itemNodes.Clear();
            recipeInfos.Clear();
        }

        public void ClearUserPreference()
        {
            userPreference.Clear();
        }

        public void SetTargetItemAndBeginSolve(int targetItem)
        {
            ClearTree();
            this.targetItem = targetItem;
            Solve();
        }

        public void ChangeTargetSpeed(float targetSpeed)
        {
            this.targetSpeed = targetSpeed;
        }


        public void Solve()
        {
            root = new ItemNode(targetItem, targetSpeed, this);
            ItemNode node = root;
            Push(node);

            SolvePath();

            CalcAll();
        }

        /// <summary>
        /// 解决物品合成路径（找到一个无环有向图）
        /// </summary>
        public void SolvePath()
        {
            while (unsolved)
            {
                Debug.Log("S1");
                // 出栈！对这个节点进行处理。注意：出栈时这个节点虽然已经连接入
                ItemNode cur = Pop();
                int itemId = cur.itemId;
                // 开始处理这个item 选择合理的配方 或者 此物品已经解决过路线，不再寻找一次了

                // 1. 如果这个节点已经被处理过了，那么要么他已经解决，unsolvedCount是0，如果不是0，则代表着出现了环路，需要更换配方
                if(itemNodes.ContainsKey(itemId))
                {
                    Debug.Log("S2");
                    int unsolvedCount = itemNodes[itemId].unsolvedCount;

                    if(unsolvedCount <= 0)
                    {
                        if (cur.parents.Count > 0)
                        {
                            cur.parents[0].SolveOne();
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
                        Debug.Log("S3");
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
                            UIRealtimeTip.Popup("存在环路！请清除用户配置".Translate());
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
                    Debug.Log("S4");
                    // 将此物品node加入到处理过的节点记录中
                    itemNodes[itemId] = cur;

                    bool isOre = CalcDB.itemDict[itemId].defaultAsOre || CalcDB.itemDict[itemId].recipes.Count == 0;
                    if (userPreference.itemConfigs.ContainsKey(itemId))
                    {
                        isOre = userPreference.itemConfigs[itemId].consideredAsOre || isOre;
                    }
                    if (isOre) // 如果是原矿
                    {
                        Debug.Log("S4-ore");
                        cur.unsolvedCount = 0;
                        if(cur.parents.Count > 0)
                            cur.parents[0].SolveOne();

                        Debug.Log("S4-ore-2");
                    }
                    else
                    {
                        int recipeId = CalcDB.itemDict[itemId].recipes[0].ID; // 默认取这个物品0号位的配方
                        if (userPreference.itemConfigs.ContainsKey(itemId)) // 配方查询用哪个配方：用户是否指定了该物品的处理规则（特定配方或者视为原矿），如果有则读取用户设置
                        {
                            recipeId = userPreference.itemConfigs[itemId].recipeID;
                        }
                        Debug.Log("S5");
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
                    Debug.Log("S6");
                }

                Debug.Log("S7");
            }

            Debug.Log("SOLVED!");
        }

        /// <summary>
        /// 在准备好的有向图的基础上，计算各个节点需要的生产速度
        /// </summary>
        public void CalcAll()
        {

        }

        /// <summary>
        /// 成比例地修改所有物品的生产和需求速度
        /// </summary>
        /// <param name="ratio"></param>
        public void ChangeAllSpeedRatio(float ratio)
        {
            displaySpeedRatio = ratio;
            // Refresh 一些显示？ 应该由调用此方法的UI执行！
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
    }

    // 以下情况的附产物溢出问题，不进行解决。
    // 举例: D的配方：1D←1C，还有0.1D+0.2C+1B←1A；C的配方是1C←1B。
    // 在需求1D的情况下，默认采用了1D←1C←1B←1A的路线，其中末端1B←1A实际上采用了0.1D+0.2C+1B←1A这一配方，会产出0.1D和0.2C的副产物，这其实整个路线可以优化，但是处理这种类似的情况可能很复杂？因此对这种成环的副产物溢出，不做处理。
    // 如何处理不成环的同时，跳过成环的副产物溢出情况呢？对于每个溢出的item，挨个判断所有生成它的recipe，对每个recipe，如果它哪怕有一个产出是不溢出的，该recipe不能被修改。如果该recipe所有产物都溢出（当然，如果只有一个产物，就是本体这个溢出的item，那么显然也符合所有产物都溢出的情况），则按照最小溢出比例，cut该recipe的数量，然后继续向下处理该recipe的原材料溢出，直到原矿
}
