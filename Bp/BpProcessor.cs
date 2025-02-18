using DSPCalculator.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DSPCalculator.BP
{
    public class BpProcessor
    {
        public static bool enabled = true;

        public RecipeInfo recipeInfo;
        public bool canGenerate { get { return !recipeInfo.useIA && BpDB.assemblerInfos.ContainsKey(recipeInfo.assemblerItemId) && cargoCount <= 6; } } // 返回是否可以生成蓝图，也决定着蓝图生成按钮是否显示
        public SolutionTree solution;
        public int supportAssemblerCount; // 可支持的工厂数量
        public double bpCountToSatisfy; // 蓝图的数量
        public List<BpCargoInfo> cargoInfoDescending; // 按货物数量降序排序的货物信息
        public List<BpCargoInfo> cargoInfoOrderByNorm; // 货物信息，按照工厂内1，外1，内2（共享带），外2，外3，内3（不支持双行）排列
        //   5 如果使用了这个带子，代表cargoCount为6，则只支持单行工厂
        //   2 共享带
        //   0 这边是双行的内侧（单行的上侧）
        //  工厂
        //   1 这边是双行的外侧（单行的下侧）
        //   3
        //   4
        public bool doubleRow; // 是否双行工程蓝图
        public int cargoCount; // 共有几种货物
        public List<int> insufficientSorterItems; // 由于科技限制，单个分拣器运力不足的那些物品们
        public bool resourceGenCoater { get { return recipeInfo.incLevel > 0 && solution.userPreference.bpResourceCoater >= 0 || solution.userPreference.bpResourceCoater == 1; } } // 原材料有喷涂机
        public bool productGenCoater { get { return solution.userPreference.bpProductCoater; } } // 产物有喷涂机
        public bool genCoater { get { return resourceGenCoater || productGenCoater; } } // 有喷涂机
        public bool PLSProvideProliferator { get { return genCoater && solution.userPreference.bpStationProlifSlot; } } // PLS提供增产剂

        public Dictionary<int, Dictionary<int, int>> gridMap;
        public List<BlueprintBuilding> buildings;
        public List<int> PLSs;

        public BlueprintData blueprintData;

        public BpProcessor() 
        {
            gridMap = new Dictionary<int, Dictionary<int, int>>();
            buildings = new List<BlueprintBuilding>();
            blueprintData = BpBuilder.CreateEmpty();
            PLSs = new List<int>();
        }

        public BpProcessor(RecipeInfo recipeInfo, SolutionTree solution)
        {
            this.solution = solution;
            this.recipeInfo = recipeInfo;
            cargoCount = recipeInfo.recipeNorm.oriProto.Items.Length + recipeInfo.recipeNorm.oriProto.Results.Length;
            doubleRow = solution.userPreference.bpRowCount == 2 && cargoCount < 6; // cargoCount == 6一定不能做双行的
            insufficientSorterItems = new List<int>();
            PreProcess();
        }

        /// <summary>
        /// 提前计算一下蓝图的大致使用情况，包括传送带分配和可支持的设施数
        /// </summary>
        public void PreProcess()
        {
            //if (recipeInfo.useIA)
            //    return;
            //if (cargoCount > 6)
            //    return; // 超过六个货物的无法生成蓝图

            if (!canGenerate)
                return;

            InitCargoInfos();

            cargoInfoOrderByNorm = new List<BpCargoInfo> { null, null, null, null, null, null };
            // 然后决定谁当共享带。双行的情况下，除非只有两种货物（一进一出），否则一定共用cargoInfoOrderByNorm[2]那条。
            if (doubleRow) // 双行情况不会出现cargoCount>=6的情况，因为在初始化tryDoubleRow时已经保证了cargoCount<6才会置true
            {
                if (cargoCount >= 2)
                {
                    cargoInfoOrderByNorm[0] = cargoInfoDescending[0];
                }
                else
                {
                    Debug.LogError("出现异常，一个配方处理的货物种类小于2种");
                    return;
                }

                if (cargoCount == 2)
                {
                    // 如果只有两种货物，且一种小于另一种的一半，则采用0、2号带的方式，即中间共用
                    if (cargoInfoDescending[1].maxSupportAssemblerCount >= 2 * cargoInfoDescending[0].maxSupportAssemblerCount && cargoInfoDescending[1].maxSorterDistance >= 2)
                    {
                        cargoInfoOrderByNorm[2] = cargoInfoDescending[1];
                    }
                    else // 否则，不共用，每个都是用最贴近的单带
                    {
                        cargoInfoOrderByNorm[1] = cargoInfoDescending[1];
                    }
                }
                else if (cargoCount == 3)
                {
                    cargoInfoOrderByNorm[1] = cargoInfoDescending[1];
                    cargoInfoOrderByNorm[2] = cargoInfoDescending[2]; // 共享排序最末的那个，防止在共享带上
                }
                else if (cargoCount == 4)
                {
                    cargoInfoOrderByNorm[1] = cargoInfoDescending[1];
                    cargoInfoOrderByNorm[2] = cargoInfoDescending[3]; // 共享排序最末的那个，防止在共享带上
                    cargoInfoOrderByNorm[3] = cargoInfoDescending[2];
                }
                else if (cargoCount == 5)
                {
                    cargoInfoOrderByNorm[1] = cargoInfoDescending[1];

                    if (cargoInfoDescending[3].maxSorterDistance >= 3) // 这意味着原本按顺序只需要放置在距离2的那条货物，分拣器的可用水平可以支持其放置在距离3的带子上
                    {
                        cargoInfoOrderByNorm[2] = cargoInfoDescending[4]; // 则将最末尾速度最慢的那个货物作为共享带
                        cargoInfoOrderByNorm[3] = cargoInfoDescending[2];
                        cargoInfoOrderByNorm[4] = cargoInfoDescending[3]; // 也就是这条，支持三距离，那么放置在orderByNorm的index4上
                    }
                    else // 否则，将那个货物Descending[3]放置在距离3的带子（norm[4]）会导致因为分拣器效率不够而使得工厂跑不满，只能将按速度降序排序的index3放置在共享带上了
                    {
                        cargoInfoOrderByNorm[2] = cargoInfoDescending[3]; // 共享带正常选取
                        cargoInfoOrderByNorm[3] = cargoInfoDescending[2];
                        cargoInfoOrderByNorm[4] = cargoInfoDescending[4];
                    }
                }
                else
                {
                    Debug.LogError("不应出现的情况，确定使用双行却发现货物数量超过5");
                    return;
                }
            }
            else // 如果单行，不需要决定谁是共享带
            {
                for (int i = 0; i < cargoInfoDescending.Count; i++)
                {
                    cargoInfoOrderByNorm[i] = cargoInfoDescending[i];
                }
            }

            // 然后处理单个蓝图可以支持的工厂数
            supportAssemblerCount = int.MaxValue;
            if(doubleRow)
            {
                for (int i = 0; i < cargoInfoOrderByNorm.Count; i++)
                {
                    if (cargoInfoOrderByNorm[i] == null)
                        continue;

                    supportAssemblerCount = Math.Min(supportAssemblerCount, cargoInfoOrderByNorm[i].maxSupportAssemblerCount * (i == 2 ? 1 : 2)); // i==2是共享带，不能乘2来计算货物支持的工厂数，其他带子因为在双行蓝图中都有两条带子，所以工厂数乘2
                }
            }
            else
            {
                for (int i = 0; i < cargoInfoDescending.Count; i++)
                {
                    supportAssemblerCount = Math.Min(supportAssemblerCount, cargoInfoDescending[i].maxSupportAssemblerCount);
                }
            }

            if(supportAssemblerCount > recipeInfo.assemblerCount) // 如果支持的工厂数超出实际需求，则支持的工厂数等于实际需求
            {
                supportAssemblerCount = (int)Math.Ceiling(recipeInfo.assemblerCount);
            }

            if(supportAssemblerCount <= 1 && doubleRow)
                doubleRow = false;

            bpCountToSatisfy = recipeInfo.assemblerCount / supportAssemblerCount;
        }
        public void InitCargoInfos()
        {
            RecipeProto recipeProto = recipeInfo.recipeNorm.oriProto;
            cargoInfoDescending = new List<BpCargoInfo>();
            bool sorterMk4Available = solution.sortersAvailable.Last().grade >= 4;
            for (int i = 0; i < recipeProto.Results.Length; i++)
            {
                BpCargoInfo c = new BpCargoInfo();
                c.itemId = recipeProto.Results[i];
                c.index = i;
                c.isResource = false;
                if (sorterMk4Available)
                    c.beltSpeedRequiredPerAssembler = recipeInfo.GetOutputSpeedOriProto(i, recipeInfo.count) / recipeInfo.assemblerCount / solution.userPreference.bpSorterMk4OutputStack; // 出货如果可以用集装分拣器，则有x堆叠（取决于设置和科技）
                else
                    c.beltSpeedRequiredPerAssembler = recipeInfo.GetOutputSpeedOriProto(i, recipeInfo.count) / recipeInfo.assemblerCount;

                c.maxSupportAssemblerCount = (int)(solution.beltsAvailable.Last().speedPerMin / c.beltSpeedRequiredPerAssembler);

                if (sorterMk4Available)
                    c.maxSorterDistance = 3;
                else
                    c.maxSorterDistance = (int)(solution.sortersAvailable.Last().speedPerMin / c.beltSpeedRequiredPerAssembler);

                if (DSPCalculatorPlugin.developerMode)
                {
                    Utils.logger.LogDebug($"物品{c.itemId.GetItemName()}的单个设施速度{c.beltSpeedRequiredPerAssembler}，根据计算单带可支持最大工厂数量{c.maxSupportAssemblerCount}，当前可用的分拣器可支持最远间隔{c.maxSorterDistance}.");
                }
                cargoInfoDescending.Add(c);
            }
            for (int i = 0; i < recipeProto.Items.Length; i++)
            {
                BpCargoInfo c = new BpCargoInfo();
                c.itemId = recipeProto.Items[i];
                c.index = i;
                c.isResource = true;
                c.beltSpeedRequiredPerAssembler = recipeInfo.GetInputSpeedOriProto(i, recipeInfo.count) / recipeInfo.assemblerCount / solution.userPreference.bpStack;
                c.maxSupportAssemblerCount = (int)(solution.beltsAvailable.Last().speedPerMin / c.beltSpeedRequiredPerAssembler);
                if (solution.sortersAvailable.Last().grade >= 4)
                    c.maxSorterDistance = 3;
                else
                    c.maxSorterDistance = (int)(solution.sortersAvailable.Last().speedPerMin / c.beltSpeedRequiredPerAssembler);
                if (DSPCalculatorPlugin.developerMode)
                {
                    Utils.logger.LogDebug($"物品{c.itemId.GetItemName()}的单个设施速度{c.beltSpeedRequiredPerAssembler}，根据计算单带可支持最大工厂数量{c.maxSupportAssemblerCount}，当前可用的分拣器可支持最远间隔{c.maxSorterDistance}.");
                }
                cargoInfoDescending.Add(c);
            }
            cargoInfoDescending = cargoInfoDescending.OrderByDescending(x => x.beltSpeedRequiredPerAssembler).ToList(); // 需求量最大的放在第一格
        }
        public bool GenerateBlueprint(bool withPLS)
        {
            if (!canGenerate)
                return false;

            gridMap = new Dictionary<int, Dictionary<int, int>>();
            buildings = new List<BlueprintBuilding>();
            blueprintData = BpBuilder.CreateEmpty();
            PLSs = new List<int>();
            insufficientSorterItems = new List<int>();


            // 创建第一行工厂，工厂y为0
            int assemblerCountFirstRow = doubleRow ? (int)Math.Ceiling(supportAssemblerCount*1.0 / 2) : supportAssemblerCount; // 每行工厂的数量，如果是单行蓝图则就等于support
            int assemblerCountSecondRow = supportAssemblerCount - assemblerCountFirstRow;
            int assemblerId = recipeInfo.assemblerItemId;
            BpAssemblerInfo assemblerInfo = BpDB.assemblerInfos[assemblerId];

            List<int> assemblersFirstRow = new List<int>(); // 暂存所有的第一排工厂的index
            List<int> assemblersSecondRow = new List<int>(); // 暂存所有的第二排工厂（如果有）的index
            if (true)
            {
                int assemblerX = 0;
                int assemblerY = 0;

                for (int i = 0; i < assemblerCountFirstRow; i++)
                {
                    assemblersFirstRow.Add(this.AddAssembler(assemblerId, recipeInfo.ID, assemblerX + assemblerInfo.DragDistanceX * i, assemblerY, 0));
                }
                // 处理带子
                // ------首先处理用什么带子，以及用什么分拣器
                for (int i = 0; i < cargoInfoOrderByNorm.Count; i++)
                {
                    BpCargoInfo cargoInfo = cargoInfoOrderByNorm[i];
                    if (cargoInfo != null)
                    {
                        int assemblerCountOnThisBelt = i == 2 ? supportAssemblerCount : assemblerCountFirstRow;
                        int beltId = -1;
                        if (solution.userPreference.bpBeltHighest) // 如果用户配置为总是使用最高级
                        {
                            beltId = solution.beltsAvailable.Last().itemId;
                        }
                        else // 否则尽可能使用便宜的
                        {
                            for (int b = 0; b < solution.beltsAvailable.Count; b++)
                            {
                                if (solution.beltsAvailable[b].Satisfy(assemblerCountOnThisBelt * cargoInfo.beltSpeedRequiredPerAssembler))
                                {
                                    beltId = solution.beltsAvailable[b].itemId;
                                    break;
                                }
                            }
                        }
                        if (beltId < 0)
                        {
                            Utils.logger.LogError("没有找到满足的传送带，这种情况不应该发生");
                            return false;
                        }
                        cargoInfo.useBeltItemId = beltId;

                        int sorterId = -1;

                        // 如果用户配置为总是使用最高级，则使用最高级分拣器。或者：“集装分拣器是可用的情况下，在 出货口 一定用集装分拣器，无论用户怎么设置”（因为集装分拣器可用的话，之前会按可堆叠计算带速，而普通分拣器无法堆叠）
                        if (solution.userPreference.bpSorterHighest || (solution.sortersAvailable.Last().grade >= 4 && !cargoInfo.isResource))
                        {
                            sorterId = solution.sortersAvailable.Last().itemId;
                        }
                        else  // 否则尽可能使用便宜的
                        {
                            for (int s = 0; s < solution.sortersAvailable.Count; s++)
                            {
                                sorterId = solution.sortersAvailable[s].itemId;
                                if (solution.sortersAvailable[s].Satisfy(cargoInfo.beltSpeedRequiredPerAssembler, i/2))
                                    break;
                            }
                        }
                        if (solution.sortersAvailable.Last().speedPerMin * solution.userPreference.bpStack < cargoInfo.beltSpeedRequiredPerAssembler) // 最快的一个可用爪子（但是会受限于科技）都不能满足一个工厂的进料
                        {
                            sorterId = BpDB.sortersAscending.Last().itemId; // 直接使用集装分拣器
                            insufficientSorterItems.Add(cargoInfo.itemId); // 将这个配方记录为分拣器无法满足，需要用户自行调整（比如换成两个低级爪子）
                        }
                        cargoInfo.useSorterItemId = sorterId;
                    }
                }
                // ------然后开始创建带子
                int leftExtend = assemblerInfo.slotConnectBeltXPositions.Min() - 1; // 最左格子对应的传送带的坐标，再额外延长一格
                int rightExtend = assemblerInfo.slotConnectBeltXPositions.Max() + 1; // 最右格子对应的传送带坐标，再额外延长一格
                int reserveForCoater = BpDB.coaterBeltBackwardLen;
                if (!genCoater)
                    reserveForCoater = 0;
                int beltLeftX = leftExtend - reserveForCoater;
                int beltRightX = (assemblerCountFirstRow - 1) * assemblerInfo.DragDistanceX + rightExtend;
                for (int i = 0; i < cargoInfoOrderByNorm.Count; i++)
                {
                    BpCargoInfo cargoInfo = cargoInfoOrderByNorm[i];
                    if (cargoInfo != null)
                    {
                        int Y = GetCargoBeltY(i, assemblerInfo, true);
                        if (cargoInfo.isResource)
                        {
                            this.AddBelts(cargoInfo.useBeltItemId, beltLeftX, Y, 0, beltRightX, Y, 0, -1, cargoInfo.itemId, 0);
                        }
                        else
                        {
                            this.AddBelts(cargoInfo.useBeltItemId, beltRightX, Y, 0, beltLeftX, Y, 0, -1, 0, cargoInfo.itemId);
                        }

                        // 创建喷涂机
                        if (resourceGenCoater && cargoInfo.isResource || productGenCoater && !cargoInfo.isResource)
                        {
                            this.AddCoater(beltLeftX + BpDB.coaterOffsetX, Y);
                        }
                    }
                }
                // ------然后创建爪子
                for (int i = 0; i < assemblersFirstRow.Count; i++)
                {
                    int assemblerBuildingIndex = assemblersFirstRow[i];
                    for (int c = 0; c < cargoInfoOrderByNorm.Count; c++)
                    {
                        BpCargoInfo cargoInfo = cargoInfoOrderByNorm[c];
                        if (cargoInfo != null)
                        {
                            int slot = assemblerInfo.cargoNormIndex2SlotMap_FirstRow[c];
                            this.AssemblerConnectToBelt(assemblerBuildingIndex, slot, cargoInfo.useSorterItemId, 1 + c / 2, cargoInfo.isResource, cargoInfo.isResource ? 0 : cargoInfo.itemId, cargoInfo.isResource);
                        }
                    }
                }
            }
            if (doubleRow)
            {
                // 处理第二行工厂
                int assemblerX2 = 0;
                int assemblerY2 = GetAssemblerY(false, assemblerInfo);

                for (int i = 0; i < assemblerCountSecondRow; i++)
                {
                    assemblersSecondRow.Add(this.AddAssembler(assemblerId, recipeInfo.ID, assemblerX2 + assemblerInfo.DragDistanceX * i, assemblerY2, 0));
                }
                // 不再处理cargoInfo，而是直接创建带子
                // ------开始创建带子
                int leftExtend = assemblerInfo.slotConnectBeltXPositions.Min() - 1; // 最左格子对应的传送带的坐标，再额外延长一格
                int rightExtend = assemblerInfo.slotConnectBeltXPositions.Max() + 1; // 最右格子对应的传送带坐标，再额外延长一格
                int reserveForCoater = BpDB.coaterBeltBackwardLen;
                if (!genCoater)
                    reserveForCoater = 0;
                int beltLeftX = leftExtend - reserveForCoater;
                int beltRightX = (assemblerCountFirstRow - 1) * assemblerInfo.DragDistanceX + rightExtend;
                for (int i = 0; i < cargoInfoOrderByNorm.Count; i++)
                {
                    if (i == 2) // 共享带子不能重复创建
                        continue;
                    BpCargoInfo cargoInfo = cargoInfoOrderByNorm[i];
                    if (cargoInfo != null)
                    {
                        int Y = GetCargoBeltY(i, assemblerInfo, false); // 这里是第二行工厂的袋子，要false
                        if (cargoInfo.isResource)
                        {
                            this.AddBelts(cargoInfo.useBeltItemId, beltLeftX, Y, 0, beltRightX, Y, 0, -1, cargoInfo.itemId, 0);
                        }
                        else
                        {
                            this.AddBelts(cargoInfo.useBeltItemId, beltRightX, Y, 0, beltLeftX, Y, 0, -1, 0, cargoInfo.itemId);
                        }

                        // 创建喷涂机
                        if (resourceGenCoater && cargoInfo.isResource || productGenCoater && !cargoInfo.isResource)
                        {
                            this.AddCoater(beltLeftX + BpDB.coaterOffsetX, Y);
                        }
                    }
                } 
                // ------然后创建爪子
                for (int i = 0; i < assemblersSecondRow.Count; i++)
                {
                    int assemblerBuildingIndex = assemblersSecondRow[i];
                    for (int c = 0; c < cargoInfoOrderByNorm.Count; c++)
                    {
                        BpCargoInfo cargoInfo = cargoInfoOrderByNorm[c];
                        if (cargoInfo != null)
                        {
                            int slot = assemblerInfo.cargoNormIndex2SlotMap_SecondRow[c];
                            this.AssemblerConnectToBelt(assemblerBuildingIndex, slot, cargoInfo.useSorterItemId, 1 + c / 2, cargoInfo.isResource, cargoInfo.isResource ? 0 : cargoInfo.itemId, cargoInfo.isResource);
                        }
                    }
                }
            }

            PostProcess();
            return true;
        }

        

        public void PostProcess()
        {
            if (recipeInfo.useIA)
                return;

            float minX = 0;
            float minY = 0;
            float minZ = 0;
            float maxX = 0;
            float maxY = 0;
            float maxZ = 0;
            if (buildings.Count > 0)
            {
                minX = Math.Min(buildings[0].localOffset_x, buildings[0].localOffset_x2);
                maxX = Math.Max(buildings[0].localOffset_x, buildings[0].localOffset_x2);
                minY = Math.Min(buildings[0].localOffset_y, buildings[0].localOffset_y2);
                maxY = Math.Max(buildings[0].localOffset_y, buildings[0].localOffset_y2);
                minZ = Math.Min(buildings[0].localOffset_z, buildings[0].localOffset_z2);
                maxZ = Math.Max(buildings[0].localOffset_z, buildings[0].localOffset_z2);
            }
            for (int i = 1; i < buildings.Count; i++)
            {
                float minXCur = Math.Min(buildings[i].localOffset_x, buildings[i].localOffset_x2);
                float maxXCur = Math.Max(buildings[i].localOffset_x, buildings[i].localOffset_x2);
                float minYCur = Math.Min(buildings[i].localOffset_y, buildings[i].localOffset_y2);
                float maxYCur = Math.Max(buildings[i].localOffset_y, buildings[i].localOffset_y2);
                float minZCur = Math.Min(buildings[i].localOffset_z, buildings[i].localOffset_z2);
                float maxZCur = Math.Max(buildings[i].localOffset_z, buildings[i].localOffset_z2);
                minX = Math.Min(minXCur, minX);
                maxX = Math.Max(maxXCur, maxX);
                minY = Math.Min(minYCur, minY);
                maxY = Math.Max(maxYCur, maxY);
                minZ = Math.Min(minZCur, minZ);
                maxZ = Math.Max(maxZCur, maxZ);
            }

            for (int i = 0; i < buildings.Count; i++)
            {
                buildings[i].localOffset_x -= minX;
                buildings[i].localOffset_x2 -= minX;
                buildings[i].localOffset_y -= minY;
                buildings[i].localOffset_y2 -= minY;
            }

            blueprintData.dragBoxSize_x = (int)Math.Ceiling(maxX - minX);
            blueprintData.dragBoxSize_y = (int)Math.Ceiling(maxY - minY);
            blueprintData.areas[0].width = blueprintData.dragBoxSize_x;
            blueprintData.areas[0].height = blueprintData.dragBoxSize_y;
            blueprintData.buildings = buildings.ToArray();
        }

        public int GetAssemblerY(bool isFirstAssemblerRow, BpAssemblerInfo assemblerInfo)
        {
            if (isFirstAssemblerRow)
                return 0;

            int y = assemblerInfo.centerDistanceTop + assemblerInfo.centerDistanceBottom + 1;
            if (cargoInfoOrderByNorm[2] != null) // 中间多插了一条共享带
                y++;
            return y;
        }

        public int GetCargoBeltY(int normIndex, BpAssemblerInfo assemblerInfo, bool isFirstAssemblerRow)
        {
            int centerY = GetAssemblerY(isFirstAssemblerRow, assemblerInfo);

            if (isFirstAssemblerRow)
            {
                switch (normIndex)
                {
                    case 0:
                        return centerY + assemblerInfo.centerDistanceTop;
                    case 1:
                        return centerY - assemblerInfo.centerDistanceBottom;
                    case 2:
                        return centerY + assemblerInfo.centerDistanceTop + 1;
                    case 3:
                        return centerY - assemblerInfo.centerDistanceBottom - 1;
                    case 4:
                        return centerY - assemblerInfo.centerDistanceBottom - 2;
                    case 5:
                        return centerY + assemblerInfo.centerDistanceTop + 2;
                    default:
                        break;
                }
            }
            else
            {
                switch (normIndex)
                {
                    case 0:
                        return centerY - assemblerInfo.centerDistanceBottom;
                    case 1:
                        return centerY + assemblerInfo.centerDistanceTop;
                    case 2:
                        return centerY - assemblerInfo.centerDistanceBottom - 1; 
                    case 3:
                        return centerY + assemblerInfo.centerDistanceTop + 1;
                    case 4:
                        return centerY + assemblerInfo.centerDistanceTop + 2;
                    default:
                        break;
                }
            }
            Debug.LogError("cargoCount数量不符。");
            return 0;
        }
    }

    public class BpCargoInfo
    {
        public int itemId;
        public int index;
        public bool isResource; // 是配方的输入材料true，是配方的产物false
        public double beltSpeedRequiredPerAssembler; // 每个设施每分钟的需求的带速，对于进料要将物品的速度除以stack，对于出料则取决于能否用集装分拣器，能用则除以4，否则不除。如果集装分拣器可用，无论怎么设置，出货口一定用集装分拣器
        public int maxSupportAssemblerCount; // 最高级可用传送带，单带可支持的工厂数量
        public int maxSorterDistance; // 最高级可用分拣器的最远距离（可以支持设施不间断运行）
        // 以下在初次实例化时并未赋值，创建蓝图时才会赋值
        public int useBeltItemId; // 使用的传送带ItemId
        public int useSorterItemId; // 使用的分拣器的ItemId
    }

    //public class BpCargoBeltInfo
    //{
    //    public int y; // 主线路相对于最下面的生产设置中心的y坐标（0）的y偏移
    //    public BpCargoInfo cargoInfo; // 承载的物品，如果是null则为生产设施
    //}

    //public struct BpPoint
    //{
    //    public int x;
    //    public int y;
    //}
}
