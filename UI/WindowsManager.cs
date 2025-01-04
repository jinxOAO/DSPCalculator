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
        public static bool hasOpenedWindow;

        public static bool ShiftDown;
        public static bool CtrlDown;
        public static bool AltDown;


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
            hasOpenedWindow = false;
        }

        public static void OnUpdate()
        {
            hasOpenedWindow = false;
            if (windows != null)
            {
                for (int i = 0; i < windows.Count; i++)
                {
                    windows[i].OnUpdate();
                    hasOpenedWindow = hasOpenedWindow || windows[i].windowObj.activeSelf;
                }
            }

            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
                ShiftDown = true;
            if (Input.GetKeyUp(KeyCode.LeftShift) || Input.GetKeyUp(KeyCode.RightShift))
                ShiftDown = false;
            if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl))
                CtrlDown = true;
            if (Input.GetKeyUp(KeyCode.LeftControl) || Input.GetKeyUp(KeyCode.RightControl))
                CtrlDown = false;
            if (Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.RightAlt))
                AltDown = true;
            if (Input.GetKeyUp(KeyCode.LeftAlt) || Input.GetKeyUp(KeyCode.RightAlt))
                AltDown = false;

            if (Input.GetKeyDown(DSPCalculatorPlugin.OpenWindowHotKey.Value) && UIHotkeySettingPatcher.CheckModifier(1, ShiftDown, CtrlDown, AltDown))
            {
                OpenOne();
            }

            if (UIPauseBarPatcher.pauseBarObj != null)
            {
                if (hasOpenedWindow)
                    UIPauseBarPatcher.pauseBarObj.SetActive(true);
                else
                {
                    UIPauseBarPatcher.pauseBarObj.SetActive(false);
                }
            }
            //if (Input.GetKeyDown(KeyCode.L))
            //{
            //    OpenOne();
            //}
        }

        public static void HideAll()
        {

        }

        public static void OpenOne()
        {
            UIPauseBarPatcher.Init();

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
            if (windows != null)
            {
                for (int i = 0; i < windows.Count; i++)
                {
                    if (windows[i].isTopAndActive)
                    {
                        windows[i].CloseWindow();
                        return;
                    }
                }
            }
        }
    }
}
