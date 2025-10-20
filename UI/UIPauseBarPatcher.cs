using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace DSPCalculator.UI
{
    public class UIPauseBarPatcher
    {
        public static GameObject pauseBarObj;
        public static Text pauseBarText;
        public static Image pauseBarImage;
        //public static GameObject effectObj;
        public static UIButton pauseBarUIBtn;

        public static Sprite pauseIconSprite;
        public static Sprite playIconSprite;
        public static void Init()
        {
            if(pauseBarObj == null)
            {
                GameObject ori = GameObject.Find("UI Root/Overlay Canvas/In Game/Fullscreen UIs/Tech Tree/pause-button");
                GameObject parent = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows");
                pauseBarObj = GameObject.Instantiate(ori, parent.transform);
                pauseBarObj.SetActive(false);
                pauseBarObj.GetComponent<Button>().onClick.RemoveAllListeners();
                pauseBarObj.GetComponent<Button>().onClick.AddListener(()=> { SwitchGamePause(0); });
                pauseBarUIBtn = pauseBarObj.GetComponent<UIButton>();
                pauseBarUIBtn.transitions[2].highlightColorOverride = new Color(1, 1, 1, 0.050f);
                //GameObject effectObj = pauseBarObj.transform.Find("bar/effect").gameObject;
                //effectObj.GetComponent<Image>().color = new Color(1, 1, 1, 0.050f);

                pauseBarImage = pauseBarObj.transform.Find("content/pause-icon").GetComponent<Image>();
                pauseBarText = pauseBarObj.transform.Find("content/button-text").GetComponent<Text>();

                pauseIconSprite = Resources.Load<Sprite>("ui/textures/sprites/icons/pause-icon");
                playIconSprite = Resources.Load<Sprite>("ui/textures/sprites/icons/play-icon");
            }
        }

        public static void SwitchGamePause(int forceSet = 0)
        {
            var fullscreenPausedField = HarmonyLib.AccessTools.Field(typeof(GameMain), "_fullscreenPaused");
            bool currentPaused = (bool)(fullscreenPausedField?.GetValue(GameMain.instance) ?? false);
            
            if (forceSet == 0)
                fullscreenPausedField?.SetValue(GameMain.instance, !currentPaused);
            else
                fullscreenPausedField?.SetValue(GameMain.instance, forceSet == -1);

            bool isPaused = (bool)(fullscreenPausedField?.GetValue(GameMain.instance) ?? false);
            if(isPaused)
            {
            }
            else
            {
            }
        }

        public static void OnUpdate()
        {
            if(pauseBarObj != null && GameMain.instance!=null)
            {
                if(pauseBarObj.activeSelf)
                {
                    var fullscreenPausedField = HarmonyLib.AccessTools.Field(typeof(GameMain), "_fullscreenPaused");
                    bool isPaused = (bool)(fullscreenPausedField?.GetValue(GameMain.instance) ?? false);
                    if (isPaused)
                    {
                        pauseBarUIBtn.highlighted = false;
                        pauseBarImage.sprite = pauseIconSprite;
                        pauseBarText.text = "游戏时间已暂停".Translate();
                    }
                    else
                    {
                        pauseBarUIBtn.highlighted = true;
                        pauseBarImage.sprite = playIconSprite;
                        pauseBarText.text = "游戏时间流逝中".Translate();
                    }

                }
            }
        }
    }
}
