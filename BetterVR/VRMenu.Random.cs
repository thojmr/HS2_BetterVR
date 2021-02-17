using Manager;
using GameLoadCharaFileSystem;
using HS2VR;
using UnityEngine;
using HarmonyLib;
using UnityEngine.UI;
using System.Collections.Generic;

namespace BetterVR
{
    public static class VRMenuRandom  
    {
        public static List<GameCharaFileInfo> females = new List<GameCharaFileInfo>();
        public static List<GameCharaFileInfo> males = new List<GameCharaFileInfo>();

        /// <summary>
        /// When the Random button is presses, set a random female/male, and start the HScene
        /// </summary>
        public static void OnSelectRandomBtn()
        {
            //Just need this for fade
            VRSelectManager vrMgr = Singleton<VRSelectManager>.Instance;

            //Get a random female and set it to the HSCeneManager
            var female = GetRandomFemale(females);   
            if (female == null) return;         

            //Set one female and one empty
            Singleton<HSceneManager>.Instance.vrStatusInfos[0].Set(female.status, female.resistH, female.resistPain, female.resistAnal);

            //When multiple heroine config option is selected, add second heroine
            if (BetterVRPlugin.MultipleRandomHeroine.Value) 
            {
                var female2 = GetRandomFemale(females);
                if (female2 == null) return;

                Singleton<HSceneManager>.Instance.pngFemales = new string[] {female.fileName, female2.fileName};
                Singleton<HSceneManager>.Instance.vrStatusInfos[1].Set(female2.status, female2.resistH, female2.resistPain, female2.resistAnal);
            }
            else 
            {
                Singleton<HSceneManager>.Instance.pngFemales = new string[] {female.fileName, ""};
                Singleton<HSceneManager>.Instance.vrStatusInfos[1].Set(0, false, false, false);
            }
            
            Singleton<HSceneManager>.Instance.mapID = Singleton<Game>.Instance.mapNo;

            //Set one male, and one empty
            Singleton<HSceneManager>.Instance.pngMale = GetRandomMaleName(males);
            Singleton<HSceneManager>.Instance.pngMaleSecond = "";

            Singleton<HSceneManager>.Instance.bFutanari = false;            
            Singleton<HSceneManager>.Instance.bFutanariSecond = false;

            //Load the HScene
            Scene.LoadReserve(new Scene.Data
            {
                levelName = "VRHScene",
                fadeType = FadeCanvas.Fade.In
            }, true);
            
            vrMgr.Fade.StartFade(FadeSphere.Fade.In, false);
            Singleton<Game>.Instance.IsFade = true;
        }


        public static int GetRandomNum(int start, int limit = 10)
        {
            return UnityEngine.Random.Range(start, limit);
        }


        /// <summary>
        /// Get all female card character file information
        /// </summary>
        public static List<GameCharaFileInfo> GetAllFemales()
        {
            return GameCharaFileInfoAssist.CreateCharaFileInfoList(0, false, true, false, false, false, true, true);
        }


        public static List<GameCharaFileInfo> GetAllMales()
        {
            return GameCharaFileInfoAssist.CreateCharaFileInfoList(1, true, false, true, false, true, true, true);
        }


        public static VRSelectManager.SelectCardInfo GetRandomFemale(List<GameCharaFileInfo> females)
        {
            //Check that females list is populated
            females = females.Count <= 0 ? GetAllFemales() : females;
            var femaleCount = females.Count;
            if (femaleCount <= 0) return null;

            var winnerNum = GetRandomNum(0, femaleCount-1);
            var winner = females[winnerNum];

            //Shape it to match HScene expected format
            var femaleCardInfo = new VRSelectManager.SelectCardInfo();
            femaleCardInfo.fileName = winner.FileName;
            femaleCardInfo.status = GetRandomNum(0, 6);//TODO how to transcribe this from char card info?
            femaleCardInfo.resistH = winner.resistH == 1;
            femaleCardInfo.resistPain = winner.resistPain == 1;
            femaleCardInfo.resistAnal = winner.resistAnal == 1;

            return femaleCardInfo;
        }


        public static string GetRandomMaleName(List<GameCharaFileInfo> males)
        {
            //Check that males list is populated
            males = males.Count <= 0 ? GetAllMales() : males;
            var maleCount = males.Count;
            if (maleCount <= 0) return null;

            var winnerNum = GetRandomNum(0, maleCount-1);
            var winner = males[winnerNum];

            return winner.FileName;
        }


        /// <summary>
        /// Adds a button labeled "Random" next to the "Optional" button, that will select a random male and female and start HScene
        /// </summary>
        internal static void AppendRandomButton(VRSelectScene __instance)
        {
            //Find a button to copy from, near where you want this new button to be
            var systemButton = GameObject.Find("btnOption");
            if (systemButton == null) return;
       
            // if (BetterVRPlugin.debugLog) DebugTools.LogChildrenComponents(systemButton.transform.parent.gameObject);

            //Make a copy of the "Optional" button as a template
            var btnGOCopy = GameObject.Instantiate(systemButton.transform).gameObject;            
            btnGOCopy.name = "btnRandom";                             

            var rectTf = btnGOCopy.GetComponent<RectTransform>();
            if (rectTf == null) return;

            //Set new buttton position
            var rectPos = rectTf.anchoredPosition;
            var layoutElem = btnGOCopy.GetComponent<LayoutElement>();
            if (layoutElem == null) return;

            rectTf.localPosition = new Vector3(rectPos.x -150, rectPos.y);
            layoutElem.ignoreLayout = true;

            //Remove the image
            var image = btnGOCopy.GetComponent<Image>();            
            if (image != null)
            {
                image.enabled = false;
                GameObject.DestroyImmediate(image);
            }

            //Add custom text to button
            var text = btnGOCopy.GetComponentInChildren<Text>();
            if (text != null) 
            {
                text.text = "Random";
                text.enabled = true;
            }

            // Traverse.Create(copy.GetComponent<ObservablePointerEnterTrigger>()).Field("onPointerEnter").SetValue(Traverse.Create(original.GetComponent<ObservablePointerEnterTrigger>()).Field("onPointerEnter").GetValue());

            var btn = btnGOCopy.GetComponent<Button>();
            if (btn == null) return;

            //On click button start random HScene
            btn.onClick = new Button.ButtonClickedEvent();
            btn.onClick.AddListener(() =>
            {
                if (BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" Random button clicked ");  
                // Utils.Sound.Play(SystemSE.sel);            
                OnSelectRandomBtn();
            });

            // if (BetterVRPlugin.debugLog) DebugTools.LogChildrenComponents(btnGOCopy);

            //Set back to parent on canvas
            btnGOCopy.transform.SetParent(systemButton.transform.parent, false);

            //The below may hook up onhover visuals
            // var newMenuItem = new VRSelectScene.MenuItemUI();
            // newMenuItem.btn = btnGOCopy.GetComponent<Button>();
            // newMenuItem.texts = new List<Text> { btnGOCopy.GetComponentInChildren<Text>() };
            
            //This should add onHover functionality
            // var systemButtons = Traverse.Create(__instance).Field("systems").GetValue<VRSelectScene.MenuItemUI[]>();
            // if (systemButtons == null) return;
            // systemButtons.AddItem(newMenuItem);

            females = GetAllFemales();
            males = GetAllMales();
        }

    }

}