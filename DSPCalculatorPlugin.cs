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
        public const string VERSION = "0.1.0";

        // ---------------------------------------------------------------------------
        public static bool developerMode = true; //           发布前修改             |
        // ---------------------------------------------------------------------------

        public static ConfigEntry<KeyCode> OpenWindowHotKey;
        public static ConfigEntry<KeyCode> SwitchWindowSizeHotKey;

        public void Awake()
        {
            OpenWindowHotKey = Config.Bind<KeyCode>("config", "OpenWindowHotKey", KeyCode.F1, "打开计算器窗口的快捷键。HotKey to open calculator window.");
            SwitchWindowSizeHotKey = Config.Bind<KeyCode>("config", "SwitchWindowSizeHotKey", KeyCode.F2, "将计算器窗口展开或缩小的快捷键。HotKey to fold or unfold calculator window.");

            Harmony.CreateAndPatchAll(typeof(DSPCalculatorPlugin));
            Harmony.CreateAndPatchAll(typeof(RecipePickerPatcher));
            Localizations.AddLocalizations();
        }

        public void Start()
        {

            WindowsManager.OnStart();
        }

        public void Update()
        {
            WindowsManager.OnUpdate();
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
                bool flag2 = VFInput.rtsCancel.onDown || VFInput.escKey.onDown || VFInput.escape || VFInput.delayedEscape;
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
