using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using UnityEngine;
using HarmonyLib;
using System;
using KKAPI;

namespace BetterVR 
{
    [BepInPlugin(GUID, GUID, Version)]
    [BepInProcess("HoneySelect2VR")]
    public partial class BetterVRPlugin : BaseUnityPlugin 
    {
        public const string GUID = "BetterVR";
        public const string Version = "0.1";


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
            DebugTools.logger = Logger;
            VRControllerColliderHelper.pluginInstance = this;

            PluginConfigInit();

            //Set up game mode detectors to start certain logic when loading into main game
            VRControllerColliderHelper.TriggerHelperCoroutine();
            //Watch for headset initialized
            VRControllerInput.CheckVROrigin(this);

            //Harmony init.  It's magic!
            Harmony harmony_controller = new Harmony(GUID + "_controller");                        
            VRControllerHooks.InitHooks(harmony_controller, this);

            Harmony harmony_menu = new Harmony(GUID + "_menu");                        
            VRMenuHooks.InitHooks(harmony_menu, this);



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
            if (BetterVRPluginHelper.VROrigin == null) VRControllerInput.CheckVROrigin(this);

            //When the user squeezes the controller, apply hand rotation to headset
            if (SqueezeToTurn.Value && BetterVRPluginHelper.VROrigin != null)
            {
                VRControllerInput.CheckInputForSqueezeTurn();
            }
        }

    }
}