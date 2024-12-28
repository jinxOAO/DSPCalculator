using CommonAPI.Systems.ModLocalization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSPCalculator
{
    public class Localizations
    {
        public static void AddLocalizations()
        {
            LocalizationModule.RegisterTranslation("来自calc", "- From ", "- 来自", "- From ");
            LocalizationModule.RegisterTranslation("实际需求calc", "- Actual demand  ", "- 实际需求  ", "- Actual demand  ");
            LocalizationModule.RegisterTranslation("溢出calc", "- Excessive output  ", "- 产出过量  ", "- Excessive output  ");
            LocalizationModule.RegisterTranslation("当前配方", "current recipe", "当前配方", "current recipe");
            LocalizationModule.RegisterTranslation("产出过量标签", "Excessive", "产出过量", "Excessive");
            LocalizationModule.RegisterTranslation("溢出或副产物", "Overflow / By-product", "溢出/副产物", "Overflow / By-product");
            LocalizationModule.RegisterTranslation("产出calc", "Total Output", "总产出", "Total Output");

            LocalizationModule.RegisterTranslation("更改配方标题", "Change Recipe", "更改配方", "Change Recipe");
            LocalizationModule.RegisterTranslation("更改配方说明", "Click to specify the recipe for producing this item. This setting will remain in effect for this item until the rule is cleared.", "点击以指定生产此物品的配方。在清除规则前，该设定将一直对此物品生效。", "Click to specify the recipe for producing this item. This setting will remain in effect for this item until the rule is cleared.");
            LocalizationModule.RegisterTranslation("清除配方设定标题", "Clear Recipe Specification", "清除强制指定的配方", "Clear Recipe Specification");
            LocalizationModule.RegisterTranslation("清除配方设定说明", "No longer specifying that this item must use a certain recipe, it can be selected by the calculator itself.", "不再指定此物品必须使用某个配方，可由计算器自由选取。", "No longer specifying that this item must use a certain recipe, it can be selected by the calculator itself.");
            LocalizationModule.RegisterTranslation("视为原矿标题", "Treat as Raw Ore", "视为原矿", "Treat as Raw Ore");
            LocalizationModule.RegisterTranslation("视为原矿说明", "Treating this item as raw ore will no longer count towards the production line that produces it. This item will serve as the basic input as a directly obtainable original item.", "将该物品视为原矿，将不再计算生产该物品的产线。该物品将作为直接可获取的原始物品作为基本输入。", "Treating this item as raw ore will no longer count towards the production line that produces it. This item will serve as the basic input as a directly obtainable original item.");

            LocalizationModule.RegisterTranslation("不再视为原矿标题", "Remove From Raw Ore", "不再视为原矿", "Remove From Raw Ore");
            LocalizationModule.RegisterTranslation("不再视为原矿说明", "No longer treat this item as directly obtainable raw material.", "不再将该物品视为可直接获得的原矿。", "No longer treat this item as directly obtainable raw material.");

            LocalizationModule.RegisterTranslation("原矿需求", "Raw Resources Demands", "原材料需求", "Raw Resources Demands");
            LocalizationModule.RegisterTranslation("副产物和溢出产物", "By-product / Excessive product", "副产物 / 过量产物", "By-product / Excessive product");

        }
    }
}
