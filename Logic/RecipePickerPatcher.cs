using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DSPCalculator.Logic
{
    public class RecipePickerPatcher
    {
        /// <summary>
        /// 通过将filter设置为负数，用来表示只显示某些物品的配方
        /// </summary>
        /// <param name="__instance"></param>
        /// <returns></returns>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIRecipePicker), "RefreshIcons")]
        public static bool RecipePickerPrefix(ref UIRecipePicker __instance)
        {
            if((int)__instance.filter >= 0)
                return true;

            int itemId = -(int)__instance.filter; 
            Array.Clear(__instance.indexArray, 0, __instance.indexArray.Length);
            Array.Clear(__instance.protoArray, 0, __instance.protoArray.Length);
            IconSet iconSet = GameMain.iconSet;
            List<NormalizedRecipe> recipes = CalcDB.itemDict[itemId].recipes;
            for (int i = 0; i < recipes.Count; i++)
            {
                RecipeProto recipeProto = recipes[i].oriProto;
                if (recipeProto.GridIndex >= 1101)
                {
                    int num = recipeProto.GridIndex / 1000;
                    int num2 = (recipeProto.GridIndex - num * 1000) / 100 - 1;
                    int num3 = recipeProto.GridIndex % 100 - 1;
                    if (num2 >= 0 && num3 >= 0 && num2 < 8 && num3 < 14)
                    {
                        int num4 = num2 * 14 + num3;
                        if (num4 >= 0 && num4 < __instance.indexArray.Length && num == __instance.currentType)
                        {
                            __instance.indexArray[num4] = iconSet.recipeIconIndex[recipeProto.ID];
                            __instance.protoArray[num4] = recipeProto;
                        }
                    }
                }
            }
            return false;
        }

    }
}
