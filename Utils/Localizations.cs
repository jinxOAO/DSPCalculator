using CommonAPI.Systems.ModLocalization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DSPCalculator
{
    public class Localizations
    {
        public static string allENTranslations = "";
        public static void AddLocalizations()
        {
            RegisterTranslation("量化计算器", "Calculator", "量化计算器", "Calculator");
            RegisterTranslation("设置目标产物", "Target Product", "设置目标产物", "Target Product");
            RegisterTranslation("目标产物", "Target Products", "目标产物", "Target Products");

            RegisterTranslation("来自calc", "> From ", "> 来自", "- From ");
            RegisterTranslation("实际需求calc", "- Actual demand  ", "- 实际需求  ", "- Actual demand  ");
            RegisterTranslation("溢出calc", "- Excessive output  ", "- 产出过量  ", "- Excessive output  ");
            RegisterTranslation("当前配方", "current recipe", "当前配方", "current recipe");
            RegisterTranslation("产出过量标签", "Excessive", "产出过量", "Excessive");
            RegisterTranslation("溢出或副产物", "Overflow / By-product", "溢出/副产物", "Overflow / By-product");
            RegisterTranslation("产出calc", "Total Output", "总产出", "Total Output");
            RegisterTranslation("存在环路警告", "There is a no solution loop in the recipe solving process! \nThe changes will not take effect, please try reset all configurations.", "配方求解过程出现环路！\n更改将不会生效，请尝试还原默认配置。", "There is a no solution loop in the recipe solving process! \nThe changes will not take effect, please try reset all configurations.");
            RegisterTranslation("配方成环切换到下一个配方", "New recipe will cause a loop, skipping that recipe and continuing to switch to the next recipe to try.", "切换的新配方会导致环路，跳过该配方并继续切换到下一个配方尝试。", "New recipe will cause a loop, skipping that recipe and continuing to switch to the next recipe to try.");

            RegisterTranslation("更改配方标题", "Change Recipe", "更改配方", "Change Recipe");
            RegisterTranslation("更改配方说明", "Click to specify the recipe for producing this item. This setting will remain in effect for this item until the rule is cleared.", "点击以指定生产此物品的配方。在清除规则前，该设定将一直对此物品生效。", "Click to specify the recipe for producing this item. This setting will remain in effect for this item until the rule is cleared.");
            RegisterTranslation("切换配方说明", "Click to switch the recipe for producing this item. This setting will remain in effect for this item until the rule is cleared.", "点击以切换生产此物品的配方。在清除规则前，该设定将一直对此物品生效。", "Click to switch the recipe for producing this item. This setting will remain in effect for this item until the rule is cleared.");
            RegisterTranslation("清除配方设定标题", "Clear Recipe Specification", "清除强制指定的配方", "Clear Recipe Specification");
            RegisterTranslation("清除配方设定说明", "No longer specifying that this item must use a certain recipe, it can be selected by the calculator itself.", "不再指定此物品必须使用某个配方，可由计算器自由选取。", "No longer specifying that this item must use a certain recipe, it can be selected by the calculator itself.");
            RegisterTranslation("视为原矿标题", "Treat as Raw Ore", "视为原矿", "Treat as Raw Ore");
            RegisterTranslation("视为原矿说明", "Treating this item as raw ore will no longer count towards the production line that produces it. This item will serve as the basic input as a directly obtainable original item.\nIf you hold down Shift and click, it will also open a new calculator window and calculate the production line for this item separately.", "将该物品视为原矿，将不再计算生产该物品的产线。该物品将作为直接可获取的原始物品作为基本输入。\n若按住Shift点击，还会打开一个新的计算器窗口，并单独计算此物品的生产线。", "Treating this item as raw ore will no longer count towards the production line that produces it. This item will serve as the basic input as a directly obtainable original item.\nIf you hold down Shift and click, it will also open a new calculator window and calculate the production line for this item separately.");

            RegisterTranslation("不再视为原矿标题", "Remove From Raw Ore", "不再视为原矿", "Remove From Raw Ore");
            RegisterTranslation("不再视为原矿说明", "No longer treat this item as directly obtainable raw material.", "不再将该物品视为可直接获得的原矿。", "No longer treat this item as directly obtainable raw material.");
            RegisterTranslation("移除目标产物标题", "Remove", "移除", "Remove");
            RegisterTranslation("移除目标产物说明", "Remove this target product and recalculate the production line.", "将此目标产物移除，然后重新计算生产线。", "Remove this target product and recalculate the production line.");

            RegisterTranslation("添加新目标产物标题", "Add New Target Product", "添加新的目标产物", "Add New Target Product");
            RegisterTranslation("添加新目标产物说明", "Add new target products and recalculate the total production line after completing the product and speed settings.", "添加新的目标产物，在完成产物和速度设定后，重新计算总生产线。", "Add new target products and recalculate the total production line after completing the product and speed settings.");

            RegisterTranslation("在新窗口中计算标题", "Calculate in New Window", "在新窗口中计算", "Calculate in New Window");
            RegisterTranslation("在新窗口中计算说明", "Open a new quantification calculator window and calculate the production line for this item in the new window.", "打开一个新的量化计算器窗口，在该窗口中计算此物品的生产线。", "Open a new quantification calculator window and calculate the production line for this item in the new window.");

            RegisterTranslation("移除并在新窗口中计算标题", "Remove and Calculate in New Window", "移除并在新窗口中计算", "Remove and Calculate in New Window");
            RegisterTranslation("移除并在新窗口中计算说明", "<color=#FD965EC0>Remove this item</color> from target products, then open a new quantification calculator window and calculate the production line for this item in the new window.\n\n<color=#FD965EC0>Holding Shift will retain this target product</color> instead of removing it.", "<color=#FD965EC0>移除此目标产物</color>，并打开一个新的量化计算器窗口，在该窗口中计算此物品的生产线。\n\n<color=#FD965EC0>按住Shift</color>还会在当前计算器中<color=#FD965EC0>保留此目标产物</color>。", "<color=#FD965EC0>Remove this item</color> from target products, then open a new quantification calculator window and calculate the production line for this item in the new window.\n\n<color=#FD965EC0>Holding Shift will retain this target product</color> instead of removing it.");

            RegisterTranslation("还原默认配置标题", "Reset All Configuration", "还原初始配置", "Reset All Configuration");
            RegisterTranslation("还原默认配置说明", "All preferred production buildings, proliferator settings, raw ore settings, etc. in this window will be restored to mod's vanilla configurations.", "此窗口的所有首选生产建筑、增产、原矿等设置将被还原为初始配置。", "All preferred production buildings, proliferator settings, raw ore settings, etc. in this window will be restored to mod's vanilla configurations.");

            RegisterTranslation("预估电量", "Power Consumption Est.", "预估电量需求", "Power Consumption Est.");
            RegisterTranslation("工厂需求", "Production Facilities", "生产设施", "Production Facilities");
            RegisterTranslation("原矿需求", "Raw Resources Demands", "原材料需求", "Raw Resources Demands");
            RegisterTranslation("副产物和溢出产物", "By-product / Excessive product", "副产物 / 过量产物", "By-product / Excessive product");
            //RegisterTranslation("强制增产效能", "Proliferator Extra              %", "强制增产效能              %", "Proliferator Extra              %");
            //RegisterTranslation("强制加速效能", "Proliferator Spd+              %", "强制加速效能              %", "Proliferator Spd+              %");
            RegisterTranslation("强制增产效能", "Proliferator Extra", "强制增产效能", "Proliferator Extra");
            RegisterTranslation("强制加速效能", "Proliferator Spd+", "强制加速效能", "Proliferator Spd+");

            RegisterTranslation("强制增产效能标题", "Specifying the Proliferator Extra Product Efficiency", "强制指定增产剂增产效能", "Specifying the Proliferator Extra Product Efficiency");
            RegisterTranslation("强制增产效能说明", "If checked, each item in the calculator will be calculated using the player's input extra product efficiency, ignoring the game's current proliferator properties, regardless of which proliferator is selected. (Unless you've chosen NOT USE PROLIFERATOR button)", "勾选此项后，计算器的每一项都将使用玩家输入的增产/加速效能来进行计算，而无视游戏当前的增产/加速比例设定，无论选择了何种增产剂。（除非你选择了“不使用增产剂”按钮）", "If checked, each item in the calculator will be calculated using the player's input extra product efficiency, ignoring the game's current proliferator properties, regardless of which proliferator is selected. (Unless you've chosen NOT USE PROLIFERATOR button)");
            RegisterTranslation("强制加速效能标题", "Specifying the Proliferator Production Speedup Efficiency", "强制指定增产剂加速效能", "Specifying the Proliferator Production Speedup Efficiency");
            RegisterTranslation("强制加速效能说明", "If checked, each item in the calculator will be calculated using the player's input production speedup efficiency, ignoring the game's current proliferator properties, regardless of which proliferator is selected. (Unless you've chosen NOT USE PROLIFERATOR button)", "勾选此项后，计算器的每一项都将使用玩家输入的增产/加速效能来进行计算，而无视游戏当前的增产/加速比例设定，无论选择了何种增产剂。（除非你选择了“不使用增产剂”按钮）", "If checked, each item in the calculator will be calculated using the player's input production speedup efficiency, ignoring the game's current proliferator properties, regardless of which proliferator is selected. (Unless you've chosen NOT USE PROLIFERATOR button)");

            RegisterTranslation("收起/展开", "Fold / Unfold", "收起/展开窗口", "Fold / Unfold");
            RegisterTranslation("生产加速calc", "Production Speedup", "生产加速", "Production Speedup");
            RegisterTranslation("额外产出calc", "Extra products", "额外产出", "Extra products");


            RegisterTranslation("打开量化计算器窗口", "Open the Quantitative Calculator Window", "打开量化计算器窗口", "Open the Quantitative Calculator Window");

            RegisterTranslation("切换计算器窗口大小", "Fold/Unfold Calculator Window", "切换计算器窗口大小", "Fold/Unfold Calculator Window");

            RegisterTranslation("显示混带信息", "Show Mix-belt Scheme", "显示混带信息", "Show Mix-belt Scheme");
            RegisterTranslation("混带显示标题", "Show Mix-belt Scheme", "显示混带信息", "Show Mix-belt Scheme");
            RegisterTranslation("混带显示说明", "Calculated with 4 default stacks, the step size of product for calculation is 60/min. \nThis feature is still in the testing stage and cannot guarantee the operation of mixed belts.\nAnd this mode is not suitable for computing large-scale production lines", "以4默认堆叠计算，产量计算的步长为60/min。\n该功能尚在测试阶段，并不能确保混带的运行，\n且该模式不适用于计算大规模生产线。", "Calculated with 4 default stacks, the step size of product for calculation is 60/min. \nThis feature is still in the testing stage and cannot guarantee the operation of mixed belts.\nAnd this mode is not suitable for computing large-scale production lines");

            RegisterTranslation("份calc", "units", "份", "units");
            RegisterTranslation("份数标题", "Unit", "份", "Unit");
            RegisterTranslation("份数说明", "Each unit represents 60/min, which is equivalent to the capacity of the sorter at a distance of 3 grids.\nSorter value: represents the required quantity of sorters at 1/2/3 grid distances.", "每1份为60/min，相当于分拣器在3格距离的运力。\n分拣器数值：代表1/2/3格距离的分拣器的所需数量。", "Each unit represents 60/min, which is equivalent to the capacity of the sorter at a distance of 3 grids.\nSorter value: represents the required quantity of sorters at 1/2/3 grid distances.");

            RegisterTranslation("生产设施数量显示向上取整", "Round up facilitiy number", "生产设施向上取整", "Round up facilitiy number");
            RegisterTranslation("增产剂并入产线", "Add proliferator product line", "将增产剂并入产线", "Add proliferator product line");
            RegisterTranslation("增产剂并入产线描述", "Proliferators will no longer be considered as directly input raw materials from external sources, but will be incorporated into the current production line to calculate the overall production line demand. When calculating the demand for proliferators, any one of them is considered to have been self sprayed before being used for spraying on the production line.\nThe calculation of additional proliferator types (e.g. Mk.IV) added by mod is currently not supported.", "增产剂将不再视为外部直接输入的原料，而是并入当前产线中计算整体产线的需求。计算增产剂需求量时，任何增产剂都视作经过自喷涂后再用于产线的喷涂。\n此选项目前不支持mod添加的额外增产剂类型（例如Mk.IV型）的计算。", "Proliferators will no longer be considered as directly input raw materials from external sources, but will be incorporated into the current production line to calculate the overall production line demand. When calculating the demand for proliferators, any one of them is considered to have been self sprayed before being used for spraying on the production line.\nThe calculation of additional proliferator types (e.g. MK.IV) added by mod is currently not supported.");

            RegisterTranslation("增产剂生产消耗比产出多警告", "The production process of proliferator consumes more of itself than it produces, which cannot be calculated. Please reset the configurations.", "增产剂生产过程对自身的消耗比产出多，无法计算。请重置用户设置。", "The production process of proliferator consumes more of itself than it produces, which cannot be calculated. Please reset the configurations.");
            RegisterTranslation("求解出错警告", "There is no correct solution in solving process! \nPlease try reset all configurations.", "配方求解过程出现无解情况！\n请尝试还原默认配置。", "There is no correct solution in solving process! \nPlease try reset all configurations.");

            RegisterTranslation("混带需求", "Mix-Belt Demands", "混带需求", "Mix-Belt Demands");
            RegisterTranslation("条calc", "lines", "条", "lines");
            RegisterTranslation("标记为已完成", "Mark as completed", "标记为已完成", "Mark as completed");


            RegisterTranslation("使用星际组装厂按钮标题", "Interstellar Assembly", "星际组装厂", "Interstellar Assembly");
            RegisterTranslation("使用星际组装厂按钮说明", "Click to switch all recipes' production facilities to Interstellar Assembly. Click again to switch the specialization of Interstellar Assembly.\nAttention: Due to the internal transmission and specialized settings of the Interstellar Assembly, the actual consumption of proliferator may be less than the calculated results. Therefore, to avoid affecting the calculation of main production line, it is not recommended to check the option of \"Add proliferator product line\". ", "将所有配方的生产设施切换为星际组装厂，重复点击以切换星际组装厂特化。\n注意：由于星际组装厂的内部传递以及特化设定，实际消耗的增产剂可能比计算结果要少，因此不推荐勾选“将增产剂并入产线”。", "Click to switch all recipes' production facilities to Interstellar Assembly. Click again to switch the specialization of Interstellar Assembly.\nAttention: Due to the internal transmission and specialized settings of the Interstellar Assembly, the actual consumption of proliferator may be less than the calculated results. Therefore, to avoid affecting the calculation of main production line, it is not recommended to check the option of \"Add proliferator product line\".");
            RegisterTranslation("无特化", "No Specialization", "无特化", "No Specialization");
            RegisterTranslation("组装厂输入", "Write", "填写", "Write");


            RegisterTranslation("右侧面板0", "Main Info", "主要信息", "Main Info");
            RegisterTranslation("右侧面板1", "Blueprint Settings", "蓝图设置", "Blueprint Settings");


            RegisterTranslation("生成蓝图0标题calc", "Sorter Preference", "生成蓝图", "Sorter Preference");
            RegisterTranslation("生成蓝图0说明calc", "Generate a blueprint for this production line and enter the blueprint paste mode.\nHold <color=#FD965EC0>Shift</color> to generate a blueprint <color=#FD965EC0>with PLS</color>.\nHold <color=#FD965EC0>Ctrl</color> to generate a blueprint <color=#FD965EC0>with only production facilities</color>.\nOther configurations can be changed in the blueprint page on the right panel. The blueprint does not include power distribution facilities.", "生成此产线的蓝图，并进入粘贴蓝图模式。\n按住<color=#FD965EC0>Shift</color>则生成<color=#FD965EC0>带有行星内物流塔</color>的蓝图。\n按住<color=#FD965EC0>Ctrl</color>则生成<color=#FD965EC0>仅包含生产设施</color>的蓝图。\n其他配置可在右侧蓝图设置页面中更改。蓝图无法生成配电设施。", "Generate a blueprint for this production line and start the blueprint mode mode.\nHold <color=#FD965EC0>Shift</color> to generate a blueprint <color=#FD965EC0>with PLS</color>.\nHold <color=#FD965EC0>Ctrl</color> to generate a blueprint <color=#FD965EC0>with only production facilities</color>.\nOther configurations can be changed in the blueprint page on the right panel. The blueprint does not include power distribution facilities.");


            RegisterTranslation("蓝图补充说明", "\n\nDue to tech or conveyor belt speed limitations, one blueprint can only support up to <color=#FD965EC0>{0}</color> production facilities, so you need to paste a total of <color=#FD965EC0>{1}</color> blueprints.", "\n\n由于科技或传送带最大速度限制，单个蓝图最多支持<color=#FD965EC0>{0}</color>个生产设施，因此你需要粘贴共<color=#FD965EC0>{1}</color>次蓝图。", "\n\nDue to tech or conveyor belt speed limitations, one blueprint can only support up to <color=#FD965EC0>{0}</color> production facilities, so you need to paste a total of <color=#FD965EC0>{1}</color> blueprints.");
            RegisterTranslation("GB无法生成蓝图说明", "<color=#FD965EC0>Due to the low level of conveyor belt and stacking technology, a single belt cannot support a minimum of one production facility, therefore blueprint cannot be generated.</color>", "<color=#FD965EC0>由于传送带和堆叠科技过低，单带无法支持最低1台生产设施，无法生成蓝图。</color>", "<color=#FD965EC0>Due to the low level of conveyor belt and stacking technology, a single belt cannot support a minimum of one production facility, therefore blueprint cannot be generated.</color>");

            RegisterTranslation("蓝图行数", "Number of Rows", "优先行数", "Number of Rows");
            RegisterTranslation("蓝图行数单行", "Single", "单行", "Single");
            RegisterTranslation("蓝图行数双行", "Double", "双行", "Double");
            //RegisterTranslation("蓝图行数自动", "Auto", "自动", "Auto");
            RegisterTranslation("生成喷涂机", "Spray Resources", "喷涂原材料", "Spray Resources");
            RegisterTranslation("生成喷涂机自动", "Auto", "自动", "Auto");
            RegisterTranslation("生成喷涂机自动说明", "If proliferator is used in this recipe, spraying coaters will be generated for all input material belts in the blueprint, otherwise no spraying coater will be generated.", "如果配方使用了增产剂，则在蓝图中为所有原材料的传送带生成喷涂机，否则不生成喷涂机。", "If proliferator is used in this recipe, spraying coaters will be generated for all input material belts in the blueprint, otherwise no spraying coater will be generated.");
            RegisterTranslation("生成喷涂机总是", "Always", "总是", "Always");
            RegisterTranslation("生成喷涂机从不", "Never", "从不", "Never");

            RegisterTranslation("生成产物喷涂机", "Spray Products", "喷涂产物", "Spray Products");
            RegisterTranslation("产物生成喷涂机说明", "For the product coater, the default proliferator is to use the same proliferator as the raw material uses. If the raw material is set to not spray the proliferator, the product will default use Proliferator Mk.III. \n<color=#FD965EC0>If the entire production line uses multiple different proliferator settings, it is recommended to disable this option.</color>", "对于产物喷涂机的增产剂，默认使用与原材料同种的增产剂。如果原材料设置为不喷涂增产剂，则产物默认使用Mk.III型。\n<color=#FD965EC0>若整个产线使用了多种不同的增产剂设置，建议不要开启此选项。</color>", "For the product coater, the default proliferator is to use the same proliferator as the raw material uses. If the raw material is set to not spray the proliferator, the product will default use Proliferator Mk.III. \n<color=#FD965EC0>If the entire production line uses multiple different proliferator settings, it is recommended to disable this option.</color>");

            RegisterTranslation("物流塔提供增产剂", "PLS provide proliferators", "物流塔提供增产剂", "PLS provide proliferators");
            RegisterTranslation("物流塔提供增产剂是", "Yes", "是", "Yes");
            RegisterTranslation("物流塔提供增产剂否", "No", "否", "No");

            RegisterTranslation("首选传送带", "Belt Preference", "首选传送带", "Belt Preference");
            RegisterTranslation("首选传送带最高级", "Highest", "最高", "Highest");
            RegisterTranslation("首选传送带最高级说明", "Only use the highest level conveyor belt.", "只使用最高级传送带。", "Only use the highest level conveyor belt.");
            RegisterTranslation("首选传送带最便宜", "Cheapest", "低价", "Cheapest");
            RegisterTranslation("首选传送带最便宜说明", "On the premise of ensuring compliance with the production line, use the lowest level conveyor belt.", "在保证满足产线的情况下，使用最低级的传送带。", "On the premise of ensuring compliance with the production line, use the lowest level conveyor belt.");
            RegisterTranslation("传送带科技限制", "Belt Tech Limit", "传送带科技限制", "Belt Tech Limit");
            RegisterTranslation("传送带科技限制无限制", "No Limit", "无限制", "No Limit");
            RegisterTranslation("传送带科技限制当前科技", "Current Tech", "当前科技", "Current Tech");

            RegisterTranslation("首选分拣器", "Sorter Preference", "首选分拣器", "Sorter Preference");
            RegisterTranslation("首选分拣器最高级", "Highest", "最高", "Highest");
            RegisterTranslation("首选分拣器最高级说明", "Only use the highest level sorter.", "只使用最高级分拣器。", "Only use the highest level sorter.");
            RegisterTranslation("首选分拣器最便宜", "Cheapest", "低价", "Cheapest");
            RegisterTranslation("首选分拣器最便宜说明", "On the premise of ensuring compliance with the production line, use the lowest level sorter.", "在保证满足产线的情况下，使用最低级的分拣器。", "On the premise of ensuring compliance with the production line, use the lowest level sorter.");
            RegisterTranslation("分拣器科技限制", "Sorter Tech Limit", "分拣器科技限制", "Sorter Tech Limit");
            RegisterTranslation("分拣器科技限制无限制", "No Limit", "无限制", "No Limit");
            RegisterTranslation("分拣器科技限制当前科技", "Current Tech", "当前科技", "Current Tech");

            RegisterTranslation("传送带堆叠", "Item Stack", "物料堆叠", "Item Stack");
            RegisterTranslation("传送带堆叠当前科技", "Current Tech", "当前科技", "Current Tech");

            RegisterTranslation("生成黑盒蓝图标题", "Assemble to Generate Blackbox Blueprint", "拼接以生成黑盒蓝图", "Assemble to Generate Blackbox Blueprint");
            RegisterTranslation("生成黑盒蓝图说明", "<color=#FD965EC0>[Testing Function]</color> Unable to generate blueprints with excessive output.\nHold <color=#FD965EC0>Shift</color> to generate a blueprint <color=#FD965EC0>with PLS</color>.", "<color=#FD965EC0>[测试中的功能]</color>无法生成产量过大的蓝图。\n按住<color=#FD965EC0>Shift</color>则生成<color=#FD965EC0>带有行星内物流塔</color>的蓝图。", "<color=#FD965EC0>[Testing Function]</color> Unable to generate blueprints with excessive output.\nHold <color=#FD965EC0>Shift</color> to generate a blueprint <color=#FD965EC0>with PLS</color>.");
            RegisterTranslation("黑盒连接喷涂机进料口", "Blackbox Proliferator", "黑盒喷涂机供给带", "Blackbox Proliferator");
            RegisterTranslation("黑盒连接喷涂机生成", "Generate", "生成", "Generate");
            RegisterTranslation("黑盒连接喷涂机不生成", "Don't Generate", "不生成", "Don't Generate");
            RegisterTranslation("黑盒连接喷涂机进料口标题", "Generate", "生成", "Generate");
            RegisterTranslation("黑盒连接喷涂机进料口说明", "<color=#FD965EC0>This setting only applies to the generation of integrated blackbox blueprint.</color>\nThe proliferator supply conveyor belt will be automatically created for all coaters, but <color=#FD965EC0>this will cause a large number of gaps in the blueprint</color>.", "<color=#FD965EC0>此设置仅对生成拼合的整体黑盒蓝图生效。</color>\n生成喷涂机的供给传送带，会在生成蓝图时自动为所有喷涂机创建增产剂的进料传送带，但<color=#FD965EC0>这会导致蓝图产生大量空隙</color>。", "<color=#FD965EC0>This setting only applies to the generation of integrated blackbox blueprint.</color>\nThe proliferator supply conveyor belt will be automatically created for all coaters, but <color=#FD965EC0>this will cause a large number of gaps in the blueprint</color>.");
            RegisterTranslation("黑盒不连接喷涂机进料口标题", "Don't Generate", "不生成", "Don't Generate");
            RegisterTranslation("黑盒不连接喷涂机进料口说明", "<color=#FD965EC0>This setting only applies to the generation of integrated blackbox blueprint.</color>\nWhen generating blueprints, not proliferator feeding belts will be created for coaters.The <color=#FD965EC0>blueprint will be assembled as densely as possible</color>, but you will need to build those proliferators supply belts by yourself.", "<color=#FD965EC0>此设置仅对生成拼合的整体黑盒蓝图生效。</color>\n生成蓝图时，不创建喷涂机增产剂的进料传送带，<color=#FD965EC0>蓝图会尽可能密集地拼合</color>，但你需要自行铺设用于为喷涂机供给增产剂的传送带。", "<color=#FD965EC0>This setting only applies to the generation of integrated blackbox blueprint.</color>\nWhen generating blueprints, not proliferator feeding belts will be created for coaters.The <color=#FD965EC0>blueprint will be assembled as densely as possible</color>, but you will need to build those proliferators supply belts by yourself.");

            RegisterTranslation("不在行星上无法粘贴蓝图警告", "You must be on a planet to paste blueprints!", "你必须处在行星上才能粘贴蓝图！", "You must be on a planet to paste blueprints!");

            RegisterTranslation("分拣器科技不足警告", "Warning! Due to the low level of sorter technology, the sorter speed of some materials cannot support the factory's full speed. Therefore, the blueprint used  unlocked sorters to identify these locations. Please handle them manually.", "警告！由于分拣器科技水平过低，部分材料的抓取速度无法支持工厂满速运行，因此使用了你未解锁的分拣器进行标识，请手动处理这些位置。", "Warning! Due to the low level of sorter technology, the sorter speed of some materials cannot support the factory's full speed. Therefore, the blueprint used  unlocked sorters to identify these locations. Please handle them manually.");


            RegisterTranslation("传送带比例BpGB", "Belt Routes Count: ", "传送带线路数：", "Belt Routes Count: ");
            RegisterTranslation("入BpGB", "(in)", "(入)", "(in)");
            RegisterTranslation("出BpGB", "(out)", "(出)", "(out)");


            RegisterTranslation("ttenyx白糖预制蓝图提示", "This blueprint is a <color=#FD965EC0>prefabricated blueprint</color>, so the output speed and building count may not match the calculated values. Please modify the number yourself.\nThe production speed of this blueprint is 11,250/min, with Proliferator MkIII.\n<color=#FF4020C0>You MUST NOT upgrade or downgrade any belt or sorter in this blueprint!!!</color>\n\n<color=#FD965EC0>This blueprint is from TTenYX</color>", "该蓝图为固定的<color=#FD965EC0>预制蓝图</color>，因此产量与计算器中的数值不一致，请自行修改建筑数量。\n单个蓝图的产量为11250/min，使用3级增产剂\n<color=#FF4020C0>绝不要升级或降级任何蓝图中的传送带或分拣器！！！</color>\n\n<color=#FD965EC0>此蓝图来自于TTenYX</color>", "This blueprint is a <color=#FD965EC0>prefabricated blueprint</color>, so the output speed and building count may not match the calculated values. Please modify the number yourself.\nThe production speed of this blueprint is 11,250/min, with Proliferator MkIII.\n<color=#FF4020C0>You MUST NOT upgrade or downgrade any belt or sorter in this blueprint!!!</color>\n\n<color=#FD965EC0>This blueprint is from TTenYX</color>");
            RegisterTranslation("ttenyx白糖预制蓝图提示2901", "This blueprint is a <color=#FD965EC0>prefabricated blueprint</color>, so the output speed and building count may not match the calculated values. Please modify the number yourself.\nThe production speed of this blueprint is 3,750/min, with Proliferator MkIII.\n<color=#FF4020C0>You MUST NOT upgrade or downgrade any belt or sorter in this blueprint!!!</color>\n\n<color=#FD965EC0>This blueprint is from TTenYX</color>", "该蓝图为固定的<color=#FD965EC0>预制蓝图</color>，因此产量与计算器中的数值不一致，请自行修改建筑数量。\n单个蓝图的产量为3750/min，使用3级增产剂\n<color=#FF4020C0>绝不要升级或降级任何蓝图中的传送带或分拣器！！！</color>\n\n<color=#FD965EC0>此蓝图来自于TTenYX</color>", "This blueprint is a <color=#FD965EC0>prefabricated blueprint</color>, so the output speed and building count may not match the calculated values. Please modify the number yourself.\nThe production speed of this blueprint is 3,750/min, with Proliferator MkIII.\n<color=#FF4020C0>You MUST NOT upgrade or downgrade any belt or sorter in this blueprint!!!</color>\n\n<color=#FD965EC0>This blueprint is from TTenYX</color>");

            RegisterTranslation("calc警告", "Warning", "警告", "Warning");
            RegisterTranslation("calc提示", "Note", "提示", "Note");
            RegisterTranslation("蓝图生成失败", "Generation Failed", "生成失败", "Generation Failed");
            RegisterTranslation("没有任何可以生成蓝图的产线", "There is no production line that can generate blueprints.", "没有任何可以生成蓝图的产线", "There is no production line that can generate blueprints.");
            RegisterTranslation("部分蓝图无法生成说明", "At least one item in the entire production line is unable to generate a blueprint, possibly due to that it is a fractionation recipe, or it uses an Interstellar Assembly.\nTherefore, the required materials for this production line will be output with additional terminal port,\nlikewise, the output of this production line will be treated as additional inputs.\nThat production line itself needs to be completed outside of the blueprint by you yourself.\nThe relevant recipes are: ", "整个产线中有至少一条子产线无法生成蓝图，可能是由于其为分馏配方，或使用了星际组装厂\n因此：该产线的需求材料会被作为额外的蓝图输出\n该产线的产出物会被作为额外的蓝图输入\n而该产线本身需要由用户在蓝图外自行完成\n相关配方为：", "At least one item in the entire production line is unable to generate a blueprint, possibly due to that it is a fractionation recipe, or it uses an Interstellar Assembly.\nTherefore, the required materials for this production line will be output with additional terminal port,\nlikewise, the output of this production line will be treated as additional inputs.\nThat production line itself needs to be completed outside of the blueprint by you yourself.\nThe relevant recipes are: ");
            RegisterTranslation("有蓝图传送带运力不够说明", "Unable to generate blueprint due to at least one item (highlighted) having a capacity requirement that exceeds the capacity of a single conveyor belt at the current level of technology.\n\nSuggestion: Reduce the demand for the final target product,\nor increase the level of stacking technology, belt technology, and pile sorter upgrade technology,\nor change the recipe or upstream recipe of that item, or set it to raw ore (which may not necessarily be effective).", "无法生成蓝图，由于：\n至少有一种物品（高亮显示）的运力需求，超过了当前的科技等级的单条传送带的运力\n\n建议：\n减小最终产物的需求量，或\n提升堆叠科技、传送带科技、集装分拣器科技等级，或\n更改该物品的配方或上游配方、设置原矿（并不一定有效）。", "Unable to generate blueprint due to at least one item (highlighted) having a capacity requirement that exceeds the capacity of a single conveyor belt at the current level of technology.\n\nSuggestion: Reduce the demand for the final target product,\nor increase the level of stacking technology, belt technology, and pile sorter upgrade technology,\nor change the recipe or upstream recipe of that item, or set it to raw ore (which may not necessarily be effective).");
            RegisterTranslation("传送带斜插说明", "Due to the fact that the max level of the belt required to connect the bp blocks in an orthogonal manner exceeds the limitations of current technology, a diagonal direct connection method has been adopted.\n\nTo generate an orthogonal conveyor belt, it is recommended to\nupgrade the technology level of vertical construction,\nor try changing the recipe or proliferator settings (which may not necessarily be effective).", "由于以正交方式连接传送带的方法需要的传送带高度超出了当前科技的限制，因此采用了斜线直连方法\n\n若想生成正交传送带，建议：\n提升垂直建造的科技等级，或\n尝试更改配方、增产剂设定（并不一定有效）。", "Due to the fact that the max level of the belt required to connect the bp blocks in an orthogonal manner exceeds the limitations of current technology, a diagonal direct connection method has been adopted.\n\nTo generate an orthogonal conveyor belt, it is recommended to\nupgrade the technology level of vertical construction,\nor try changing the recipes or proliferator settings (which may not necessarily be effective.");
            RegisterTranslation("传送带高度科技限制说明", "A blueprint has been generated, but it may not be able to be constructed properly due to: \nThe required conveyor belt height exceeds the current technological limitations. \n\nSuggestions:\nUpgrade the technology level of vertical construction,\nor try changing the recipes or proliferator settings (which may not necessarily be effective).", "生成了蓝图，但是可能无法正常建造，由于：\n要求的传送带高度超出了当前科技的限制\n\n建议：\n提升垂直建造的科技等级，或\n尝试更改配方、增产剂设定（并不一定有效）。", "A blueprint has been generated, but it may not be able to be constructed properly due to: \nThe required conveyor belt height exceeds the current technological limitations. \n\nSuggestions:\nUpgrade the technology level of vertical construction,\nor try changing the recipes or proliferator settings (which may not necessarily be effective).");
            RegisterTranslation("传送带高度游戏限制说明", "Unable to generate blueprints due to:\nThe required conveyor belt height exceeds the game's limit.\n\nSuggestion:\nSplit the production line and generate multiple blueprints one by one,\nor try changing the recipes or proliferator settings (which may not necessarily be effective).", "无法生成蓝图，由于：\n要求的传送带高度超出了游戏的限制\n\n建议：将生产线拆分，分批次生成多个蓝图，或\n尝试更改配方或增产剂设定（并不一定有效）。", "Unable to generate blueprints due to:\nThe required conveyor belt height exceeds the game's limit.\n\nSuggestion:\nSplit the production line and generate multiple blueprints one by one,\nor try changing the recipes or proliferator settings (which may not necessarily be effective).");
            RegisterTranslation("未解锁垂直建造传送带科技警告", "Unable to generate blackbox blueprint, as you have not unlocked the technology for conveyor belt slope limitation:", "无法生成黑盒蓝图，由于你尚未解锁传送带坡度限制的科技：", "Unable to generate blackbox blueprint, as you have not unlocked the technology for conveyor belt slope limitation:");
            RegisterTranslation("calc继续", "Continue", "继续", "Continue");
            RegisterTranslation("calc取消", "Cancel", "取消", "Cancel");
            RegisterTranslation("calc关闭", "Close", "关闭", "Close");
            RegisterTranslation("calc确定", "OK", "确定", "OK");

            RegisterTranslation("calc复制标题", "Copy", "复制", "Copy");
            RegisterTranslation("calc复制说明", "Copy the target product, production line settings, and blueprint settings of the current window to the clipboard.", "复制当前窗口的目标产物、产线设置和蓝图设置到剪贴板。", "Copy the target product, production line settings, and blueprint settings of the current window to the clipboard.");
            RegisterTranslation("calc粘贴标题", "Paste", "粘贴", "Paste");
            RegisterTranslation("calc粘贴说明", "Load the target product, production line settings, and blueprint settings from the clipboard, then solve them.", "从剪贴板中加载目标产物、产线设置和蓝图设置，并进行求解。", "");
            RegisterTranslation("calc复制成功提示", "Data Copied", "复制成功", "Data Copied");
            RegisterTranslation("calc复制失败提示", "Copy Failed!!!", "复制失败！！！", "Copy Failed!!!");
            RegisterTranslation("calc粘贴成功提示", "Data Pasted", "粘贴成功", "Data Pasted");
            RegisterTranslation("calc粘贴失败提示", "Paste failed, please check the contents of the clipboard. All configurations have been reset.", "粘贴失败，请检查剪贴板内容。所有配置已被重置。", "Paste failed, please check the contents of the clipboard. All configurations have been reset.");
            RegisterTranslation("calc设置成功提示", "Succeeded", "设置成功", "Succeeded");


            RegisterTranslation("保存为默认配置标题", "Save as Dafault", "保存为默认配置", "Save as Dafault");
            RegisterTranslation("保存为默认配置说明", "Save all configurations and target products of the current window as the default configuration, and all newly opened windows in the future (even after restarting the game) will load this configuration and targets by default.\nHold Ctrl and click to clear the saved configurations. Future windows will be blank windows.", "将当前窗口的所有配置和目标产物保存为默认配置，未来所有新打开的窗口（包括重启游戏后），都将默认加载此目标产线及其配置。\n按住Ctrl点击，则清除已保存的自定义配置，未来打开的窗口将为空窗口。", "Save all configurations and target products of the current window as the default configuration, and all newly opened windows in the future (even after restarting the game) will load this configuration and targets by default.\nHold Ctrl and click to clear the saved configurations. Future windows will be blank windows.");

            RegisterTranslation("预先设置原矿标题", "Add Raw Ore", "添加原矿", "Add Raw Ore");
            RegisterTranslation("预先设置原矿说明", "Click to select an item and treat it as raw ore.\nRight click to restore all raw ore settings to default configuration.", "点击以选择一种物品，将其视为原矿。\n右键点击则还原所有原矿设定为默认状态。", "Click to select an item and treat it as raw ore.\nRight click to restore all raw ore settings to default configuration.");

            RegisterTranslation("还原配置成功", "Raw ore configuration restored", "原矿配置已还原", "Raw ore configuration restored");
            RegisterTranslation("已设置原矿", "Raw ore has been set", "原矿已设定", "Raw ore has been set");
        }

        public static void AddPtBr()
        {
            const string lcid = "1046";
            RegisterTranslationByLang(lcid,"量化计算器", "Calculadora");
            RegisterTranslationByLang(lcid,"设置目标产物", "Produto Alvo");
            RegisterTranslationByLang(lcid,"目标产物", "Produtos Alvo");

            RegisterTranslationByLang(lcid,"来自calc", "> De");
            RegisterTranslationByLang(lcid,"实际需求calc", "- Demanda Real");
            RegisterTranslationByLang(lcid,"溢出calc", "- Produção em Excesso");
            RegisterTranslationByLang(lcid,"当前配方", "receita atual");
            RegisterTranslationByLang(lcid,"产出过量标签", "Excesso");
            RegisterTranslationByLang(lcid,"溢出或副产物", "Excedente / Subproduto");
            RegisterTranslationByLang(lcid,"产出calc", "Produção Total");
            RegisterTranslationByLang(lcid,"存在环路警告", "Existe um loop sem solução no processo de resolução da receita! \nAs alterações não terão efeito, por favor, tente redefinir todas as configurações.");
            RegisterTranslationByLang(lcid,"配方成环切换到下一个配方", "A nova receita causará um loop, ignorando essa receita e mudando para a próxima para continuar tentando.");

            RegisterTranslationByLang(lcid,"更改配方标题", "Alterar Receita");
            RegisterTranslationByLang(lcid,"更改配方说明", "Clique para definir a receita de produção deste item. Esta configuração permanecerá em vigor até que seja redefinida.");
            RegisterTranslationByLang(lcid,"切换配方说明", "Clique para mudar a receita de produção deste item. Esta configuração permanecerá em vigor até que seja redefinida.");
            RegisterTranslationByLang(lcid,"清除配方设定标题", "Limpar Receita Especificada");
            RegisterTranslationByLang(lcid,"清除配方设定说明", "Deixando de especificar que este item deve usar uma certa receita, esta poderá ser selecionada pela própria calculadora.");
            RegisterTranslationByLang(lcid,"视为原矿标题", "Tratar como Matéria-Prima");
            RegisterTranslationByLang(lcid,"视为原矿说明", "Tratar este item como matéria-prima não contribuirá mais para a linha de produção que o gera. Este item servirá como insumo básico, sendo um item original diretamente coletável.\nAo segurar Shift e clicar, uma nova janela da calculadora será aberta para calcular a linha de produção deste item separadamente.");

            RegisterTranslationByLang(lcid,"不再视为原矿标题", "Remover de Matérias-Primas");
            RegisterTranslationByLang(lcid,"不再视为原矿说明", "Este item não será mais tratado como matéria-prima diretamente coletável.");
            RegisterTranslationByLang(lcid,"移除目标产物标题", "Remover");
            RegisterTranslationByLang(lcid,"移除目标产物说明", "Remova este produto alvo e recalcule a linha de produção.");

            RegisterTranslationByLang(lcid,"添加新目标产物标题", "Adicionar Novo Produto Alvo");
            RegisterTranslationByLang(lcid,"添加新目标产物说明", "Adicione novos produtos alvo e recalcule a linha de produção total após concluir as configurações de produto e velocidade.");

            RegisterTranslationByLang(lcid,"在新窗口中计算标题", "Calcular em Nova Janela");
            RegisterTranslationByLang(lcid,"在新窗口中计算说明", "Abra uma nova janela da calculadora para calcular a linha de produção deste item.");

            RegisterTranslationByLang(lcid,"移除并在新窗口中计算标题", "Remover e Calcular em Nova Janela");
            RegisterTranslationByLang(lcid,"移除并在新窗口中计算说明", "<color=#FD965EC0>Remova este item</color> dos produtos alvo, abra uma nova janela da calculadora e calcule a linha de produção para este item.\n\n<color=#FD965EC0>Segurar Shift manterá este produto alvo</color> em vez de removê-lo.");

            RegisterTranslationByLang(lcid,"还原默认配置标题", "Redefinir Todas as Configurações");
            RegisterTranslationByLang(lcid,"还原默认配置说明", "Todas as configurações de preferência desta janela para edifícios de produção, proliferadores, matérias-primas, etc. serão redefinidas para o padrão do mod.");

            RegisterTranslationByLang(lcid,"预估电量", "Estimativa de Consumo de Energia");
            RegisterTranslationByLang(lcid,"工厂需求", "Instalações de Produção");
            RegisterTranslationByLang(lcid,"原矿需求", "Demandas de Matérias-Primas");
            RegisterTranslationByLang(lcid,"副产物和溢出产物", "Subproduto / Produto em Excesso");

            RegisterTranslationByLang(lcid,"强制增产效能", "Proliferador Extra");
            RegisterTranslationByLang(lcid,"强制加速效能", "Proliferador Vel+");

            RegisterTranslationByLang(lcid,"强制增产效能标题", "Forçar Eficiência Extra do Proliferador");
            RegisterTranslationByLang(lcid,"强制增产效能说明", "Se esta opção for marcada, a calculadora usará os valores de produção extra e aceleração inseridos pelo jogador para cada item, ignorando os bónus atual do jogo, independentemente do proliferador selecionado. (A menos que tenha selecionado o botão 'NÃO USAR PROLIFERADOR').");
            RegisterTranslationByLang(lcid,"强制加速效能标题", "Forçar Eficiência de Aceleração do Proliferador");
            RegisterTranslationByLang(lcid,"强制加速效能说明", "Ao marcar esta opção, a calculadora usará os valores de produção extra e aceleração inseridos pelo jogador para todos os itens. Isto irá ignorar os bónus atual do jogo, independentemente do proliferador selecionado (a menos que o botão 'Não Usar Proliferador' seja selecionado).");

            RegisterTranslationByLang(lcid,"收起/展开", "Recolher / Expandir");
            RegisterTranslationByLang(lcid,"生产加速calc", "Aceleração da Produção");
            RegisterTranslationByLang(lcid,"额外产出calc", "Produtos Extras");


            RegisterTranslationByLang(lcid,"打开量化计算器窗口", "Abrir Calculadora de Quantidades");

            RegisterTranslationByLang(lcid,"切换计算器窗口大小", "Recolher/Expandir Janela da Calculadora");

            RegisterTranslationByLang(lcid,"显示混带信息", "Mostrar Informações da Esteira Mista");
            RegisterTranslationByLang(lcid,"混带显示标题", "Mostrar Informações da Esteira Mista");
            RegisterTranslationByLang(lcid,"混带显示说明", "Cálculo baseado numa pilha padrão de 4, com um incremento de produção de 60/min. \\nEste recurso ainda está em teste e não pode garantir o funcionamento de esteiras mistas.\nEste modo não é adequado para o cálculo de linhas de produção em grande escala.");

            RegisterTranslationByLang(lcid,"份calc", "unidades");
            RegisterTranslationByLang(lcid,"份数标题", "Unidade");
            RegisterTranslationByLang(lcid,"份数说明", "Cada unidade representa 60/min, o que equivale à capacidade de transporte de um separador a 3 espaços de distância.\nValor do separador: Representa o número de separadores necessários para distâncias de 1, 2 e 3 espaços.");

            RegisterTranslationByLang(lcid,"生产设施数量显示向上取整", "Arredondar nº de Instalações para Cima");
            RegisterTranslationByLang(lcid,"增产剂并入产线", "Adicionar Linha de Produção do Proliferador");
            RegisterTranslationByLang(lcid,"增产剂并入产线描述", "Os proliferadores deixarão de ser considerados insumos externos, sendo incorporados à linha de produção para o cálculo da demanda geral. Ao calcular a sua necessidade, assume-se que todos os proliferadores são primeiro auto-pulverizados antes de serem usados na produção.\nAtualmente, esta opção não suporta o cálculo de tipos de proliferadores adicionais adicionados por mods (ex: MK.IV).");

            RegisterTranslationByLang(lcid,"增产剂生产消耗比产出多警告", "O processo de produção do proliferador consome mais de si mesmo do que produz, tornando o cálculo impossível. Por favor, redefina as configurações.");
            RegisterTranslationByLang(lcid,"求解出错警告", "Não foi possível encontrar uma solução para a receita! \nPor favor, tente restaurar as configurações padrão.");

            RegisterTranslationByLang(lcid,"混带需求", "Demandas de Esteira Mista");
            RegisterTranslationByLang(lcid,"条calc", "linhas");
            RegisterTranslationByLang(lcid,"标记为已完成", "Marcar como Concluído");


            RegisterTranslationByLang(lcid,"使用星际组装厂按钮标题", "Montadora Interestelar");
            RegisterTranslationByLang(lcid,"使用星际组装厂按钮说明", "Clique para mudar as instalações de todas as receitas para a Montadora Interestelar. Clique novamente para alternar a sua especialização.\nAtenção: Devido à transferência interna e às configurações de especialização da Montadora Interestelar, o consumo real de proliferadores pode ser inferior ao valor calculado. Portanto, para não afetar o cálculo da linha de produção principal, não é recomendado marcar a opção \"Adicionar Linha de Produção do Proliferador\".");
            RegisterTranslationByLang(lcid,"无特化", "Sem Especialização");
            RegisterTranslationByLang(lcid,"组装厂输入", "Inserir");


            RegisterTranslationByLang(lcid,"右侧面板0", "Informações Principais");
            RegisterTranslationByLang(lcid,"右侧面板1", "Configurações do Blueprint");


            RegisterTranslationByLang(lcid,"生成蓝图0标题calc", "Gerar Blueprint");
            RegisterTranslationByLang(lcid,"生成蓝图0说明calc", "Gera um blueprint desta linha de produção e entra no modo de colagem.\nMantenha <color=#FD965EC0>Shift</color> pressionado para gerar um blueprint com Torres de Logística Planetária (PLS).\nMantenha <color=#FD965EC0>Ctrl</color> pressionado para gerar um blueprint contendo apenas as instalações de produção.\nOutras configurações podem ser alteradas na página de blueprints, no painel direito. O blueprint não inclui instalações de distribuição de energia.");


            RegisterTranslationByLang(lcid,"蓝图补充说明", "\n\nDevido a limitações de tecnologia ou da velocidade máxima das esteiras, um único blueprint suporta um máximo de <color=#FD965EC0>{0}</color> instalações de produção, pelo que terá de colar um total de <color=#FD965EC0>{1}</color> blueprints.");
            RegisterTranslationByLang(lcid,"GB无法生成蓝图说明", "<color=#FD965EC0>Não é possível gerar o blueprint: o nível da tecnologia de esteiras e de empilhamento é demasiado baixo para suportar sequer uma instalação de produção.</color>");

            RegisterTranslationByLang(lcid,"蓝图行数", "Número de Linhas");
            RegisterTranslationByLang(lcid,"蓝图行数单行", "Única");
            RegisterTranslationByLang(lcid,"蓝图行数双行", "Dupla");
            RegisterTranslationByLang(lcid,"生成喷涂机", "Pulverizar Recursos");
            RegisterTranslationByLang(lcid,"生成喷涂机自动", "Automático");
            RegisterTranslationByLang(lcid,"生成喷涂机自动说明", "Se a receita usar proliferadores, serão gerados pulverizadores para todas as esteiras de insumos no blueprint; caso contrário, não serão gerados.");
            RegisterTranslationByLang(lcid,"生成喷涂机总是", "Sempre");
            RegisterTranslationByLang(lcid,"生成喷涂机从不", "Nunca");

            RegisterTranslationByLang(lcid,"生成产物喷涂机", "Pulverizar Produtos");
            RegisterTranslationByLang(lcid,"产物生成喷涂机说明", "O proliferador padrão para o pulverizador do produto é o mesmo utilizado nos insumos. Caso os insumos não sejam pulverizados, o produto usará, por defeito, o Proliferador Mk.III.\n<color=#FD965EC0>Se toda a linha de produção usar múltiplas e diferentes configurações de proliferador, recomenda-se desativar esta opção.</color>");

            RegisterTranslationByLang(lcid,"物流塔提供增产剂", "PLS Fornece Proliferadores");
            RegisterTranslationByLang(lcid,"物流塔提供增产剂是", "Sim");
            RegisterTranslationByLang(lcid,"物流塔提供增产剂否", "Não");

            RegisterTranslationByLang(lcid,"首选传送带", "Preferência de Esteiras");
            RegisterTranslationByLang(lcid,"首选传送带最高级", "Maior nível");
            RegisterTranslationByLang(lcid,"首选传送带最高级说明", "Usar apenas a esteira de maior nível.");
            RegisterTranslationByLang(lcid,"首选传送带最便宜", "Menor nível");
            RegisterTranslationByLang(lcid,"首选传送带最便宜说明", "Usar a esteira de menor nível que garanta a capacidade de transporte necessária.");
            RegisterTranslationByLang(lcid,"传送带科技限制", "Limite das Esteiras");
            RegisterTranslationByLang(lcid,"传送带科技限制无限制", "Sem Limite");
            RegisterTranslationByLang(lcid,"传送带科技限制当前科技", "Tecnologia Atual");

            RegisterTranslationByLang(lcid,"首选分拣器", "Preferência de Separadores");
            RegisterTranslationByLang(lcid,"首选分拣器最高级", "Maior nível");
            RegisterTranslationByLang(lcid,"首选分拣器最高级说明", "Usar apenas o separador de maior nível.");
            RegisterTranslationByLang(lcid,"首选分拣器最便宜", "Menor nível");
            RegisterTranslationByLang(lcid,"首选分拣器最便宜说明", "Usar o separador de menor nível que garanta a capacidade de transporte necessária.");
            RegisterTranslationByLang(lcid,"分拣器科技限制", "Limite dos Separadores");
            RegisterTranslationByLang(lcid,"分拣器科技限制无限制", "Sem Limite");
            RegisterTranslationByLang(lcid,"分拣器科技限制当前科技", "Tecnologia Atual");

            RegisterTranslationByLang(lcid,"传送带堆叠", "Empilhamento de Itens");
            RegisterTranslationByLang(lcid,"传送带堆叠当前科技", "Tecnologia Atual");

            RegisterTranslationByLang(lcid,"生成黑盒蓝图标题", "Montar para Gerar Blueprint de Caixa-Preta");
            RegisterTranslationByLang(lcid,"生成黑盒蓝图说明", "<color=#FD965EC0>[Funcionalidade em Teste]</color> Não é possível gerar blueprints com uma produção demasiado elevada.\nMantenha <color=#FD965EC0>Shift</color> pressionado para gerar um blueprint com Torres de Logística Planetária (PLS).");
            RegisterTranslationByLang(lcid,"黑盒连接喷涂机进料口", "Proliferador de Caixa Preta");
            RegisterTranslationByLang(lcid,"黑盒连接喷涂机生成", "Gerar");
            RegisterTranslationByLang(lcid,"黑盒连接喷涂机不生成", "Não Gerar");
            RegisterTranslationByLang(lcid,"黑盒连接喷涂机进料口标题", "Gerar");
            RegisterTranslationByLang(lcid,"黑盒连接喷涂机进料口说明", "<color=#FD965EC0>Esta configuração aplica-se apenas à geração de blueprints integrados de caixa-preta.</color>\nSerá criada automaticamente uma esteira de alimentação de proliferadores para todos os pulverizadores, mas <color=#FD965EC0>isto irá gerar um grande número de espaços vazios no blueprint</color>.");
            RegisterTranslationByLang(lcid,"黑盒不连接喷涂机进料口标题", "Não Gerar");
            RegisterTranslationByLang(lcid,"黑盒不连接喷涂机进料口说明", "<color=#FD965EC0>Esta configuração aplica-se apenas à geração de blueprints integrados de caixa-preta.</color>\nAo gerar os blueprints, não serão criadas esteiras de alimentação para os proliferadores. O <color=#FD965EC0>blueprint será montado da forma mais compacta possível</color>, mas terá de construir essas esteiras de alimentação manualmente.");

            RegisterTranslationByLang(lcid,"不在行星上无法粘贴蓝图警告", "Você precisa estar em um planeta para colar blueprints!");

            RegisterTranslationByLang(lcid,"分拣器科技不足警告", "Aviso! Devido ao baixo nível da sua tecnologia de separadores, a velocidade de transporte de alguns materiais não consegue suportar a produção em velocidade máxima. Por isso, para identificar estes locais, o blueprint utilizou separadores que ainda não desbloqueou. Por favor, faça os ajustes manualmente.");


            RegisterTranslationByLang(lcid,"传送带比例BpGB", "N.º de Linhas de Esteira:");
            RegisterTranslationByLang(lcid,"入BpGB", "(entrada)");
            RegisterTranslationByLang(lcid,"出BpGB", "(saída)");


            RegisterTranslationByLang(lcid,"ttenyx白糖预制蓝图提示", "Este é um <color=#FD965EC0>blueprint pré-fabricado</color>, pelo que a sua velocidade de produção e o número de edifícios podem não corresponder aos valores calculados. Por favor, ajuste a quantidade manualmente.\nA velocidade de produção deste blueprint é de 11.250/min, com Proliferador Mk.III.\n<color=#FF4020C0>NÃO atualize nem rebaixe QUALQUER esteira ou separador deste blueprint!!!</color>\n<color=#FD965EC0>Este blueprint foi criado por TTenYX</color>");
            RegisterTranslationByLang(lcid,"ttenyx白糖预制蓝图提示2901", "Este é um <color=#FD965EC0>blueprint pré-fabricado</color>, pelo que a sua velocidade de produção e o número de edifícios podem não corresponder aos valores calculados. Por favor, ajuste a quantidade manualmente.\nA velocidade de produção deste blueprint é de 3.750/min, com Proliferador Mk.III.\n<color=#FF4020C0>NÃO atualize nem rebaixe QUALQUER esteira ou separador deste blueprint!!!</color>\n<color=#FD965EC0>Este blueprint foi criado por TTenYX</color>");

            RegisterTranslationByLang(lcid,"calc警告", "Aviso");
            RegisterTranslationByLang(lcid,"calc提示", "Nota");
            RegisterTranslationByLang(lcid,"蓝图生成失败", "Geração Falhou");
            RegisterTranslationByLang(lcid,"没有任何可以生成蓝图的产线", "Nenhuma linha de produção disponível que permita gerar um blueprint.");
            RegisterTranslationByLang(lcid,"部分蓝图无法生成说明", "Não foi possível gerar um blueprint completo porque uma ou mais secções da linha de produção não são compatíveis, possivelmente por conterem uma receita de fracionamento ou por usarem uma Montadora Interestelar.\nComo resultado:\nOs materiais necessários para essa secção serão tratados como saídas adicionais do blueprint.\nOs produtos finais dessa secção serão tratados como entradas adicionais do blueprint.\nEssa parte da linha de produção terá de ser construída manualmente por si, fora do blueprint.\nAs receitas afetadas são:");
            RegisterTranslationByLang(lcid,"有蓝图传送带运力不够说明", "Não foi possível gerar o blueprint, porque a capacidade de transporte necessária para pelo menos um item (destacado) excede a capacidade máxima de uma única esteira ao nível tecnológico atual.\nSugestões:\nReduzir a procura pelo produto final;\nMelhorar as tecnologias de empilhamento, de esteiras e do Separador de Pilha;\nAlterar a receita desse item (ou das suas receitas a montante), ou defini-lo como matéria-prima (embora esta ação possa não ser eficaz).");
            RegisterTranslationByLang(lcid,"传送带斜插说明", "Foi utilizada uma conexão diagonal direta porque a altura necessária para conectar os blocos do blueprint de forma ortogonal excede os limites da sua tecnologia atual.\nPara gerar uma conexão ortogonal, recomenda-se:\nMelhorar o nível da tecnologia de construção vertical;\nTentar alterar as receitas ou as configurações do proliferador (embora esta ação possa não ser eficaz).");
            RegisterTranslationByLang(lcid,"传送带高度科技限制说明", "O blueprint foi gerado, mas a sua construção pode não ser possível devido ao seguinte:\nA altura necessária das esteiras excede os limites da sua tecnologia atual.\nSugestões:\nMelhorar o nível da tecnologia de construção vertical;\nTentar alterar as receitas ou as configurações do proliferador (embora esta ação possa não ser eficaz).");
            RegisterTranslationByLang(lcid,"传送带高度游戏限制说明", "Não foi possível gerar o blueprint porque a altura necessária das esteiras excede o limite do jogo.\nSugestões:\nDividir a linha de produção e gerar múltiplos blueprints separadamente;\nTentar alterar as receitas ou as configurações do proliferador (embora esta ação possa não ser eficaz).");
            RegisterTranslationByLang(lcid,"未解锁垂直建造传送带科技警告", "Não é possível gerar o blueprint de caixa-preta, pois ainda não desbloqueou a tecnologia do limite de inclinação das esteiras:");
            RegisterTranslationByLang(lcid,"calc继续", "Continuar");
            RegisterTranslationByLang(lcid,"calc取消", "Cancelar");
            RegisterTranslationByLang(lcid,"calc关闭", "Fechar");
            RegisterTranslationByLang(lcid,"calc确定", "OK");

            RegisterTranslationByLang(lcid,"calc复制标题", "Copiar");
            RegisterTranslationByLang(lcid,"calc复制说明", "Copia para a área de transferência o produto alvo, as configurações da linha de produção e as configurações de blueprint da janela atual.");
            RegisterTranslationByLang(lcid,"calc粘贴标题", "Colar");
            RegisterTranslationByLang(lcid, "calc粘贴说明", "Carrega da área de transferência o produto alvo, as configurações da linha de produção e as configurações de blueprint e, de seguida, calcula a solução.");
            RegisterTranslationByLang(lcid,"calc复制成功提示", "Dados Copiados");
            RegisterTranslationByLang(lcid,"calc复制失败提示", "Falha ao Copiar!!!");
            RegisterTranslationByLang(lcid,"calc粘贴成功提示", "Dados Colados");
            RegisterTranslationByLang(lcid,"calc粘贴失败提示", "Falha ao colar. Por favor, verifique o conteúdo da área de transferência. Todas as configurações foram redefinidas.");
            RegisterTranslationByLang(lcid,"calc设置成功提示", "Sucesso");


            RegisterTranslationByLang(lcid,"保存为默认配置标题", "Salvar como Padrão");
            RegisterTranslationByLang(lcid,"保存为默认配置说明", "Salva todas as configurações e produtos alvo da janela atual como a configuração padrão. Futuramente, todas as novas janelas (mesmo após reiniciar o jogo) carregarão esta configuração por defeito.\nManter Ctrl pressionado e clicar limpa a configuração padrão guardada, fazendo com que as janelas futuras voltem a abrir em branco.");

            RegisterTranslationByLang(lcid,"预先设置原矿标题", "Engadir Ore Raw");
            RegisterTranslationByLang(lcid,"预先设置原矿说明", "Clique para selecionar um item e trata-lo como mineral bruto.\nClique direito para restaurar todas as configurações de mineral bruto para a configuração predeterminada.");

            RegisterTranslationByLang(lcid,"还原配置成功", "A configuração do mineral bruto restaurada");
            RegisterTranslationByLang(lcid,"已设置原矿", "O mineral bruto foi colocado");
        }

        public static void LoadCustomTranslation()
        {
            //2052,简体中文,zhCN,zh,1033,1
            //1033,English,enUS,en,2052,0
            //1036,français,frFR,fr,1033,0,0
            //1031,Deutsch,deDE,de,1033,0,0
            //1041,日本語,jaJA,ja,1033,1,0
            //1042,한국어,koKO,ko,1033,1,0

            // 读取可以由玩家修改的翻译文件
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            string dir = Path.GetDirectoryName(executingAssembly.Location);
            string localeDir = Path.Combine(dir, "Locale");
            try
            {
                if (!Directory.Exists(localeDir))
                    Directory.CreateDirectory(localeDir);

                if (Directory.Exists(localeDir))
                {
                    string[] files = Directory.GetFiles(localeDir);
                    foreach (string file in files)
                    {
                        int LCID;
                        string[] fileNameArray = Path.GetFileName(file).Split('.');
                        if (int.TryParse(fileNameArray[0], out LCID) && fileNameArray.Length == 2)
                        {
                            string LCIDStr = LCID.ToString();
                            string text = File.ReadAllText(file);
                            string[] lines = text.Split('\n');

                            foreach (string line in lines)
                            {
                                string[] pair = line.Split('\t'); // 用两个tab分隔的key与translation，则应该在Split后读取[0]和[2]
                                if (pair.Length > 2)
                                {
                                    EditTranslationByLang(LCIDStr, pair[0].Replace("\\n","\n"), pair[2].Replace("\\n", "\n"));
                                }
                            }
                        }
                    }
                }

            }
            catch (Exception)
            {
                Utils.logger.LogError("Error when loading custom localization files.");
            }

            // 创建模板文件
            string templateFile = Path.Combine(localeDir, "1033.txt.template");
            if (!File.Exists(templateFile))
            {
                File.WriteAllText(templateFile, allENTranslations);
            }
        }

        // 记录mod加入的翻译并存入文件中
        public static void RegisterTranslation(string key, string enUS, string zhCN, string frFR)
        {
            LocalizationModule.RegisterTranslation(key, enUS, zhCN, frFR);
            allENTranslations += key.Replace("\n","\\n") + "\t\t" + enUS.Replace("\n", "\\n") + "\n";
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="abbr">abbr or (string)LCID</param>
        /// <param name="key"></param>
        /// <param name="translation"></param>
        public static void RegisterTranslationByLang(string abbr, string key, string translation)
        {
            LocalizationModule.RegisterTranslation(key, new Dictionary<string, string> { { abbr, translation } });
        }

        public static void EditTranslationByLang(string abbr, string key, string translation)
        {
            LocalizationModule.EditTranslation(key, new Dictionary<string, string> { { abbr, translation } });
        }

    }
}
