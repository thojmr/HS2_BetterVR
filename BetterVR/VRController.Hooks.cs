using HTC.UnityPlugin.Vive;
using HarmonyLib;
using UnityEngine;

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

        [HarmonyPrefix, HarmonyPatch(typeof(HScene), "GetAxis")]
        public static bool PatchGetAxis(HScene __instance, HandRole _hand, ref Vector2 __result)
        {
            if (_hand != HandRole.RightHand)
            {
                // For safety, fall back to vanilla logic in left hand as a workaround in case there is something missing in this patch.
                return true;
            }

            // This method works for Oculus controllers' thumbsticks too.
            var axis = ViveInput.GetPadAxisEx<HandRole>(HandRole.RightHand);

            if (axis == Vector2.zero)
            {
                return true;
            }

            // The vanilla pad/thumb stick detection is half broken and does not work on some platforms, giving rise to the necessity of this patch.
            __result = axis;

            return false;
        }
    }
}
