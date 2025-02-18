using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEngine.PostProcessing.ScreenSpaceReflectionComponent;

namespace DSPCalculator.BP
{
    public static class BpBuilder
    {
        // outputSlot = 1才能连接到下一个Belt
        public static void AddBelts(this BpProcessor processor, int itemId, float startX, float startY, float startZ, float endX, float endY, float endZ, int connectTo = -1, int beginIcon = 0, int endIcon = 0)
        {
            ref List<BlueprintBuilding> list = ref processor.buildings;
            ref Dictionary<int, Dictionary<int, int>> gridMap = ref processor.gridMap;

            int bumpY = 0;
            if (endY > startY)
                bumpY = -1;
            else if (endY < startY)
                bumpY = 1;

            int bumpX = 0;
            if(endX > startX)
                bumpX = -1;
            else if (endX < startX)
                bumpX = 1;

            int bumpZ = 0;
            if (endZ > startZ)
                bumpZ = -1;
            else if (endZ < startZ)
                bumpZ = 1;

            short itemShort = (short)itemId;
            short modelIndex = (short)LDB.items.Select(itemId).prefabDesc.modelIndex;

            // 带子先垂直后水平，所以逆向建造是先建造水平的后建造垂直的
            if (bumpX != 0)
            {
                int count = (int)((startX - endX) / bumpX) + 1; // 这里要带头带尾，所以要+1
                int yaw = bumpX > 0 ? 270 : 90;
                for (int i = 0; i < count; i++)
                {
                    BlueprintBuilding b = new BlueprintBuilding();
                    b.index = list.Count;
                    b.areaIndex = 0;
                    b.localOffset_x = endX;
                    b.localOffset_y = endY;
                    b.localOffset_z = endZ;
                    b.localOffset_x2 = b.localOffset_x;
                    b.localOffset_y2 = b.localOffset_y;
                    b.localOffset_z2 = b.localOffset_z;
                    b.inputToSlot = 1;
                    b.yaw = yaw;
                    b.yaw2 = b.yaw;
                    b.itemId = itemShort;
                    b.modelIndex = modelIndex;
                    if(i != 0 || connectTo >= 0) // 最末尾的传送带（第一个建立的传送带）没有后继带子，除非有强制连接的带子
                    {
                        b.outputToSlot = 1;
                        if (connectTo >= 0)
                        {
                            b.outputObj = list[connectTo];
                            connectTo = -1; // 后续不需要再识别connectTo
                        }
                        else
                            b.outputObj = list[list.Count - 1]; // 连接上一个
                    }
                    if(i == count -1)
                    {
                        // 这里yaw如果后续有垂直连接的话，应该是45度、135度、225度或者315度，但是蓝图粘贴后会自己调整，所以没事
                    }
                    b.parameters = new int[0];
                    if(i == 0 && endIcon > 0)
                    {
                        b.parameters = new int[] { endIcon, 0 };
                        endIcon = 0;
                    }
                    list.Add(b);
                    if (b.localOffset_z == 0)
                        gridMap.SetBuilding((int)b.localOffset_x, (int)b.localOffset_y, b.index);
                    endX += bumpX;
                }
                endX -= bumpX; // 把末尾的移到下一个坐标移回来
            }
            if(bumpY != 0)
            {
                int count = (int)((startY - endY) / bumpY);
                if (bumpX == 0) // 只包括垂直方向的时候，垂直带也要构建尾部最后一个。而水平垂直方向都有带子的时候，垂直带不需要最尾部的带子，反而是连接已经构造好的尾部带子（在水平方向上最后一个构建的）
                    count++;
                else
                    endY += bumpY; // 不构建末尾带，则跳过该位置

                int yaw = bumpY > 0 ? 180 : 0;
                for (int i = 0; i < count; i++)
                {
                    BlueprintBuilding b = new BlueprintBuilding();
                    b.index = list.Count;
                    b.areaIndex = 0;
                    b.localOffset_x = endX;
                    b.localOffset_y = endY;
                    b.localOffset_z = endZ;
                    b.localOffset_x2 = b.localOffset_x;
                    b.localOffset_y2 = b.localOffset_y;
                    b.localOffset_z2 = b.localOffset_z;
                    b.inputToSlot = 1;
                    b.yaw = yaw;
                    b.yaw2 = b.yaw;
                    b.itemId = itemShort;
                    b.modelIndex = modelIndex;
                    if (i != 0 || bumpX != 0 || connectTo >= 0) // 最末尾的传送带（第一个建立的传送带）没有后继带子
                    {
                        b.outputToSlot = 1;
                        if (connectTo >= 0)
                        {
                            b.outputObj = list[connectTo];
                            connectTo = -1; // 后续不需要再识别connectTo
                        }
                        else
                            b.outputObj = list[list.Count - 1];
                    }
                    b.parameters = new int[0];
                    if (i == 0 && endIcon > 0)
                    {
                        b.parameters = new int[] { endIcon, 0 };
                        endIcon = 0;
                    }
                    list.Add(b);
                    if (b.localOffset_z == 0)
                        gridMap.SetBuilding((int)b.localOffset_x, (int)b.localOffset_y, b.index);
                    endY += bumpY;
                }
                endY -= bumpY;
            }
            if(bumpZ != 0)
            {
                int count = (int)((startZ - endZ) / bumpZ);
                if(bumpY ==0 && bumpX == 0) // 类似地，只有自己时才构建尾部带子
                {
                    count++;
                }
                else
                {
                    endZ += bumpZ;
                }
                int yaw = 90;
                if (bumpX > 0)
                    yaw = 270;
                if (bumpY > 0)
                    yaw = 180;
                else if (bumpY < 0)
                    yaw = 0;

                for (int i = 0; i < count; i++)
                {
                    BlueprintBuilding b = new BlueprintBuilding();
                    b.index = list.Count;
                    b.areaIndex = 0;
                    b.localOffset_x = endX;
                    b.localOffset_y = endY;
                    b.localOffset_z = endZ;
                    b.localOffset_x2 = b.localOffset_x;
                    b.localOffset_y2 = b.localOffset_y;
                    b.localOffset_z2 = b.localOffset_z;
                    b.inputToSlot = 1;
                    b.yaw = yaw;
                    b.yaw2 = b.yaw;
                    b.itemId = itemShort;
                    b.modelIndex = modelIndex;
                    if (i != 0 || bumpX != 0 || bumpY != 0 || connectTo >= 0) // 最末尾的传送带（第一个建立的传送带）没有后继带子
                    {
                        b.outputToSlot = 1;
                        if (connectTo >= 0)
                        {
                            b.outputObj = list[connectTo];
                            connectTo = -1; // 后续不需要再识别connectTo
                        }
                        else
                            b.outputObj = list[list.Count - 1];
                    }
                    b.parameters = new int[0];
                    if (i == 0 && endIcon > 0)
                    {
                        b.parameters = new int[] { endIcon, 0 };
                        endIcon = 0;
                    }

                    list.Add(b);
                    if (b.localOffset_z == 0)
                        gridMap.SetBuilding((int)b.localOffset_x, (int)b.localOffset_y, b.index);
                    endZ += bumpZ;
                }
                endZ -= bumpZ;
            }
            if(beginIcon > 0)
                list[list.Count - 1].parameters = new int[] { beginIcon, 0 };
        }

