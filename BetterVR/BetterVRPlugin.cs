using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using KKAPI;
using HS2VR;

namespace BetterVR 
{
    [BepInPlugin(GUID, GUID, Version)]
    [BepInProcess("HoneySelect2VR")]
    public class BetterVRPlugin : BaseUnityPlugin 
    {
        public const string GUID = "BetterVR";
        public const string Version = "0.1";
        public static ConfigEntry<bool> EnableControllerColliders { get; private set; }
        public static ConfigEntry<float> SetVRControllerPointerAngle { get; private set; }

        internal static new ManualLogSource Logger { get; private set; }
        internal static bool VREnabled = false;

#if DEBUG
        internal static bool debugLog = true;
#else
        internal static bool debugLog = false;
#endif

        internal void Start() 
        {
            Logger = base.Logger;
            VRControllerColliderHelper.pluginInstance = this;

            EnableControllerColliders = Config.Bind<bool>("VR General", "Enable VR controller colliders (boop!)", true, 
                "Allows collision of VR controllers with all dynamic bones");
            EnableControllerColliders.SettingChanged += EnableControllerColliders_SettingsChanged;  


            SetVRControllerPointerAngle = Config.Bind<float>("VR General", "Laser pointer angle", 0, 
                new ConfigDescription("0 is the default angle, and negative is down.",
                new AcceptableValueRange<float>(-90, 90)));
            SetVRControllerPointerAngle.SettingChanged += SetVRControllerPointerAngle_SettingsChanged;            
                     

            //Set up game mode detectors to start certain logic when loading into main game
            // GameAPI.RegisterExtraBehaviour<VRCameraGameController>(GUID + "_camera");
            VRControllerColliderHelper.TriggerHelperCoroutine();

            //Harmony init.  It's magic!
            // Harmony harmonyCamera = new Harmony(GUID + "_camera");                        
            // VRCameraHooks.InitHooks(harmonyCamera);
        }      





        internal void EnableControllerColliders_SettingsChanged(object sender, System.EventArgs e) 
        {
            if (!EnableControllerColliders.Value) 
            {            
                //Force recalculate all verts.  With balloon active it will automatically calaulcate the correct new boundaries
                VRControllerColliderHelper.StopHelperCoroutine();                                      
            } 
            else 
            {                
                VRControllerColliderHelper.TriggerHelperCoroutine();
            }
        }


        internal void SetVRControllerPointerAngle_SettingsChanged(object sender, System.EventArgs e) 
        {
            VRControllerHelper.SetControllerPointerAngle(SetVRControllerPointerAngle.Value);
        }



    }
}