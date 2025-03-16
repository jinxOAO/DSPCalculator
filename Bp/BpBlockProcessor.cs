using DSPCalculator.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSPCalculator.Bp
{
    /// <summary>
    /// 专用于BpConnector构建黑盒时的每个recipeInfo的小蓝图构建
    /// </summary>
    public class BpBlockProcessor : BpProcessor
    {
        public BpConnector parentConnector;

        public BpBlockProcessor(RecipeInfo recipeInfo, SolutionTree solution, BpConnector parentConnector, int forceRowCount) : base(recipeInfo, solution, forceRowCount)
        {
            this.parentConnector = parentConnector;
        }

        public new bool GenerateBlueprint(int genLevel)
        {
            if (bpPrefabId == 1)
            {
                this.blueprintData = BpBuilder.CreateEmpty();
                if (this.blueprintData.FromBase64String(BpPrefabs.universeMatrix) == BlueprintDataIOError.OK)
                {
                    if (recipeInfo.assemblerItemId == 2901)
                    {
                        short modelIndex = (short)LDB.items.Select(2901).prefabDesc.modelIndex;
                        int len = blueprintData.buildings.Length;
                        for (int i = 0; i < len; i++)
                        {
                            if (blueprintData.buildings[i].itemId == 2902)
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
            if (maxLevel > supportAssemblerCount)
                maxLevel = supportAssemblerCount;
            int groupCount = (int)Math.Ceiling(supportAssemblerCount * 1.0 / maxLevel);
            if (!isLab)
                maxLevel = 1;
            // 创建第一行工厂，第一行工厂y为0
            int assemblerCountFirstRow = doubleRow ? (int)Math.Ceiling(supportAssemblerCount * 1.0 / 2) : supportAssemblerCount; // 每行工厂的数量，如果是单行蓝图则就等于support
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
                            if (parentConnector.itemSumInfos.ContainsKey(cargoInfo.itemId)) // 这里是因为，如果串联带子有多个配方相关，单条带必须足够运力所有配方。则这种情况下父级节点会有相关设定
                            {
                                beltId = parentConnector.itemSumInfos[cargoInfo.itemId].needBeltId;
                                if (!BpDB.beltInfos[beltId].Satisfy(assemblerCountOnThisBelt * cargoInfo.beltSpeedRequiredPerAssembler))
                                {
                                    Utils.logger.LogError($"{Utils.ItemName(cargoInfo.itemId)}使用了父级要求的{Utils.ItemName(beltId)}，但是无法满足配方。这不应该出现。");
                                }
                            }
                            else
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
                //// 处理第二行工厂
                //int assemblerX2 = 1; // 错开一格
                //int assemblerY2 = GetAssemblerY(false, assemblerInfo);
                //if (!isLab)
                //{
                //    for (int i = 0; i < assemblerCountSecondRow; i++)
                //    {
                //        assemblersSecondRow.Add(this.AddAssembler(assemblerId, recipeInfo.ID, assemblerX2 + assemblerInfo.DragDistanceX * i, assemblerY2, 0, assemblerInfo.defaultYaw, recipeInfo.isInc));
                //    }
                //}
                //else
                //{
                //    for (int i = 0; i < assemblerCountSecondRow; i++)
                //    {
                //        assemblersSecondRow.Add(this.AddLab(assemblerId, recipeInfo.ID, assemblerX2 + assemblerInfo.DragDistanceX * i, assemblerY2, maxLevel, recipeInfo.isInc));
                //    }
                //}

                //if (genLevel >= 0)
                //{
                //    // 不再处理cargoInfo，而是直接创建带子
                //    // ------开始创建带子

                //    int beltLeftX = GetBeltLeftX(assemblerInfo);
                //    int rightExtend = assemblerInfo.slotConnectBeltXPositions.Max() + 1; // 最右格子对应的传送带坐标，再额外延长一格
                //    int beltRightX = (assemblerCountSecondRow - 1) * assemblerInfo.DragDistanceX + rightExtend; // +1是因为第二行会右移一格
                //    for (int i = 0; i < cargoInfoOrderByNorm.Count; i++)
                //    {
                //        if (i == 2) // 共享带子不能重复创建
                //            continue;
                //        if (share3Belts && (i == 0 || i == 5))
                //            continue;
                //        BpCargoInfo cargoInfo = cargoInfoOrderByNorm[i];
                //        if (cargoInfo != null)
                //        {
                //            int Y = GetCargoBeltY(i, assemblerInfo, false); // 这里是第二行工厂的袋子，要false
                //            if (cargoInfo.isResource)
                //            {
                //                this.AddBelts(cargoInfo.useBeltItemId, beltLeftX, Y, 0, beltRightX, Y, 0, -1, -1, cargoInfo.itemId, 0);
                //            }
                //            else
                //            {
                //                this.AddBelts(cargoInfo.useBeltItemId, beltRightX, Y, 0, beltLeftX, Y, 0, -1, -1, 0, cargoInfo.itemId);
                //            }
                //            if (i == 1 && Y < 8 && genLevel > 0) // 说明那条带子不够高，不能直接横着拉到PLS的对应Slot正上方，所以要接到那个格子。只有第二行工厂的i==1的那条有可能不够高
                //            {
                //                if (cargoInfo.isResource)
                //                {
                //                    this.AddBelts(cargoInfo.useBeltItemId, beltLeftX, 8.1f, 0, beltLeftX, Y + 1, 0, -1, gridMap.GetBuilding(beltLeftX, Y), 0, 0);
                //                }
                //                else
                //                {
                //                    this.AddBelts(cargoInfo.useBeltItemId, beltLeftX, Y + 1.1f, 0, beltLeftX, 8, 0, gridMap.GetBuilding(beltLeftX, Y), -1, 0, 0);
                //                }
                //                Y = 8;
                //            }

                //            // 将带子端点信息记录
                //            if (i == 0 && cargoInfoOrderByNorm[2] == null)// 说明中间不是三带，则第二行工厂的0号新带位置应该是4而非5（5来自于BpDB.cargoInfoNormIndexToBeltPosIndexMap_SecondRow[i]）
                //                cargoBeltPoses[4] = new BpCargoBeltPos(cargoInfo, beltLeftX, Y);
                //            else if (i != 2)
                //                cargoBeltPoses[BpDB.cargoInfoNormIndexToBeltPosIndexMap_SecondRow[i]] = new BpCargoBeltPos(cargoInfo, beltLeftX, Y);


                //            // 创建喷涂机
                //            if (resourceGenCoater && cargoInfo.isResource || productGenCoater && !cargoInfo.isResource)
                //            {
                //                this.AddCoater(beltLeftX + BpDB.coaterOffsetX, Y);
                //            }
                //        }
                //    }
                //    // ------然后创建爪子，如果是3共享，必须专门判断每一个爪子id、filter和位置
                //    for (int i = 0; i < assemblersSecondRow.Count; i++)
                //    {
                //        int assemblerBuildingIndex = assemblersSecondRow[i];
                //        for (int c = 0; c < cargoInfoOrderByNorm.Count; c++)
                //        {
                //            BpCargoInfo cargoInfo = cargoInfoOrderByNorm[c];
                //            if (cargoInfo != null)
                //            {
                //                bool isResource = cargoInfo.isResource;
                //                int cargoItemId = cargoInfo.itemId;
                //                int sorterId = cargoInfo.useSorterItemId;
                //                int mappedCargoIndex = c; // 如果3带共享，具体是什么cargo需要映射过去
                //                if (share3Belts) // 3共享的带子，不能直接读cargoInfoOrderByNorm的属性，因为和第一行不一样，是倒着的
                //                {
                //                    if (c == 0)
                //                    {
                //                        if (cargoInfoOrderByNorm[5] != null)
                //                        {
                //                            mappedCargoIndex = 5;
                //                        }
                //                        else if (cargoInfoOrderByNorm[2] != null)
                //                        {
                //                            mappedCargoIndex = 2;
                //                        }
                //                    }
                //                    else if (c == 2 && cargoInfoOrderByNorm[5] == null) // 这种特殊情况是只有两条带子共享
                //                    {
                //                        mappedCargoIndex = 0;
                //                    }
                //                    else if (c == 5)
                //                    {
                //                        mappedCargoIndex = 0;
                //                    }
                //                    isResource = cargoInfoOrderByNorm[mappedCargoIndex].isResource;
                //                    cargoItemId = cargoInfoOrderByNorm[mappedCargoIndex].itemId;

                //                    // 要独立计算分拣器使用！，既不能用
                //                    if (solution.userPreference.bpSorterHighest || (solution.sortersAvailable.Last().grade >= 4 && !isResource))
                //                    {
                //                        sorterId = solution.sortersAvailable.Last().itemId;
                //                    }
                //                    else  // 否则尽可能使用便宜的
                //                    {
                //                        int distance = c / 2 + 1;
                //                        for (int s = 0; s < solution.sortersAvailable.Count; s++)
                //                        {
                //                            if (solution.sortersAvailable[s].Satisfy(cargoInfoOrderByNorm[mappedCargoIndex].beltSpeedRequiredPerAssembler * (isLab ? maxLevel : 1), distance))
                //                            {
                //                                sorterId = solution.sortersAvailable[s].itemId;
                //                                break;
                //                            }
                //                        }
                //                    }
                //                    if (sorterId < 0) // 最快的一个可用爪子（但是会受限于科技）都不能满足一个工厂的进料
                //                    {
                //                        sorterId = BpDB.sortersAscending.Last().itemId; // 直接使用集装分拣器
                //                        if (!insufficientSorterItems.Contains(cargoItemId))
                //                            insufficientSorterItems.Add(cargoItemId); // 将这个配方记录为分拣器无法满足，需要用户自行调整（比如换成两个低级爪子）
                //                    }
                //                }
                //                int slot = assemblerInfo.cargoNormIndex2SlotMap_SecondRow[c];
                //                if (share3Belts && onlyShare2Belts) // 只共享了两条袋子的话，用三条带子的slot分配会冲突，要全部右移一格
                //                {
                //                    if (c == 0)
                //                        slot = assemblerInfo.cargoNormIndex2SlotMap_SecondRow[2];
                //                    else if (c == 2)
                //                        slot = assemblerInfo.cargoNormIndex2SlotMap_SecondRow[5];
                //                }
                //                this.AssemblerConnectToBelt(assemblerBuildingIndex, slot, sorterId, 1 + c / 2, isResource, isResource ? 0 : cargoItemId, isResource);
                //            }
                //        }
                //    }
                //}
            }
            if (genLevel > 0)
                GenerateAndConnectPLS();
            if (insufficientSorterItems.Count > 0)
                UIRealtimeTip.Popup("分拣器科技不足警告".Translate());
            PostProcess();
            return true;
        }

        public new void PostProcess()
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
            if(!genCoater)
            {
                ERecipeType type = (ERecipeType)recipeInfo.recipeNorm.type;
                if(type != ERecipeType.Smelt && type != ERecipeType.Assemble && type != ERecipeType.Research)
                {
                    minX -= 2;
                }
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
    }
}
