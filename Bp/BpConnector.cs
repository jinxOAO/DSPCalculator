using DSPCalculator.Bp;
using DSPCalculator.Logic;
using DSPCalculator.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSPCalculator.Bp
{
    public class BpConnector
    {
        public static bool enabled = false;

        public UICalcWindow calcWindow;
        public SolutionTree solution; 
        public BlueprintData blueprintData;
        public List<BlueprintBuilding> buildings;
         // 因为某些点的建筑或者传送带有高度重叠所以不能占用

        public BpConnector(UICalcWindow calcWindow) 
        {
            this.calcWindow = calcWindow;
            blueprintData = BpBuilder.CreateEmpty();
        }
    }

    public class BpBlockLineInfo
    {
        public int height;
        public int occupiedWidth;
        public List<BpProcessor> processors; // 在此行内的蓝图们
    }
}
