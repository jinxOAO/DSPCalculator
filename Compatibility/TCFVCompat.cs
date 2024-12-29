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
    public class TCFVC : BaseUnityPlugin
    {
        public const string DEPENDENCY_GUID = "com.ckcz123.DSP_Battle";

        public const string NAME = "DSPCalcTCFVCompat";
        public const string GUID = "com.GniMaerd.DSPCalcTCFVCompat";
        public const string VERSION = "0.1.0";
        void Awake()
        {
            CompatManager.TCFV = true;
        }
    }
}