        public static int AddAssembler(this BpProcessor processor, int itemId, int recipeId, int x, int y, int z)
        {

            ref List<BlueprintBuilding> list = ref processor.buildings;
            ref Dictionary<int, Dictionary<int, int>> gridMap = ref processor.gridMap;

            ItemProto itemProto = LDB.items.Select(itemId);
            if (itemProto == null)
                return -1;
            PrefabDesc prefabDesc = itemProto.prefabDesc;

            BlueprintBuilding b = new BlueprintBuilding();
            b.index = list.Count;
            b.areaIndex = 0;
            b.localOffset_x = x;
            b.localOffset_y = y;
            b.localOffset_z = z;
            b.localOffset_x2 = b.localOffset_x;
            b.localOffset_y2 = b.localOffset_y;
            b.localOffset_z2 = b.localOffset_z;
            b.itemId = (short)itemId;
            b.recipeId = recipeId;
            b.modelIndex = (short)prefabDesc.modelIndex;
            b.parameters = new int[] { 0 };
            
            list.Add(b);
            return b.index;
        }

        /// <summary>
        /// 只需要依次加上最上层的，并将上层的InputObj设置为下层的lab，所有lab均不需要设置outputObj
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="itemId"></param>
        /// <param name="recipeId"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static int AddLab(this BpProcessor processor, int itemId, int recipeId, int x, int y, int level)
        {

            ref List<BlueprintBuilding> list = ref processor.buildings;
            ref Dictionary<int, Dictionary<int, int>> gridMap = ref processor.gridMap;

            ItemProto itemProto = LDB.items.Select(itemId);
            if (itemProto == null)
                return -1;
            PrefabDesc prefabDesc = itemProto.prefabDesc;

            BlueprintBuilding b = new BlueprintBuilding();
            b.index = list.Count;
            b.areaIndex = 0;
            b.localOffset_x = x;
            b.localOffset_y = y;
            b.localOffset_z = 0;
            b.localOffset_x2 = b.localOffset_x;
            b.localOffset_y2 = b.localOffset_y;
            b.localOffset_z2 = b.localOffset_z;
            b.itemId = (short)itemId;
            b.recipeId = recipeId;
            b.modelIndex = (short)prefabDesc.modelIndex;
            b.parameters = new int[] { 1, 0 }; // 1,0为制造，2,0为科研
            b.inputFromSlot = BpDB.assemblerInfos[itemId].inputFromSlot;
            b.inputToSlot = BpDB.assemblerInfos[itemId].inputToSlot;
            b.outputFromSlot = BpDB.assemblerInfos[itemId].outputFromSlot;
            b.outputToSlot = BpDB.assemblerInfos[itemId].outputToSlot;

            list.Add(b);
            return b.index;
        }

