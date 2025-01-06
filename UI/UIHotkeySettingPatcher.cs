using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace DSPCalculator.UI
{
    public class UIHotkeySettingPatcher
    {
        // modifier shift control alt 分别为 1 2 4 （0001 0010 0100）
        public static Text title1;
        public static Text keyText1;
        public static UIButton uibtn1;
        public static GameObject waitingTextObj1;
        public static InputField inputField1;

        public static Text title2;
        public static Text keyText2;
        public static UIButton uibtn2;
        public static GameObject waitingTextObj2;
        public static InputField inputField2;

        public static bool isWaiting1;
        public static bool isWaiting2;
        //public static bool ShiftDown;
        //public static bool CtrlDown;
        //public static bool AltDown;

        public static KeyCode temp1;
        public static int tempModifier1;
        public static KeyCode temp2;
        public static int tempModifier2;
        public static void Init()
        {
            GameObject oriSettingObj = GameObject.Find("UI Root/Overlay Canvas/Top Windows/Option Window/details/content-4/list/scroll-view/viewport/content/key-entry");
            GameObject oriParent = GameObject.Find("UI Root/Overlay Canvas/Top Windows/Option Window/details/content-4/list/scroll-view/viewport/content");


            float oriWidth = oriParent.GetComponent<RectTransform>().sizeDelta.x;
            float oriHeight = oriParent.GetComponent<RectTransform>().sizeDelta.y;
            oriParent.GetComponent<RectTransform>().sizeDelta = new Vector2(oriWidth, oriHeight + 84);

            GameObject openWindowHKSettingObj = GameObject.Instantiate(oriSettingObj, oriParent.transform);
            openWindowHKSettingObj.SetActive(true);
            GameObject.DestroyImmediate(openWindowHKSettingObj.GetComponent<UIKeyEntry>());
            openWindowHKSettingObj.transform.Find("clear-key-btn").gameObject.SetActive(false);

            openWindowHKSettingObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(30, -(oriHeight + 42) + 40);
            title1 = openWindowHKSettingObj.GetComponent<Text>();
            title1.text = "打开量化计算器窗口".Translate();
            keyText1 = openWindowHKSettingObj.transform.Find("key").GetComponent<Text>();
            uibtn1 = openWindowHKSettingObj.transform.Find("input/InputField").GetComponent<UIButton>();
            inputField1 = openWindowHKSettingObj.transform.Find("input/InputField").GetComponent<InputField>();
            waitingTextObj1 = openWindowHKSettingObj.transform.Find("input/waiting-text").gameObject;
            uibtn1.onClick += (x) => { OnSetKey1ButtonClick(); };

            openWindowHKSettingObj.transform.Find("set-default-btn").GetComponent<Button>().onClick.RemoveAllListeners();
            openWindowHKSettingObj.transform.Find("set-default-btn").GetComponent<Button>().onClick.AddListener(OnSetDefault1ButtonClick);


            GameObject openWindowHKSettingObj2 = GameObject.Instantiate(oriSettingObj, oriParent.transform);
            openWindowHKSettingObj2.SetActive(true);
            GameObject.DestroyImmediate(openWindowHKSettingObj2.GetComponent<UIKeyEntry>());
            openWindowHKSettingObj2.transform.Find("clear-key-btn").gameObject.SetActive(false);

            openWindowHKSettingObj2.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(30, -(oriHeight + 84) + 40);
            title2 = openWindowHKSettingObj2.GetComponent<Text>();
            title2.text = "切换计算器窗口大小".Translate();
            keyText2 = openWindowHKSettingObj2.transform.Find("key").GetComponent<Text>();
            uibtn2 = openWindowHKSettingObj2.transform.Find("input/InputField").GetComponent<UIButton>();
            inputField2 = openWindowHKSettingObj2.transform.Find("input/InputField").GetComponent<InputField>();
            waitingTextObj2 = openWindowHKSettingObj2.transform.Find("input/waiting-text").gameObject;
            uibtn2.onClick += (x) => { OnSetKey2ButtonClick(); };

            openWindowHKSettingObj2.transform.Find("set-default-btn").GetComponent<Button>().onClick.RemoveAllListeners();
            openWindowHKSettingObj2.transform.Find("set-default-btn").GetComponent<Button>().onClick.AddListener(OnSetDefault2ButtonClick);


            RefreshAll();
        }
        public static bool CheckModifier(int type, bool shift, bool ctrl, bool alt)
        {
            int modifier = DSPCalculatorPlugin.OpenWindowModifier.Value;
            if(type == 2)
                modifier = DSPCalculatorPlugin.SwitchWindowModifier.Value;

            if (modifier <= 0)
                return true;

            if((modifier & 1) > 0 && !shift)
                return false;
            if ((modifier & 2) > 0 && !ctrl)
                return false;
            if ((modifier & 4) > 0 && !alt)
                return false;

            return true;
        }

        public static void OnUpdate()
        {
            bool ShiftDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool CtrlDown  = Input.GetKey(KeyCode.LeftControl) || Input.GetKey (KeyCode.RightControl);
            bool AltDown = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            if(isWaiting1 || isWaiting2)
            {
                KeyCode key = KeyCode.Q;
                bool got = false;
                for (int i = 0; i < 26; i++)
                {
                    if(Input.GetKeyDown( (KeyCode)(97 + i)))
                    {
                        key = (KeyCode.A + i);
                        got = true;
                        break;
                    }
                }
                if (!got)
                {
                    for (int i = 0; i < 12; i++)
                    {
                        if (Input.GetKeyDown((KeyCode)(KeyCode.F1 + i)))
                        {
                            key = (KeyCode.F1 + i);
                            got = true;
                            break;
                        }
                    }
                }
                if (!got)
                {
                    for (int i = 0; i < 10; i++)
                    {
                        if (Input.GetKeyDown((KeyCode)((int)KeyCode.Keypad0 + i)))
                        {
                            key = (KeyCode.Keypad0 + i);
                            got = true;
                            break;
                        }
                    }
                }
                if (got && key >= KeyCode.A && key <= KeyCode.Z || key >= KeyCode.F1 && key <= KeyCode.F12 || key >= KeyCode.Keypad0 && key <= KeyCode.Keypad9)
                {
                    if (isWaiting1)
                        SetOpenWindowHotKey(key, ShiftDown, CtrlDown, AltDown);
                    else if (isWaiting2)
                        SetSwitchWindowHotKey(key, ShiftDown, CtrlDown, AltDown);

                    isWaiting1 = false;
                    isWaiting2 = false;

                    RefreshAll();
                }
                
            }
        }

        public static void RefreshAll()
        {
            if (isWaiting1)
            {
                uibtn1.highlighted = true;
                waitingTextObj1.SetActive(true);
            }
            else
            {
                uibtn1.highlighted = false;
                waitingTextObj1.SetActive(false);
            }
            if (isWaiting2)
            {
                uibtn2.highlighted = true;
                waitingTextObj2.SetActive(true);
            }
            else
            {
                uibtn2.highlighted = false;
                waitingTextObj2.SetActive(false);
            }

            int modifier1 = tempModifier1;
            string txt1 = "";
            if((modifier1 & 1) > 0)
            {
                txt1 += "Shift";
            }
            if ((modifier1 & 2) > 0)
            {
                if (txt1.Length > 0)
                    txt1 += " + ";
                txt1 += "Ctrl";
            }
            if ((modifier1 & 4) > 0)
            {
                if (txt1.Length > 0)
                    txt1 += " + ";
                txt1 += "Alt";
            }
            if (txt1.Length > 0)
                txt1 += " + ";
            txt1 += temp1.ToString();
            keyText1.text = txt1;


            int modifier2 = tempModifier2;
            string txt2 = "";

            if ((modifier2 & 1) > 0)
            {
                txt2 += "Shift";
            }
            if ((modifier2 & 2) > 0)
            {
                if (txt2.Length > 0)
                    txt2 += " + ";
                txt2 += "Ctrl";
            }
            if ((modifier2 & 4) > 0)
            {
                if (txt2.Length > 0)
                    txt2 += " + ";
                txt2 += "Alt";
            }
            if (txt2.Length > 0)
                txt2 += " + ";
            txt2 += temp2.ToString();
            keyText2.text = txt2;
        }

        public static void OnSetKey1ButtonClick()
        {
            isWaiting1 = !isWaiting1;
            isWaiting2 = false;
            RefreshAll();
        }

        public static void OnSetKey2ButtonClick()
        {
            isWaiting2 = !isWaiting2;
            isWaiting1 = false;
            RefreshAll();
        }


        public static void OnSetDefault1ButtonClick()
        {
            isWaiting1 = false;
            isWaiting2 = false;
            SetOpenWindowHotKey(KeyCode.Q, false, false, false);
            RefreshAll();
        }

        public static void OnSetDefault2ButtonClick()
        {
            isWaiting1 = false;
            isWaiting2 = false;
            SetSwitchWindowHotKey(KeyCode.Tab, false, false, false);
            RefreshAll();
        }

        public static void SetOpenWindowHotKey(KeyCode key, bool shift, bool ctrl, bool alt)
        {
            temp1 = key;
            int modifier = 0;
            if (shift) modifier += 1;
            if (ctrl) modifier += 2;
            if (alt) modifier += 4;
            tempModifier1 = modifier;
        }

        public static void SetSwitchWindowHotKey(KeyCode key, bool shift, bool ctrl, bool alt)
        {
            temp2 = key;
            int modifier = 0;
            if (shift) modifier += 1;
            if (ctrl) modifier += 2;
            if (alt) modifier += 4;
            tempModifier2 = modifier;
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIOptionWindow), methodName: "OnApplyClick")]
        public static void Confirm()
        {
            DSPCalculatorPlugin.OpenWindowHotKey.Value = temp1;
            DSPCalculatorPlugin.SwitchWindowSizeHotKey.Value = temp2;
            DSPCalculatorPlugin.OpenWindowModifier.Value = tempModifier1;
            DSPCalculatorPlugin.SwitchWindowModifier.Value = tempModifier2;
            DSPCalculatorPlugin.OpenWindowHotKey.ConfigFile.Save();
            RefreshAll();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIOptionWindow), methodName: "OnCancelClick")]
        public static void Cancel()
        {
            temp1 = DSPCalculatorPlugin.OpenWindowHotKey.Value;
            temp2 = DSPCalculatorPlugin.SwitchWindowSizeHotKey.Value;
            tempModifier1 = DSPCalculatorPlugin.OpenWindowModifier.Value;
            tempModifier2 = DSPCalculatorPlugin.SwitchWindowModifier.Value;
            RefreshAll();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIOptionWindow), methodName: "_OnOpen")]
        public static void OnOpen()
        {
            temp1 = DSPCalculatorPlugin.OpenWindowHotKey.Value;
            temp2 = DSPCalculatorPlugin.SwitchWindowSizeHotKey.Value;
            tempModifier1 = DSPCalculatorPlugin.OpenWindowModifier.Value;
            tempModifier2 = DSPCalculatorPlugin.SwitchWindowModifier.Value;
            RefreshAll();
        }

        public static string GetFoldHotkeyString()
        {
            string result = "";
            int modifier = DSPCalculatorPlugin.SwitchWindowModifier.Value;
            if ((modifier & 1) > 0)
                result += "Shift";
            if ((modifier & 2) > 0)
            {
                if (result.Length > 0)
                    result += " + ";
                result += "Ctrl";
            }
            if ((modifier & 4) > 0)
            {
                if (result.Length > 0)
                    result += " + ";
                result += "Alt";
            }
            if (result.Length > 0)
                result += " + ";
            result += DSPCalculatorPlugin.SwitchWindowSizeHotKey.Value;
            return " (" + result + ")";
        }
    }
}
