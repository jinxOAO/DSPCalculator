using DSPCalculator.Compatibility;
using DSPCalculator.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSPCalculator.Bp
{
    public static class BpDB
    {
        public static Dictionary<int, BpAssemblerBuildingInfo> assemblerInfos; // key为assemblerId
        public static Dictionary<int, BpBeltInfo> beltInfos;
        public static Dictionary<int, BpSorterInfo> sorterInfos;
        public static List<BpBeltInfo> beltsAscending;
        public static List<BpSorterInfo> sortersAscending;
        public static Dictionary<int, int> GBMegas;
        public static int GBMegaTowerHeight = 20;

        public static int stationParam320 = 1000000; // 最大充电功率
        public static int stationParam321 = -100000000;
        public static int stationParam322 = 240000000;
        public static int stationParam323 = 1;
        public static int stationParam324 = 480000;
        public static int stationParam325 = 1;
        public static int stationParam326 = 100;
        public static int stationParam327 = 100; // 疑似起送量比例
        public static int stationParam330 = 1; // 自动放入飞机
        public static int stationMaxItemCount = 10000;

        public static int coaterBeltBackwardLen = 4; // 放置喷涂机需要在x方向延长的距离（绝对值，具体加还是减取决于喷涂机在带子的东还是西）
        public static int coaterOffsetX = 2; // 放置喷涂机的位置，相对于延长后的带子最端点向里的距离（绝对值）

        // station 的 param [i*6]=slotItemId。[i*6 + 1] = 0,1,2分别是仓储、供应和需求。[i*6 + 3]=10000代表上限。i是storage的index，为0~3
        // station的param[192 + i*4] =0或1或2，1代表输出，2代表输入，0代表无传送带接入。 param[192 + i*4+1]=0,1,2,3代表传送带接入的storageSlot是第几个。i是portSlot（传送带出入口）的index，为0~11

        public static int PLS = 2103;
        public static int PLSDistance = 13;
        public static List<int> cargoInfoNormIndexToBeltPosIndexMap_FirstRow = new List<int> { 3, 2, 4, 1, 0, 5 }; // cargoInfoOrderByNorm的index，对应放到cargoBeltPoses的index的map。适用于第一行工厂
        public static List<int> cargoInfoNormIndexToBeltPosIndexMap_SecondRow = new List<int> { 5, 6, -1, 7, 8 }; // 适用于第二行工厂，注意共享带不能重复录入，所以共享带会在第二行工厂处返回-1，读取这个是会越界的
        public static List<int> beltPosIndexToPLSStorageIndexMap = new List<int> { };
        public static int PLSMaxStorageKinds = 4;
        public static int labHeight = 3;

        public static void Init()
        {
            assemblerInfos = new Dictionary<int, BpAssemblerBuildingInfo>();
            beltInfos = new Dictionary<int, BpBeltInfo>();
            sorterInfos = new Dictionary<int, BpSorterInfo>();
            beltsAscending = new List<BpBeltInfo>();
            sortersAscending = new List<BpSorterInfo>();
            GBMegas = new Dictionary<int, int>();

            BpAssemblerBuildingInfo assembler = new BpAssemblerBuildingInfo();
            assembler.centerDistanceBottom = 2;
            assembler.centerDistanceTop = 2;
            assembler.DragDistanceX = 4;
            assembler.slotConnectBeltXPositions = new List<int> { -1, 0, 1, 0, 0, 0, 1, 0, -1, 0, 0, 0 };
            assembler.slotConnectBeltSorterOffsets = new List<int> { 4, -1, -4, 0, 0, 0, -4, 1, 4, 0, 0, 0 };
            assembler.vanillaRecipeType = ERecipeType.Assemble;
            assembler.cargoNormIndex2SlotMap_FirstRow = new List<int> { 2, 8, 1, 7, 6, 0 };
            assembler.cargoNormIndex2SlotMap_SecondRow = new List<int> { 8, 0, 7, 1, 2, 6 }; 
            assembler.slotYDirection = new List<int> { 1, 1, 1, 0, 0, 0, -1, -1, -1, 0, 0, 0 };
            assembler.height = 3;

            BpAssemblerBuildingInfo smelter = new BpAssemblerBuildingInfo();
            smelter.centerDistanceBottom = 2;
            smelter.centerDistanceTop = 2;
            smelter.DragDistanceX = 3;
            smelter.slotConnectBeltXPositions = new List<int> { -1, 0, 1, 0, 0, 0, 1, 0, -1, 0, 0, 0 };
            smelter.slotConnectBeltSorterOffsets = new List<int> { 4, -1, -4, 0, 0, 0, -4, 1, 4, 0, 0, 0 };
            smelter.vanillaRecipeType = ERecipeType.Smelt;
            smelter.cargoNormIndex2SlotMap_FirstRow = new List<int> { 2, 8, 1, 7, 6, 0 };
            smelter.cargoNormIndex2SlotMap_SecondRow = new List<int> { 8, 0, 7, 1, 2, 6 };
            smelter.slotYDirection = new List<int> { 1, 1, 1, 0, 0, 0, -1, -1, -1, 0, 0, 0 };
            smelter.height = 3;

            BpAssemblerBuildingInfo chemical = new BpAssemblerBuildingInfo();
            chemical.centerDistanceBottom = 2;
            chemical.centerDistanceTop = 3;
            chemical.DragDistanceX = 8; // 极其接近赤道时可以是7，但是稍微远一点就得是8
            chemical.slotConnectBeltXPositions = new List<int> { -1, 0, 1, 2, 1, 0, -1, 2 };
            chemical.slotConnectBeltSorterOffsets = new List<int> { 4, 0, -4, -9, -4, 0, 4, -9 };
            chemical.vanillaRecipeType = ERecipeType.Chemical;
            chemical.cargoNormIndex2SlotMap_FirstRow = new List<int> { 2, 6, 1, 5, 4, 0 };
            chemical.cargoNormIndex2SlotMap_SecondRow = new List<int> { 5, 1, 4, 2, 7, 3 };
            chemical.slotYDirection = new List<int> { 1, 1, 1, -1, -1, -1, -1, 1 };
            chemical.height = 5;

            BpAssemblerBuildingInfo refinery = new BpAssemblerBuildingInfo();
            refinery.centerDistanceBottom = 2;
            refinery.centerDistanceTop = 2;
            refinery.DragDistanceX = 7;
            refinery.defaultYaw = 90;
            refinery.slotConnectBeltXPositions = new List<int> { -1, 0, 1, 1, 0, -1, 0, 0, 0 };
            refinery.slotConnectBeltSorterOffsets = new List<int> { 5, 0, -4, -4, 0, 5, 0, 0, 0 };
            refinery.vanillaRecipeType = ERecipeType.Refine;
            refinery.cargoNormIndex2SlotMap_FirstRow = new List<int> { 3, 0, 4, 1, 2, 5 };
            refinery.cargoNormIndex2SlotMap_SecondRow = new List<int> { 0, 5, 1, 4, 3, 2 };
            refinery.slotYDirection = new List<int> { -1, -1, -1, 1, 1, 1, 0, 0, 0 };
            refinery.height = 7;

            BpAssemblerBuildingInfo collider = new BpAssemblerBuildingInfo();
            collider.centerDistanceBottom = 3;
            collider.centerDistanceTop = 3;
            collider.DragDistanceX = 10;
            collider.slotConnectBeltXPositions = new List<int> { -1, -2, -2, 0, 0, 0, -2, -2, -1 };
            collider.slotConnectBeltSorterOffsets = new List<int> { 4, 8, -9, 0, 0, 0, -9, 8, 4 };
            collider.vanillaRecipeType = ERecipeType.Particle;
            collider.cargoNormIndex2SlotMap_FirstRow = new List<int> { 0, 6, 1, 7, 8, 2 };
            collider.cargoNormIndex2SlotMap_SecondRow = new List<int> { 6, 2, 7, 1, 0, 8 };
            collider.slotYDirection = new List<int> { 1, 1, 1, 0, 0, 0, -1, -1, -1 };
            collider.hitboxExtendX = 2;
            collider.height = 9;

            BpAssemblerBuildingInfo lab = new BpAssemblerBuildingInfo();
            lab.centerDistanceBottom = 3;
            lab.centerDistanceTop = 3;
            lab.DragDistanceX = 5;
            lab.slotConnectBeltXPositions = new List<int> { 1, 0, -1, 0, 0, 0, -1, 0, 1, 0, 0, 0 };
            lab.slotConnectBeltSorterOffsets = new List<int> { -5, -1, 4, 0, 0, 0, 4, -1, -5, 0, 0, 0 };
            lab.outputToSlot = 14;
            lab.inputFromSlot = 15;
            lab.outputFromSlot = 15;
            lab.inputToSlot = 14;
            lab.vanillaRecipeType = ERecipeType.Research;
            lab.cargoNormIndex2SlotMap_FirstRow = new List<int> { 0, 6, 1, 7, 8, 2 };
            lab.cargoNormIndex2SlotMap_SecondRow = new List<int> { 6, 2, 7, 1, 0, 8 };
            lab.slotYDirection = new List<int> { 1, 1, 1, 0, 0, 0, -1, -1, -1, 0, 0, 0 };
            lab.height = 3;

            foreach (var building in CalcDB.assemblerDict)
            {
                int itemId = building.Key;
                ERecipeType recipeType = LDB.items.Select(itemId).prefabDesc.assemblerRecipeType;
                if (recipeType == ERecipeType.Assemble)
                {
                    assemblerInfos[itemId] = assembler;
                }
                else if (recipeType == ERecipeType.Smelt)
                {
                    assemblerInfos[itemId] = smelter;
                }
                else if (recipeType == ERecipeType.Chemical)
                {
                    assemblerInfos[itemId] = chemical;
                }
                else if (recipeType == ERecipeType.Particle)
                {
                    assemblerInfos[itemId] = collider;
                }
                else if (recipeType == ERecipeType.Refine)
                {
                    assemblerInfos[itemId] = refinery;
                }
                else if (LDB.items.Select(itemId).prefabDesc.isLab)
                {
                    assemblerInfos[itemId] = lab;
                }
            }
            if(CompatManager.GB)
            {
                assemblerInfos[2304] = assembler;
                assemblerInfos[2305] = assembler;
                assemblerInfos[2318] = assembler;
                assemblerInfos[6230] = smelter;
                assemblerInfos[2319] = smelter;
                assemblerInfos[2317] = chemical;
                GBMegas[6257] = 1;
                GBMegas[6258] = 1;
                GBMegas[6259] = 1;
                GBMegas[6260] = 1;
                GBMegas[6264] = 1;
                GBMegas[6265] = 1;
            }
            int slotMax = LDB.items.Select(2103).prefabDesc.stationMaxItemCount;
            if (slotMax > stationMaxItemCount)
                stationMaxItemCount = slotMax;
            if (CompatManager.GB)
                stationMaxItemCount = 15000;

            // 初始化分拣器、传送带信息
            int itemLen = LDB.items.Length;
            for (int i = 0; i < itemLen; i++) // 注意，dataArray的地址i和ID完全不对等
            {
                ItemProto item = LDB.items.dataArray[i];
                if (item != null)
                { 
                    PrefabDesc prefabDesc = item.prefabDesc;
                    if(prefabDesc.isBelt)
                    {
                        beltInfos[item.ID] = new BpBeltInfo(item);
                    }
                    else if (prefabDesc.isInserter)
                    {
                        sorterInfos[item.ID] = new BpSorterInfo(item);
                    }
                }
            }

            // 将分拣器、传送带按速度排序
            List<KeyValuePair<int, BpBeltInfo>> belts = beltInfos.OrderBy(x => x.Value.speedPerMin).ToList();
            for (int i = 0; i < belts.Count; i++)
            {
                beltsAscending.Add(belts[i].Value);
            }
            List<KeyValuePair<int, BpSorterInfo>> sorters = sorterInfos.OrderBy(x => x.Value.speedPerMin).ToList();
            for(int i = 0;i < sorters.Count; i++)
            {
                sortersAscending.Add(sorters[i].Value);
            }

            ItemProto PLSProto = LDB.items.Select(2103);
            if(PLSProto != null)
            {
                PLSMaxStorageKinds = PLSProto.prefabDesc.stationMaxItemKinds;
            }

            // 目前不考虑PLS客制化槽位的情况，所以
            PLSMaxStorageKinds = 4;
        }
    }

    public class BpAssemblerBuildingInfo
    {
        public int centerDistanceBottom; // 下边紧贴的带子距离工厂中心的距离
        public int centerDistanceTop; // 上边紧贴的带子距离工厂中心的距离
        public int DragDistanceX;
        //public int DragDistanceY;
        public int defaultYaw;
        public List<int> slotConnectBeltXPositions; // 对应的slot连接带子时，带子的位置相对于工厂中心的x偏移数。只有上下的接口可用，别的都是0但实际不是，也不应该用到
        public List<int> slotConnectBeltSorterOffsets; // 带子向右时，爪子连接工厂和传送带时，对应slot连接的sorter在传送带上的input或者output的offset
        public int outputToSlot;
        public int outputFromSlot;
        public int inputToSlot;
        public int inputFromSlot;
        public ERecipeType vanillaRecipeType; // 这个type只代表模型在原版游戏中对应的配方类型，并不一定指代它实际处理的配方类型（主要是指在创世里）
        public int height;
        public List<int> cargoNormIndex2SlotMap_FirstRow; // 单行或第一行时，将对应index位置（cargoInfoOrderByNorm的index）需要使用生产设施的slot的index
        public List<int> cargoNormIndex2SlotMap_SecondRow; // 双行蓝图的第二行（上边那行），对应的norm的index获取slot的index
        public List<int> slotYDirection; // slot在工厂的哪个方向
        public int hitboxExtendX; // 由于建筑碰撞体积，需要PLS比平时离得更远一些，额外原理的距离
        

        public BpAssemblerBuildingInfo()
        {
            defaultYaw = 0;
            outputToSlot = 0;
            outputFromSlot = 0;
            inputToSlot = 0;
            inputFromSlot = 0;
            height = 3;
            hitboxExtendX = 0;
        }
    }

    public class BpBeltInfo
    {
        public int itemId;
        public double speedPerMin; // 单层堆叠的速度
        public bool unlocked { get { return GameMain.history.ItemUnlocked(itemId); } }


        public BpBeltInfo(ItemProto item)
        {
            itemId = item.ID;
            speedPerMin = item.prefabDesc.beltSpeed * CalcDB.beltSpeedToPerSecFactor * 60;
        }

        public bool Satisfy(double speedNeed)
        {
            return speedPerMin > speedNeed - 0.001;
        }
    }

    public class BpSorterInfo
    {
        public int itemId;
        public int grade;
        public double speedPerMin; // 单层堆叠的速度，单格距离的速度
        public bool unlocked { get { return GameMain.history.ItemUnlocked(itemId); } }

        public BpSorterInfo(ItemProto item)
        {
            itemId = item.ID;
            grade = item.prefabDesc.inserterGrade;
            speedPerMin = 300000.0 / item.prefabDesc.inserterSTT * 60;
        }

        public bool Satisfy(double speedNeed, int distance)
        {
            if(grade >= 4)
                return true;
            if (distance == 0)
                distance = 1;

            return speedPerMin / distance > speedNeed - 0.001;
        }
    }
}
