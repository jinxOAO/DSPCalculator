﻿using DSPCalculator.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine;

namespace DSPCalculator.UI
{
    public class UIItemNodeSimple
    {
        // 一些公共资源
        public static Sprite backgroundSprite = null;
        public static Sprite buttonBackgroundSprite = null;
        public static Sprite gearSprite = null;
        public static Sprite filterSprite = null;
        public static Sprite biaxialArrowSprite = null;
        public static Sprite oreSprite = null;
        public static Sprite crossSprite = null;
        public static Sprite bannedSprite = null;
        public static Sprite todoListSprite = null;
        public static Color backgroundImageColor = new Color(0f, 0.811f, 1f, 0.072f);
        public static Color iconButtonHighLightColor = new Color(0.737f, 0.802f, 1f, 0.362f);
        public static Color incModeImageColor = new Color(0.287f, 0.824f, 1, 0.266f);
        public static Color accModeImageColor = new Color(0.9906f, 0.5897f, 0.3691f, 0.384f);
        public static Color incModeTextColor = new Color(0.282f, 0.845f, 1, 0.705f);
        public static Color accModeTextColor = new Color(0.9906f, 0.5897f, 0.3691f, 0.705f);
        public static int buttonCountPerRow = 3; // 一行几个带图标的按钮
        public static Vector3 recipeGroupLocalPosition = new Vector3(0, 20, 0);

        // 对象资源
        public GameObject obj;

        public int ID;
        public UICalcWindow parentCalcWindow;
        public ItemNode itemNode;
        public RecipeInfo mainRecipeInfo;

        public Text outputText;
        public GameObject overflowNoteTextObj; // 溢出提示文本
        //public GameObject multiRecipeInfoTextObj; // 有非主要配方的生产时，需要显示其他配方的产出
        public Text multiRecipeInfoText;

        public GameObject assemblerIconObj; // 生产机器图标
        public Text assemblerCountText; // 生产机器数量 

        public GameObject recipeGroupObj; // 主要配方的显示
        // public GameObject clearRecipePreferenceButtonObj; // 用于清除特定配方设定的按钮

        public Dictionary<int, UIButton> assemblerUIButtons;
        public Dictionary<int, UIButton> proliferatorUsedButtons; // 这里的key是incLevel

        public GameObject incToggleObj;
        public Text incText;

        public UIItemNodeSimple(ItemNode node, bool isResources ,UICalcWindow calcWindow, bool isProliferatorDemand = false)
        {
            // 如果公共资源尚未被初始化，则初始化
            if (backgroundSprite == null)
            {
                backgroundSprite = Resources.Load<Sprite>("ui/textures/sprites/sci-fi/window-content-3");
                buttonBackgroundSprite = Resources.Load<Sprite>("ui/textures/sprites/sci-fi/window-content-3");
                gearSprite = Resources.Load<Sprite>("icons/signal/signal-405");
                filterSprite = Resources.Load<Sprite>("ui/textures/sprites/icons/filter-icon");
                biaxialArrowSprite = Resources.Load<Sprite>("ui/textures/sprites/icons/biaxial-arrow");
                oreSprite = Resources.Load<Sprite>("ui/textures/sprites/icons/vein-icon-56");
                crossSprite = Resources.Load<Sprite>("ui/textures/sprites/icons/delete-icon");
                bannedSprite = Resources.Load<Sprite>("icons/signal/signal-509");
                todoListSprite = Resources.Load<Sprite>("ui/textures/sprites/test/test-list-alt");
            }


            obj = new GameObject();
            obj.name = "item";

            this.itemNode = node;
            this.parentCalcWindow = calcWindow;

            obj.AddComponent<RectTransform>().sizeDelta = new Vector2(UICalcWindow.sideCellWidth, UICalcWindow.sideCellHeight);

            // 加入背景图
            GameObject backObj = new GameObject();
            backObj.name = "bg";
            backObj.transform.SetParent(obj.transform);
            Image background = backObj.AddComponent<Image>();
            background.sprite = backgroundSprite;
            background.type = Image.Type.Sliced;
            background.color = backgroundImageColor;
            backObj.GetComponent<RectTransform>().sizeDelta = new Vector2(UICalcWindow.sideCellWidth - UICalcWindow.cellDistance, UICalcWindow.sideCellHeight - UICalcWindow.cellDistance / 2);

            // 设置图标
            GameObject iconObj = GameObject.Instantiate(UICalcWindow.iconObj_ButtonTip);
            iconObj.name = "icon";
            iconObj.transform.SetParent(obj.transform, false);
            iconObj.transform.localScale = Vector3.one;
            iconObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(15, 0, 0);

            // 产出速度文本
            GameObject outputSpeedTextObj = GameObject.Instantiate(UICalcWindow.TextWithUITip);
            outputSpeedTextObj.name = "speed-text";
            outputSpeedTextObj.transform.SetParent(obj.transform, false);
            outputSpeedTextObj.transform.localScale = Vector3.one;
            outputSpeedTextObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(65, 0, 0);
            outputText = outputSpeedTextObj.GetComponent<Text>();
            outputText.fontSize = 18;

            if (this.itemNode != null)
            {
                int itemId = itemNode.itemId;
                bool mainRecipeActive = itemNode.mainRecipe != null && itemNode.mainRecipe.count > 0.001f;

                // 设置产物图标和tip
                iconObj.GetComponent<Image>().sprite = LDB.items.Select(itemId)?.iconSprite;
                iconObj.GetComponent<UIButton>().tips.itemId = itemId;
                iconObj.GetComponent<UIButton>().tips.corner = 3;
                iconObj.GetComponent<UIButton>().tips.delay = 0.1f;

                string finalSpeedStr = Utils.KMG(itemNode.satisfiedSpeed);
                if(!isResources && !isProliferatorDemand) // 说明是为了显示副产物或者溢出量
                    finalSpeedStr = Utils.KMG(itemNode.satisfiedSpeed - itemNode.needSpeed);
                outputText.text = finalSpeedStr;
                if(isProliferatorDemand) // 如果是专门用于显示额外增产剂需求的
                {
                    outputText.fontSize = 16;
                    int maxAbility = 0;
                    for (int i = 0; i < CalcDB.proliferatorAbilities.Count; i++)
                    {
                        if (CalcDB.proliferatorAbilities[i] > maxAbility && CalcDB.proliferatorAbilities[i] < Cargo.incTableMilli.Length)
                            maxAbility = CalcDB.proliferatorAbilities[i];
                    }
                    outputText.text += $"\n({Utils.KMG(itemNode.satisfiedSpeed / (1.0 + Utils.GetIncMilli(maxAbility, parentCalcWindow.solution.userPreference)))})";
                }

                string speedDetails = "";

                // 如果有生产他的配方，说明可以不设为原矿，则增加取消作为原矿的按钮
                if (CalcDB.itemDict[itemId].recipes.Count > 0 && isResources)
                {
                    GameObject clearRecipePreferenceButtonObj = GameObject.Instantiate(UICalcWindow.iconObj_ButtonTip);
                    clearRecipePreferenceButtonObj.name = "cancel-ore";
                    clearRecipePreferenceButtonObj.transform.SetParent(obj.transform, false);
                    clearRecipePreferenceButtonObj.transform.localScale = Vector3.one;
                    clearRecipePreferenceButtonObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(120, 17, 0);
                    clearRecipePreferenceButtonObj.GetComponent<RectTransform>().sizeDelta = new Vector2(16, 16);
                    clearRecipePreferenceButtonObj.GetComponent<Image>().sprite = crossSprite;
                    clearRecipePreferenceButtonObj.GetComponent<Button>().onClick.AddListener(() => { RemoveThisFromRawOre(); });
                    clearRecipePreferenceButtonObj.GetComponent<UIButton>().tips.tipTitle = "不再视为原矿标题".Translate();
                    clearRecipePreferenceButtonObj.GetComponent<UIButton>().tips.tipText = "不再视为原矿说明".Translate();
                    clearRecipePreferenceButtonObj.GetComponent<UIButton>().tips.corner = 3;
                    clearRecipePreferenceButtonObj.GetComponent<UIButton>().tips.width = 200;
                    clearRecipePreferenceButtonObj.GetComponent<UIButton>().transitions[0].normalColor = new Color(0.6f, 0, 0, 1);
                    clearRecipePreferenceButtonObj.GetComponent<UIButton>().transitions[0].pressedColor = new Color(0.6f, 0, 0, 1);
                    clearRecipePreferenceButtonObj.GetComponent<UIButton>().transitions[0].mouseoverColor = new Color(0.9f, 0.2f, 0.2f, 1);
                }
            }
            // 加入到窗口中显示出来
            if (calcWindow != null)
            {
                obj.transform.SetParent(calcWindow.sideContentTrans, false);
                obj.transform.localScale = Vector3.one;
                obj.SetActive(false);
                obj.SetActive(true);
            }
        }