        public static int AddPLS(this BpProcessor processor, int x, int y)
        {
            int itemId = BpDB.PLS;
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
            b.modelIndex = (short)LDB.items.Select(itemId).prefabDesc.modelIndex;
            b.parameters = new int[2048];
            b.parameters[320] = BpDB.stationParam320;
            b.parameters[321] = BpDB.stationParam321;
            b.parameters[322] = BpDB.stationParam322;
            b.parameters[323] = BpDB.stationParam323;
            b.parameters[324] = BpDB.stationParam324;
            b.parameters[325] = BpDB.stationParam325;
            b.parameters[326] = BpDB.stationParam326;
            b.parameters[327] = BpDB.stationParam327;
            processor.buildings.Add(b);
            processor.gridMap.SetBuilding(x, y, b.index);
            processor.PLSs.Add(b.index);
            return b.index;
        }

        public static int AddCoater(this BpProcessor processor, int x, int y)
        {
            int yaw = 90;
            BlueprintBuilding b = new BlueprintBuilding();
            b.index = processor.buildings.Count;
            b.areaIndex = 0;
            b.localOffset_x = x;
            b.localOffset_y = y;
            b.localOffset_z = 0;
            b.localOffset_x2 = b.localOffset_x;
            b.localOffset_y2 = b.localOffset_y;
            b.localOffset_z2 = 0;
            b.yaw = yaw;
            b.yaw2 = yaw;
            b.itemId = 2313;
            b.modelIndex = (short)LDB.items.Select(2313).prefabDesc.modelIndex;
            b.outputFromSlot = 15;
            b.outputToSlot = 14;
            b.inputFromSlot = 15;
            b.inputToSlot = 14;
            b.parameters = new int[0];
            processor.buildings.Add(b);
            return b.index;
        }

