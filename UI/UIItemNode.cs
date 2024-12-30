using CommonAPI;
using DSPCalculator.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace DSPCalculator.UI
{
    public class UIItemNode
    {
        // 一些公共资源
        public static Color backgroundImageColor = new Color(0f, 0.811f, 1f, 0.072f);
        public static Color iconButtonHighLightColor = new Color(0.737f, 0.802f,1f, 0.362f);
        public static int buttonCountPerRow = 3; // 一行几个带图标的按钮
        public static Vector3 recipeGroupLocalPosition = new Vector3(0, 20, 0);

        // 对象资源
        public GameObject obj;

        public int ID;
        public UICalcWindow parentCalcWindow;
        public ItemNode itemNode;

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

        public UIItemNode(ItemNode node, UICalcWindow calcWindow) 
        {
            // 如果公共资源尚未被初始化，则初始化

            assemblerUIButtons = new Dictionary<int, UIButton>();
            proliferatorUsedButtons = new Dictionary<int, UIButton>();

            obj = new GameObject();
            obj.name = "product";

            this.itemNode = node;
            this.parentCalcWindow = calcWindow;

            obj.AddComponent<RectTransform>().sizeDelta = new Vector2(UICalcWindow.cellWidth, UICalcWindow.cellHeight);

            // 加入背景图
            GameObject backObj = new GameObject();
            backObj.name = "bg";
            backObj.transform.SetParent(obj.transform);
            Image background = backObj.AddComponent<Image>();
            background.sprite = UICalcWindow.backgroundSprite;
            background.type = Image.Type.Sliced;
            background.color = backgroundImageColor;
            backObj.GetComponent<RectTransform>().sizeDelta = new Vector2(UICalcWindow.cellWidth - UICalcWindow.cellDistance, UICalcWindow.cellHeight - UICalcWindow.cellDistance/2);
            
            // 设置图标
            GameObject iconObj = GameObject.Instantiate(UICalcWindow.iconObj_ButtonTip);
            iconObj.name = "icon";
            iconObj.transform.SetParent(obj.transform, false);
            iconObj.transform.localScale = Vector3.one;
            iconObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(20, 0, 0);

            // 产出速度文本
            GameObject outputSpeedTextObj = GameObject.Instantiate(UICalcWindow.TextWithUITip);
            outputSpeedTextObj.name = "speed-text";
            outputSpeedTextObj.transform.SetParent(obj.transform, false);
            outputSpeedTextObj.transform.localScale = Vector3.one;
            outputSpeedTextObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(80, 0, 0);
            outputText = outputSpeedTextObj.GetComponent<Text>();
            outputText.fontSize = 18;

            // 多配方说明文本，已废弃，改用鼠标悬停在速度上查看
            //multiRecipeInfoTextObj = GameObject.Instantiate(UICalcWindow.TextObj);
            //multiRecipeInfoTextObj.name = "multi-recipe-info";
            //multiRecipeInfoTextObj.transform.SetParent(obj.transform, false);
            //multiRecipeInfoTextObj.transform.localScale = Vector3.one;
            //multiRecipeInfoTextObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(130, 0, 0);
            //multiRecipeInfoTextObj.SetActive(false);
            //multiRecipeInfoText = multiRecipeInfoTextObj.GetComponent<Text>();

            // 产出过量提示文本
            overflowNoteTextObj = GameObject.Instantiate(UICalcWindow.TextObj);
            overflowNoteTextObj.transform.SetParent(obj.transform, false);
            overflowNoteTextObj.name = "overflow-note";
            overflowNoteTextObj.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
            overflowNoteTextObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(40, -25, 0);
            overflowNoteTextObj.GetComponent<Text>().text = "产出过量标签".Translate();
            overflowNoteTextObj.GetComponent<Text>().color = UICalcWindow.TextWarningColor;
            overflowNoteTextObj.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
            overflowNoteTextObj.SetActive(false);

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
                outputText.text = finalSpeedStr;

                string speedDetails = "";
                if (itemNode.byProductRecipes.Count > 0) // 说明产出有多个来源
                {
                    if (itemNode.mainRecipe != null) 
                    {
                        if(mainRecipeActive)
                            speedDetails += "来自calc".Translate() + "当前配方".Translate() + " " + Utils.KMG(itemNode.mainRecipe.GetOutputSpeedByItemId(itemId));
                    }
                    foreach (var recipeInfo in itemNode.byProductRecipes)
                    {
                        if (speedDetails.Length > 0)
                            speedDetails += "\n";

                        speedDetails += "来自calc".Translate() + recipeInfo.recipeNorm.oriProto.name + " " + Utils.KMG(recipeInfo.GetOutputSpeedByItemId(itemId));
                    }
                }
                

                if(itemNode.satisfiedSpeed - itemNode.needSpeed > 0.001f) // 说明有溢出
                {
                    overflowNoteTextObj.SetActive(true); // 溢出提示要显示出来
                    if (speedDetails.Length > 0)
                        speedDetails += "\n\n";
                    speedDetails += "实际需求calc".Translate() + Utils.KMG(itemNode.needSpeed) + "\n";
                    speedDetails += "溢出calc".Translate() + Utils.KMG(itemNode.satisfiedSpeed - itemNode.needSpeed);
                }
                if (speedDetails.Length > 0)
                {
                    outputText.color = UICalcWindow.TextWarningColor; // 速度颜色有变，提示玩家可以悬停查看细节
                    outputSpeedTextObj.GetComponent<UIButton>().tips.tipTitle = "产出calc".Translate() + " " + finalSpeedStr + "      ";
                    outputSpeedTextObj.GetComponent<UIButton>().tips.tipText = speedDetails;
                    outputSpeedTextObj.GetComponent<UIButton>().tips.corner = 3;
                    outputSpeedTextObj.GetComponent<UIButton>().tips.delay = 0.1f;
                    outputSpeedTextObj.GetComponent<Text>().raycastTarget = true; // 必须有这个鼠标悬停才能显示Tip
                }

                // 调整按钮、机器图标、机器数量、配方显示
                // 只有在主配方激活时才允许调整
                if (mainRecipeActive)
                {
                    // 显示生产机器图标及其所需数量的对象初始化
                    assemblerIconObj = GameObject.Instantiate(UICalcWindow.iconObj_ButtonTip);
                    assemblerIconObj.name = "assembler-icon";
                    assemblerIconObj.transform.SetParent(obj.transform, false);
                    assemblerIconObj.transform.localScale = Vector3.one;
                    assemblerIconObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(160, 0, 0);

                    // 生产机器数量文本
                    GameObject assemblerCountTextObj = GameObject.Instantiate(UICalcWindow.TextWithUITip);
                    assemblerCountTextObj.name = "assembler-count";
                    assemblerCountTextObj.transform.SetParent(obj.transform, false);
                    assemblerCountTextObj.transform.localScale = Vector3.one;
                    assemblerCountTextObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(205, 0, 0);
                    assemblerCountText = assemblerCountTextObj.GetComponent<Text>();
                    assemblerCountText.fontSize = 18;

                    int type = itemNode.mainRecipe.recipeNorm.type;
                    // 生产机器可调按钮
                    if (CalcDB.assemblerListByType.ContainsKey(type) && CalcDB.assemblerListByType[type].Count > 1)
                    {
                        List<AssemblerData> assemblers = CalcDB.assemblerListByType[type];
                        GameObject assemblerSelectionObj = new GameObject();
                        assemblerSelectionObj.name = "assembler-select";
                        assemblerSelectionObj.transform.SetParent(obj.transform, false);
                        assemblerSelectionObj.transform.localPosition = new Vector3(170, -2, 0);
                        for (int i = 0; i < assemblers.Count; i++)
                        {
                            int assemblerItemId = assemblers[i].ID;
                            GameObject aBtnObj = GameObject.Instantiate(UICalcWindow.imageButtonObj, assemblerSelectionObj.transform);
                            aBtnObj.GetComponent<Image>().sprite = UICalcWindow.buttonBackgroundSprite;
                            Image icon = aBtnObj.transform.Find("icon").GetComponent<Image>();
                            icon.sprite = assemblers[i].iconSprite;
                            aBtnObj.transform.localPosition = new Vector3(i % buttonCountPerRow * 35, i / buttonCountPerRow * 35);
                            aBtnObj.GetComponent<Button>().onClick.AddListener(() => { SetAssemblerPreference(assemblerItemId); });
                            // aBtnObj.GetComponent<UIButton>().transitions[0].highlightColorOverride = iconButtonHighLightColor;
                            assemblerUIButtons[assemblerItemId] = aBtnObj.GetComponent<UIButton>();
                            assemblerUIButtons[assemblerItemId].tips.itemId = assemblerItemId;
                            assemblerUIButtons[assemblerItemId].tips.corner = 3;
                            assemblerUIButtons[assemblerItemId].tips.delay = 0.1f;
                        }
                    }


                    // 更改配方按钮
                    if (CalcDB.itemDict[itemId].recipes.Count > 1)
                    {
                        GameObject recipeChangeButtonObj = GameObject.Instantiate(UICalcWindow.iconObj_ButtonTip);
                        recipeChangeButtonObj.name = "recipe-change";
                        recipeChangeButtonObj.transform.SetParent(obj.transform, false);
                        recipeChangeButtonObj.transform.localScale = Vector3.one;
                        recipeChangeButtonObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(292, 16, 0);
                        recipeChangeButtonObj.GetComponent<RectTransform>().sizeDelta = new Vector2(30, 30);
                        recipeChangeButtonObj.GetComponent<Image>().sprite = UICalcWindow.biaxialArrowSprite;
                        recipeChangeButtonObj.GetComponent<Button>().onClick.AddListener(() => { OnRecipeChangeButtonClick(); });
                        recipeChangeButtonObj.GetComponent<UIButton>().tips.tipTitle = "更改配方标题".Translate();
                        recipeChangeButtonObj.GetComponent<UIButton>().tips.tipText = "更改配方说明".Translate();
                        recipeChangeButtonObj.GetComponent<UIButton>().tips.corner = 3;
                        recipeChangeButtonObj.GetComponent<UIButton>().tips.width = 200;
                    }

                    // 清除指定配方规则的按钮
                    // 因为每次该配方都会重新计算，所以不用刷新，只在实例化时生成，并决定是否显示
                    if (parentCalcWindow.solution.userPreference.itemConfigs.ContainsKey(itemId) && parentCalcWindow.solution.userPreference.itemConfigs[itemId].recipeID > 0)
                    {
                        GameObject clearRecipePreferenceButtonObj = GameObject.Instantiate(UICalcWindow.iconObj_ButtonTip);
                        clearRecipePreferenceButtonObj.name = "clear-recipe-preference";
                        clearRecipePreferenceButtonObj.transform.SetParent(obj.transform, false);
                        clearRecipePreferenceButtonObj.transform.localScale = Vector3.one;
                        clearRecipePreferenceButtonObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(312, 27, 0);
                        clearRecipePreferenceButtonObj.GetComponent<RectTransform>().sizeDelta = new Vector2(16, 16);
                        clearRecipePreferenceButtonObj.GetComponent<Image>().sprite = UICalcWindow.crossSprite;
                        clearRecipePreferenceButtonObj.GetComponent<Button>().onClick.AddListener(() => { OnClearRecipePreferenceButtonClick(); });
                        clearRecipePreferenceButtonObj.GetComponent<UIButton>().tips.tipTitle = "清除配方设定标题".Translate();
                        clearRecipePreferenceButtonObj.GetComponent<UIButton>().tips.tipText = "清除配方设定说明".Translate();
                        clearRecipePreferenceButtonObj.GetComponent<UIButton>().tips.corner = 3;
                        clearRecipePreferenceButtonObj.GetComponent<UIButton>().tips.width = 200;
                        clearRecipePreferenceButtonObj.GetComponent<UIButton>().transitions[0].normalColor = new Color(0.6f, 0, 0, 1);
                        clearRecipePreferenceButtonObj.GetComponent<UIButton>().transitions[0].pressedColor = new Color(0.6f, 0, 0, 1);
                        clearRecipePreferenceButtonObj.GetComponent<UIButton>().transitions[0].mouseoverColor = new Color(0.9f, 0.2f, 0.2f, 1);
                    }

                    // 视为原矿按钮
                    GameObject treatAsOreButtonObj = GameObject.Instantiate(UICalcWindow.iconObj_ButtonTip);
                    treatAsOreButtonObj.name = "treat-as-ore";
                    treatAsOreButtonObj.transform.SetParent(obj.transform, false);
                    treatAsOreButtonObj.transform.localScale = Vector3.one;
                    treatAsOreButtonObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(292, -16, 0);
                    treatAsOreButtonObj.GetComponent<RectTransform>().sizeDelta = new Vector2(30, 30);
                    treatAsOreButtonObj.GetComponent<Image>().sprite = UICalcWindow.oreSprite;
                    treatAsOreButtonObj.GetComponent<Button>().onClick.AddListener(() => { OnConsiderAsRawOreButtonClick(); });
                    treatAsOreButtonObj.GetComponent<UIButton>().tips.tipTitle = "视为原矿标题".Translate();
                    treatAsOreButtonObj.GetComponent<UIButton>().tips.tipText = "视为原矿说明".Translate();
                    treatAsOreButtonObj.GetComponent<UIButton>().tips.corner = 3;
                    treatAsOreButtonObj.GetComponent<UIButton>().tips.width = 200;

                    // 生成配方显示
                    recipeGroupObj = new GameObject();
                    recipeGroupObj.name = "recipe-group";
                    recipeGroupObj.transform.SetParent(obj.transform, false);
                    // recipeGroupObj.transform.localPosition = new Vector3(-120, 20, 0); // 无UITip的位置设置
                    recipeGroupObj.transform.localPosition = new Vector3(-120, 0, 0); // 有UITip的位置设置
                    RecipeProto recipeProto = itemNode.mainRecipe.recipeNorm.oriProto;


                    int posX = 0;
                    int posXDelta = 40;
                    int iconSize = 40;
                    int totalCount = recipeProto.Results.Length + recipeProto.Items.Length;
                    if(totalCount >= 7) // 过长配方，图表变小
                    {
                        posXDelta = 32;
                        iconSize = 32;
                    }

                    for (int i = 0; i < recipeProto.Results.Length; i++)
                    {
                        // GameObject recipeItem = GameObject.Instantiate(UICalcWindow.iconObj_NoTip, recipeGroupObj.transform); // 使用无Tip版本则用这个，但是要注意下面的Tip设置也要删除
                        GameObject recipeItem = GameObject.Instantiate(UICalcWindow.iconObj_ButtonTip, recipeGroupObj.transform); // 使用有tip版本
                        recipeItem.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(posX, 0, 0);
                        recipeItem.GetComponent<RectTransform>().sizeDelta = new Vector2(iconSize, iconSize);
                        recipeItem.GetComponent<Image>().sprite = LDB.items.Select(recipeProto.Results[i]).iconSprite;
                        recipeItem.transform.Find("count").GetComponent<Text>().text = recipeProto.ResultCounts[i].ToString();
                        recipeItem.transform.Find("count").gameObject.SetActive(true); // 有tip的图标才需要，因为之前隐藏这个了
                        recipeItem.GetComponent<UIButton>().tips.itemId = recipeProto.Results[i]; // 有tip的图标才能写
                        recipeItem.GetComponent<UIButton>().tips.corner = 3; // 有tip的图标才能写
                        recipeItem.GetComponent<UIButton>().tips.delay = 0.3f; // 有tip的图标才能写
                        if (recipeProto.Type == ERecipeType.Fractionate)
                            recipeItem.transform.Find("count").GetComponent<Text>().text = "1";

                        posX += posXDelta;
                    }
                    GameObject recipeArrow = GameObject.Instantiate(UICalcWindow.arrowObj, recipeGroupObj.transform);
                    // recipeArrow.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(posX, -28, 0); // 无UITip的位置设置
                    recipeArrow.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(posX, -7, 0); // 有UITip的位置设置
                    recipeArrow.transform.Find("time").GetComponent<Text>().text = (1.0 * recipeProto.TimeSpend / 60).ToString() + " s";
                    if (recipeProto.Type == ERecipeType.Fractionate)
                        recipeArrow.transform.Find("time").GetComponent<Text>().text = string.Format("{0:P}", recipeProto.ResultCounts[0] * 1.0 / recipeProto.ItemCounts[0]);

                    posX += posXDelta;
                    for (int i = 0; i < recipeProto.Items.Length; i++)
                    {
                        // GameObject recipeItem = GameObject.Instantiate(UICalcWindow.iconObj_NoTip, recipeGroupObj.transform); // 使用无Tip版本则用这个，但是要注意下面的Tip设置也要删除
                        GameObject recipeItem = GameObject.Instantiate(UICalcWindow.iconObj_ButtonTip, recipeGroupObj.transform); // 使用有tip版本
                        recipeItem.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(posX, 0, 0);
                        recipeItem.GetComponent<RectTransform>().sizeDelta = new Vector2(iconSize, iconSize);
                        recipeItem.GetComponent<Image>().sprite = LDB.items.Select(recipeProto.Items[i]).iconSprite;
                        recipeItem.transform.Find("count").GetComponent<Text>().text = recipeProto.ItemCounts[i].ToString();
                        recipeItem.transform.Find("count").gameObject.SetActive(true); // 有tip的图标才需要，因为之前隐藏这个了
                        recipeItem.GetComponent<UIButton>().tips.itemId = recipeProto.Items[i]; // 有tip的图标才能写
                        recipeItem.GetComponent<UIButton>().tips.corner = 3; // 有tip的图标才能写
                        recipeItem.GetComponent<UIButton>().tips.delay = 0.3f; // 有tip的图标才能写
                        if (recipeProto.Type == ERecipeType.Fractionate)
                            recipeItem.transform.Find("count").GetComponent<Text>().text = "1";

                        posX += posXDelta;
                    }

                    // 切换增产按钮
                    incToggleObj = GameObject.Instantiate(UICalcWindow.incTogglePrefabObj, obj.transform);
                    incToggleObj.name = "inc-setting";
                    incToggleObj.transform.localPosition = new Vector3(175, 19, 0);
                    incToggleObj.transform.Find("inc-switch").GetComponent<Button>().onClick.AddListener(() => { OnIncToggleClick(); });
                    incText = incToggleObj.transform.Find("inc-effect-type-text").GetComponent<Text>();
                    GameObject incToggleThumb = incToggleObj.transform.Find("inc-switch/switch-thumb").gameObject;
                    bool isInc = parentCalcWindow.solution.userPreference.globalIsInc;
                    if (parentCalcWindow.solution.userPreference.recipeConfigs.ContainsKey(itemNode.mainRecipe.ID))
                    {
                        int forceIncMode = parentCalcWindow.solution.userPreference.recipeConfigs[itemNode.mainRecipe.ID].forceIncMode;
                        if (forceIncMode >= 0)
                            isInc = forceIncMode == 1;
                    }
                    isInc = isInc && itemNode.mainRecipe.recipeNorm.productive;
                    if(isInc)
                    {
                        incToggleThumb.GetComponent<RectTransform>().anchoredPosition = new Vector2(-10, 0);
                        incText.text = "额外产出calc".Translate();
                        incText.color = UICalcWindow.incModeTextColor;
                        incToggleObj.transform.Find("inc-switch").GetComponent<Image>().color = UICalcWindow.incModeImageColor;
                    }
                    else
                    {
                        incToggleThumb.GetComponent<RectTransform>().anchoredPosition = new Vector2(10, 0);
                        incText.text = "生产加速calc".Translate();
                        incText.color = UICalcWindow.accModeTextColor;
                        incToggleObj.transform.Find("inc-switch").GetComponent<Image>().color = UICalcWindow.accModeImageColor;
                        if(!itemNode.mainRecipe.recipeNorm.productive)
                            incToggleObj.transform.Find("inc-switch").GetComponent<Button>().interactable = false;
                    }
                    incToggleObj.SetActive(true);

                    // 增产剂切换图标
                    GameObject proliferatorSelectionObj = new GameObject();
                    proliferatorSelectionObj.name = "proliferator-select";
                    proliferatorSelectionObj.transform.SetParent(obj.transform, false);
                    proliferatorSelectionObj.transform.localPosition = new Vector3(295, -2, 0);
                    //先创建一个禁用增产剂的图标
                    int proliferatorItemId0 = 0;
                    int incLevel0 = 0;
                    GameObject pBtnObj0 = GameObject.Instantiate(UICalcWindow.imageButtonObj, proliferatorSelectionObj.transform);
                    pBtnObj0.GetComponent<Image>().sprite = UICalcWindow.buttonBackgroundSprite;
                    Image icon0 = pBtnObj0.transform.Find("icon").GetComponent<Image>();
                    icon0.sprite = UICalcWindow.bannedSprite;
                    icon0.color = Color.red; // 默认白色看不清
                    pBtnObj0.transform.localPosition = new Vector3(0, 0);
                    pBtnObj0.GetComponent<Button>().onClick.AddListener(() => { SetIncLevel(incLevel0); });
                    // pBtnObj0.GetComponent<UIButton>().transitions[0].highlightColorOverride = iconButtonHighLightColor;
                    proliferatorUsedButtons[incLevel0] = pBtnObj0.GetComponent<UIButton>();
                    proliferatorUsedButtons[incLevel0].tips.itemId = proliferatorItemId0;

                    for (int i = 0; i < CalcDB.proliferatorItemIds.Count; i++)
                    {
                        int proliferatorItemId = CalcDB.proliferatorItemIds[i];
                        int incLevel = CalcDB.proliferatorAbilities[i];
                        GameObject pBtnObj = GameObject.Instantiate(UICalcWindow.imageButtonObj, proliferatorSelectionObj.transform);
                        pBtnObj.GetComponent<Image>().sprite = UICalcWindow.buttonBackgroundSprite;
                        Image icon = pBtnObj.transform.Find("icon").GetComponent<Image>();
                        icon.sprite = LDB.items.Select(proliferatorItemId).iconSprite;
                        pBtnObj.transform.localPosition = new Vector3((i + 1) * 35, 0);
                        pBtnObj.GetComponent<Button>().onClick.AddListener(() => { SetIncLevel(incLevel); });
                        // pBtnObj.GetComponent<UIButton>().transitions[0].highlightColorOverride = iconButtonHighLightColor;
                        proliferatorUsedButtons[incLevel] = pBtnObj.GetComponent<UIButton>();
                        proliferatorUsedButtons[incLevel].tips.itemId = proliferatorItemId;
                    }
                }
            }


            // 加入到窗口中显示出来
            if (calcWindow != null)
            {
                obj.transform.SetParent(calcWindow.contentTrans, false);
                obj.transform.localScale = Vector3.one;
                obj.SetActive(false);
                obj.SetActive(true);
            }
            RefreshIncLevelDisplay();
            RefreshAssemblerDisplay(false);
        }


        public void RefreshAssemblerDisplay(bool refreshGlobal = true)
        {
            if (assemblerCountText != null && assemblerIconObj != null && itemNode.mainRecipe != null)
            {
                UserPreference preference = parentCalcWindow.solution.userPreference;
                RecipeInfo recipeInfo = itemNode.mainRecipe;
                int assemblerItemId = recipeInfo.assemblerItemId;
                if (assemblerItemId <= 0) // 代表全局和当前配方均无设置，那么读取最后一个配方，这种情况通常不会出现！！！！因为不会返回0，出现了就有问题了
                {
                    assemblerItemId = CalcDB.assemblerListByType[recipeInfo.recipeNorm.type][0].ID;
                }

                // 设置按钮高亮
                foreach (var item in assemblerUIButtons)
                {
                    if (item.Key == assemblerItemId)
                        item.Value.highlighted = true;
                    else
                        item.Value.highlighted = false;
                }

                AssemblerData assemblerData = CalcDB.assemblerDict[assemblerItemId];
                // 设置图标
                assemblerIconObj.GetComponent<Image>().sprite = assemblerData.iconSprite;
                // 设置鼠标悬停tip
                assemblerIconObj.GetComponent<UIButton>().tips.itemId = assemblerItemId;
                assemblerIconObj.GetComponent<UIButton>().tips.corner = 3; // 有tip的图标才能写
                assemblerIconObj.GetComponent<UIButton>().tips.delay = 0.1f; // 有tip的图标才能写
                // 设置工厂数量显示
                double finalCount = recipeInfo.count / assemblerData.speed / 60; // 除以60是因为计算都是以每s计算的，最终转换成工厂数量等都要按min，所以都是60分之一。
                if(!itemNode.mainRecipe.isInc && itemNode.mainRecipe.incLevel >=0 && itemNode.mainRecipe.incLevel< Cargo.accTableMilli.Length)
                {
                    finalCount = finalCount / (1.0 +  Utils.GetAccMilli(itemNode.mainRecipe.incLevel, parentCalcWindow.solution.userPreference));
                }
                assemblerCountText.text = "× " + Utils.KMG(finalCount);
            }
            if (refreshGlobal)
            {
                parentCalcWindow.RefreshFinalInfoText();
                parentCalcWindow.RefreshAssemblerDemandsDisplay();
            }
        }

        public void SetAssemblerPreference(int assemblerId)
        {
            if(itemNode != null && itemNode.mainRecipe != null)
            {
                RecipeInfo recipeInfo = itemNode.mainRecipe;
                int recipeId = recipeInfo.ID;
                UserPreference preference = parentCalcWindow.solution.userPreference;
                if(preference.recipeConfigs.ContainsKey(recipeId))
                {
                    preference.recipeConfigs[recipeId].assemblerItemId = assemblerId;
                }
                else
                {
                    preference.recipeConfigs[recipeId] = new RecipeConfig(recipeInfo);
                    preference.recipeConfigs[recipeId].assemblerItemId = assemblerId;
                }

                RefreshAssemblerDisplay();
            }

        }

        public void OnRecipeChangeButtonClick()
        {
            UIRecipePicker.Popup(new Vector2(100f, 200f), OnRecipePickerReturn, (ERecipeType)(-itemNode.itemId));
        }

        public void OnRecipePickerReturn(RecipeProto recipeProto)
        {
            if(recipeProto != null)
            {
                UserPreference preference = parentCalcWindow.solution.userPreference;
                if (!preference.itemConfigs.ContainsKey(itemNode.itemId))
                    preference.itemConfigs[itemNode.itemId] = new ItemConfig(itemNode.itemId);
                int oriRecipeId = preference.itemConfigs[itemNode.itemId].recipeID;
                preference.itemConfigs[itemNode.itemId].recipeID = recipeProto.ID;
                if (!parentCalcWindow.solution.ReSolve()) // 如果未解决路径，则还原此次用户配置
                {
                    preference.itemConfigs[itemNode.itemId].recipeID = oriRecipeId;
                    parentCalcWindow.solution.ReSolve();
                }
                parentCalcWindow.RefreshAll();
            }
        }

        public void OnClearRecipePreferenceButtonClick()
        {
            UserPreference preference = parentCalcWindow.solution.userPreference;
            if (preference.itemConfigs.ContainsKey(itemNode.itemId))
                preference.itemConfigs[itemNode.itemId].recipeID = 0;
            parentCalcWindow.solution.ReSolve();
            parentCalcWindow.RefreshAll();
        }

        public void OnConsiderAsRawOreButtonClick()
        {
            UserPreference preference = parentCalcWindow.solution.userPreference;
            if (!preference.itemConfigs.ContainsKey(itemNode.itemId))
                preference.itemConfigs[itemNode.itemId] = new ItemConfig(itemNode.itemId);

            preference.itemConfigs[itemNode.itemId].consideredAsOre = true;
            preference.itemConfigs[itemNode.itemId].forceNotOre = false;
            parentCalcWindow.solution.ReSolve();
            parentCalcWindow.RefreshAll();
        }

        public void OnIncToggleClick()
        {
            int recipeId = itemNode.mainRecipe.ID;
            UserPreference preference = parentCalcWindow.solution.userPreference;
            bool targetInc;
            if (!preference.recipeConfigs.ContainsKey(recipeId))
            {
                preference.recipeConfigs[recipeId] = new RecipeConfig(itemNode.mainRecipe);
                targetInc = !preference.globalIsInc;
                preference.recipeConfigs[recipeId].forceIncMode = targetInc ? 1 : 0;
            }
            else
            {
                if(preference.recipeConfigs[recipeId].forceIncMode >= 0)
                {
                    targetInc = !(preference.recipeConfigs[recipeId].forceIncMode == 1);
                }
                else
                {
                    targetInc = !preference.globalIsInc;
                }
                preference.recipeConfigs[recipeId].forceIncMode = targetInc ? 1 : 0;
            }
            GameObject incToggleThumb = incToggleObj.transform.Find("inc-switch/switch-thumb").gameObject;
            bool isInc = targetInc;
            if (isInc)
            {
                incToggleThumb.GetComponent<RectTransform>().anchoredPosition = new Vector2(-10, 0);
                incText.text = "额外产出calc".Translate();
                incText.color = UICalcWindow.incModeTextColor;
                incToggleObj.transform.Find("inc-switch").GetComponent<Image>().color = UICalcWindow.incModeImageColor;
            }
            else
            {
                incToggleThumb.GetComponent<RectTransform>().anchoredPosition = new Vector2(10, 0);
                incText.text = "生产加速calc".Translate();
                incText.color = UICalcWindow.accModeTextColor;
                incToggleObj.transform.Find("inc-switch").GetComponent<Image>().color = UICalcWindow.accModeImageColor;
                if (!itemNode.mainRecipe.recipeNorm.productive)
                    incToggleObj.transform.Find("inc-switch").GetComponent<Button>().interactable = false;
            }

            parentCalcWindow.nextFrameRecalc = true; // 不在这里直接计算是为了切换按钮能够感受更好的响应性
            //parentCalcWindow.solution.ReSolve();
            //parentCalcWindow.RefreshAll();
        }

        public void SetIncLevel(int incLevel)
        {
            int recipeId = itemNode.mainRecipe.ID;
            UserPreference preference = parentCalcWindow.solution.userPreference;
            int oriIncLevel = preference.globalIncLevel;
            if(preference.recipeConfigs.ContainsKey(recipeId))
            {
                oriIncLevel = preference.recipeConfigs[recipeId].incLevel;
            }
            if (oriIncLevel != incLevel)
            {
                if (!preference.recipeConfigs.ContainsKey(recipeId))
                {
                    preference.recipeConfigs[recipeId] = new RecipeConfig(itemNode.mainRecipe);
                    preference.recipeConfigs[recipeId].incLevel = incLevel;
                }
                else
                {
                    preference.recipeConfigs[recipeId].incLevel = incLevel;
                }

                bool isInc = preference.globalIsInc;
                if (preference.recipeConfigs[recipeId].forceIncMode >= 0)
                    isInc = preference.recipeConfigs[recipeId].forceIncMode == 1;
                RefreshIncLevelDisplay();
                if (!isInc) // 如果当前配方不是增产，不需要重新计算路径
                {
                    parentCalcWindow.RefreshAssemblerDemandsDisplay();
                    parentCalcWindow.RefreshFinalInfoText();
                    foreach (var uiNodeData in parentCalcWindow.uiItemNodes)
                    {
                        uiNodeData.RefreshAssemblerDisplay(false); // 刷新每个节点的assembler显示即可
                    }
                }
                else // 如果是增产，需要重新解决
                {
                    parentCalcWindow.nextFrameRecalc = true;
                }
            }
        }
        
        public void RefreshIncLevelDisplay()
        {
            int recipeId = itemNode.mainRecipe.ID;
            UserPreference preference = parentCalcWindow.solution.userPreference;
            int oriIncLevel = preference.globalIncLevel;
            if (preference.recipeConfigs.ContainsKey(recipeId))
            {
                if(preference.recipeConfigs[recipeId].incLevel >= 0)
                    oriIncLevel = preference.recipeConfigs[recipeId].incLevel;
            }
            foreach (var item in proliferatorUsedButtons)
            {
                if (item.Key == oriIncLevel)
                    item.Value.highlighted = true;
                else
                    item.Value.highlighted = false;
            }
        }
    }
}
