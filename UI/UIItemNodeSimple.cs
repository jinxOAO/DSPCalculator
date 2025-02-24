using DSPCalculator.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine;

namespace DSPCalculator.UI
{
    public class UIItemNodeSimple : UINode
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
        //public GameObject obj;
        public Image backgroundImg;

        public int ID;
        //public UICalcWindow parentCalcWindow;
        public ItemNode itemNode;
        public RecipeInfo mainRecipeInfo;
        public bool isProliferatorDemand;

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

        public RecipeInfo lastFocusedRecipe; // 仅用于，手动点击一个溢出产物时，如果该产物是由多个配方溢出的，则多次点击会在这些配方间循环聚焦

        public UIItemNodeSimple(ItemNode node, bool isResources, UICalcWindow calcWindow, bool isProliferatorDemand = false, bool isMixBeltInfo = false)
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
            this.isProliferatorDemand = isProliferatorDemand;

            obj.AddComponent<RectTransform>().sizeDelta = new Vector2(UICalcWindow.sideCellWidth, UICalcWindow.sideCellHeight);

            // 加入背景图
            GameObject backObj = new GameObject();
            backObj.name = "bg";
            backObj.transform.SetParent(obj.transform);
            backgroundImg = backObj.AddComponent<Image>();
            backgroundImg.sprite = backgroundSprite;
            backgroundImg.type = Image.Type.Sliced;
            backgroundImg.color = backgroundImageColor;
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
                if (!isProliferatorDemand) // 显示原矿需求时，需要的数值是speedFromOre
                {
                    finalSpeedStr = Utils.KMG(itemNode.speedFromOre);
                    if (!isResources) // 说明是为了显示副产物或者溢出量
                    {
                        finalSpeedStr = Utils.KMG(itemNode.satisfiedSpeed - itemNode.needSpeed);
                        iconObj.GetComponent<Button>().onClick.AddListener(() => { FocusTargetNode(itemId); });
                    }
                }
                outputText.text = finalSpeedStr;
                if (isMixBeltInfo)
                    outputText.text = itemNode.satisfiedSpeed.ToString() + " " + "条calc".Translate();
                if (isProliferatorDemand) // 如果是专门用于显示额外增产剂需求的
                {
                    outputText.fontSize = 16;
                    //int oriCount = LDB.items.Select(itemId).HpMax;
                    //int ability = CalcDB.proliferatorAbilitiesMap[itemId];
                    //int proliferatedCount = (int)(oriCount * (1.0 + Utils.GetIncMilli(ability, parentCalcWindow.solution.userPreference)));
                    if (parentCalcWindow.solution.proliferatorCount[itemId] > parentCalcWindow.solution.proliferatorCountSelfSprayed[itemId])
                        outputText.text += $"\n({Utils.KMG(parentCalcWindow.solution.proliferatorCountSelfSprayed[itemId])})";
                }

                string speedDetails = "";

                // 如果有生产他的配方，说明可以不设为原矿，则增加取消作为原矿的按钮，以及在新窗口中计算按钮
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

