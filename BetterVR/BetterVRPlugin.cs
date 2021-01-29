using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using UnityEngine;
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
        public static ConfigEntry<bool> SqueezeToTurn { get; private set; }

        internal static new ManualLogSource Logger { get; private set; }
        internal static bool VREnabled = false;

        // internal static bool isOculus = XRDevice.model.Contains("Oculus");

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

            SqueezeToTurn = Config.Bind<bool>("VR General", "Squeeze to Turn", true, 
                new ConfigDescription("Allows you to turn the headset with hand rotation while zqueezing the controller."));

            SetVRControllerPointerAngle = Config.Bind<float>("VR General", "Laser Pointer Angle", 0, 
                new ConfigDescription("0 is the default angle, and negative is down.",
                new AcceptableValueRange<float>(-90, 90)));
            SetVRControllerPointerAngle.SettingChanged += SetVRControllerPointerAngle_SettingsChanged;             
                     

            //Set up game mode detectors to start certain logic when loading into main game
            VRControllerColliderHelper.TriggerHelperCoroutine();
            //Watch for headset initialized
            VRControllerInput.CheckVROrigin(this);

            //Harmony init.  It's magic!
            Harmony harmony_controller = new Harmony(GUID + "_controller");                        
            VRControllerHooks.InitHooks(harmony_controller, this);




            //Potentially important Hs2 classes
            //ControllerManager  has button input triggers, and the laser pointer
            //ControllerManagerSample   same thing?
            //ShowMenuOnClick   shows controller GUI
            //LaserPointer  -> lineRenderer  (NOT USED AT ALL)
            //vrTest
        }      


        //Check for controller input changes
        internal void Update()
        {
            // if (BetterVRPlugin.debugLog && Time.frameCount % 10 == 0) BetterVRPlugin.Logger.LogInfo($" SqueezeToTurn {SqueezeToTurn.Value} VRControllerInput.VROrigin {VRControllerInput.VROrigin}");        
            if (VRControllerInput.VROrigin == null) VRControllerInput.CheckVROrigin(this);

            //When the user squeezes the controller, apply hand rotation to headset
            if (SqueezeToTurn.Value && VRControllerInput.VROrigin != null)
            {
                VRControllerInput.CheckInputForSqueezeTurn();
            }
        }



        internal void EnableControllerColliders_SettingsChanged(object sender, System.EventArgs e) 
        {
            if (!EnableControllerColliders.Value) 
            {            
                VRControllerColliderHelper.StopHelperCoroutine();                                      
            } 
            else 
            {                
                VRControllerColliderHelper.TriggerHelperCoroutine();
            }
        }


        internal void SetVRControllerPointerAngle_SettingsChanged(object sender, System.EventArgs e) 
        {
            VRControllerPointer.UpdateControllerPointerAngle(SetVRControllerPointerAngle.Value);
        }


    }
}