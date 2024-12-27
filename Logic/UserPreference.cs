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
        public int globalAccLevel; // 如果非负，则视为全局属性
        public bool globalIsInc; // 全局是否是增产模式
        public Dictionary<int, int> globalAssemblerIndexByType; // 全局：每种type的强制使用工厂


        public UserPreference()
        {
            recipeConfigs = new Dictionary<int, RecipeConfig>();
            itemConfigs = new Dictionary<int, ItemConfig>();
            globalIncLevel = 0;
            globalAccLevel = 0;
            globalIsInc = false;
            globalAssemblerIndexByType = new Dictionary<int, int>();
        }

        public void Clear()
        {
            recipeConfigs.Clear();
            itemConfigs.Clear();
            globalIncLevel = 0;
            globalAccLevel = 0;
            globalIsInc = false;
            globalAssemblerIndexByType.Clear();
        }
    }

    public class RecipeConfig
    {
        public int ID; // recipeId
        public int incLevel; // 如果非零，视为用户需要这个配方应用增产
        public int accLevel; // 如果非零，视为用户需要这个配方应用加速
        public bool isInc;
        public int assemblerIndex; // 如果大于零，视为用户需要这个配方用特定的建筑生产，此处记录的是CalcDB.assemblerDict[对应配方的type]的list中的地址。
    }

    public class ItemConfig
    {
        public int ID; // itemId
        public int recipeID; // 如果大于零，视为用户需要这个物品（不作为副产物的部分）必须由该配方生产
        public bool consideredAsOre; // 如果为true，无论如何视为原矿，直接输入产量。如果为false，则根据itemData.defaultAsOre决定。
    }
}
