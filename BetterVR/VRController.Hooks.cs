using HTC.UnityPlugin.Vive;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BetterVR
{
    internal static class VRControllerHooks
    {

        internal static BetterVRPlugin pluginInstance;

        public static void InitHooks(Harmony harmonyInstance, BetterVRPlugin _pluginInstance)
        {
            pluginInstance = _pluginInstance;
            harmonyInstance.PatchAll(typeof(VRControllerHooks));
        }

        /// <summary>
        /// When the vr controller laser pointer is updated, change the angle to the configured value
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(ControllerManager), nameof(ControllerManager.SetLeftLaserPointerActive), typeof(bool))]
        internal static void LaserPointer_SetLeftLaserPointerActive(ControllerManager __instance, bool value)
        {
            if (!value) return;

            //If the pointer game object is active, then set the cursor angle
            if (BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" LaserPointer L active, setting angle to {BetterVRPlugin.SetVRControllerPointerAngle.Value}");

            // Not working currently.
            // pluginInstance.StartCoroutine(
            //    VRControllerPointer.SetAngleAfterTime(BetterVRPlugin.SetVRControllerPointerAngle.Value, BetterVRPluginHelper.VR_Hand.left)
            // );

        }

        [HarmonyPostfix, HarmonyPatch(typeof(ControllerManager), nameof(ControllerManager.SetRightLaserPointerActive), typeof(bool))]
        internal static void LaserPointer_SetRightLaserPointerActive(ControllerManager __instance, bool value)
        {
            if (!value) return;

            //If the pointer game object is active, then set the cursor angle
            if (BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" LaserPointer R active, setting angle to {BetterVRPlugin.SetVRControllerPointerAngle.Value}");

            // Not working currently.
            // pluginInstance.StartCoroutine(
            //    VRControllerPointer.SetAngleAfterTime(BetterVRPlugin.SetVRControllerPointerAngle.Value, BetterVRPluginHelper.VR_Hand.right)
            // );
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HS2VR.GripMoveCrtl), "ControllerMove")]
        internal static bool ControllerMovePatch()
        {
            if (!BetterVRPluginHelper.LeftHandGripPress() && !BetterVRPluginHelper.RightHandGripPress())
            {
                // If no grip is pressed, allow vanilla logic to handle turning.
                return true;
            }

            if (BetterVRPlugin.SqueezeToTurn.Value == "One-handed")
            {
                // Completely delegate to mod logic for both rotation and locomotion.
                return false;
            }

            if (BetterVRPlugin.SqueezeToTurn.Value == "Two-handed")
            {
                if (BetterVRPluginHelper.LeftHandGripPress() && BetterVRPluginHelper.RightHandGripPress())
                {
                    // Completely delegate to mod logic for both rotation and locomotion.
                    return false;
                }

                if (BetterVRPluginHelper.LeftHandGripPress() && BetterVRPluginHelper.LeftHandTriggerPress())
                {
                    // Only one grip is pressed, let vanilla game handle locomotion.
                    return true;
                }

                if (BetterVRPluginHelper.RightHandGripPress() && BetterVRPluginHelper.RightHandTriggerPress())
                {
                    // Only one grip is pressed, let vanilla game handle locomotion.
                    return true;
                }

                return false;
            }

            return true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HScene), "GetAxis")]
        public static bool PatchGetAxis(HandRole _hand, ref Vector2 __result)
        {
            if (_hand != HandRole.RightHand)
            {
                // For safety, fall back to vanilla logic in left hand as a workaround in case there is something missing in this patch.
                return true;
            }

            // This method works for Oculus controllers' thumbsticks too.
            var axis = BetterVRPluginHelper.GetRightHandPadStickCombinedOutput();

            if (axis == Vector2.zero)
            {
                return true;
            }

            // The vanilla pad/thumb stick detection is half broken and does not work on some platforms, giving rise to the necessity of this patch.
            __result = axis;

            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HS2VR.GripMoveCrtl), "GetAxis")]
        public static bool GripMoveCrtlGetAxisPatch(HandRole _hand, ref Vector2 __result)
        {
            return PatchGetAxis(_hand, ref __result);
        }

        // [HarmonyPostfix, HarmonyPatch(typeof(AIChara.ChaControl), nameof(AIChara.ChaControl.Initialize))]
        // internal static void ChaControlStartPatch(AIChara.ChaControl __instance, GameObject _objRoot)
        // {
        //    __instance.GetOrAddComponent<StripColliderUpdater>().Init(__instance);
        // }

        [HarmonyPrefix, HarmonyPatch(typeof(AIChara.ChaControl), "LoadCharaFbxDataAsync")]
        internal static void ChaControlLoadCharaFbxDataAsyncPrefix(AIChara.ChaControl __instance)
        {
            __instance.GetOrAddComponent<StripColliderUpdater>().Init(__instance);
            
            if (__instance.name.Contains("chaF_001"))
            {
                VRControllerCollider.characterForHeightReference = __instance.transform;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Illusion.Component.UI.ColorPicker.Info), "SetImagePosition")]
        internal static void SetImagePositionReporter(Illusion.Component.UI.ColorPicker.Info __instance, PointerEventData cursorPos)
        {
            // var rtField = typeof(Illusion.Component.UI.ColorPicker.Info).GetField("myRt", BindingFlags.NonPublic | BindingFlags.Instance);
            // var rt = (RectTransform)rtField.GetValue(__instance);
            var dummyRtField = typeof(Illusion.Component.UI.ColorPicker.Info).GetField("dummyRT", BindingFlags.NonPublic | BindingFlags.Instance);
            var dummyRt = (RectTransform)dummyRtField.GetValue(__instance);
            if (!dummyRt)
            {
                // Some color pickers in the game is missing dummyRT and does not respond to cursor drag properly.
                // Add dummyRT to fix it as needed.
                GameObject dummyRtHolder = new GameObject();
                dummyRtHolder.transform.parent = __instance.transform;
                dummyRtHolder.transform.localPosition = Vector3.zero;
                dummyRtHolder.transform.localRotation = Quaternion.identity;
                RectTransform newDummyRt = dummyRtHolder.AddComponent<RectTransform>();
                newDummyRt.position = new Vector3(-80, -80, 0);
                newDummyRt.sizeDelta = new Vector2(160, 160);
                dummyRtField.SetValue(__instance, newDummyRt);
            }
        }
    }
}
