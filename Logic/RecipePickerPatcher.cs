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
            var filter = AccessTools.Field(typeof(UIRecipePicker), "filter")?.GetValue(__instance);
            if(filter == null || (int)filter >= 0)
                return true;

            int itemId = -(int)filter; 
            var indexArray = (int[])AccessTools.Field(typeof(UIRecipePicker), "indexArray")?.GetValue(__instance);
            var protoArray = (RecipeProto[])AccessTools.Field(typeof(UIRecipePicker), "protoArray")?.GetValue(__instance);
            var currentType = (int)AccessTools.Field(typeof(UIRecipePicker), "currentType")?.GetValue(__instance);
            
            if(indexArray == null || protoArray == null)
                return true;
                
            Array.Clear(indexArray, 0, indexArray.Length);
            Array.Clear(protoArray, 0, protoArray.Length);
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
                        if (num4 >= 0 && num4 < indexArray.Length && num == currentType)
                        {
                            indexArray[num4] = (int)iconSet.recipeIconIndex[recipeProto.ID];
                            protoArray[num4] = recipeProto;
                        }
                    }
                }
            }
            return false;
        }

    }
}
