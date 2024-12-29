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
    public class GBCompat : BaseUnityPlugin
    {
        public const string DEPENDENCY_GUID = "org.LoShin.GenesisBook";

        public const string NAME = "DSPCalcGBCompat";
        public const string GUID = "com.GniMaerd.DSPCalcGBCompat";
        public const string VERSION = "0.1.0";
        void Awake()
        {
            CompatManager.GB = true;
        }
    }
}
