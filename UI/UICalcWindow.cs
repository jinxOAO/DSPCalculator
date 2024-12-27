using DSPCalculator.Logic;
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
        public const float cellHeight = 100;
        public static Color itemIconNormalColor = new Color(0.6f, 0.6f, 0.6f, 1);
        public static Color itemIconHighlightColor = new Color(0.7f, 0.7f, 0.7f, 1);

        public bool isTopAndActive;

        // UI元素
        public GameObject windowObj;
        public GameObject mainCanvasObj;
        public Text titleText;
        public Image targetProductIcon;

        public SolutionTree solution; // 该窗口对应的量化计算路径

        /// <summary>
        /// 创建新窗口
        /// </summary>
        public UICalcWindow(int i)
        {
            solution = new SolutionTree();
            isTopAndActive = true;

            GameObject oriWindowObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Blueprint Browser");
            GameObject parentWindowObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Calc Window Group");

            windowObj = GameObject.Instantiate(oriWindowObj);
            windowObj.name = "calc-window" + " " + i.ToString();
            windowObj.transform.SetParent(parentWindowObj.transform);

            windowObj.transform.localScale = Vector3.one;
            windowObj.transform.localPosition = Vector3.zero;

            // 移除不必要的obj
            GameObject.Destroy(windowObj.transform.Find("inspector-group").gameObject);
            GameObject.Destroy(windowObj.transform.Find("folder-info-group").gameObject);
            GameObject.Destroy(windowObj.transform.Find("title-group").gameObject);

            titleText = windowObj.transform.Find("panel-bg/title-text").GetComponent<Text>();

            GameObject closeButtonObj = windowObj.transform.Find("panel-bg/x").gameObject;
            Button closeButton = closeButtonObj.GetComponent<Button>();
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => { CloseWindow(); });


            // 将主要的浏览区 设置为可滚动，自动适配高度
            mainCanvasObj = windowObj.transform.Find("view-group/Scroll View/Viewport/Content").gameObject;
            GridLayoutGroup gridLayoutGroup = mainCanvasObj.AddComponent<GridLayoutGroup>();
            ContentSizeFitter contentSizeFitter = mainCanvasObj.AddComponent<ContentSizeFitter>();
            gridLayoutGroup.cellSize = new Vector2(890, 100);
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            
            windowObj.SetActive(true);


            titleText.text = "量化计算器".Translate(); // 一定要在设置active之后进行

            Transform panelParent = windowObj.transform.Find("panel-bg");

            // 初始化一个按钮 -------------
            GameObject addNewLayerButton = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel/hierarchy/layers/buttons-group/buttons/add-button");
            GameObject buttonObjPrefab = GameObject.Instantiate(addNewLayerButton);
            buttonObjPrefab.name = "button";
            buttonObjPrefab.transform.localScale = Vector3.one;
            buttonObjPrefab.GetComponent<Button>().onClick.RemoveAllListeners();
            // ----------------------------

            GameObject oriItemIconObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Station Window/storage-box-0/storage-icon-empty");

            if (oriItemIconObj != null)
            {
                GameObject targetProductIconObj = GameObject.Instantiate(oriItemIconObj);
                targetProductIconObj.name = "target-product";
                targetProductIconObj.transform.SetParent(panelParent);
                targetProductIconObj.transform.localScale = Vector3.one;
                RectTransform rect = targetProductIconObj.GetComponent<RectTransform>();
                rect.anchorMax = new Vector2(0, 1);
                rect.anchorMin = new Vector2(0, 1);
                rect.anchoredPosition3D = new Vector3(120, -50, 0);
                rect.sizeDelta = new Vector2(54, 54); // 原本是64
                targetProductIconObj.transform.Find("white").GetComponent<RectTransform>().sizeDelta = new Vector2(40, 40); // 原本是54

                targetProductIconObj.GetComponent<UIButton>().transitions[0].normalColor = itemIconNormalColor; // 原本的颜色较暗，大约在0.5，高亮0.66
                targetProductIconObj.GetComponent<UIButton>().transitions[0].mouseoverColor = itemIconHighlightColor;

                targetProductIcon = targetProductIconObj.transform.Find("white").GetComponent<Image>();
                targetProductIconObj.GetComponent<Button>().onClick.RemoveAllListeners();
                targetProductIconObj.GetComponent<Button>().onClick.AddListener(() => { OnTargetProductIconClick(); });
                //targetProductIcon.sprite = Resources.Load<Sprite>("ui/textures/sprites/icons/explore-icon"); // 是个放大镜图标
                targetProductIcon.sprite = Resources.Load<Sprite>("ui/textures/sprites/icons/controlpanel-icon-40"); // 是循环箭头环绕的齿轮，去掉40则是64*64大小的相同图标
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

            if (!isTopAndActive) return;

            if(isTopAndActive && Input.GetKeyDown(KeyCode.K))
            {
                AddCellTest();
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
            UIItemPicker.Popup(windowObj.GetComponent<RectTransform>().anchoredPosition + new Vector2(-300f, 200f), OnTargetProductChange);
        }

        public void OnTargetProductChange(ItemProto item)
        {
            if (item != null)
            {
                targetProductIcon.sprite = item.iconSprite;
                solution.SetTargetItemAndBeginSolve(item.ID);
            }
        }
    }
}
