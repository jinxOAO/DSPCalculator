using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DSPCalculator.Logic
{
    /// <summary>
    /// 规划产线路线时，每个产物原材料的节点
    /// </summary>
    public class ItemNode
    {
        public int itemId;
        public SolutionTree solutionTree;
        public double needSpeed; // 所需数量
        public double satisfiedSpeed; // 配方已产出的数量
        public double speedFromOre; // 作为原矿部分直接提供的数量，用于最后处理溢出时，可以无论何种情况直接移除的最大部分
        public RecipeInfo mainRecipe; // 最初用于生成此Item的recipe的信息
        public List<RecipeInfo> byProductRecipes; // 在建立solutionTree完成之后，会因为配方的副产物，连接数个节点，连过来时，要在byProductRecipes里面加入
        public List<ItemNode> parents; // 此物品被用于制造
        public List<ItemNode> children; // 制造此物品净需要的原材料物品
        public int unsolvedCount; // 寻找路径的过程中，子物品还未被完全解决的数量，如果为0，则代表此物品已经可以无环地由原矿链式合成。初始时，未解决数量为所用配方的净原材料数
        

        public ItemNode(int itemId, double needSpeed, SolutionTree solutionTree)
        {
            this.itemId = itemId;
            this.needSpeed = needSpeed;
            this.satisfiedSpeed = 0f;
            this.speedFromOre = 0f;
            byProductRecipes = new List<RecipeInfo>();
            parents = new List<ItemNode>();
            children = new List<ItemNode>();
            this.solutionTree = solutionTree;
            this.unsolvedCount = 0;
        }

        public void TryRecipe(NormalizedRecipe recipe)
        {
            int count = 0;
            for (int i = 0; i < recipe.resourceCounts.Length; i++)
            {
                if (recipe.resourceCounts[i] > 0) // 只有净原材料才会被计数
                    count++;
            }

            unsolvedCount = count; // 这个很重要，决定着这个节点是否解决！

            if(byProductRecipes.Count > 0)
            {
                Debug.LogWarning($"itemid = {itemId} 在 TryRecipe时发现已经有byProductRecipe了，不应该出现这种情况。");
            }

            // 然后将配方记录下来
            if (solutionTree.recipeInfos.ContainsKey(recipe.ID)) // 如果其他节点已经用过相同配方了，则引用该配方信息！
            {
                mainRecipe = solutionTree.recipeInfos[recipe.ID];
            }
            else // 否则新建一个配方信息
            {
                mainRecipe = new RecipeInfo(recipe, solutionTree.userPreference);
                solutionTree.recipeInfos[recipe.ID] = mainRecipe;
            }
        }

        public void SetUnsolvedCountByRecipe(NormalizedRecipe recipe)
        {
            int count = 0;
            for (int i = 0; i < recipe.resourceCounts.Length; i++)
            {
                if (recipe.resourceCounts[i] > 0) // 只有净原材料才会被计数
                    count++;
            }

            unsolvedCount = count; // 这个很重要，决定着这个节点是否解决！
        }

        public void AddChild(ItemNode child)
        {
            children.Add(child);
            child.parents.Add(this);
        }

        // 当一个node节点的一个原材料彻底安全时（不会由该原材料的后续原材料导致成环），视为该原材料已经解决，则此项的未解决计数-1
        public void SolveOne()
        {
            if (this.unsolvedCount > 0)
            {
                this.unsolvedCount--;
                if (this.unsolvedCount == 0)
                {
                    for (int i = 0; i < parents.Count; i++)
                    {
                        parents[i].SolveOne();
                    }
                }
            }
        }

        public bool IsOre(UserPreference userPreference)
        {
            bool isOre = CalcDB.itemDict[itemId].defaultAsOre || CalcDB.itemDict[itemId].recipes.Count == 0;
            if (userPreference.itemConfigs.ContainsKey(itemId)) // 查询用户是否指定了该物品的处理规则，是否视为原矿
            {
                isOre = userPreference.itemConfigs[itemId].consideredAsOre || isOre;
                if (userPreference.itemConfigs[itemId].forceNotOre && CalcDB.itemDict[itemId].recipes.Count > 0)
                    isOre = false;
            }
            return isOre;
        }

        /// <summary>
        /// 混带计算专用，用于返回每秒产出份数，目前每份对应1/s，60/min
        /// </summary>
        /// <returns></returns>
        public int GetInserterRatio()
        {
            double perSec = satisfiedSpeed / 60;
            return (int)Math.Ceiling(perSec);
        }

        public void CalcInserterNeeds(out int[] mk3, out int[] mk2, out int[] mk1)
        {
            int total = GetInserterRatio();
            int[] abilities = new int[] { 24, 12, 8, 6, 4, 3, 2 };
            int[] ids = new int[] { CalcDB.inserterMk3Id, CalcDB.inserterMk2Id, CalcDB.inserterMk3Id, CalcDB.inserterMk1Id, CalcDB.inserterMk2Id, CalcDB.inserterMk1Id, CalcDB.inserterMk1Id };
            int[] distance = new int[] { 0, 0, 2, 0, 2, 1, 2 };

            mk3 = new int[] { 0, 0, 0 };
            mk2 = new int[] { 0, 0, 0 };
            mk1 = new int[] { 0, 0, 0 };

            int count = abilities.Length;
            for (int i = 0; i < count && total > 0; i++)
            {
                if(total >= abilities[i])
                {
                    int curCount = total / abilities[i];
                    if (ids[i] == CalcDB.inserterMk3Id)
                        mk3[distance[i]] += curCount;
                    else if (ids[i] == CalcDB.inserterMk2Id)
                        mk2[distance[i]] += curCount;
                    else
                        mk1[distance[i]] += curCount;

                    total -= curCount * abilities[i];
                }
            }

            if (total > 0)
                mk1[2] += 1;
        }
    }
}
