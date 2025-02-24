using BepInEx.Logging;
using DSPCalculator.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace DSPCalculator
{
    public static class Utils
    {
        public static ManualLogSource logger { get { return DSPCalculatorPlugin.logger; } }
        public static string KMG(double num)
        {
            string tail = "";
            if(num < 100)
            {
                return num.ToString("0.##");
            }
            else if (num < 1000)
            {
                return num.ToString("0.#");
            }
            else if (num < 10000)
            {
                return num.ToString("N0");
            }
            else if (num < 1000000)
            {
                tail = "k";
                num /= 1000;
            }
            else if (num < 1000000000L)
            {
                tail = "M";
                num /= 1000000;
            }
            else if (num < 1000000000000L)
            {
                tail = "G";
                num /= 1000000000;
            }
            else if (num < 1000000000000000L)
            {
                tail = "T";
                num /= 1000000000000L;
            }
            else if (num < 1000000000000000000L)
            {
                tail = "P";
                num /= 1000000000000000L;
            }
            else
            {
                tail = "E";
                num /= 1000000000000000000L;
            }

            // 不用0.##二用0.00是仅在有kMG的时候才必定保留小数位数。如果是小于10000的时候，小数部分为0的则不显示
            if(num < 9.995)
            {
                return string.Format("{0:0.000} {1}", num, tail); // 0,5:0.###这样也对不齐，这个字体空格比较窄
            }
            else if (num < 99.95)
            {
                return string.Format("{0:0.00} {1}", num, tail);
            }
            else if (num < 999.5)
            {
                return string.Format("{0:0.0} {1}", num, tail);
            }
            else
            {
                return string.Format("{0:N0} {1}", num, tail);
            }
        }

        public static string KMG(long num)
        {
            if (num <= 9999)
            {
                return num.ToString("N0");
            }
            else
            {
                return KMG((double)num);
            }
            
        }

        public static string KMGForceDigi(double num)
        {
            if (num < 10000)
            {
                return num.ToString("N2");
            }
            else
            {
                return KMG(num);
            }
        }

        public static double GetIncMilli(int index, UserPreference preference)
        {
            if (preference.customizeIncMilli && index > 0)
                return preference.incMilliOverride;
            else
                return Cargo.incTableMilli[index];
        }

        public static double GetAccMilli(int index, UserPreference preference)
        {
            if(preference.customizeAccMilli && index > 0)
                return preference.accMilliOverride;
            else
                return Cargo.accTableMilli[index];
        }

        public static double GetPowerRatio(int index, UserPreference preference)
        {
            return Cargo.powerTableRatio[index];
        }

        /// <summary>
        /// 专用于求解增产剂
        /// </summary>
        /// <param name="augMatrix"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool SolveEquationsLimited(double[,] augMatrix, out double[] result)
        {
            int r = augMatrix.GetLength(1);
            result = new double[r];
            int eCount = augMatrix.GetLength(0);
            for (int i = 0; i < eCount; i++)
            {

            }



            return true;
        }


        public static string GetItemName(this int itemId)
        {
            ItemProto itemProto = LDB.items.Select(itemId);
            if (itemProto != null)
                return itemProto.name;
            else
                return "null";
        }

        public static string GetRecipeName(this int recipeId)
        {

            RecipeProto recipeProto = LDB.recipes.Select(recipeId);
            if (recipeProto != null)
                return recipeProto.name;
            else
                return "null";
        }

        public static GameObject CreateEmptyGameObject(this GameObject parentObj, string name)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parentObj.transform, false);
            obj.transform.localScale = Vector3.one;
            obj.transform.localPosition = Vector3.zero;
            return obj;
        }

        public static GameObject CreateEmptyGameObject(this Transform parent, string name)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.localScale = Vector3.one;
            obj.transform.localPosition = Vector3.zero;
            return obj;
        }

        public static UIButton AddUIButtonForImage(this GameObject obj)
        {
            UIButton uibtn = obj.AddComponent<UIButton>();
            uibtn.tips = new UIButton.TipSettings();
            if(obj.GetComponent<Button>() != null)
            {
                uibtn.button = obj.GetComponent<Button>();
            }
            uibtn.transitions = new UIButton.Transition[1];
            uibtn.transitions[0] = CreateUIButtonTransition(obj.GetComponent<Image>());
            return uibtn;
        }

        public static UIButton.Transition CreateUIButtonTransition(Graphic target)
        {
            UIButton.Transition trans = new UIButton.Transition();
            trans.target = target;
            trans.damp = 0.3f;
            trans.highlightAlphaMultiplier = 1.0f;
            trans.highlightColorMultiplier = 1.0f;
            trans.highlightSizeMultiplier = 1.0f;
            trans.mouseoverSize = 1;
            trans.pressedSize = 1;
            return trans;
        }
    }
}
