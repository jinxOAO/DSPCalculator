using DSPCalculator.Bp;
using DSPCalculator.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static UnityEngine.TouchScreenKeyboard;

namespace DSPCalculator.Bp
{
    public class BpProcessorGB
    {
        public BpProcessor processor;
        public SolutionTree solution { get { return processor.solution; } }
        public List<BpGBCargoInfo> cargoInfos;
        public List<int> beltLines;

        public List<int> megaAssemblers;

        public PrefabDesc prefabDesc; // 巨塔的prefabDesc

        public static int megaAssemblerDistance = 7; // 塔厂间距
        public static int slotDirIn = 2;
        public static int slotDirOut = 1;
        public static int accMode = 1;
        public static List<int> curMegaSlotByBeltLinesIndex = new List<int> { 5, 4, 3, 2, 6, 1 }; // 当前巨塔的slot口去连上一个巨塔的对应口，index是beltLines的index，但是最后一个1不能使用！要根据奇偶性选（要从curSlotIndex5的取值）
        public static List<int> lastMegaSlotByBeltLinesIndex = new List<int> { 9, 10, 11, 0, 8, 1 };
        public static List<int> curSlotIndex5 = new List<int> { 1, 7 }; // 如果是第6条带子（beltLines的index == 5），若塔的index为偶数（%2==0），则取0号位置的slot，否则取1号位置的slot，cur和last对于此用同样的slot相互连接

        public BpProcessorGB(BpProcessor processor)
        {
            this.processor = processor;
            cargoInfos = new List<BpGBCargoInfo>();
        }

