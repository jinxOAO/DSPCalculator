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
        public double count; // 需要1.0倍率的工厂数量，以每秒计算的话
        public NormalizedRecipe recipeNorm;
        public Dictionary<int, int> productIndices; // key为product的itemId, value为productId所在的index
        public Dictionary<int, int> resourceIndices; // key为resource的itemId, value为resourceId所在的index
        public int incLevel;
        public int accLevel;
        public bool isInc;
        public int assemblerItemId;
        public double displayCount { get { return count / 60; } } // 以每分钟计算产量的话，则应该

        public RecipeInfo(NormalizedRecipe recipe, UserPreference preference)
        {
            this.ID = recipe.ID;
            this.recipeNorm = recipe;
            productIndices = new Dictionary<int, int>();
            resourceIndices = new Dictionary<int, int>();
            this.count = 0;

            for (int i = 0; i < recipe.products.Length; i++)
            {
                if (recipe.productCounts[i] > 0) // 只有净产物才算
                {
                    int productId = recipe.products[i];
                    productIndices[productId] = i;
                }
            }
            for (int i = 0; i < recipe.resources.Length; i++)
            {
                if (recipe.resourceCounts[i] > 0) // 只有净原材料才算
                {
                    int resourceId = recipe.resources[i];
                    resourceIndices[resourceId] = i;
                }
            }


            if (preference.recipeConfigs.ContainsKey(ID)) // 如果该配方有专属设置
            {
                RecipeConfig config = preference.recipeConfigs[ID];
                incLevel = config.incLevel;
                accLevel = config.accLevel;
                if(config.forceIncMode < 0)
                    isInc = recipe.productive && preference.globalIsInc;
                else
                    isInc = config.forceIncMode == 1; // 等于0代表强制加速
                assemblerItemId = config.assemblerItemId;
            }
            else // 否则应用全局设置
            {
                incLevel = preference.globalIncLevel;
                accLevel = preference.globalAccLevel;
                isInc = recipe.productive && preference.globalIsInc;
                if (preference.globalAssemblerIdByType.ContainsKey(recipe.type))
                    assemblerItemId = preference.globalAssemblerIdByType[recipe.type];
                else
                    assemblerItemId = 0;
            }
        }

        public void LoadConfig(UserPreference preference)
        {
            if (preference.recipeConfigs.ContainsKey(ID)) // 如果该配方有专属设置
            {
                RecipeConfig config = preference.recipeConfigs[ID];
                incLevel = config.incLevel;
                accLevel = config.accLevel;
                if (config.forceIncMode < 0)
                    isInc = recipeNorm.productive && preference.globalIsInc;
                else
                    isInc = config.forceIncMode == 1; // 等于0代表强制加速
                assemblerItemId = config.assemblerItemId;
            }
            else // 否则应用全局设置
            {
                incLevel = preference.globalIncLevel;
                accLevel = preference.globalAccLevel;
                isInc = recipeNorm.productive && preference.globalIsInc;
                if (preference.globalAssemblerIdByType.ContainsKey(recipeNorm.type))
                    assemblerItemId = preference.globalAssemblerIdByType[recipeNorm.type];
                else
                    assemblerItemId = 0;
            }
        }

        /// <summary>
        /// 根据传入的目标物品的需求速度（/s），增加recipeInfo的最终倍率，来提供足够的输出
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="needSpeed"></param>
        /// <returns></returns>
        public double AddCountByOutputNeed(int itemId, double needSpeed)
        {
            double addedCount = CalcCountByOutputSpeed(itemId, needSpeed);
            this.count += addedCount;
            return addedCount;
        }

        public double CalcCountByOutputSpeed(int itemId, double speed)
        {
            if (productIndices.ContainsKey(itemId))
            {
                int index = productIndices[itemId];
                double addedCount = speed / recipeNorm.productCounts[index] * recipeNorm.time; 
                return addedCount;
            }
            else
            {
                return 0;
            }
        }

        public double GetOutputSpeedByChangedCount(int itemId, double changedCount)
        {
            if (productIndices.ContainsKey(itemId))
            {
                int index = productIndices[itemId];
                return changedCount * recipeNorm.productCounts[index] / recipeNorm.time;
            }
            Debug.LogWarning($"获取配方输出数量时，试图用非此配方{recipeNorm.oriProto.name}的产物{itemId}计算，先前路径可能存在逻辑错误。");
            return 0;
        }

        public double GetInputSpeedByChangedCount(int itemId, double changedCount)
        {
            if (resourceIndices.ContainsKey(itemId))
            {
                int index = resourceIndices[itemId];
                return changedCount * recipeNorm.resourceCounts[index] / recipeNorm.time;
            }
            Debug.LogWarning($"获取配方输入数量时，试图用非此配方{recipeNorm.oriProto.name}的原材料{itemId}计算，先前路径可能存在逻辑错误。");
            return 0;
        }

        public double GetOutputSpeedByItemId(int itemId)
        {
            return GetOutputSpeedByChangedCount(itemId, count);
        }
    }
}
