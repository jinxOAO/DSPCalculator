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
        public const float cellWidth = 890;
        public const float cellHeight = 100;

        public bool isTopAndActive;
        public GameObject windowObj;
        public GameObject mainCanvasObj;
        public Text titleText;

        /// <summary>
        /// 创建新窗口
        /// </summary>
        public UICalcWindow(int i)
        {
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
    }
}
