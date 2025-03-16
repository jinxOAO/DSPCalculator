using DSPCalculator.Bp;
using DSPCalculator.Compatibility;
using DSPCalculator.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DSPCalculator.Bp
{
    public class BpProcessor
    {
        public static bool enabled = true;

        public RecipeInfo recipeInfo;
        public bool prePocessed;
        public bool canGenerate { get { return !recipeInfo.useIA && ((BpDB.assemblerInfos.ContainsKey(recipeInfo.assemblerItemId) && cargoCount <= 6) || isGBMega || bpPrefabId > 0); } }// 返回是否可以生成蓝图，也决定着蓝图生成按钮是否显示
        public bool isGBMega { get { return CompatManager.GB && BpDB.GBMegas.ContainsKey(recipeInfo.assemblerItemId) && cargoCount <= 6; } }
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
        public List<BpCargoBeltPos> cargoBeltPoses; // 货物带的端点信息，用于后续连接物流塔
        //  8 ---------------                  
        //  7 ---------------                  8 不存在这条带子
        //  6 ---------------                  7 不存在这条带子
        //         工厂                        6 ---------------
        //  5 ---------------                         工厂     
        //  4 ---------------        或        4 ---------------                    
        //  3 ---------------                  3 ---------------
        //         工厂                               工厂     
        //  2 ---------------                  2 ---------------
        //  1 ---------------                  1 不存在这条带子
        //  0 ---------------                  0 不存在这条带子
        public bool doubleRow; // 是否双行工程蓝图
        public int cargoCount; // 共有几种货物
        public bool share3Belts; // 是否可以共享三条带子
        public int bpPrefabId; // 预制
        public List<int> insufficientSorterItems; // 由于科技限制，单个分拣器运力不足的那些物品们
        public bool resourceGenCoater { get { return recipeInfo.incLevel > 0 && solution.userPreference.bpResourceCoater >= 0 || solution.userPreference.bpResourceCoater == 1; } } // 原材料有喷涂机
        public bool productGenCoater { get { return solution.userPreference.bpProductCoater; } } // 产物有喷涂机
        public bool genCoater { get { return resourceGenCoater || productGenCoater; } } // 有喷涂机
        public bool PLSProvideProliferator { get { return genCoater && solution.userPreference.bpStationProlifSlot; } } // PLS提供增产剂

        public Dictionary<int, Dictionary<int, int>> gridMap;
        public List<BlueprintBuilding> buildings;
        public List<int> PLSs;

        public BlueprintData blueprintData;

        public BpProcessorGB processorGB;

        public Dictionary<int, BlueprintBuilding> inputBelts; // 蓝图的某种物料从哪个带子输入
        public Dictionary<int, BlueprintBuilding> outputBelts; // 蓝图的某种物料从哪个带子输出

        public int width { get { return blueprintData != null ? blueprintData.areas[0].width : 0; } }
        public int height { get { return blueprintData != null ? blueprintData.areas[0].height : 0; } }
        //public BpProcessor() 
        //{
        //    gridMap = new Dictionary<int, Dictionary<int, int>>();
        //    buildings = new List<BlueprintBuilding>();
        //    blueprintData = BpBuilder.CreateEmpty();
        //    PLSs = new List<int>();
        //    share3Belts = false;
        //    bpPrefabId = 0;
        //}

        public BpProcessor(RecipeInfo recipeInfo, SolutionTree solution)
        {
            this.solution = solution;
            this.recipeInfo = recipeInfo;
            cargoCount = recipeInfo.recipeNorm.oriProto.Items.Length + recipeInfo.recipeNorm.oriProto.Results.Length;
            doubleRow = solution.userPreference.bpRowCount == 2 && cargoCount < 6; // cargoCount == 6一定不能做双行的
            insufficientSorterItems = new List<int>();
            share3Belts = false;
            bpPrefabId = 0;
            prePocessed = false;
            // PreProcess();
        }

        public BpProcessor(RecipeInfo recipeInfo, SolutionTree solution, int forceRowCount)
        {
            this.solution = solution;
            this.recipeInfo = recipeInfo;
            cargoCount = recipeInfo.recipeNorm.oriProto.Items.Length + recipeInfo.recipeNorm.oriProto.Results.Length;
            doubleRow = forceRowCount == 2 && cargoCount < 6;
            insufficientSorterItems = new List<int>();
            share3Belts = false;
            bpPrefabId = 0;
            prePocessed = false;
            // PreProcess();
        }

        /// <summary>
        /// 提前计算一下蓝图的大致使用情况，包括传送带分配和可支持的设施数
        /// </summary>
        public void PreProcess()
        {
            if(prePocessed)
                return;

            prePocessed = true;

            if (isVanilla6006())
                bpPrefabId = 1; // 标记为预支蓝图 6006

            if (!canGenerate || bpPrefabId > 0)
                return;

            if(isGBMega)
            {
                processorGB = new BpProcessorGB(this);
                processorGB.PreProcess();
                return;
            }

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
                    // 三带共享优先级判断
                    if (cargoInfoDescending[0].maxSorterDistance >= 2 && cargoInfoDescending[1].maxSorterDistance >= 2)
                    {
                        share3Belts = true;
                        cargoInfoOrderByNorm[2] = cargoInfoDescending[1];
                    }
                    else
                    {
                        share3Belts = false;
                        // 如果只有两种货物，且一种小于另一种的一半，则采用0、2号带的方式，即中间共用
                        if (cargoInfoDescending[1].maxSupportAssemblerCount >= 2 * cargoInfoDescending[0].maxSupportAssemblerCount && cargoInfoDescending[1].maxSorterDistance >= 2)
                        {
                            cargoInfoOrderByNorm[2] = cargoInfoDescending[1];
                        }
                        else if (cargoInfoDescending[1].maxSupportAssemblerCount >= recipeInfo.assemblerCount && solution.sortersAvailable.Last().grade >= 4) // 或者如果分拣器速度无限制，并且单条带能支持整个全部的产线，也可以共享
                        {
                            cargoInfoOrderByNorm[2] = cargoInfoDescending[1];
                        }
                        else // 否则，不共用，每个都是用最贴近的单带
                        {
                            cargoInfoOrderByNorm[1] = cargoInfoDescending[1];
                        }

                    }

                }
                else if (cargoCount == 3)
                {
                    // 三带共享要求分拣器可以支持2个三距离和一个2距离
                    if (cargoInfoDescending[0].maxSorterDistance >= 2 && cargoInfoDescending[1].maxSorterDistance >= 3 && cargoInfoDescending[2].maxSorterDistance >= 3 && BpDB.assemblerInfos[recipeInfo.assemblerItemId].DragDistanceX >= 4) // 最后一个是能保证工厂在两行错开时，可以共享三条带而分拣器不碰撞。主要针对的是熔炉（钛合金产线），如果密铺共享三条带是会碰撞的
                    {
                        share3Belts = true;
                        cargoInfoOrderByNorm[0] = cargoInfoDescending[1]; // 1和3距离，可用分拣器必须满足最远的3距离
                        cargoInfoOrderByNorm[2] = cargoInfoDescending[0]; // 中间2距离
                        cargoInfoOrderByNorm[5] = cargoInfoDescending[2]; // 1和3距离，可用分拣器必须满足最远的3距离
                    }
                    else
                    {
                        share3Belts = false;
                        cargoInfoOrderByNorm[1] = cargoInfoDescending[1];
                        cargoInfoOrderByNorm[2] = cargoInfoDescending[2]; // 共享排序最末的那个，防止在共享带上
                    }
                }
                else if (cargoCount == 4)
                {
                    // 三带共享
                    if (cargoInfoDescending[1].maxSorterDistance >= 2 && cargoInfoDescending[2].maxSorterDistance >= 3 && cargoInfoDescending[3].maxSorterDistance >= 3 && BpDB.assemblerInfos[recipeInfo.assemblerItemId].DragDistanceX >= 4)
                    {
                        share3Belts = true;
                        cargoInfoOrderByNorm[0] = cargoInfoDescending[2]; // 1和3距离，可用分拣器必须满足最远的3距离
                        cargoInfoOrderByNorm[2] = cargoInfoDescending[1]; // 中间2距离
                        cargoInfoOrderByNorm[5] = cargoInfoDescending[3]; // 1和3距离，可用分拣器必须满足最远的3距离
                        cargoInfoOrderByNorm[1] = cargoInfoDescending[0]; // 1距离，两行都有各自的
                    }
                    else
                    {
                        share3Belts = false;
                        cargoInfoOrderByNorm[1] = cargoInfoDescending[1];
                        cargoInfoOrderByNorm[2] = cargoInfoDescending[3]; // 共享排序最末的那个，防止在共享带上
                        cargoInfoOrderByNorm[3] = cargoInfoDescending[2];
                    }
                }
                else if (cargoCount == 5)
                {
                    if (cargoInfoDescending[1].maxSorterDistance >= 2 && cargoInfoDescending[2].maxSorterDistance >= 2 && cargoInfoDescending[3].maxSorterDistance >= 3 && cargoInfoDescending[4].maxSorterDistance >= 3 && BpDB.assemblerInfos[recipeInfo.assemblerItemId].DragDistanceX >= 4)
                    {
                        share3Belts = true;
                        cargoInfoOrderByNorm[1] = cargoInfoDescending[0];
                        cargoInfoOrderByNorm[3] = cargoInfoDescending[1];
                        cargoInfoOrderByNorm[2] = cargoInfoDescending[2];
                        cargoInfoOrderByNorm[0] = cargoInfoDescending[3];
                        cargoInfoOrderByNorm[5] = cargoInfoDescending[4];
                    }
                    else
                    {
                        share3Belts = false;
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
            if (doubleRow)
            {
                for (int i = 0; i < cargoInfoOrderByNorm.Count; i++)
                {
                    if (cargoInfoOrderByNorm[i] == null)
                        continue;
                    bool wontDoubleBecauseShared = i == 2 || ((i == 0 || i == 5) && share3Belts);
                    // 共享带，不能乘2来计算货物支持的工厂数，其他带子因为在双行蓝图中都有两条带子，所以工厂数乘2
                    supportAssemblerCount = (int)Math.Min(supportAssemblerCount, cargoInfoOrderByNorm[i].maxSupportAssemblerCount * (wontDoubleBecauseShared ? 1 : 2));
                    if (supportAssemblerCount < 0)
                        supportAssemblerCount = int.MaxValue;
                }

            }
            else
            {
                for (int i = 0; i < cargoInfoDescending.Count; i++)
                {
                    supportAssemblerCount = (int)Math.Min(supportAssemblerCount, cargoInfoDescending[i].maxSupportAssemblerCount);
                    if (supportAssemblerCount < 0)
                        supportAssemblerCount = int.MaxValue;
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

                c.maxSupportAssemblerCount = (long)(solution.beltsAvailable.Last().speedPerMin / c.beltSpeedRequiredPerAssembler);
                

                if (sorterMk4Available)
                    c.maxSorterDistance = 3;
                else
                    c.maxSorterDistance = (int)(solution.sortersAvailable.Last().speedPerMin / c.beltSpeedRequiredPerAssembler);

                if (DSPCalculatorPlugin.developerMode)
                {
                    Utils.logger.LogInfo($"item{c.itemId.GetItemName()} spd per factory {c.beltSpeedRequiredPerAssembler} maxC{c.maxSupportAssemblerCount} sorter max distance{c.maxSorterDistance}.");
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
                c.maxSupportAssemblerCount = (long)(solution.beltsAvailable.Last().speedPerMin / c.beltSpeedRequiredPerAssembler);
                if (solution.sortersAvailable.Last().grade >= 4)
                    c.maxSorterDistance = 3;
                else
                    c.maxSorterDistance = (int)(solution.sortersAvailable.Last().speedPerMin / c.beltSpeedRequiredPerAssembler);
                if (DSPCalculatorPlugin.developerMode)
                {
                    Utils.logger.LogInfo($"item{c.itemId.GetItemName()} spd per factory{c.beltSpeedRequiredPerAssembler} maxC{c.maxSupportAssemblerCount} sorter max distance{c.maxSorterDistance}.");
                }
                cargoInfoDescending.Add(c);
            }
            cargoInfoDescending = cargoInfoDescending.OrderByDescending(x => x.beltSpeedRequiredPerAssembler).ToList(); // 需求量最大的放在第一格

        }
        public bool GenerateBlueprint(int genLevel)
        {
            if(bpPrefabId == 1)
            {
                this.blueprintData = BpBuilder.CreateEmpty();
                if (this.blueprintData.FromBase64String(BpPrefabs.universeMatrix) == BlueprintDataIOError.OK)
                {
                    if(recipeInfo.assemblerItemId == 2901)
                    {
                        short modelIndex = (short)LDB.items.Select(2901).prefabDesc.modelIndex;
                        int len = blueprintData.buildings.Length;
                        for (int i = 0; i < len; i++)
                        {
                            if(blueprintData.buildings[i].itemId == 2902)
                            {
                                blueprintData.buildings[i].itemId = 2901;
                                blueprintData.buildings[i].modelIndex = modelIndex;
                            }
                        }
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }

            if (!canGenerate || bpPrefabId > 0)
                return false;


            gridMap = new Dictionary<int, Dictionary<int, int>>();
            buildings = new List<BlueprintBuilding>();
            cargoBeltPoses = new List<BpCargoBeltPos> { null, null, null, null, null, null, null, null, null };
            blueprintData = BpBuilder.CreateEmpty();
            insufficientSorterItems = new List<int>();
            inputBelts = new Dictionary<int, BlueprintBuilding>();
            outputBelts = new Dictionary<int, BlueprintBuilding>();
            if (isGBMega)
            {
                if (processorGB != null)
                    return processorGB.GenerateBlueprints(genLevel);

                return false;
            }

            bool isLab = LDB.items.Select(recipeInfo.assemblerItemId).prefabDesc.isLab;//recipeInfo.recipeNorm.oriProto.Type == ERecipeType.Research;
            int maxLevel = solution.userPreference.labMaxLevel;
            if(maxLevel > supportAssemblerCount)
                maxLevel = supportAssemblerCount;
            int groupCount = (int)Math.Ceiling(supportAssemblerCount * 1.0 / maxLevel);
            if (!isLab)
                maxLevel = 1;
            // 创建第一行工厂，第一行工厂y为0
            int assemblerCountFirstRow = doubleRow ? (int)Math.Ceiling(supportAssemblerCount*1.0 / 2) : supportAssemblerCount; // 每行工厂的数量，如果是单行蓝图则就等于support
            int assemblerCountSecondRow = supportAssemblerCount - assemblerCountFirstRow;
            if (isLab)
            {
                assemblerCountFirstRow = doubleRow ? (int)Math.Ceiling(groupCount * 1.0 / 2) : groupCount;
                assemblerCountSecondRow = groupCount - assemblerCountFirstRow;
            }
            int assemblerId = recipeInfo.assemblerItemId;
            BpAssemblerInfo assemblerInfo = BpDB.assemblerInfos[assemblerId];

            List<int> assemblersFirstRow = new List<int>(); // 暂存所有的第一排工厂的index
            List<int> assemblersSecondRow = new List<int>(); // 暂存所有的第二排工厂（如果有）的index
            if (true)
            {
                int assemblerX = 0;
                int assemblerY = 0;
                if (!isLab)
                {
                    for (int i = 0; i < assemblerCountFirstRow; i++)
                    {
                        assemblersFirstRow.Add(this.AddAssembler(assemblerId, recipeInfo.ID, assemblerX + assemblerInfo.DragDistanceX * i, assemblerY, 0, assemblerInfo.defaultYaw, recipeInfo.isInc));
                    }
                }
                else
                {
                    for (int i = 0; i < assemblerCountFirstRow; i++)
                    {
                        if (i != assemblerCountFirstRow - 1)
                            assemblersFirstRow.Add(this.AddLab(assemblerId, recipeInfo.ID, assemblerX + assemblerInfo.DragDistanceX * i, assemblerY, maxLevel, recipeInfo.isInc));
                        else
                            assemblersFirstRow.Add(this.AddLab(assemblerId, recipeInfo.ID, assemblerX + assemblerInfo.DragDistanceX * i, assemblerY, supportAssemblerCount - (groupCount - 1) * maxLevel, recipeInfo.isInc));
                    }
                }
                // 处理带子
                // ------首先处理用什么带子，以及用什么分拣器
                for (int i = 0; i < cargoInfoOrderByNorm.Count; i++)
                {
                    BpCargoInfo cargoInfo = cargoInfoOrderByNorm[i];
                    if (cargoInfo != null)
                    {
                        bool isSharedBelt = i == 2 || ((i == 0 || i == 5) && share3Belts);
                        int assemblerCountOnThisBelt = isSharedBelt ? supportAssemblerCount : assemblerCountFirstRow;
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
                                if (solution.sortersAvailable[s].Satisfy(cargoInfo.beltSpeedRequiredPerAssembler * (isLab ? maxLevel : 1), i / 2 + 1))
                                {
                                    sorterId = solution.sortersAvailable[s].itemId;
                                    break;
                                }
                            }
                        }
                        if (sorterId < 0) // 最快的一个可用爪子（但是会受限于科技）都不能满足一个工厂的进料
                        {
                            sorterId = BpDB.sortersAscending.Last().itemId; // 直接使用集装分拣器
                            insufficientSorterItems.Add(cargoInfo.itemId); // 将这个配方记录为分拣器无法满足，需要用户自行调整（比如换成两个低级爪子）
                        }
                        cargoInfo.useSorterItemId = sorterId;
                    }
                }
                if (genLevel >= 0)
                {
                    // ------然后开始创建带子
                    int beltLeftX = GetBeltLeftX(assemblerInfo);

                    int rightExtend = assemblerInfo.slotConnectBeltXPositions.Max() + 1; // 最右格子对应的传送带坐标，再额外延长一格
                    int beltRightX = (assemblerCountFirstRow - 1) * assemblerInfo.DragDistanceX + rightExtend;
                    for (int i = 0; i < cargoInfoOrderByNorm.Count; i++)
                    {
                        BpCargoInfo cargoInfo = cargoInfoOrderByNorm[i];
                        if (cargoInfo != null)
                        {
                            int Y = GetCargoBeltY(i, assemblerInfo, true);
                            if (cargoInfo.isResource)
                            {
                                this.AddBelts(cargoInfo.useBeltItemId, beltLeftX, Y, 0, beltRightX, Y, 0, -1, -1, cargoInfo.itemId, 0);
                                SetInputBelt(cargoInfo.itemId, beltLeftX, Y);
                                SetOutputBelt(cargoInfo.itemId, beltRightX, Y);
                            }
                            else
                            {
                                this.AddBelts(cargoInfo.useBeltItemId, beltRightX, Y, 0, beltLeftX, Y, 0, -1, -1, 0, cargoInfo.itemId);
                                SetInputBelt(cargoInfo.itemId, beltRightX, Y);
                                SetOutputBelt(cargoInfo.itemId, beltLeftX, Y);
                            }

                            // 将带子端点信息记录
                            cargoBeltPoses[BpDB.cargoInfoNormIndexToBeltPosIndexMap_FirstRow[i]] = new BpCargoBeltPos(cargoInfo, beltLeftX, Y);

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
            }
            if (doubleRow)
            {
                // 处理第二行工厂
                int assemblerX2 = 1; // 错开一格
                int assemblerY2 = GetAssemblerY(false, assemblerInfo);
                if (!isLab)
                {
                    for (int i = 0; i < assemblerCountSecondRow; i++)
                    {
                        assemblersSecondRow.Add(this.AddAssembler(assemblerId, recipeInfo.ID, assemblerX2 + assemblerInfo.DragDistanceX * i, assemblerY2, 0, assemblerInfo.defaultYaw, recipeInfo.isInc));
                    }
                }
                else
                {
                    for (int i = 0; i < assemblerCountSecondRow; i++)
                    {
                        assemblersSecondRow.Add(this.AddLab(assemblerId, recipeInfo.ID, assemblerX2 + assemblerInfo.DragDistanceX * i, assemblerY2, maxLevel, recipeInfo.isInc));
                    }
                }

                if (genLevel >= 0)
                {
                    // 不再处理cargoInfo，而是直接创建带子
                    // ------开始创建带子

                    int beltLeftX = GetBeltLeftX(assemblerInfo);
                    int rightExtend = assemblerInfo.slotConnectBeltXPositions.Max() + 1; // 最右格子对应的传送带坐标，再额外延长一格
                    int beltRightX = (assemblerCountSecondRow - 1) * assemblerInfo.DragDistanceX + rightExtend; // +1是因为第二行会右移一格
                    for (int i = 0; i < cargoInfoOrderByNorm.Count; i++)
                    {
                        if (i == 2) // 共享带子不能重复创建
                            continue;
                        if (share3Belts && (i == 0 || i == 5))
                            continue;
                        BpCargoInfo cargoInfo = cargoInfoOrderByNorm[i];
                        if (cargoInfo != null)
                        {
                            int Y = GetCargoBeltY(i, assemblerInfo, false); // 这里是第二行工厂的袋子，要false
                            if (cargoInfo.isResource)
                            {
                                this.AddBelts(cargoInfo.useBeltItemId, beltLeftX, Y, 0, beltRightX, Y, 0, -1, -1, cargoInfo.itemId, 0);
                            }
                            else
                            {
                                this.AddBelts(cargoInfo.useBeltItemId, beltRightX, Y, 0, beltLeftX, Y, 0, -1, -1, 0, cargoInfo.itemId);
                            }
                            if (i == 1 && Y < 8 && genLevel > 0) // 说明那条带子不够高，不能直接横着拉到PLS的对应Slot正上方，所以要接到那个格子。只有第二行工厂的i==1的那条有可能不够高
                            {
                                if (cargoInfo.isResource)
                                {
                                    this.AddBelts(cargoInfo.useBeltItemId, beltLeftX, 8.1f, 0, beltLeftX, Y + 1, 0, -1, gridMap.GetBuilding(beltLeftX, Y), 0, 0);
                                }
                                else
                                {
                                    this.AddBelts(cargoInfo.useBeltItemId, beltLeftX, Y + 1.1f, 0, beltLeftX, 8, 0, gridMap.GetBuilding(beltLeftX, Y), -1, 0, 0);
                                }
                                Y = 8;
                            }

                            // 将带子端点信息记录
                            if (i == 0 && cargoInfoOrderByNorm[2] == null)// 说明中间不是三带，则第二行工厂的0号新带位置应该是4而非5（5来自于BpDB.cargoInfoNormIndexToBeltPosIndexMap_SecondRow[i]）
                                cargoBeltPoses[4] = new BpCargoBeltPos(cargoInfo, beltLeftX, Y);
                            else if (i != 2)
                                cargoBeltPoses[BpDB.cargoInfoNormIndexToBeltPosIndexMap_SecondRow[i]] = new BpCargoBeltPos(cargoInfo, beltLeftX, Y);
                            

                            // 创建喷涂机
                            if (resourceGenCoater && cargoInfo.isResource || productGenCoater && !cargoInfo.isResource)
                            {
                                this.AddCoater(beltLeftX + BpDB.coaterOffsetX, Y);
                            }
                        }
                    }
                    // ------然后创建爪子，如果是3共享，必须专门判断每一个爪子id、filter和位置
                    for (int i = 0; i < assemblersSecondRow.Count; i++)
                    {
                        int assemblerBuildingIndex = assemblersSecondRow[i];
                        for (int c = 0; c < cargoInfoOrderByNorm.Count; c++)
                        {
                            BpCargoInfo cargoInfo = cargoInfoOrderByNorm[c];
                            if (cargoInfo != null)
                            {
                                bool isResource = cargoInfo.isResource;
                                int cargoItemId = cargoInfo.itemId;
                                int sorterId = cargoInfo.useSorterItemId;
                                int mappedCargoIndex = c; // 如果3带共享，具体是什么cargo需要映射过去
                                if(share3Belts) // 3共享的带子，不能直接读cargoInfoOrderByNorm的属性，因为和第一行不一样，是倒着的
                                {
                                    if(c == 0)
                                    {
                                        if (cargoInfoOrderByNorm[5] != null)
                                        {
                                            mappedCargoIndex = 5;
                                        }
                                        else if (cargoInfoOrderByNorm[2] != null)
                                        {
                                            mappedCargoIndex = 2;
                                        }
                                    }
                                    else if (c == 2 && cargoInfoOrderByNorm[5] == null) // 这种特殊情况是只有两条带子共享
                                    {
                                        mappedCargoIndex = 0;
                                    }
                                    else if (c == 5)
                                    {
                                        mappedCargoIndex = 0;
                                    }
                                    isResource = cargoInfoOrderByNorm[mappedCargoIndex].isResource;
                                    cargoItemId = cargoInfoOrderByNorm[mappedCargoIndex].itemId;

                                    // 要独立计算分拣器使用！，既不能用
                                    if (solution.userPreference.bpSorterHighest || (solution.sortersAvailable.Last().grade >= 4 && !isResource))
                                    {
                                        sorterId = solution.sortersAvailable.Last().itemId;
                                    }
                                    else  // 否则尽可能使用便宜的
                                    {
                                        int distance = c / 2 + 1;
                                        for (int s = 0; s < solution.sortersAvailable.Count; s++)
                                        {
                                            if (solution.sortersAvailable[s].Satisfy(cargoInfoOrderByNorm[mappedCargoIndex].beltSpeedRequiredPerAssembler * (isLab ? maxLevel : 1), distance))
                                            {
                                                sorterId = solution.sortersAvailable[s].itemId;
                                                break;
                                            }
                                        }
                                    }
                                    if (sorterId < 0) // 最快的一个可用爪子（但是会受限于科技）都不能满足一个工厂的进料
                                    {
                                        sorterId = BpDB.sortersAscending.Last().itemId; // 直接使用集装分拣器
                                        if(!insufficientSorterItems.Contains(cargoItemId))
                                            insufficientSorterItems.Add(cargoItemId); // 将这个配方记录为分拣器无法满足，需要用户自行调整（比如换成两个低级爪子）
                                    }
                                }
                                int slot = assemblerInfo.cargoNormIndex2SlotMap_SecondRow[c];
                                if(share3Belts && onlyShare2Belts) // 只共享了两条袋子的话，用三条带子的slot分配会冲突，要全部右移一格
                                {
                                    if(c == 0)
                                        slot = assemblerInfo.cargoNormIndex2SlotMap_SecondRow[2];
                                    else if (c == 2)
                                        slot = assemblerInfo.cargoNormIndex2SlotMap_SecondRow[5];
                                }
                                this.AssemblerConnectToBelt(assemblerBuildingIndex, slot, sorterId, 1 + c / 2, isResource, isResource ? 0 : cargoItemId, isResource);
                            }
                        }
                    }
                }
            }
            if (genLevel > 0)
                GenerateAndConnectPLS();
            if (insufficientSorterItems.Count > 0)
                UIRealtimeTip.Popup("分拣器科技不足警告".Translate());
            PostProcess();
            return true;
        }

        public void GenerateAndConnectPLS()
        {
            PLSs = new List<int>();
            // 首先生成PLS
            int totalItemCount = cargoCount; // 所有物品数量决定要几个pls，超过4个就要俩pls
            if (PLSProvideProliferator && genCoater)
                totalItemCount++;
            int assemblerId = recipeInfo.assemblerItemId;
            BpAssemblerInfo assemblerInfo = BpDB.assemblerInfos[assemblerId];
            int x1;
            int y1;
            GetPLSPos(0, assemblerInfo, out x1, out y1);
            this.AddPLS(x1, y1);

            int x2;
            int y2;
            GetPLSPos(1, assemblerInfo, out x2, out y2);
            if (totalItemCount > BpDB.PLSMaxStorageKinds)
            {
                this.AddPLS(x2, y2);
            }

            
            List<int> corresSlotX = new List<int> { x2 + 1, x1, x1 + 1, 0, 0, 0, x1 + 1, x1, x2 + 1 };
            List<int> corresPLSListIndex = new List<int> { 1, 0, 0, 0, 0, 0, 0, 0, 1 };
            List<int> beltPosesIndexToPLSSlotMap = new List<int> { 8, 7, 8, 9, 10, 11, 0, 1, 0 }; // cargoBeltPos会固定去连接物流塔的slot
            if (cargoCount == 6) // 只能单行，这种情况下slot和线路对应情况有些不一样
            {
                corresSlotX[0] = x2;
                corresSlotX[1] = x2 + 1;
                corresPLSListIndex[1] = 1;
                beltPosesIndexToPLSSlotMap[0] = 7;
                beltPosesIndexToPLSSlotMap[1] = 8;
            }
            bool storageConflicts = false;
            for (int i = 0; i < cargoBeltPoses.Count; i++)
            {
                BpCargoBeltPos beltPos = cargoBeltPoses[i];
                if (beltPos != null)
                {
                    if ((i <= 2 || i >= 6))
                    {
                        // 将所有未默认对好带子的端点延长到PLS对应Slot的正下方
                        if (beltPos.cargoInfo.isResource)
                            this.AddBelts(beltPos.beltId, corresSlotX[i], beltPos.y, 0, beltPos.x - 1, beltPos.y, 0, -1, gridMap.GetBuilding(beltPos.x, beltPos.y), 0, 0);
                        else
                            this.AddBelts(beltPos.beltId, beltPos.x - 1, beltPos.y, 0, corresSlotX[i], beltPos.y, 0, gridMap.GetBuilding(beltPos.x, beltPos.y), -1, 0, 0);

                        // 修改beltPos
                        beltPos.x = corresSlotX[i];
                    }

                    // 然后设置storage并连接
                    if(PLSs.Count > corresPLSListIndex[i])
                    {
                        int PLSIndex = PLSs[corresPLSListIndex[i]];
                        int storageIndex;
                        bool status = this.SetOrGetPLSStorage(PLSIndex, beltPos.cargoItemId, beltPos.isResource, out storageIndex); // 设置PLS的物流塔信息
                        if (!status && storageIndex >= 0) // 说明有冲突
                            storageConflicts = true;
                        //Utils.logger.LogInfo($"i = {i}, slot = {beltPosesIndexToPLSSlotMap[i]}");
                        if(storageIndex >= 0)
                            this.ConnectPLSToBelt(PLSIndex, beltPosesIndexToPLSSlotMap[i], beltPos.isResource ? storageIndex : -1, gridMap.GetBuilding(beltPos.x, beltPos.y)); // 连接物流塔
                    }
                }
            }
            // 物流塔供给增产剂
            if(PLSProvideProliferator && genCoater)
            {
                int coaterSlotPosX = GetBeltLeftX(assemblerInfo) + BpDB.coaterOffsetX - 1;
                int coaterBeginY = 999;
                int coaterEndY = -999;

                for (int i = 0; i < cargoBeltPoses.Count; i++)
                {
                    BpCargoBeltPos beltPos = cargoBeltPoses[i];
                    if(beltPos != null)
                    {
                        if(beltPos.y < coaterBeginY)
                            coaterBeginY = beltPos.y;

                        if(beltPos.isResource && resourceGenCoater || !beltPos.isResource && productGenCoater)
                        {
                            if(beltPos.y > coaterEndY)
                                coaterEndY = beltPos.y;
                        }
                    }
                }
                coaterBeginY -= 1;
                if (coaterBeginY > -2)
                    coaterBeginY = -2;
                if(coaterEndY > coaterBeginY)
                {
                    this.AddBelts(solution.beltsAvailable.Last().itemId, coaterSlotPosX, coaterBeginY, 0, coaterSlotPosX, coaterEndY, 1);
                }
                // 延长到物流塔位置
                int portSlotIndex = 6;
                int portSlotXPos;
                int PLSListIndex = 0;
                if (totalItemCount > 4)
                    PLSListIndex = 1;

                int PLSX, PLSY;
                GetPLSPos(PLSListIndex, assemblerInfo, out PLSX, out PLSY);
                portSlotXPos = PLSX - 1;

                int proliferatorStorageIndex;
                int proliferatorId = 1143;
                this.AddBelts(solution.beltsAvailable.Last().itemId, portSlotXPos, coaterBeginY, 0, coaterSlotPosX - 1, coaterBeginY, 0, -1, gridMap.GetBuilding(coaterSlotPosX, coaterBeginY));
                if(resourceGenCoater && CalcDB.proliferatorAbilityToId.ContainsKey(recipeInfo.incLevel))
                {
                    proliferatorId = CalcDB.proliferatorAbilityToId[recipeInfo.incLevel];
                }
                bool status = this.SetOrGetPLSStorage(PLSs[PLSListIndex], proliferatorId, true, out proliferatorStorageIndex);

                if (proliferatorStorageIndex >= 0)
                    this.ConnectPLSToBelt(PLSs[PLSListIndex], portSlotIndex, proliferatorStorageIndex, gridMap.GetBuilding(portSlotXPos, coaterBeginY));
            }


            // 对于部分配方，原材料和产出物有重叠的时候，需要根据那个多，来处理物流塔的货物是需求还是供给
            if (storageConflicts)
            {
                for (int i = 0; i < recipeInfo.recipeNorm.resourceCounts.Length; i++)
                {
                    if (recipeInfo.recipeNorm.resourceCounts[i] <= 0)
                    {
                        int itemId = recipeInfo.recipeNorm.resources[i];
                        if (recipeInfo.productIndices.ContainsKey(itemId))
                        {
                            int productIndex = recipeInfo.productIndices[itemId];
                            if (recipeInfo.recipeNorm.productCounts[productIndex] > 0)// 说明是净产出
                            {
                                for (int p = 0; p < PLSs.Count; p++)
                                {
                                    int PLSIndex = PLSs[p];
                                    for (int s = 0; s < BpDB.PLSMaxStorageKinds; s++)
                                    {
                                        if (buildings[PLSIndex].parameters[s*6] == itemId)
                                        {
                                            buildings[PLSIndex].parameters[s * 6 + 1] = 1; // 强制设定为供给模式
                                        }    
                                    }
                                }
                            }
                        }
                    }
                }
                for (int i = 0; i < recipeInfo.recipeNorm.productCounts.Length; i++)
                {
                    if (recipeInfo.recipeNorm.productCounts[i] <= 0)
                    {
                        int itemId = recipeInfo.recipeNorm.resources[i];
                        if(recipeInfo.resourceIndices.ContainsKey(itemId))
                        {
                            int resourceIndex = recipeInfo.resourceIndices[itemId];
                            if (recipeInfo.recipeNorm.resourceCounts[resourceIndex] >=0) // 说明是净需求或者催化剂
                            {
                                for (int p = 0; p < PLSs.Count; p++)
                                {
                                    int PLSIndex = PLSs[p];
                                    for (int s = 0; s < BpDB.PLSMaxStorageKinds; s++)
                                    {
                                        if (buildings[PLSIndex].parameters[s * 6] == itemId)
                                        {
                                            buildings[PLSIndex].parameters[s * 6 + 1] = 2; // 强制设定为需求模式
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

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

            blueprintData.dragBoxSize_x = (int)Math.Ceiling(maxX - minX) + 1;
            blueprintData.dragBoxSize_y = (int)Math.Ceiling(maxY - minY) + 1;
            blueprintData.areas[0].width = blueprintData.dragBoxSize_x;
            blueprintData.areas[0].height = blueprintData.dragBoxSize_y;
            blueprintData.buildings = buildings.ToArray();
        }

        public int GetAssemblerY(bool isFirstAssemblerRow, BpAssemblerInfo assemblerInfo)
        {
            if (isFirstAssemblerRow)
                return 0;

            int y = assemblerInfo.centerDistanceTop + assemblerInfo.centerDistanceBottom + 1;
            if (cargoInfoOrderByNorm[2] != null) // 单共享时，中间多插了一条共享带，或者三共享为true时，实际上不是2共享（也就是5号位有货）
            {
                if (cargoInfoOrderByNorm[5]!=null || !share3Belts)
                    y++;
            }
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

        public void GetPLSPos(int PLSListIndex, BpAssemblerInfo assemblerInfo, out int x, out int y)
        {
            x = -7 - assemblerInfo.hitboxExtendX - (genCoater ? BpDB.coaterBeltBackwardLen : 0) - PLSListIndex * BpDB.PLSDistance; 
            y = GetCargoBeltY(2, assemblerInfo, true);
        }

        public int GetBeltLeftX(BpAssemblerInfo assemblerInfo)
        {
            int leftExtend = assemblerInfo.slotConnectBeltXPositions.Min() - 1; // 最左格子对应的传送带的坐标，再额外延长一格
            int reserveForCoater = BpDB.coaterBeltBackwardLen;
            if (!genCoater)
                reserveForCoater = 0;
            int beltLeftX = leftExtend - reserveForCoater;
            return beltLeftX;

            //int leftExtend = assemblerInfo.slotConnectBeltXPositions.Min() - 1; // 最左格子对应的传送带的坐标，再额外延长一格
            //int rightExtend = assemblerInfo.slotConnectBeltXPositions.Max() + 1; // 最右格子对应的传送带坐标，再额外延长一格
            //int reserveForCoater = BpDB.coaterBeltBackwardLen;
            //if (!genCoater)
            //    reserveForCoater = 0;
        }

        public bool isVanilla6006()
        {
            RecipeProto proto = recipeInfo.recipeNorm.oriProto;
            if(proto.Results.Length == 1 && proto.Items.Length == 6)
            {
                if (proto.Results[0] == 6006 && proto.ResultCounts[0] == 1)
                {
                    int[] items = proto.Items;
                    if (items[0] == 6001 && items[1] == 6002 && items[2] == 6003 && items[3] == 6004 && items[4] == 6005 && items[5] == 1122)
                    {
                        for (int i = 0; i < 6; i++)
                        {
                            if (proto.ItemCounts[i] != 1)
                                return false;
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        public bool onlyShare2Belts
        {
            get
            {
                return share3Belts && cargoInfoOrderByNorm[5] == null;
            }
        }

        public bool SetInputBelt(int itemId, int x, int y)
        {
            if (inputBelts == null)
                return false;
            if (inputBelts.ContainsKey(itemId))
                Utils.logger.LogWarning($"物料{LDB.items.Select(itemId).name}的传送带入口端点被覆盖");
            inputBelts[itemId] = buildings[gridMap.GetBuilding(x, y)];
            return true;
        }

        public bool SetOutputBelt(int itemId, int x, int y) 
        {
            if(outputBelts == null)
                return false;
            if (outputBelts.ContainsKey(itemId))
                Utils.logger.LogWarning($"物料{LDB.items.Select(itemId).name}的传送带出口端点被覆盖");
            outputBelts[itemId] = buildings[gridMap.GetBuilding(x, y)];
            return true;
        }
    }

    public class BpCargoInfo
    {
        public int itemId;
        public int index;
        public bool isResource; // 是配方的输入材料true，是配方的产物false
        public double beltSpeedRequiredPerAssembler; // 每个设施每分钟的需求的带速，对于进料要将物品的速度除以stack，对于出料则取决于能否用集装分拣器，能用则除以4，否则不除。如果集装分拣器可用，无论怎么设置，出货口一定用集装分拣器
        public long maxSupportAssemblerCount; // 最高级可用传送带，单带可支持的工厂数量
        public int maxSorterDistance; // 最高级可用分拣器的最远距离（可以支持设施不间断运行）
        // 以下在初次实例化时并未赋值，创建蓝图时才会赋值
        public int useBeltItemId; // 使用的传送带ItemId
        public int useSorterItemId; // 使用的分拣器的ItemId
    }

    public class BpCargoBeltPos
    {
        public BpCargoInfo cargoInfo;
        public int x;
        public int y;

        public BpCargoBeltPos(BpCargoInfo cargoInfo, int x, int y)
        {
            this.cargoInfo = cargoInfo;
            this.x = x;
            this.y = y;
        }

        public int beltId { get { return cargoInfo.useBeltItemId; } }
        public int cargoItemId { get { return cargoInfo.itemId; } }
        public bool isResource { get { return cargoInfo.isResource; } }
    }
}
