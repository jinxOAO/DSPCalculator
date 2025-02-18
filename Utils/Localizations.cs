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

            LocalizationModule.RegisterTranslation("来自calc", "> From ", "> 来自", "- From ");
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
            LocalizationModule.RegisterTranslation("视为原矿说明", "Treating this item as raw ore will no longer count towards the production line that produces it. This item will serve as the basic input as a directly obtainable original item.\nIf you hold down Shift and click, it will also open a new calculator window and calculate the production line for this item separately.", "将该物品视为原矿，将不再计算生产该物品的产线。该物品将作为直接可获取的原始物品作为基本输入。\n若按住Shift点击，还会打开一个新的计算器窗口，并单独计算此物品的生产线。", "Treating this item as raw ore will no longer count towards the production line that produces it. This item will serve as the basic input as a directly obtainable original item.\nIf you hold down Shift and click, it will also open a new calculator window and calculate the production line for this item separately.");

            LocalizationModule.RegisterTranslation("不再视为原矿标题", "Remove From Raw Ore", "不再视为原矿", "Remove From Raw Ore");
            LocalizationModule.RegisterTranslation("不再视为原矿说明", "No longer treat this item as directly obtainable raw material.", "不再将该物品视为可直接获得的原矿。", "No longer treat this item as directly obtainable raw material.");

            LocalizationModule.RegisterTranslation("在新窗口中计算标题", "Calculate In New Window", "在新窗口中计算", "Calculate In New Window");
            LocalizationModule.RegisterTranslation("在新窗口中计算说明", "Open a new quantification calculator window and calculate the production line for this item in the new window.", "打开一个新的量化计算器窗口，在该窗口中计算此物品的生产线。", "Open a new quantification calculator window and calculate the production line for this item in the new window.");

            LocalizationModule.RegisterTranslation("还原默认配置标题", "Reset All Configuration", "还原默认配置", "Reset All Configuration");
            LocalizationModule.RegisterTranslation("还原默认配置说明", "All preferred production buildings, proliferator settings, raw ore settings, etc. will be restored to default configurations.", "所有首选生产建筑、增产、原矿等设置将被还原为默认配置。", "All preferred production buildings, proliferator settings, raw ore settings, etc. will be restored to default configurations.");

            LocalizationModule.RegisterTranslation("预估电量", "Power Consumption Est.", "预估电量需求", "Power Consumption Est.");
            LocalizationModule.RegisterTranslation("工厂需求", "Production Facilities", "生产设施", "Production Facilities");
            LocalizationModule.RegisterTranslation("原矿需求", "Raw Resources Demands", "原材料需求", "Raw Resources Demands");
            LocalizationModule.RegisterTranslation("副产物和溢出产物", "By-product / Excessive product", "副产物 / 过量产物", "By-product / Excessive product");
            //LocalizationModule.RegisterTranslation("强制增产效能", "Proliferator Extra              %", "强制增产效能              %", "Proliferator Extra              %");
            //LocalizationModule.RegisterTranslation("强制加速效能", "Proliferator Spd+              %", "强制加速效能              %", "Proliferator Spd+              %");
            LocalizationModule.RegisterTranslation("强制增产效能", "Proliferator Extra", "强制增产效能", "Proliferator Extra");
            LocalizationModule.RegisterTranslation("强制加速效能", "Proliferator Spd+", "强制加速效能", "Proliferator Spd+");

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
            LocalizationModule.RegisterTranslation("混带显示说明", "Calculated with 4 default stacks, the step size of product for calculation is 60/min. \nThis feature is still in the testing stage and cannot guarantee the operation of mixed belts.\nAnd this mode is not suitable for computing large-scale production lines", "以4默认堆叠计算，产量计算的步长为60/min。\n该功能尚在测试阶段，并不能确保混带的运行，\n且该模式不适用于计算大规模生产线。", "Calculated with 4 default stacks, the step size of product for calculation is 60/min. \nThis feature is still in the testing stage and cannot guarantee the operation of mixed belts.\nAnd this mode is not suitable for computing large-scale production lines");

            LocalizationModule.RegisterTranslation("份calc", "units", "份", "units");
            LocalizationModule.RegisterTranslation("份数标题", "Unit", "份", "Unit");
            LocalizationModule.RegisterTranslation("份数说明", "Each unit represents 60/min, which is equivalent to the capacity of the sorter at a distance of 3 grids.\nSorter value: represents the required quantity of sorters at 1/2/3 grid distances.", "每1份为60/min，相当于分拣器在3格距离的运力。\n分拣器数值：代表1/2/3格距离的分拣器的所需数量。", "Each unit represents 60/min, which is equivalent to the capacity of the sorter at a distance of 3 grids.\nSorter value: represents the required quantity of sorters at 1/2/3 grid distances.");

            LocalizationModule.RegisterTranslation("生产设施数量显示向上取整", "Round up facilitiy number", "生产设施数向上取整", "Round up facilitiy number");
            LocalizationModule.RegisterTranslation("增产剂并入产线", "Add proliferator product line", "将增产剂并入产线", "Add proliferator product line");
            LocalizationModule.RegisterTranslation("增产剂并入产线描述", "Proliferators will no longer be considered as directly input raw materials from external sources, but will be incorporated into the current production line to calculate the overall production line demand. When calculating the demand for proliferators, any one of them is considered to have been self sprayed before being used for spraying on the production line.\nThe calculation of additional proliferator types (e.g. Mk.IV) added by mod is currently not supported.", "增产剂将不再视为外部直接输入的原料，而是并入当前产线中计算整体产线的需求。计算增产剂需求量时，任何增产剂都视作经过自喷涂后再用于产线的喷涂。\n此选项目前不支持mod添加的额外增产剂类型（例如Mk.IV型）的计算。", "Proliferators will no longer be considered as directly input raw materials from external sources, but will be incorporated into the current production line to calculate the overall production line demand. When calculating the demand for proliferators, any one of them is considered to have been self sprayed before being used for spraying on the production line.\nThe calculation of additional proliferator types (e.g. MK.IV) added by mod is currently not supported.");

            LocalizationModule.RegisterTranslation("增产剂生产消耗比产出多警告", "The production process of proliferator consumes more of itself than it produces, which cannot be calculated. Please reset the configurations.", "增产剂生产过程对自身的消耗比产出多，无法计算。请重置用户设置。", "The production process of proliferator consumes more of itself than it produces, which cannot be calculated. Please reset the configurations.");
            LocalizationModule.RegisterTranslation("求解出错警告", "There is no correct solution in solving process! \nPlease try reset all configurations.", "配方求解过程出现无解情况！\n请尝试还原默认配置。", "There is no correct solution in solving process! \nPlease try reset all configurations.");

            LocalizationModule.RegisterTranslation("混带需求", "Mix-Belt Demands", "混带需求", "Mix-Belt Demands");
            LocalizationModule.RegisterTranslation("条calc", "lines", "条", "lines");
            LocalizationModule.RegisterTranslation("标记为已完成", "Mark as completed", "标记为已完成", "Mark as completed");


            LocalizationModule.RegisterTranslation("使用星际组装厂按钮标题", "Interstellar Assembly", "星际组装厂", "Interstellar Assembly");
            LocalizationModule.RegisterTranslation("使用星际组装厂按钮说明", "Click to switch all recipes' production facilities to Interstellar Assembly. Click again to switch the specialization of Interstellar Assembly.\nAttention: Due to the internal transmission and specialized settings of the Interstellar Assembly, the actual consumption of proliferator may be less than the calculated results. Therefore, to avoid affecting the calculation of main production line, it is not recommended to check the option of \"Add proliferator product line\". ", "将所有配方的生产设施切换为星际组装厂，重复点击以切换星际组装厂特化。\n注意：由于星际组装厂的内部传递以及特化设定，实际消耗的增产剂可能比计算结果要少，因此不推荐勾选“将增产剂并入产线”。", "Click to switch all recipes' production facilities to Interstellar Assembly. Click again to switch the specialization of Interstellar Assembly.\nAttention: Due to the internal transmission and specialized settings of the Interstellar Assembly, the actual consumption of proliferator may be less than the calculated results. Therefore, to avoid affecting the calculation of main production line, it is not recommended to check the option of \"Add proliferator product line\".");
            LocalizationModule.RegisterTranslation("无特化", "No Specialization", "无特化", "No Specialization");
            LocalizationModule.RegisterTranslation("组装厂输入", "Write", "填写", "Write");


            LocalizationModule.RegisterTranslation("右侧面板0", "Main Info", "主要信息", "Main Info");
            LocalizationModule.RegisterTranslation("右侧面板1", "Blueprint Settings", "蓝图设置", "Blueprint Settings");


            LocalizationModule.RegisterTranslation("生成蓝图0标题calc", "Sorter Preference", "生成蓝图", "Sorter Preference");
            LocalizationModule.RegisterTranslation("生成蓝图0说明calc", "Generate a blueprint for this production line and enter the blueprint paste mode.\nHold <color=#FD965EC0>Shift</color> to generate a blueprint <color=#FD965EC0>with PLS</color>.\nOther configurations can be changed in the blueprint page on the right panel. The blueprint does not include power distribution facilities.", "生成此产线的蓝图，并进入粘贴蓝图模式。\n按住<color=#FD965EC0>Shift</color>则生成<color=#FD965EC0>带有行星内物流塔</color>的蓝图。\n其他配置可在右侧蓝图设置页面中更改。蓝图无法生成配电设施。", "Generate a blueprint for this production line and start the blueprint mode mode.\nHold <color=#FD965EC0>Shift</color> to generate a blueprint <color=#FD965EC0>with PLS</color>.\nOther configurations can be changed in the blueprint page on the right panel. The blueprint does not include power distribution facilities.");


            LocalizationModule.RegisterTranslation("蓝图补充说明", "\n\nDue to tech or conveyor belt speed limitations, one blueprint can only support up to <color=#FD965EC0>{0}</color> production facilities, so you need to paste a total of <color=#FD965EC0>{1}</color> blueprints.", "\n\n由于科技或传送带最大速度限制，单个蓝图最多支持<color=#FD965EC0>{0}</color>个生产设施，因此你需要粘贴共<color=#FD965EC0>{1}</color>次蓝图。", "\n\nDue to tech or conveyor belt speed limitations, one blueprint can only support up to <color=#FD965EC0>{0}</color> production facilities, so you need to paste a total of <color=#FD965EC0>{1}</color> blueprints.");

            LocalizationModule.RegisterTranslation("蓝图行数", "Number of Rows", "优先行数", "Number of Rows");
            LocalizationModule.RegisterTranslation("蓝图行数单行", "Single", "单行", "Single");
            LocalizationModule.RegisterTranslation("蓝图行数双行", "Double", "双行", "Double");
            //LocalizationModule.RegisterTranslation("蓝图行数自动", "Auto", "自动", "Auto");
            LocalizationModule.RegisterTranslation("生成喷涂机", "Spray Resources", "喷涂原材料", "Spray Resources");
            LocalizationModule.RegisterTranslation("生成喷涂机自动", "Auto", "自动", "Auto");
            LocalizationModule.RegisterTranslation("生成喷涂机自动说明", "If proliferator is used in this recipe, spraying coaters will be generated for all input material belts in the blueprint, otherwise no spraying coater will be generated.", "如果配方使用了增产剂，则在蓝图中为所有原材料的传送带生成喷涂机，否则不生成喷涂机。", "If proliferator is used in this recipe, spraying coaters will be generated for all input material belts in the blueprint, otherwise no spraying coater will be generated.");
            LocalizationModule.RegisterTranslation("生成喷涂机总是", "Always", "总是", "Always");
            LocalizationModule.RegisterTranslation("生成喷涂机从不", "Never", "从不", "Never");

            LocalizationModule.RegisterTranslation("生成产物喷涂机", "Spray Products", "喷涂产物", "Spray Products");

            LocalizationModule.RegisterTranslation("物流塔提供增产剂", "PLS provide proliferators", "物流塔提供增产剂", "PLS provide proliferators");
            LocalizationModule.RegisterTranslation("物流塔提供增产剂是", "Yes", "是", "Yes");
            LocalizationModule.RegisterTranslation("物流塔提供增产剂否", "No", "否", "No");

            LocalizationModule.RegisterTranslation("首选传送带", "Belt Preference", "首选传送带", "Belt Preference");
            LocalizationModule.RegisterTranslation("首选传送带最高级", "Highest", "最高", "Highest");
            LocalizationModule.RegisterTranslation("首选传送带最高级说明", "Only use the highest level conveyor belt.", "只使用最高级传送带。", "Only use the highest level conveyor belt.");
            LocalizationModule.RegisterTranslation("首选传送带最便宜", "Cheapest", "低价", "Cheapest");
            LocalizationModule.RegisterTranslation("首选传送带最便宜说明", "On the premise of ensuring compliance with the production line, use the lowest level conveyor belt.", "在保证满足产线的情况下，使用最低级的传送带。", "On the premise of ensuring compliance with the production line, use the lowest level conveyor belt.");
            LocalizationModule.RegisterTranslation("传送带科技限制", "Belt Tech Limit", "传送带科技限制", "Belt Tech Limit");
            LocalizationModule.RegisterTranslation("传送带科技限制无限制", "No Limit", "无限制", "No Limit");
            LocalizationModule.RegisterTranslation("传送带科技限制当前科技", "Current Tech", "当前科技", "Current Tech");

            LocalizationModule.RegisterTranslation("首选分拣器", "Sorter Preference", "首选分拣器", "Sorter Preference");
            LocalizationModule.RegisterTranslation("首选分拣器最高级", "Highest", "最高", "Highest");
            LocalizationModule.RegisterTranslation("首选分拣器最高级说明", "Only use the highest level sorter.", "只使用最高级分拣器。", "Only use the highest level sorter.");
            LocalizationModule.RegisterTranslation("首选分拣器最便宜", "Cheapest", "低价", "Cheapest");
            LocalizationModule.RegisterTranslation("首选分拣器最便宜说明", "On the premise of ensuring compliance with the production line, use the lowest level sorter.", "在保证满足产线的情况下，使用最低级的传送带。", "On the premise of ensuring compliance with the production line, use the lowest level sorter.");
            LocalizationModule.RegisterTranslation("分拣器科技限制", "Sorter Tech Limit", "分拣器科技限制", "Sorter Tech Limit");
            LocalizationModule.RegisterTranslation("分拣器科技限制无限制", "No Limit", "无限制", "No Limit");
            LocalizationModule.RegisterTranslation("分拣器科技限制当前科技", "Current Tech", "当前科技", "Current Tech");

            LocalizationModule.RegisterTranslation("传送带堆叠", "Item Stack", "物料堆叠", "Item Stack");
            LocalizationModule.RegisterTranslation("传送带堆叠当前科技", "Current Tech", "当前科技", "Current Tech");

        }
    }
}
