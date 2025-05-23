using DSPCalculator.Bp;
using DSPCalculator.Logic;
using DSPCalculator.UI;
using MathNet.Numerics.Distributions;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.Expando;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DSPCalculator.Bp
{
    public class BpConnector : BpBuildingList
    {
        public static bool enabled = true;

        public UICalcWindow calcWindow;
        public SolutionTree solution;
        public BlueprintData blueprintData;
        public List<BpBlockProcessor> processors;
        public Dictionary<int, BpBlockProcessor> processorsMap; // recipeId到对应的processor映射
        public List<BpBlockProcessor> labProcessors; // 所有使用可堆叠的研究站的蓝图，不放在processorMap里面，而是放在这里面，为了防止交叉走线穿过研究站（特别高），因此所有使用lab的蓝图要左端对齐，统一放在最右侧边缘，这样就会产生高架传送带穿过lab的情况
        public Dictionary<int, BpItemSumInfo> itemSumInfos; // 存储每个Item所有配方的产出或者消耗总和，所需求的带速的最大值，表示潜在的单条带需要承担的最大运力
        public Dictionary<int, BpItemPathInfo> itemPathInfos; // 存储每个Item用于构造串联线路的信息

        public List<BpBlockLineInfo> lines;
        public List<BpSegmentLayer> segLayers; // 所有架空连接的传送带所在层的信息
        public List<BpTerminalInfo> terminalInfos; // 所有向外连接的输入、输出接口的belt信息
        public Dictionary<int, int> additionalInputItems; // 由于部分中间产物的蓝图无法生成（例如星际组装厂解决此配方），所有这个蓝图提供的物品都需要额外的输入口
        public Dictionary<int, int> additionalOutputItems; //  由于部分中间产物的蓝图无法生成，所有这个蓝图需求的物品都要作为额外的输出口
        public Vector2 labBlocksOriginPoint; // 所有lab蓝图集中放置的起始点坐标
        public List<int> needUserBuildOutsideRecipeIds; // 那些需要用户在黑盒蓝图外自行构建的配方（由于分馏或者星际组装厂或者其他导致生成蓝图失败的原因）

        public Dictionary<int, int> proliferatorUsed; // 标记所有用过的增产剂

        public int genLevel;
        public bool orthogonalConnect; // 正交连接
        public bool forcePortOnLeft; // 强制出入口放在左侧而不是上边
        public bool connectCoaters; // 是否生成串联喷涂机入口的带子
        public int priorityPLSTopOrLeft; // -1 = forceLeft;   1 = forceTop;   0 = noForce
        public int width;
        public int height;

        public int maxZTechLimited = 47; // 当前科技允许的传送带最大高度（实际为maxZ + 0.5）
        public int maxZGameLimited = 47; // 整个游戏允许的传送带最大高度（48）
        public int usedZ = 0; // 使用的最大Z高度
        public int minZ = 5; // 允许的传送带最小高度（实际为minZ + 0.5)，会根据黑盒蓝图里面有什么生产建筑决定
        public bool succeeded;

        // 以下为静态量
        public static float minBeltDistance = 0.5f; // 两条带在同一高度时，允许的最近点的距离

        public BpConnector(UICalcWindow calcWindow, int genLevel)
        {
            this.calcWindow = calcWindow;
            solution = calcWindow.solution;
            blueprintData = BpBuilder.CreateEmpty();
            processors = new List<BpBlockProcessor>();
            processorsMap = new Dictionary<int, BpBlockProcessor>();
            labProcessors = new List<BpBlockProcessor>();
            buildings = new List<BlueprintBuilding>();
            PLSs = new List<int>();
            gridMap = new Dictionary<int, Dictionary<int, int>>();
            itemSumInfos = new Dictionary<int, BpItemSumInfo>();
            itemPathInfos = new Dictionary<int, BpItemPathInfo>();
            lines = new List<BpBlockLineInfo>();
            segLayers = new List<BpSegmentLayer>();
            terminalInfos = new List<BpTerminalInfo>();
            additionalInputItems = new Dictionary<int, int>();
            additionalOutputItems = new Dictionary<int, int>();
            labBlocksOriginPoint = Vector2.zero;
            proliferatorUsed = new Dictionary<int, int>();
            needUserBuildOutsideRecipeIds = new List<int>();
            succeeded = GenerateFullBlueprint(genLevel);
        }

        private bool GenerateFullBlueprint(int genLevel = 0, bool forcePortOnLeft = false, bool orthogonalConnect = true)
        {
            if(!GameMain.history.TechUnlocked(1711) && LDB.techs.Select(1711) != null)
            {
                UIMessageBox.Show("蓝图生成失败".Translate(), "未解锁垂直建造传送带科技警告".Translate() + LDB.techs.Select(1711).name, "calc关闭".Translate(), 1, new UIMessageBox.Response(() => { }));
                return false;
            }

            Stopwatch timer = new Stopwatch();
            this.genLevel = genLevel;
            this.priorityPLSTopOrLeft = 0;
            this.forcePortOnLeft = forcePortOnLeft;
            this.connectCoaters = solution.userPreference.bpConnectBlackboxCoater;
            this.orthogonalConnect = orthogonalConnect;
            maxZTechLimited = GetMaxBeltZByCurTech() - 1;
            ResetBp();
            // 判断每种物品，单带运力是否足够
            timer.Start();
            if (!CalcItemSumInfos())
                return false;
            timer.Stop();
            // Utils.logger.LogInfo($"判断每种物品的单带运力是否足够耗时{timer.Elapsed.TotalMilliseconds}ms");


            // 生成所有processor
            timer.Start();
            if (!GenProcessors())
                return false;
            timer.Stop();
            // Utils.logger.LogInfo($"生成processor过程耗时{timer.Elapsed.TotalMilliseconds}ms");


            if (needUserBuildOutsideRecipeIds.Count > 0)
            {
                string recipeNames = "";
                for (int i = 0; i < needUserBuildOutsideRecipeIds.Count; i++)
                {
                    recipeNames += LDB.recipes.Select(needUserBuildOutsideRecipeIds[i])?.name;
                    if (i < needUserBuildOutsideRecipeIds.Count - 1)
                        recipeNames += ", ";
                }
                UIMessageBox.Show("calc提示".Translate(), "部分蓝图无法生成说明".Translate() + recipeNames, "calc确定".Translate(), 1, new UIMessageBox.Response(() => { }));
            }

            // 根据长度降序排序
            timer.Restart();
            processors = processors.OrderByDescending(x => x.width).ToList();
            timer.Stop();
            // Utils.logger.LogInfo($"排序processor过程耗时{timer.Elapsed.TotalMilliseconds}ms");

            //填充行列表
            timer.Restart();
            if (!ArrangeBpBlocks(processors))
                return false;
            timer.Stop();
            // Utils.logger.LogInfo($"排列processor过程耗时{timer.Elapsed.TotalMilliseconds}ms");


            //放置建筑
            timer.Restart();
            if (!PlaceBuildings())
                return false;
            timer.Stop();
            // Utils.logger.LogInfo($"重新放置buildings过程耗时{timer.Elapsed.TotalMilliseconds}ms");


            //放置建筑
            timer.Restart();
            bool connectSucceeded = ConnectBlocks();
            timer.Stop();
            if (!connectSucceeded)
            {
                if(usedZ > maxZGameLimited)
                {
                    UIMessageBox.Show("蓝图生成失败".Translate(), "传送带高度游戏限制说明".Translate(), "calc关闭".Translate(), 1, new UIMessageBox.Response(() => { }));
                }
                else
                {
                    UIMessageBox.Show("蓝图生成失败".Translate(), "Unexpected Error 503", "calc关闭".Translate(), 1, new UIMessageBox.Response(() => { }));
                }
                return false;
            }
            else if (connectSucceeded && !this.orthogonalConnect)
            {
                if (usedZ > maxZTechLimited)
                {
                    // 传送带斜插提示
                    UIMessageBox.Show("calc提示".Translate(), "传送带高度科技限制说明".Translate(), "calc确定".Translate(), 1, new UIMessageBox.Response(() => { }));
                }
                else
                {
                    // 传送带斜插提示
                    UIMessageBox.Show("calc提示".Translate(), "传送带斜插说明".Translate(), "calc确定".Translate(), 1, new UIMessageBox.Response(() => { }));
                }
            }
            // Utils.logger.LogInfo($"连接各个block过程耗时{timer.Elapsed.TotalMilliseconds}ms");

            PostProcess(); // 后处理，完成蓝图构建
            return true;
        }

        private void ResetBp()
        {
            buildings.Clear();
            PLSs.Clear();
            gridMap.Clear();
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
                                UIMessageBox.Show("蓝图生成失败".Translate(), "Unexpected Error 101", "calc关闭".Translate(), 1, new UIMessageBox.Response(() => { }));
                                return false;
                            }
                        }
                        // 非首次计入时，将配方的输出量，除以白爪堆叠数来计算占用的带速
                        itemSumInfos[itemId].needBeltSpeed += recipeInfo.GetOutputSpeedOriProto(i, recipeInfo.count) / outputStack;
                    }

                    // 处理全局传送带最小高度
                    if (BpDB.assemblerInfos.ContainsKey(recipeInfo.assemblerItemId))
                    {
                        int requireBeltMinHeight = BpDB.assemblerInfos[recipeInfo.assemblerItemId].height + 1;
                        minZ = Math.Max(minZ, requireBeltMinHeight);
                    }
                }
            }
            // 如果增产剂并入了产线，但是用户又把增产剂作为了原矿输入（很奇怪但是万一有这样的人呢），那么还要注意增产剂的用量和带子分配
            for (int i = 0; i < CalcDB.proliferatorItemIds.Count; i++)
            {
                int proliferatorItemId = CalcDB.proliferatorItemIds[i];
                if (solution.itemNodes.ContainsKey(proliferatorItemId) && solution.itemNodes[proliferatorItemId].needSpeed > 0.001f)
                {
                    if (!itemSumInfos.ContainsKey(proliferatorItemId))
                        itemSumInfos[proliferatorItemId] = new BpItemSumInfo(proliferatorItemId, solution.itemNodes[proliferatorItemId].needSpeed / rawOreInputStack);
                    else
                        itemSumInfos[proliferatorItemId].needBeltSpeed = Math.Max(itemSumInfos[proliferatorItemId].needBeltSpeed, solution.itemNodes[proliferatorItemId].needSpeed / outputStack); // 这里为什么用outputStack是因为既然已经有这项说明肯定是有生产设施了，那就要以生产设施产出的堆叠为准去计算带子运力需求
                }
            }
            // 没有任何配方产出的item不会在itemSumInfo里面出现，这代表着他们只从原矿输入，无所谓在黑盒的起点处拥有几条带子（可以每个block都拥有独立的输入带，即使对于同一个视为原矿的item）
            // 因此不需要判断“单带”运力是否足够，只需要后续判断单个蓝图是否可以满足即可
            foreach (var sumInfoKV in itemSumInfos)
            {
                BpItemSumInfo sumInfo = sumInfoKV.Value;
                if (sumInfo.needBeltSpeed > solution.beltsAvailable.Last().speedPerMin)
                {
                    // Utils.logger.LogInfo($"由于{Utils.ItemName(sumInfo.itemId)}速度过大{sumInfo.needBeltSpeed}>{solution.beltsAvailable.Last().speedPerMin}");
                    calcWindow.FocusTargetNode(sumInfo.itemId);
                    UIMessageBox.Show("蓝图生成失败".Translate(), "有蓝图传送带运力不够说明".Translate(), "calc关闭".Translate(), 1, new UIMessageBox.Response(() => {
                        calcWindow.FocusTargetNode(sumInfo.itemId);
                    }));
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
            labProcessors.Clear();
            processorsMap.Clear();
            itemPathInfos.Clear();
            proliferatorUsed.Clear();
            needUserBuildOutsideRecipeIds.Clear();
            foreach (var recipeInfoKV in solution.recipeInfos)
            {
                RecipeInfo recipeInfo = recipeInfoKV.Value;
                if (recipeInfo != null && recipeInfo.assemblerCount > 0.0001f)
                {
                    BpBlockProcessor processor = new BpBlockProcessor(recipeInfo, solution, this, 1);
                    if (processor.isGBMega)
                    {
                        processor.processorGB = new BpProcessorGB(processor as BpBlockProcessor);
                        processor.PreProcessGB();
                        minZ = Math.Max(BpDB.GBMegaTowerHeight, minZ);
                    }
                    else
                    {
                        processor.PreProcess();
                    }
                    bool hasBp = false; // 这个processor是否真的生成了蓝图
                    if (processor.bpCountToSatisfy > 1 || processor.bpCountToSatisfy < 0)
                    {
                        //Utils.logger.LogWarning($"生成bp block 过程失败，由于配方{LDB.recipes.Select(recipeInfo.ID).name}的单个蓝图无法承载全部产出。");
                        // 首先高亮有问题的产线
                        int itemId = -1;
                        for (int i = 0; i < recipeInfo.recipeNorm.oriProto.Results.Length; i++)
                        {
                            itemId = recipeInfo.recipeNorm.oriProto.Results[i];
                            if (solution.itemNodes.ContainsKey(itemId) && solution.itemNodes[itemId].mainRecipe != null && solution.itemNodes[itemId].mainRecipe.ID == recipeInfo.ID)
                            {
                                calcWindow.FocusTargetNode(itemId);
                                break;
                            }
                        }
                        UIMessageBox.Show("蓝图生成失败".Translate(), "有蓝图传送带运力不够说明".Translate(), "calc关闭".Translate(), 1, new UIMessageBox.Response(() => {
                            calcWindow.FocusTargetNode(itemId);
                        }));
                        return false;
                    }
                    else
                    {
                        hasBp = processor.GenerateBlueprint(0, connectCoaters);
                        if (hasBp)
                        {
                            bool isLabModel = false;
                            if (BpDB.assemblerInfos.ContainsKey(processor.recipeInfo.assemblerItemId))
                            {
                                isLabModel = BpDB.assemblerInfos[processor.recipeInfo.assemblerItemId].vanillaRecipeType == ERecipeType.Research;
                            }
                            if (!isLabModel)
                            {
                                processors.Add(processor);
                            }
                            else
                            {
                                labProcessors.Add(processor);
                            }
                            processorsMap[recipeInfo.ID] = processor;

                            if (processor.proliferatorId > 0)
                                proliferatorUsed[processor.proliferatorId] = 1;
                        }
                        else
                        {
                            needUserBuildOutsideRecipeIds.Add(recipeInfo.ID);
                        }
                    }
                    if (hasBp && processor.isVanilla6006())
                        hasBp = false; // 对于原始的白糖预制蓝图，没有高架传送带，虽然按照正常蓝图放置即可，但所有它需要的原材料都要有额外的外出口

                    RecipeProto proto = recipeInfo.recipeNorm.oriProto;
                    for (int i = 0; i < proto.Items.Length; i++)
                    {
                        int itemId = proto.Items[i];
                        if (hasBp)
                        {
                            if (!itemPathInfos.ContainsKey(itemId))
                            {
                                itemPathInfos[itemId] = new BpItemPathInfo(itemId, solution.userPreference.IsOre(itemId) && solution.itemNodes[itemId].speedFromOre > 0.001f);
                            }
                            itemPathInfos[itemId].AddDemander(processor);
                        }
                        else // 没生成蓝图的（比如用了星际组装厂），则把他们本来要吞的原材料增加额外的外出端口
                        {
                            additionalOutputItems[itemId] = 1;
                        }
                    }
                    for (int i = 0; i < proto.Results.Length; i++)
                    {
                        int itemId = proto.Results[i];
                        if (hasBp)
                        {
                            if (!itemPathInfos.ContainsKey(itemId))
                            {
                                itemPathInfos[itemId] = new BpItemPathInfo(itemId, solution.userPreference.IsOre(itemId) && solution.itemNodes[itemId].speedFromOre > 0.001f);
                            }
                            itemPathInfos[itemId].AddProvider(processor);
                        }
                        else // 没生成蓝图的（比如用了星际组装厂），则把他们本来要提供的产物，增加额外的外入端口
                        {
                            additionalInputItems[itemId] = 1;
                        }
                    }

                }
            }
            labProcessors = labProcessors.OrderBy(p => p.height).ToList(); // 为了防止原版白糖预制蓝图被放在最下面不好剔除（剔除后非常不工整）
            if (processors.Count <= 0 && labProcessors.Count <= 0)
                return false;

            return true;
        }

        // 构造蓝图排列
        private bool ArrangeBpBlocks(List<BpBlockProcessor> processors)
        {
            if (processors.Count <= 0)
            {
                UIMessageBox.Show("蓝图生成失败".Translate(), "没有任何可以生成蓝图的产线".Translate(), "calc关闭".Translate(), 1, new UIMessageBox.Response(() => { }));
                return false;
            }

            lines.Clear();
            height = 0;
            width = 0;
            BpBlockLineInfo blockLine0 = new BpBlockLineInfo(this);
            blockLine0.AddBpBlock(processors[0]);
            lines.Add(blockLine0);
            int blockCount = processors.Count;
            int terminalMaxX = 0; // 最右侧的传送带连接点的X坐标，专门用于限制研究站蓝图的放置方位
            // 首先处理常规非研究站的蓝图
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
                    if (line.height + 5 >= curHeight && line.unusedWidth >= curWidth && line.height > maxHeight)
                    {
                        maxHeight = line.height;
                        maxHeightLine = j;
                    }
                }

                if (minWidthLine >= 0)
                {
                    terminalMaxX = Math.Max(terminalMaxX, lines[minWidthLine].width + processor.leftTerminalX);
                    lines[minWidthLine].AddBpBlock(processor);
                    continue;
                }
                else if (maxHeightLine >= 0)
                {
                    terminalMaxX = Math.Max(terminalMaxX, lines[maxHeightLine].width + processor.leftTerminalX);
                    lines[maxHeightLine].AddBpBlock(processor);
                    continue;
                }
                else if (minWidthUnconditionLine >= 0)
                {
                    int newWidth = minWidthUncondition + curWidth;
                    int newHeight = height + curHeight;

                    if (newWidth <= newHeight)
                    {
                        terminalMaxX = Math.Max(terminalMaxX, lines[minWidthUnconditionLine].width + processor.leftTerminalX);
                        lines[minWidthUnconditionLine].AddBpBlock(processor);
                    }
                    else
                    {
                        terminalMaxX = Math.Max(terminalMaxX, processor.leftTerminalX);
                        BpBlockLineInfo extraLine = new BpBlockLineInfo(this);
                        extraLine.AddBpBlock(processor);
                        lines.Add(extraLine);
                    }
                }
                else
                {
                    Utils.logger.LogError("在排列蓝图块时出错误，而这种情况不应该发生！");
                    UIMessageBox.Show("蓝图生成失败".Translate(), "Unexpected Error 301", "calc关闭".Translate(), 1, new UIMessageBox.Response(() => { }));
                    return false;
                }
            }
            lines = lines.OrderByDescending(line => line.width).ToList();

            // 然后处理研究站的蓝图该从哪里开始放置
            if (labProcessors.Count > 0 && processors.Count > 0)
            {
                // 所有研究站的蓝图不得比terminalMaxX靠左，并且全部左对齐
                // 首先计算所有lab的最大的宽度
                int maxLabBpWidth = 0;
                int labTotalHeight = 0;
                for (int i = 0; i < labProcessors.Count; i++)
                {
                    maxLabBpWidth = Math.Max(maxLabBpWidth, labProcessors[i].width);
                    labTotalHeight += labProcessors[i].height;
                }
                // 优先查看现有的lines里面有无足够的空间放下所有的lab（现在的lab已经排过序了！），有的话放在空置空间内
                bool finished = false;
                if (height > labTotalHeight)
                {
                    int heightRemaining = height;
                    for (int i = 0; i < lines.Count; i++)
                    {
                        if (heightRemaining < labTotalHeight) // 说明此方法不行，因为剩余的行高总和已经放不下所有的研究站了
                            break;

                        if (lines[i].unusedWidth >= maxLabBpWidth) // 如果某一行的空余宽度足够填充，则设定lab的放置原点，并不再判断后面的方案
                        {
                            finished = true;
                            labBlocksOriginPoint = new Vector2(lines[i].width, height - heightRemaining);
                            break;
                        }
                        heightRemaining -= lines[i].height;
                    }
                }

                // 如果还是没找到，只能将所有lab蓝图放在最右侧
                if (!finished)
                {
                    labBlocksOriginPoint = new Vector2(width, 0);
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
            // 首先是常规蓝图
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

            // 然后是研究站蓝图
            int baseLabX = (int)labBlocksOriginPoint.x;
            int baseLabY = (int)labBlocksOriginPoint.y;
            for (int i = 0; i < labProcessors.Count; i++)
            {
                BpBlockProcessor processor = labProcessors[i];
                int buildingsLen = processor.blueprintData.buildings.Length;
                for (int k = 0; k < buildingsLen; k++)
                {
                    BlueprintBuilding bb = processor.blueprintData.buildings[k];
                    bb.localOffset_x += baseLabX;
                    bb.localOffset_y += baseLabY;
                    bb.localOffset_x2 += baseLabX;
                    bb.localOffset_y2 += baseLabY;
                    bb.index = buildings.Count;
                    buildings.Add(bb);
                }
                baseLabY += labProcessors[i].height;
            }

            return true;
        }

        // 连接所有物料出入口，包括创建向外的输入输出口
        private bool ConnectBlocks()
        {
            // 我发现垂直传送带（z为整数高的节点连接起来的），不会与层高为半层(z=n+0.5)的传送带产生碰撞。因此，所有斜向连接（水平直接连接）都要在n+0.5的高度进行
            segLayers.Clear();
            terminalInfos.Clear();
            usedZ = 0;
            Dictionary<int, int> targets = new Dictionary<int, int>();
            for (int i = 0; i < solution.targets.Count; i++)
            {
                targets[solution.targets[i].itemId] = 1;
            }

            bool succeeded = true;

            // 额外输入输出口的基本信息
            int portX0, portY0, expandX, expandY, directionX, directionY; // expand代表port越来越多时，坐标的bump数值。direction代表一条带子从port输入输出端点，到连接block内的端点的方向
            bool isTopSide;
            // 根据优先级（forceLeft > priority有设定的值 > 优先选取最窄边）
            if (forcePortOnLeft)
                isTopSide = false;
            else if (priorityPLSTopOrLeft > 0)
                isTopSide = true;
            else if (priorityPLSTopOrLeft < 0)
                isTopSide = false;
            else if (width >= height)
                isTopSide = false;
            else
                isTopSide = true;

            if (isTopSide) 
            {
                isTopSide = true;
                portX0 = 1;
                portY0 = height + 2;
                expandX = 2;
                expandY = 0;
                directionX = 0;
                directionY = -1;
            }
            else
            {
                isTopSide = false;
                portX0 = -3;
                portY0 = 1;
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
                    BlueprintBuilding curTerminal = null;
                    // 不是原矿但是强制额外增加了port外入口的话，需要增加输入口
                    if (additionalInputItems.ContainsKey(itemId))
                    {
                        int beltItemId = -1;
                        if (itemPath.provideProcessors.Count > 0)
                            beltItemId = itemPath.provideProcessors[0].inputBelts[itemId].itemId;
                        else if (itemPath.demandProcessors.Count > 0)
                            beltItemId = itemPath.demandProcessors[0].inputBelts[itemId].itemId;

                        if (beltItemId > 0)
                        {
                            int PLSIndex = terminalInfos.Count / BpDB.PLSMaxStorageKinds;
                            int subIndex = terminalInfos.Count % BpDB.PLSMaxStorageKinds;
                            int terminalX = portX0 + (expandX / 2) * BpDB.PLSDistance * PLSIndex + expandX * subIndex;
                            int terminalY = portY0 + (expandY / 2) * BpDB.PLSDistance * PLSIndex + expandY * subIndex;
                            BlueprintBuilding connectTerminal = AddBelt(beltItemId, terminalX + 2 * directionX, terminalY + 2 * directionY, 0, null, null);
                            BlueprintBuilding midBelt = AddBelt(beltItemId, terminalX + directionX, terminalY + directionY, 0, null, connectTerminal);
                            BlueprintBuilding portTerminal = AddBelt(beltItemId, terminalX, terminalY, 0, null, midBelt, itemId);
                            curTerminal = connectTerminal;
                            terminalInfos.Add(new BpTerminalInfo(portTerminal, itemId, true));
                            if (itemPath.provideProcessors.Count > 0)
                            {
                                BlueprintBuilding blockBeltTerminal = itemPath.provideProcessors[0].inputBelts[itemId];
                                succeeded = succeeded && TryConnect(connectTerminal, blockBeltTerminal);
                                curTerminal = itemPath.provideProcessors[0].outputBelts[itemId];
                            }
                        }
                        else
                        {
                            Utils.logger.LogError($"在处理{Utils.ItemName(itemId)}的额外入口时出错，这种情况不应该发生");
                            UIMessageBox.Show("蓝图生成失败".Translate(), "Unexpected Error 501", "calc关闭".Translate(), 1, new UIMessageBox.Response(() => { }));
                            return false;
                        }
                    }
                    else if (itemPath.provideProcessors.Count > 0) // 否则说明一定至少有一个生产配方，或者也有可能因为有蓝buff导致没有生产配方
                    {
                        curTerminal = itemPath.provideProcessors[0].outputBelts[itemId];
                    }

                    // 首先串联每个生产block产出物线路
                    if (itemPath.provideProcessors.Count > 0)
                    {
                        for (int i = 1; i < itemPath.provideProcessors.Count; i++)
                        {
                            BlueprintBuilding nextInput = itemPath.provideProcessors[i].inputBelts[itemId];
                            succeeded = succeeded && TryConnect(curTerminal, nextInput);
                            curTerminal = itemPath.provideProcessors[i].outputBelts[itemId];
                        }
                    }

                    // 然后依次连接所有消耗项
                    for (int i = 0; i < itemPath.demandProcessors.Count; i++)
                    {
                        BlueprintBuilding nextInput = itemPath.demandProcessors[i].inputBelts[itemId];
                        if(curTerminal != null)
                            succeeded = succeeded && TryConnect(curTerminal, nextInput);
                        curTerminal = itemPath.demandProcessors[i].outputBelts[itemId];
                    }


                    // 对于目标产物或者溢出产物，需要放置额外的输出口并连接
                    if (targets.ContainsKey(itemId) || solution.itemNodes[itemId].satisfiedSpeed > solution.itemNodes[itemId].needSpeed + 0.0001f || additionalOutputItems.ContainsKey(itemId))
                    {
                        int PLSIndex = terminalInfos.Count / BpDB.PLSMaxStorageKinds;
                        int subIndex = terminalInfos.Count % BpDB.PLSMaxStorageKinds;
                        int terminalX = portX0 + (expandX / 2) * BpDB.PLSDistance * PLSIndex + expandX * subIndex;
                        int terminalY = portY0 + (expandY / 2) * BpDB.PLSDistance * PLSIndex + expandY * subIndex;
                        BlueprintBuilding portTerminal = AddBelt(curTerminal.itemId, terminalX, terminalY, 0, null, null, itemId);
                        BlueprintBuilding midBelt = AddBelt(curTerminal.itemId, terminalX + directionX, terminalY + directionY, 0, null, portTerminal);
                        BlueprintBuilding connectTerminal = AddBelt(curTerminal.itemId, terminalX + 2 * directionX, terminalY + 2 * directionY, 0, null, midBelt);
                        succeeded = succeeded && TryConnect(curTerminal, connectTerminal);
                        curTerminal = portTerminal;
                        terminalInfos.Add(new BpTerminalInfo(portTerminal, itemId, false));
                        //itemPath.inputTerminal = null; // 只要是有外入口，那么后续就不能再处理他的inputTerminal了，想要获取，统一从terminalInfos里面获取
                    }

                    itemPath.outputTerminal = curTerminal;
                }
                else // 如果是原矿
                {
                    if (itemPath.provideProcessors.Count == 0 && !additionalInputItems.ContainsKey(itemId)) // 如果没有任何建筑产出，则单纯地每个获取的端点都从外部进料即可
                    {
                        for (int i = 0; i < itemPath.demandProcessors.Count; i++)
                        {
                            int PLSIndex = terminalInfos.Count / BpDB.PLSMaxStorageKinds;
                            int subIndex = terminalInfos.Count % BpDB.PLSMaxStorageKinds;
                            int terminalX = portX0 + (expandX / 2) * BpDB.PLSDistance * PLSIndex + expandX * subIndex;
                            int terminalY = portY0 + (expandY / 2) * BpDB.PLSDistance * PLSIndex + expandY * subIndex;
                            BlueprintBuilding blockBeltTerminal = itemPath.demandProcessors[i].inputBelts[itemId];
                            BlueprintBuilding connectTerminal = AddBelt(blockBeltTerminal.itemId, terminalX + 2 * directionX, terminalY + 2 * directionY, 0, null, null);
                            BlueprintBuilding midBelt = AddBelt(blockBeltTerminal.itemId, terminalX + directionX, terminalY + directionY, 0, null, connectTerminal);
                            BlueprintBuilding portTerminal = AddBelt(blockBeltTerminal.itemId, terminalX, terminalY, 0, null, midBelt, itemId);
                            succeeded = succeeded && TryConnect(connectTerminal, blockBeltTerminal);
                            terminalInfos.Add(new BpTerminalInfo(portTerminal, itemId, true));
                        }

                        // 对于增产剂，要特别处理一下输出端口，以备后面使用
                        if (CalcDB.proliferatorItemIds.Contains(itemId) && itemPath.demandProcessors.Count > 0)
                            itemPath.outputTerminal = itemPath.demandProcessors.Last().outputBelts[itemId];
                    }
                    else // 又有配方产出，又有原矿输入
                    {
                        // 首先串联每个生产block产出物线路
                        BlueprintBuilding curTerminal;
                        // 不是原矿但是强制额外增加了port外入口的话，需要增加输入口
                        if (additionalInputItems.ContainsKey(itemId))
                        {
                            int beltItemId = -1;
                            if (itemPath.provideProcessors.Count > 0)
                                beltItemId = itemPath.provideProcessors[0].inputBelts[itemId].itemId;
                            else if (itemPath.demandProcessors.Count > 0)
                                beltItemId = itemPath.demandProcessors[0].inputBelts[itemId].itemId;

                            if (beltItemId > 0)
                            {
                                int PLSIndex = terminalInfos.Count / BpDB.PLSMaxStorageKinds;
                                int subIndex = terminalInfos.Count % BpDB.PLSMaxStorageKinds;
                                int terminalX = portX0 + (expandX / 2) * BpDB.PLSDistance * PLSIndex + expandX * subIndex;
                                int terminalY = portY0 + (expandY / 2) * BpDB.PLSDistance * PLSIndex + expandY * subIndex;
                                BlueprintBuilding connectTerminal = AddBelt(beltItemId, terminalX + 2 * directionX, terminalY + 2 * directionY, 0, null, null);
                                BlueprintBuilding midBelt = AddBelt(beltItemId, terminalX + directionX, terminalY + directionY, 0, null, connectTerminal);
                                BlueprintBuilding portTerminal = AddBelt(beltItemId, terminalX, terminalY, 0, null, midBelt, itemId);
                                curTerminal = connectTerminal;
                                terminalInfos.Add(new BpTerminalInfo(portTerminal, itemId, true));
                                if (itemPath.provideProcessors.Count > 0)
                                {
                                    BlueprintBuilding blockBeltTerminal = itemPath.provideProcessors[0].inputBelts[itemId];
                                    succeeded = succeeded && TryConnect(connectTerminal, blockBeltTerminal);
                                    curTerminal = itemPath.provideProcessors[0].outputBelts[itemId];
                                }
                            }
                            else
                            {
                                Utils.logger.LogError($"在处理{Utils.ItemName(itemId)}的额外入口时出错，这种情况不应该发生");
                                UIMessageBox.Show("蓝图生成失败".Translate(), "Unexpected Error 502", "calc关闭".Translate(), 1, new UIMessageBox.Response(() => { }));
                                return false;
                            }
                        }
                        else // 否则说明一定至少有一个生产配方
                        {
                            curTerminal = itemPath.provideProcessors[0].outputBelts[itemId];
                        }
                        for (int i = 1; i < itemPath.provideProcessors.Count; i++)
                        {
                            BlueprintBuilding nextInput = itemPath.provideProcessors[i].inputBelts[itemId];
                            succeeded = succeeded && TryConnect(curTerminal, nextInput);
                            curTerminal = itemPath.provideProcessors[i].outputBelts[itemId];
                        }

                        // 对于每个输入，都将之前的带子作为主路输入，并加入一个旁路的外入（视为原矿输入）入口
                        for (int i = 0; i < itemPath.demandProcessors.Count; i++)
                        {
                            int PLSIndex = terminalInfos.Count / BpDB.PLSMaxStorageKinds;
                            int subIndex = terminalInfos.Count % BpDB.PLSMaxStorageKinds;
                            int terminalX = portX0 + (expandX / 2) * BpDB.PLSDistance * PLSIndex + expandX * subIndex;
                            int terminalY = portY0 + (expandY / 2) * BpDB.PLSDistance * PLSIndex + expandY * subIndex;
                            BlueprintBuilding endBelt = AddBelt(curTerminal.itemId, terminalX + expandX / 2 + 2 * directionX, terminalY + expandY / 2 + 2 * directionY, 0, null, null);
                            BlueprintBuilding convergeBelt = AddBelt(curTerminal.itemId, terminalX + expandX / 2 + directionX, terminalY + expandY / 2 + directionY, 0, null, endBelt);
                            BlueprintBuilding beginBelt = AddBelt(curTerminal.itemId, terminalX + expandX / 2, terminalY + expandY / 2, 0, null, convergeBelt);
                            BlueprintBuilding bypassInputBelt = AddBelt(curTerminal.itemId, terminalX + directionX, terminalY + directionY, 0, null, convergeBelt, 0, 2);
                            BlueprintBuilding portTerminal = AddBelt(curTerminal.itemId, terminalX, terminalY, 0, null, bypassInputBelt, itemId);

                            succeeded = succeeded && TryConnect(curTerminal, beginBelt);
                            succeeded = succeeded && TryConnect(endBelt, itemPath.demandProcessors[i].inputBelts[itemId]);
                            curTerminal = itemPath.demandProcessors[i].outputBelts[itemId];
                            terminalInfos.Add(new BpTerminalInfo(portTerminal, itemId, true));
                        }

                        // 视为原矿也有可能溢出哦，也需要放置额外的输出口并连接
                        if (solution.itemNodes[itemId].satisfiedSpeed > solution.itemNodes[itemId].needSpeed + 0.0001f || additionalOutputItems.ContainsKey(itemId))
                        {
                            int PLSIndex = terminalInfos.Count / BpDB.PLSMaxStorageKinds;
                            int subIndex = terminalInfos.Count % BpDB.PLSMaxStorageKinds;
                            int terminalX = portX0 + (expandX / 2) * BpDB.PLSDistance * PLSIndex + expandX * subIndex;
                            int terminalY = portY0 + (expandY / 2) * BpDB.PLSDistance * PLSIndex + expandY * subIndex;
                            BlueprintBuilding portTerminal = AddBelt(curTerminal.itemId, terminalX, terminalY, 0, null, null, itemId);
                            BlueprintBuilding midBelt = AddBelt(curTerminal.itemId, terminalX + directionX, terminalY + directionY, 0, null, portTerminal);
                            BlueprintBuilding connectTerminal = AddBelt(curTerminal.itemId, terminalX + 2 * directionX, terminalY + 2 * directionY, 0, null, midBelt);
                            succeeded = succeeded && TryConnect(curTerminal, connectTerminal);
                            curTerminal = portTerminal;
                            terminalInfos.Add(new BpTerminalInfo(portTerminal, itemId, false));
                        }

                    }
                }

            }

            // 最后连接所有喷涂机的增产剂进出口
            if (connectCoaters)
            {
                int pBeltItemId = solution.beltsAvailable.Last().itemId;
                bool solveProliferators = solution.userPreference.solveProliferators;
                foreach (var proliferatorKV in proliferatorUsed)
                {
                    // 对于每个增产剂，首先找到terminal，然后依次连接。根据设定不同，找terminal的方法不同
                    int proliferatorItemId = proliferatorKV.Key;
                    bool isOre = solution.userPreference.IsOre(proliferatorItemId);
                    BlueprintBuilding terminal;
                    if (!solveProliferators || (solveProliferators && !itemPathInfos.ContainsKey(proliferatorItemId))) // 如果没有将增产剂并入产线，说明增产剂一定是外入的。或者并入了，但是没有产出增产剂的产线记录，说明被视为原矿了
                    {
                        int PLSIndex = terminalInfos.Count / BpDB.PLSMaxStorageKinds;
                        int subIndex = terminalInfos.Count % BpDB.PLSMaxStorageKinds;
                        int terminalX = portX0 + (expandX / 2) * BpDB.PLSDistance * PLSIndex + expandX * subIndex;
                        int terminalY = portY0 + (expandY / 2) * BpDB.PLSDistance * PLSIndex + expandY * subIndex;
                        BlueprintBuilding connectTerminal = AddBelt(pBeltItemId, terminalX + 2 * directionX, terminalY + 2 * directionY, 0, null, null);
                        BlueprintBuilding midBelt = AddBelt(pBeltItemId, terminalX + directionX, terminalY + directionY, 0, null, connectTerminal);
                        BlueprintBuilding portTerminal = AddBelt(pBeltItemId, terminalX, terminalY, 0, null, midBelt, proliferatorItemId);
                        terminalInfos.Add(new BpTerminalInfo(portTerminal, proliferatorItemId, true));
                        terminal = connectTerminal;
                    }
                    else // 否则，一定有产线生产它，那么一定有outputTerminal可用
                    {
                        BpItemPathInfo itemPath = itemPathInfos[proliferatorItemId];
                        // 如果有外入需求
                        if (solution.itemNodes[proliferatorItemId].speedFromOre > 0.001f)
                        {
                            // 这时就要判断这条线路是不是已经有过外入了
                            bool hasInputTerminal = false;
                            for (int i = 0; i < terminalInfos.Count; i++)
                            {
                                if (terminalInfos[i].itemId == proliferatorItemId)
                                {
                                    hasInputTerminal = true;
                                    break;
                                }
                            }
                            if (!hasInputTerminal) // 如果没有外入
                            {
                                int PLSIndex = terminalInfos.Count / BpDB.PLSMaxStorageKinds;
                                int subIndex = terminalInfos.Count % BpDB.PLSMaxStorageKinds;
                                int terminalX = portX0 + (expandX / 2) * BpDB.PLSDistance * PLSIndex + expandX * subIndex;
                                int terminalY = portY0 + (expandY / 2) * BpDB.PLSDistance * PLSIndex + expandY * subIndex;
                                BlueprintBuilding connectTerminal = AddBelt(pBeltItemId, terminalX + 2 * directionX, terminalY + 2 * directionY, 0, null, null);
                                BlueprintBuilding midBelt = AddBelt(pBeltItemId, terminalX + directionX, terminalY + directionY, 0, null, connectTerminal);
                                BlueprintBuilding portTerminal = AddBelt(pBeltItemId, terminalX, terminalY, 0, null, midBelt, proliferatorItemId);
                                terminalInfos.Add(new BpTerminalInfo(portTerminal, proliferatorItemId, true));
                                if (itemPath.provideProcessors.Count > 0)
                                {
                                    BlueprintBuilding blockBeltTerminal = itemPath.provideProcessors[0].inputBelts[proliferatorItemId];
                                    succeeded = succeeded && TryConnect(connectTerminal, blockBeltTerminal);
                                }
                            }
                        }

                        terminal = itemPath.outputTerminal;
                    }

                    // 然后将获取到的terminal依次串联所有喷涂机出入口
                    if (terminal == null)
                    {
                        Utils.logger.LogWarning($"terminal is null");
                    }
                    for (int i = 0; i < lines.Count; i++)
                    {
                        BpBlockLineInfo line = lines[i];
                        for (int j = 0; j < line.processors.Count; j++)
                        {
                            BpBlockProcessor processor = line.processors[j];
                            if (processor.proliferatorInputBelts.ContainsKey(proliferatorItemId))
                            {
                                succeeded = succeeded && TryConnect(terminal, processor.proliferatorInputBelts[proliferatorItemId]);
                                terminal = processor.proliferatorOutputBelts[proliferatorItemId];
                            }
                        }
                    }
                }
            }
            if(!succeeded && orthogonalConnect) // 如果没成功，那么可能是由于高度不够导致的连接失败，则取消正交连接的限制，改为直接斜线连接过去进行尝试
            {
                this.orthogonalConnect = false;
                //Utils.logger.LogInfo("由于正交连接失败，改为斜线直连");
                CalcItemSumInfos();
                GenProcessors();
                processors = processors.OrderByDescending(x => x.width).ToList();
                ArrangeBpBlocks(processors);
                PlaceBuildings();
                return ConnectBlocks();
                
            }
            bool needToRegenerate = false;

            // 如果优先放在短边的策略导致了超出对应宽度有点太多了，则改为优先放在长的那边
            if (priorityPLSTopOrLeft == 0)
            {
                int PLSCount = terminalInfos.Count / BpDB.PLSMaxStorageKinds + 1;
                if (terminalInfos.Count % BpDB.PLSMaxStorageKinds == 0)
                    PLSCount--;
                int PLSNeed = (PLSCount - 1) * BpDB.PLSDistance + 9;
                if (terminalInfos.Count % BpDB.PLSMaxStorageKinds == 0 && terminalInfos.Count > 0)
                    PLSNeed++;
                if (isTopSide && (PLSNeed > width + 4 || (PLSNeed > width && height <= PLSNeed + 8)))  // 超出过多，或者超出一点，而另一边恰好正好
                {
                    priorityPLSTopOrLeft = -1;
                    needToRegenerate = true;
                }
                else if (!isTopSide && (labProcessors.Count == 0 || !portXExceedLabTerminal) && (PLSNeed > height + 4 || (PLSNeed > height && width <= PLSNeed + 8)))
                {
                    priorityPLSTopOrLeft = 1;
                    needToRegenerate = true;
                }
            }
            if (expandX > 0 && labProcessors.Count > 0) // 如果是在上边缘向右逐步部署port，且有lab，则需要注意port是否超出了lab的terminal的X，超出有可能产生碰撞，这时候需要以强制在左侧生成port的要求重新生成一遍蓝图
            {
                if (portXExceedLabTerminal)
                {
                    this.forcePortOnLeft = true;
                    //Utils.logger.LogInfo("由于port超出了lab的X，强制在左侧生成port");
                    needToRegenerate = true;
                }
            }
            

            // 如果因任何原因需要重新生成，则重新执行前面的操作
            if(needToRegenerate)
            {
                ResetBp();
                CalcItemSumInfos();
                GenProcessors();
                processors = processors.OrderByDescending(x => x.width).ToList();
                ArrangeBpBlocks(processors);
                PlaceBuildings();
                return ConnectBlocks();
            }

            if(genLevel > 0)
            {
                CreatePLSs(isTopSide);
            }

            return true;
        }


        public bool CreatePLSs(bool isTopSide)
        {
            gridMap.Clear();
            PLSs.Clear();
            int portTerminalCnt = terminalInfos.Count;
            int PLSIter = portTerminalCnt / BpDB.PLSMaxStorageKinds;
            int expandX, expandY, beginX, beginY;
            if(isTopSide)
            {
                beginX = 4;
                beginY = height + 6;
                expandX = 1;
                expandY = 0;
            }
            else
            {
                beginX = -7;
                beginY = 4;
                expandX = 0;
                expandY = 1;
            }
            for (int i = 0; i <= PLSIter ; i++)
            {
                if (i * BpDB.PLSMaxStorageKinds >= portTerminalCnt)
                    break;

                // 首先在正确位置创建PLS
                int x = beginX + i * BpDB.PLSDistance * expandX;
                int y = beginY + i * BpDB.PLSDistance * expandY;
                int plsBuildingListIndex = this.AddPLS(x, y);

                for (int j = 0; j < BpDB.PLSMaxStorageKinds; j++)
                {
                    int index = i * BpDB.PLSMaxStorageKinds + j;
                    if(index >= portTerminalCnt)
                    {
                        break;
                    }
                    BpTerminalInfo terminalInfo = terminalInfos[index];
                    if (j == 1 || j == 2)
                    {
                        int slotNum = j == 1 ? 6 : 8;
                        if (!isTopSide)
                            slotNum = j == 1 ? 9 : 11;
                        int storageIndex;
                        this.SetOrGetPLSStorage(plsBuildingListIndex, terminalInfo.itemId, terminalInfo.isDemand, out storageIndex);
                        this.ConnectPLSToBelt(plsBuildingListIndex, slotNum, terminalInfo.isDemand ? storageIndex : -1, terminalInfo.belt.index);
                    }
                    else if (j == 0 || j == 3)
                    {
                        int finalBeltId;
                        List<int> xs;
                        List<int> ys;
                        if (isTopSide)
                        {
                            ys = new List<int> { y - 1, y - 2, y - 3, y - 3 };
                            if (j == 0)
                                xs = new List<int> { x - 4, x - 4, x - 4, x - 3 };
                            else
                                xs = new List<int> { x + 4, x + 4, x + 4, x + 3 };
                        }
                        else
                        {
                            xs = new List<int> { x + 1, x + 2, x + 3, x + 3 };
                            if (j == 0)
                                ys = new List<int> { y - 4, y - 4, y - 4, y - 3 };
                            else
                                ys = new List<int> { y + 4, y + 4, y + 4, y + 3 };

                        }
                        int beginIdx = 0;
                        int inc = 1;
                        if(terminalInfo.isDemand)
                        {
                            beginIdx = 3;
                            inc = -1;
                        }
                        BlueprintBuilding lastBelt = null;
                        for (int k = 0; k < 4; k++)
                        {
                            int actualListIdx = beginIdx + k * inc;
                            int actualX = xs[actualListIdx];
                            int actualY = ys[actualListIdx];
                            BlueprintBuilding b;
                            if (k == 0 && terminalInfo.isDemand)
                                b = AddBelt(terminalInfo.belt.itemId, actualX, actualY, 0, null, terminalInfo.belt);
                            else if (k == 3 && !terminalInfo.isDemand)
                                b = AddBelt(terminalInfo.belt.itemId, actualX, actualY, 0, terminalInfo.belt, lastBelt);
                            else
                                b = AddBelt(terminalInfo.belt.itemId, actualX, actualY, 0, null, lastBelt);

                            lastBelt = b;
                        }
                        int waitingToConnectPLS = gridMap.GetBuilding(xs[0], ys[0]);
                        int slotNum = j == 0 ? 5 : 9;
                        if (!isTopSide)
                            slotNum = j == 0 ? 8 : 0;

                        int storageIndex;
                        this.SetOrGetPLSStorage(plsBuildingListIndex, terminalInfo.itemId, terminalInfo.isDemand, out storageIndex);

                        this.ConnectPLSToBelt(plsBuildingListIndex, slotNum, terminalInfo.isDemand ? storageIndex : -1, waitingToConnectPLS);

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fromBelt"></param>
        /// <param name="toBelt"></param>
        /// <param name="changeXFirst">在连接传送带时，从toBelt向fromBelt连接，先在X方向上创建直线传送带的是changeXFirst，代表着创建传送带方向先change谁，而不是传送带流向</param>
        /// <returns></returns>
        private int FindLegalLayer(BlueprintBuilding fromBelt, BlueprintBuilding toBelt, out bool changeXFirst, out bool isDirectlyConnect)
        {
            changeXFirst = false;
            isDirectlyConnect = IsDirectlyConnect(fromBelt, toBelt);
            int beginZ = minZ;
            usedZ = Math.Max(usedZ, minZ);
            if (segLayers.Count <= beginZ)
            {
                while (segLayers.Count < beginZ)
                {
                    segLayers.Add(null);
                }
                segLayers.Add(new BpSegmentLayer(beginZ, minBeltDistance));
            }
            Segment segment = new Segment(fromBelt.localOffset_x, fromBelt.localOffset_y, toBelt.localOffset_x, toBelt.localOffset_y);
            Segment xFirst1 = new Segment(toBelt.localOffset_x, toBelt.localOffset_y, fromBelt.localOffset_x, toBelt.localOffset_y); // 从toBelt开始先在x方向上摆放传送带，再在y方向上连接到fromBelt（因为创建传送带的方向和传送带自身流向相反）
            Segment xFirst2 = new Segment(fromBelt.localOffset_x, toBelt.localOffset_y, fromBelt.localOffset_x, fromBelt.localOffset_y);
            Segment yFirst1 = new Segment(toBelt.localOffset_x, toBelt.localOffset_y, toBelt.localOffset_x, fromBelt.localOffset_y); // 先在y方向上
            Segment yFirst2 = new Segment(toBelt.localOffset_x, fromBelt.localOffset_y, fromBelt.localOffset_x, fromBelt.localOffset_y);
            for (int z = beginZ; z < segLayers.Count; z++)
            {
                if (isDirectlyConnect) // 直连，只需要判断斜线
                {
                    if (segLayers[z].CollideCheckValid(segment))
                    {
                        return z;
                    }
                }
                else
                {
                    if (segLayers[z].CollideCheckValid(xFirst1) && segLayers[z].CollideCheckValid(xFirst2))
                    {
                        changeXFirst = true;
                        return z;
                    }
                    else if (segLayers[z].CollideCheckValid(yFirst1) && segLayers[z].CollideCheckValid(yFirst2))
                    {
                        changeXFirst = false;
                        return z;
                    }
                }
            }

            // 到这里说明现有层都有碰撞，则新建一层（前提是没有超出最高层数），如果已经是非正交情况了，则超出游戏科技限制也行，但是也不能超出游戏的最高限制
            if (segLayers.Count <= maxZTechLimited || (segLayers.Count <= maxZGameLimited && !orthogonalConnect))
            {
                segLayers.Add(new BpSegmentLayer(segLayers.Count, minBeltDistance));
                return segLayers.Count - 1;
            }
            else
            {
                return -1;
            }
            
        }

        private bool IsDirectlyConnect(BlueprintBuilding fromBelt, BlueprintBuilding toBelt)
        {
            if (!orthogonalConnect)
                return true;
            if (Math.Abs(fromBelt.localOffset_x - toBelt.localOffset_x) < 0.5f || Math.Abs(fromBelt.localOffset_y - toBelt.localOffset_y) < 0.5f)
                return true;

            return false;
        }

        /// <summary>
        /// 尝试将两个点的带子连接起来，通过直连或高架的方式
        /// </summary>
        /// <param name="fromBelt"></param>
        /// <param name="toBelt"></param>
        /// <returns></returns>
        private bool TryConnect(BlueprintBuilding fromBelt, BlueprintBuilding toBelt)
        {
            if (fromBelt == null || toBelt == null)
                return false;

            if (!ConnectBeltsIfAdjacent(fromBelt, toBelt))
            {
                bool changeXFirst;
                bool isDirectlyConnect;
                int layerZ = FindLegalLayer(fromBelt, toBelt, out changeXFirst, out isDirectlyConnect);
                usedZ = Math.Max(usedZ, layerZ);
                if (layerZ < 0)
                {
                    //Utils.logger.LogWarning("由于可用层数不足，连接失败");
                    return false;
                }
                else
                {
                    if (isDirectlyConnect)
                    {
                        ConnectBeltsInLayer(fromBelt, toBelt, layerZ + 0.5f);
                        segLayers[layerZ].AddSegmentDirect(fromBelt, toBelt);
                    }
                    else // 正交
                    {
                        ConnectBeltsOrthogonalInLayer(fromBelt, toBelt, layerZ + 0.5f, changeXFirst);
                        if(changeXFirst)
                        {
                            segLayers[layerZ].AddSegment(toBelt.localOffset_x, toBelt.localOffset_y, fromBelt.localOffset_x, toBelt.localOffset_y);
                            segLayers[layerZ].AddSegment(fromBelt.localOffset_x, toBelt.localOffset_y, fromBelt.localOffset_x, fromBelt.localOffset_y);
                        }
                        else
                        {
                            segLayers[layerZ].AddSegment(toBelt.localOffset_x, toBelt.localOffset_y, toBelt.localOffset_x, fromBelt.localOffset_y);
                            segLayers[layerZ].AddSegment(toBelt.localOffset_x, fromBelt.localOffset_y, fromBelt.localOffset_x, fromBelt.localOffset_y);
                        }
                    }
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
            while (true)
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

        /// <summary>
        /// 目前对fromBelt或者toBelt在非整数格点的情况的处理可能会有问题
        /// </summary>
        /// <param name="fromBelt"></param>
        /// <param name="toBelt"></param>
        /// <param name="actualHeight"></param>
        /// <param name="changeXFirst"></param>
        /// <returns></returns>
        private bool ConnectBeltsOrthogonalInLayer(BlueprintBuilding fromBelt, BlueprintBuilding toBelt, float actualHeight, bool changeXFirst)
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
            // 在层上正交连接
            float inflectionX, inflectionY;
            int stepCount1, stepCount2, bumpX1, bumpY1, bumpX2, bumpY2;
            if (changeXFirst)
            {
                inflectionX = fromBelt.localOffset_x;
                inflectionY = toBelt.localOffset_y;
                bumpX1 = inflectionX - toBelt.localOffset_x > 0 ? 1 : -1;
                bumpY1 = 0;
                bumpX2 = 0;
                bumpY2 = fromBelt.localOffset_y - inflectionY > 0 ? 1 : -1;
                stepCount1 = (int)Math.Round(Math.Abs(fromBelt.localOffset_x - toBelt.localOffset_x));
                stepCount2 = (int)Math.Round(Math.Abs(fromBelt.localOffset_y - toBelt.localOffset_y));
            }
            else
            {
                inflectionX = toBelt.localOffset_x;
                inflectionY = fromBelt.localOffset_y;
                bumpX1 = 0;
                bumpY1 = inflectionY - toBelt.localOffset_y > 0 ? 1 : -1;
                bumpX2 = fromBelt.localOffset_x - inflectionX > 0 ? 1 : -1;
                bumpY2 = 0;
                stepCount1 = (int)Math.Round(Math.Abs(fromBelt.localOffset_y - toBelt.localOffset_y));
                stepCount2 = (int)Math.Round(Math.Abs(fromBelt.localOffset_x - toBelt.localOffset_x));
            }

            // step1
            for (int i = 0; i <= stepCount1; i++) // 这里的边界条件是等于哦
            {
                BlueprintBuilding b = new BlueprintBuilding();
                b.index = buildings.Count;
                b.areaIndex = 0;
                b.localOffset_x = toBelt.localOffset_x + bumpX1 * i;
                b.localOffset_y = toBelt.localOffset_y + bumpY1 * i;
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
                last = b;
            }
            // step2
            for (int i = 0; i <= stepCount2; i++)
            {
                BlueprintBuilding b = new BlueprintBuilding();
                b.index = buildings.Count;
                b.areaIndex = 0;
                b.localOffset_x = inflectionX + bumpX2 * i;
                b.localOffset_y = inflectionY + bumpY2 * i;
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
                last = b;
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
        /// <summary>
        /// 将两个belt使用高架连接
        /// </summary>
        /// <param name="beltItemId"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="fromBelt"></param>
        /// <param name="toBelt"></param>
        /// <param name="icon"></param>
        /// <param name="inputAsByPass">如果是旁路输入的传送带，需要为2甚至3，非旁路则是1</param>
        /// <returns></returns>
        private BlueprintBuilding AddBelt(int beltItemId, float x, float y, float z, BlueprintBuilding fromBelt, BlueprintBuilding toBelt, int icon = 0, int inputAsByPass = 1)
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
            if (toBelt != null)
            {
                b.outputToSlot = inputAsByPass;
                b.outputObj = toBelt;
            }
            b.parameters = new int[0];
            if (icon > 0)
            {
                b.parameters = new int[] { icon, 0 };
            }
            buildings.Add(b);
            gridMap.SetBuilding((int)Math.Round(b.localOffset_x), (int)Math.Round(b.localOffset_y), b.index);
            if (fromBelt != null)
            {
                fromBelt.outputToSlot = 1;
                fromBelt.outputObj = b;
            }
            return b;
        }

        /// <summary>
        /// 判断当前所有对外输入输出口是否超出了lab的所有连接端口的X（这是为了防止高架传送带穿过叠层的lab），如果是，则强制使用在左侧生成port的方法重新生成一遍蓝图
        /// </summary>
        private bool portXExceedLabTerminal
        {
            get
            {
                int PLSIndex = (terminalInfos.Count - 1) / BpDB.PLSMaxStorageKinds;
                int subIndex = (terminalInfos.Count - 1) % BpDB.PLSMaxStorageKinds;
                int terminalX = BpDB.PLSDistance * PLSIndex + 2 * subIndex + 1;

                return terminalX + 1 > labBlocksOriginPoint.x;
            }
        }

        public int GetMaxBeltZByCurTech()
        {
            if (GameMain.history.TechUnlocked(3706))
                return 48;
            else if (GameMain.history.TechUnlocked(3705))
                return 38;
            else if (GameMain.history.TechUnlocked(3704))
                return 32;
            else if (GameMain.history.TechUnlocked(3703))
                return 26;
            else if (GameMain.history.TechUnlocked(3702))
                return 20;
            else if (GameMain.history.TechUnlocked(3701))
                return 14;

            return 8;
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
        //public BlueprintBuilding inputTerminal;
        public BlueprintBuilding outputTerminal;
        public bool isOre; // 视作原矿时，每个require都要从外面单独有一条输入口（如果有配方也产出此物品，则仍需要串联起来，并且每个require都要多加一条外入带，并进串联的主线）

        public BpItemPathInfo(int itemId, bool isOre)
        {
            this.itemId = itemId;
            demandProcessors = new List<BpBlockProcessor>();
            provideProcessors = new List<BpBlockProcessor>();
            //inputTerminal = null;
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

        public void AddSegmentDirect(BlueprintBuilding fromBelt, BlueprintBuilding toBelt)
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
                {
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// 存储每种需要外入或者最终输出的端点信息
    /// </summary>
    public class BpTerminalInfo
    {
        public int itemId;
        public bool isDemand;
        public int x;
        public int y;
        public BlueprintBuilding belt;

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
