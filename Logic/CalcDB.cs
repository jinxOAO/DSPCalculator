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
        public static Dictionary<int, List<AssemblerData>> assemblerDict; // key为可以处理的recipeType，将符合的工厂建筑全部放入list里面

        public static int energyNexusID = 2209;
        public static int emptyBatteryID = 2206;
        public static int fullBatteryID = 2207;

        public static int assemblerSpeedNormalized = 10000; // 在prefabDesc中，1.0x倍速的assemblerSpeed值
        public static int researchSpeedNormalized = 10000; // 默认游戏中，与assembler的1.0倍速的基数是相同的


        public static void TryInit()
        {
            // 为什么不是只init一次呢？因为这个patch会在LDBTool加入mod的proto之前进行，所以每次读取游戏都要执行一次，防止只在开启游戏时加载，而导致读取不到mod后加入的proto
            // 为什么不改为postPatch LDBTool的方法呢？因为怕他改名或者出什么问题，这样比较稳定（吧？）
            if (true) 
            {
                recipeDict = new Dictionary<int, NormalizedRecipe>();
                itemDict = new Dictionary<int, ItemData>();
                assemblerDict = new Dictionary<int, List<AssemblerData>>();

                // 加载配方
                int recipeLen = LDB.recipes.Length;
                for(int i = 0; i < recipeLen; i++) // 注意，dataArray的地址i和ID完全不对等
                {
                    RecipeProto recipe = LDB.recipes.dataArray[i];
                    if(recipe != null && recipe.Items!= null && recipe.Results != null)
                    {
                        NormalizedRecipe normalizedRecipe = new NormalizedRecipe(recipe);
                        recipeDict[recipe.ID] = normalizedRecipe;
                    }
                }

                // 处理所有物品
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
                                if (assemblerDict.ContainsKey(NormalizedRecipe.researchType))
                                {
                                    assemblerDict[type].Add(assemblerData);
                                }
                                else
                                {
                                    assemblerDict[type] = new List<AssemblerData> { assemblerData };
                                }
                            }
                            else if (model.prefabDesc.isAssembler)
                            {
                                AssemblerData assemblerData = new AssemblerData(item, model.prefabDesc);
                                int type = (int)model.prefabDesc.assemblerRecipeType;
                                if(assemblerDict.ContainsKey(type))
                                {
                                    assemblerDict[type].Add(assemblerData);
                                }
                                else
                                {
                                    assemblerDict[type] = new List<AssemblerData> { assemblerData };
                                }
                            }
                        }
                    }
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
                        float timeSpend = batteryModel.prefabDesc.maxAcuEnergy / nexusModel.prefabDesc.exchangeEnergyPerTick / 60; // 满功率充满需要多少秒
                        if(timeSpend > 0)
                        {
                            int timeInt = (int)Math.Ceiling(timeSpend); // 向上取整
                            chargeRecipe.TimeSpend = timeInt;
                            chargeNormed.time = timeInt;
                        }
                    }
                    recipeDict[-1] = chargeNormed;
                }

                // 处理所有工厂

                // 仅用于测试！！！！
                if(DSPCalculatorPlugin.developerMode)
                {
                    //foreach(var item in itemDict)
                    //{
                    //    string name = LDB.items.Select(item.Value.ID).name;
                    //    var itemData = item.Value;
                    //    if (itemData.defaultAsOre)
                    //    {
                    //        Debug.Log("--- " + name + $" (ID{itemData.ID}) 视为原矿");
                    //    }
                    //    else
                    //    {
                    //        Debug.Log("--- " + name + $" (ID{itemData.ID}) 的配方包含");
                    //        for (int i = 0; i < itemData.recipes.Count; i++)
                    //        {
                    //            Debug.Log("  |--- " + itemData.recipes[i].oriProto.name);
                    //        }
                    //    }
                    //    Debug.Log("");
                    //}

                    foreach(var item in assemblerDict)
                    {
                        ERecipeType type = (ERecipeType)item.Key;
                        Debug.Log($"for type {type}, we have");
                        for (int i = 0; i < item.Value.Count; i++)
                        {
                            Debug.Log($"     {LDB.items.Select(item.Value[i].ID).name}");
                        }
                    }
                }
                inited = true;
            }
        }
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
        public int time;

        public static int factionateType = (int)ERecipeType.Fractionate;
        public static int researchType = (int)ERecipeType.Research;

        // 处理分馏！！！！！！！
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
            time = recipe.TimeSpend;

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
        public Sprite icon;
        public float speed;
        public float workEnergyKW; // 以KW为单位的每个工厂的能量需求

        public AssemblerData(ItemProto item, PrefabDesc desc)
        {
            ID = item.ID;
            icon = item.iconSprite;
            workEnergyKW = (1.0f * desc.workEnergyPerTick * 60 / 1000);
            if (desc.isLab)
            {
                speed = 1.0f * desc.labResearchSpeed / CalcDB.researchSpeedNormalized;
            }
            else if (desc.isAssembler)
            {
                speed = 1.0f * desc.assemblerSpeed / CalcDB.assemblerSpeedNormalized;
            }
            else
            {
                speed = 1.0f;
            }
        }

    }
}
