using DSPCalculator.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine;
using CommonAPI;

namespace DSPCalculator.UI
{
    public class UIItemNodeTarget : UINode
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

        public static Color defaultBgColor = new Color(0, 0.811f, 1f, 0.072f);
        public static Color mouseOverBgColor = new Color(0.5f, 0.811f, 1f, 0.1f);
        public static Color unfocusedBgColor = new Color(0.5f, 0.7f, 0.9f, 0.072f);

        // 对象资源
        //public GameObject obj;
        public Image backgroundImg;

        public int itemId;
        public double targetSpeed;
        public int targetsIndex;
        //public UICalcWindow parentCalcWindow;

        public GameObject mainInfoGroupObj;
        public GameObject addTextObj;
        public GameObject speedInputObj;
        public GameObject overflowNoteTextObj; // 溢出提示文本

        public UIButton bgUIBtn;

        public UIButton targetProductIconUIBtn;
        public Image targetProductIcon;
        public UIButton calcInNewWindowUIBtn;
        public Image calcInNewWindowIcon;

        /// <summary>
        /// 专门用于创建增加一个目标产物的按钮
        /// </summary>
        public UIItemNodeTarget(int index, int itemId, double speed, UICalcWindow calcWindow)
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
            this.targetsIndex = index;
            this.itemId = itemId;
            this.targetSpeed = speed;
            this.parentCalcWindow = calcWindow;

            obj = new GameObject();
            obj.name = "item";

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

            // 主题信息组
            mainInfoGroupObj = obj.CreateEmptyGameObject("main-info");

            // 设置图标
            GameObject targetProductIconObj = GameObject.Instantiate(parentCalcWindow.targetProductIconObj, mainInfoGroupObj.transform);
            targetProductIconObj.transform.localScale = Vector3.one;
            RectTransform rect = targetProductIconObj.GetComponent<RectTransform>();
            rect.anchorMax = new Vector2(0, 1);
            rect.anchorMin = new Vector2(0, 1);
            rect.anchoredPosition3D = new Vector3(-42, 0, 0); // 10
            rect.sizeDelta = new Vector2(40, 40); // 原本是64->54
            targetProductIconObj.transform.Find("white").GetComponent<RectTransform>().sizeDelta = new Vector2(32, 32); // 原本是54->40

            targetProductIconObj.GetComponent<UIButton>().transitions[0].normalColor = UICalcWindow.itemIconNormalColor; // 原本的颜色较暗，大约在0.5，高亮0.66
            targetProductIconObj.GetComponent<UIButton>().transitions[0].mouseoverColor = UICalcWindow.itemIconHighlightColor;

            targetProductIcon = targetProductIconObj.transform.Find("white").GetComponent<Image>();
            targetProductIconObj.GetComponent<Button>().onClick.RemoveAllListeners();
            targetProductIconObj.GetComponent<Button>().onClick.AddListener(() => { OnTargetProductIconClick(); });
            //targetProductIcon.sprite = Resources.Load<Sprite>("ui/textures/sprites/icons/explore-icon"); // 是个放大镜图标
            targetProductIconUIBtn = targetProductIconObj.GetComponent<UIButton>();
            if (itemId > 0)
            {
                targetProductIcon.sprite = LDB.items.Select(itemId)?.iconSprite;
                targetProductIconUIBtn.tips.itemId = itemId;
            }
            else
            {
                targetProductIcon.sprite = UICalcWindow.itemNotSelectedSprite; // 是循环箭头环绕的齿轮，去掉40则是64*64大小的相同图标
                targetProductIconUIBtn.tips.itemId = 0;
            }

