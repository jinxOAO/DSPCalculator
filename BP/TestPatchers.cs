using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSPCalculator.BP
{
    public class TestPatchers
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameData), "NewGame")]
        public static void TestLog()
        {

        }
    }
}
