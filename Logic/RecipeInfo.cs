using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DSPCalculator.Logic
{
    /// <summary>
    /// 在计算过程中，每个item存储的生成它的那个配方的信息。在一个solution中，每一个ID的recipeInfo，只能有一个实例
    /// </summary>
    public class RecipeInfo
    {
        public int ID;
        public float count; // 需要1.0倍率的工厂数量
        public NormalizedRecipe recipeNorm;
        public Dictionary<int, int> productIndices; // key为product的itemId, value为productId所在的index
        public Dictionary<int, int> resourceIndices; // key为resource的itemId, value为resourceId所在的index
        public int incLevel;
        public int accLevel;
        public bool isInc;
        public int assemblerIndex;

        public RecipeInfo(NormalizedRecipe recipe, UserPreference preference)
        {
            this.ID = recipe.ID;
            this.recipeNorm = recipe;
            productIndices = new Dictionary<int, int>();
            resourceIndices = new Dictionary<int, int>();

            for (int i = 0; i < recipe.products.Length; i++)
            {
                int productId = recipe.products[i];
                productIndices[productId] = i;
            }
            for (int i = 0; i < recipe.resources.Length; i++)
            {
                int resourceId = recipe.resources[i];
                resourceIndices[resourceId] = i;
            }


            if (preference.recipeConfigs.ContainsKey(ID)) // 如果该配方有专属设置
            {
                RecipeConfig config = preference.recipeConfigs[ID];
                incLevel = config.incLevel;
                accLevel = config.accLevel;
                isInc = config.isInc;
                assemblerIndex = config.assemblerIndex;
            }
            else // 否则应用全局设置
            {
                incLevel = preference.globalIncLevel;
                accLevel = preference.globalAccLevel;
                isInc = recipe.productive && preference.globalIsInc;
                if (preference.globalAssemblerIndexByType.ContainsKey(recipe.type))
                    assemblerIndex = preference.globalAssemblerIndexByType[recipe.type];
                else
                    assemblerIndex = 0;
            }
        }

        public float GetOutputCount(int itemId)
        {
            if (productIndices.ContainsKey(itemId))
            {
                int index = productIndices[itemId];
                return count * recipeNorm.productCounts[index];
            }
            Debug.LogWarning($"获取配方输出数量时，试图用非此配方{recipeNorm.oriProto.name}的产物{itemId}计算，先前路径可能存在逻辑错误。");
            return 0;
        }
    }
}