        /// <summary>
        /// 专用于创建一个文字标签，并可以放在GridLayoutGroup里
        /// </summary>
        /// <param name="text"></param>
        public UIItemNodeSimple(string text, UICalcWindow calcWindow)
        {
            obj = new GameObject();
            obj.name = "label";

            this.parentCalcWindow = calcWindow;

            obj.AddComponent<RectTransform>().sizeDelta = new Vector2(UICalcWindow.sideCellWidth, UICalcWindow.sideCellHeight);

            // 加入背景图
            //GameObject backObj = new GameObject();
            //backObj.name = "bg";
            //backObj.transform.SetParent(obj.transform);
            //Image background = backObj.AddComponent<Image>();
            //background.sprite = backgroundSprite;
            //background.type = Image.Type.Sliced;
            //background.color = new Color(0,0,0,0); // 设置背景图为透明
            //backObj.GetComponent<RectTransform>().sizeDelta = new Vector2(UICalcWindow.sideCellWidth - UICalcWindow.cellDistance, UICalcWindow.sideCellHeight - UICalcWindow.cellDistance / 2);

            // 加入文本
            GameObject textObj = GameObject.Instantiate(UICalcWindow.TextObj, obj.transform);
            textObj.GetComponent<Text>().text = text;
            textObj.GetComponent<Text>().fontSize = 18;
            textObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(20, -2, 0);

            // 加入到窗口中显示出来
            if (calcWindow != null)
            {
                obj.transform.SetParent(calcWindow.sideContentTrans, false);
                obj.transform.localScale = Vector3.one;
                obj.SetActive(false);
                obj.SetActive(true);
            }
        }

        public void RemoveThisFromRawOre()
        {
            UserPreference preference = parentCalcWindow.solution.userPreference;

            if (!preference.itemConfigs.ContainsKey(itemNode.itemId))
                preference.itemConfigs[itemNode.itemId] = new ItemConfig(itemNode.itemId);

            preference.itemConfigs[itemNode.itemId].forceNotOre = true;
            preference.itemConfigs[itemNode.itemId].consideredAsOre = false;
            preference.itemConfigs[itemNode.itemId].forceNotOre = true;
            preference.itemConfigs[itemNode.itemId].consideredAsOre = false;

            parentCalcWindow.solution.ReSolve();
            parentCalcWindow.RefreshAll();
        }
    }
}
