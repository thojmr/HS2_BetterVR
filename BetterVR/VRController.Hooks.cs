using HarmonyLib;

namespace BetterVR
{ 
    internal static class VRControllerHooks
    {
        public static void InitHooks(Harmony harmonyInstance)
        {
            harmonyInstance.PatchAll(typeof(VRControllerHooks));
        }

        /// <summary>
        /// When the vr controller laser pointer is updated, change the angle to the configured value
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(ControllerManager), nameof(ControllerManager.SetLeftLaserPointerActive), typeof(bool))]
        internal static void LaserPointer_SetLeftLaserPointerActive(LaserPointer __instance, bool value)
        {
            if (!value) return;                    

            //If the pointer game object is active, then set the cursor angle
            if (BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" LaserPointer R active, setting angle to {BetterVRPlugin.SetVRControllerPointerAngle.Value}");
            VRControllerPointer.SetControllerPointerAngle(BetterVRPlugin.SetVRControllerPointerAngle.Value);        
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ControllerManager), nameof(ControllerManager.SetRightLaserPointerActive), typeof(bool))]
        internal static void LaserPointer_SetRightLaserPointerActive(LaserPointer __instance, bool value)
        {
            if (!value) return;                    

            //If the pointer game object is active, then set the cursor angle
            if (BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" LaserPointer L active, setting angle to {BetterVRPlugin.SetVRControllerPointerAngle.Value}");
            VRControllerPointer.SetControllerPointerAngle(BetterVRPlugin.SetVRControllerPointerAngle.Value);        
        }

    }
}