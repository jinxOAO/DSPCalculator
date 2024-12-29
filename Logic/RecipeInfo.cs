using DSPCalculator.Compatibility;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        public UserPreference userPreference;
        public int incLevel
        {
            get
            {

                if (userPreference.recipeConfigs.ContainsKey(ID) && userPreference.recipeConfigs[ID].incLevel >= 0) // 如果该配方有专属设置，且不使用全局
                {
                    RecipeConfig config = userPreference.recipeConfigs[ID];
                    return config.incLevel;
                }
                else // 否则应用全局设置
                {
                    return userPreference.globalIncLevel;
                }
            }
        }
        public bool isInc
        {
            get
            {
                if (userPreference.recipeConfigs.ContainsKey(ID) && userPreference.recipeConfigs[ID].forceIncMode >= 0) // 如果该配方有专属设置，且不使用全局
                {
                     return userPreference.recipeConfigs[ID].forceIncMode == 1; // 等于0代表强制加速
                }
                else // 否则应用全局设置
                {
                    return recipeNorm.productive && userPreference.globalIsInc;
                }
            }
        }
        public int assemblerItemId
        {
            get
            {
                if (userPreference.recipeConfigs.ContainsKey(ID) && userPreference.recipeConfigs[ID].assemblerItemId > 0)
                {
                    return userPreference.recipeConfigs[ID].assemblerItemId;
                }
                else if (userPreference.globalAssemblerIdByType.ContainsKey(recipeNorm.type) && userPreference.globalAssemblerIdByType[recipeNorm.type] > 0)
                {
                    return userPreference.globalAssemblerIdByType[recipeNorm.type];
                }
                else
                {
                    return CalcDB.assemblerListByType[recipeNorm.type][0].ID;
                }
            }
        }

        public double assemblerCount
        {
            get
            {
                AssemblerData assemblerData = CalcDB.assemblerDict[assemblerItemId];
                double countD = count / assemblerData.speed / 60;
                if (!isInc && incLevel >= 0 && incLevel < Cargo.accTableMilli.Length)
                    countD = countD / (1.0 + Cargo.accTableMilli[incLevel]);
                return countD;
            }
        }

        public double displayCount { get { return count / 60; } } // 以每分钟计算产量的话，则应该

        public RecipeInfo(NormalizedRecipe recipe, UserPreference preference)
        {
            this.ID = recipe.ID;
            this.recipeNorm = recipe;
            this.userPreference = preference;
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

        // 狄拉克buff
        public double CalcOutputDiracInc(int itemId)
        {
            if(userPreference.dirac)
            {
                if(recipeNorm.type == (int)ERecipeType.Particle && recipeNorm.products[0] == 1122 && recipeNorm.products.Length > 1)
                {
                    return 0.5;
                }
            }
            return 0;
        }

        public double CalcCountByOutputSpeed(int itemId, double speed)
        {
            if (productIndices.ContainsKey(itemId))
            {
                // 劣质加工
                if (userPreference.inferior && itemId == 1501 && recipeNorm.products[0] == 1501)
                    speed = speed / 2;

                int index = productIndices[itemId];

                double addedCount = speed / recipeNorm.productCounts[index] * recipeNorm.time;

                if (isInc && incLevel >= 0 && incLevel < Cargo.incTableMilli.Length) // 增产效果计算
                {
                    return addedCount / (1.0 + Cargo.incTableMilli[incLevel] + CalcOutputDiracInc(itemId));
                }
                else
                {
                    return addedCount / (1.0 + CalcOutputDiracInc(itemId));
                }
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
                double factor = 1.0;
                // 劣质加工
                if (userPreference.inferior && itemId == 1501 && recipeNorm.products[0] == 1501)
                    factor = factor + 1.0;

                int index = productIndices[itemId];
                if (isInc && incLevel >= 0 && incLevel <= Cargo.incTableMilli.Length)
                    return factor * changedCount * recipeNorm.productCounts[index] / recipeNorm.time * (1.0 + Cargo.incTableMilli[incLevel] + CalcOutputDiracInc(itemId));
                else
                    return factor * changedCount * recipeNorm.productCounts[index] / recipeNorm.time * (1.0 + CalcOutputDiracInc(itemId));
            }
            Debug.LogWarning($"获取配方输出数量时，试图用非此配方{recipeNorm.oriProto.name}的产物{itemId}计算，先前路径可能存在逻辑错误。");
            return 0;
        }

        public double GetInputSpeedByChangedCount(int itemId, double changedCount)
        {
            
            double result = 0;
            if (resourceIndices.ContainsKey(itemId))
            {
                int index = resourceIndices[itemId];
                result = changedCount * recipeNorm.resourceCounts[index] / recipeNorm.time;
            }
            else
            {
                Debug.LogWarning($"获取配方输入数量时，试图用非此配方{recipeNorm.oriProto.name}的原材料{itemId}计算，先前路径可能存在逻辑错误。");
            }

            // 判断蓝buff
            bool blueBuffFlag = userPreference.bluebuff;
            blueBuffFlag = blueBuffFlag && (recipeNorm.type == (int)ERecipeType.Assemble || recipeNorm.type == 9 || recipeNorm.type == 10 || recipeNorm.type == 12);
            blueBuffFlag = blueBuffFlag && recipeNorm.resources.Length > 1 && recipeNorm.products[0] != 1803 && recipeNorm.products[0] != 6006;
            blueBuffFlag = blueBuffFlag && recipeNorm.resources[0] == itemId;
            if (blueBuffFlag)
            {
                double output = 0;
                // 这里不能用getoutput方法，因为也许有的第一产物不是直接的净产物，所以显然下面用的recipe也要是oriProto而不是norm里面的
                if (isInc && incLevel >= 0 && incLevel <= Cargo.incTableMilli.Length)
                    output = changedCount * recipeNorm.oriProto.ResultCounts[0] / recipeNorm.time * (1.0 + Cargo.incTableMilli[incLevel]);
                else
                    output = changedCount * recipeNorm.oriProto.ResultCounts[0] / recipeNorm.time;
                result -= output;
            }

            // 判断能量迸发
            bool energyBurstFlag = userPreference.energyBurst;
            int returnIndex = -1;
            int rocketId = recipeNorm.products[0];
            if (energyBurstFlag)
            {
                if (rocketId >= 9488 && rocketId <= 9490 || rocketId == 1503 && CompatManager.GB)
                    returnIndex = 2;
                else if (rocketId == 9491 || rocketId == 9492 || rocketId == 9510 || rocketId == 1503)
                    returnIndex = 1;
                if(returnIndex > 0 && returnIndex < recipeNorm.resources.Length && productIndices.ContainsKey(itemId) && returnIndex == resourceIndices[itemId])
                {
                    double output = 0;
                    // 这里不能用getoutput方法，因为也许有的第一产物不是直接的净产物，所以显然下面用的recipe也要是oriProto而不是norm里面的
                    if (isInc && incLevel >= 0 && incLevel <= Cargo.incTableMilli.Length)
                        output = changedCount * recipeNorm.oriProto.ResultCounts[0] / recipeNorm.time * (1.0 + Cargo.incTableMilli[incLevel]);
                    else
                        output = changedCount * recipeNorm.oriProto.ResultCounts[0] / recipeNorm.time;
                    result -= 2 * output;
                }
            }
            if(result < 0)
                result = 0;
            return result;
        }

        public double GetOutputSpeedByItemId(int itemId)
        {
            return GetOutputSpeedByChangedCount(itemId, count);
        }

        public double GetTotalEnergyConsumption()
        {
            double ratio = 1.0;
            if(incLevel >= 0 && incLevel < Cargo.powerTableRatio.Length)
            {
                ratio = Cargo.powerTableRatio[incLevel];
            }
            int fullyWork = (int)assemblerCount;
            double partIdle = assemblerCount - fullyWork;
            double additionalIdle = 0;
            if(partIdle > 0.001f) 
            {
                additionalIdle = 1 - partIdle;
            }
            AssemblerData assemblerData = CalcDB.assemblerDict[assemblerItemId];

            return ((fullyWork + partIdle) * assemblerData.workEnergyW * ratio) + additionalIdle * assemblerData.idleEnergyW;
        }

        // 增产剂消耗只考虑蓝buff，其余的能量迸发等均不考虑
        public void GetProliferatorUsed(out int proliferatorId, out double proliferatorCount)
        {
            if(incLevel == 0 || !CalcDB.proliferatorAbilityToId.ContainsKey(incLevel))
            {
                proliferatorId = 0;
                proliferatorCount = 0;
                return;
            }
            proliferatorId = CalcDB.proliferatorAbilityToId[incLevel];
            int itemsPerProliferator = LDB.items.Select(proliferatorId).HpMax;

            RecipeProto recipeProto = recipeNorm.oriProto;
            double itemNeeds = 0;
            for (int i = 0; i < recipeProto.ItemCounts.Length; i++)
            {
                itemNeeds += recipeProto.ItemCounts[i];
            }
            // 蓝buff返回的原材料不需要涂增产，所以要减去
            bool blueBuffFlag = userPreference.bluebuff;
            blueBuffFlag = blueBuffFlag && (recipeNorm.type == (int)ERecipeType.Assemble || recipeNorm.type == 9 || recipeNorm.type == 10 || recipeNorm.type == 12);
            blueBuffFlag = blueBuffFlag && recipeNorm.resources.Length > 1 && recipeNorm.products[0] != 1803 && recipeNorm.products[0] != 6006;
            double shrinkByRelic = 0;
            if (blueBuffFlag)
            {
                // 这里不能用getoutput方法，因为也许有的第一产物不是直接的净产物，所以显然下面用的recipe也要是oriProto而不是norm里面的
                if (isInc && incLevel >= 0 && incLevel <= Cargo.incTableMilli.Length)
                    shrinkByRelic += recipeNorm.oriProto.ResultCounts[0] * (1.0 + Cargo.incTableMilli[incLevel]);
                else
                    shrinkByRelic += recipeNorm.oriProto.ResultCounts[0];
            }
            // 能量迸发
            bool energyBurstFlag = userPreference.energyBurst;
            int returnIndex = -1;
            int rocketId = recipeNorm.products[0];
            if (energyBurstFlag)
            {
                if (rocketId >= 9488 && rocketId <= 9490 || rocketId == 1503 && CompatManager.GB)
                    returnIndex = 2;
                else if (rocketId == 9491 || rocketId == 9492 || rocketId == 9510 || rocketId == 1503)
                    returnIndex = 1;
                if (returnIndex > 0 && returnIndex < recipeNorm.resources.Length)
                {
                    
                    double output = 0;
                    // 这里不能用getoutput方法，因为也许有的第一产物不是直接的净产物，所以显然下面用的recipe也要是oriProto而不是norm里面的
                    if (isInc && incLevel >= 0 && incLevel <= Cargo.incTableMilli.Length)
                        shrinkByRelic += Math.Min(2 * recipeNorm.oriProto.ResultCounts[0], recipeNorm.oriProto.ItemCounts[returnIndex]) * (1.0 + Cargo.incTableMilli[incLevel]); // 返还不能超过用量
                    else
                        shrinkByRelic += Math.Min(2 * recipeNorm.oriProto.ResultCounts[0], recipeNorm.oriProto.ItemCounts[returnIndex]);
                }
            }

            proliferatorCount = 1.0 * count / recipeNorm.time * itemNeeds / itemsPerProliferator - shrinkByRelic;

        }
    }
}
