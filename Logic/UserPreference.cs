using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSPCalculator.Logic
{
    /// <summary>
    /// 用户自定义配置项，比如：某些（可由多种配方产出的）物品需要用特定配方生产、某些物品的配方需要几级增产剂等等
    /// </summary>
    public class UserPreference
    {
        public Dictionary<int, RecipeConfig> recipeConfigs;
        public Dictionary<int, ItemConfig> itemConfigs;

        // 下列为全局属性，全局属性的优先级会被每个物品、配方的独特设定覆盖
        public int globalIncLevel; // 如果非负，则视为全局属性
        public bool globalIsInc; // 全局是否是增产模式
        public Dictionary<int, int> globalAssemblerIdByType; // 全局：每种type的强制使用工厂。参数1是type的int，参数2是工厂的itemId而非index！
        public bool bluebuff;
        public bool energyBurst;
        public bool dirac;
        public bool inferior;

        public UserPreference()
        {
            recipeConfigs = new Dictionary<int, RecipeConfig>();
            itemConfigs = new Dictionary<int, ItemConfig>();
            globalIncLevel = 0;
            globalIsInc = true;
            bluebuff = false;
            energyBurst = false;
            dirac = false;
            inferior = false;
            globalAssemblerIdByType = new Dictionary<int, int>();
        }

        public void Clear()
        {
            recipeConfigs.Clear();
            itemConfigs.Clear();
            globalIncLevel = 0;
            globalIsInc = true;
            globalAssemblerIdByType.Clear();
        }
    }

    public class RecipeConfig
    {
        public int ID; // recipeId
        public int incLevel; // 如果非负，视为用户需要这个配方应用增产
        public int forceIncMode; // -1 为使用全局，0为强制加速，1为强制增产
        public int assemblerItemId; // 如果大于零，视为用户需要这个配方用特定的建筑生产，此处记录的是对应建筑ItemId，要去CalcDB.assemblerDict[对应建筑ItemId]这里访问。

        public RecipeConfig(RecipeInfo recipeInfo)
        {
            ID = recipeInfo.ID;
            incLevel = -1; // -1为使用全局
            forceIncMode = -1;
            assemblerItemId = -1;
        }
    }

    public class ItemConfig
    {
        public int ID; // itemId
        public int recipeID; // 如果大于零，视为用户需要这个物品（不作为副产物的部分）必须由该配方生产
        public bool consideredAsOre; // 如果为true，无论如何视为原矿，直接输入产量。如果为false，则根据itemData.defaultAsOre决定。
        public bool forceNotOre; // 如果为true，无论如何不视为原矿，如果为false，则根据itemData.defaultAsOre决定

        public ItemConfig(int itemId)
        {
            ID= itemId;
            recipeID = 0;
            consideredAsOre = false;
            forceNotOre = false;
        }
    }
}
