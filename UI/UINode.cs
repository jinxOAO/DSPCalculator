using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DSPCalculator.UI
{
    public class UINode
    {
        public GameObject obj;
        public UICalcWindow parentCalcWindow;

        public virtual void OnUpdate(bool isMoving)
        {

        }
    }
}
