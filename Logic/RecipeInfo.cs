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
                if (IASpecializationType == 2 && GetSpecBuffLevel() > 0) // 巨构反应釜固定给增产
                    return 4;
                if(useIA)
                {
                    if (!isInc)
                        return 0; // 星际组装厂有的配方无法设定为
                }

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
                if (useIA)
                {
                    return canInc; // 对于星际组装厂，能增产的配方都设定为增产，无法增产的配方都设定为加速，但是加速永远是0级
                }
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

        public bool canInc
        {
            get 
            { 
                if (useIA)
                {
                    if (GetSpecBuffLevel() > 0) // 星际组装厂特化会让激活的配方允许增产
                        return true;
                }

                return recipeNorm.productive;
            }
        }
        /// <summary>
        /// -1：该配方不使用星际组装厂。>=0则代表组装厂及其特化。
        /// </summary>
        public int IASpecializationType
        {
            get
            {
                if (!CompatManager.MMS)
                    return -1;

                bool IAActive = false;
                if (userPreference.recipeConfigs.ContainsKey(ID)) // 如果此配方有专属设定
                {
                    if (userPreference.recipeConfigs[ID].forceUseIA) // 强制使用组装厂，无视全局设定
                    {
                        if (userPreference.recipeConfigs[ID].IAType >= 0)
                            return userPreference.recipeConfigs[ID].IAType;
                        else
                            return userPreference.globalIAType;
                    }
                    else
                    {
                        if (userPreference.recipeConfigs[ID].assemblerItemId <= 0) // 说明没有特殊指定的生产设施，则要根据全局的默认设置来
                        {
                            if (userPreference.globalUseIA) // 虽然采用了全局是否使用组装厂的设定，但是还是要首先看自己的组装厂特化有没有特殊设定
                            {
                                if (userPreference.recipeConfigs[ID].IAType >= 0)
                                    return userPreference.recipeConfigs[ID].IAType;
                                else
                                    return userPreference.globalIAType;
                            }
                            else
                                return -1;
                        }
                        else // 也就是说有强制指定的非星际组装厂的生产设施
                        {
                            return -1;
                        }
                    }
                }
                else if (userPreference.globalUseIA) // 此配方没有专属设定，则直接使用全局设定，如果全局启用则返回全局的特化类型
                {
                    return userPreference.globalIAType;
                }
                else
                {
                    return -1;
                }

            }
        }
        public bool useIA
        {
            get
            {
                return IASpecializationType >= 0;
            }
        }
        public int assemblerItemId
        {
            get
            {
                if (userPreference.recipeConfigs.ContainsKey(ID) && userPreference.recipeConfigs[ID].assemblerItemId > 0) // 有特殊配方设定
                {
                    return userPreference.recipeConfigs[ID].assemblerItemId;
                }
                else if (userPreference.globalAssemblerIdByType.ContainsKey(recipeNorm.type) && userPreference.globalAssemblerIdByType[recipeNorm.type] > 0) // 有全局（按照配方类型设定的）统一使用的设施
                {
                    return userPreference.globalAssemblerIdByType[recipeNorm.type];
                }
                else if (CalcDB.assemblerListByType.ContainsKey(recipeNorm.type) && CalcDB.assemblerListByType[recipeNorm.type].Count > 0) // 什么特殊设定都没有，使用0号位置的默认设施
                {
                    return CalcDB.assemblerListByType[recipeNorm.type][0].ID;
                }
                else // 正常情况下不应该到这里
                {
                    Debug.LogError("DSPCalculator get assemblerId Error. no such aseembler that could match the recipe.");
                    return -1;
                }
            }
        }

        public double assemblerCount
        {
            get
            {
                if (useIA) // 星际组装厂
                    return 1;

                AssemblerData assemblerData = CalcDB.assemblerDict[assemblerItemId];
                double countD = count / assemblerData.speed / 60;
                if (!isInc && incLevel >= 0 && incLevel < Cargo.accTableMilli.Length)
                    countD = countD / (1.0 + Utils.GetAccMilli(incLevel, userPreference));
                return countD;
            }
        }

        /// <summary>
        /// 黑雾台会有双倍产出（虽然是以增产的形式提供，但是25%增产会产出2.5倍产物，而非2.25，所以二者实际叠乘而非叠加）
        /// </summary>
        public double bonusFactor
        {
            get
            {
                if (assemblerItemId == CalcDB.dfSmelterId && CompatManager.GB && !useIA)
                    return 2.0;
                else
                    return 1.0;
            }
        }

        public double bonusInc
        {
            get 
            {
                if(CompatManager.MMS && useIA)
                {
                    int specType = IASpecializationType;
                    int specLevel = GetSpecBuffLevel();
                    if(specLevel > 0)
                    {
                        if (specType == 3)
                        {
                            return 0.25;
                        }
                        else if (specType == 4)
                        {
                            return 0.25 * specLevel;
                        }
                        else if (specType == 5)
                        {
                            return 0.25 * specLevel;
                        }
                    }
                }
                return 0f;
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

        /// <summary>
        /// 根据物品id和需要的输出速度，计算出需要多少倍的配方才能达到
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="speed"></param>
        /// <returns></returns>
        public double CalcCountByOutputSpeed(int itemId, double speed)
        {
            if (productIndices.ContainsKey(itemId))
            {
                // 劣质加工
                if (userPreference.inferior && itemId == 1501 && recipeNorm.products[0] == 1501 && !useIA)
                    speed = speed * recipeNorm.productCounts[0] / (recipeNorm.productCounts[0] + 1);

                int index = productIndices[itemId];

                double addedCount = speed / recipeNorm.productCounts[index] * recipeNorm.time;

                if (isInc && incLevel >= 0 && incLevel < Cargo.incTableMilli.Length) // 增产效果计算
                {
                    return addedCount / (1.0 + Utils.GetIncMilli(incLevel, userPreference) + CalcOutputDiracInc(itemId) + bonusInc) / bonusFactor;
                }
                else
                {
                    return addedCount / (1.0 + CalcOutputDiracInc(itemId) + bonusInc) / bonusFactor;
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
                double factor = 1.0 * bonusFactor;
                // 劣质加工
                if (userPreference.inferior && itemId == 1501 && recipeNorm.products[0] == 1501 && !useIA)
                    factor = factor * (recipeNorm.productCounts[0] + 1) / recipeNorm.productCounts[0];

                int index = productIndices[itemId];
                if (isInc && incLevel >= 0 && incLevel <= Cargo.incTableMilli.Length)
                    return factor * changedCount * recipeNorm.productCounts[index] / recipeNorm.time * (1.0 + Utils.GetIncMilli(incLevel, userPreference) + CalcOutputDiracInc(itemId) + bonusInc);
                else
                    return factor * changedCount * recipeNorm.productCounts[index] / recipeNorm.time * (1.0 + CalcOutputDiracInc(itemId) + bonusInc);
            }
            Debug.LogWarning($"获取配方输出数量时，试图用非此配方{recipeNorm.oriProto.name}的产物{itemId}计算，先前路径可能存在逻辑错误。");
            return 0;
        }

        public double GetOutputSpeedOriProto(int index, double changedCount) // 不用recipeNorm
        {
            int itemId = recipeNorm.oriProto.Results[index];
            double factor = 1.0 * bonusFactor;
            // 劣质加工
            if (userPreference.inferior && itemId == 1501 && recipeNorm.oriProto.Results[0] == 1501 && !useIA)
                factor = factor * (recipeNorm.oriProto.ResultCounts[0] + 1) / recipeNorm.oriProto.ResultCounts[0];

            if (isInc && incLevel >= 0 && incLevel <= Cargo.incTableMilli.Length)
                return factor * changedCount * recipeNorm.oriProto.ResultCounts[index] / recipeNorm.time * (1.0 + Utils.GetIncMilli(incLevel, userPreference) + CalcOutputDiracInc(itemId) + bonusInc);
            else
                return factor * changedCount * recipeNorm.oriProto.ResultCounts[index] / recipeNorm.time * (1.0 + CalcOutputDiracInc(itemId) + bonusInc);

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
            blueBuffFlag = blueBuffFlag && (recipeNorm.type == (int)ERecipeType.Assemble || recipeNorm.type == 9 || recipeNorm.type == 10 || recipeNorm.type == 12 || useIA);
            blueBuffFlag = blueBuffFlag && recipeNorm.resources.Length > 1 && recipeNorm.products[0] != 1803 && recipeNorm.products[0] != 6006;
            blueBuffFlag = blueBuffFlag && recipeNorm.resources[0] == itemId;
            if (blueBuffFlag)
            {
                double output = 0;
                // 这里不能用getoutput方法，因为也许有的第一产物不是直接的净产物，所以显然下面用的recipe也要是oriProto而不是norm里面的
                if (isInc && incLevel >= 0 && incLevel <= Cargo.incTableMilli.Length)
                    output = changedCount * recipeNorm.oriProto.ResultCounts[0] / recipeNorm.time * (1.0 + Utils.GetIncMilli(incLevel, userPreference) + bonusInc);
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
                if(returnIndex > 0 && returnIndex < recipeNorm.resources.Length && resourceIndices.ContainsKey(itemId) && returnIndex == resourceIndices[itemId])
                {
                    double output = 0;
                    // 这里不能用getoutput方法，因为也许有的第一产物不是直接的净产物，所以显然下面用的recipe也要是oriProto而不是norm里面的
                    if (isInc && incLevel >= 0 && incLevel <= Cargo.incTableMilli.Length)
                        output = changedCount * recipeNorm.oriProto.ResultCounts[0] / recipeNorm.time * (1.0 + Utils.GetIncMilli(incLevel, userPreference) + bonusInc);
                    else
                        output = changedCount * recipeNorm.oriProto.ResultCounts[0] / recipeNorm.time;
                    result -= 2 * output;
                }
            }
            if (result < 0 && changedCount >= 0)
            {
                //Debug.LogWarning($"GetInputSpeedByChangedCount({itemId}, {changedCount})试图在changedCount非负时，返回负数");
                result = 0;
            }
            else if (result > 0 && changedCount < 0)
            {
                //Debug.LogWarning($"GetInputSpeedByChangedCount({itemId}, {changedCount})试图在changedCount非正时，返回正数");
                result = 0;
            }

            return result;
        }

        public double GetInputSpeedOriProto(int index, double changedCount)
        {
            int itemId = recipeNorm.oriProto.Items[index];
            double result = 0;
            result = changedCount * recipeNorm.oriProto.ItemCounts[index] / recipeNorm.time;

            // 判断蓝buff
            bool blueBuffFlag = userPreference.bluebuff;
            blueBuffFlag = blueBuffFlag && (recipeNorm.type == (int)ERecipeType.Assemble || recipeNorm.type == 9 || recipeNorm.type == 10 || recipeNorm.type == 12 || useIA);
            blueBuffFlag = blueBuffFlag && recipeNorm.resources.Length > 1 && recipeNorm.products[0] != 1803 && recipeNorm.products[0] != 6006;
            blueBuffFlag = blueBuffFlag && recipeNorm.resources[0] == itemId;
            if (blueBuffFlag)
            {
                double output = 0;
                // 这里不能用getoutput方法，因为也许有的第一产物不是直接的净产物，所以显然下面用的recipe也要是oriProto而不是norm里面的
                if (isInc && incLevel >= 0 && incLevel <= Cargo.incTableMilli.Length)
                    output = changedCount * recipeNorm.oriProto.ResultCounts[0] / recipeNorm.time * (1.0 + Utils.GetIncMilli(incLevel, userPreference) + bonusInc);
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
                if (returnIndex > 0 && returnIndex < recipeNorm.resources.Length && resourceIndices.ContainsKey(itemId) && returnIndex == resourceIndices[itemId])
                {
                    double output = 0;
                    // 这里不能用getoutput方法，因为也许有的第一产物不是直接的净产物，所以显然下面用的recipe也要是oriProto而不是norm里面的
                    if (isInc && incLevel >= 0 && incLevel <= Cargo.incTableMilli.Length)
                        output = changedCount * recipeNorm.oriProto.ResultCounts[0] / recipeNorm.time * (1.0 + Utils.GetIncMilli(incLevel, userPreference) + bonusInc);
                    else
                        output = changedCount * recipeNorm.oriProto.ResultCounts[0] / recipeNorm.time;
                    result -= 2 * output;
                }
            }
            if (result < 0 && changedCount >= 0)
            {
                Debug.LogWarning($"GetInputSpeedByChangedCount({itemId}, {changedCount})试图在changedCount非负时，返回负数");
                result = 0;
            }
            else if (result > 0 && changedCount < 0)
            {
                Debug.LogWarning($"GetInputSpeedByChangedCount({itemId}, {changedCount})试图在changedCount非正时，返回正数");
                result = 0;
            }

            if (result == 0)
                result = 0.00001;

            return result;
        }


        public double GetOutputSpeedByItemId(int itemId)
        {
            return GetOutputSpeedByChangedCount(itemId, count);
        }

        public double GetTotalEnergyConsumption()
        {
            if (useIA) // 星际组装厂
                return 0;

            double ratio = 1.0;
            if(incLevel >= 0 && incLevel < Cargo.powerTableRatio.Length)
            {
                ratio = Utils.GetPowerRatio(incLevel,userPreference);
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

        // 增产剂消耗，目前尚未考虑星际组装厂
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
            // 制造台配方和组装厂可以享受蓝buff
            blueBuffFlag = blueBuffFlag && (recipeNorm.type == (int)ERecipeType.Assemble || recipeNorm.type == 9 || recipeNorm.type == 10 || recipeNorm.type == 12 || useIA); 
            blueBuffFlag = blueBuffFlag && recipeNorm.resources.Length > 1 && recipeNorm.products[0] != 1803 && recipeNorm.products[0] != 6006;
            double shrinkByRelic = 0;
            if (blueBuffFlag)
            {
                // 这里不能用getoutput方法，因为也许有的第一产物不是直接的净产物，所以显然下面用的recipe也要是oriProto而不是norm里面的
                if (isInc && incLevel >= 0 && incLevel <= Cargo.incTableMilli.Length)
                    shrinkByRelic += Math.Min(recipeNorm.oriProto.ResultCounts[0] * (1.0 + Utils.GetIncMilli(incLevel, userPreference)), recipeNorm.oriProto.ItemCounts[0]);
                else
                    shrinkByRelic += Math.Min(recipeNorm.oriProto.ResultCounts[0], recipeNorm.oriProto.ItemCounts[0]);
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
                        shrinkByRelic += Math.Min(2 * recipeNorm.oriProto.ResultCounts[0] * (1.0 + Utils.GetIncMilli(incLevel, userPreference)), recipeNorm.oriProto.ItemCounts[returnIndex]); // 返还不能超过用量
                    else
                        shrinkByRelic += Math.Min(2 * recipeNorm.oriProto.ResultCounts[0], recipeNorm.oriProto.ItemCounts[returnIndex]);
                }
            }

            proliferatorCount = 1.0 * count / recipeNorm.time * (itemNeeds - shrinkByRelic) / itemsPerProliferator ;

            if (IASpecializationType == 2 && GetSpecBuffLevel() > 0)
            {
                proliferatorId = 0;
                proliferatorCount = 0;
            }
        }

        public int GetSpecBuffLevel()
        {
            if(CompatManager.MMS && IASpecializationType > 0)
            {
                int curSpecType = IASpecializationType;
                int slotSpecBuffLvl = 0;
                ERecipeType recipeType = recipeNorm.oriProto.Type;

                // 1
                if (curSpecType == 1)
                {
                    if (recipeType == ERecipeType.Smelt)
                    {
                        slotSpecBuffLvl = 1;
                    }
                }

                // 2
                if (curSpecType == 2)
                {
                    if (recipeType == ERecipeType.Chemical || recipeType == ERecipeType.Refine || (int)recipeType == 16 || recipeNorm.products.Contains(1141) || recipeNorm.products.Contains(1142) || recipeNorm.products.Contains(1143))
                    {
                        slotSpecBuffLvl = 1;
                    }
                }

                // 3
                if (curSpecType == 3)
                {
                    bool flag3 = false;
                    for (int i = 0; i < recipeNorm.resources.Length; i++)
                    {
                        if (recipeNorm.resources[i] == 1121 || recipeNorm.resources[i] == 1122)
                        {
                            flag3 = true;
                            break;
                        }
                    }
                    if (!flag3)
                    {
                        for (int i = 0; i < recipeNorm.products.Length; i++)
                        {
                            if (recipeNorm.products[i] == 1121 || recipeNorm.products[i] == 1122)
                            {
                                flag3 = true;
                                break;
                            }
                        }
                    }
                    if (flag3)
                    {
                        slotSpecBuffLvl = 1;
                    }
                }
                // 4
                if (curSpecType == 4)
                {
                    int flag4Lvl = 0;
                    for (int i = 0; i < recipeNorm.products.Length; i++)
                    {
                        if (recipeNorm.products[i] == 1303 || recipeNorm.products[i] == 1305 || recipeNorm.products[i] == 9486)
                        {
                            flag4Lvl = 2;
                            break;
                        }
                    }
                    if (flag4Lvl <= 0)
                    {
                        for (int i = 0; i < recipeNorm.resources.Length; i++)
                        {
                            if (recipeNorm.resources[i] == 1303 || recipeNorm.resources[i] == 1305 || recipeNorm.resources[i] == 9486)
                            {
                                flag4Lvl = 1;
                                break;
                            }
                        }
                    }
                    if (flag4Lvl > 0)
                    {
                        slotSpecBuffLvl = flag4Lvl;
                    }
                }

                // 5
                if (curSpecType == 5)
                {
                    int flag5Lvl = 0;
                    for (int i = 0; i < recipeNorm.products.Length; i++) // 遍历产物 是否有弹药或防御、舰队等
                    {
                        int pId = recipeNorm.products[i];
                        ItemProto pItem = LDB.items.Select(pId);
                        if (pItem.isAmmo) // 要加||isBomb? 没发现有符合的，可能后续会需要改动
                        {
                            flag5Lvl = 4;
                            break;
                        }
                        else if (pItem.Type == EItemType.Defense || pItem.Type == EItemType.Turret || pItem.isCraft) // isFighter等是冗余的？ 类似上面可能后续需要改动
                        {
                            flag5Lvl = 2; // 不要break是万一产物既有弹药又有防御，按高的算（这合理吗？）反正目前没有这种recipe无所谓啦
                        }
                    }
                    slotSpecBuffLvl = flag5Lvl;
                }

                return slotSpecBuffLvl;
            }

            return 0;
        }

        public long GetIAInputCount()
        {
            double speedFactor = 1;
            if (IASpecializationType == 1 && GetSpecBuffLevel() > 0) 
                speedFactor = 3;
            else if (IASpecializationType == 2 && GetSpecBuffLevel() > 0)
                speedFactor = 2;

            return (long)Math.Ceiling(count / recipeNorm.time * recipeNorm.oriProto.ResultCounts[0] / speedFactor);
        }
    }
}
