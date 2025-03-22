using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using CommonAPI;
using CommonAPI.Systems;
using CommonAPI.Systems.ModLocalization;
using DSPCalculator.Bp;
using DSPCalculator.Logic;
using DSPCalculator.UI;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DSPCalculator
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency(CommonAPIPlugin.GUID)]
    [CommonAPISubmoduleDependency(nameof(ProtoRegistry))]
    [CommonAPISubmoduleDependency(nameof(TabSystem))]
    [CommonAPISubmoduleDependency(nameof(LocalizationModule))]
    public class DSPCalculatorPlugin: BaseUnityPlugin
    {
        public const string NAME = "DSPCalculator";
        public const string GUID = "com.GniMaerd.DSPCalculator";
        public const string VERSION = "0.5.1";

        // ---------------------------------------------------------------------------
        public static bool developerMode = false; //           发布前修改             |
        // ---------------------------------------------------------------------------

        public static ConfigEntry<KeyCode> OpenWindowHotKey;
        public static ConfigEntry<KeyCode> SwitchWindowSizeHotKey;
        public static ConfigEntry<int> OpenWindowModifier;
        public static ConfigEntry<int> SwitchWindowModifier;
        public static ConfigEntry<bool> EditableTitle;
        public static ConfigEntry<bool> RoundUpAssemblerNum;
        public static ConfigEntry<bool> SingleWindow;
        public static ConfigEntry<int> MaxWindowsReservedAfterClose;
        public static ConfigEntry<bool> ClickToSwitchRecipeMode;
        public static ConfigEntry<bool> OnlyCountUnfinishedFacilities;

        //public static ConfigEntry<bool> assemblerNumberKMG; // 生产设施是否用使用最大千分位符号
        //public static ConfigEntry<int> assemblerNumberDecimalPlaces; // 生产设施数量显示的小数位数，-1表示默认3位有效数字，正数表示恒定保留x位小数
        //public static ConfigEntry<bool> resourceNumberKMG; // 原材料、产物是否用使用最大千分位符号
        //public static ConfigEntry<int> resourceNumberDecimalPlaces; // 原材料、产物数量显示的小数位数，-1表示默认3位有效数字，正数表示恒定保留x位小数

        public static ManualLogSource logger;
        public static bool showMixBeltCheckbox = false;

        public void Awake()
        {
            logger = Logger;
            OpenWindowHotKey = Config.Bind<KeyCode>("config", "OpenWindowHotKey", KeyCode.Q, "打开计算器窗口的快捷键。HotKey to open calculator window.");
            SwitchWindowSizeHotKey = Config.Bind<KeyCode>("config", "SwitchWindowSizeHotKey", KeyCode.Tab, "将计算器窗口展开或缩小的快捷键。HotKey to fold or unfold calculator window.");
            OpenWindowModifier = Config.Bind<int>("config", "OpenWindowHKModifier", 0, "byte shift = 1, ctrl = 2, alt = 4");
            SwitchWindowModifier = Config.Bind<int>("config", "SwitchWindowHKModifier", 0, "byte shift = 1, ctrl = 2, alt = 4");
            EditableTitle = Config.Bind<bool>("config", "EditTitle", false, "将此项置为true可以使计算器的窗口标题可以被编辑。Set this to true will allow you to edit the calculator window's title in game.");
            RoundUpAssemblerNum = Config.Bind<bool>("config", "RoundUpAssemblerNum", true, "生产设施数量显示是否自动向上取整。Is the display of the number of production facilities automatically rounded up.");
            SingleWindow = Config.Bind<bool>("config", "SingleWindow", false, "单窗口模式启用时，打开和关闭窗口都将使用打开窗口的那个快捷键。这会使你无法开启多个窗口，除非你同时按住Ctrl+Shift+Alt。  When single window mode is enabled, both opening and closing calculator window will use the same shortcut key (i.e. the open the window hot key). But if you want to open multiple windows in this mode, you must hold down Ctrl+Shift+Alt at the same time.");
            MaxWindowsReservedAfterClose = Config.Bind<int>("config", "MaxWindowsReservedAfterClose", 5, "最后被关闭的几个窗口可以仍在后台保留其计算结果和玩家设置，而不被销毁或重置。若设置为0，则所有关闭的窗口都不会被销毁（不推荐）。The last few closed windows can still retain their calculation results and player settings in the background without being destroyed or reset. If set to 0, all closed windows will not be destroyed (not recommended).");
            ClickToSwitchRecipeMode = Config.Bind<bool>("config", "ClickToSwitchRecipeMode", false, "切换配方选取的模式会使用“点击循环切换”而非“点击然后选取配方”的模式。The mode for switching recipe selection will use the \"click loop switch\" instead of the \"click and select recipe\" mode.");

            OnlyCountUnfinishedFacilities = Config.Bind<bool>("config", "OnlyCountUnfinishedFacilities", false, "如果设置为true，一旦你将某个物品产出勾选为已完成，其需求的生产设施将不再被计入右侧面板的生产设施需求总数。If set to true, once you check an item as finished, its required production facilities will no longer be included in the total production facility demand on the right panel.");

            Harmony.CreateAndPatchAll(typeof(DSPCalculatorPlugin));
            Harmony.CreateAndPatchAll(typeof(RecipePickerPatcher));
            Harmony.CreateAndPatchAll(typeof(UIHotkeySettingPatcher));
            Harmony.CreateAndPatchAll(typeof(WindowsManager));
            if (TestPatchers.enabled)
            {
                Harmony.CreateAndPatchAll(typeof(TestPatchers));
            }
            Localizations.AddLocalizations();
        }

        public void Start()
        {
            UIHotkeySettingPatcher.Init();
            WindowsManager.OnStart();
        }

        public void Update()
        {
            UIHotkeySettingPatcher.OnUpdate();
            WindowsManager.OnUpdate();
            UIPauseBarPatcher.OnUpdate();
            if(TestPatchers.enabled && Input.GetKeyDown(KeyCode.K))
            {
                TestPatchers.TestCreateExample();
            }
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameData), "NewGame")]
        [HarmonyPatch(typeof(GameData), "Import")]
        public static void OnLoadGame()
        {
            WindowsManager.InitUIResolution();
            CalcDB.TryInit();
            BpDB.Init();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameMain), "LateUpdate")]
        public static bool EscLogicBlocker()
        {
            if(WindowsManager.hasOpenedWindow)
            {
                bool flag = !VFInput._godModeMechaMove;
                bool flag2 = VFInput.escKey.onDown || VFInput.escape || VFInput.delayedEscape; // 没有捕获 VFInput.rtsCancel.onDown
                if (flag && flag2)
                {
                    WindowsManager.CloseTopWindow();
                    return true;
                }
            }
            return true;
        }

        
    }
}