        public void PreProcess()
        {
            int stack = processor.solution.userPreference.bpStack;
            RecipeProto recipeProto = processor.recipeInfo.recipeNorm.oriProto;
            // 首先初始化cargoInfos，把每种物品加入列表
            // 其中还要计算每种货物，单塔需要的传送带条数（一般为小于1的小数）
            double maxBeltSpeed = solution.beltsAvailable.Last().speedPerMin * stack;
            double assemblerCount = processor.recipeInfo.assemblerCount;
            for (int i = 0; i < recipeProto.Items.Length; i++)
            {
                int itemId = recipeProto.Items[i];
                double speedEach = processor.recipeInfo.GetInputSpeedOriProto(i, processor.recipeInfo.count) / assemblerCount;
                double beltRequiredEach = speedEach / maxBeltSpeed;
                BpGBCargoInfo cargoInfo = new BpGBCargoInfo();
                cargoInfo.itemId = itemId;
                cargoInfo.itemIndex = i;
                cargoInfo.isResource = true;
                cargoInfo.speedPerMegaAssembler = speedEach;
                cargoInfo.beltRequiredPerMegaAssembler = beltRequiredEach;
                cargoInfos.Add(cargoInfo);
            }
            for (int i = 0; i < recipeProto.Results.Length; i++)
            {
                int itemId = recipeProto.Results[i];
                double speedEach = processor.recipeInfo.GetOutputSpeedOriProto(i, processor.recipeInfo.count) / assemblerCount;
                double beltRequiredEach = speedEach / maxBeltSpeed;
                BpGBCargoInfo cargoInfo = new BpGBCargoInfo();
                cargoInfo.itemId = itemId;
                cargoInfo.itemIndex = i;
                cargoInfo.isResource = false;
                cargoInfo.speedPerMegaAssembler = speedEach;
                cargoInfo.beltRequiredPerMegaAssembler = beltRequiredEach;
                cargoInfos.Add(cargoInfo);
            }


            // 先算每个物品都用单带，能不能满足整个蓝图的产量
            bool singleBeltEachSatisfied = true;
            for (int i = 0; i < recipeProto.Items.Length; i++)
            {
                double totalSpeed = processor.recipeInfo.GetInputSpeedOriProto(i, processor.recipeInfo.count);
                if (totalSpeed > solution.beltsAvailable.Last().speedPerMin * stack)
                {
                    singleBeltEachSatisfied = false;
                    break;
                }
            }
            if(singleBeltEachSatisfied)
            {
                for (int i = 0; i < recipeProto.Results.Length; i++)
                {
                    double totalSpeed = processor.recipeInfo.GetOutputSpeedOriProto(i, processor.recipeInfo.count);
                    if (totalSpeed > solution.beltsAvailable.Last().speedPerMin * stack)
                    {
                        singleBeltEachSatisfied = false;
                        break;
                    }
                }
            }
            if (singleBeltEachSatisfied) // 每种原材料都单带就能满足，直接ok
            {
                processor.supportAssemblerCount = (int)Math.Ceiling(processor.recipeInfo.assemblerCount);
            }
            else // 只有每种原材料都单带不能满足，并且有富裕带位可以供某一种或几种物品增加带子时，考虑某些物品用多带的方案
            {

                // 首先，默认赋值，如果出现所有可用带位都用上才能勉强满足一个塔厂满速工作的情况，要能准备
                int tempBeltLineCountTotal = 0;
                for (int i = 0; i < cargoInfos.Count; i++)
                {
                    cargoInfos[i].beltLineCount = (int)Math.Ceiling(cargoInfos[i].beltRequiredPerMegaAssembler);
                    tempBeltLineCountTotal += cargoInfos[i].beltLineCount;
                }

                if(tempBeltLineCountTotal > 6) // 由于带子和堆叠运力太差，所有位置都用上都不足以满足单塔，直接返回，不能给出蓝图
                {
                    processor.supportAssemblerCount = 0;
                    processor.bpCountToSatisfy = -1;
                    Utils.logger.LogWarning("所有位置都用上都不足以满足单塔，直接返回！");
                    return;
                }

                // 对cargoInfos降序排序
                cargoInfos = cargoInfos.OrderByDescending(x => x.beltRequiredPerMegaAssembler).ToList();
                // 然后从需求最低的（末尾的）开始，尝试让他跑满带（整数个巨建之后最大且低于1的值（如果总货物数少于3或者2，还可以以2倍或3倍尝试）），那么需求更高带的适量加倍之后能否保证所有带子数量总和少于6条带子
                bool found = false;
                for (int i = cargoInfos.Count - 1; i > 0; i--)
                {
                    int maxFactor = 6 / processor.cargoCount;
                    for (int f = maxFactor; f >= 0; f--)
                    {
                        double megaCountDouble = 1.0 * maxFactor / cargoInfos[i].beltRequiredPerMegaAssembler;
                        int realCount = (int)megaCountDouble; // 该货物f倍带可供给塔数的最大值
                        int beltNeedsTotal = 0;
                        for (int c = 0; c < cargoInfos.Count; c++) // 计算按此标准（当前带单带跑满，按比例需要的传送带总条数）
                        {
                            beltNeedsTotal += (int)Math.Ceiling(cargoInfos[c].beltRequiredPerMegaAssembler * realCount);
                        }
                        if (beltNeedsTotal <= 6) // 如果总条数少于6，就是可解的最佳方案
                        {
                            found = true;
                            processor.supportAssemblerCount = realCount;
                            for (int c = 0; c < cargoInfos.Count; c++) // 将计算结果应用
                            {
                                cargoInfos[c].beltLineCount = (int)Math.Ceiling(cargoInfos[c].beltRequiredPerMegaAssembler * realCount);
                            }
                            break;
                        }
                    }
                    // 有解，跳出循环
                    if (found)
                        break;
                }
                if(!found) // 如果没有找到完美解，则依次将空余belt分配给瓶颈资源。注意，如果多个资源同时为瓶颈，则同时分配带子数量，如果不够都分配到，则都不分配，并停止继续寻找
                {
                    int freeBeltLineCount = 6;
                    foreach (var ci in cargoInfos)
                    {
                        freeBeltLineCount -= ci.beltLineCount;

                        Utils.logger.LogInfo($"cargo {LDB.items.Select(ci.itemId).name} already has {ci.beltLineCount}");
                    }
                    while(freeBeltLineCount > 0)
                    {
                        List<int> maxIndexes = new List<int>(); // 瓶颈资源地址，所有瓶颈资源（只取最高的那一个，如果有相等瓶颈，则都存进来）
                        double maxValue = -1;
                        for (int ci = 0; ci < cargoInfos.Count; ci++)
                        {
                            double curValue = cargoInfos[ci].beltRequiredPerMegaAssembler / cargoInfos[ci].beltLineCount;
                            if(curValue > maxValue)
                            {
                                maxValue = curValue;
                                maxIndexes.Clear();
                                maxIndexes.Add(ci);
                            }
                            else if (curValue == maxValue)
                            {
                                maxIndexes.Add(ci);
                            }
                        }
                        if(freeBeltLineCount >= maxIndexes.Count)
                        {
                            freeBeltLineCount -= maxIndexes.Count;
                            foreach (var ciIndex in maxIndexes)
                            {
                                cargoInfos[ciIndex].beltLineCount++;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            // 处理完了cargoInfos的传送带倍率，计算蓝图最大支持的工厂数
            int supportMegaCount = int.MaxValue;
            for (int i = 0; i < cargoInfos.Count; i++)
            {
                BpGBCargoInfo info = cargoInfos[i];
                supportMegaCount = Math.Min(supportMegaCount, (int)(info.beltLineCount / info.beltRequiredPerMegaAssembler));
            }
            supportMegaCount = (int)Math.Min(supportMegaCount, Math.Ceiling(processor.recipeInfo.assemblerCount));
            processor.supportAssemblerCount = supportMegaCount;
            if(supportMegaCount > 0)
            {
                processor.bpCountToSatisfy = processor.recipeInfo.assemblerCount / supportMegaCount;
            }

        }

        public bool GenerateBlueprints(int genLevel)
        {
            // 然后根据预处理过的cargoInfos里面各物品需要的带子数，分配带子，带子顺序如下
            //   -----5（物流塔进巨塔口）     5-----下一个巨塔5流通口
            //        |  3-----------------3  |
            //    ┌──┴┐             ┌┴──┐
            //    │      │--2-------2--│      │
            //    │      │--1-------1--│      │
            //    │      │--0-------0--│      │
            //    └──┬┘             └┬──┘
            //        | 4------------------4  |
            //        5----首个巨塔5流通口----5

            prefabDesc = LDB.items.Select(processor.recipeInfo.assemblerItemId).prefabDesc;

            beltLines = new List<int>();
            int beltUsed = 0;
            for (int i = 0; i < cargoInfos.Count; i++)
            {
                int curUse = cargoInfos[i].beltLineCount;
                for (int c = 0; c < curUse; c++)
                {
                    beltLines.Add(i);
                }
                beltUsed += curUse;
            }
            if(beltUsed > 6)
            {
                Utils.logger.LogError("错误！怎么会使用了超过6条带子呢？");
            }

            // 分配完带子之后，直接造塔厂
            megaAssemblers = new List<int>();
            for (int i = 0; i < processor.supportAssemblerCount; i++)
            {
                int X = i * megaAssemblerDistance;
                int Y = 0;
                BuildMegaAssembler(X, Y);
                ConnectMegaAssemblerToLast(i);
            }

            GenTerminalBeltsAndPLS(genLevel);

            GenDesc();
            processor.PostProcess();
            return true;
        }

        public int BuildMegaAssembler(int x, int y)
        {
            int itemId = processor.recipeInfo.assemblerItemId;
            BlueprintBuilding b = new BlueprintBuilding();
            b.index = processor.buildings.Count;
            b.areaIndex = 0;
            b.localOffset_x = x;
            b.localOffset_y = y;
            b.localOffset_z = 0;
            b.localOffset_x2 = b.localOffset_x;
            b.localOffset_y2 = b.localOffset_y;
            b.localOffset_z2 = b.localOffset_z;
            b.itemId = (short)itemId;
            b.recipeId = processor.recipeInfo.recipeNorm.ID;
            b.modelIndex = (short)LDB.items.Select(itemId).prefabDesc.modelIndex;
            b.parameters = new int[2048];
            if (!processor.recipeInfo.isInc)
                b.parameters[0] = 1;
            processor.buildings.Add(b);
            processor.gridMap.SetBuilding(x, y, b.index);
            megaAssemblers.Add(b.index);
            return b.index;
        }

        public void ConnectMegaAssemblerToLast(int megaListIndex)
        {
            int beltItemId = solution.beltsAvailable.Last().itemId;
            int paramBegin = 192;
            if (megaListIndex > 0)
            {
                BlueprintBuilding cur = processor.buildings[megaAssemblers[megaListIndex]];
                BlueprintBuilding last = processor.buildings[megaAssemblers[megaListIndex - 1]];

                // 连接每一条货物
                for (int i = 0; i < beltLines.Count; i++)
                {
                    int cargoIndex = beltLines[i];
                    BpGBCargoInfo cargoInfo = cargoInfos[cargoIndex];
                    int itemIndex = cargoInfo.itemIndex;
                    bool isResources = cargoInfo.isResource;
                    int curSlot = curMegaSlotByBeltLinesIndex[i];
                    int lastSlot = lastMegaSlotByBeltLinesIndex[i];
                    if(i == 5)
                    {
                        curSlot = curSlotIndex5[megaListIndex % 2];
                        lastSlot = curSlot;
                    }
                    float curPortBeltX = cur.localOffset_x + prefabDesc.portPoses[curSlot].position.x / 1.3f;
                    float curPortBeltY = cur.localOffset_y + prefabDesc.portPoses[curSlot].position.z / 1.3f;
                    float lastPortBeltX = last.localOffset_x + prefabDesc.portPoses[lastSlot].position.x / 1.3f;
                    float lastPortBeltY = last.localOffset_y + prefabDesc.portPoses[lastSlot].position.z / 1.3f;
                    if (i <= 2) // 中间三行直线连接过去
                    {
                        if(isResources)
                        {
                            float midEndX = cur.localOffset_x - 3;
                            int incX = -1;
                            int yaw = 90;
                            int midBeltCount = (int)Math.Round(cur.localOffset_x - 3 - (last.localOffset_x + 3)) + 1;
                            BlueprintBuilding bb0 = AddSingleBelt(beltItemId, curPortBeltX, curPortBeltY, 0, yaw, null, cur, 0, curSlot);
                            for (int b = 0; b < midBeltCount; b++)
                            {
                                bb0 = AddSingleBelt(beltItemId, midEndX + b * incX, curPortBeltY, 0, yaw, null, bb0, 0, 1);
                            }
                            AddSingleBelt(beltItemId, lastPortBeltX, lastPortBeltY, 0, yaw, last, bb0, lastSlot, 1, cargoInfo.itemId);

                            // 塔的port设置
                            int storageIndex = processor.recipeInfo.recipeNorm.oriProto.Results.Length + cargoInfo.itemIndex + 1;
                            last.parameters[paramBegin + 4 * lastSlot] = 1;
                            last.parameters[paramBegin + 4 * lastSlot + 1] = storageIndex;
                            cur.parameters[paramBegin + 4 * curSlot] = 2;
                        }
                        else
                        {
                            float midEndX = last.localOffset_x + 3;
                            int incX = 1;
                            int yaw = 270;
                            int midBeltCount = (int)Math.Round(cur.localOffset_x - 3 - (last.localOffset_x + 3)) + 1;
                            BlueprintBuilding bb0 = AddSingleBelt(beltItemId, lastPortBeltX, lastPortBeltY, 0, yaw, null, last, 0, lastSlot);
                            for (int b = 0; b < midBeltCount; b++)
                            {
                                bb0 = AddSingleBelt(beltItemId, midEndX + b * incX, lastPortBeltY, 0, yaw, null, bb0, 0, 1);
                            }
                            AddSingleBelt(beltItemId, curPortBeltX, curPortBeltY, 0, yaw, cur, bb0, curSlot, 1, cargoInfo.itemId);

                            // 塔的port设置
                            int storageIndex = cargoInfo.itemIndex + 1;
                            last.parameters[paramBegin + 4 * lastSlot] = 2;
                            cur.parameters[paramBegin + 4 * curSlot] = 1;
                            cur.parameters[paramBegin + 4 * curSlot + 1] = storageIndex;
                        }
                    }
                    else if (i <= 5)
                    {
                        int distance = 1; // 向上或下延长出来的距离
                        if(i == 5)
                        {
                            distance = 2;
                        }

                        // 每个口向外接带子
                        int upDown = prefabDesc.portPoses[curSlot].position.z > 0 ? 1 : -1;
                        BlueprintBuilding beginBelt = null;
                        BlueprintBuilding endBelt = null;
                        BlueprintBuilding intoMega = cur;
                        BlueprintBuilding outfromMega = last;
                        float outfromPortBeltX = lastPortBeltX;
                        float outfromPortBeltY = lastPortBeltY;
                        float intoPortBeltX = curPortBeltX;
                        float intoPortBeltY = curPortBeltY;
                        int outfromPort = lastSlot;
                        int intoPort = curSlot;
                        if(!isResources)
                        {
                            intoMega = last;
                            outfromMega = cur;
                            outfromPortBeltX = curPortBeltX;
                            outfromPortBeltY = curPortBeltY;
                            intoPortBeltX = lastPortBeltX;
                            intoPortBeltY = lastPortBeltY;
                            outfromPort = curSlot;
                            intoPort = lastSlot;
                        }
                        // 资源来的塔，是前一个塔
                        BlueprintBuilding bb0 = null;
                        int yawOutfrom = upDown > 0 ? 0 : 180;
                        int yawInto = 180 - yawOutfrom;
                        for (int dis = distance; dis > 0; dis--)
                        {
                            float x = outfromPortBeltX;
                            float y = outfromMega.localOffset_y + (2 + dis) * upDown;
                            if (dis == distance)
                            {
                                beginBelt = AddSingleBelt(beltItemId, x, y, 0, yawOutfrom, null, null, 0, 1);
                                bb0 = beginBelt;
                            }
                            else
                                bb0 = AddSingleBelt(beltItemId, x, y, 0, yawOutfrom, null, bb0, 0, 1);
                        }
                        AddSingleBelt(beltItemId, outfromPortBeltX, outfromPortBeltY, 0, yawOutfrom, outfromMega, bb0, outfromPort, 1, cargoInfo.itemId);// 出塔带
                        outfromMega.parameters[paramBegin + 4 * outfromPort] = slotDirOut;
                        outfromMega.parameters[paramBegin + 4 * outfromPort + 1] = isResources ? (cargoInfo.itemIndex + processor.recipeInfo.recipeNorm.oriProto.Results.Length + 1) : (cargoInfo.itemIndex + 1);

                        // 资源去的塔
                        bb0 = AddSingleBelt(beltItemId, intoPortBeltX, intoPortBeltY, 0, yawInto, null, intoMega, 0, intoPort); // 入塔带
                        for (int dis = 1; dis <= distance; dis++)
                        {
                            float x = intoPortBeltX;
                            float y = intoMega.localOffset_y + (2 + dis) * upDown;
                            bb0 = AddSingleBelt(beltItemId, x, y, 0, yawInto, null, bb0, 0, 1);
                            endBelt = bb0;
                        }
                        intoMega.parameters[paramBegin + 4 * intoPort] = slotDirIn;

                        // 连接beginBelt和endBelt
                        int xDirection = (endBelt.localOffset_x - beginBelt.localOffset_x > 0) ? 1 : -1;
                        int yawConn = xDirection > 0 ? 90 : 270;
                        int countConn = Math.Abs((int)Math.Round(endBelt.localOffset_x) - (int)Math.Round(beginBelt.localOffset_x)) - 1;
                        bb0 = endBelt;
                        for (int conn = 0; conn < countConn; conn++)
                        {
                            float y = endBelt.localOffset_y;
                            float x = endBelt.localOffset_x - (conn + 1) * xDirection;
                            bb0 = AddSingleBelt(beltItemId, x, y, 0, yawConn, null, bb0, 0, 1);
                        }
                        beginBelt.outputObj = bb0;
                        beginBelt.outputToSlot = 1;
                        
                    }
                }
            }
        }

        public void GenTerminalBeltsAndPLS(int genLevel)
        {
            int paramBegin = 192;
            int beltItemId = solution.beltsAvailable.Last().itemId;
            List<int> beltLineTerminals = new List<int> { -1, -1, -1, -1, -1, -1 };

            if (processor.genCoater || genLevel >= 1)
            {
                int prolifSupplyX = -7;
                for (int i = 0; i < beltLines.Count; i++)
                {
                    BpGBCargoInfo cargoInfo = cargoInfos[beltLines[i]];
                    bool isResource = cargoInfo.isResource;
                    int portSlot = curMegaSlotByBeltLinesIndex[i];
                    int distanceY = 2;
                    BlueprintBuilding mega = processor.buildings[megaAssemblers[0]];
                    int terminalX0 = (int)Math.Round(mega.localOffset_x) - 4 - (processor.genCoater ? BpDB.coaterBeltBackwardLen : 0);
                    if (i == 5)
                    {
                        portSlot = curSlotIndex5[0];
                        distanceY = 3;
                    }
                    float X = mega.localOffset_x + prefabDesc.portPoses[portSlot].position.x / 1.3f;
                    float Y = mega.localOffset_y + prefabDesc.portPoses[portSlot].position.z / 1.3f;
                    int portYSide = prefabDesc.portPoses[portSlot].position.z > 0 ? 1 : -1;
                    int yawPort = 0;
                    if (i <= 2)
                        yawPort = isResource ? 90 : 270;
                    else if (i == 4)
                        yawPort = isResource ? 0 : 180;
                    else
                        yawPort = isResource ? 180 : 0;
                    if (isResource)
                    {
                        beltLineTerminals[i] = AddSingleBelt(beltItemId, X, Y, 0, yawPort, null, mega, 0, portSlot).index;
                        mega.parameters[paramBegin + 4 * portSlot] = slotDirIn;
                        if(i > 2) // 竖直方向需要先延长
                        {
                            int endY = (int)Math.Round(Y) + portYSide;
                            int beginY = (int)Math.Round(Y) + distanceY * portYSide;
                            processor.AddBelts(beltItemId, X, beginY, 0, X, endY, 0, -1, beltLineTerminals[i]);
                            beltLineTerminals[i] = processor.gridMap.GetBuilding((int)Math.Round(X), beginY);
                        }
                        processor.AddBelts(beltItemId, terminalX0, processor.buildings[beltLineTerminals[i]].localOffset_y, 0, (int)Math.Round(processor.buildings[beltLineTerminals[i]].localOffset_x) - 1, processor.buildings[beltLineTerminals[i]].localOffset_y, 0, -1, beltLineTerminals[i], cargoInfo.itemId);
                        beltLineTerminals[i] = processor.gridMap.GetBuilding(terminalX0, (int)Math.Round(processor.buildings[beltLineTerminals[i]].localOffset_y));
                        if (processor.resourceGenCoater)
                            processor.AddCoater(terminalX0 + BpDB.coaterOffsetX, (int)Math.Round(processor.buildings[beltLineTerminals[i]].localOffset_y));
                    }
                    else
                    {
                        beltLineTerminals[i] = AddSingleBelt(beltItemId, X, Y, 0, yawPort, mega, null, portSlot, 0).index;
                        mega.parameters[paramBegin + 4 * portSlot] = slotDirOut;
                        mega.parameters[paramBegin + 4 * portSlot + 1] = isResource ? (processor.recipeInfo.recipeNorm.oriProto.Results.Length + cargoInfo.itemIndex + 1) : (cargoInfo.itemIndex + 1);
                        if (i > 2) // 竖直方向需要先延长
                        {
                            int beginY = (int)Math.Round(Y) + portYSide;
                            int endY = (int)Math.Round(Y) + distanceY * portYSide;
                            processor.AddBelts(beltItemId, X, beginY, 0, X, endY, 0, beltLineTerminals[i], -1);
                            beltLineTerminals[i] = processor.gridMap.GetBuilding((int)Math.Round(X), endY);
                        }
                        processor.AddBelts(beltItemId, (int)Math.Round(processor.buildings[beltLineTerminals[i]].localOffset_x) - 1, processor.buildings[beltLineTerminals[i]].localOffset_y, 0, terminalX0, processor.buildings[beltLineTerminals[i]].localOffset_y, 0, beltLineTerminals[i], -1, 0, cargoInfo.itemId);
                        beltLineTerminals[i] = processor.gridMap.GetBuilding(terminalX0, (int)Math.Round(processor.buildings[beltLineTerminals[i]].localOffset_y));
                        if (processor.productGenCoater)
                            processor.AddCoater(terminalX0 + BpDB.coaterOffsetX, (int)Math.Round(processor.buildings[beltLineTerminals[i]].localOffset_y));
                    }

                }

                if(genLevel >= 1)
                {
                    bool storageConflicts = false;
                    List<int> beltLineToPLSSlot = new List<int> { 9, 10, 11, 0, 8, 1 };
                    int prolifSlot = 7;
                    processor.PLSs = new List<int>();
                    int line5PLS = 0;
                    int prolifPLS = 0;
                    int distanceFromTerminal = 4;
                    int itemCount = 0;
                    Dictionary<int, int> recorder = new Dictionary<int, int>();
                    for (int i = 0; i < cargoInfos.Count; i++)
                    {
                        if (!recorder.ContainsKey(cargoInfos[i].itemId))
                        {
                            recorder[cargoInfos[i].itemId] = 0;
                            itemCount++;
                        }
                    }
                    itemCount = itemCount + (processor.PLSProvideProliferator ? 1 : 0);
                    int PLSCount = itemCount > 5 ? 2 : 1;
                    for (int i = 0; i < PLSCount; i++)
                    {
                        int X = (int)Math.Round(processor.buildings[beltLineTerminals[0]].localOffset_x) - distanceFromTerminal - BpDB.PLSDistance * i;
                        int Y = 0;
                        processor.AddPLS(X, Y);
                    }
                    // 连接塔
                    for (int i = 0; i < beltLines.Count; i++)
                    {
                        int PLSIndex = 0;
                        int storageIndex;
                        BpGBCargoInfo cargoInfo = cargoInfos[beltLines[i]];
                        bool status = SetOrGetPLSStorage(processor.PLSs[0], cargoInfo.itemId, cargoInfo.isResource, out storageIndex);
                        if (!status && storageIndex >= 0) // 说明有冲突
                        { 
                            storageConflicts = true; 
                        }
                        else if (!status)
                        {
                            PLSIndex = 1;
                            SetOrGetPLSStorage(processor.PLSs[1], cargoInfo.itemId, cargoInfo.isResource, out storageIndex);
                        }
                        if (i <= 2 && PLSIndex > 0)
                        {
                            Utils.logger.LogError("中间三条带竟然会无法从第0个PLS获取storage位置，这不正常!");
                        }
                        int PLSSlot = beltLineToPLSSlot[i];
                        if (i > 2) // 要延长到Slot为止
                        {
                            int slotX = (int)Math.Round(processor.buildings[processor.PLSs[PLSIndex]].localOffset_x) + 1;
                            if (i == 5)
                                slotX -= 1;
                            int terminalY = (int)Math.Round(processor.buildings[beltLineTerminals[i]].localOffset_y);
                            int terminalX = (int)Math.Round(processor.buildings[beltLineTerminals[i]].localOffset_x);
                            if (cargoInfo.isResource)
                                processor.AddBelts(beltItemId, slotX, terminalY, 0, terminalX - 1, terminalY, 0, -1, beltLineTerminals[i]);
                            else
                                processor.AddBelts(beltItemId, terminalX - 1, terminalY, 0, slotX, terminalY, 0, beltLineTerminals[i], -1);

                            beltLineTerminals[i] = processor.gridMap.GetBuilding(slotX, terminalY);
                        }

                        int beltIndex = beltLineTerminals[i];
                        processor.ConnectPLSToBelt(processor.PLSs[PLSIndex], PLSSlot, cargoInfo.isResource ? storageIndex : -1, beltIndex);
                    }

                    // 塔提供增产剂
                    if(processor.PLSProvideProliferator)
                    {
                        List<int> lineIndexToY = new List<int> { -1, 0, 1, 4, -4, 5 };
                        int maxY = -999;
                        int prolifSupplyY = -5;
                        for (int i = beltLines.Count - 1; i >= 0; i--)
                        {
                            BpGBCargoInfo cargoInfo = cargoInfos[beltLines[i]];
                            if(cargoInfo.isResource && processor.resourceGenCoater || !cargoInfo.isResource && processor.productGenCoater)
                            {
                                int curY = lineIndexToY[i];
                                if (curY > maxY)
                                    maxY = curY;
                            }
                        }
                        if(maxY > -5)
                        {
                            processor.AddBelts(beltItemId, prolifSupplyX, prolifSupplyY, 0, prolifSupplyX, maxY, 1);
                            int terminalBeltIndex = processor.gridMap.GetBuilding(prolifSupplyX, prolifSupplyY);

                            int PLSIndexP = 0;
                            int storageIndexP;
                            int proliferatorId = 1143;
                            if (processor.resourceGenCoater && CalcDB.proliferatorAbilityToId.ContainsKey(processor.recipeInfo.incLevel))
                            {
                                proliferatorId = CalcDB.proliferatorAbilityToId[processor.recipeInfo.incLevel];
                            }
                            bool status = SetOrGetPLSStorage(processor.PLSs[0], proliferatorId, true, out storageIndexP);
                            if (!status && storageIndexP < 0)
                            {
                                SetOrGetPLSStorage(processor.PLSs[1], proliferatorId, true, out storageIndexP);
                                PLSIndexP = 1;
                            }

                            int prolifSupplyPortX = (int)Math.Round(processor.buildings[processor.PLSs[PLSIndexP]].localOffset_x);
                            processor.AddBelts(beltItemId, prolifSupplyPortX, prolifSupplyY, 0, prolifSupplyX - 1, prolifSupplyY, 0, -1, terminalBeltIndex);
                            processor.ConnectPLSToBelt(processor.PLSs[PLSIndexP], 7, storageIndexP, processor.gridMap.GetBuilding(prolifSupplyPortX, prolifSupplyY));
                        }
                    }

                    // 对于部分配方，原材料和产出物有重叠的时候，需要根据那个多，来处理物流塔的货物是需求还是供给
                    if (storageConflicts)
                    {
                        for (int i = 0; i < processor.recipeInfo.recipeNorm.resourceCounts.Length; i++)
                        {
                            if (processor.recipeInfo.recipeNorm.resourceCounts[i] <= 0)
                            {
                                int itemId = processor.recipeInfo.recipeNorm.resources[i];
                                if (processor.recipeInfo.productIndices.ContainsKey(itemId))
                                {
                                    int productIndex = processor.recipeInfo.productIndices[itemId];
                                    if (processor.recipeInfo.recipeNorm.productCounts[productIndex] > 0)// 说明是净产出
                                    {
                                        for (int p = 0; p < processor.PLSs.Count; p++)
                                        {
                                            int PLSIndex = processor.PLSs[p];
                                            for (int s = 0; s < BpDB.PLSMaxStorageKinds; s++)
                                            {
                                                if (processor.buildings[PLSIndex].parameters[s * 6] == itemId)
                                                {
                                                    processor.buildings[PLSIndex].parameters[s * 6 + 1] = 1; // 强制设定为供给模式
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        for (int i = 0; i < processor.recipeInfo.recipeNorm.productCounts.Length; i++)
                        {
                            if (processor.recipeInfo.recipeNorm.productCounts[i] <= 0)
                            {
                                int itemId = processor.recipeInfo.recipeNorm.resources[i];
                                if (processor.recipeInfo.resourceIndices.ContainsKey(itemId))
                                {
                                    int resourceIndex = processor.recipeInfo.resourceIndices[itemId];
                                    if (processor.recipeInfo.recipeNorm.resourceCounts[resourceIndex] >= 0) // 说明是净需求或者催化剂
                                    {
                                        for (int p = 0; p < processor.PLSs.Count; p++)
                                        {
                                            int PLSIndex = processor.PLSs[p];
                                            for (int s = 0; s < BpDB.PLSMaxStorageKinds; s++)
                                            {
                                                if (processor.buildings[PLSIndex].parameters[s * 6] == itemId)
                                                {
                                                    processor.buildings[PLSIndex].parameters[s * 6 + 1] = 2; // 强制设定为需求模式
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                }

                
            }
        }


        public bool SetOrGetPLSStorage(int PLSBuildingIndex, int itemId, bool isNeed, out int index)
        {
            for (int i = 0; i < 5; i++) // 创世小塔特供！5格！
            {
                if (processor.buildings[PLSBuildingIndex].parameters[i * 6] == 0)
                {
                    processor.buildings[PLSBuildingIndex].parameters[i * 6] = itemId;
                    processor.buildings[PLSBuildingIndex].parameters[i * 6 + 1] = isNeed ? 2 : 1;
                    processor.buildings[PLSBuildingIndex].parameters[i * 6 + 3] = BpDB.stationMaxItemCount;
                    index = i;
                    return true;
                }
                else if (processor.buildings[PLSBuildingIndex].parameters[i * 6] == itemId)
                {
                    index = i;
                    int shouldStatus = isNeed ? 2 : 1;
                    if (processor.buildings[PLSBuildingIndex].parameters[i * 6 + 1] != shouldStatus)
                        return false; // 代表有冲突
                    return true;
                }
            }
            index = -1;
            return false; // 代表没有找到空位
        }

        public void GenDesc()
        {
            string desc = "传送带比例BpGB".Translate();
            for (int i = 0;i < cargoInfos.Count;i++) 
            {
                desc += LDB.items.Select(cargoInfos[i].itemId).name;
                desc += cargoInfos[i].isResource ? "入BpGB".Translate() : "出BpGB".Translate();
                desc += "=" + cargoInfos[i].beltLineCount.ToString();
                if (i != cargoInfos.Count - 1)
                    desc += ";   ";
            }
            processor.blueprintData.desc = desc;
        }
        public BlueprintBuilding AddSingleBelt(int itemId, float x, float y, float z, int yaw, BlueprintBuilding from, BlueprintBuilding to, int inputFromSlot, int outputToSlot, int iconId = 0)
        {
            BlueprintBuilding b = new BlueprintBuilding();
            b.index = processor.buildings.Count;
            b.areaIndex = 0;
            b.localOffset_x = x;
            b.localOffset_y = y;
            b.localOffset_z = z;
            b.localOffset_x2 = b.localOffset_x;
            b.localOffset_y2 = b.localOffset_y;
            b.localOffset_z2 = b.localOffset_z;
            b.inputToSlot = 1;
            b.yaw = yaw;
            b.yaw2 = b.yaw;
            b.itemId = (short)itemId;
            b.modelIndex = (short)LDB.items.Select(itemId).prefabDesc.modelIndex;
            b.inputFromSlot = inputFromSlot;
            b.outputToSlot = outputToSlot;
            if(from != null) 
                b.inputObj = from;
            if(to != null)
                b.outputObj = to;
            if (iconId > 0)
            {
                b.parameters = new int[] { iconId, 0};
            }
            processor.gridMap.SetBuilding((int)Math.Round(x),(int)Math.Round(y), b.index);
            processor.buildings.Add(b);
            return b;
        }

    }

    public class BpGBCargoInfo
    {
        public int itemId;
        public int itemIndex; // item在原材料或者产物的index
        public bool isResource;
        public double speedPerMegaAssembler;
        public double beltRequiredPerMegaAssembler; // 以当前可用的最高级带子计
        public int beltLineCount; // 使用几条传送带的倍率

        public BpGBCargoInfo()
        {
            beltLineCount = 1;
        }
    }
}
