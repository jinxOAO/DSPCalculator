using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DSPCalculator.UI
{
    public class WindowsManager
    {
        // 存储所有窗口实例
        public static List<UICalcWindow> windows;
        public static UICalcWindow lastClosedWindow;

        public static GameObject calcWindowGroupObj;

        /// <summary>
        /// mod加载时初始化
        /// </summary>
        public static void OnStart()
        {
            windows = new List<UICalcWindow>();

            // 创建所有window的父级obj
            GameObject parentWindowObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows");
            calcWindowGroupObj = new GameObject();
            calcWindowGroupObj.name = "Calc Window Group";
            calcWindowGroupObj.transform.SetParent(parentWindowObj.transform, false);
            calcWindowGroupObj.transform.localScale = Vector3.one;
            calcWindowGroupObj.transform.localPosition = Vector3.zero;
        }

        public static void OnUpdate()
        {
            if (windows != null)
            {
                for (int i = 0; i < windows.Count; i++)
                {
                    windows[i].OnUpdate();
                }
            }

            if (Input.GetKeyDown(KeyCode.F1))
            {
                OpenOne();
            }
            if (Input.GetKeyDown(KeyCode.L))
            {
                OpenOne();
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {

            }
        }

        public static void HideAll()
        {

        }

        public static void OpenOne()
        {
            if(lastClosedWindow != null)
            {
                lastClosedWindow.OpenWindow();
            }
            else
            {
                windows.Add(new UICalcWindow(windows.Count));
            }
        }

        public static void CloseTopWindow()
        {

        }
    }
}