            // 可编辑速度文本
            speedInputObj = GameObject.Instantiate(parentCalcWindow.speedInputObj, mainInfoGroupObj.transform);
            speedInputObj.name = "speed-input";
            speedInputObj.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 25); // 100 30
            speedInputObj.GetComponent<RectTransform>().pivot = new Vector2(0, 0.5f);
            speedInputObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(-20, 0, 0);
            speedInputObj.GetComponent<InputField>().text = ((long)speed).ToString();
            speedInputObj.GetComponent<InputField>().contentType = InputField.ContentType.DecimalNumber;
            speedInputObj.GetComponent<InputField>().characterLimit = 12;
            speedInputObj.GetComponent<InputField>().transition = Selectable.Transition.None; // 要不然鼠标不在上面时颜色会很浅，刚打开容易找不到，不够明显
            speedInputObj.GetComponent<InputField>().onEndEdit.RemoveAllListeners();
            speedInputObj.GetComponent<InputField>().onEndEdit.AddListener((x) => OnSpeedEndEdit(x));
            speedInputObj.GetComponent<Image>().color = new Color(0, 0, 0, 0.5f);
            speedInputObj.transform.Find("value-text").GetComponent<Text>().color = Color.white;
            speedInputObj.transform.Find("value-text").GetComponent<Text>().fontSize = 16;
            speedInputObj.GetComponent<UIButton>().tips.tipTitle = "";
            speedInputObj.GetComponent<UIButton>().tips.tipText = "";
            speedInputObj.SetActive(false);
            speedInputObj.SetActive(true); // 这样切一次颜色才能显示正常

            // 从目标中移除按钮，以及在新窗口中计算按钮
            if (targetsIndex < parentCalcWindow.solution.targets.Count)
            {
                GameObject clearRecipePreferenceButtonObj = GameObject.Instantiate(UICalcWindow.iconObj_ButtonTip);
                clearRecipePreferenceButtonObj.name = "cancel-ore";
                clearRecipePreferenceButtonObj.transform.SetParent(obj.transform, false);
                clearRecipePreferenceButtonObj.transform.localScale = Vector3.one;
                clearRecipePreferenceButtonObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(120, 17, 0);
                clearRecipePreferenceButtonObj.GetComponent<RectTransform>().sizeDelta = new Vector2(16, 16);
                clearRecipePreferenceButtonObj.GetComponent<Image>().sprite = crossSprite;
                clearRecipePreferenceButtonObj.GetComponent<Button>().onClick.AddListener(() => { RemoveThisFromTargets(); });
                clearRecipePreferenceButtonObj.GetComponent<UIButton>().tips.tipTitle = "移除目标产物标题".Translate();
                clearRecipePreferenceButtonObj.GetComponent<UIButton>().tips.tipText = "移除目标产物说明".Translate();
                clearRecipePreferenceButtonObj.GetComponent<UIButton>().tips.corner = 3;
                clearRecipePreferenceButtonObj.GetComponent<UIButton>().tips.width = 210;
                clearRecipePreferenceButtonObj.GetComponent<UIButton>().transitions[0].normalColor = new Color(0.6f, 0, 0, 1);
                clearRecipePreferenceButtonObj.GetComponent<UIButton>().transitions[0].pressedColor = new Color(0.6f, 0, 0, 1);
                clearRecipePreferenceButtonObj.GetComponent<UIButton>().transitions[0].mouseoverColor = new Color(0.9f, 0.2f, 0.2f, 1);

                //ui/textures/sprites/icons/insert-icon 方框右上角有加号
                //ui/textures/sprites/dashboard/pading-icon-2 箭头向右上角指进方框
                //ui/textures/sprites/icons/padding-icon 箭头向左下角指进方框
                GameObject calcInNewWindowButtonObj = GameObject.Instantiate(UICalcWindow.iconObj_ButtonTip);
                calcInNewWindowButtonObj.name = "open-new";
                calcInNewWindowButtonObj.transform.SetParent(obj.transform, false);
                calcInNewWindowButtonObj.transform.localScale = Vector3.one;
                calcInNewWindowButtonObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(100, 17, 0);
                calcInNewWindowButtonObj.GetComponent<RectTransform>().sizeDelta = new Vector2(16, 16);
                calcInNewWindowIcon = calcInNewWindowButtonObj.GetComponent<Image>();
                calcInNewWindowButtonObj.GetComponent<Image>().sprite = UICalcWindow.arrowInBoxSprite;
                calcInNewWindowButtonObj.GetComponent<Button>().onClick.AddListener(() => { CalcInNewWindow(); });
                calcInNewWindowUIBtn = calcInNewWindowButtonObj.GetComponent<UIButton>();
                calcInNewWindowButtonObj.GetComponent<UIButton>().tips.tipTitle = "移除并在新窗口中计算标题".Translate();
                calcInNewWindowButtonObj.GetComponent<UIButton>().tips.tipText = "移除并在新窗口中计算说明".Translate();
                calcInNewWindowButtonObj.GetComponent<UIButton>().tips.corner = 3;
                calcInNewWindowButtonObj.GetComponent<UIButton>().tips.width = 210;
                calcInNewWindowButtonObj.GetComponent<UIButton>().transitions[0].normalColor = new Color(0.4f, 0.4f, 0.7f, 1);
                calcInNewWindowButtonObj.GetComponent<UIButton>().transitions[0].pressedColor = new Color(0.4f, 0.4f, 0.7f, 1);
                calcInNewWindowButtonObj.GetComponent<UIButton>().transitions[0].mouseoverColor = new Color(0.5f, 0.5f, 0.8f, 1);
            }
            else // 说明是新增目标产物的Node，显示一个加号可以点
            {
                mainInfoGroupObj.SetActive(false);

                Button btn = obj.transform.Find("bg").gameObject.AddComponent<Button>();
                btn.onClick.AddListener(() => { OnAddButtonClick(); });
                bgUIBtn = obj.transform.Find("bg").gameObject.AddUIButtonForImage();
                UIButton.Transition uibtn0 = bgUIBtn.transitions[0];
                bgUIBtn.transitions = new UIButton.Transition[2];
                bgUIBtn.transitions[0] = uibtn0;

                addTextObj = GameObject.Instantiate(UICalcWindow.TextWithUITip);
                addTextObj.name = "add";
                addTextObj.transform.SetParent(obj.transform, false);
                addTextObj.transform.localScale = Vector3.one;
                addTextObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(55, 4, 0);
                GameObject.Destroy(addTextObj.GetComponent<UIButton>());
                Text addText = addTextObj.GetComponent<Text>();
                addText.fontSize = 50;
                addText.text = "+";
                addText.raycastTarget = false;

                uibtn0.normalColor = unfocusedBgColor;
                uibtn0.pressedColor = unfocusedBgColor;
                uibtn0.mouseoverColor = mouseOverBgColor;

                bgUIBtn.transitions[1] = Utils.CreateUIButtonTransition(addText);
                bgUIBtn.transitions[1].normalColor = new Color(0.5f, 0.5f, 0.5f, 1f);
                bgUIBtn.transitions[1].mouseoverColor = new Color(0.7f, 0.7f, 0.7f, 1f);
                bgUIBtn.transitions[1].pressedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
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

        public override void OnUpdate(bool isMoving)
        {
            if (calcInNewWindowUIBtn != null)
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    if (calcInNewWindowUIBtn.transitions[0].normalColor.r != 0.4f)
                    {
                        calcInNewWindowUIBtn.transitions[0].normalColor = new Color(0.4f, 0.4f, 0.7f, 1);
                        calcInNewWindowUIBtn.transitions[0].pressedColor = new Color(0.4f, 0.4f, 0.7f, 1);
                        calcInNewWindowUIBtn.transitions[0].mouseoverColor = new Color(0.5f, 0.5f, 0.8f, 1);
                        calcInNewWindowIcon.color = (calcInNewWindowUIBtn.isPointerEnter && !calcInNewWindowUIBtn.isPointerDown) ? calcInNewWindowUIBtn.transitions[0].mouseoverColor : calcInNewWindowUIBtn.transitions[0].normalColor;
                    }
                }
                else
                {
                    if (calcInNewWindowUIBtn.transitions[0].normalColor.r != 0.7f)
                    {
                        calcInNewWindowUIBtn.transitions[0].normalColor = new Color(0.7f, 0.4f, 0.2f, 1);
                        calcInNewWindowUIBtn.transitions[0].pressedColor = new Color(0.7f, 0.4f, 0.2f, 1);
                        calcInNewWindowUIBtn.transitions[0].mouseoverColor = new Color(0.8f, 0.5f, 0.2f, 1);
                        calcInNewWindowIcon.color = (calcInNewWindowUIBtn.isPointerEnter && !calcInNewWindowUIBtn.isPointerDown) ? calcInNewWindowUIBtn.transitions[0].mouseoverColor : calcInNewWindowUIBtn.transitions[0].normalColor;
                    }
                }
            }
        }

        public void OnAddButtonClick()
        {
            bgUIBtn.transitions[0].normalColor = defaultBgColor;
            bgUIBtn.transitions[0].highlightColorOverride = defaultBgColor;
            bgUIBtn.transitions[0].pressedColor = defaultBgColor;
            addTextObj.SetActive(false);
            mainInfoGroupObj.SetActive(true);
            // 顺便帮玩家按下选择目标产物按钮
            OnTargetProductIconClick();
        }

        public void RemoveThisFromTargets()
        {
            if(parentCalcWindow.solution.targets.Count > targetsIndex)
            {
                parentCalcWindow.solution.targets.RemoveAt(targetsIndex);
                parentCalcWindow.nextFrameRecalc = true;
            }
        }

        public void FocusTargetNode()
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
        }

        /// <summary>
        /// 打开一个新窗口来计算这个拥有配方的原材料
        /// </summary>
        public void CalcInNewWindow(bool autoFold = false)
        {
            UICalcWindow calcWindow = WindowsManager.OpenOne(true);
            long requiredSpeed = (long)Math.Ceiling(targetSpeed);

            // 打开新窗口后，该物品必须默认不被视为原矿，否则对于默认视为原矿的材料没有意义
            calcWindow.solution.userPreference = parentCalcWindow.solution.userPreference.DeepCopy();
            UserPreference thatPreference = calcWindow.solution.userPreference;
            if (!thatPreference.itemConfigs.ContainsKey(itemId))
                thatPreference.itemConfigs[itemId] = new ItemConfig(itemId);
            thatPreference.itemConfigs[itemId].forceNotOre = true;
            thatPreference.itemConfigs[itemId].consideredAsOre = false;

            calcWindow.speedInputObj.GetComponent<InputField>().text = requiredSpeed.ToString();
            calcWindow.OnTargetSpeedChange(requiredSpeed.ToString());
            calcWindow.OnTargetProductChange(LDB.items.Select(itemId));

            if (autoFold)
                calcWindow.SwitchWindowSize();

            if(!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
                RemoveThisFromTargets();
        }


        public void OnSpeedEndEdit(string text)
        {
            double newTargetSpeed = Convert.ToDouble(speedInputObj.GetComponent<InputField>().text);
            if (targetSpeed > 0 && newTargetSpeed <= 0)
            {
                RemoveThisFromTargets();
            }
            else
            {
                targetSpeed = newTargetSpeed;
                CheckAndRecalc();
            }
        }

        public void OnTargetProductIconClick()
        {
            if(targetsIndex >= parentCalcWindow.solution.targets.Count) // 说明是新增目标产物的UINode，允许更改目标item
            {
                UIItemPicker.showAll = true;
                if (parentCalcWindow.windowObj.GetComponent<RectTransform>().anchoredPosition.x > -150 && !parentCalcWindow.isLargeWindow)
                    UIItemPicker.Popup(new Vector2(-300f, 100f), OnTargetProductChange);
                else
                    UIItemPicker.Popup(parentCalcWindow.windowObj.GetComponent<RectTransform>().anchoredPosition + new Vector2(300f, -200f), OnTargetProductChange);
            }
            else
            {
                FocusTargetNode();
            }
        }

        public void OnTargetProductChange(ItemProto item)
        {
            UIItemPicker.showAll = false;
            if (item != null)
            {
                targetProductIcon.sprite = item.iconSprite;
                targetProductIconUIBtn.tips.corner = 3;
                targetProductIconUIBtn.tips.itemId = item.ID;
                targetProductIconUIBtn.tips.delay = 0.1f;
                itemId = item.ID;
                targetSpeed = Convert.ToDouble(speedInputObj.GetComponent<InputField>().text);
                CheckAndRecalc();
            }
        }

        public void CheckAndRecalc()
        {
            if (itemId > 0 && targetSpeed > 0) // 两个都是有效数值，才会去计算
            {
                parentCalcWindow.AddOrUpdateTargetThenResolve(targetsIndex, itemId, targetSpeed);
            }
        }
    }
}