        /// <summary>
        /// 只支持直线正对连接
        /// </summary>
        public static void ConnectPLSToBelt(this BpProcessor processor, int PLSIndex, int PLSSlot, int storageIndex, int beltIndex)
        {
            int PLSX = (int)processor.buildings[PLSIndex].localOffset_x;
            int PLSY = (int)processor.buildings[PLSIndex].localOffset_y;
            int endX = (int)processor.buildings[beltIndex].localOffset_x;
            int endY = (int)processor.buildings[beltIndex].localOffset_y;
            short beltItemId = processor.buildings[beltIndex].itemId;
            short beltModelIndex = processor.buildings[beltIndex].modelIndex;
            bool isOutput = storageIndex >= 0;
            int bumpX = 0;
            int bumpY = 0;
            if (PLSSlot >= 0 && PLSSlot <= 2)
                bumpY = -1;
            else if (PLSSlot >= 3 && PLSSlot <= 5)
                bumpX = 1;
            else if (PLSSlot >= 6 && PLSSlot <= 8)
                bumpY = 1;
            else
                bumpX = -1;

            if(!isOutput) // 如果是输入
            {
                bumpY *= -1;
                bumpX *= -1;
            }

            int count = 0;
            if(bumpY != 0)
            {
                count = Math.Abs((int)Math.Round((PLSY - endY)*1.0)) - 2;
            }
            else
            {
                count = Math.Abs((int)Math.Round((PLSX - endX) * 1.0)) - 2;
            }
            if(count < 2)
            {
                Debug.LogWarning("构建从物流塔到传送带的连接时出现错误，因为距离小于2");
                return;
            }
            int yaw = 0;
            if (bumpY > 0)
                yaw = 180;
            else if (bumpX > 0)
                yaw = 270;
            else if (bumpX < 0)
                yaw = 90;

            if(isOutput)
            {
                processor.buildings[PLSIndex].parameters[192 + PLSSlot * 4] = 1;
                processor.buildings[PLSIndex].parameters[192 + PLSSlot * 4 + 1] = storageIndex + 1;
                int lastBeltIndex = beltIndex;
                for (int i = 0; i < count; i++) 
                {
                    BlueprintBuilding b = new BlueprintBuilding();
                    b.index = processor.buildings.Count;
                    b.areaIndex = 0;
                    if (i < count - 1) // 除了最后一个，都是直线连过去就行
                    {
                        b.localOffset_x = endX + bumpX * (i + 1);
                        b.localOffset_y = endY + bumpY * (i + 1);
                        b.parameters = new int[0];
                    }
                    else // 最后一个特殊
                    {
                        b.localOffset_x = PLSX + LDB.items.Select(BpDB.PLS).prefabDesc.portPoses[PLSSlot].position.x * 0.8f;
                        b.localOffset_y = PLSY + LDB.items.Select(BpDB.PLS).prefabDesc.portPoses[PLSSlot].position.z * 0.8f;
                        b.inputFromSlot = PLSSlot;
                        b.inputObj = processor.buildings[PLSIndex];
                        b.parameters = new int[] { processor.GetPLSItemByStorageIndex(PLSIndex, storageIndex), 0 };
                    }
                    b.localOffset_z = 0;
                    b.localOffset_x2 = b.localOffset_x;
                    b.localOffset_y2 = b.localOffset_y;
                    b.localOffset_z2 = b.localOffset_z;
                    b.inputToSlot = 1;
                    b.yaw = yaw;
                    b.yaw2 = b.yaw;
                    b.itemId = beltItemId;
                    b.modelIndex = beltModelIndex;

                    b.outputToSlot = 1;
                    b.outputObj = processor.buildings[lastBeltIndex];
                    lastBeltIndex = b.index;


                    processor.buildings.Add(b);
                    processor.gridMap.SetBuilding((int)b.localOffset_x, (int)b.localOffset_y, b.index);
                }

            }
            else
            {
                processor.buildings[PLSIndex].parameters[192 + PLSSlot * 4] = 2;
                int lastBeltIndex = -1;
                for (int i = 0; i < count; i++)
                {
                    BlueprintBuilding b = new BlueprintBuilding();
                    b.index = processor.buildings.Count;
                    b.areaIndex = 0;
                    if (i == 0)
                    {
                        b.localOffset_x = PLSX + LDB.items.Select(BpDB.PLS).prefabDesc.portPoses[PLSSlot].position.x * 0.8f;
                        b.localOffset_y = PLSY + LDB.items.Select(BpDB.PLS).prefabDesc.portPoses[PLSSlot].position.z * 0.8f;
                        b.outputToSlot = PLSSlot;
                        b.outputObj = processor.buildings[PLSIndex];
                    }
                    else
                    {
                        b.localOffset_x = endX - bumpX * count + bumpX * i;
                        b.localOffset_y = endY - bumpY * count + bumpY * i;
                        b.outputToSlot = 1;
                        b.outputObj = processor.buildings[lastBeltIndex];
                    }
                    lastBeltIndex = b.index;
                    b.localOffset_z = 0;
                    b.localOffset_x2 = b.localOffset_x;
                    b.localOffset_y2 = b.localOffset_y;
                    b.localOffset_z2 = b.localOffset_z;
                    b.inputToSlot = 1;
                    b.yaw = yaw;
                    b.yaw2 = b.yaw;
                    b.itemId = beltItemId;
                    b.modelIndex = beltModelIndex;

                    processor.buildings.Add(b);
                    processor.gridMap.SetBuilding((int)b.localOffset_x, (int)b.localOffset_y, b.index);
                }
                processor.buildings[beltIndex].outputToSlot = 1;
                processor.buildings[beltIndex].outputObj  = processor.buildings[lastBeltIndex];
            }

        }

