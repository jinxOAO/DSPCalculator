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
        public float needSpeed; // 所需数量
        public float satisfiedSpeed; // 配方已产出的数量
        public RecipeInfo mainRecipe; // 最初用于生成此Item的recipe的信息
        public List<RecipeInfo> byProductRecipes; // 在建立solutionTree完成之后，会因为配方的副产物，连接数个节点，连过来时，要在byProductRecipes里面加入
        public List<ItemNode> parents; // 此物品被用于制造
        public List<ItemNode> children; // 制造此物品净需要的原材料物品
        public int unsolvedCount; // 寻找路径的过程中，子物品还未被完全解决的数量，如果为0，则代表此物品已经可以无环地由原矿链式合成。初始时，未解决数量为所用配方的净原材料数
        public bool isOre { get { return children.Count == 0; } } // 该节点是否是原矿

        public ItemNode(int itemId, float needSpeed, SolutionTree solutionTree)
        {
            this.itemId = itemId;
            this.needSpeed = needSpeed;
            this.satisfiedSpeed = 0f;
            byProductRecipes = new List<RecipeInfo>();
            parents = new List<ItemNode>();
            children = new List<ItemNode>();
            this.solutionTree = solutionTree;
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
    }
}
