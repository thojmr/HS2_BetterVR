using HarmonyLib;
using HS2VR;
using Illusion.Game;
using Manager;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace BetterVR
{ 
    internal static class VRMenuHooks
    {

        internal static BetterVRPlugin pluginInstance;


        public static void InitHooks(Harmony harmonyInstance, BetterVRPlugin _pluginInstance)
        {
            pluginInstance = _pluginInstance;
            harmonyInstance.PatchAll(typeof(VRMenuHooks));
        }


        /// <summary>
        /// When VRSelectScene is activated 
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(VRSelectScene), "Start")]
        internal static void VRSelectScene_Start(VRSelectScene __instance)
        {                   
            //If the pointer game object is active, then set the cursor angle
            if (BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" VRSelectScene_Start ");   

            //Get the character card data
            VRSelectManager vrMgr = Singleton<VRSelectManager>.Instance;

            if (BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" selectFelameInfos count {vrMgr.selectFelameInfos.Length} "); 
            if (BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" selectMaleFileNames count {vrMgr.selectMaleFileNames.Length} "); 

            //Add Random button to GUI
            AppendRandomButton(__instance);                  
        }


        /// <summary>
        /// Adds a button labeed "Random" next to the Option button, that will select a random male and female and start HScene
        /// </summary>
        internal static void AppendRandomButton(VRSelectScene __instance)
        {
            //Find a button to copy from, near where you want this new button to be
            var systemButton = GameObject.Find("btnOption");
            if (systemButton == null) return;
       
            if (BetterVRPlugin.debugLog) BetterVRPluginHelper.LogParents(systemButton.transform.gameObject, 2);

            //Make a copy of the "Optional" button as a template
            var btnGOCopy = GameObject.Instantiate(systemButton.transform);            
            btnGOCopy.name = "btnRandom";                 

            if (BetterVRPlugin.debugLog) BetterVRPluginHelper.LogParents(btnGOCopy.gameObject, 2);

            //Try moving the button to a better location
            var rectPos = btnGOCopy.GetComponent<RectTransform>().anchoredPosition;
            btnGOCopy.GetComponent<RectTransform>().anchoredPosition.Set(rectPos.x -10, rectPos.y);
            btnGOCopy.GetComponent<LayoutElement>().ignoreLayout = true;

            // if (BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($"parentObject rectTransform : {btnGOCopy.GetComponent<RectTransform>().anchoredPosition}");

            //Remove the image
            var image = btnGOCopy.gameObject.GetComponent<Image>();
            if (image != null)
            {
                GameObject.Destroy(image);
            }

            //Add custom text
            var text = btnGOCopy.gameObject.AddComponent<Text>();
            if (text != null)
            {
                text.text = "Random";
            }

            // Traverse.Create(copy.GetComponent<ObservablePointerEnterTrigger>()).Field("onPointerEnter").SetValue(Traverse.Create(original.GetComponent<ObservablePointerEnterTrigger>()).Field("onPointerEnter").GetValue());

            //On click button start random HScene
            btnGOCopy.GetOrAddComponent<Button>().onClick = new Button.ButtonClickedEvent();
            btnGOCopy.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" Random button clicked ");  
                // Utils.Sound.Play(SystemSE.sel);            
                VRMenuRandom.OnSelectRandomBtn();
            });

            //Set to parent on canvas
            btnGOCopy.SetParent(systemButton.transform.parent, false);

            var newMenuItem = new VRSelectScene.MenuItemUI();
            newMenuItem.btn = btnGOCopy.GetComponent<Button>();
            newMenuItem.texts = new List<Text> { btnGOCopy.GetComponentInChildren<Text>() };
            
            //This should add onHover functionality
            var systemButtons = Traverse.Create(__instance).Field("systems").GetValue<VRSelectScene.MenuItemUI[]>();
            if (systemButtons == null) return;
            systemButtons.AddItem(newMenuItem);
        }


    }
}