        /// <summary>
        /// 只支持在带子和工厂之间抓，目前在带子和工厂之间抓只支持纵向（南北向）抓
        /// </summary>
        public static void AddSorter(this BpProcessor processor, int itemId, int distance, int fromIndex, int toIndex, int fromSlot, int toSlot, float yaw, int filter, bool beltDirectionEast = true)
        {

            ref List<BlueprintBuilding> list = ref processor.buildings;

            float x1;
            float x2;
            float y1;
            float y2;
            bool fromAssembler = fromSlot >= 0;
            bool toAssembler = toSlot >= 0;
            if (fromAssembler)
            {
                PrefabDesc prefab = LDB.items.Select(list[fromIndex].itemId).prefabDesc;
                x1 = list[fromIndex].localOffset_x + prefab.slotPoses[fromSlot].position.x * 0.8f;
                y1 = list[fromIndex].localOffset_y + prefab.slotPoses[fromSlot].position.z * 0.8f; // 注意这里是position.z表示蓝图的y方向！
            }
            else // 带子
            {
                x1 = list[fromIndex].localOffset_x;
                y1 = list[fromIndex].localOffset_y;
            }
            if (toAssembler)
            {
                PrefabDesc prefab = LDB.items.Select(list[toIndex].itemId).prefabDesc;
                x2 = list[toIndex].localOffset_x + prefab.slotPoses[toSlot].position.x * 0.8f;
                y2 = list[toIndex].localOffset_y + prefab.slotPoses[toSlot].position.z * 0.8f; // 注意这里是position.z表示蓝图的y方向！
            }
            else
            {
                x2 = list[toIndex].localOffset_x;
                y2 = list[toIndex].localOffset_y;
            }
            BlueprintBuilding b = new BlueprintBuilding();
            if (fromAssembler && !toAssembler)
            {
                x2 = x1; // 爪子x位置取决于工厂的slot的x
                int assemblerItemId = list[fromIndex].itemId; // 与传送带相接的offset
                ERecipeType type = LDB.items.Select(assemblerItemId).prefabDesc.assemblerRecipeType;
                if (BpDB.assemblerInfos.ContainsKey(assemblerItemId))
                {
                    b.outputOffset = BpDB.assemblerInfos[assemblerItemId].slotConnectBeltSorterOffsets[fromSlot];

                    if (!beltDirectionEast)
                        b.outputOffset *= -1;

                }

            }
            else if (!fromAssembler && toAssembler)
            {
                x1 = x2;
                int assemblerItemId = list[toIndex].itemId;
                ERecipeType type = LDB.items.Select(assemblerItemId).prefabDesc.assemblerRecipeType;
                if (BpDB.assemblerInfos.ContainsKey(assemblerItemId))
                {
                    b.inputOffset = BpDB.assemblerInfos[assemblerItemId].slotConnectBeltSorterOffsets[toSlot];
                    if (!beltDirectionEast)
                        b.inputOffset *= -1;
                }

            }
            b.index = list.Count;
            b.areaIndex = 0;
            b.localOffset_x = x1;
            b.localOffset_y = y1;
            b.localOffset_z = 0;
            b.localOffset_x2 = x2;
            b.localOffset_y2 = y2;
            b.localOffset_z2 = b.localOffset_z;
            b.yaw = yaw;
            b.yaw2 = yaw;
            b.inputToSlot = 1; // 无论如何都要这么写
            b.inputFromSlot = fromSlot;
            b.outputToSlot = toSlot;
            b.inputObj = list[fromIndex];
            b.outputObj = list[toIndex];

            b.itemId = (short)itemId;
            b.modelIndex = (short)LDB.items.Select(itemId).prefabDesc.modelIndex;
            b.filterId = filter;
            b.parameters = new int[] { distance };

            list.Add(b);
        }

