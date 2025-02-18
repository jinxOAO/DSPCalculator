using HarmonyLib;
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

        //public static GameObject calcWindowGroupObj;
        public static bool hasOpenedWindow;


        public static Transform inGameWindows;

        public static bool temporaryCloseBecausePasteBp = false; // 由于粘贴蓝图，而暂时关闭了计算器窗口，会在粘贴完成后（退出蓝图粘贴窗口时）自动打开

        /// <summary>
        /// mod加载时初始化
        /// </summary>
        public static void OnStart()
        {
            windows = new List<UICalcWindow>();

            // 创建所有window的父级obj
            //GameObject parentWindowObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows");
            //calcWindowGroupObj = new GameObject();
            //calcWindowGroupObj.name = "Calc Window Group";
            //calcWindowGroupObj.transform.SetParent(parentWindowObj.transform, false);
            //calcWindowGroupObj.transform.localScale = Vector3.one;
            //calcWindowGroupObj.transform.localPosition = Vector3.zero;
            hasOpenedWindow = false;
            inGameWindows = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows").transform;
        }

        public static void OnUpdate()
        {
            if(inGameWindows == null)
                inGameWindows = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows").transform;

            int childCount = inGameWindows.childCount;
            for (int i = 1; i <childCount; i++)
            {
                GameObject childObj = inGameWindows.GetChild(childCount - 1).gameObject;
                if (!childObj.activeSelf)
                    childObj.transform.SetAsFirstSibling(); // 如果尾巴不是激活状态的，给他丢到第一位置，防止干扰UICalcWindow判断自己是不是topAndActive
                else
                    break; // 尾巴是active的，就break;

                // i的存在保证了循环最多=childCount - 1次
            }

            hasOpenedWindow = false;
            if (windows != null)
            {
                for (int i = 0; i < windows.Count; i++)
                {
                    windows[i].OnUpdate();
                    hasOpenedWindow = hasOpenedWindow || windows[i].windowObj.activeSelf;
                }
            }

            bool ShiftDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool CtrlDown = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool AltDown = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            if (!VFInput.inputing)
            {
                if (Input.GetKeyDown(DSPCalculatorPlugin.OpenWindowHotKey.Value) && UIHotkeySettingPatcher.CheckModifier(1, ShiftDown, CtrlDown, AltDown))
                {
                    if (!DSPCalculatorPlugin.SingleWindow.Value || (DSPCalculatorPlugin.SingleWindow.Value && ShiftDown && CtrlDown && AltDown)) // CtrlShiftAlt都按下时，且是单窗口模式，则无视单窗口模式，打开新窗口
                    {
                        OpenOne();
                    }
                    else // 单窗口模式下，开启窗口的快捷键在已经有窗口的情况下反而会被用于关闭窗口
                    {
                        bool hasOpenedWindow = false;
                        // 如果有开启的窗口，关闭所有窗口
                        if (windows != null && windows.Count > 0)
                        {
                            for (int i = windows.Count - 1; i >= 0; i--)
                            {
                                if(i >= windows.Count)
                                {
                                    continue;
                                }
                                else if (windows[i].windowObj.activeSelf)
                                {
                                    hasOpenedWindow = true;
                                    windows[i].CloseWindow();
                                }
                            }
                        }

                        // 如果没有开启的窗口，打开一个窗口
                        if (!hasOpenedWindow)
                            OpenOne();
                    }
                }
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

        public static UICalcWindow OpenOne(bool forceNewWindow = false)
        {
            UIPauseBarPatcher.Init();
            temporaryCloseBecausePasteBp = false;

            if (lastClosedWindow != null && !forceNewWindow)
            {
                lastClosedWindow.OpenWindow();
                return lastClosedWindow;
            }
            else
            {
                UICalcWindow window = new UICalcWindow(windows.Count);
                windows.Add(window);
                return window;
            }
        }

        public static UICalcWindow OpenOne(bool forceNewWindow, float offsetX, float offsetY)
        {
            UIPauseBarPatcher.Init();
            temporaryCloseBecausePasteBp = false;

            if (lastClosedWindow != null && !forceNewWindow)
            {
                lastClosedWindow.OpenWindow();
                Vector3 oriPos = lastClosedWindow.windowObj.transform.localPosition;
                lastClosedWindow.windowObj.transform.localPosition = new Vector3(oriPos.x + offsetX, oriPos.y + offsetY, oriPos.z);
                return lastClosedWindow;
            }
            else
            {
                UICalcWindow window = new UICalcWindow(windows.Count);
                windows.Add(window);
                Vector3 oriPos = window.windowObj.transform.localPosition;
                window.windowObj.transform.localPosition = new Vector3(oriPos.x + offsetX, oriPos.y + offsetY, oriPos.z);
                return window;
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

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIBlueprintInspector), "_OnClose")]
        public static void OnQuitBpPasteMode()
        {
            if(temporaryCloseBecausePasteBp)
            {
                OpenOne();
                temporaryCloseBecausePasteBp = false;
            }
        }
    }
}
