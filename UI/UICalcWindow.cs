using DSPCalculator.Logic;
using NGPT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace DSPCalculator.UI
{
    public class UICalcWindow
    {
        // 固定参数
        public const float cellWidth = 890;
        public const float cellHeight = 80;
        public const float cellDistance = 6; // 实际上被用来设定，每个cell北京图片的大小（等于cell的长宽减去这个distance），以此来实现cell间有缝隙的效果
        public const float sideCellWidth = 138.66f;
        public const float sideCellHeight = 45;
        public const int sideCellCountPerRow = 3;
        public static Color itemIconNormalColor = new Color(0.6f, 0.6f, 0.6f, 1);
        public static Color itemIconHighlightColor = new Color(0.7f, 0.7f, 0.7f, 1);
        public static Color itemIconPressedColor = new Color(0.6f, 0.6f, 0.6f, 1);
        public static Color itemIconDisabledColor = new Color(0.5f, 0.5f, 0.5f, 1);
        public static Color TextWhiteColor = new Color(0.588f, 0.588f, 0.588f, 1f);
        public static Color TextWarningColor = new Color(0.852f, 0.487f, 0.022f, 1f);
        public static float largeWindowWidth = 1340;
        public static float smallWindowWidth = 315;
        public static float largeWindowViewGroupWidth = 890;
        public static float smallWindowViewGroupWidth = 290;
        public static float animationSpeed = 150;
        // "ui/textures/sprites/icons/eraser-icon" 橡皮擦
        // "ui/textures/sprites/icons/minus-32" 粗短横线

        public bool isTopAndActive;
        public bool nextFrameRecalc;

        // 一些全局prefab
        public static GameObject recipeObj;
        public static GameObject arrowObj;
        public static GameObject iconObj_NoTip; // 无UITip的图标
        public static GameObject iconButtonObj; // 有UITip且有Button的图标
        public static GameObject TextObj; // 文本
        public static GameObject TextWithUITip; // 具有UITip的文本
        public static GameObject textButtonObj; // 有文字的按钮
        public static GameObject imageButtonObj; // 只有图片的按钮
        public static GameObject incToggleObj; // 增产切换按钮
        public static Sprite leftTriangleSprite;
        public static Sprite rightTriangleSprite;

        // UI元素
        public GameObject windowObj;
        public GameObject viewGroupObj;
        public GameObject mainCanvasObj;
        public GameObject switchSizeButtonObj;
        public Text titleText;
        public Image targetProductIcon;
        public UIButton targetProductIconUIBtn;
        public List<UIItemNode> uiItemNodes;
        public List<UIItemNodeSimple> uiSideItemNodes;
        public ScrollRect contentScrollRect; // 可滚动的rect
        public Transform contentTrans; // 用于放置UIItemNode的GameObject的父级物体的transform
        public GameObject targetProductIconObj;
        public GameObject speedInputObj; // 输入产物速度的
        public GameObject perMinTextObj; // /min 文本
        public Transform sideContentTrans; // 右侧防止小型溢出产物、原材料等的父级物体的transform


        public SolutionTree solution; // 该窗口对应的量化计算路径

        public bool isLargeWindow;

        /// <summary>
        /// 创建新窗口
        /// </summary>
        public UICalcWindow(int i)
        {
            TryInitStaticPrefabs();

            solution = new SolutionTree();
            uiItemNodes = new List<UIItemNode>();
            uiSideItemNodes = new List<UIItemNodeSimple>();
            isTopAndActive = true;
            isLargeWindow = true;
            nextFrameRecalc = false;

            GameObject oriWindowObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Blueprint Browser");
            GameObject parentWindowObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Calc Window Group");

            windowObj = GameObject.Instantiate(oriWindowObj);
            windowObj.name = "calc-window" + " " + i.ToString();
            windowObj.transform.SetParent(parentWindowObj.transform);
            windowObj.GetComponent<RectTransform>().pivot = new Vector2(0, 1); // 这是为了能让其在缩放时，以左上角为锚点，而不是中心
            windowObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(-670, 357,0);
            windowObj.transform.localScale = Vector3.one;
            windowObj.AddComponent<RectMask2D>();

            // 移除不必要的obj
            GameObject.Destroy(windowObj.transform.Find("inspector-group").gameObject);
            GameObject.Destroy(windowObj.transform.Find("folder-info-group").gameObject);
            GameObject.Destroy(windowObj.transform.Find("title-group").gameObject);

            titleText = windowObj.transform.Find("panel-bg/title-text").GetComponent<Text>();

            GameObject closeButtonObj = windowObj.transform.Find("panel-bg/x").gameObject;
            Button closeButton = closeButtonObj.GetComponent<Button>();
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => { CloseWindow(); });

            // 窗口大小切换按钮
            switchSizeButtonObj = GameObject.Instantiate(closeButtonObj, windowObj.transform.Find("panel-bg"));
            switchSizeButtonObj.name = "-";
            switchSizeButtonObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(-43, -13); // 原本的x是-13, -13
            switchSizeButtonObj.GetComponent<Image>().sprite = leftTriangleSprite;
            Button switchSizeButton = switchSizeButtonObj.GetComponent<Button>();
            switchSizeButton.onClick.RemoveAllListeners();
            switchSizeButton.onClick.AddListener(() => { SwitchWindowSize(); });

            viewGroupObj = windowObj.transform.Find("view-group").gameObject;

            // 将主要的浏览区 设置为可滚动，自动适配高度
            mainCanvasObj = windowObj.transform.Find("view-group/Scroll View/Viewport/Content").gameObject;
            contentTrans = mainCanvasObj.transform;
            GridLayoutGroup gridLayoutGroup = mainCanvasObj.AddComponent<GridLayoutGroup>();
            ContentSizeFitter contentSizeFitter = mainCanvasObj.AddComponent<ContentSizeFitter>();
            gridLayoutGroup.cellSize = new Vector2(cellWidth, cellHeight);
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentScrollRect = windowObj.transform.Find("view-group/Scroll View").GetComponent<ScrollRect>();
            windowObj.transform.Find("view-group/Scroll View/Viewport").GetComponent<RectTransform>().sizeDelta = new Vector2(0, -cellDistance / 2);
            Vector3 oriLocalPosition = windowObj.transform.Find("view-group/Scroll View/Viewport").transform.localPosition;
            windowObj.transform.Find("view-group/Scroll View/Viewport").transform.localPosition = new Vector3(oriLocalPosition.x, oriLocalPosition.y - cellDistance / 2, oriLocalPosition.z);


            // 将viewGroup复制一份到右侧面板
            Transform sidePanel = windowObj.transform.Find("inspector-group-bg");
            sidePanel.GetComponent<RectTransform>().sizeDelta = new Vector2(416, 245); // 背景图片大小变成上半部分的背景 尽管还是掌握着整个右半部分的obj
            GameObject sideViewGroupObj = GameObject.Instantiate(viewGroupObj, sidePanel);
            sideViewGroupObj.GetComponent<RectTransform>().sizeDelta = new Vector2(416, 407);
            sideViewGroupObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(0, -250, 0);
            GameObject sideContentObj = sideViewGroupObj.transform.Find("Scroll View/Viewport/Content").gameObject;
            sideContentTrans = sideContentObj.transform;
            sideContentObj.GetComponent<GridLayoutGroup>().cellSize = new Vector2(sideCellWidth, sideCellHeight);

            windowObj.SetActive(true);

            titleText.text = "量化计算器".Translate(); // 一定要在设置active之后进行

            Transform panelParent = windowObj.transform.Find("panel-bg");

           


            // 目标产物图标
            GameObject oriItemIconObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Station Window/storage-box-0/storage-icon-empty");

            if (oriItemIconObj != null)
            {
                targetProductIconObj = GameObject.Instantiate(oriItemIconObj);
                targetProductIconObj.name = "target-product";
                targetProductIconObj.transform.SetParent(panelParent);
                targetProductIconObj.transform.localScale = Vector3.one;
                RectTransform rect = targetProductIconObj.GetComponent<RectTransform>();
                rect.anchorMax = new Vector2(0, 1);
                rect.anchorMin = new Vector2(0, 1);
                rect.anchoredPosition3D = new Vector3(52, -50, 0);
                rect.sizeDelta = new Vector2(54, 54); // 原本是64
                targetProductIconObj.transform.Find("white").GetComponent<RectTransform>().sizeDelta = new Vector2(40, 40); // 原本是54

                targetProductIconObj.GetComponent<UIButton>().transitions[0].normalColor = itemIconNormalColor; // 原本的颜色较暗，大约在0.5，高亮0.66
                targetProductIconObj.GetComponent<UIButton>().transitions[0].mouseoverColor = itemIconHighlightColor;

                targetProductIcon = targetProductIconObj.transform.Find("white").GetComponent<Image>();
                targetProductIconObj.GetComponent<Button>().onClick.RemoveAllListeners();
                targetProductIconObj.GetComponent<Button>().onClick.AddListener(() => { OnTargetProductIconClick(); });
                //targetProductIcon.sprite = Resources.Load<Sprite>("ui/textures/sprites/icons/explore-icon"); // 是个放大镜图标
                targetProductIcon.sprite = Resources.Load<Sprite>("ui/textures/sprites/icons/controlpanel-icon-40"); // 是循环箭头环绕的齿轮，去掉40则是64*64大小的相同图标

                targetProductIconUIBtn = targetProductIconObj.GetComponent<UIButton>();
            }
            else
            {
                Debug.LogError("Error when init UICalcWindow.");
            }

            // 目标速度输入的文本框
            GameObject oriInputFieldObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Blueprint Browser/inspector-group/Scroll View/Viewport/Content/group-1/input-short-text");
            if (oriInputFieldObj == null)
                oriInputFieldObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Blueprint Browser/inspector-group/BP-panel-scroll(Clone)/Viewport/pane/group-1/input-short-text");
            if (oriInputFieldObj == null)
                Debug.LogError("Error when init oriInputField because some other mods has changed the Blueprint Browser UI. Please check if you've install the BluePrintTweaks and then contant jinxOAO.");
            speedInputObj = GameObject.Instantiate(oriInputFieldObj, panelParent);
            speedInputObj.name = "speed-input";
            speedInputObj.transform.localPosition = new Vector3(120, 0, 0);
            speedInputObj.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 30);
            speedInputObj.GetComponent<RectTransform>().pivot = new Vector2(0, 0.5f);
            speedInputObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(100, -50, 0); 
            speedInputObj.GetComponent<InputField>().text = "3600";
            speedInputObj.GetComponent<InputField>().contentType = InputField.ContentType.DecimalNumber;
            speedInputObj.GetComponent<InputField>().characterLimit = 12;
            speedInputObj.GetComponent<InputField>().transition = Selectable.Transition.None; // 要不然鼠标不在上面时颜色会很浅，刚打开容易找不到，不够明显
            speedInputObj.GetComponent<InputField>().onEndEdit.RemoveAllListeners();
            speedInputObj.GetComponent<InputField>().onEndEdit.AddListener((x) => OnTargetSpeedChange(x));
            speedInputObj.GetComponent<Image>().color = new Color(0, 0, 0, 0.5f);
            speedInputObj.transform.Find("value-text").GetComponent<Text>().color = Color.white;
            speedInputObj.transform.Find("value-text").GetComponent<Text>().fontSize = 16;
            speedInputObj.GetComponent<UIButton>().tips.tipTitle = "";
            speedInputObj.GetComponent<UIButton>().tips.tipText = "";
            speedInputObj.SetActive(false);
            speedInputObj.SetActive(true); // 这样切一次颜色才能显示正常

            perMinTextObj = GameObject.Instantiate(TextObj, panelParent);
            perMinTextObj.name = "per-minute";
            perMinTextObj.GetComponent<RectTransform>().anchorMax = new Vector2(0, 1);
            perMinTextObj.GetComponent<RectTransform>().anchorMin = new Vector2(0, 1);
            perMinTextObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(210, -50, 0);
            perMinTextObj.GetComponent<Text>().text = "/min";
        }


        public static void TryInitStaticPrefabs()
        {
            // 初始化一些Prefab
            if (recipeObj == null)
            {
                UIItemTip uiItemTipPrefabClone = GameObject.Instantiate<UIItemTip>(Configs.builtin.uiItemTipPrefab);
                recipeObj = uiItemTipPrefabClone.gameObject.transform.Find("recipe").gameObject;
                arrowObj = recipeObj.transform.Find("arrow").gameObject;
                iconObj_NoTip = recipeObj.transform.Find("icon").gameObject;

                // 手动创建一个带有UIButton的icon---------------------------------------------------------------------
                iconButtonObj = GameObject.Instantiate(iconObj_NoTip);
                iconButtonObj.GetComponent<Image>().raycastTarget = true; // 一定要设置这个，否则无法互动或者显示tip
                iconButtonObj.transform.Find("count").gameObject.SetActive(false); // 隐藏右下角小数字
                UIButton iconUIBtn = iconButtonObj.AddComponent<UIButton>();
                Button iconBtn = iconButtonObj.AddComponent<Button>();
                iconUIBtn.button = iconBtn;
                iconUIBtn.audios = new UIButton.AudioSettings();
                iconUIBtn.audios.enterName = "ui-hover-0";
                iconUIBtn.audios.downName = "ui-click-0";
                iconUIBtn.audios.upName = "";
                iconUIBtn.transitions = new UIButton.Transition[1];
                UIButton.Transition transition = new UIButton.Transition();
                iconUIBtn.transitions[0] = transition;

                transition.target = iconButtonObj.GetComponent<Image>();
                transition.damp = 0.3f;
                transition.mouseoverSize = 1f;
                transition.pressedSize = 1f;
                transition.normalColor = itemIconNormalColor;
                transition.mouseoverColor = itemIconHighlightColor;
                transition.pressedColor = itemIconPressedColor;
                transition.disabledColor = itemIconDisabledColor;
                // alphaonly 属性和 highligh相关属性暂时不需要设置
                // 下面设置anchor和pivot方便后面处理位置
                RectTransform rect = iconButtonObj.GetComponent<RectTransform>();
                rect.anchorMax = new Vector2(0, 0.5f);
                rect.anchorMin = new Vector2(0, 0.5f);
                rect.pivot = new Vector2(0, 0.5f);
                // ---------------------------------------------------------------------------------------------------

                TextObj = GameObject.Instantiate(GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Assembler Window/state/state-text"));
                TextObj.GetComponent<Text>().text = "";
                TextObj.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
                TextObj.GetComponent<Text>().color = TextWhiteColor;
                TextObj.GetComponent<Text>().lineSpacing = 0.7f; // 行间距更密集一些

                TextWithUITip = GameObject.Instantiate(TextObj);
                UIButton textUIBtn = TextWithUITip.AddComponent<UIButton>();
                Button textBtn = TextWithUITip.AddComponent<Button>();
                textBtn.transition = Selectable.Transition.None;
                textUIBtn.button = textBtn;
                textUIBtn.transitions = new UIButton.Transition[0];
                //textUIBtn.transitions[0] = new UIButton.Transition();


                // 初始化按钮 -------------
                GameObject addNewLayerButton = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel/hierarchy/layers/buttons-group/buttons/add-button");
                textButtonObj = GameObject.Instantiate(addNewLayerButton);
                textButtonObj.name = "button";
                textButtonObj.transform.localScale = Vector3.one;
                textButtonObj.GetComponent<Button>().onClick.RemoveAllListeners();
                textButtonObj.GetComponent<RectTransform>().anchorMax = new Vector2(0, 1);
                textButtonObj.GetComponent<RectTransform>().anchorMin = new Vector2(0, 1);
                // 文本obj是其子对象"Text"

                GameObject oriMode1ButtonObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Power Generator Window/ray-receiver/switch-button-1");
                imageButtonObj = GameObject.Instantiate(oriMode1ButtonObj);
                imageButtonObj.name = "button-img";
                imageButtonObj.GetComponent<Button>().onClick.RemoveAllListeners();
                imageButtonObj.GetComponent<RectTransform>().anchorMax = new Vector2(0, 1);
                imageButtonObj.GetComponent<RectTransform>().anchorMin = new Vector2(0, 1);
                GameObject.DestroyImmediate(imageButtonObj.GetComponent<Button>());
                imageButtonObj.AddComponent<Button>();
                GameObject.DestroyImmediate(imageButtonObj.transform.Find("button-text").gameObject);
                imageButtonObj.GetComponent<RectTransform>().sizeDelta = new Vector2(32, 32);
                imageButtonObj.GetComponent<UIButton>().tips.tipTitle = "";
                imageButtonObj.GetComponent<UIButton>().tips.tipText = "";
                GameObject icon = new GameObject();
                icon.name = "icon"; // 图标子对象
                icon.transform.SetParent(imageButtonObj.transform, false);
                Image img = icon.AddComponent<Image>();
                img.sprite = LDB.items.Select(1101)._iconSprite;
                icon.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
                icon.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
                icon.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
                icon.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(0,0,0);
                icon.GetComponent<RectTransform>().sizeDelta = new Vector2(30, 30);
                icon.transform.localScale = Vector3.one;

                // 增产切换按钮
                incToggleObj = GameObject.Instantiate(GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Assembler Window/produce/inc-info"));
                incToggleObj.transform.Find("inc-switch").GetComponent<Button>().onClick.RemoveAllListeners();
                GameObject.DestroyImmediate(incToggleObj.transform.Find("inc-label").gameObject);
                GameObject.DestroyImmediate(incToggleObj.transform.Find("inc-effect-value-text").gameObject);
                GameObject.DestroyImmediate(incToggleObj.transform.Find("inc-effect-value-text-2").gameObject);
                GameObject.DestroyImmediate(incToggleObj.transform.Find("inc-effect-type-text-2").gameObject);

                leftTriangleSprite = Resources.Load<Sprite>("ui/textures/sprites/test/last-icon");
                rightTriangleSprite = Resources.Load<Sprite>("ui/textures/sprites/test/next-icon");
            }
        }

        public void OnUpdate()
        {
            if (windowObj.transform.parent.GetChild(windowObj.transform.parent.childCount - 1) == windowObj.transform && windowObj.activeSelf)
            {
                isTopAndActive = true;
            }
            else
            {
                isTopAndActive = false;
            }

            float targetWindowWidth = largeWindowWidth;
            if(!isLargeWindow)
            {
                targetWindowWidth = smallWindowWidth;
            }

            float oriWidth = windowObj.GetComponent<RectTransform>().sizeDelta.x;
            float curWidth = oriWidth;
            if (curWidth < targetWindowWidth && targetWindowWidth - curWidth > 0.01f)
            {
                float height = windowObj.GetComponent<RectTransform>().sizeDelta.y;
                float viewGroupHeight = viewGroupObj.GetComponent<RectTransform>().sizeDelta.y;
                curWidth += animationSpeed;
                if(curWidth > targetWindowWidth)
                    curWidth = targetWindowWidth;
                float viewGroupWidth = curWidth - (smallWindowWidth - smallWindowViewGroupWidth); // 保持与大窗口的边距
                if (viewGroupWidth > largeWindowViewGroupWidth) // 但是不能超过原始最大大小
                    viewGroupWidth = largeWindowViewGroupWidth;
                windowObj.GetComponent<RectTransform>().sizeDelta = new Vector2(curWidth, height);
                viewGroupObj.GetComponent<RectTransform>().sizeDelta = new Vector2(viewGroupWidth, viewGroupHeight);

                // 顶部选择配方的也需要在小窗口状态下稍微低一点，防止和标题太近
                float fixedPosY = (curWidth - smallWindowWidth) / (largeWindowWidth - smallWindowWidth) * 10 + -60;
                float oriX = targetProductIconObj.GetComponent<RectTransform>().anchoredPosition.x;
                targetProductIconObj.GetComponent<RectTransform>().anchoredPosition = new Vector2 (oriX, fixedPosY);
                oriX = speedInputObj.GetComponent<RectTransform>().anchoredPosition.x;
                speedInputObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(oriX, fixedPosY);
                oriX = perMinTextObj.GetComponent<RectTransform>().anchoredPosition.x;
                perMinTextObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(oriX, fixedPosY);
            }
            else if(curWidth > targetWindowWidth && curWidth - targetWindowWidth > 0.01f)
            {
                float height = windowObj.GetComponent<RectTransform>().sizeDelta.y;
                float viewGroupHeight = viewGroupObj.GetComponent<RectTransform>().sizeDelta.y;
                curWidth -= animationSpeed;
                if (curWidth < targetWindowWidth)
                    curWidth = targetWindowWidth;
                float viewGroupWidth = curWidth - (smallWindowWidth - smallWindowViewGroupWidth); // 保持与大窗口的边距
                if (viewGroupWidth > largeWindowViewGroupWidth) // 但是不能超过原始最大大小
                    viewGroupWidth = largeWindowViewGroupWidth;
                windowObj.GetComponent<RectTransform>().sizeDelta = new Vector2(curWidth, height);
                viewGroupObj.GetComponent<RectTransform>().sizeDelta = new Vector2(viewGroupWidth, viewGroupHeight);

                // 顶部选择配方的也需要在小窗口状态下稍微低一点，防止和标题太近
                float fixedPosY = (curWidth - smallWindowWidth) / (largeWindowWidth - smallWindowWidth) * 10 + -60;
                float oriX = targetProductIconObj.GetComponent<RectTransform>().anchoredPosition.x;
                targetProductIconObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(oriX, fixedPosY);
                oriX = speedInputObj.GetComponent<RectTransform>().anchoredPosition.x;
                speedInputObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(oriX, fixedPosY);
                oriX = perMinTextObj.GetComponent<RectTransform>().anchoredPosition.x;
                perMinTextObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(oriX, fixedPosY);
            }
            if (oriWidth >= 0.5f * largeWindowWidth && curWidth < 0.5f * largeWindowWidth )
            {
                switchSizeButtonObj.GetComponent<Image>().sprite = rightTriangleSprite;
            }
            else if (oriWidth <= 0.5f * largeWindowWidth && curWidth > 0.5f * largeWindowWidth)
            {
                switchSizeButtonObj.GetComponent<Image>().sprite = leftTriangleSprite;
            }
            if (nextFrameRecalc)
            {
                nextFrameRecalc = false;
                solution.ReSolve();
                RefreshAll();
            }

            if (!isTopAndActive) return;
        }


        public void AddCellTest()
        {
            GameObject obj = new GameObject();
            Image img = obj.AddComponent<Image>();
            img.sprite = Resources.Load<Sprite>("Assets/DSPBattle/r3-5");
            obj.GetComponent<RectTransform>().sizeDelta = new Vector2(cellWidth, cellHeight);

            obj.transform.SetParent(mainCanvasObj.transform, false);
            obj.SetActive(false);
            obj.SetActive(true);
        }

        // 将窗口关闭时，永远会保留一个最后关闭的窗口
        public void CloseWindow()
        {
            // 如果存在其他的最后关闭的窗口，将其销毁
            if(WindowsManager.lastClosedWindow != null)
            {
                if (WindowsManager.windows != null)
                {
                    WindowsManager.windows.Remove(WindowsManager.lastClosedWindow);
                }
                GameObject.Destroy(WindowsManager.lastClosedWindow.windowObj);
            }

            isTopAndActive = false;
            windowObj.transform.SetAsFirstSibling(); // 将其不再占用最top的UI，防止其占用其他窗口的isTopAndActive
            windowObj.SetActive(false);
            WindowsManager.lastClosedWindow = this;
        }

        public void SwitchWindowSize()
        {
            isLargeWindow = !isLargeWindow;
        }

        public void OpenWindow()
        {
            if (WindowsManager.lastClosedWindow == this)
                WindowsManager.lastClosedWindow = null;

            windowObj.transform.SetAsLastSibling();
            windowObj.SetActive(true);
            titleText.text = "量化计算器".Translate();
        }

        public void OnTargetProductIconClick()
        {
            UIItemPicker.Popup(windowObj.GetComponent<RectTransform>().anchoredPosition + new Vector2(300f, -200f), OnTargetProductChange);
        }

        public void OnTargetProductChange(ItemProto item)
        {
            if (item != null)
            {
                targetProductIcon.sprite = item.iconSprite;
                targetProductIconUIBtn.tips.corner = 3;
                targetProductIconUIBtn.tips.itemId = item.ID;
                targetProductIconUIBtn.tips.delay = 0.1f;
                solution.SetTargetItemAndBeginSolve(item.ID);
                RefreshAll();
            }
        }

        public void OnTargetSpeedChange(string num)
        {
            try
            {
                double newTargetSpeed = Convert.ToDouble(num);
                solution.ChangeTargetSpeedAndSolve(newTargetSpeed);
                RefreshAll();
            }
            catch (Exception)
            {

            }
        }

        public void RefreshAll()
        {
            RefreshProductContent();
            RefreshResourceNeedAndByProductContent();
        }

        /// <summary>
        /// 刷新主界面的所有产物
        /// </summary>
        public void RefreshProductContent()
        {
            // 清理已存在的元素
            for (int i = 0; i < uiItemNodes.Count; i++)
            {
                GameObject.DestroyImmediate(uiItemNodes[i].obj);
            }
            uiItemNodes.Clear();

            if (solution.targetItem > 0 && solution.root != null)
            {
                // 下面创建UI子节点
                List<ItemNode> stack = new List<ItemNode>();
                stack.Add(solution.root);
                Dictionary<int, ItemNode> visitedNodes = new Dictionary<int, ItemNode>();
                while (stack.Count > 0)
                {
                    ItemNode oriNode = stack[stack.Count - 1];
                    ItemNode curNode = solution.itemNodes[oriNode.itemId];
                    if (!visitedNodes.ContainsKey(curNode.itemId) && curNode.needSpeed > 0.001f)
                    {
                        visitedNodes.Add(curNode.itemId, curNode);
                        if (!curNode.IsOre(solution.userPreference))
                        {
                            // 将不认为是原矿的节点输出
                            UIItemNode uiNode = new UIItemNode(curNode, this);
                            uiItemNodes.Add(uiNode);
                        }
                    }
                    stack.RemoveAt(stack.Count - 1);


                    for (int i = 0; i < curNode.children.Count; i++)
                    {
                        stack.Add(curNode.children[i]);
                    }
                }
            }
        }

        public void RefreshResourceNeedAndByProductContent()
        {
            for (int i = 0; i < uiSideItemNodes.Count; i++)
            {
                GameObject.DestroyImmediate(uiSideItemNodes[i].obj);
            }
            uiSideItemNodes.Clear();

            // 首先显示原矿需求
            UIItemNodeSimple label1 = new UIItemNodeSimple("原矿需求".Translate(), this);
            uiSideItemNodes.Add(label1);
            for (int i = 1; i < sideCellCountPerRow; i++)
            {
                UIItemNodeSimple empty1 = new UIItemNodeSimple("", this); // 是因为一行有多个元素，所以需要空来占位
                uiSideItemNodes.Add(empty1);
            }
            int count = 0;
            foreach (var node in solution.itemNodes)
            {
                if(node.Value.IsOre(solution.userPreference) && node.Value.needSpeed > 0.001f)
                {
                    UIItemNodeSimple uiResourceNode = new UIItemNodeSimple(node.Value, true, this);
                    uiSideItemNodes.Add(uiResourceNode);
                    count++;
                }
            }
            if(count % sideCellCountPerRow != 0) // 不足一行的用空填满
            {
                int unfilled = sideCellCountPerRow - count % sideCellCountPerRow;
                for (int i = 0; i < unfilled; i++)
                {
                    UIItemNodeSimple emptyEnd = new UIItemNodeSimple("", this);
                    uiSideItemNodes.Add(emptyEnd);
                }
            }
            // 然后显示副产物
            UIItemNodeSimple label2 = new UIItemNodeSimple("副产物和溢出产物".Translate(), this);
            uiSideItemNodes.Add(label2);
            for (int i = 1; i < sideCellCountPerRow; i++)
            {
                UIItemNodeSimple empty1 = new UIItemNodeSimple("", this); // 是因为一行有多个元素，所以需要空来占位
                uiSideItemNodes.Add(empty1);
            }
            count = 0;
            foreach (var node in solution.itemNodes)
            {
                if (!node.Value.IsOre(solution.userPreference) && (node.Value.satisfiedSpeed - node.Value.needSpeed > 0.001f))
                {
                    UIItemNodeSimple uiOverflowNode = new UIItemNodeSimple(node.Value, false, this);
                    uiSideItemNodes.Add(uiOverflowNode);
                    count++;
                }
            }
            if(count == 0) // 没有副产物，则移除副产物的标签
            {
                int lastIndex = uiSideItemNodes.Count - 1;
                for (int i = 0; i < sideCellCountPerRow; i++)
                {
                    GameObject.DestroyImmediate(uiSideItemNodes[lastIndex - i].obj);
                    uiSideItemNodes.RemoveAt(lastIndex - i);
                }
            }
            else if (count % sideCellCountPerRow != 0) // 不足一行的用空填满
            {
                int unfilled = sideCellCountPerRow - count % sideCellCountPerRow;
                for (int i = 0; i < unfilled; i++)
                {
                    UIItemNodeSimple emptyEnd = new UIItemNodeSimple("", this); 
                    uiSideItemNodes.Add(emptyEnd);
                }
            }
        }
    }
}
