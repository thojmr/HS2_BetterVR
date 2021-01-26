using HarmonyLib;
using UnityEngine;
using ADV;
using System.Collections.Generic;
using KKAPI.MainGame;

namespace BetterVR
{
    public static class VRCameraHooks
    {
        public static Harmony harmonyInstance;

        internal static void InitHooks(Harmony _harmonyInstance = null)
        {
            if (_harmonyInstance != null) harmonyInstance = _harmonyInstance;

            if (harmonyInstance == null) return;
            harmonyInstance.PatchAll(typeof(VRCameraHooks));
        }

        internal static void UnInitHooks(string harmonyGUID)
        {
            if (harmonyInstance == null) return;
            harmonyInstance.UnpatchAll(harmonyGUID);
        }

        //When the heroine changes location (ADVScene like Going to lunch, exercising, Date)
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ADVScene), "Update")]
        internal static void ADVScene_Update(ADVScene __instance)
        {
            if (!BetterVRPlugin.VREnabled || !BetterVRPlugin.MoveWithTalkScene.Value) return;

            MainScenario scenario = Traverse.Create(__instance).Field("scenario").GetValue<MainScenario>();
            if (scenario == null || scenario.commandController == null) return;

            System.Collections.Generic.Dictionary<int, ADV.CharaData> characters = scenario.commandController.Characters;
            if (characters == null || characters.Count <= 0 || characters[0] == null) return;

            //Get the main heroine (is it always at index 0, probably not)?
            ChaControl charCtrl = characters[0].chaCtrl;
            if (charCtrl == null || charCtrl.objHead == null) return;

            //Gets heroines head position.  Will place the user facing this position
            Transform heroineTransform = charCtrl.objHead.transform;
            if (heroineTransform == null) return;

            VRCameraController.MoveToFaceHeroine_ADVScene(heroineTransform.position, heroineTransform.rotation);                
        }
        
        //When the ADV scene (TalkScene) is done clear the last ADVScene position
        [HarmonyPostfix]
        [HarmonyPatch(typeof(TalkScene), "OnDestroy")]
        internal static void TalkScene_OnDestroy(TalkScene __instance)
        {
            if (!BetterVRPlugin.MoveWithTalkScene.Value) return;
            if (BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" TalkScene_OnDestroy ");

            VRCameraController.ClearLastPosition();                
        }

        //When heroine changes to a new location after user selects pink location pin (ActionScene, HScene)
        [HarmonyPostfix]
        [HarmonyPatch(typeof(HSceneProc), "SetLocalPosition", typeof(HSceneProc.AnimationListInfo))]
        internal static void HSceneProc_SetLocalPosition(HSceneProc __instance)
        {
            if (!BetterVRPlugin.VREnabled || !BetterVRPlugin.MoveWithTalkScene.Value) return;
            if (BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" SetLocalPosition ");
            
            List<ChaControl> lstFemale = Traverse.Create(__instance).Field("lstFemale").GetValue<List<ChaControl>>();
            if (lstFemale == null || lstFemale[0] == null) return;

            ChaControl female = lstFemale[0];
            if (female == null || female.objHead == null) return;

            //Gets heroines head position.  Will place the user facing this position
            Transform femaleTransform = female.objHead.transform;
            if (femaleTransform == null) return;

            VRCameraController.MoveToFaceHeroine_HScene(femaleTransform.position, femaleTransform.rotation);
        }        


        //         /// <summary>
        // /// Triggers when changing location in h scenes
        // /// </summary>
        // [HarmonyPostfix]
        // [HarmonyPatch(typeof(HSceneProc), nameof(HSceneProc.ChangeCategory))]
        // private static void HLocationChangeHook(List<ChaControl> ___lstFemale)
        // {
        //     try
        //     {
        //         var hsceneCenterPoint = ___lstFemale[0].transform.position;
        //         MobManager.GatherMobsAroundPoint(hsceneCenterPoint);
        //     }
        //     catch (Exception ex)
        //     {
        //         UnityEngine.Debug.LogException(ex);
        //     }
        // }


        //         [HarmonyPostfix]
        // [HarmonyPatch(typeof(ChaStatusScene), "Start")]
        // private static void CreateButtons(ChaStatusScene __instance, ChaStatusComponent ___cmpMale)


        // [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeClothesShorts))]
        //     private static void ChangeClothesShorts(ChaControl __instance) => chaControl = __instance;

        // [HarmonyPostfix, HarmonyPatch(typeof(Info), "Init")]
        // private static void InfoInit(Info __instance) => ActionGameInfoInstance = __instance;


        //             /// <summary>
        // /// Something that happens at the end of H scene loading, good enough place to initialize stuff
        // /// </summary>
        // [HarmonyPrefix, HarmonyPatch(typeof(HSceneProc), "MapSameObjectDisable")]
        // private static void MapSameObjectDisable(HSceneProc __instance)
        // {


        //                 /// <summary>
        // /// Set the new original position when changing positions via the H point picker scene
        // /// </summary>
        // /// <param name="__instance"></param>
        // [HarmonyPostfix, HarmonyPatch(typeof(HSceneProc), "ChangeCategory")]
        // private static void ChangeCategory(HSceneProc __instance)
        // {

    }
}