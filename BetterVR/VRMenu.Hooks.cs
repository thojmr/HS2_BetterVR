using HarmonyLib;
using HS2VR;
using Manager;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

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

            //Add Random button to GUI, next to optional button
            VRMenuRandom.AppendRandomButton(__instance);
            VRMenuRandom.VRSelectSceneStart();
            BetterVRPluginHelper.UpdatePrivacyScreen(Color.gray);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GripMoveCrtl), "Start")]
        internal static void FindVrOrigin(GripMoveCrtl __instance)
        {
            GameObject objVROrigin = (GameObject)typeof(GripMoveCrtl).GetField("objVROrigin", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
            if (objVROrigin)
            {
                BetterVRPluginHelper.Init(objVROrigin);
            }
        }

        static bool FirstHSceneAnimationPending = true;

        [HarmonyPrefix, HarmonyPatch(typeof(HS2.TitleScene), "Start")]
        internal static void TitleSceneStartPatch()
        {
            BetterVRPluginHelper.UpdatePrivacyScreen(Color.black);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HScene), nameof(HScene.Start))]
        internal static void HSceneStartPatch()
        {
            FirstHSceneAnimationPending = true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HScene), nameof(HScene.ChangeAnimation))]
        internal static void ChangeAnimationPatch()
        {
            if (FirstHSceneAnimationPending)
            {
                FirstHSceneAnimationPending = false;
                // Allow the vanilla game to reset camera for the first animation.
                return;
            }

            if (Manager.Config.HData.InitCamera)
            {
                return;
            }

            VRControllerInput.RecordVrOriginTransform();
        }

        [HarmonyPostfix, HarmonyPatch(typeof(HScene), nameof(HScene.ChangeAnimation))]
        internal static void ChangeAnimationPostfix()
        {
            VRControllerCollider.UpdateDynamicBoneColliders();
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HScene), "SetClothStateStartMotion")]
        internal static bool SetClothStateStartMotionPatch()
        {
            return false;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(VRSettingUI), "Start")]
        internal static void FindResetViewButton(VRSettingUI __instance)
        {
            Button recenterButton = (Button)typeof(VRSettingUI).GetField("btnRecenter", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
            if (recenterButton != null)
            {
                BetterVRPluginHelper.recenterVR = recenterButton.onClick;
            }
        }

        private static bool hasStartedTitleSceneForFirstTime = false;

        [HarmonyPostfix, HarmonyPatch(typeof(HS2.TitleScene), "Start")]
        internal static void TitleScenePatch(HS2.TitleScene __instance)
        {
            bool shouldPlay = BetterVRPlugin.SkipTitleScene.Value && !hasStartedTitleSceneForFirstTime;
            hasStartedTitleSceneForFirstTime = true;
            if (shouldPlay) __instance.OnPlay();
            BetterVRPluginHelper.UpdateControllersVisibilty();
        }


        [HarmonyPostfix, HarmonyPatch(typeof(HSceneManager.HSceneTables), "LoadAnimationFileName")]
        internal static void PositionUnlockPatch(HSceneManager.HSceneTables __instance)
        {
            if (!BetterVRPlugin.UnlockAllPositions.Value || __instance.lstAnimInfo == null) return;
            foreach (var infos in __instance.lstAnimInfo)
            {
                if (infos == null) continue;
                foreach (var info in infos)
                {
                    if (info == null || info.nStatePtns == null) continue;
                    for (int i = 0; i < 7; i++) if (!info.nStatePtns.Contains(i)) info.nStatePtns.Add(i);
                    BetterVRPlugin.Logger.LogInfo("Unlocked position: " + info.nameAnimation);
                }
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HScene), "CheckStartBase")]
        internal static void CheckStartBasePrefix()
        {
            PositionUnlockPatch(HSceneManager.HResourceTables);
        }
    }
}
