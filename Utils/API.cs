using DSPCalculator.Logic;
using DSPCalculator.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSPCalculator
{
    /// <summary>
    /// Provide API to create Calculator Windows or other things.
    /// </summary>
    public static class API
    {
        /// <summary>
        /// Create a calculator window, then calculate the production line acrroding to the "demands". You must confirm all itemIds are legal.
        /// </summary>
        /// <param name="demands">A Demand List, with all the target items you want to calculate.</param>
        /// <param name="useDefaultSettings">Whether use the defaut user preferences (such as priority recipes or production facility).</param>
        public static UICalcWindow OpenNewWindowWithDemands(List<Demand> demands, bool useDefaultSettings = true)
        {
            UICalcWindow window = WindowsManager.OpenOne();
            window.solution.targets.Clear();
            if (useDefaultSettings)
                window.solution.ClearUserPreference();
            
            int index = 0;
            if(demands != null)
            {
                foreach (Demand demand in demands)
                {
                    if(demand.itemId != 0)
                    {
                        window.AddOrUpdateTargetButNotResolve(index, demand.itemId, demand.speedPerMinute);
                        index++;
                    }
                }
            }
            window.nextFrameRecalc = true;
            return window;
        }

        /// <summary>
        /// Create a calculator window, then calculate the target production line. You must confirm the itemId is legal.
        /// </summary>
        /// <param name="itemId">The target item Id.</param>
        /// <param name="speedPerMinute">The target production speed (per minute).</param>
        /// <param name="useDefaultSettings">Whether use the defaut user preferences (such as priority recipes or production facility).</param>
        public static UICalcWindow OpenNewWindowWithDemand(int itemId, double speedPerMinute, bool useDefaultSettings = true)
        {
            UICalcWindow window = WindowsManager.OpenOne();
            window.solution.targets.Clear();
            if (useDefaultSettings)
                window.solution.ClearUserPreference();
            window.AddOrUpdateTargetThenResolve(0, itemId, speedPerMinute);
            window.nextFrameRecalc = true;
            return window;
        }

        /// <summary>
        /// Open the last closed window. Won't run any calculation or refreshing.
        /// </summary>
        /// <returns></returns>
        public static UICalcWindow OpenOneWindow()
        {
            return WindowsManager.OpenOne();
        }

        /// <summary>
        /// Calculation the production line and refresh the UI.
        /// </summary>
        /// <param name="window"></param>
        public static void CalculateAndRefresh(UICalcWindow window)
        {
            if (window == null) return;
            window.nextFrameRecalc = true;
        }
    }

    public struct Demand
    {
        public int itemId;
        public double speedPerMinute;

        public Demand(int itemId, double speedPerMinute)
        {
            this.itemId = itemId;
            this.speedPerMinute = speedPerMinute;
        }
    }
}
