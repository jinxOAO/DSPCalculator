using DSPCalculator.Bp;
using DSPCalculator.Logic;
using DSPCalculator.UI;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSPCalculator.Bp
{
    public class BpConnector
    {
        public static bool enabled = true;

        public UICalcWindow calcWindow;
        public SolutionTree solution; 
        public BlueprintData blueprintData;
        public List<BpBlockProcessor> processors;
        public Dictionary<int, BpBlockProcessor> processorsMap; // recipeId到对应的processor映射
        public List<BlueprintBuilding> buildings;
        public Dictionary<int, BpItemSumInfo> itemSumInfos; // 存储每个Item所有配方的产出或者消耗总和，所需求的带速的最大值，表示潜在的单条带需要承担的最大运力
        public Dictionary<int, BpItemPathInfo> itemPathInfos; // 存储每个Item用于构造串联线路的信息

        public List<BpBlockLineInfo> lines;
        public List<BpSegmentLayer> segLayers; // 所有架空连接的传送带所在层的信息
        public List<BpTerminalInfo> terminalInfos; // 所有向外连接的输出、输出接口的belt信息
        public int width;
        public int height;

        public int maxZ = 49; // 允许的传送带最大高度（实际为maxZ + 0.5）
        public int minZ = 5; // 允许的传送带最小高度（实际为minZ + 0.5)，会根据黑盒蓝图里面有什么生产建筑决定
        public bool succeeded;

        // 以下为静态量
        public static float minBeltDistance = 0.5f; // 两条带在同一高度时，允许的最近点的距离

        public BpConnector(UICalcWindow calcWindow) 
        {
            this.calcWindow = calcWindow;
            solution = calcWindow.solution;
            blueprintData = BpBuilder.CreateEmpty();
            processors = new List<BpBlockProcessor>();
            processorsMap = new Dictionary<int, BpBlockProcessor>();
            buildings = new List<BlueprintBuilding>();
            itemSumInfos = new Dictionary<int, BpItemSumInfo>();
            itemPathInfos = new Dictionary<int, BpItemPathInfo>();
            lines = new List<BpBlockLineInfo>();
            segLayers = new List<BpSegmentLayer>();
            terminalInfos = new List<BpTerminalInfo>();
            succeeded = GenerateFullBlueprint();
        }

        private bool GenerateFullBlueprint()
        {
            Stopwatch timer = new Stopwatch();

            // 判断每种物品，单带运力是否足够
            timer.Start();
            if (!CalcItemSumInfos())
                return false;
            timer.Stop();
            Utils.logger.LogInfo($"判断每种物品的单带运力是否足够耗时{timer.Elapsed.TotalMilliseconds}ms");


            // 生成所有processor
            timer.Start();
            if(!GenProcessors())
                return false;
            timer.Stop();
            Utils.logger.LogInfo($"生成processor过程耗时{timer.Elapsed.TotalMilliseconds}ms");

            // 根据长度降序排序
            timer.Restart();
            processors = processors.OrderByDescending(x => x.width).ToList();
            timer.Stop();
            Utils.logger.LogInfo($"排序processor过程耗时{timer.Elapsed.TotalMilliseconds}ms");

            //填充行列表
            timer.Restart();
            if (!ArrangeBpBlocks(processors))
                return false;
            timer.Stop();
            Utils.logger.LogInfo($"排列processor过程耗时{timer.Elapsed.TotalMilliseconds}ms");


            //放置建筑
            timer.Restart();
            if (!PlaceBuildings())
                return false;
            timer.Stop();
            Utils.logger.LogInfo($"重新放置buildings过程耗时{timer.Elapsed.TotalMilliseconds}ms");


            //放置建筑
            timer.Restart();
            if (!ConnectBlocks())
                return false;
            timer.Stop();
            Utils.logger.LogInfo($"连接各个block过程耗时{timer.Elapsed.TotalMilliseconds}ms");


            PostProcess(); // 后处理，完成蓝图构建
            return true;
        }

        // 判断每种物品，单带运力是否足够
        private bool CalcItemSumInfos()
        {
            itemSumInfos.Clear();
            int rawOreInputStack = solution.userPreference.bpStack; // 作为原矿的输入堆叠数
            int outputStack = solution.userPreference.bpSorterMk4OutputStack; // 作为白爪输出的堆叠数
            foreach (var recipeInfoKV in solution.recipeInfos)
            {
                RecipeInfo recipeInfo = recipeInfoKV.Value;
                if (recipeInfo.assemblerCount > 0.0001f)
                {
                    int[] results = recipeInfo.recipeNorm.oriProto.Results;
                    for (int i = 0; i < results.Length; i++)
                    {
                        int itemId = results[i];
                        if (!itemSumInfos.ContainsKey(itemId)) // 首次计入某种物品时，将来自原矿的部分按照rawOreInputStack堆叠计算带速占用
                        {
                            if (solution.itemNodes.ContainsKey(itemId))
                            {
                                double fromOreBeltSpeedNeed = solution.itemNodes[itemId].speedFromOre / rawOreInputStack;
                                itemSumInfos[itemId] = new BpItemSumInfo(itemId, fromOreBeltSpeedNeed);
                            }
                            else
                            {
                                Utils.logger.LogError($"判断物品{Utils.ItemName(itemId)}单带运力是否足够时发生了错误，节点在solution中不存在。");
                                return false;
                            }
                        }
                        // 非首次计入时，将配方的输出量，除以白爪堆叠数来计算占用的带速
                        itemSumInfos[itemId].needBeltSpeed += recipeInfo.GetOutputSpeedOriProto(i, recipeInfo.count) / outputStack;
                    }

                    // 处理全局传送带最小高度
                    int requireBeltMinHeight = BpDB.assemblerInfos[recipeInfo.assemblerItemId].height + 1;
                    minZ = Math.Max(minZ, requireBeltMinHeight);
                }
            }
            // 没有任何配方产出的item不会在itemSumInfo里面出现，这代表着他们只从原矿输入，无所谓在黑盒的起点处拥有几条带子（可以每个block都拥有独立的输入带，即使对于同一个视为原矿的item）
            // 因此不需要判断“单带”运力是否足够，只需要后续判断单个蓝图是否可以满足即可
            foreach (var sumInfoKV in itemSumInfos)
            {
                BpItemSumInfo sumInfo = sumInfoKV.Value;
                if(sumInfo.needBeltSpeed > solution.beltsAvailable.Last().speedPerMin)
                {
                    Utils.logger.LogInfo($"由于{Utils.ItemName(sumInfo.itemId)}速度过大{sumInfo.needBeltSpeed}>{solution.beltsAvailable.Last().speedPerMin}");
                    return false;
                }
                if (solution.userPreference.bpBeltHighest) // 如果用户配置为总是使用最高级
                {
                    sumInfo.needBeltId = solution.beltsAvailable.Last().itemId;
                }
                else
                {
                    for (int i = 0; i < solution.beltsAvailable.Count; i++)
                    {
                        double beltSpd = solution.beltsAvailable[i].speedPerMin;
                        if (beltSpd >= sumInfo.needBeltSpeed)
                        {
                            //Utils.logger.LogInfo($"{Utils.ItemName(sumInfo.itemId)}需要{sumInfo.needBeltSpeed}，{Utils.ItemName(solution.beltsAvailable[i].itemId)}速度{solution.beltsAvailable[i].speedPerMin}满足了");
                            sumInfo.needBeltId = solution.beltsAvailable[i].itemId;
                            break;
                        }
                    }
                }
            }
            return true;
        }

        // 对每一个配方生成其蓝图
        private bool GenProcessors()
        {
            processors.Clear();
            processorsMap.Clear();
            itemPathInfos.Clear();
            foreach (var recipeInfoKV in solution.recipeInfos)
            {
                RecipeInfo recipeInfo = recipeInfoKV.Value;
                if (recipeInfo != null && recipeInfo.assemblerCount > 0.0001f)
                {
                    BpBlockProcessor processor = new BpBlockProcessor(recipeInfo, solution, this, 1);
                    processor.PreProcess();
                    if (processor.bpCountToSatisfy > 1)
                    {
                        Utils.logger.LogWarning($"生成bp block 过程失败，由于配方{LDB.recipes.Select(recipeInfo.ID).name}的单个蓝图无法承载全部产出。");
                        return false;
                    }
                    else
                    {
                        if (processor.GenerateBlueprint(0))
                        {
                            processors.Add(processor);
                            processorsMap[recipeInfo.ID] = processor;
                        }
                        else
                        {
                            Utils.logger.LogWarning($"生成bp block 过程失败，由于配方{LDB.recipes.Select(recipeInfo.ID).name}生成蓝图过程失败。");
                            return false;
                        }
                    }
                    RecipeProto proto = recipeInfo.recipeNorm.oriProto;
                    for (int i = 0; i < proto.Items.Length; i++)
                    {
                        int itemId = proto.Items[i];
                        if(!itemPathInfos.ContainsKey(itemId))
                        {
                            itemPathInfos[itemId] = new BpItemPathInfo(itemId, solution.userPreference.IsOre(itemId) && solution.itemNodes[itemId].speedFromOre > 0.001f);
                        }
                        itemPathInfos[itemId].AddDemander(processor);
                    }
                    for (int i = 0; i < proto.Results.Length; i++)
                    {
                        int itemId = proto.Results[i];
                        if(!itemPathInfos.ContainsKey(itemId))
                        {
                            itemPathInfos[itemId] = new BpItemPathInfo(itemId, solution.userPreference.IsOre(itemId) && solution.itemNodes[itemId].speedFromOre > 0.001f);
                        }
                        itemPathInfos[itemId].AddProvider(processor);
                    }
                }
            }
            if (processors.Count <= 0)
                return false;

            return true;
        }

        // 构造蓝图排列
        private bool ArrangeBpBlocks(List<BpBlockProcessor> processors)
        {
            if(processors.Count <= 0) 
                return false;

            lines.Clear();
            height = 0;
            width = 0;
            BpBlockLineInfo blockLine0 = new BpBlockLineInfo(this);
            blockLine0.AddBpBlock(processors[0]);
            lines.Add(blockLine0);
            int blockCount = processors.Count;
            for (int i = 1; i < blockCount; i++)
            {
                BpBlockProcessor processor = processors[i];
                // 将这块蓝图插入某行中或者新建独立的一行给它
                // 逻辑按照优先级依次进行判断：
                // 如果至少有一行的height不小于此块蓝图，且空余的width不小于此块蓝图，则插入到空余width最小的那个————————记录为minWidth
                // 如果上面的条件不满足，则如果至少有一行的height+5不小于此块蓝图，且空余的width不小于此块蓝图，则插入到height最大的那个（尽量该行的增高量）————————记录为maxHeight
                // 如果上面的条件不满足，判断：新width = width最小的一行+此块width，新height = 现有height + 此块height，如果新height小，则新建独立的一行给此块蓝图，否则将此块蓝图插入到width最小的那行————————记录为minWidthUncondition

                int minWidthLine = -1;
                int minWidth = int.MaxValue;
                int minWidthUnconditionLine = -1;
                int minWidthUncondition = int.MaxValue;
                int maxHeightLine = -1;
                int maxHeight = 0;
                int curWidth = processor.width;
                int curHeight = processor.height;

                for (int j = 0; j < lines.Count; j++)
                {
                    BpBlockLineInfo line = lines[j];

                    if (line.width < minWidthUncondition)
                    {
                        minWidthUncondition = line.width;
                        minWidthUnconditionLine = j;
                    }
                    if (line.height >= curHeight && line.unusedWidth >= curWidth && line.width < minWidth)
                    { 
                        minWidth = line.width;
                        minWidthLine = j;
                    }
                    if(line.height + 5 >= curHeight && line.unusedWidth >= curWidth && line.height > maxHeight)
                    {
                        maxHeight = line.height;
                        maxHeightLine = j;
                    }
                }

                if(minWidthLine >= 0)
                {
                    lines[minWidthLine].AddBpBlock(processor);
                    continue;
                }
                else if (maxHeightLine >= 0)
                {
                    lines[maxHeightLine].AddBpBlock(processor);
                    continue;
                }
                else if (minWidthUnconditionLine >= 0)
                {
                    int newWidth = minWidthUncondition + curWidth;
                    int newHeight = height + curHeight;

                    if (newWidth <= newHeight)
                        lines[minWidthUnconditionLine].AddBpBlock(processor);
                    else
                    {
                        BpBlockLineInfo extraLine = new BpBlockLineInfo(this);
                        extraLine.AddBpBlock(processor);
                        lines.Add(extraLine);
                    }
                }
                else
                {
                    Utils.logger.LogError("在排列蓝图块时出错误，而这种情况不应该发生！");
                    return false;
                }
            }
            blueprintData.dragBoxSize_x = width;
            blueprintData.dragBoxSize_y = height;
            blueprintData.areas[0].width = width;
            blueprintData.areas[0].height = height;
            return true;
        }

        // 将每个原始蓝图的每个建筑防止在黑盒的对应位置
        private bool PlaceBuildings()
        {
            int lineCount = lines.Count;
            int baseY = 0;
            for (int i = 0; i < lineCount; i++)
            {
                int baseX = 0;
                BpBlockLineInfo line = lines[i];
                for (int j = 0; j < line.processors.Count; j++)
                {
                    BpBlockProcessor processor = line.processors[j];
                    int buildingsLen = processor.blueprintData.buildings.Length;
                    for (int k = 0; k < buildingsLen; k++)
                    {
                        BlueprintBuilding bb = processor.blueprintData.buildings[k];
                        bb.localOffset_x += baseX;
                        bb.localOffset_y += baseY;
                        bb.localOffset_x2 += baseX;
                        bb.localOffset_y2 += baseY;
                        bb.index = buildings.Count;
                        buildings.Add(bb);
                    }

                    baseX += processor.width;
                }
                baseY += line.height;
            }

            

            return true;
        }

        // 连接所有物料出入口，包括创建向外的输入输出口
        private bool ConnectBlocks()
        {
            // 我发现垂直传送带（z为整数高的节点连接起来的），不会与层高为半层(z=n+0.5)的传送带产生碰撞。因此，所有斜向连接（水平直接连接）都要在n+0.5的高度进行
            segLayers.Clear();
            terminalInfos.Clear();
            Dictionary<int, int> targets = new Dictionary<int, int>();
            for (int i = 0; i < solution.targets.Count; i++)
            {
                targets[solution.targets[i].itemId] = 1;
            }

            // 额外输入输出口的基本信息
            int portX0, portY0, expandX, expandY, directionX, directionY; // expand代表port越来越多时，坐标的bump数值。direction代表一条带子沿着自己的来时方向的坐标的bump数值
            if(width >= height) // 黑盒的横向宽大于纵向高度，则输入输出口都在上边缘
            {
                portX0 = 0;
                portY0 = height + 1;
                expandX = 2;
                expandY = 0;
                directionX = 0;
                directionY = -1;
            }
            else // 否则输入输出口都在左边缘
            {
                portX0 = -2;
                portY0 = 0;
                expandX = 0;
                expandY = 2;
                directionX = 1;
                directionY = 0;
            }

            for (int i = 0; i < minZ; i++)
            {
                segLayers.Add(null);
            }
            foreach (var itemPathKV in itemPathInfos)
            {
                BpItemPathInfo itemPath = itemPathKV.Value;
                int itemId = itemPathKV.Key;
                if (!itemPath.isOre)
                {
                    // 首先串联每个生产block产出物线路
                    BlueprintBuilding curTerminal = itemPath.provideProcessors[0].outputBelts[itemId];
                    for (int i = 1; i < itemPath.provideProcessors.Count; i++)
                    {
                        BlueprintBuilding nextInput = itemPath.provideProcessors[i].inputBelts[itemId];
                        //if (!ConnectBeltsIfAdjacent(curTerminal, nextInput))
                        //{
                        //    int layerZ = FindLegalLayer(curTerminal, nextInput);
                        //    if (layerZ < 0)
                        //    {
                        //        Utils.logger.LogWarning("由于可用层数不足，连接失败");
                        //        return false;
                        //    }
                        //    else
                        //    {
                        //        ConnectBeltsInLayer(curTerminal, nextInput, layerZ + 0.5f);
                        //        segLayers[layerZ].AddSegment(curTerminal, nextInput);
                        //    }
                        //}
                        TryConnect(curTerminal, nextInput);
                        curTerminal = itemPath.demandProcessors[i].outputBelts[itemId];
                    }

                    // 然后依次连接所有消耗项
                    for (int i = 0; i < itemPath.demandProcessors.Count; i++)
                    {
                        BlueprintBuilding nextInput = itemPath.demandProcessors[i].inputBelts[itemId];
                        //if (!ConnectBeltsIfAdjacent(curTerminal, nextInput))
                        //{
                        //    int layerZ = FindLegalLayer(curTerminal, nextInput);
                        //    if (layerZ < 0)
                        //    {
                        //        Utils.logger.LogWarning("由于可用层数不足，连接失败");
                        //        return false;
                        //    }
                        //    else
                        //    {
                        //        ConnectBeltsInLayer(curTerminal, nextInput, layerZ + 0.5f);
                        //        segLayers[layerZ].AddSegment(curTerminal, nextInput);
                        //    }
                        //}
                        TryConnect(curTerminal, nextInput);
                        curTerminal = itemPath.demandProcessors[i].outputBelts[itemId];
                    }

                    
                    // 对于目标产物或者溢出产物，需要放置额外的输出口并连接
                    if (targets.ContainsKey(itemId) || solution.itemNodes[itemId].satisfiedSpeed > solution.itemNodes[itemId].needSpeed + 0.0001f) 
                    {
                        int PLSIndex = terminalInfos.Count / BpDB.PLSMaxStorageKinds;
                        int subIndex = terminalInfos.Count % BpDB.PLSMaxStorageKinds;
                        int terminalX = portX0 + (expandX / 2) * BpDB.PLSDistance * PLSIndex + expandX * subIndex;
                        int terminalY = portY0 + (expandY / 2) * BpDB.PLSDistance * PLSIndex + expandY * subIndex;
                        int connectBeltX = terminalX + directionX;
                        int connectBeltY = terminalY + directionY;
                        BlueprintBuilding portTerminal = AddBelt(curTerminal.itemId, terminalX, terminalY, 0, null, null, itemId);
                        BlueprintBuilding connectTerminal = AddBelt(curTerminal.itemId, connectBeltX, connectBeltY, 0, null, portTerminal, 0);
                        TryConnect(curTerminal, connectTerminal);
                        curTerminal = portTerminal;
                        terminalInfos.Add(new BpTerminalInfo(portTerminal, itemId, false));
                    }

                    itemPath.outputTerminal = curTerminal;
                }
                else // 如果是原矿
                {
                    if(itemPath.provideProcessors.Count == 0) // 如果没有任何建筑产出，则单纯地每个获取的端点都从外部进料即可
                    {
                        for (int i = 0; i < itemPath.demandProcessors.Count; i++)
                        {
                            int PLSIndex = terminalInfos.Count / BpDB.PLSMaxStorageKinds;
                            int subIndex = terminalInfos.Count % BpDB.PLSMaxStorageKinds;
                            int terminalX = portX0 + (expandX / 2) * BpDB.PLSDistance * PLSIndex + expandX * subIndex;
                            int terminalY = portY0 + (expandY / 2) * BpDB.PLSDistance * PLSIndex + expandY * subIndex;
                            int connectBeltX = terminalX + directionX;
                            int connectBeltY = terminalY + directionY;
                            BlueprintBuilding blockBeltTerminal = itemPath.demandProcessors[i].inputBelts[itemId];
                            BlueprintBuilding connectTerminal = AddBelt(blockBeltTerminal.itemId, connectBeltX, connectBeltY, 0, null, null, 0);
                            BlueprintBuilding portTerminal = AddBelt(blockBeltTerminal.itemId, terminalX, terminalY, 0, null, connectTerminal, itemId);
                            TryConnect(connectTerminal, blockBeltTerminal);
                            terminalInfos.Add(new BpTerminalInfo(portTerminal, itemId, true));
                        }
                    }
                    else // 又有配方产出，又有原矿输入
                    {

                    }
                }
            }

            return true;
        }


        private bool PostProcess()
        {
            blueprintData.buildings = buildings.ToArray();
            return true;
        }

        private int FindLegalLayer(BlueprintBuilding fromBelt, BlueprintBuilding toBelt)
        {
            Segment segment = new Segment(fromBelt.localOffset_x, fromBelt.localOffset_y, toBelt.localOffset_x, toBelt.localOffset_y);
            int beginZ = minZ;
            if(segLayers.Count <= beginZ)
            {
                while(segLayers.Count < beginZ)
                {
                    segLayers.Add(null);
                }
                segLayers.Add(new BpSegmentLayer(beginZ, minBeltDistance));
            }
            for (int z = beginZ; z < segLayers.Count; z++)
            {
                if (segLayers[z].CollideCheckValid(segment))
                {
                    return z;
                }
            }

            // 到这里说明现有层都有碰撞，则新建一层（前提是没有超出最高层数）
            if (segLayers.Count <= maxZ)
            {
                segLayers.Add(new BpSegmentLayer(segLayers.Count, minBeltDistance));
                return segLayers.Count - 1;
            }
            else
            {
                return -1;
            }

        }

        /// <summary>
        /// 尝试将两个点的带子连接起来，通过直连或高架的方式
        /// </summary>
        /// <param name="fromBelt"></param>
        /// <param name="toBelt"></param>
        /// <returns></returns>
        private bool TryConnect(BlueprintBuilding fromBelt, BlueprintBuilding toBelt)
        {
            if (!ConnectBeltsIfAdjacent(fromBelt, toBelt))
            {
                int layerZ = FindLegalLayer(fromBelt, toBelt);
                if (layerZ < 0)
                {
                    Utils.logger.LogWarning("由于可用层数不足，连接失败");
                    return false;
                }
                else
                {
                    ConnectBeltsInLayer(fromBelt, toBelt, layerZ + 0.5f);
                    segLayers[layerZ].AddSegment(fromBelt, toBelt);
                }
            }
            return true;
        }

        /// <summary>
        /// 如果两个belt就是相邻的，直接连接，不需要架高连接
        /// </summary>
        /// <param name="fromBelt"></param>
        /// <param name="toBelt"></param>
        /// <returns></returns>
        private bool ConnectBeltsIfAdjacent(BlueprintBuilding fromBelt, BlueprintBuilding toBelt)
        {
            float xDis = Math.Abs(fromBelt.localOffset_x - toBelt.localOffset_x);
            float yDis = Math.Abs(fromBelt.localOffset_y - toBelt.localOffset_y);
            float zDis = Math.Abs(fromBelt.localOffset_z - toBelt.localOffset_z);
            if (zDis <= 0.01f)
            {
                if (xDis <= 1.01f && yDis <= 0.01f || xDis <= 0.01f && yDis <= 1.01f)
                {
                    fromBelt.outputToSlot = 1;
                    fromBelt.outputObj = toBelt;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 将两个belt使用高架连接
        /// </summary>
        /// <param name="fromBelt"></param>
        /// <param name="toBelt"></param>
        /// <param name="actualHeight"></param>
        /// <returns></returns>
        private bool ConnectBeltsInLayer(BlueprintBuilding fromBelt, BlueprintBuilding toBelt, float actualHeight)
        {

            if (actualHeight < 0)
            {
                return false;
            }

            int toZ = (int)Math.Round(toBelt.localOffset_z);
            int fromZ = (int)Math.Round(fromBelt.localOffset_z);
            int zBase = (int)actualHeight; // 传送带那层的层高的整数部分
            if (zBase < toZ || zBase < fromZ)
            {
                return false;
            }

            actualHeight = (int)actualHeight + 0.5f; // 传送带那层的层高的实际数值
            short itemId = toBelt.itemId;
            short modelIndex = toBelt.modelIndex;
            BlueprintBuilding last = toBelt;
            float x1 = toBelt.localOffset_x;
            float y1 = toBelt.localOffset_y;
            float x2 = fromBelt.localOffset_x;
            float y2 = fromBelt.localOffset_y;
            // 从toBelt一路向上直到目标层的zBase高度
            for (int z = toZ + 1; z <= zBase; z++)
            {
                BlueprintBuilding b = new BlueprintBuilding();
                b.index = buildings.Count;
                b.areaIndex = 0;
                b.localOffset_x = x1;
                b.localOffset_y = y1;
                b.localOffset_z = z;
                b.localOffset_x2 = b.localOffset_x;
                b.localOffset_y2 = b.localOffset_y;
                b.localOffset_z2 = b.localOffset_z;
                b.inputToSlot = 1;
                b.yaw = 0;
                b.yaw2 = b.yaw;
                b.itemId = itemId;
                b.modelIndex = modelIndex;
                b.outputToSlot = 1;
                b.outputObj = last; // 连接上一个
                b.parameters = new int[0];
                buildings.Add(b);
                //gridMap.SetBuilding((int)Math.Round(b.localOffset_x), (int)Math.Round(b.localOffset_y), b.index);
                last = b;
            }
            // 在层上横跨
            float beltDis = 1.4f;
            float xStep;
            float yStep;
            float stepCount;
            if (Math.Abs(x2 - x1) > 0.01f)
            {
                float k = (y2 - y1) / (x2 - x1);
                xStep = beltDis / (float)Math.Sqrt(1 + k * k);
                if (x2 < x1)
                    xStep = -xStep;
                yStep = xStep * k;
                stepCount = Math.Abs((x2 - x1) / xStep);
            }
            else
            {
                xStep = 0;
                yStep = y2 > y1 ? beltDis : -beltDis;
                stepCount = Math.Abs((y2 - y1) / yStep);
            }
            if (stepCount <= 0.1f)
            {
                Utils.logger.LogWarning("出入口传送带距离太近，无法生成高架连接图");
                return false; // 太近了，无法生成
            }
            int count = 0;
            while(true)
            {
                BlueprintBuilding b = new BlueprintBuilding();
                b.index = buildings.Count;
                b.areaIndex = 0;
                b.localOffset_x = x1 + count * xStep;
                b.localOffset_y = y1 + count * yStep;
                b.localOffset_z = actualHeight;
                b.localOffset_x2 = b.localOffset_x;
                b.localOffset_y2 = b.localOffset_y;
                b.localOffset_z2 = b.localOffset_z;
                b.inputToSlot = 1;
                b.yaw = 0;
                b.yaw2 = b.yaw;
                b.itemId = itemId;
                b.modelIndex = modelIndex;
                b.outputToSlot = 1;
                b.outputObj = last; // 连接上一个
                b.parameters = new int[0];
                buildings.Add(b);
                //gridMap.SetBuilding((int)Math.Round(b.localOffset_x), (int)Math.Round(b.localOffset_y), b.index);
                last = b;

                if (stepCount - count >= 2)
                {
                    count++;
                }
                else // 进入收尾阶段
                {
                    if (stepCount - count > 1) // 则加一个中间点
                    {
                        float actualCount = count + 0.5f * (stepCount - count);
                        BlueprintBuilding b2 = new BlueprintBuilding();
                        b2.index = buildings.Count;
                        b2.areaIndex = 0;
                        b2.localOffset_x = x1 + actualCount * xStep;
                        b2.localOffset_y = y1 + actualCount * yStep;
                        b2.localOffset_z = actualHeight;
                        b2.localOffset_x2 = b2.localOffset_x;
                        b2.localOffset_y2 = b2.localOffset_y;
                        b2.localOffset_z2 = b2.localOffset_z;
                        b2.inputToSlot = 1;
                        b2.yaw = 0;
                        b2.yaw2 = b2.yaw;
                        b2.itemId = itemId;
                        b2.modelIndex = modelIndex;
                        b2.outputToSlot = 1;
                        b2.outputObj = last; // 连接上一个
                        b2.parameters = new int[0];
                        buildings.Add(b2);
                        //gridMap.SetBuilding((int)Math.Round(b.localOffset_x), (int)Math.Round(b.localOffset_y), b.index);
                        last = b2;
                    }

                    // 然后是终点带子
                    BlueprintBuilding b3 = new BlueprintBuilding();
                    b3.index = buildings.Count;
                    b3.areaIndex = 0;
                    b3.localOffset_x = x2;
                    b3.localOffset_y = y2;
                    b3.localOffset_z = actualHeight;
                    b3.localOffset_x2 = b3.localOffset_x;
                    b3.localOffset_y2 = b3.localOffset_y;
                    b3.localOffset_z2 = b3.localOffset_z;
                    b3.inputToSlot = 1;
                    b3.yaw = 0;
                    b3.yaw2 = b3.yaw;
                    b3.itemId = itemId;
                    b3.modelIndex = modelIndex;
                    b3.outputToSlot = 1;
                    b3.outputObj = last; // 连接上一个
                    b3.parameters = new int[0];
                    buildings.Add(b3);
                    //gridMap.SetBuilding((int)Math.Round(b.localOffset_x), (int)Math.Round(b.localOffset_y), b.index);
                    last = b3;
                    break;
                }
            }
            // 然后从目标层的zBase高度一路向下直到fromBelt
            for (int z = zBase; z > fromZ; z--)
            {
                BlueprintBuilding b = new BlueprintBuilding();
                b.index = buildings.Count;
                b.areaIndex = 0;
                b.localOffset_x = x2;
                b.localOffset_y = y2;
                b.localOffset_z = z;
                b.localOffset_x2 = b.localOffset_x;
                b.localOffset_y2 = b.localOffset_y;
                b.localOffset_z2 = b.localOffset_z;
                b.inputToSlot = 1;
                b.yaw = 0;
                b.yaw2 = b.yaw;
                b.itemId = itemId;
                b.modelIndex = modelIndex;
                b.outputToSlot = 1;
                b.outputObj = last; // 连接上一个
                b.parameters = new int[0];
                buildings.Add(b);
                //gridMap.SetBuilding((int)Math.Round(b.localOffset_x), (int)Math.Round(b.localOffset_y), b.index);
                last = b;
            }
            // 最后连接fromBelt
            fromBelt.outputToSlot = 1;
            fromBelt.outputObj = last;

            return true;
        }

        private BlueprintBuilding AddBelt(int beltItemId, float x, float y, float z, BlueprintBuilding fromBelt, BlueprintBuilding toBelt, int icon = 0)
        {
            BlueprintBuilding b = new BlueprintBuilding();
            b.index = buildings.Count;
            b.areaIndex = 0;
            b.localOffset_x = x;
            b.localOffset_y = y;
            b.localOffset_z = z;
            b.localOffset_x2 = b.localOffset_x;
            b.localOffset_y2 = b.localOffset_y;
            b.localOffset_z2 = b.localOffset_z;
            b.inputToSlot = 1;
            b.yaw = 0;
            b.yaw2 = b.yaw;
            b.itemId = (short)beltItemId;
            b.modelIndex = (short)LDB.items.Select(beltItemId).prefabDesc.modelIndex;
            if(toBelt  != null )
            {
                b.outputToSlot = 1;
                b.outputObj = toBelt;
            }
            b.parameters = new int[0];
            if(icon > 0)
            {
                b.parameters = new int[] { icon, 0 };
            }
            buildings.Add(b);
            //gridMap.SetBuilding((int)Math.Round(b.localOffset_x), (int)Math.Round(b.localOffset_y), b.index);
            if(fromBelt != null)
            {
                fromBelt.outputToSlot = 1;
                fromBelt.outputObj = b;
            }
            return b;
        }
    }

    /// <summary>
    /// 黑盒蓝图中，小block按行填充进去，一行可以有一个或多个小蓝图block
    /// </summary>
    public class BpBlockLineInfo
    {
        public BpConnector parent;
        public int height;
        public int width;
        public List<BpBlockProcessor> processors; // 在此行内的蓝图们

        public BpBlockLineInfo(BpConnector connecter)
        {
            parent = connecter;
            height = 0;
            width = 0;
            processors = new List<BpBlockProcessor>();
        }

        // 将processor的蓝图块纳入本行
        public void AddBpBlock(BpBlockProcessor processor)
        {
            width = width + processor.width;
            parent.width = Math.Max(parent.width, width); // 刷新总黑盒的宽度
            int curHeight = processor.height;
            if(curHeight > height) // 如果新加入的block高度超过当前行高，则处理行高和总黑盒的高度
            {
                int extra = curHeight - height;
                height += extra;
                parent.height += extra;
            }
            processors.Add(processor);
        }

        // 返回本行相对于整个黑盒蓝图的宽度，空余出来的宽度
        public int unusedWidth { get { return parent.width - width; } }
    }

    /// <summary>
    /// 黑盒蓝图中，某一种物品的带子全部串联起来的信息
    /// </summary>
    public class BpItemSumInfo
    {
        public int itemId;
        public double needBeltSpeed;
        public int needBeltId;

        public BpItemSumInfo(int itemId, double needBeltSpeed = 0)
        {
            this.itemId = itemId;
            this.needBeltSpeed = needBeltSpeed;
            needBeltId = -1;
        }
    }

    /// <summary>
    /// 黑盒蓝图中，某种物品的所有产出recipe、消耗recipe的processor记录，并在串联起来时用于保存串联信息
    /// </summary>
    public class BpItemPathInfo
    {
        public int itemId;
        public List<BpBlockProcessor> demandProcessors; // 所有需要此物品的processor
        public List<BpBlockProcessor> provideProcessors; // 所有产出此物品的processor

        public BlueprintBuilding outputTerminal;
        public bool isOre; // 视作原矿时，每个require都要从外面单独有一条输入口（如果有配方也产出此物品，则仍需要串联起来，并且每个require都要多加一条外入带，并进串联的主线）

        public BpItemPathInfo(int itemId, bool isOre)
        {
            this.itemId = itemId;
            demandProcessors = new List<BpBlockProcessor>();
            provideProcessors = new List<BpBlockProcessor>();
            outputTerminal = null;
            this.isOre = isOre;
        }

        public void AddDemander(BpBlockProcessor processor)
        {
            demandProcessors.Add(processor);
        }

        public void AddProvider(BpBlockProcessor processor)
        {
            provideProcessors.Add(processor);
        }
    }

    /// <summary>
    /// 用于存储每一层的架空传送带信息
    /// </summary>
    public class BpSegmentLayer
    {
        public int z; // 层高，实际传送带的 z = 此z+0.5
        public List<Segment> segments; // 存储所有在此层横跨、穿梭的传送带的带子的线段，用于检测碰撞
        public float minDistance; // 检测碰撞时要求的最小距离不低于

        public BpSegmentLayer(int z, float minDistance)
        {
            this.z = z;
            this.minDistance = minDistance;
            segments = new List<Segment>();
        }

        public void AddSegment(BlueprintBuilding fromBelt, BlueprintBuilding toBelt)
        {
            AddSegment(fromBelt.localOffset_x, fromBelt.localOffset_y, toBelt.localOffset_x, toBelt.localOffset_y);
        }

        public void AddSegment(Segment segment)
        {
            segments.Add(segment);
        }

        public void AddSegment(float x1, float y1, float x2, float y2)
        {
            segments.Add(new Segment(x1, y1, x2, y2));
        }


        /// <summary>
        /// 检测新来的线段，如果和本层的所有线段都无碰撞且不近，则返回true，说明可以放进本层
        /// </summary>
        /// <param name="segment"></param>
        /// <returns></returns>
        public bool CollideCheckValid(Segment segment)
        {
            for (int i = 0; i < segments.Count; i++)
            {
                if (segments[i].CrossOrNear(segment, minDistance))
                    return false;
            }

            return true;
        }
    }

    /// <summary>
    /// 存储每种需要外入或者最终输出的端点信息
    /// </summary>
    public class BpTerminalInfo
    {
        int itemId;
        bool isDemand;
        int x;
        int y;
        BlueprintBuilding belt;

        public BpTerminalInfo(BlueprintBuilding belt, int itemId, bool isDemand)
        {
            this.belt = belt;
            x = (int)Math.Round(belt.localOffset_x);
            y = (int)Math.Round(belt.localOffset_y);
            this.itemId = itemId;
            this.isDemand = isDemand;
        }
    }
}
