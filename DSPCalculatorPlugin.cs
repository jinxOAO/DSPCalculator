using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using CommonAPI;
using CommonAPI.Systems;
using CommonAPI.Systems.ModLocalization;
using DSPCalculator.Logic;
using DSPCalculator.UI;
using HarmonyLib;

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


        public void Awake()
        {
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
    }
}