        /// <summary>
        /// 
        /// </summary>
        public static void AssemblerConnectToBelt(this BpProcessor processor, int assemblerIndex, int slot, int sorterId, int distance, bool isInput, int filter, bool beltDirectionEast = true)
        {
            int assemblerItemId = processor.buildings[assemblerIndex].itemId;
            BpAssemblerInfo bpa = BpDB.assemblerInfos[assemblerItemId];

            int centerX = (int)processor.buildings[assemblerIndex].localOffset_x;
            int centerY = (int)processor.buildings[assemblerIndex].localOffset_y;

            int beltY;
            if (bpa.slotYDirection[slot] < 0)
                distance *= -1;
            if(distance > 0)
            {
                beltY = centerY + (bpa.centerDistanceTop - 1) + distance;
            }
            else
            {
                beltY = centerY - (bpa.centerDistanceBottom - 1) + distance;
            }
            int beltX = centerX + bpa.slotConnectBeltXPositions[slot];
            int beltIndex = processor.gridMap.GetBuilding(beltX, beltY);
            if (beltIndex < 0)
            {

                Debug.LogWarning("为工厂创建分拣器时，没有找到目标位置的传送带。");
                return;
            }
            int yaw = 0;
            if (distance > 0 && isInput || distance < 0 && !isInput)
                yaw = 180;

            if(isInput)
            {
                processor.AddSorter(sorterId, Math.Abs(distance), beltIndex, assemblerIndex, -1, slot, yaw, filter, beltDirectionEast);
            }
            else
            {
                processor.AddSorter(sorterId, Math.Abs(distance), assemblerIndex, beltIndex, slot, -1, yaw, filter, beltDirectionEast);
            }
        }

        public static int GetPLSItemByStorageIndex(this BpProcessor processor, int PLSIndex, int storageIndex)
        {
            return processor.buildings[PLSIndex].parameters[storageIndex * 6];
        }

        public static void SetPLSStorage(this BpProcessor processor, int PLSIndex, int storageIndex, int itemId, bool isNeed)
        {
            processor.buildings[PLSIndex].parameters[storageIndex * 6] = itemId;
            processor.buildings[PLSIndex].parameters[storageIndex * 6 + 1] = isNeed ? 2 : 1;
            processor.buildings[PLSIndex].parameters[storageIndex * 6 + 3] = BpDB.stationMaxItemCount;
        }

        public static BlueprintData CreateEmpty()
        {
            BlueprintData bp = new BlueprintData();
            bp.ResetAsEmpty();
            bp.layout = EIconLayout.OneIcon;
            bp.icon0 = 41508; // 戴森球计划白色标志
            bp.patch = 1;
            bp.cursorOffset_x = 0;
            bp.cursorOffset_y = 0;
            bp.shortDesc = "DSPCalc_Quick";
            bp.desc = "";
            bp.cursorTargetArea = 0;
            bp.dragBoxSize_x = 1;
            bp.dragBoxSize_y = 1;
            bp.primaryAreaIdx = 0;
            BlueprintArea bpa = new BlueprintArea();
            bp.areas[0] = bpa;
            bpa.index = 0;
            bpa.parentIndex = -1;
            bpa.tropicAnchor = 0;
            bpa.areaSegments = 200;
            bpa.anchorLocalOffsetX = 0;
            bpa.anchorLocalOffsetY = 0;
            bpa.width = 1;
            bpa.height = 1;
            bp.buildings = new BlueprintBuilding[0];
            return bp;
        }


        public static void SetBuilding(this Dictionary<int, Dictionary<int, int>> gridMap, int x, int y, int index)
        {
            if (!gridMap.ContainsKey(x))
                gridMap[x] = new Dictionary<int, int>();
            if (!gridMap[x].ContainsKey(y))
                gridMap[x].Add(y, index);
        }

        public static int GetBuilding(this Dictionary<int, Dictionary<int, int>> gridMap, int x, int y)
        {
            if (!gridMap.ContainsKey(x))
            {
                Debug.LogError($"gridMap中并不存在行为{x}的建筑");
                return -1;
            }
            if (!gridMap[x].ContainsKey(y))
            {
                Debug.LogError($"gridMap中并不存在位置为{x},{y}的建筑");
                return -1;
            }
            return gridMap[x][y];
        }

    }
}