                if (CalcDB.itemDict[itemId].recipes.Count > 0 && isResources || isProliferatorDemand)
                {
                    //ui/textures/sprites/icons/insert-icon 方框右上角有加号
                    //ui/textures/sprites/dashboard/pading-icon-2 箭头向右上角指进方框
                    //ui/textures/sprites/icons/padding-icon 箭头向左下角指进方框
                    GameObject calcInNewWindowButtonObj = GameObject.Instantiate(UICalcWindow.iconObj_ButtonTip);
                    calcInNewWindowButtonObj.name = "open-new";
                    calcInNewWindowButtonObj.transform.SetParent(obj.transform, false);
                    calcInNewWindowButtonObj.transform.localScale = Vector3.one;
                    calcInNewWindowButtonObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(100, 17, 0);
                    if (isProliferatorDemand)
                        calcInNewWindowButtonObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(120, 17, 0);
                    calcInNewWindowButtonObj.GetComponent<RectTransform>().sizeDelta = new Vector2(16, 16);
                    calcInNewWindowButtonObj.GetComponent<Image>().sprite = UICalcWindow.arrowInBoxSprite;
                    calcInNewWindowButtonObj.GetComponent<Button>().onClick.AddListener(() => { CalcInNewWindow(); });
                    calcInNewWindowButtonObj.GetComponent<UIButton>().tips.tipTitle = "在新窗口中计算标题".Translate();
                    calcInNewWindowButtonObj.GetComponent<UIButton>().tips.tipText = "在新窗口中计算说明".Translate();
                    calcInNewWindowButtonObj.GetComponent<UIButton>().tips.corner = 3;
                    calcInNewWindowButtonObj.GetComponent<UIButton>().tips.width = 210;
                    calcInNewWindowButtonObj.GetComponent<UIButton>().transitions[0].normalColor = new Color(0.4f, 0.4f, 0.7f, 1);
                    calcInNewWindowButtonObj.GetComponent<UIButton>().transitions[0].pressedColor = new Color(0.4f, 0.4f, 0.7f, 1);
                    calcInNewWindowButtonObj.GetComponent<UIButton>().transitions[0].mouseoverColor = new Color(0.5f, 0.5f, 0.8f, 1);
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


        public override void OnUpdate(bool isMoving)
        {
            if (!isMoving)
            {
                Color targetColor = backgroundImageColor;
                if (backgroundImg != null && backgroundImg.color.a > targetColor.a)
                {
                    float targetAlpha = backgroundImg.color.a - 0.02f;
                    if (targetAlpha < targetColor.a)
                        targetAlpha = targetColor.a;
                    backgroundImg.color = new Color(targetColor.r, targetColor.g, targetColor.b, targetAlpha);
                }
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

            parentCalcWindow.solution.ReSolve(Convert.ToDouble(parentCalcWindow.speedInputObj.GetComponent<InputField>().text));
            parentCalcWindow.RefreshAll();
        }

        public void FocusTargetNode(int itemId)
        {
            bool canLocateAndIsMainProduct = false; // 这个判断条件说明，溢出产物本身是某种主产物，是生产线中必须要输入的一种，并且有其主要配方，因此要定位到主要配方上
            if (parentCalcWindow.uiItemNodeOrders.ContainsKey(itemId) && parentCalcWindow.solution.itemNodes.ContainsKey(itemId))
            {
                if (parentCalcWindow.solution.itemNodes[itemId].mainRecipe != null && parentCalcWindow.solution.itemNodes[itemId].mainRecipe.count > 0.001f)
                    canLocateAndIsMainProduct = true;
            }
            if (canLocateAndIsMainProduct)
            {
                int order = parentCalcWindow.uiItemNodeOrders[itemId];
                int totalCount = parentCalcWindow.uiItemNodeOrders.Count;
                if (order >= 0 && order < parentCalcWindow.uiItemNodeOrders.Count)
                {
                    // 跳转到目标位置，小于8不需要跳转
                    if (totalCount >= 8)
                    {
                        int calcOrder = order - 3;
                        if (calcOrder < 0)
                            calcOrder = 0;
                        int calcTotal = totalCount - 7;
                        if (calcOrder > calcTotal)
                            calcOrder = calcTotal;
                        float vPos = 1f - (1.0f * calcOrder / calcTotal);
                        parentCalcWindow.targetVerticalPosition = vPos;
                        //parentCalcWindow.contentScrollRect.verticalNormalizedPosition = vPos;
                    }


                    UIItemNode targetNode = parentCalcWindow.uiItemNodes[order];
                    Color oldColor = targetNode.backgroundImg.color;
                    targetNode.backgroundImg.color = new Color(oldColor.r, oldColor.g, oldColor.b, 1f); // 让他闪烁一次
                }
            }
            else // 说明这个溢出产物不被任何主要生产线中作为需求，或者是没有主要配方（全靠其他主要产线的副产物的产量就足够全部需求了），这两种情况下都应该定位到产出此产物配方的那个产线上
            {
                Dictionary<int, RecipeInfo> recipeInfos = parentCalcWindow.solution.recipeInfos;
                bool canSearch = lastFocusedRecipe == null;
                foreach (var recipeInfoData in recipeInfos)
                {
                    if (!canSearch)
                    {
                        if (recipeInfoData.Value != lastFocusedRecipe)
                        {
                            continue;
                        }
                        else
                        {
                            canSearch = true;
                            continue;
                        }
                    }
                    else
                    {
                        if (recipeInfoData.Value.recipeNorm.products.Contains(itemId))
                        {
                            for (int i = 0; i < recipeInfoData.Value.recipeNorm.products.Length; i++)
                            {
                                int maybeMainProductId = recipeInfoData.Value.recipeNorm.products[i];
                                Dictionary<int, ItemNode> itemNodes = parentCalcWindow.solution.itemNodes;
                                if (itemNodes.ContainsKey(maybeMainProductId) && itemNodes[maybeMainProductId].mainRecipe != null && itemNodes[maybeMainProductId].mainRecipe.ID == recipeInfoData.Value.ID) // 找到啦
                                {
                                    lastFocusedRecipe = recipeInfoData.Value;
                                    int focusItemId = maybeMainProductId;
                                    FocusTargetNode(focusItemId); // 不会无限递归，因为此处保证了itemNodes里面含有这个id，所以不再能继续递归
                                    return;
                                }
                            }
                        }
                    }
                }
                // 如果能进行到这里，说明没找到（上面没有return成功），则置last记录为null，从头找
                lastFocusedRecipe = null;
                foreach (var recipeInfoData in recipeInfos)
                {
                    if (recipeInfoData.Value.recipeNorm.products.Contains(itemId))
                    {
                        for (int i = 0; i < recipeInfoData.Value.recipeNorm.products.Length; i++)
                        {
                            int maybeMainProductId = recipeInfoData.Value.recipeNorm.products[i];
                            Dictionary<int, ItemNode> itemNodes = parentCalcWindow.solution.itemNodes;
                            if (itemNodes.ContainsKey(maybeMainProductId) && itemNodes[maybeMainProductId].mainRecipe != null && itemNodes[maybeMainProductId].mainRecipe.ID == recipeInfoData.Value.ID) // 找到啦
                            {
                                lastFocusedRecipe = recipeInfoData.Value;
                                int focusItemId = maybeMainProductId;
                                FocusTargetNode(focusItemId); // 不会无限递归，因为此处保证了itemNodes里面含有这个id，所以不再能继续递归
                                return;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 打开一个新窗口来计算这个拥有配方的原材料
        /// </summary>
        public void CalcInNewWindow(bool autoFold = false)
        {
            UICalcWindow calcWindow = WindowsManager.OpenOne(true);
            int itemId = itemNode.itemId;
            long requiredSpeed = (long)Math.Ceiling(itemNode.speedFromOre);
            if (isProliferatorDemand)
                requiredSpeed = (long)Math.Ceiling(parentCalcWindow.solution.proliferatorCountSelfSprayed[itemId]);

            // 打开新窗口后，该物品必须默认不被视为原矿，否则对于默认视为原矿的材料没有意义
            UserPreference thatPreference = calcWindow.solution.userPreference;
            if (!thatPreference.itemConfigs.ContainsKey(itemNode.itemId))
                thatPreference.itemConfigs[itemNode.itemId] = new ItemConfig(itemNode.itemId);
            thatPreference.itemConfigs[itemNode.itemId].forceNotOre = true;
            thatPreference.itemConfigs[itemNode.itemId].consideredAsOre = false;

            calcWindow.speedInputObj.GetComponent<InputField>().text = requiredSpeed.ToString();
            calcWindow.OnTargetSpeedChange(requiredSpeed.ToString());
            calcWindow.OnTargetProductChange(LDB.items.Select(itemId));

            if (autoFold)
                calcWindow.SwitchWindowSize();
        }
    }
}
