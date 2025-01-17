using DSPCalculator.Compatibility;
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
        public const float sidePanelWidth = 400;
        public static float targetIconAnchoredPosX = 52;
        public static float targetIconAnchoredPosYLargeWindow = -80;
        public static float targetIconAnchoredPosYSmallWindow = -60;
        public const int assemblerDemandCountPerRow = 4;
        public const int TYPE_FILTER = 100000;
        public static Color itemIconNormalColor = new Color(0.6f, 0.6f, 0.6f, 1);
        public static Color itemIconHighlightColor = new Color(0.7f, 0.7f, 0.7f, 1);
        public static Color itemIconPressedColor = new Color(0.6f, 0.6f, 0.6f, 1);
        public static Color itemIconDisabledColor = new Color(0.5f, 0.5f, 0.5f, 1);
        public static Color TextWhiteColor = new Color(0.588f, 0.588f, 0.588f, 1f);
        public static Color TextWarningColor = new Color(0.852f, 0.487f, 0.022f, 1f);
        public static Color TextBlueColor = new Color(0.282f, 0.845f, 1f, 0.705f);
        public static Color incModeImageColor = new Color(0.287f, 0.824f, 1, 0.266f);
        public static Color accModeImageColor = new Color(0.9906f, 0.5897f, 0.3691f, 0.384f);
        public static Color incModeTextColor = new Color(0.282f, 0.845f, 1, 0.705f);
        public static Color accModeTextColor = new Color(0.9906f, 0.5897f, 0.3691f, 0.705f);
        public static float largeWindowWidth = 1340;
        public static float smallWindowWidth = 315;
        public static float largeWindowViewGroupWidth = 890;
        public static float smallWindowViewGroupWidth = 290;
        public static float largeWindowViewGroupHeight = 570;
        public static float smallWindowViewGroupHeight = 610;
        public static float animationSpeed = 150;
        // "ui/textures/sprites/icons/eraser-icon" 橡皮擦
        // "ui/textures/sprites/icons/minus-32" 粗短横线

        public bool isTopAndActive;
        public bool nextFrameRecalc;

        // 一些全局prefab
        public static GameObject recipeObj;
        public static GameObject arrowObj;
        public static GameObject iconObj_NoTip; // 无UITip的图标
        public static GameObject iconObj_ButtonTip; // 有UITip且有Button的图标
        public static GameObject TextObj; // 文本
        public static GameObject TextWithUITip; // 具有UITip的文本
        public static GameObject textButtonObj; // 有文字的按钮
        public static GameObject imageButtonObj; // 只有图片的按钮
        public static GameObject incTogglePrefabObj; // 增产切换按钮
        public static GameObject checkBoxObj;
        public static Sprite leftTriangleSprite;
        public static Sprite rightTriangleSprite;
        public static Sprite backgroundSprite = null;
        public static Sprite buttonBackgroundSprite = null;
        public static Sprite gearSprite = null;
        public static Sprite filterSprite = null;
        public static Sprite biaxialArrowSprite = null;
        public static Sprite oreSprite = null;
        public static Sprite crossSprite = null;
        public static Sprite bannedSprite = null;
        public static Sprite todoListSprite = null;
        public static Sprite resetSprite = null;
        public static Sprite checkboxOnSprite = null;
        public static Sprite checkboxOffSprite = null;

        // UI元素
        public GameObject windowObj;
        public GameObject viewGroupObj;
        public GameObject mainCanvasObj;
        public GameObject switchSizeButtonObj;
        public GameObject customTitleInputObj;
        public Image titleInputBG;
        public InputField titleInput;
        public UIButton titleInputUIBtn;
        public Text titleText;
        public GameObject targetProductTextObj;
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
        public GameObject incToggleObj; // 全局增产切换按钮
        public Text incText;
        public GameObject proliferatorSelectionObj; // 全局增产剂选择按钮
        public GameObject assemblerSelectionObj; // 全局工厂选择按钮
        public Dictionary<int, UIButton> proliferatorUsedButtons; // 增产剂选择按钮列表
        public Dictionary<int, Dictionary<int, UIButton>> assemblerUsedButtons; // 工厂选择按钮列表
        public Text finalInfoText;
        public Text assemblerDemandsTitleText;
        public GameObject assemblersDemandsGroupObj; // 显示所有工厂数量的group
        public List<GameObject> assemblersDemandObjs; // 所有工厂需求数量的obj列表

        public Image cbBluebuff;
        public Image cbEnergyBurst;
        public Image cbDirac;
        public Image cbInferior;
        public Image cbIncMilli;
        public Image cbAccMilli;
        public Image cbRoundUp;
        public Image cbMixbelt;
        public Image cbSolveProlifer;
        public Text txtBluebuff;
        public Text txtEnergyBurst;
        public Text txtDirac;
        public Text txtInferior;
        public Text txtIncMilli;
        public Text txtAccMilli;
        public Text txtRoundUp;
        public Text txtMixbelt;
        public Text txtSolveProlifer;
        public InputField incInput;
        public InputField accInput;

        public Dictionary<int, int> uiItemNodeOrders;
        public Dictionary<int, UIItemNodeSimple> uiItemSimplesByItemId;

        public SolutionTree solution; // 该窗口对应的量化计算路径

        public bool isLargeWindow;
        public float targetVerticalPosition; // 主内容ScrollRect需要移动到的位置

        /// <summary>
        /// 创建新窗口
        /// </summary>
        public UICalcWindow(int idx)
        {
            TryInitStaticPrefabs();

            solution = new SolutionTree();
            uiItemNodes = new List<UIItemNode>();
            uiSideItemNodes = new List<UIItemNodeSimple>();
            proliferatorUsedButtons = new Dictionary<int, UIButton>();
            assemblerUsedButtons = new Dictionary<int, Dictionary<int, UIButton>>();
            assemblersDemandObjs = new List<GameObject>();
            uiItemNodeOrders = new Dictionary<int, int>();
            uiItemSimplesByItemId = new Dictionary<int, UIItemNodeSimple>();
            isTopAndActive = true;
            isLargeWindow = true;
            nextFrameRecalc = false;
            targetVerticalPosition = -1;

            GameObject oriWindowObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Blueprint Browser");
            GameObject parentWindowObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows"); // (-----/Calc Window Group");

            windowObj = GameObject.Instantiate(oriWindowObj);
            windowObj.name = "calc-window" + " " + idx.ToString();
            windowObj.transform.SetParent(parentWindowObj.transform);
            windowObj.GetComponent<RectTransform>().pivot = new Vector2(0, 1); // 这是为了能让其在缩放时，以左上角为锚点，而不是中心
            windowObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(-670, 357,0);
            windowObj.transform.localScale = Vector3.one;
            windowObj.AddComponent<RectMask2D>();
            Vector3 centerPos = windowObj.transform.localPosition;
            int range = 40;
            float randX = UnityEngine.Random.Range(centerPos.x - range, centerPos.x + range);
            float randY = UnityEngine.Random.Range(centerPos.y - range, centerPos.y + range);
            windowObj.transform.localPosition = new Vector3(randX, randY, centerPos.z);

            // 移除不必要的obj
            GameObject.Destroy(windowObj.transform.Find("inspector-group").gameObject);
            GameObject.Destroy(windowObj.transform.Find("folder-info-group").gameObject);
            GameObject.Destroy(windowObj.transform.Find("title-group").gameObject);

            // 移除所有子蓝图图标
            GameObject contentObj = windowObj.transform.Find("view-group/Scroll View/Viewport/Content").gameObject;
            while (contentObj.transform.childCount > 0)
            {
                GameObject.DestroyImmediate(contentObj.transform.GetChild(contentObj.transform.childCount - 1).gameObject);
            }
            GameObject windowTitleObj = windowObj.transform.Find("panel-bg/title-text").gameObject;
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
            switchSizeButtonObj.GetComponent<UIButton>().tips.tipTitle = "收起/展开".Translate() + UIHotkeySettingPatcher.GetFoldHotkeyString();
            switchSizeButtonObj.GetComponent<UIButton>().tips.corner = 3;
            switchSizeButtonObj.GetComponent<UIButton>().tips.delay = 0.3f;
            Button switchSizeButton = switchSizeButtonObj.GetComponent<Button>();
            switchSizeButton.onClick.RemoveAllListeners();
            switchSizeButton.onClick.AddListener(() => { SwitchWindowSize(); });

            viewGroupObj = windowObj.transform.Find("view-group").gameObject;
            // 下面调整至以左下角为基准，且调整一下大小
            viewGroupObj.GetComponent<RectTransform>().anchorMax = new Vector2(0, 0);
            viewGroupObj.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
            viewGroupObj.GetComponent<RectTransform>().pivot = new Vector2(0, 0);
            viewGroupObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(12, 12, 0);
            viewGroupObj.GetComponent<RectTransform>().sizeDelta = new Vector2(largeWindowViewGroupWidth, largeWindowViewGroupHeight);

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
            sideViewGroupObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(0, -412, 0);
            GameObject sideContentObj = sideViewGroupObj.transform.Find("Scroll View/Viewport/Content").gameObject;
            sideContentTrans = sideContentObj.transform;
            sideContentObj.GetComponent<GridLayoutGroup>().cellSize = new Vector2(sideCellWidth, sideCellHeight);

            windowObj.SetActive(true);

            titleText.text = "量化计算器".Translate(); // 一定要在设置active之后进行

            Transform panelParent = windowObj.transform.Find("panel-bg");


            // 可编辑标题
            GameObject oriInputFieldObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Blueprint Browser/inspector-group/Scroll View/Viewport/Content/group-1/input-short-text");
            if (oriInputFieldObj == null)
                oriInputFieldObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Blueprint Browser/inspector-group/BP-panel-scroll(Clone)/Viewport/pane/group-1/input-short-text");
            if (oriInputFieldObj == null)
                Debug.LogError("Error when init oriInputField because some other mods has changed the Blueprint Browser UI. Please check if you've install the BluePrintTweaks and then contant jinxOAO.");
            customTitleInputObj = GameObject.Instantiate(oriInputFieldObj, windowObj.transform);
            customTitleInputObj.name = "inputfield-title";
            customTitleInputObj.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 30);
            customTitleInputObj.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 1f);
            customTitleInputObj.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 1f);
            customTitleInputObj.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1f);
            customTitleInputObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(0, -10, 0);
            titleInputBG = customTitleInputObj.GetComponent<Image>();
            titleInput = customTitleInputObj.GetComponent<InputField>();
            titleInput.text = "量化计算器".Translate();
            titleInput.characterLimit = 30;
            titleInput.transition = Selectable.Transition.None; // 要不然鼠标不在上面时颜色会很浅，刚打开容易找不到，不够明显
            titleInput.onEndEdit.RemoveAllListeners();
            customTitleInputObj.GetComponent<Image>().color = new Color(0, 0, 0, 0.5f);
            customTitleInputObj.transform.Find("value-text").GetComponent<Text>().color = Color.white;
            customTitleInputObj.transform.Find("value-text").GetComponent<Text>().fontSize = 18;
            customTitleInputObj.transform.Find("value-text").GetComponent<Text>().font = titleText.font;
            customTitleInputObj.transform.Find("value-text").GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
            customTitleInputObj.GetComponent<UIButton>().tips.tipTitle = "";
            customTitleInputObj.GetComponent<UIButton>().tips.tipText = "";
            customTitleInputObj.SetActive(false);
            customTitleInputObj.SetActive(true);
            titleInputUIBtn = customTitleInputObj.GetComponent<UIButton>();
            UIButton.Transition transition = new UIButton.Transition();
            transition.target = customTitleInputObj.GetComponent<Image>();
            transition.normalColor = new Color(0, 0, 0, 0.01f); // 不能试0，否则会失效，好奇怪
            transition.mouseoverColor = new Color(0, 0, 0, 0.3f);
            transition.highlightColorOverride = new Color(0, 0, 0, 0.5f);
            transition.damp = 0.3f;
            transition.highlightColorMultiplier = 1;
            transition.highlightSizeMultiplier = 1;
            transition.mouseoverSize = 1;
            transition.pressedColor = new Color(0, 0, 0, 0.5f);
            transition.pressedSize = 1;
            titleInputUIBtn.transitions = new UIButton.Transition[] { transition };
            titleInputUIBtn.highlighted = false;

            if(DSPCalculatorPlugin.EditableTitle.Value)
            {
                windowTitleObj.SetActive(false);
            }
            else
            {
                customTitleInputObj.SetActive(false);
            }

            // 目标产物文本
            targetProductTextObj = GameObject.Instantiate(TextObj, panelParent);
            targetProductTextObj.name = "target-title";
            targetProductTextObj.transform.localScale = Vector3.one;
            targetProductTextObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(25, 318, 0);
            targetProductTextObj.GetComponent<Text>().text = "设置目标产物".Translate();
            targetProductTextObj.GetComponent<Text>().fontSize = 16;

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
                rect.anchoredPosition3D = new Vector3(targetIconAnchoredPosX, targetIconAnchoredPosYLargeWindow, 0);
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
            speedInputObj = GameObject.Instantiate(oriInputFieldObj, panelParent);
            speedInputObj.name = "speed-input";
            speedInputObj.transform.localPosition = new Vector3(120, 0, 0);
            speedInputObj.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 30);
            speedInputObj.GetComponent<RectTransform>().pivot = new Vector2(0, 0.5f);
            speedInputObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(100, targetIconAnchoredPosYLargeWindow, 0); 
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
            perMinTextObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(210, targetIconAnchoredPosYLargeWindow, 0);
            perMinTextObj.GetComponent<Text>().text = "/min";

            
            // 切换增产按钮
            incToggleObj = GameObject.Instantiate(incTogglePrefabObj, panelParent);
            incToggleObj.name = "inc-setting";
            incToggleObj.transform.localPosition = new Vector3(-38, 287, 0);
            incToggleObj.transform.Find("inc-switch").GetComponent<Button>().onClick.AddListener(() => { OnGlobalIncToggleClick(); });
            incText = incToggleObj.transform.Find("inc-effect-type-text").GetComponent<Text>();
            GameObject incToggleThumb = incToggleObj.transform.Find("inc-switch/switch-thumb").gameObject;
            bool isInc = solution.userPreference.globalIsInc;
            if (isInc)
            {
                incToggleThumb.GetComponent<RectTransform>().anchoredPosition = new Vector2(-10, 0);
                incText.text = "额外产出calc".Translate();
                incText.color = incModeTextColor;
                incToggleObj.transform.Find("inc-switch").GetComponent<Image>().color = incModeImageColor;
            }
            else
            {
                incToggleThumb.GetComponent<RectTransform>().anchoredPosition = new Vector2(10, 0);
                incText.text = "生产加速calc".Translate();
                incText.color = accModeTextColor;
                incToggleObj.transform.Find("inc-switch").GetComponent<Image>().color = accModeImageColor;
            }
            incToggleObj.SetActive(true);

            // 增产剂切换图标
            proliferatorSelectionObj = new GameObject();
            proliferatorSelectionObj.name = "proliferator-select";
            proliferatorSelectionObj.transform.SetParent(panelParent, false);
            proliferatorSelectionObj.transform.localPosition = new Vector3(82, 270, 0);
            //先创建一个禁用增产剂的图标
            int proliferatorItemId0 = 0;
            int incLevel0 = 0;
            GameObject pBtnObj0 = GameObject.Instantiate(UICalcWindow.imageButtonObj, proliferatorSelectionObj.transform);
            pBtnObj0.GetComponent<Image>().sprite = buttonBackgroundSprite;
            Image icon0 = pBtnObj0.transform.Find("icon").GetComponent<Image>();
            icon0.sprite = bannedSprite;
            icon0.color = Color.red; // 默认白色看不清
            pBtnObj0.transform.localPosition = new Vector3(0, 0);
            pBtnObj0.GetComponent<Button>().onClick.AddListener(() => { SetGlobalIncLevel(incLevel0); });
            // pBtnObj0.GetComponent<UIButton>().transitions[0].highlightColorOverride = iconButtonHighLightColor;
            proliferatorUsedButtons[incLevel0] = pBtnObj0.GetComponent<UIButton>();
            proliferatorUsedButtons[incLevel0].tips.itemId = proliferatorItemId0;

            for (int p = 0; p < CalcDB.proliferatorItemIds.Count; p++)
            {
                int proliferatorItemId = CalcDB.proliferatorItemIds[p];
                int incLevel = CalcDB.proliferatorAbilitiesMap[proliferatorItemId];
                GameObject pBtnObj = GameObject.Instantiate(imageButtonObj, proliferatorSelectionObj.transform);
                pBtnObj.GetComponent<Image>().sprite = buttonBackgroundSprite;
                Image icon = pBtnObj.transform.Find("icon").GetComponent<Image>();
                icon.sprite = LDB.items.Select(proliferatorItemId).iconSprite;
                pBtnObj.transform.localPosition = new Vector3((p + 1) * 35, 0);
                pBtnObj.GetComponent<Button>().onClick.AddListener(() => { SetGlobalIncLevel(incLevel); });
                // pBtnObj.GetComponent<UIButton>().transitions[0].highlightColorOverride = iconButtonHighLightColor;
                proliferatorUsedButtons[incLevel] = pBtnObj.GetComponent<UIButton>();
                proliferatorUsedButtons[incLevel].tips.itemId = proliferatorItemId;
            }

            // 工厂选择图标
            assemblerSelectionObj = new GameObject();
            assemblerSelectionObj.name = "assembler-select";
            assemblerSelectionObj.transform.SetParent(panelParent, false);
            float posXDelta = 0;
            float posYDelta = 0;
            if (CompatManager.GB)
            {
                posXDelta = -45;
                posYDelta = -10;
            }
            assemblerSelectionObj.transform.localPosition = new Vector3(-358 + posXDelta, 270 + posYDelta, 0);
            Dictionary <int, int> alreadyAddedAssembler= new Dictionary<int, int>(); // 用于储存已经加入过的工厂，就不再实例化按钮了
            int btnCount = 0; // 工厂数
            int typeCount = 0; // 类别数
            int rowCount = 0; // 行数
            foreach (var rType in CalcDB.assemblerListByType)
            {
                if(btnCount * 35 + typeCount * 10 + posXDelta > 9 * 35)
                {
                    rowCount++;
                    btnCount = 0;
                    typeCount = 0;
                }
                int typeInt = rType.Key;
                if(!assemblerUsedButtons.ContainsKey(typeInt))
                {
                    assemblerUsedButtons[typeInt] = new Dictionary<int, UIButton>();
                }
                List<AssemblerData> assemblerList = rType.Value;
                if (assemblerList.Count > 1) // 有可变选项才有提供按钮允许更换的意义
                {
                    for (int j = 0; j < assemblerList.Count; j++)
                    {
                        AssemblerData ad = assemblerList[j];
                        int assemblerItemId = ad.ID;
                        alreadyAddedAssembler[assemblerItemId] = 1;

                        GameObject aBtnObj = GameObject.Instantiate(UICalcWindow.imageButtonObj, assemblerSelectionObj.transform);
                        aBtnObj.GetComponent<Image>().sprite = UICalcWindow.buttonBackgroundSprite;
                        Image icon = aBtnObj.transform.Find("icon").GetComponent<Image>();
                        icon.sprite = ad.iconSprite;
                        aBtnObj.transform.localPosition = new Vector3((btnCount) * 35 + typeCount * 10, rowCount * 35);
                        aBtnObj.GetComponent<Button>().onClick.AddListener(() => { SetGlobalAssemblerPreference(typeInt * TYPE_FILTER + assemblerItemId); });
                        // aBtnObj.GetComponent<UIButton>().transitions[0].highlightColorOverride = iconButtonHighLightColor;
                        aBtnObj.GetComponent<UIButton>().highlighted = false; // 永远不会高亮
                        aBtnObj.GetComponent<UIButton>().tips.itemId = assemblerItemId;
                        aBtnObj.GetComponent<UIButton>().tips.corner = 3;
                        aBtnObj.GetComponent<UIButton>().tips.delay = 0.1f;
                        assemblerUsedButtons[typeInt][assemblerItemId] = aBtnObj.GetComponent<UIButton>();
                        btnCount ++;
                    }
                    typeCount++;
                }
            }

            // 右侧最终文本信息
            GameObject finalInfoTextObj = GameObject.Instantiate(TextObj, sidePanel);
            finalInfoTextObj.name = "final-info";
            finalInfoTextObj.transform.localPosition = new Vector3(15, -20, 0);
            finalInfoText = finalInfoTextObj.GetComponent<Text>();
            finalInfoText.fontSize = 16;
            finalInfoText.alignment = TextAnchor.UpperLeft;

            GameObject assemblerDemandsTitleObj = GameObject.Instantiate(TextObj, sidePanel);
            assemblerDemandsTitleObj.transform.localPosition = new Vector3(15, -82, 0);
            assemblerDemandsTitleText = assemblerDemandsTitleObj.GetComponent<Text>();
            assemblerDemandsTitleText.fontSize = 16;
            assemblerDemandsTitleText.alignment = TextAnchor.UpperLeft;
            assemblerDemandsTitleText.text= "工厂需求".Translate();

            // 右侧最终工厂信息
            assemblersDemandsGroupObj = new GameObject();
            assemblersDemandsGroupObj.name = "assember-demands";
            assemblersDemandsGroupObj.transform.SetParent(sidePanel);
            assemblersDemandsGroupObj.transform.localScale = Vector3.one;
            assemblersDemandsGroupObj.transform.localPosition = new Vector3(13, -110, 0);
            // 实际创建工厂信息由RefreshAssemblerDemands()完成

            // 还原所有配置按钮
            GameObject resetUserPreferenceButtonObj = GameObject.Instantiate(UICalcWindow.iconObj_ButtonTip);
            resetUserPreferenceButtonObj.name = "reset-all";
            resetUserPreferenceButtonObj.transform.SetParent(panelParent, false);
            resetUserPreferenceButtonObj.transform.localScale = Vector3.one;
            resetUserPreferenceButtonObj.GetComponent<RectTransform>().anchorMin = new Vector2(1, 1);
            resetUserPreferenceButtonObj.GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);
            resetUserPreferenceButtonObj.GetComponent<RectTransform>().pivot = new Vector2(1, 1);
            resetUserPreferenceButtonObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(-75, -15, 0);
            resetUserPreferenceButtonObj.GetComponent<RectTransform>().sizeDelta = new Vector2(18, 18);
            resetUserPreferenceButtonObj.GetComponent<Image>().sprite = UICalcWindow.resetSprite;
            resetUserPreferenceButtonObj.GetComponent<Button>().onClick.AddListener(() => { ClearAllUserPreference(); });
            resetUserPreferenceButtonObj.GetComponent<UIButton>().tips.tipTitle = "还原默认配置标题".Translate();
            resetUserPreferenceButtonObj.GetComponent<UIButton>().tips.tipText = "还原默认配置说明".Translate();
            resetUserPreferenceButtonObj.GetComponent<UIButton>().tips.corner = 3;
            resetUserPreferenceButtonObj.GetComponent<UIButton>().tips.width = 200;
            resetUserPreferenceButtonObj.GetComponent<UIButton>().transitions[0].normalColor = new Color(0.6f, 0, 0, 1);
            resetUserPreferenceButtonObj.GetComponent<UIButton>().transitions[0].pressedColor = new Color(0.6f, 0, 0, 1);
            resetUserPreferenceButtonObj.GetComponent<UIButton>().transitions[0].mouseoverColor = new Color(0.9f, 0.2f, 0.2f, 1);

            // 右侧可调整元驱动等配置信息                         
            GameObject checkBoxGroupObj = new GameObject();
            checkBoxGroupObj.name = "checkbox-group";
            checkBoxGroupObj.transform.SetParent(sidePanel);
            checkBoxGroupObj.transform.localScale = Vector3.one;
            checkBoxGroupObj.transform.localPosition = new Vector3(155, -20, 0); // ori -28

            if(CompatManager.TCFV)
            {
                GameObject bluebuffCbObj = GameObject.Instantiate(checkBoxObj, checkBoxGroupObj.transform);
                bluebuffCbObj.name = "checkbox-bluebuff";
                cbBluebuff = bluebuffCbObj.GetComponent<Image>();
                txtBluebuff = bluebuffCbObj.transform.Find("text").GetComponent<Text>();
                bluebuffCbObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(0, 0, 0);
                bluebuffCbObj.GetComponent<Button>().onClick.AddListener(OnBluebuffClick);

                GameObject energyBurstCbObj = GameObject.Instantiate(checkBoxObj, checkBoxGroupObj.transform);
                energyBurstCbObj.name = "checkbox-energyburst";
                cbEnergyBurst = energyBurstCbObj.GetComponent<Image>();
                txtEnergyBurst = energyBurstCbObj.transform.Find("text").GetComponent<Text>();
                energyBurstCbObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(0, -20, 0);
                energyBurstCbObj.GetComponent<Button>().onClick.AddListener(OnEnergyBurstClick);

                GameObject diracCbObj = GameObject.Instantiate(checkBoxObj, checkBoxGroupObj.transform);
                diracCbObj.name = "checkbox-dirac";
                cbDirac = diracCbObj.GetComponent<Image>();
                txtDirac = diracCbObj.transform.Find("text").GetComponent<Text>();
                diracCbObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(0, -40, 0);
                diracCbObj.GetComponent<Button>().onClick.AddListener(OnDiracClick);

                GameObject inferiorCbObj = GameObject.Instantiate(checkBoxObj, checkBoxGroupObj.transform);
                inferiorCbObj.name = "checkbox-inferior";
                cbInferior = inferiorCbObj.GetComponent<Image>();
                txtInferior = inferiorCbObj.transform.Find("text").GetComponent<Text>();
                //inferiorCbObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(110, 0, 0);
                inferiorCbObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(0, -60, 0);
                inferiorCbObj.GetComponent<Button>().onClick.AddListener(OnInferiorClick);
            }

            // 用户可以自定义增产效果，来覆盖游戏的增产效果
            GameObject customIncMilliCbObj = GameObject.Instantiate(checkBoxObj, checkBoxGroupObj.transform);
            customIncMilliCbObj.name = "checkbox-custom-inc";
            cbIncMilli = customIncMilliCbObj.GetComponent<Image>();
            txtIncMilli = customIncMilliCbObj.transform.Find("text").GetComponent<Text>();
            customIncMilliCbObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(110, 0, 0);
            customIncMilliCbObj.GetComponent<Button>().onClick.AddListener(OnCustomIncMilliClick);
            customIncMilliCbObj.GetComponent<UIButton>().tips.tipTitle = "强制增产效能标题".Translate();
            customIncMilliCbObj.GetComponent<UIButton>().tips.tipText = "强制增产效能说明".Translate();
            customIncMilliCbObj.GetComponent<UIButton>().tips.corner = 1;
            customIncMilliCbObj.GetComponent<UIButton>().tips.delay = 0.1f;
            customIncMilliCbObj.GetComponent<UIButton>().tips.width = 400;
            GameObject customIncInput = GameObject.Instantiate(oriInputFieldObj,checkBoxGroupObj.transform);
            customIncInput.name = "inputfield-inc";
            customIncInput.GetComponent<RectTransform>().sizeDelta = new Vector2(30, 20);
            customIncInput.GetComponent<RectTransform>().pivot = new Vector2(0, 0.5f);
            customIncInput.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(212, 0, 0);
            incInput = customIncInput.GetComponent<InputField>();
            customIncInput.GetComponent<InputField>().text = "25";
            customIncInput.GetComponent<InputField>().contentType = InputField.ContentType.IntegerNumber;
            customIncInput.GetComponent<InputField>().characterLimit = 3;
            customIncInput.GetComponent<InputField>().transition = Selectable.Transition.None; // 要不然鼠标不在上面时颜色会很浅，刚打开容易找不到，不够明显
            customIncInput.GetComponent<InputField>().onEndEdit.RemoveAllListeners();
            customIncInput.GetComponent<InputField>().onEndEdit.AddListener((x) => OnEndEditIncMilli(x));
            customIncInput.GetComponent<Image>().color = new Color(0, 0, 0, 0.5f);
            customIncInput.transform.Find("value-text").GetComponent<Text>().color = Color.white;
            customIncInput.transform.Find("value-text").GetComponent<Text>().fontSize = 12;
            customIncInput.GetComponent<UIButton>().tips.tipTitle = "";
            customIncInput.GetComponent<UIButton>().tips.tipText = "";
            customIncInput.SetActive(false);
            customIncInput.SetActive(true);

            GameObject customAccMilliCbObj = GameObject.Instantiate(checkBoxObj, checkBoxGroupObj.transform);
            customAccMilliCbObj.name = "checkbox-custom-acc";
            cbAccMilli = customAccMilliCbObj.GetComponent<Image>();
            txtAccMilli = customAccMilliCbObj.transform.Find("text").GetComponent<Text>();
            customAccMilliCbObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(110, -20, 0);
            customAccMilliCbObj.GetComponent<Button>().onClick.AddListener(OnCustomAccMilliClick);
            customAccMilliCbObj.GetComponent<UIButton>().tips.tipTitle = "强制加速效能标题".Translate();
            customAccMilliCbObj.GetComponent<UIButton>().tips.tipText = "强制加速效能说明".Translate();
            customAccMilliCbObj.GetComponent<UIButton>().tips.corner = 1;
            customAccMilliCbObj.GetComponent<UIButton>().tips.delay = 0.1f;
            customAccMilliCbObj.GetComponent<UIButton>().tips.width = 400;
            GameObject customAccInput = GameObject.Instantiate(oriInputFieldObj, checkBoxGroupObj.transform);
            customAccInput.name = "inputfield-acc";
            customAccInput.GetComponent<RectTransform>().sizeDelta = new Vector2(30, 20);
            customAccInput.GetComponent<RectTransform>().pivot = new Vector2(0, 0.5f);
            customAccInput.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(212, -20, 0);
            accInput = customAccInput.GetComponent<InputField>();
            customAccInput.GetComponent<InputField>().text = "100";
            customAccInput.GetComponent<InputField>().contentType = InputField.ContentType.IntegerNumber;
            customAccInput.GetComponent<InputField>().characterLimit = 3;
            customAccInput.GetComponent<InputField>().transition = Selectable.Transition.None; // 要不然鼠标不在上面时颜色会很浅，刚打开容易找不到，不够明显
            customAccInput.GetComponent<InputField>().onEndEdit.RemoveAllListeners();
            customAccInput.GetComponent<InputField>().onEndEdit.AddListener((x) => OnEndEditAccMilli(x));
            customAccInput.GetComponent<Image>().color = new Color(0, 0, 0, 0.5f);
            customAccInput.transform.Find("value-text").GetComponent<Text>().color = Color.white;
            customAccInput.transform.Find("value-text").GetComponent<Text>().fontSize = 12;
            customAccInput.GetComponent<UIButton>().tips.tipTitle = "";
            customAccInput.GetComponent<UIButton>().tips.tipText = "";
            customAccInput.SetActive(false);
            customAccInput.SetActive(true);


            GameObject roundUpCbObj = GameObject.Instantiate(checkBoxObj, checkBoxGroupObj.transform);
            roundUpCbObj.name = "checkbox-roundup";
            cbRoundUp = roundUpCbObj.GetComponent<Image>();
            txtRoundUp = roundUpCbObj.transform.Find("text").GetComponent<Text>();
            //inferiorCbObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(110, 0, 0);
            roundUpCbObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(110, -40, 0);
            roundUpCbObj.GetComponent<Button>().onClick.AddListener(OnAssemblerRoundUpSettingChange);



            GameObject solveProliferObj = GameObject.Instantiate(checkBoxObj, checkBoxGroupObj.transform);
            solveProliferObj.name = "checkbox-roundup";
            cbSolveProlifer = solveProliferObj.GetComponent<Image>();
            txtSolveProlifer = solveProliferObj.transform.Find("text").GetComponent<Text>();
            //inferiorCbObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(110, 0, 0);
            solveProliferObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(110, -60, 0);
            solveProliferObj.GetComponent<Button>().onClick.AddListener(OnSolveProliferatorSettingChange);
            solveProliferObj.GetComponent<UIButton>().tips.tipTitle = "增产剂并入产线".Translate();
            solveProliferObj.GetComponent<UIButton>().tips.tipText = "增产剂并入产线描述".Translate();
            solveProliferObj.GetComponent<UIButton>().tips.corner = 1;
            solveProliferObj.GetComponent<UIButton>().tips.delay = 0.1f;
            solveProliferObj.GetComponent<UIButton>().tips.width = 300;

            GameObject showHideMixBeltInfoObj = GameObject.Instantiate(checkBoxObj, checkBoxGroupObj.transform);
            showHideMixBeltInfoObj.name = "checkbox-mixbelt";
            cbMixbelt = showHideMixBeltInfoObj.GetComponent<Image>();
            txtMixbelt = showHideMixBeltInfoObj.transform.Find("text").GetComponent<Text>();
            showHideMixBeltInfoObj.GetComponent<UIButton>().tips.tipTitle = "混带显示标题".Translate();
            showHideMixBeltInfoObj.GetComponent<UIButton>().tips.tipText = "混带显示说明".Translate();
            showHideMixBeltInfoObj.GetComponent<UIButton>().tips.corner = 1;
            showHideMixBeltInfoObj.GetComponent<UIButton>().tips.delay = 0.1f;
            showHideMixBeltInfoObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(110, -60, 0);
            showHideMixBeltInfoObj.GetComponent<Button>().onClick.AddListener(OnMixbeltInfoCbClick);
            showHideMixBeltInfoObj.SetActive(DSPCalculatorPlugin.showMixBeltCheckbox);

            RefreshFinalInfoText();
            RefreshCheckBoxes();
            RefreshAssemblerButtonDisplay();
            RefreshProliferatorButtonDisplay();
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
                iconObj_ButtonTip = GameObject.Instantiate(iconObj_NoTip);
                iconObj_ButtonTip.GetComponent<Image>().raycastTarget = true; // 一定要设置这个，否则无法互动或者显示tip
                iconObj_ButtonTip.transform.Find("count").gameObject.SetActive(false); // 隐藏右下角小数字
                UIButton iconUIBtn = iconObj_ButtonTip.AddComponent<UIButton>();
                Button iconBtn = iconObj_ButtonTip.AddComponent<Button>();
                iconUIBtn.button = iconBtn;
                iconUIBtn.audios = new UIButton.AudioSettings();
                iconUIBtn.audios.enterName = "ui-hover-0";
                iconUIBtn.audios.downName = "ui-click-0";
                iconUIBtn.audios.upName = "";
                iconUIBtn.transitions = new UIButton.Transition[1];
                UIButton.Transition transition = new UIButton.Transition();
                iconUIBtn.transitions[0] = transition;

                transition.target = iconObj_ButtonTip.GetComponent<Image>();
                transition.damp = 0.3f;
                transition.mouseoverSize = 1f;
                transition.pressedSize = 1f;
                transition.normalColor = itemIconNormalColor;
                transition.mouseoverColor = itemIconHighlightColor;
                transition.pressedColor = itemIconPressedColor;
                transition.disabledColor = itemIconDisabledColor;
                // alphaonly 属性和 highligh相关属性暂时不需要设置
                // 下面设置anchor和pivot方便后面处理位置
                RectTransform rect = iconObj_ButtonTip.GetComponent<RectTransform>();
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
                incTogglePrefabObj = GameObject.Instantiate(GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Assembler Window/produce/inc-info"));
                incTogglePrefabObj.transform.Find("inc-switch").GetComponent<Button>().onClick.RemoveAllListeners();
                GameObject.DestroyImmediate(incTogglePrefabObj.transform.Find("inc-label").gameObject);
                GameObject.DestroyImmediate(incTogglePrefabObj.transform.Find("inc-effect-value-text").gameObject);
                GameObject.DestroyImmediate(incTogglePrefabObj.transform.Find("inc-effect-value-text-2").gameObject);
                GameObject.DestroyImmediate(incTogglePrefabObj.transform.Find("inc-effect-type-text-2").gameObject);
                incTogglePrefabObj.transform.Find("inc-effect-type-text").GetComponent<RectTransform>().anchoredPosition3D = new Vector3(138, 0, 0);
                incTogglePrefabObj.transform.Find("inc-effect-type-text").GetComponent<Text>().alignment = TextAnchor.MiddleRight;

                // checkbox
                checkBoxObj = GameObject.Instantiate(GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel/hierarchy/layers/display-group/display-toggle-3/checkbox-back-structures"));
                checkBoxObj.name = "check-box";
                checkBoxObj.GetComponent<Button>().onClick.RemoveAllListeners();
                checkBoxObj.transform.Find("text").GetComponent<Text>().color = Color.white;


                leftTriangleSprite = Resources.Load<Sprite>("ui/textures/sprites/test/last-icon");
                rightTriangleSprite = Resources.Load<Sprite>("ui/textures/sprites/test/next-icon");
                backgroundSprite = Resources.Load<Sprite>("ui/textures/sprites/sci-fi/window-content-3");
                buttonBackgroundSprite = Resources.Load<Sprite>("ui/textures/sprites/sci-fi/window-content-3");
                gearSprite = Resources.Load<Sprite>("icons/signal/signal-405");
                filterSprite = Resources.Load<Sprite>("ui/textures/sprites/icons/filter-icon");
                biaxialArrowSprite = Resources.Load<Sprite>("ui/textures/sprites/icons/biaxial-arrow");
                oreSprite = Resources.Load<Sprite>("ui/textures/sprites/icons/vein-icon-56");
                crossSprite = Resources.Load<Sprite>("ui/textures/sprites/icons/delete-icon");
                bannedSprite = Resources.Load<Sprite>("icons/signal/signal-509");
                todoListSprite = Resources.Load<Sprite>("ui/textures/sprites/test/test-list-alt");
                resetSprite = Resources.Load<Sprite>("ui/textures/sprites/icons/refresh-32-icon");
                checkboxOnSprite = Resources.Load<Sprite>("ui/textures/sprites/icons/checkbox-on");
                checkboxOffSprite = Resources.Load<Sprite>("ui/textures/sprites/icons/checkbox-off");
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
                float mainContentHeight = (curWidth - smallWindowWidth) / (largeWindowWidth - smallWindowWidth) * (largeWindowViewGroupHeight - smallWindowViewGroupHeight) + smallWindowViewGroupHeight;
                viewGroupObj.GetComponent<RectTransform>().sizeDelta = new Vector2(viewGroupWidth, mainContentHeight);

                // 顶部选择配方的需要在小窗口状态下改变
                float fixedPosY = (curWidth - smallWindowWidth) / (largeWindowWidth - smallWindowWidth) * (targetIconAnchoredPosYLargeWindow - targetIconAnchoredPosYSmallWindow) + targetIconAnchoredPosYSmallWindow;
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
                float mainContentHeight = (curWidth - smallWindowWidth) / (largeWindowWidth - smallWindowWidth) * (largeWindowViewGroupHeight - smallWindowViewGroupHeight) + smallWindowViewGroupHeight;
                viewGroupObj.GetComponent<RectTransform>().sizeDelta = new Vector2(viewGroupWidth, mainContentHeight);

                // 顶部选择配方的需要在小窗口状态下改变
                float fixedPosY = (curWidth - smallWindowWidth) / (largeWindowWidth - smallWindowWidth) * (targetIconAnchoredPosYLargeWindow-targetIconAnchoredPosYSmallWindow) + targetIconAnchoredPosYSmallWindow;
                float oriX = targetProductIconObj.GetComponent<RectTransform>().anchoredPosition.x;
                targetProductIconObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(oriX, fixedPosY);
                oriX = speedInputObj.GetComponent<RectTransform>().anchoredPosition.x;
                speedInputObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(oriX, fixedPosY);
                oriX = perMinTextObj.GetComponent<RectTransform>().anchoredPosition.x;
                perMinTextObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(oriX, fixedPosY);

            }

            ShowHideChildrenWhenWindowSizeChanged(oriWidth, curWidth);
            if (nextFrameRecalc)
            {
                nextFrameRecalc = false;
                solution.ReSolve();
                RefreshAll();
            }

            if (titleInput.isFocused)
                titleInputUIBtn.highlighted = true;
            else
                titleInputUIBtn.highlighted = false;

            if(windowObj.activeSelf)
            {
                if(targetVerticalPosition >= 0)
                {
                    float distance = targetVerticalPosition - contentScrollRect.verticalNormalizedPosition;
                    float minMove = 0.01f;
                    float moveRatio = 0.1f;
                    if(uiItemNodeOrders.Count > 12)
                    {
                        float shrink = uiItemNodeOrders.Count * 1.0f / 12;
                        minMove *= (0.2f + 0.8f /shrink);
                        moveRatio *= (0.8f + 0.2f / shrink);
                    }
                        
                    if(distance <= minMove && distance >= -minMove)
                    {
                        contentScrollRect.verticalNormalizedPosition = targetVerticalPosition;
                        targetVerticalPosition = -1;
                    }
                    else
                    {
                        float move = distance * moveRatio;
                        if(move > 0 && move < minMove)
                            move = minMove;
                        else if (move < 0 && move > -minMove)
                            move = -minMove;

                        contentScrollRect.verticalNormalizedPosition += move;
                    }


                }
                bool isMoveing = targetVerticalPosition >= 0;
                for (int i = 0; i < uiItemNodes.Count; i++)
                {
                    uiItemNodes[i].OnUpdate(isMoveing);
                }
                for (int i = 0; i < uiSideItemNodes.Count; i++)
                {
                    uiSideItemNodes[i].OnUpdate(isMoveing);
                }
            }



            if (!isTopAndActive) return;
            // 下面的只有Topwindow可以响应

            bool ShiftDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool CtrlDown = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool AltDown = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            if (Input.GetKeyDown(DSPCalculatorPlugin.SwitchWindowSizeHotKey.Value) && UIHotkeySettingPatcher.CheckModifier(2, ShiftDown, CtrlDown, AltDown))
            {
                SwitchWindowSize();
            }

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

            if (UIPauseBarPatcher.pauseBarObj != null)
            {
                bool hasOpenedWindow = false;
                if (WindowsManager.windows != null)
                {
                    for (int i = 0; i < WindowsManager.windows.Count; i++)
                    {
                        if(WindowsManager.windows[i].windowObj.activeSelf)
                        {
                            hasOpenedWindow = true;
                            break;
                        }
                    }
                }

                if(!hasOpenedWindow)
                {
                    UIPauseBarPatcher.pauseBarObj.SetActive(false);
                    if (GameMain.instance != null)
                        GameMain.instance._fullscreenPaused = false;
                }
            }

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
            RefreshCheckBoxes();
        }

        public void OnTargetProductIconClick()
        {
            UIItemPicker.showAll = true;
            UIItemPicker.Popup(windowObj.GetComponent<RectTransform>().anchoredPosition + new Vector2(300f, -200f), OnTargetProductChange);
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

        public void ShowHideChildrenWhenWindowSizeChanged(float oriWidth, float curWidth)
        {
            if (oriWidth >= 0.5f * largeWindowWidth && curWidth < 0.5f * largeWindowWidth)
            {
                incToggleObj.SetActive(false);
                proliferatorSelectionObj.SetActive(false);
                targetProductTextObj.SetActive(false);
                switchSizeButtonObj.GetComponent<Image>().sprite = rightTriangleSprite;
            }
            else if (oriWidth <= 0.5f * largeWindowWidth && curWidth > 0.5f * largeWindowWidth)
            {
                incToggleObj.SetActive(true);
                proliferatorSelectionObj.SetActive(true);
                targetProductTextObj.SetActive(true);
                switchSizeButtonObj.GetComponent<Image>().sprite = leftTriangleSprite;
            }
            if (oriWidth >= 0.9f * largeWindowWidth && curWidth < 0.9f * largeWindowWidth)
            {
                assemblerSelectionObj.SetActive(false);
            }
            else if (oriWidth <= 0.9f * largeWindowWidth && curWidth > 0.9f * largeWindowWidth)
            {
                assemblerSelectionObj.SetActive(true);
            }
        }

        public void RefreshAll()
        {
            RefreshProductContent();
            RefreshResourceNeedAndByProductContent();
            RefreshFinalInfoText();
            RefreshAssemblerDemandsDisplay();
            RefreshAssemblerButtonDisplay();
            RefreshIncToggle();
            RefreshProliferatorButtonDisplay();
            RefreshCheckBoxes();
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
            ClearNodes();
            int nodeOrder = 0;

            if (solution.targetItem > 0 && solution.root != null && !solution.userPreference.solveProliferators)
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
                            uiItemNodeOrders[curNode.itemId] = nodeOrder;
                            nodeOrder++;
                        }
                        else if (solution.userPreference.showMixBeltInfo) // 如果是混带信息，则原矿也要展示
                        {
                            UIItemNode uiNode = new UIItemNode(curNode, this);
                            uiItemNodes.Add(uiNode);
                            uiItemNodeOrders[curNode.itemId] = nodeOrder;
                            nodeOrder++;
                        }
                    }
                    stack.RemoveAt(stack.Count - 1);


                    for (int i = curNode.children.Count - 1; i >= 0; i--)
                    {
                        stack.Add(curNode.children[i]);
                    }
                }
            }
            if(solution.targetItem > 0 && solution.root != null && solution.userPreference.solveProliferators)
            {
                Dictionary<int, ItemNode> visitedNodes = new Dictionary<int, ItemNode>();
                for (int n = CalcDB.proliferatorItemIds.Count; n >= 0; n--)
                {
                    List<ItemNode> stack = new List<ItemNode>();
                    if (n == CalcDB.proliferatorItemIds.Count)
                    {
                        stack.Add(solution.root);
                    }
                    else
                    {
                        int index = n;
                        int proliferatorId = CalcDB.proliferatorItemIds[index];
                        if (solution.itemNodes.ContainsKey(proliferatorId) && solution.itemNodes[proliferatorId].needSpeed > 0)
                        {
                            stack.Add(solution.itemNodes[proliferatorId]);
                        }
                    }
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
                                uiItemNodeOrders[curNode.itemId] = nodeOrder;
                                nodeOrder++;
                            }
                            else if (solution.userPreference.showMixBeltInfo) // 如果是混带信息，则原矿也要展示
                            {
                                UIItemNode uiNode = new UIItemNode(curNode, this);
                                uiItemNodes.Add(uiNode);
                                uiItemNodeOrders[curNode.itemId] = nodeOrder;
                                nodeOrder++;
                            }
                        }
                        stack.RemoveAt(stack.Count - 1);

                        for (int c = curNode.children.Count - 1; c >= 0; c--)
                        {
                            stack.Add(curNode.children[c]);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 刷新所有副屏的原材料需求和溢出产物内容
        /// </summary>
        public void RefreshResourceNeedAndByProductContent()
        {
            for (int i = 0; i < uiSideItemNodes.Count; i++)
            {
                GameObject.DestroyImmediate(uiSideItemNodes[i].obj);
            }
            ClearSideNodes();

            if (solution.itemNodes.Count == 0)
            {
                return;
            }

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
                    uiItemSimplesByItemId[node.Value.itemId] = uiResourceNode;
                    count++;
                }
            }

            // 增产剂需求额外增加，只在不把增产剂并入产线计算时才显示
            if (!solution.userPreference.solveProliferators)
            {
                foreach (var p in solution.proliferatorCount)
                {
                    if (p.Key > 0 && p.Value > 0)
                    {
                        ItemNode node = new ItemNode(p.Key, 0, solution);
                        node.satisfiedSpeed = p.Value;
                        UIItemNodeSimple uiResourceNode = new UIItemNodeSimple(node, false, this, true);
                        uiSideItemNodes.Add(uiResourceNode);
                        count++;
                    }
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
                //if (!node.Value.IsOre(solution.userPreference) && (node.Value.satisfiedSpeed - node.Value.needSpeed > 0.001f))
                if (node.Value.satisfiedSpeed - node.Value.needSpeed > 0.001f)
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

            // 然后如果显示混带内容，显示混带
            if(solution.userPreference.showMixBeltInfo)
            {
                count = 0;
                int totalUnit = 0;
                foreach (var item in solution.itemNodes)
                {
                    totalUnit += item.Value.GetInserterRatio();
                }

                UIItemNodeSimple label3 = new UIItemNodeSimple($"{"混带需求".Translate()} : {totalUnit} {"份calc".Translate()}", this);
                uiSideItemNodes.Add(label3);
                for (int i = 1; i < sideCellCountPerRow; i++)
                {
                    UIItemNodeSimple empty1 = new UIItemNodeSimple("", this); // 是因为一行有多个元素，所以需要空来占位
                    uiSideItemNodes.Add(empty1);
                }

                int[] beltInfos = new int[CalcDB.beltsDescending.Count]; // 初始化是0

                for (int i = 1; i < CalcDB.beltsDescending.Count && totalUnit > 0; i++)
                {
                    if(totalUnit > CalcDB.beltsDescending[i].speed)
                    {
                        int need = (int)(totalUnit / CalcDB.beltsDescending[i - 1].speed);
                        totalUnit -= (int)(need * CalcDB.beltsDescending[i - 1].speed);
                        beltInfos[i - 1] = need;
                    }
                }
                if(totalUnit > 0)
                    beltInfos[beltInfos.Length - 1] += 1;

                for (int i = 0; i < beltInfos.Length; i++)
                {
                    if (beltInfos[i] > 0)
                    {
                        ItemNode node = new ItemNode(CalcDB.beltsDescending[i].ID, 0, solution);
                        node.satisfiedSpeed = beltInfos[i];
                        UIItemNodeSimple uiBeltNode = new UIItemNodeSimple(node, false, this, false, true);
                        uiSideItemNodes.Add(uiBeltNode);
                        count++;
                    }
                }

                if (count % sideCellCountPerRow != 0) // 不足一行的用空填满
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

        public void ClearNodes()
        {
            uiItemNodes.Clear();
            uiItemNodeOrders.Clear();
        }

        public void ClearSideNodes()
        {
            uiSideItemNodes.Clear();
            uiItemSimplesByItemId.Clear();
        }


        public void RefreshFinalInfoText()
        {
            double totalEnergyConsumption = 0;
            foreach (var recipeInfoData in solution.recipeInfos)
            {
                totalEnergyConsumption += recipeInfoData.Value.GetTotalEnergyConsumption();
            }
            finalInfoText.text = "预估电量".Translate() + "\n" + Utils.KMG(totalEnergyConsumption) + "W";
        }

        public void RefreshAssemblerDemandsDisplay()
        {
            foreach (var item in assemblersDemandObjs)
            {
                GameObject.DestroyImmediate(item);
            }
            assemblersDemandObjs.Clear();

            // 计算每种工厂数量
            Dictionary<int, long> counts = new Dictionary<int, long>();
            foreach (var data in solution.recipeInfos)
            {
                RecipeInfo recipeInfo = data.Value;
                int assemblerItemId = recipeInfo.assemblerItemId;
                long ceilingCount =(long) Math.Ceiling(recipeInfo.assemblerCount);
                if (!counts.ContainsKey(assemblerItemId))
                    counts[assemblerItemId] = ceilingCount;
                else
                    counts[assemblerItemId] += ceilingCount;
            }

            // 创建图标和数量文本
            int i = 0;
            float eachWidth = sidePanelWidth / assemblerDemandCountPerRow;
            foreach (var pair in counts) 
            {
                if (pair.Value > 0)
                {
                    int assemblerItemId = pair.Key;
                    long count = pair.Value;
                    GameObject assemblerObj = GameObject.Instantiate(iconObj_ButtonTip, assemblersDemandsGroupObj.transform);
                    assemblerObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(i % assemblerDemandCountPerRow * eachWidth, -(i / assemblerDemandCountPerRow * 35), 0);
                    assemblerObj.GetComponent<RectTransform>().sizeDelta = new Vector2(30, 30);
                    assemblerObj.GetComponent<Image>().sprite = LDB.items.Select(assemblerItemId).iconSprite;
                    assemblerObj.GetComponent<UIButton>().tips.itemId = assemblerItemId;
                    assemblerObj.GetComponent<UIButton>().tips.corner = 3;
                    assemblerObj.GetComponent<UIButton>().tips.delay = 0.2f;

                    GameObject assemblerCountTextObj = GameObject.Instantiate(UICalcWindow.TextWithUITip, assemblersDemandsGroupObj.transform);
                    assemblerCountTextObj.name = "assembler-count";
                    assemblerCountTextObj.transform.localScale = Vector3.one;
                    assemblerCountTextObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(i % assemblerDemandCountPerRow * eachWidth + 35, -(i / assemblerDemandCountPerRow * 35), 0);
                    assemblerCountTextObj.GetComponent<UIButton>().tips.delay = 0.4f;
                    assemblerCountTextObj.GetComponent<UIButton>().tips.corner = 2;

                    Text assemblerCountText = assemblerCountTextObj.GetComponent<Text>();
                    assemblerCountText.text = "× " + Utils.KMG(count);
                    assemblerCountText.fontSize = 16;
                    assemblerCountText.raycastTarget = true;
                    if (count > 9999)
                        assemblerCountTextObj.GetComponent<UIButton>().tips.tipTitle = count.ToString("N0"); // 如果被KMG了，要在tip里显示完整数字

                    // 加入列表
                    assemblersDemandObjs.Add(assemblerObj);
                    assemblersDemandObjs.Add(assemblerCountTextObj);

                    i++;
                }
            }
        }

        public void OnGlobalIncToggleClick()
        {
            bool target = !solution.userPreference.globalIsInc;
            solution.userPreference.globalIsInc = target;
            foreach (var config in solution.userPreference.recipeConfigs)
            {
                if (config.Value.forceIncMode >= 0)
                {
                    config.Value.forceIncMode = target ? 1 : 0;
                }
            }
            RefreshIncToggle();
            nextFrameRecalc = true;
        }

        public void RefreshIncToggle()
        {
            GameObject incToggleThumb = incToggleObj.transform.Find("inc-switch/switch-thumb").gameObject;
            bool isInc = solution.userPreference.globalIsInc;
            if (isInc)
            {
                incToggleThumb.GetComponent<RectTransform>().anchoredPosition = new Vector2(-10, 0);
                incText.text = "额外产出calc".Translate();
                incText.color = incModeTextColor;
                incToggleObj.transform.Find("inc-switch").GetComponent<Image>().color = incModeImageColor;
            }
            else
            {
                incToggleThumb.GetComponent<RectTransform>().anchoredPosition = new Vector2(10, 0);
                incText.text = "生产加速calc".Translate();
                incText.color = accModeTextColor;
                incToggleObj.transform.Find("inc-switch").GetComponent<Image>().color = accModeImageColor;
            }
        }

        public void SetGlobalIncLevel(int level)
        {
            solution.userPreference.globalIncLevel = level; 
            foreach (var config in solution.userPreference.recipeConfigs)
            {
                config.Value.incLevel = level;
            }
            RefreshProliferatorButtonDisplay();
            nextFrameRecalc = true;
        }

        public void RefreshProliferatorButtonDisplay()
        {
            foreach (var btn in proliferatorUsedButtons)
            {
                if (btn.Key == solution.userPreference.globalIncLevel)
                    btn.Value.highlighted = true;
                else
                    btn.Value.highlighted = false;
            }
        }

        public void SetGlobalAssemblerPreference(int assemblerFullCode)
        {
            int typeInt = assemblerFullCode / TYPE_FILTER;
            int assemblerItemId = assemblerFullCode % TYPE_FILTER;

            solution.userPreference.globalAssemblerIdByType[typeInt] = assemblerItemId;

            // 然后对每个独立的recipeConfig进行更改
            foreach (var recipeConfigData in solution.userPreference.recipeConfigs)
            {
                int configType = CalcDB.recipeDict[recipeConfigData.Value.ID].type;
                if(configType == typeInt)
                {
                    solution.userPreference.recipeConfigs[recipeConfigData.Key].assemblerItemId = assemblerItemId; 
                }
            }

            foreach (var uiNodeData in uiItemNodes)
            {
                uiNodeData.RefreshAssemblerDisplay(false);
            }
            if (CompatManager.GB)
            {
                nextFrameRecalc = true;
            }
            else
            {
                RefreshAssemblerButtonDisplay();
                RefreshAssemblerDemandsDisplay();
                RefreshFinalInfoText();
            }
        }

        public void RefreshAssemblerButtonDisplay()
        {
            foreach (var typeData in assemblerUsedButtons)
            {
                int type = typeData.Key;
                int assemblerId = -1;
                if(CalcDB.assemblerListByType.ContainsKey(type) && CalcDB.assemblerListByType[type].Count > 0)
                    assemblerId = CalcDB.assemblerListByType[type][0].ID;
                if (solution.userPreference.globalAssemblerIdByType.ContainsKey(type))
                    assemblerId = solution.userPreference.globalAssemblerIdByType[type];

                foreach (var aData in typeData.Value)
                {
                    if(aData.Key == assemblerId)
                    {
                        aData.Value.highlighted = true;
                    }
                    else
                    {
                        aData.Value.highlighted = false;
                    }
                }
            }
        }

        public void ClearAllUserPreference()
        {
            solution.ClearUserPreference();
            solution.ReSolve();
            RefreshAll();
        }

        public void RefreshCheckBoxes()
        {
            if(cbBluebuff != null)
            {
                if (solution.userPreference.bluebuff)
                    cbBluebuff.sprite = checkboxOnSprite;
                else
                    cbBluebuff.sprite = checkboxOffSprite;
                txtBluebuff.text = "遗物名称0-1".Translate().Split('\n')[0];
            }
            if (cbEnergyBurst != null)
            {
                if (solution.userPreference.energyBurst)
                    cbEnergyBurst.sprite = checkboxOnSprite;
                else
                    cbEnergyBurst.sprite = checkboxOffSprite;
                txtEnergyBurst.text = "遗物名称1-6".Translate().Split('\n')[0];
            }
            if (cbDirac != null)
            {
                if (solution.userPreference.dirac)
                    cbDirac.sprite = checkboxOnSprite;
                else
                    cbDirac.sprite = checkboxOffSprite;
                txtDirac.text = "遗物名称2-8".Translate().Split('\n')[0];
            }
            if(cbInferior != null)
            {
                if (solution.userPreference.inferior)
                    cbInferior.sprite = checkboxOnSprite;
                else
                    cbInferior.sprite = checkboxOffSprite;
                txtInferior.text = "遗物名称3-0".Translate().Split('\n')[0];
            }
            if(cbIncMilli != null)
            {
                if (solution.userPreference.customizeIncMilli)
                    cbIncMilli.sprite = checkboxOnSprite;
                else
                    cbIncMilli.sprite = checkboxOffSprite;
                txtIncMilli.text = "强制增产效能".Translate();

            }
            if(cbAccMilli != null)
            {
                if (solution.userPreference.customizeAccMilli)
                    cbAccMilli.sprite = checkboxOnSprite;
                else
                    cbAccMilli.sprite = checkboxOffSprite;
                txtAccMilli.text = "强制加速效能".Translate();
            }
            if(cbRoundUp != null)
            {
                if(solution.userPreference.roundUpAssemgblerNum)
                    cbRoundUp.sprite = checkboxOnSprite;
                else
                    cbRoundUp.sprite = checkboxOffSprite;
                txtRoundUp.text = "生产设施数量显示向上取整".Translate();
            }
            if (cbMixbelt != null)
            {

                if (solution.userPreference.showMixBeltInfo)
                    cbMixbelt.sprite = checkboxOnSprite;
                else
                    cbMixbelt.sprite = checkboxOffSprite;
                txtMixbelt.text = "显示混带信息".Translate();
            }
            if(cbSolveProlifer != null)
            {
                if(solution.userPreference.solveProliferators)
                    cbSolveProlifer.sprite = checkboxOnSprite;
                else
                    cbSolveProlifer.sprite= checkboxOffSprite;
                txtSolveProlifer.text = "增产剂并入产线".Translate();
            }
        }

        public void OnBluebuffClick()
        {
            solution.userPreference.bluebuff = !solution.userPreference.bluebuff;
            RefreshCheckBoxes();
            nextFrameRecalc = true;
        }

        public void OnEnergyBurstClick()
        {
            solution.userPreference.energyBurst = !solution.userPreference.energyBurst;
            RefreshCheckBoxes();
            nextFrameRecalc = true;
        }

        public void OnDiracClick()
        {
            solution.userPreference.dirac = !solution.userPreference.dirac;
            RefreshCheckBoxes();
            nextFrameRecalc = true;
        }

        public void OnInferiorClick()
        {
            solution.userPreference.inferior = !solution.userPreference.inferior;
            RefreshCheckBoxes();
            nextFrameRecalc = true;
        }

        public void OnCustomIncMilliClick()
        {
            solution.userPreference.customizeIncMilli = !solution.userPreference.customizeIncMilli;
            RefreshCheckBoxes();
            nextFrameRecalc = true;
        }

        public void OnCustomAccMilliClick()
        {
            solution.userPreference.customizeAccMilli = !solution.userPreference.customizeAccMilli;
            RefreshCheckBoxes();
            nextFrameRecalc = true;
        }

        public void OnEndEditIncMilli(string input)
        {
            int incOverride = 25;
            try
            {
                incOverride = Convert.ToInt32(input);
                if (incOverride < 0)
                    incOverride = 0;
            }
            catch (Exception)
            {
                incOverride = 25;
            }
            incInput.text = incOverride.ToString();
            solution.userPreference.incMilliOverride = 1.0 * incOverride / 100;
            nextFrameRecalc = true;
        }

        public void OnEndEditAccMilli(string input)
        {
            int accOverride = 100;
            try
            {
                accOverride = Convert.ToInt32(input);
                if (accOverride < 0)
                    accOverride = 0;
            }
            catch (Exception)
            {
                accOverride = 100;
            }
            accInput.text = accOverride.ToString();
            solution.userPreference.accMilliOverride = 1.0 * accOverride / 100;
            nextFrameRecalc = true;
        }

        public void OnAssemblerRoundUpSettingChange()
        {
            bool ori = solution.userPreference.roundUpAssemgblerNum;
            bool res = !ori;
            solution.userPreference.roundUpAssemgblerNum = res;
            DSPCalculatorPlugin.RoundUpAssemblerNum.Value = res;
            DSPCalculatorPlugin.RoundUpAssemblerNum.ConfigFile.Save();

            // nextFrameRecalc = true;
            RefreshCheckBoxes();
            foreach (var uiNodeData in uiItemNodes)
            {
                uiNodeData.RefreshAssemblerDisplay(false); // 刷新每个节点的assembler显示即可
            }
        }

        public void OnSolveProliferatorSettingChange()
        {

            bool ori = solution.userPreference.solveProliferators;
            bool res = !ori;
            solution.userPreference.solveProliferators = res;

            RefreshCheckBoxes();

            nextFrameRecalc = true;
        }


        public void OnMixbeltInfoCbClick()
        {
            solution.userPreference.showMixBeltInfo = !solution.userPreference.showMixBeltInfo;
            cbMixbelt.sprite = solution.userPreference.showMixBeltInfo ? checkboxOnSprite : checkboxOffSprite;
            nextFrameRecalc = true;
        }
    }
}
