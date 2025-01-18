using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Configuration;
using CommonAPI;
using CommonAPI.Systems;
using CommonAPI.Systems.ModLocalization;
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
        public const string VERSION = "0.1.12";

        // ---------------------------------------------------------------------------
        public static bool developerMode = false; //           发布前修改             |
        // ---------------------------------------------------------------------------

        public static ConfigEntry<KeyCode> OpenWindowHotKey;
        public static ConfigEntry<KeyCode> SwitchWindowSizeHotKey;
        public static ConfigEntry<int> OpenWindowModifier;
        public static ConfigEntry<int> SwitchWindowModifier;
        public static ConfigEntry<bool> EditableTitle;
        public static ConfigEntry<bool> RoundUpAssemblerNum;

        //public static ConfigEntry<bool> assemblerNumberKMG; // 生产设施是否用使用最大千分位符号
        //public static ConfigEntry<int> assemblerNumberDecimalPlaces; // 生产设施数量显示的小数位数，-1表示默认3位有效数字，正数表示恒定保留x位小数
        //public static ConfigEntry<bool> resourceNumberKMG; // 原材料、产物是否用使用最大千分位符号
        //public static ConfigEntry<int> resourceNumberDecimalPlaces; // 原材料、产物数量显示的小数位数，-1表示默认3位有效数字，正数表示恒定保留x位小数


        public static bool showMixBeltCheckbox = false;

        public void Awake()
        {
            OpenWindowHotKey = Config.Bind<KeyCode>("config", "OpenWindowHotKey", KeyCode.Q, "打开计算器窗口的快捷键。HotKey to open calculator window.");
            SwitchWindowSizeHotKey = Config.Bind<KeyCode>("config", "SwitchWindowSizeHotKey", KeyCode.Tab, "将计算器窗口展开或缩小的快捷键。HotKey to fold or unfold calculator window.");
            OpenWindowModifier = Config.Bind<int>("config", "OpenWindowHKModifier", 0, "byte shift = 1, ctrl = 2, alt = 4");
            SwitchWindowModifier = Config.Bind<int>("config", "SwitchWindowHKModifier", 0, "byte shift = 1, ctrl = 2, alt = 4");
            EditableTitle = Config.Bind<bool>("config", "EditTitle", false, "将此项置为true可以使计算器的窗口标题可以被编辑。Set the to true will allow you to edit the calculator window's title in game.");
            RoundUpAssemblerNum = Config.Bind<bool>("config", "RoundUpAssemblerNum", true, "生产设施数量显示是否自动向上取整。Is the display of the number of production facilities automatically rounded up.");

            Harmony.CreateAndPatchAll(typeof(DSPCalculatorPlugin));
            Harmony.CreateAndPatchAll(typeof(RecipePickerPatcher));
            Harmony.CreateAndPatchAll(typeof(UIHotkeySettingPatcher));
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
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameData), "NewGame")]
        [HarmonyPatch(typeof(GameData), "Import")]
        public static void OnLoadGame()
        {
            CalcDB.TryInit();
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
                    VFInput.UseEscape();
                    WindowsManager.CloseTopWindow();
                    return true;
                }
            }
            return true;
        }

        
    }
}
