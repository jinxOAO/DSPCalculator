using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using CommonAPI;
using CommonAPI.Systems;
using CommonAPI.Systems.ModLocalization;
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

        public void Awake()
        {

        }

        public void Start()
        {

            WindowsManager.OnStart();
        }

        public void Update()
        {
            WindowsManager.OnUpdate();
        }
    }
}
