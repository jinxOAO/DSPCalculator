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
            LocalizationModule.RegisterTranslation("量化计算器", "Calculator", "量化计算器", "Calculator");
            LocalizationModule.RegisterTranslation("设置目标产物", "Target Product", "设置目标产物", "Target Product");

            LocalizationModule.RegisterTranslation("来自calc", "- From ", "- 来自", "- From ");
            LocalizationModule.RegisterTranslation("实际需求calc", "- Actual demand  ", "- 实际需求  ", "- Actual demand  ");
            LocalizationModule.RegisterTranslation("溢出calc", "- Excessive output  ", "- 产出过量  ", "- Excessive output  ");
            LocalizationModule.RegisterTranslation("当前配方", "current recipe", "当前配方", "current recipe");
            LocalizationModule.RegisterTranslation("产出过量标签", "Excessive", "产出过量", "Excessive");
            LocalizationModule.RegisterTranslation("溢出或副产物", "Overflow / By-product", "溢出/副产物", "Overflow / By-product");
            LocalizationModule.RegisterTranslation("产出calc", "Total Output", "总产出", "Total Output");
            LocalizationModule.RegisterTranslation("存在环路警告", "There is a no solution loop in the recipe solving process! \nThe changes will not take effect, please try reset all configurations.", "配方求解过程出现环路！\n更改将不会生效，请尝试还原默认配置。", "There is a no solution loop in the recipe solving process! \nThe changes will not take effect, please try reset all configurations.");

            LocalizationModule.RegisterTranslation("更改配方标题", "Change Recipe", "更改配方", "Change Recipe");
            LocalizationModule.RegisterTranslation("更改配方说明", "Click to specify the recipe for producing this item. This setting will remain in effect for this item until the rule is cleared.", "点击以指定生产此物品的配方。在清除规则前，该设定将一直对此物品生效。", "Click to specify the recipe for producing this item. This setting will remain in effect for this item until the rule is cleared.");
            LocalizationModule.RegisterTranslation("清除配方设定标题", "Clear Recipe Specification", "清除强制指定的配方", "Clear Recipe Specification");
            LocalizationModule.RegisterTranslation("清除配方设定说明", "No longer specifying that this item must use a certain recipe, it can be selected by the calculator itself.", "不再指定此物品必须使用某个配方，可由计算器自由选取。", "No longer specifying that this item must use a certain recipe, it can be selected by the calculator itself.");
            LocalizationModule.RegisterTranslation("视为原矿标题", "Treat as Raw Ore", "视为原矿", "Treat as Raw Ore");
            LocalizationModule.RegisterTranslation("视为原矿说明", "Treating this item as raw ore will no longer count towards the production line that produces it. This item will serve as the basic input as a directly obtainable original item.", "将该物品视为原矿，将不再计算生产该物品的产线。该物品将作为直接可获取的原始物品作为基本输入。", "Treating this item as raw ore will no longer count towards the production line that produces it. This item will serve as the basic input as a directly obtainable original item.");

            LocalizationModule.RegisterTranslation("不再视为原矿标题", "Remove From Raw Ore", "不再视为原矿", "Remove From Raw Ore");
            LocalizationModule.RegisterTranslation("不再视为原矿说明", "No longer treat this item as directly obtainable raw material.", "不再将该物品视为可直接获得的原矿。", "No longer treat this item as directly obtainable raw material.");

            LocalizationModule.RegisterTranslation("还原默认配置标题", "Reset All Configuration", "还原默认配置", "Reset All Configuration");
            LocalizationModule.RegisterTranslation("还原默认配置说明", "All preferred production buildings, proliferator settings, raw ore settings, etc. will be restored to default configurations.", "所有首选生产建筑、增产、原矿等设置将被还原为默认配置。", "All preferred production buildings, proliferator settings, raw ore settings, etc. will be restored to default configurations.");

            LocalizationModule.RegisterTranslation("预估电量", "Power Consumption Est.", "预估电量需求", "Power Consumption Est.");
            LocalizationModule.RegisterTranslation("工厂需求", "Production Facilities", "生产设施", "Production Facilities");
            LocalizationModule.RegisterTranslation("原矿需求", "Raw Resources Demands", "原材料需求", "Raw Resources Demands");
            LocalizationModule.RegisterTranslation("副产物和溢出产物", "By-product / Excessive product", "副产物 / 过量产物", "By-product / Excessive product");
            LocalizationModule.RegisterTranslation("强制增产效能", "Proliferator Extra              %", "强制增产效能              %", "Proliferator Extra              %");
            LocalizationModule.RegisterTranslation("强制加速效能", "Proliferator Spd+              %", "强制加速效能              %", "Proliferator Spd+              %");

            LocalizationModule.RegisterTranslation("强制增产效能标题", "Specifying the Proliferator Extra Product Efficiency", "强制指定增产剂增产效能", "Specifying the Proliferator Extra Product Efficiency");
            LocalizationModule.RegisterTranslation("强制增产效能说明", "If checked, each item in the calculator will be calculated using the player's input extra product efficiency, ignoring the game's original proliferator properties, regardless of which proliferator is selected. (Unless you've chosen NOT USE PROLIFERATOR button)", "勾选此项后，计算器的每一项都将使用玩家输入的增产/加速效能来进行计算，而无视游戏原本的增产/加速比例设定，无论选择了何种增产剂。（除非你选择了“不使用增产剂”按钮）", "If checked, each item in the calculator will be calculated using the player's input extra product efficiency, ignoring the game's original proliferator properties, regardless of which proliferator is selected. (Unless you've chosen NOT USE PROLIFERATOR button)");
            LocalizationModule.RegisterTranslation("强制加速效能标题", "Specifying the Proliferator Production Speedup Efficiency", "强制指定增产剂加速效能", "Specifying the Proliferator Production Speedup Efficiency");
            LocalizationModule.RegisterTranslation("强制加速效能说明", "If checked, each item in the calculator will be calculated using the player's input production speedup efficiency, ignoring the game's original proliferator properties, regardless of which proliferator is selected. (Unless you've chosen NOT USE PROLIFERATOR button)", "勾选此项后，计算器的每一项都将使用玩家输入的增产/加速效能来进行计算，而无视游戏原本的增产/加速比例设定，无论选择了何种增产剂。（除非你选择了“不使用增产剂”按钮）", "If checked, each item in the calculator will be calculated using the player's input production speedup efficiency, ignoring the game's original proliferator properties, regardless of which proliferator is selected. (Unless you've chosen NOT USE PROLIFERATOR button)");

            LocalizationModule.RegisterTranslation("收起/展开", "Fold / Unfold", "收起/展开窗口", "Fold / Unfold");
            LocalizationModule.RegisterTranslation("生产加速calc", "Production Speedup", "生产加速", "Production Speedup");
            LocalizationModule.RegisterTranslation("额外产出calc", "Extra products", "额外产出", "Extra products");


            LocalizationModule.RegisterTranslation("打开量化计算器窗口", "Open the Quantitative Calculator Window", "打开量化计算器窗口", "Open the Quantitative Calculator Window");

            LocalizationModule.RegisterTranslation("切换计算器窗口大小", "Fold/Unfold Calculator Window", "切换计算器窗口大小", "Fold/Unfold Calculator Window");

            LocalizationModule.RegisterTranslation("显示混带信息", "Show Mix-belt Scheme", "显示混带信息", "Show Mix-belt Scheme");
            LocalizationModule.RegisterTranslation("混带显示标题", "Show Mix-belt Scheme", "显示混带信息", "Show Mix-belt Scheme");
            LocalizationModule.RegisterTranslation("混带显示说明", "Calculated with 4 default stacks, the step size of product for calculation is 30/min. \nThis feature is still in the testing stage and cannot guarantee the operation of mixed belts.", "以4默认堆叠计算，产量计算的步长为30/min。\n该功能尚在测试阶段，并不能确保混带的运行。", "Calculated with 4 default stacks, the step size of product for calculation is 30/min. \nThis feature is still in the testing stage and cannot guarantee the operation of mixed belts.");

        }
    }
}
