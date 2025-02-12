using CommonAPI;
using DSPCalculator.Compatibility;
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
        public static Color backgroundImageFinishedColor = new Color(0.5f, 0.5f, 0.5f, 0.28f);
        // public static Color inserterOrangeColor = new Color(0.9906f, 0.5897f, 0.3691f, 1f);
        public static int buttonCountPerRow = 3; // 一行几个带图标的按钮
        public static Vector3 recipeGroupLocalPosition = new Vector3(0, 20, 0);

        // 对象资源
        public GameObject obj;
        public Image backgroundImg;

        public int ID;
        public UICalcWindow parentCalcWindow;
        public ItemNode itemNode;

        public Text outputText;
        public GameObject overflowNoteTextObj; // 溢出提示文本
        //public GameObject multiRecipeInfoTextObj; // 有非主要配方的生产时，需要显示其他配方的产出
        public Text multiRecipeInfoText;

        public GameObject assemblerIconObj; // 生产机器图标
        public GameObject assemblerCountTextObj; // 生产机器数量Obj
        public Text assemblerCountText; // 生产机器数量 
        public GameObject IASpecializationIconGroup; // 星际组装厂切换特化类型的图标按钮的父级Obj

        public GameObject recipeGroupObj; // 主要配方的显示
        // public GameObject clearRecipePreferenceButtonObj; // 用于清除特定配方设定的按钮

        public Dictionary<int, UIButton> assemblerUIButtons;
        public Dictionary<int, UIButton> proliferatorUsedButtons; // 这里的key是incLevel
        public List<UIButton> IASpecUIButtons;

        public GameObject incToggleObj;
        public Text incText;

        public Image cbFinishedMark;

        public UIItemNode(ItemNode node, UICalcWindow calcWindow) 
        {
            // 如果公共资源尚未被初始化，则初始化

            assemblerUIButtons = new Dictionary<int, UIButton>();
            proliferatorUsedButtons = new Dictionary<int, UIButton>();
            IASpecUIButtons = new List<UIButton>();

            obj = new GameObject();
            obj.name = "product";

            this.itemNode = node;
            this.parentCalcWindow = calcWindow;

            obj.AddComponent<RectTransform>().sizeDelta = new Vector2(UICalcWindow.cellWidth, UICalcWindow.cellHeight);

            // 加入背景图
            GameObject backObj = new GameObject();
            backObj.name = "bg";
            backObj.transform.SetParent(obj.transform);
            backObj.AddComponent<Button>().onClick.AddListener(OnFinishedMarkCheckboxClick);
            backObj.AddComponent<UIButton>().transitions = new UIButton.Transition[0]; // 否则会UIButton的LateUpdate报错
            backObj.GetComponent<UIButton>().audios.enterName = "ui-hover-0";
            backObj.GetComponent<UIButton>().audios.downName = "ui-click-0";
            backgroundImg = backObj.AddComponent<Image>();
            backgroundImg.sprite = UICalcWindow.backgroundSprite;
            backgroundImg.type = Image.Type.Sliced;
            backgroundImg.color = backgroundImageColor;
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
                if (itemNode.mainRecipe != null && itemNode.mainRecipe.useIA)
                {
                    outputText.supportRichText = true;
                    outputText.text += "<size=12>\n" + "组装厂输入".Translate() + "" + itemNode.mainRecipe.GetIAInputCount().ToString("N0") + "</size>";
                }

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
                    outputSpeedTextObj.GetComponent<Text>().text = outputSpeedTextObj.GetComponent<Text>().text + "*";
                    outputSpeedTextObj.GetComponent<Text>().raycastTarget = true; // 必须有这个鼠标悬停才能显示Tip
                    // 由于其挡住了鼠标交互，所以添加点击事件（以及声音，但是不需要添加经过声音），相当于点击空白处（与其他产物格子体验一致）
                    outputSpeedTextObj.GetComponent<Button>().onClick.AddListener(OnFinishedMarkCheckboxClick);
                    outputSpeedTextObj.GetComponent<UIButton>().audios.downName = "ui-click-0";
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
                    assemblerCountTextObj = GameObject.Instantiate(UICalcWindow.TextWithUITip);
                    assemblerCountTextObj.name = "assembler-count";
                    assemblerCountTextObj.transform.SetParent(obj.transform, false);
                    assemblerCountTextObj.transform.localScale = Vector3.one;
                    assemblerCountTextObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(205, 0, 0);
                    assemblerCountTextObj.GetComponent<UIButton>().tips.delay = 0.4f;
                    assemblerCountTextObj.GetComponent<UIButton>().tips.corner = 2;
                    assemblerCountText = assemblerCountTextObj.GetComponent<Text>();
                    assemblerCountText.fontSize = 18;
                    assemblerCountText.raycastTarget = true; // 为了可以显示Tip
                    // 由于其挡住了鼠标交互，所以添加点击事件（以及声音，但是不需要添加经过声音），相当于点击空白处（与其他产物格子体验一致）
                    assemblerCountTextObj.GetComponent<Button>().onClick.AddListener(OnFinishedMarkCheckboxClick);
                    assemblerCountTextObj.GetComponent<UIButton>().audios.downName = "ui-click-0";

                    int type = itemNode.mainRecipe.recipeNorm.type;
                    // 生产机器可调按钮
                    if (CalcDB.assemblerListByType.ContainsKey(type) && (CalcDB.assemblerListByType[type].Count > 1 || CompatManager.MMS))
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
                        // 添加星际组装厂按钮
                        if (CompatManager.MMS)
                        {
                            int idx = assemblers.Count;
                            GameObject aBtnObj = GameObject.Instantiate(UICalcWindow.imageButtonObj, assemblerSelectionObj.transform);
                            aBtnObj.GetComponent<Image>().sprite = UICalcWindow.buttonBackgroundSprite;
                            Image icon = aBtnObj.transform.Find("icon").GetComponent<Image>();
                            icon.sprite = LDB.items.Select(UICalcWindow.IAIconItemId)?.iconSprite;
                            aBtnObj.transform.Find("icon").GetComponent<RectTransform>().sizeDelta = new Vector2(25, 25);
                            aBtnObj.transform.localPosition = new Vector3(idx % buttonCountPerRow * 35, idx / buttonCountPerRow * 35);
                            aBtnObj.GetComponent<Button>().onClick.AddListener(() => { SetAssemblerPreference(-1); });
                            // aBtnObj.GetComponent<UIButton>().transitions[0].highlightColorOverride = iconButtonHighLightColor;
                            assemblerUIButtons[-1] = aBtnObj.GetComponent<UIButton>();
                            assemblerUIButtons[-1].tips.tipTitle = "星际组装厂".Translate();
                            assemblerUIButtons[-1].tips.corner = 3;
                            assemblerUIButtons[-1].tips.delay = 0.1f;
                            assemblerUIButtons[-1].tips.width = 0;

                            // 特化按钮们
                            IASpecializationIconGroup = new GameObject();
                            IASpecializationIconGroup.transform.SetParent(obj.transform);
                            IASpecializationIconGroup.name = "spec-group";
                            IASpecializationIconGroup.transform.localScale = Vector3.one;
                            IASpecializationIconGroup.transform.localPosition = new Vector3(-240, -1, 0);

                            for (int i = 0; i < 6; i++) // 特化
                            {
                                GameObject iconButton = GameObject.Instantiate(UICalcWindow.iconObj_ButtonTip);
                                iconButton.name = $"{i}";
                                iconButton.transform.SetParent(IASpecializationIconGroup.transform, false);
                                iconButton.transform.localScale = Vector3.one;
                                if (i > 0)
                                {
                                    int iconId = UICalcWindow.IASpecializationIconItemMap[i];
                                    if (iconId > 0)
                                        iconButton.GetComponent<Image>().sprite = LDB.items.Select(iconId)?.iconSprite;
                                    else
                                        iconButton.GetComponent<Image>().sprite = LDB.recipes.Select(-iconId)?.iconSprite;
                                }
                                else
                                    iconButton.GetComponent<Image>().sprite = UICalcWindow.crossSprite; // x号
                                iconButton.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(i % 3 * 25, 12f - 25 * (i / 3), 0);
                                iconButton.GetComponent<RectTransform>().sizeDelta = new Vector2(20, 20);
                                UIButton uibtn = iconButton.GetComponent<UIButton>();
                                if(i > 0)
                                    uibtn.tips.tipTitle = $"特化{i}介绍标题".Translate();
                                else
                                    uibtn.tips.tipTitle = $"无特化".Translate();
                                uibtn.tips.corner = 3;
                                uibtn.transitions[0].normalColor = new Color(0.4f, 0.4f, 0.4f, 1f);
                                uibtn.transitions[0].mouseoverColor = new Color(0.5f, 0.5f, 0.5f, 1f);
                                uibtn.transitions[0].pressedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
                                uibtn.transitions[0].highlightColorOverride = new Color(0.8f, 0.8f, 0.8f, 1f);
                                uibtn.transitions[0].highlightSizeMultiplier = 1f;
                                uibtn.transitions[0].disabledColor = new Color(0.3f, 0.3f, 0.3f, 1f);
                                uibtn.transitions[0].damp = 1f;
                                IASpecUIButtons.Add(uibtn);
                                int ii = i;
                                iconButton.GetComponent<Button>().onClick.AddListener(() => { OnSpecializationIconClick(ii); });
                            }
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
                        Navigation n = new Navigation();
                        n.mode = Navigation.Mode.None;
                        recipeChangeButtonObj.GetComponent<Button>().navigation = n;
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
                        Navigation n = new Navigation();
                        n.mode = Navigation.Mode.None;
                        clearRecipePreferenceButtonObj.GetComponent<Button>().navigation = n;
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
                    Navigation nvg = new Navigation();
                    nvg.mode = Navigation.Mode.None;
                    treatAsOreButtonObj.GetComponent<Button>().navigation = nvg;

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
                        int resultItemId = recipeProto.Results[i];
                        recipeItem.GetComponent<Button>().onClick.AddListener(() => { FocusTargetNode(resultItemId); });
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
                        int resourceItemId = recipeProto.Items[i];
                        recipeItem.GetComponent<Button>().onClick.AddListener(() => { FocusTargetNode(resourceItemId); });
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
                    if (itemNode.mainRecipe.useIA)
                        isInc = itemNode.mainRecipe.isInc;
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
                        int incLevel = CalcDB.proliferatorAbilitiesMap[proliferatorItemId];
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

                    GameObject finishedMarkObj = GameObject.Instantiate(UICalcWindow.checkBoxObj, obj.transform);
                    finishedMarkObj.name = "finished-mark";
                    cbFinishedMark = finishedMarkObj.GetComponent<Image>();
                    finishedMarkObj.transform.Find("text").gameObject.SetActive(false);
                    finishedMarkObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(10, 22, 0);
                    finishedMarkObj.GetComponent<Button>().onClick.AddListener(OnFinishedMarkCheckboxClick);
                    finishedMarkObj.GetComponent<UIButton>().tips.tipTitle = "标记为已完成".Translate();
                    finishedMarkObj.GetComponent<UIButton>().tips.delay = 0.3f;
                    finishedMarkObj.GetComponent<UIButton>().tips.corner = 3;
                
                    //if(CompatManager.MMS && itemNode.mainRecipe.IASpecializationType == 2 && itemNode.mainRecipe.GetSpeckBuffLevel() > 0) // 化工厂一定享受满增产
                    //{
                    //    int recipeId = itemNode.mainRecipe.ID;
                    //    if(!parentCalcWindow.solution.userPreference.recipeConfigs.ContainsKey(recipeId))
                    //    {
                    //        parentCalcWindow.solution.userPreference.recipeConfigs[recipeId] = new RecipeConfig(itemNode.mainRecipe);
                    //    }
                    //    parentCalcWindow.solution.userPreference.recipeConfigs[recipeId].forceIncMode = 1;
                    //    parentCalcWindow.solution.userPreference.recipeConfigs[recipeId].incLevel = 4;
                    //}
                }

                // 混带的分拣器信息
                if (parentCalcWindow.solution.userPreference.showMixBeltInfo)
                {
                    if (recipeGroupObj != null)
                        recipeGroupObj.SetActive(false);
                    GameObject insertersObj = new GameObject();
                    insertersObj.name = "inserter-group";
                    insertersObj.transform.SetParent(obj.transform, false);
                    insertersObj.transform.localPosition = new Vector3(-120, 0, 0); // 有UITip的位置设置
                    int[] mk3;
                    int[] mk2;
                    int[] mk1;
                    itemNode.CalcInserterNeeds(out mk3, out mk2, out mk1);

                    GameObject totalCountTextObj = GameObject.Instantiate(UICalcWindow.TextWithUITip);
                    totalCountTextObj.name = "inserter-basic-count";
                    totalCountTextObj.transform.SetParent(insertersObj.transform, false);
                    totalCountTextObj.transform.localScale = Vector3.one;
                    totalCountTextObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(0, 0, 0);
                    Text totalCountText = totalCountTextObj.GetComponent<Text>();
                    totalCountText.fontSize = 18;
                    totalCountText.text = (itemNode.GetInserterRatio()).ToString() + " " + "份calc".Translate() + " = ";
                    totalCountTextObj.GetComponent<UIButton>().tips.tipTitle = "份数标题".Translate();
                    totalCountTextObj.GetComponent<UIButton>().tips.tipText = "份数说明".Translate();
                    totalCountTextObj.GetComponent<UIButton>().tips.corner = 3;
                    totalCountTextObj.GetComponent<UIButton>().tips.delay = 0.2f;
                    totalCountTextObj.GetComponent<Text>().raycastTarget = true; // 鼠标悬停显示Tip必须要

                    GameObject mk3IconObj = GameObject.Instantiate(UICalcWindow.iconObj_ButtonTip, insertersObj.transform); // 使用有tip版本
                    mk3IconObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(70, 0, 0);
                    mk3IconObj.GetComponent<RectTransform>().sizeDelta = new Vector2(40, 40);
                    mk3IconObj.GetComponent<Image>().sprite = LDB.items.Select(CalcDB.inserterMk3Id).iconSprite;
                    mk3IconObj.transform.Find("count").GetComponent<Text>().verticalOverflow = VerticalWrapMode.Overflow;
                    mk3IconObj.transform.Find("count").GetComponent<Text>().alignment = TextAnchor.UpperCenter;
                    mk3IconObj.transform.Find("count").GetComponent<Text>().text = $"\n{mk3[0]}/{mk3[1]}/{mk3[2]}";
                    mk3IconObj.transform.Find("count").gameObject.SetActive(true); // 有tip的图标才需要，因为之前隐藏这个了
                    mk3IconObj.GetComponent<UIButton>().tips.itemId = CalcDB.inserterMk3Id; // 有tip的图标才能写
                    mk3IconObj.GetComponent<UIButton>().tips.corner = 3; // 有tip的图标才能写
                    mk3IconObj.GetComponent<UIButton>().tips.delay = 0.3f; // 有tip的图标才能写

                    GameObject mk2IconObj = GameObject.Instantiate(UICalcWindow.iconObj_ButtonTip, insertersObj.transform); // 使用有tip版本
                    mk2IconObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(120, 0, 0);
                    mk2IconObj.GetComponent<RectTransform>().sizeDelta = new Vector2(40, 40);
                    mk2IconObj.GetComponent<Image>().sprite = LDB.items.Select(CalcDB.inserterMk2Id).iconSprite;
                    mk2IconObj.transform.Find("count").GetComponent<Text>().verticalOverflow = VerticalWrapMode.Overflow;
                    mk2IconObj.transform.Find("count").GetComponent<Text>().alignment = TextAnchor.UpperCenter;
                    mk2IconObj.transform.Find("count").GetComponent<Text>().text = $"\n{mk2[0]}/{mk2[1]}/{mk2[2]}";
                    mk2IconObj.transform.Find("count").gameObject.SetActive(true); // 有tip的图标才需要，因为之前隐藏这个了
                    mk2IconObj.GetComponent<UIButton>().tips.itemId = CalcDB.inserterMk2Id; // 有tip的图标才能写
                    mk2IconObj.GetComponent<UIButton>().tips.corner = 3; // 有tip的图标才能写
                    mk2IconObj.GetComponent<UIButton>().tips.delay = 0.3f; // 有tip的图标才能写

                    GameObject mk1IconObj = GameObject.Instantiate(UICalcWindow.iconObj_ButtonTip, insertersObj.transform); // 使用有tip版本
                    mk1IconObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(170, 0, 0);
                    mk1IconObj.GetComponent<RectTransform>().sizeDelta = new Vector2(40, 40);
                    mk1IconObj.GetComponent<Image>().sprite = LDB.items.Select(CalcDB.inserterMk1Id).iconSprite;
                    mk1IconObj.transform.Find("count").GetComponent<Text>().verticalOverflow = VerticalWrapMode.Overflow;
                    mk1IconObj.transform.Find("count").GetComponent<Text>().alignment = TextAnchor.UpperCenter;
                    mk1IconObj.transform.Find("count").GetComponent<Text>().text = $"\n{mk1[0]}/{mk1[1]}/{mk1[2]}";
                    mk1IconObj.transform.Find("count").gameObject.SetActive(true); // 有tip的图标才需要，因为之前隐藏这个了
                    mk1IconObj.GetComponent<UIButton>().tips.itemId = CalcDB.inserterMk1Id; // 有tip的图标才能写
                    mk1IconObj.GetComponent<UIButton>().tips.corner = 3; // 有tip的图标才能写
                    mk1IconObj.GetComponent<UIButton>().tips.delay = 0.3f; // 有tip的图标才能写
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
            RefreshFinishedMark();
        }

        public void OnUpdate(bool isMoving)
        {
            if (!isMoving)
            {
                Color targetColor = backgroundImageColor;
                if (parentCalcWindow.solution.userPreference.finishedRecipes.ContainsKey(itemNode.mainRecipe.ID))
                {
                    targetColor = backgroundImageFinishedColor;
                }
                if (backgroundImg.color.a > targetColor.a)
                {
                    float targetAlpha = backgroundImg.color.a - 0.02f;
                    if (targetAlpha < targetColor.a)
                        targetAlpha = targetColor.a;
                    backgroundImg.color = new Color(targetColor.r, targetColor.g, targetColor.b, targetAlpha);
                }
            }
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
                if (recipeInfo.useIA)
                    assemblerItemId = -1;

                // 设置按钮高亮
                foreach (var item in assemblerUIButtons)
                {
                    if (item.Key == assemblerItemId)
                        item.Value.highlighted = true;
                    else
                        item.Value.highlighted = false;
                }

                if (recipeInfo.useIA)
                {
                    assemblerIconObj.GetComponent<Image>().sprite = LDB.items.Select(UICalcWindow.IAIconItemId)?.iconSprite;
                    assemblerIconObj.GetComponent<UIButton>().tips.tipTitle = "星际组装厂".Translate();
                    assemblerIconObj.GetComponent<UIButton>().tips.itemId = 0;
                    assemblerIconObj.GetComponent<UIButton>().tips.corner = 3; 
                    assemblerIconObj.GetComponent<UIButton>().tips.delay = 0.1f;
                    assemblerCountTextObj.SetActive(false);
                    if (IASpecializationIconGroup != null)
                        IASpecializationIconGroup.SetActive(true);
                    // 处理切换特化的按钮
                    for (int i = 0; i < IASpecUIButtons.Count; i++)
                    {
                        if(recipeInfo.IASpecializationType == i)
                            IASpecUIButtons[i].highlighted = true;
                        else
                            IASpecUIButtons[i].highlighted = false;
                    }
                }
                else
                {
                    AssemblerData assemblerData = CalcDB.assemblerDict[assemblerItemId];
                    // 设置图标
                    assemblerIconObj.GetComponent<Image>().sprite = assemblerData.iconSprite;
                    // 设置鼠标悬停tip
                    assemblerIconObj.GetComponent<UIButton>().tips.itemId = assemblerItemId;
                    assemblerIconObj.GetComponent<UIButton>().tips.corner = 3; // 有tip的图标才能写
                    assemblerIconObj.GetComponent<UIButton>().tips.delay = 0.1f; // 有tip的图标才能写
                    // 设置工厂数量显示
                    assemblerCountTextObj.SetActive(true);
                    if(IASpecializationIconGroup != null)
                        IASpecializationIconGroup.SetActive(false);
                    double finalCount = recipeInfo.count / assemblerData.speed / 60; // 除以60是因为计算都是以每s计算的，最终转换成工厂数量等都要按min，所以都是60分之一。
                    if (!itemNode.mainRecipe.isInc && itemNode.mainRecipe.incLevel >= 0 && itemNode.mainRecipe.incLevel < Cargo.accTableMilli.Length)
                    {
                        finalCount = finalCount / (1.0 + Utils.GetAccMilli(itemNode.mainRecipe.incLevel, parentCalcWindow.solution.userPreference));
                    }
                    if (parentCalcWindow.solution.userPreference.roundUpAssemgblerNum) // 根据生产设施数量是否向上取整的设置进行
                    {
                        long ceilingCount = (long)Math.Ceiling(finalCount);
                        assemblerCountText.text = "× " + Utils.KMG(ceilingCount);
                        if (ceilingCount != finalCount && ceilingCount - finalCount > 0.005)
                        {
                            assemblerCountText.text = assemblerCountText.text + "*"; // 加星号提示玩家有向上取整
                            if (finalCount >= 1000)
                                assemblerCountTextObj.GetComponent<UIButton>().tips.tipTitle = finalCount.ToString("0,0.##");
                            else
                                assemblerCountTextObj.GetComponent<UIButton>().tips.tipTitle = finalCount.ToString("0.##");
                        }
                        else
                        {
                            if (finalCount >= 10000)
                                assemblerCountTextObj.GetComponent<UIButton>().tips.tipTitle = finalCount.ToString("0,0.##");
                            else
                                assemblerCountTextObj.GetComponent<UIButton>().tips.tipTitle = "";
                        }
                    }
                    else
                    {
                        assemblerCountText.text = "× " + Utils.KMGForceDigi(finalCount);
                        assemblerCountTextObj.GetComponent<UIButton>().tips.tipTitle = "";
                    }
                }
            }
            if (refreshGlobal)
            {
                parentCalcWindow.RefreshFinalInfoText();
                parentCalcWindow.RefreshAssemblerDemandsDisplay();
            }
        }

        public void SetAssemblerPreference(int assemblerId)
        {
            if (itemNode != null && itemNode.mainRecipe != null)
            {
                RecipeInfo recipeInfo = itemNode.mainRecipe;
                int recipeId = recipeInfo.ID;
                UserPreference preference = parentCalcWindow.solution.userPreference;
                if (!preference.recipeConfigs.ContainsKey(recipeId))
                {
                    preference.recipeConfigs[recipeId] = new RecipeConfig(recipeInfo);
                }
                if(assemblerId > 0)
                {
                    preference.recipeConfigs[recipeId].forceUseIA = false;
                    preference.recipeConfigs[recipeId].assemblerItemId = assemblerId;
                }
                else if (assemblerId == -1)
                {
                    preference.recipeConfigs[recipeId].forceUseIA = true;
                }

                if (CompatManager.GB || CompatManager.MMS)
                {
                    parentCalcWindow.nextFrameRecalc = true;
                }
                else
                {
                    RefreshAssemblerDisplay();
                }
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
            if (itemNode.mainRecipe != null)
            {
                int recipeId = itemNode.mainRecipe.ID;
                UserPreference preference = parentCalcWindow.solution.userPreference;
                int oriIncLevel = preference.globalIncLevel;
                if (preference.recipeConfigs.ContainsKey(recipeId))
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
                    if (!isInc) 
                    {
                        parentCalcWindow.nextFrameRecalc = true;
                    }
                    else 
                    {
                        parentCalcWindow.nextFrameRecalc = true;
                    }
                }
            }
        }
        
        public void RefreshIncLevelDisplay()
        {
            if (itemNode.mainRecipe != null)
            {
                int recipeId = itemNode.mainRecipe.ID;
                UserPreference preference = parentCalcWindow.solution.userPreference;
                int oriIncLevel = preference.globalIncLevel;
                if (preference.recipeConfigs.ContainsKey(recipeId))
                {
                    if (preference.recipeConfigs[recipeId].incLevel >= 0)
                        oriIncLevel = preference.recipeConfigs[recipeId].incLevel;
                }
                foreach (var item in proliferatorUsedButtons)
                {
                    if (item.Key == oriIncLevel)
                        item.Value.highlighted = true;
                    else
                        item.Value.highlighted = false;
                }

                RefreshUIsByIASpec();
            }
        }
        /// <summary>
        /// 点击星际组装厂特化按钮时的设定
        /// </summary>
        public void OnSpecializationIconClick(int type)
        {
            RecipeInfo recipeInfo = itemNode.mainRecipe;
            if(recipeInfo != null)
            {
                if (type != recipeInfo.IASpecializationType)
                {
                    int recipeId = recipeInfo.ID;
                    UserPreference preference = parentCalcWindow.solution.userPreference;
                    if(!preference.recipeConfigs.ContainsKey(recipeId))
                    {
                        preference.recipeConfigs[recipeId] = new RecipeConfig(recipeInfo);
                    }
                    preference.recipeConfigs[recipeId].IAType = type;
                    parentCalcWindow.nextFrameRecalc = true;
                }
            }
        }

        /// <summary>
        /// 将配方标记为已完成或未完成
        /// </summary>
        public void OnFinishedMarkCheckboxClick()
        {
            if(itemNode.mainRecipe != null && itemNode.mainRecipe.count > 0.001f)
            {
                if (parentCalcWindow.solution.userPreference.finishedRecipes.ContainsKey(itemNode.mainRecipe.ID))
                    parentCalcWindow.solution.userPreference.finishedRecipes.Remove(itemNode.mainRecipe.ID);
                else
                    parentCalcWindow.solution.userPreference.finishedRecipes[itemNode.mainRecipe.ID] = 1;

                foreach (var uiNode in parentCalcWindow.uiItemNodes)
                {
                    uiNode.RefreshFinishedMark();
                }
            }
        }

        /// <summary>
        /// 刷新已完成的标记
        /// </summary>
        public void RefreshFinishedMark()
        {
            if(itemNode.mainRecipe != null && cbFinishedMark != null)
            {
                if (parentCalcWindow.solution.userPreference.finishedRecipes.ContainsKey(itemNode.mainRecipe.ID))
                {
                    cbFinishedMark.sprite = UICalcWindow.checkboxOnSprite;
                    backgroundImg.color = backgroundImageFinishedColor;
                }
                else
                {
                    cbFinishedMark.sprite = UICalcWindow.checkboxOffSprite;
                    backgroundImg.color = backgroundImageColor;
                }
            }
        }

        /// <summary>
        /// 聚焦到UICalcWindow中的特定物品的node（如果是原材料则不聚焦），并使其ui背景闪烁一次
        /// </summary>
        /// <param name="itemId"></param>
        public void FocusTargetNode(int itemId)
        {
            if(parentCalcWindow.uiItemNodeOrders.ContainsKey(itemId))
            {
                int order = parentCalcWindow.uiItemNodeOrders[itemId];
                int totalCount = parentCalcWindow.uiItemNodeOrders.Count;
                if (order >= 0 && order < parentCalcWindow.uiItemNodeOrders.Count)
                {
                    // 跳转到目标位置，小于8不需要跳转
                    if(totalCount >= 8)
                    {
                        int calcOrder = order - 3;
                        int calcTotal = totalCount - 7;
                        if(calcOrder < 0)
                            calcOrder = 0;
                        if(calcOrder > calcTotal)
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
            if(parentCalcWindow.uiItemSimplesByItemId.ContainsKey(itemId))
            {
                UIItemNodeSimple targetNodeSimple = parentCalcWindow.uiItemSimplesByItemId[itemId];
                Color oldColor = targetNodeSimple.backgroundImg.color;
                targetNodeSimple.backgroundImg.color = new Color(oldColor.r, oldColor.g, oldColor.b, 1f); // 闪烁一次
            }
        }

        /// <summary>
        /// 由于巨构版本的计算器点击按钮后都会重新计算，所以此刷新只需要在刷新增产剂后执行一次，切不需要复原什么gameobject的active属性之类的
        /// </summary>
        public void RefreshUIsByIASpec()
        {
            if (itemNode.mainRecipe != null)
            {
                if (CompatManager.MMS && itemNode.mainRecipe.useIA)
                {
                    incToggleObj.transform.Find("inc-switch").GetComponent<Button>().interactable = false; // 星际组装厂禁止调整
                    if (itemNode.mainRecipe.IASpecializationType == 2 && itemNode.mainRecipe.GetSpecBuffLevel() > 0)
                    {
                        foreach (var item in proliferatorUsedButtons)
                        {
                            if (item.Key == 4)
                                item.Value.highlighted = true;
                            else
                                item.Value.gameObject.SetActive(false);
                        }
                    }

                    if(!itemNode.mainRecipe.canInc) // 无法增产的配方不能设置增产剂，无效的
                    {
                        foreach (var item in proliferatorUsedButtons)
                        {
                            item.Value.gameObject.SetActive(false);
                        }
                    }
                }
            }
        }
    }
}
