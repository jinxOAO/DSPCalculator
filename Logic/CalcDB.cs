using CommonAPI;
using DSPCalculator.Compatibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DSPCalculator.Logic
{
    /// <summary>
    /// 经过处理后，直接被用作计算的数据。需要加载游戏的recipe等数据得到
    /// </summary>
    public class CalcDB
    {
        public static bool inited = false;
        public static Dictionary<int, NormalizedRecipe> recipeDict;
        public static Dictionary<int, ItemData> itemDict;
        public static Dictionary<int, List<AssemblerData>> assemblerListByType; // key为可以处理的recipeType，将符合的工厂建筑全部放入list里面
        public static Dictionary<int, AssemblerData> assemblerDict; // key为assembler的itemId
        public static List<int> proliferatorItemIds;
        //public static List<int> proliferatorAbilities;
        public static Dictionary<int, int> proliferatorAbilitiesMap;
        public static Dictionary<int, int> proliferatorAbilityToId; // 增产剂效果转化为Id

        public static int energyNexusID = 2209;
        public static int emptyBatteryID = 2206;
        public static int fullBatteryID = 2207;

        public static int assemblerSpeedNormalized = 10000; // 在prefabDesc中，1.0x倍速的assemblerSpeed值
        public static int researchSpeedNormalized = 10000; // 默认游戏中，与assembler的1.0倍速的基数是相同的

        public static int[] defaultAsOreArray = new int[] { 1000, 1003, 1117, 1120, 6201, 6206, 6220, 6234, 7002, 7019 };// 一些即使有生产配方，也会被默认视为原矿的物品（用户可以更改偏好使其不作为原矿）
        public static bool alreadyHaveOneFracRecipe; // 为了避免分馏mod可能的大量成环危险，只会加载第一个分馏配方，其他的均不放入recipe

        public const int beltSpeedToPerSecFactor = 6; // prefabDesc.beltSpeed = 1，则为6每秒。正常三级传送带为beltSpeed = 5，实际30/s
        public static double maxBeltItemSpeedPreSec = 0; // 用于统计所有游戏物品的传送带里的最大带速（正常游戏为30），用于计算分馏建筑需求的
        public static double maxStackSize = 4; // 最大堆叠倍率，由于分馏的生产设施的生产速度只由带速决定，所以这个值影响分馏的默认速度计算
        public static int dfSmelterId = 2319;
        public static int inserterMk3Id = 2013;
        public static int inserterMk2Id = 2012;
        public static int inserterMk1Id = 2011;

        public static List<BeltData> beltsDescending; // 传送带数据，以速度降序排列
        // public static List<InserterData> insertersDescending; // 分拣器数据，以速度降序排列

        public static void TryInit()
        {
            // 为什么不是只init一次呢？因为这个patch会在LDBTool加入mod的proto之前进行，所以每次读取游戏都要执行一次，防止只在开启游戏时加载，而导致读取不到mod后加入的proto
            // 为什么不改为postPatch LDBTool的方法呢？因为怕他改名或者出什么问题，这样比较稳定（吧？）
            if (true) 
            {
                recipeDict = new Dictionary<int, NormalizedRecipe>();
                itemDict = new Dictionary<int, ItemData>();
                assemblerListByType = new Dictionary<int, List<AssemblerData>>();
                assemblerDict = new Dictionary<int, AssemblerData>();
                proliferatorItemIds = new List<int> { 1141, 1142, 1143 };
                //proliferatorAbilities = new List<int>();
                proliferatorAbilitiesMap = new Dictionary<int, int>();
                for (int i = 0; i < proliferatorItemIds.Count; i++)
                {
                    proliferatorAbilitiesMap[proliferatorItemIds[i]] = 0;
                }
                proliferatorAbilityToId = new Dictionary<int, int>();
                beltsDescending = new List<BeltData>();
                //insertersDescending = new List<InserterData>();
                alreadyHaveOneFracRecipe = false;

                // 加载配方，并初始化assemblerListByType
                int recipeLen = LDB.recipes.Length;
                for(int i = 0; i < recipeLen; i++) // 注意，dataArray的地址i和ID完全不对等
                {
                    RecipeProto recipe = LDB.recipes.dataArray[i];
                    if(recipe != null && recipe.Items!= null && recipe.Results != null)
                    {
                        if (recipe.Type != ERecipeType.Fractionate)
                        {
                            NormalizedRecipe normalizedRecipe = new NormalizedRecipe(recipe);
                            recipeDict[recipe.ID] = normalizedRecipe;
                        }
                        else if (!alreadyHaveOneFracRecipe)
                        {
                            if (recipe.ID != 115)
                            {
                                continue;
                            }
                            else
                            {
                                alreadyHaveOneFracRecipe = true;
                                NormalizedRecipe normalizedRecipe = new NormalizedRecipe(recipe);
                                recipeDict[recipe.ID] = normalizedRecipe;
                            }
                        }
                        if(!assemblerListByType.ContainsKey((int)recipe.Type))
                        {
                            assemblerListByType[(int)recipe.Type] = new List<AssemblerData>(); // 在这里创建type对应的list
                        }
                    }
                }

                // 处理所有物品、生产设施
                int itemLen = LDB.items.Length;
                for (int i = 0; i < itemLen; i++) // 注意，dataArray的地址i和ID完全不对等
                {
                    ItemProto item = LDB.items.dataArray[i];
                    if(item != null)
                    {
                        // 一些原矿等物品不出现在配方里面，所以要额外加载一遍物品
                        if (!itemDict.ContainsKey(item.ID))
                        {
                            itemDict[item.ID] = new ItemData(item.ID); // 此时默认其为原矿
                        }

                        // 如果物品是生产建筑，进行处理
                        int modelIndex = item.ModelIndex;
                        ModelProto model = LDB.models.Select(modelIndex);
                        if(model?.prefabDesc != null)
                        {
                            if(model.prefabDesc.isLab)
                            {
                                AssemblerData assemblerData = new AssemblerData(item, model.prefabDesc);
                                int type = NormalizedRecipe.researchType;
                                if (assemblerListByType.ContainsKey(NormalizedRecipe.researchType))
                                {
                                    assemblerListByType[type].Add(assemblerData);
                                }
                                else
                                {
                                    //assemblerListByType[type] = new List<AssemblerData> { assemblerData }; // 不再创建没有直接配方的列表，比如GB的19，只是为了可以同时处理smelt和11，但没有真的用19的配方
                                }
                                assemblerDict[item.ID] = assemblerData;
                            }
                            else if (model.prefabDesc.isAssembler || model.prefabDesc.isFractionator)
                            {
                                AssemblerData assemblerData = new AssemblerData(item, model.prefabDesc);
                                int type = (int)model.prefabDesc.assemblerRecipeType;
                                if(assemblerListByType.ContainsKey(type))
                                {
                                    assemblerListByType[type].Add(assemblerData);
                                }
                                else
                                {
                                    //assemblerListByType[type] = new List<AssemblerData> { assemblerData }; // 不再创建没有直接配方的列表，比如GB的19，只是为了可以同时处理smelt和11，但没有真的用19的配方
                                }
                                assemblerDict[item.ID] = assemblerData;
                            }

                            if(model.prefabDesc.isBelt)
                            {
                                maxBeltItemSpeedPreSec = Math.Max(maxBeltItemSpeedPreSec, model.prefabDesc.beltSpeed * beltSpeedToPerSecFactor);
                                beltsDescending.Add(new BeltData(item.ID, model.prefabDesc));
                            }
                        }
                    }
                }
                beltsDescending = beltsDescending.OrderByDescending(x => x.speed).ToList();
                // InitInserter(); // 暂时不需要读取该数据

                // 根据最大带速，初始化分馏设施的生产速度倍率
                if (assemblerListByType.ContainsKey((int)ERecipeType.Fractionate))
                {
                    foreach (var assemblerData in assemblerListByType[(int)ERecipeType.Fractionate])
                    {
                        assemblerData.speed = maxBeltItemSpeedPreSec * maxStackSize;
                    }
                }
                else
                {
                    Debug.Log("没有分馏设施");
                }

                // 特殊地 加入一个特别的，电池充电的配方
                if (LDB.items.Select(energyNexusID) != null && LDB.items.Select(emptyBatteryID) != null)
                {
                    RecipeProto chargeRecipe = new RecipeProto();
                    chargeRecipe.name = "用能量枢纽为电池充电";
                    chargeRecipe.ID = -1;
                    chargeRecipe.Items = new int[] { emptyBatteryID };
                    chargeRecipe.ItemCounts = new int[] { 1 };
                    chargeRecipe.Results = new int[] { fullBatteryID };
                    chargeRecipe.ResultCounts = new int[] { 1 };
                    NormalizedRecipe chargeNormed = new NormalizedRecipe(chargeRecipe);
                    chargeNormed.type = energyNexusID; // 这里用type记录能量枢纽的itemID

                    // 将配方时间设定为充满需要的秒数
                    ModelProto nexusModel = LDB.models.Select(LDB.items.Select(energyNexusID).ModelIndex);
                    ModelProto batteryModel = LDB.models.Select(LDB.items.Select(emptyBatteryID).ModelIndex);
                    if (nexusModel?.prefabDesc != null && batteryModel?.prefabDesc != null && nexusModel.prefabDesc.exchangeEnergyPerTick > 0)
                    {
                        float timeSpend = 1.0f * batteryModel.prefabDesc.maxAcuEnergy / nexusModel.prefabDesc.exchangeEnergyPerTick / 60; // 满功率充满需要多少秒
                        if(timeSpend > 0)
                        {
                            int timeInt = (int)Math.Ceiling(timeSpend * 60); // 向上取整
                            chargeRecipe.TimeSpend = timeInt;
                            chargeNormed.time = timeSpend;
                        }
                    }
                    recipeDict[-1] = chargeNormed;

                    // 也要将充电的工厂加入字典
                    AssemblerData exchangerData = new AssemblerData(LDB.items.Select(energyNexusID), nexusModel.prefabDesc);
                    exchangerData.workEnergyW = nexusModel.prefabDesc.exchangeEnergyPerTick * 60;
                    exchangerData.idleEnergyW = 0;
                    exchangerData.speed = 1;
                    assemblerListByType[energyNexusID] = new List<AssemblerData> { exchangerData };
                    assemblerDict[energyNexusID] = exchangerData;
                }


                // 处理增产剂
                for (int i = 0; i < proliferatorItemIds.Count; i++)
                {
                    ItemProto proto = LDB.items.Select(proliferatorItemIds[i]);
                    if (proto != null)
                    {
                        if (proto.Ability > 10)
                            proliferatorAbilitiesMap[proliferatorItemIds[i]] = 10;
                        else if (proto.Ability >= 0)
                            proliferatorAbilitiesMap[proliferatorItemIds[i]] = proto.Ability;

                        proliferatorAbilityToId[proto.Ability] = proto.ID;
                    }
                }


                // 处理所有物品被默认视为原矿，即使有配方可以生产
                for (int i = 0; i < defaultAsOreArray.Length; i++)
                {
                    if (itemDict.ContainsKey(defaultAsOreArray[i]))
                    {
                        itemDict[defaultAsOreArray[i]].defaultAsOre = true;
                    }
                }

                inited = true;

                GBInit();
            }
        }

        public static void GBInit()
        {
            if (!CompatManager.GB)
            {
                return;
            }

            if (assemblerDict.ContainsKey(dfSmelterId)) // 黑雾炉子 dfSmelterId
            {
                if (assemblerListByType.ContainsKey((int)ERecipeType.Smelt))
                    assemblerListByType[(int)ERecipeType.Smelt].Add(assemblerDict[dfSmelterId]); // 自己已经在19
                if (assemblerListByType.ContainsKey(11))
                    assemblerListByType[11].Add(assemblerDict[dfSmelterId]);
            }

            if (assemblerDict.ContainsKey(6257))
            {
                if (assemblerListByType.ContainsKey((int)ERecipeType.Assemble))
                    assemblerListByType[(int)ERecipeType.Assemble].Add(assemblerDict[6257]); // 自己已经在18
                if (assemblerListByType.ContainsKey(9))
                    assemblerListByType[9].Add(assemblerDict[6257]);
            }


            if (assemblerDict.ContainsKey(6258))
            {
                if (assemblerListByType.ContainsKey((int)ERecipeType.Smelt))
                    assemblerListByType[(int)ERecipeType.Smelt].Add(assemblerDict[6258]); // 自己已经在19
                if (assemblerListByType.ContainsKey(11))
                    assemblerListByType[11].Add(assemblerDict[6258]);
            }


            if (assemblerDict.ContainsKey(6259))
            {
                if (assemblerListByType.ContainsKey((int)ERecipeType.Chemical))
                    assemblerListByType[(int)ERecipeType.Chemical].Add(assemblerDict[6259]); // 自己已经在17
                if (assemblerListByType.ContainsKey((int)ERecipeType.Refine))
                    assemblerListByType[(int)ERecipeType.Refine].Add(assemblerDict[6259]);
                if (assemblerListByType.ContainsKey(16))
                    assemblerListByType[16].Add(assemblerDict[6259]);
            }

            if (assemblerDict.ContainsKey(2318)) // 黑雾制造台什么都能做
            {
                if (assemblerListByType.ContainsKey((int)ERecipeType.Assemble))
                    assemblerListByType[(int)ERecipeType.Assemble].Add(assemblerDict[2318]); // 黑雾台自己已经在12
                if (assemblerListByType.ContainsKey(9))
                    assemblerListByType[9].Add(assemblerDict[2318]);
                if (assemblerListByType.ContainsKey((int)ERecipeType.Chemical))
                    assemblerListByType[(int)ERecipeType.Chemical].Add(assemblerDict[2318]);
                if (assemblerListByType.ContainsKey((int)ERecipeType.Refine))
                    assemblerListByType[(int)ERecipeType.Refine].Add(assemblerDict[2318]);
                if (assemblerListByType.ContainsKey(16))
                    assemblerListByType[16].Add(assemblerDict[2318]);
                if (assemblerListByType.ContainsKey((int)ERecipeType.Particle))
                    assemblerListByType[(int)ERecipeType.Particle].Add(assemblerDict[2318]);
                if (assemblerListByType.ContainsKey((int)ERecipeType.Research))
                    assemblerListByType[(int)ERecipeType.Research].Add(assemblerDict[2318]);
                if (assemblerListByType.ContainsKey(10))
                    assemblerListByType[10].Add(assemblerDict[2318]);
            }
        }

        //public static void InitInserter()
        //{
        //    insertersDescending.Add(new InserterData(2011, 6));
        //    insertersDescending.Add(new InserterData(2012, 12));
        //    insertersDescending.Add(new InserterData(2013, 24));
        //    insertersDescending.Add(new InserterData(2014, 120, false));

        //    insertersDescending.OrderByDescending(x => x.speedByDistance[0]);
        //}
    }

    /// <summary>
    /// 标准化后的配方，标准化后，首产物的净产出为1，且耗时1s，不同的是需要消耗的工厂个数，且消除了原材料和产物中的相同物品（只留下多的）
    /// </summary>
    public class NormalizedRecipe
    {
        public int ID; // 对应的RecipeId
        public RecipeProto oriProto; // 对应的原始recipeProto
        // 下面为标准化后的数据
        public int[] products; 
        public int[] productCounts; // 首个产物必定为1个
        public int[] resources;
        public int[] resourceCounts; // 对应首个产物净产出为1个时，消耗的原材料数
        // public float factoryCount; // 达到1个/s时需要的工厂数量，相当于    原配方的耗时 / 首产物（的净产出）产出数量
        public bool productive; // 是否可增产
        public int type; // 转化为int之后的recipe类型，用于确定UI中的工厂可选项
        public double time;

        public static int factionateType = (int)ERecipeType.Fractionate;
        public static int researchType = (int)ERecipeType.Research;

        
        public NormalizedRecipe(RecipeProto recipe)
        {
            ID = recipe.ID;
            oriProto = recipe;
            int oriProductNum = recipe.Results.Length;
            int oriResourceNum = recipe.Items.Length;
            products = new int[oriProductNum];
            productCounts = new int[oriProductNum];
            resources = new int[oriResourceNum];
            resourceCounts = new int[oriResourceNum];
            for (int i = 0; i < oriProductNum; i++)
            {
                products[i] = recipe.Results[i];
                productCounts[i] = recipe.ResultCounts[i];
            }
            for (int i = 0; i < oriResourceNum; i++)
            {
                resources[i] = recipe.Items[i];
                resourceCounts[i] = recipe.ItemCounts[i];
            }

            // 下面这一步是为了，将原材料和产物的相同物品做减法，消除重叠的部分，如果产出比原材料多，则原材料需求为0，到时候后面计算量化的时候需求物将不包含这个原材料(会判断>0)。反之，则不计入可以生成该物品的配方
            for (int i = 0; i < oriProductNum; i++)
            {
                for (int j = 0; j < oriResourceNum; j++)
                {
                    if (resources[j] == products[i])
                    {
                        int minCount = Math.Min(productCounts[i], resourceCounts[j]);
                        productCounts[i] -= minCount;
                        resourceCounts[j] -= minCount;
                    }
                }
            }

            productive = recipe.productive;
            type = (int)recipe.Type;
            time = 1.0 * recipe.TimeSpend / 60;

            if (type == (int)ERecipeType.Fractionate && productCounts.Length > 0 && resourceCounts.Length > 0)
            {
                for (int i = 0; i < productCounts.Length; i++)
                {
                    productCounts[i] = 1;
                }
                for (int i = 0; i < resourceCounts.Length; i++)
                {
                    resourceCounts[i] = 1;
                }
                time = 1.0 * recipe.ItemCounts[0] / recipe.ResultCounts[0];
                productive = false; // 这里虽然游戏的原始配方是true，但是其逻辑是增加分馏概率，也就是加速。为了计算器逻辑清晰，故改成false
            }

            Init();
        }

        /// <summary>
        /// 将该配方链接到所有净产物下面
        /// </summary>
        public void Init()
        {
            int productNum = products.Length;
            for (int i = 0; i < productNum; i++)
            {
                if (productCounts[i] > 0) // 大于0才为净产物，才能算数
                {
                    int itemID = products[i];
                    if (CalcDB.itemDict.ContainsKey(itemID)) // 如果已经有了
                    {
                        CalcDB.itemDict[itemID].defaultAsOre = false;
                        CalcDB.itemDict[itemID].recipes.Add(this);
                    }
                    else
                    {
                        CalcDB.itemDict[itemID] = new ItemData(itemID, this);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 计算过后的所有物品的所需数据，例如可以生成该物品的所有配方，是否默认为原矿，等
    /// </summary>
    public class ItemData
    {
        public int ID; 
        public bool defaultAsOre; // 在计算过程中默认被视为原矿
        public List<NormalizedRecipe> recipes; // 可以产出这个产物的normalizedRecipe

        /// <summary>
        /// 不从配方添加物品信息的时候
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="defaultAsOre"></param>
        public ItemData(int ID, bool defaultAsOre = true)
        {
            this.ID = ID;
            this.defaultAsOre = defaultAsOre;
            this.recipes = new List<NormalizedRecipe>();
        }

        /// <summary>
        /// 从配方添加物品信息的时候
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="recipe"></param>
        public ItemData(int ID, NormalizedRecipe recipe)
        {
            this.ID = ID;
            this.defaultAsOre = false;
            this.recipes = new List<NormalizedRecipe>
            {
                recipe
            };
        }
    }

    /// <summary>
    /// 用于记录哪种RecipeType可以使用的工厂ID
    /// </summary>
    public class AssemblerData
    {
        public int ID;
        public Sprite iconSprite;
        public double speed;
        public double workEnergyW; // 以W为单位的每个工厂的能量需求
        public double idleEnergyW; 

        public AssemblerData(ItemProto item, PrefabDesc desc)
        {
            ID = item.ID;
            iconSprite = item.iconSprite;
            workEnergyW = (1.0f * desc.workEnergyPerTick * 60);
            idleEnergyW = (1.0f * desc.idleEnergyPerTick * 60);

            if (desc.isLab)
            {
                speed = 1.0f * desc.labAssembleSpeed / CalcDB.researchSpeedNormalized;
            }
            else if (desc.isAssembler)
            {
                speed = 1.0f * desc.assemblerSpeed / CalcDB.assemblerSpeedNormalized;
            }
            else if (desc.isFractionator) // 分馏的速度根据最大带速来计算，和建筑本身无关
            {
                // 不做处理，而是在所有物品加载完成后最后处理
            }
            else
            {
                speed = 1.0f;
            }
        }

    }

    public class InserterData
    {
        public int ID;
        public double[] speedByDistance; // 每秒运力，以4堆叠计
        public InserterData(int ID, int basicSpeed, bool ignoreDistance = false)
        {
            this.ID = ID;
            speedByDistance = new double[3];
            speedByDistance[0] = basicSpeed;
            speedByDistance[1] = ignoreDistance ? basicSpeed : basicSpeed / 2;
            speedByDistance[2] = ignoreDistance ? basicSpeed : basicSpeed / 3;
        }
    }

    public class BeltData
    {
        public int ID;
        public double speed; // 每秒运力，以4堆叠计。也就是相当于60/min的运力的倍数
        public BeltData (int ID, PrefabDesc prefabDesc)
        {
            this.ID = ID;
            speed = prefabDesc.beltSpeed * CalcDB.beltSpeedToPerSecFactor * 4;
        }
    }
}
