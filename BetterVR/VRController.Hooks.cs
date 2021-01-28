using HarmonyLib;

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
            pluginInstance.StartCoroutine(
                VRControllerPointer.SetAngleAfterTime(BetterVRPlugin.SetVRControllerPointerAngle.Value, __instance.leftLaserPointer)
            );
                    
        }


        [HarmonyPostfix, HarmonyPatch(typeof(ControllerManager), nameof(ControllerManager.SetRightLaserPointerActive), typeof(bool))]
        internal static void LaserPointer_SetRightLaserPointerActive(ControllerManager __instance, bool value)
        {
            if (!value) return;                    

            //If the pointer game object is active, then set the cursor angle
            if (BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" LaserPointer R active, setting angle to {BetterVRPlugin.SetVRControllerPointerAngle.Value}");
            pluginInstance.StartCoroutine(
                VRControllerPointer.SetAngleAfterTime(BetterVRPlugin.SetVRControllerPointerAngle.Value, __instance.rightLaserPointer)
            );
        }

    }
}