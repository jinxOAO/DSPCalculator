using BepInEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSPCalculator.Compatibility
{
    [BepInDependency(DEPENDENCY_GUID)]
    [BepInPlugin(GUID, NAME, VERSION)]
    public class MMSCompat : BaseUnityPlugin
    {
        public const string DEPENDENCY_GUID = "Gnimaerd.DSP.plugin.MoreMegaStructure";

        public const string NAME = "DSPCalcMMSCompat";
        public const string GUID = "com.GniMaerd.DSPCalcMMSCompat";
        public const string VERSION = "0.1.0";
        void Awake()
        {
            CompatManager.MMS = true;
        }
    }
}
