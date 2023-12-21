using BepInEx;
using BepInEx.Logging;
using Manager;
using HTC.UnityPlugin.Vive;
using HarmonyLib;

namespace BetterVR 
{
    [BepInPlugin(GUID, GUID, Version)]
    [BepInProcess("HoneySelect2VR")]
    public partial class BetterVRPlugin : BaseUnityPlugin 
    {
        public const string GUID = "BetterVR";
        public const string Version = "0.2";

        internal static new ManualLogSource Logger { get; private set; }

#if DEBUG
        internal static bool debugLog = true;
#else
        internal static bool debugLog = false;
#endif

        private static VRControllerInput.StripUpdater leftHandStripUpdater;
        private static VRControllerInput.StripUpdater rightsHandStripUpdater;

        internal void Start() 
        {
            Logger = base.Logger;
            // DebugTools.logger = Logger;
            VRControllerColliderHelper.pluginInstance = this;

            PluginConfigInit();

            //Set up game mode detectors to start certain logic when loading into main game
            VRControllerColliderHelper.TriggerHelperCoroutine();

            //Harmony init.  It's magic!
            Harmony harmony_controller = new Harmony(GUID + "_controller");                        
            VRControllerHooks.InitHooks(harmony_controller, this);

            Harmony harmony_menu = new Harmony(GUID + "_menu");
            VRMenuHooks.InitHooks(harmony_menu, this);

            //Potentially important Hs2 classes
                //ControllerManager  has button input triggers, and the laser pointer
                //ControllerManagerSample   same thing?
                //ShowMenuOnClick   shows controller GUI
                //vrTest
                    // internal static bool isOculus = XRDevice.model.Contains("Oculus");
        }

        // Check for controller input changes
        internal void Update()
        {
            if (leftHandStripUpdater == null) leftHandStripUpdater = new VRControllerInput.StripUpdater(VRControllerInput.roleL);
            leftHandStripUpdater?.CheckStrip(BetterVRPlugin.GestureStrip.Value == "Left hand");

            if (rightsHandStripUpdater == null) rightsHandStripUpdater = new VRControllerInput.StripUpdater(VRControllerInput.roleR);
            rightsHandStripUpdater?.CheckStrip(BetterVRPlugin.GestureStrip.Value == "Right hand");

            // if (BetterVRPlugin.debugLog && Time.frameCount % 10 == 0) BetterVRPlugin.Logger.LogInfo($" SqueezeToTurn {SqueezeToTurn.Value} VRControllerInput.VROrigin {VRControllerInput.VROrigin}");        

            VRControllerInput.MaybeRestoreVrOriginTransform();

            VRControllerInput.CheckInputForSqueezeScaling();
            
            // When the user squeezes the controller, apply hand rotation to headset.
            if (SqueezeToTurn.Value == "One-handed")
            {
                VRControllerInput.UpdateOneHandedMovements();
            }
            else if (SqueezeToTurn.Value == "Two-handed")
            {
                VRControllerInput.UpdateTwoHandedMovements();
            }

            if (ViveInput.GetPressUpEx<HandRole>(HandRole.LeftHand, ControllerButton.AKey) && !BetterVRPluginHelper.LeftHandGripPress()) {
                if (BetterVRPluginHelper.LeftHandTriggerPress())
                {
                    BetterVRPluginHelper.ResetView();
                }
                else
                {
                    // Toggle player part visibility.
                    Manager.Config.HData.Son = !Manager.Config.HData.Son;
                }
            }

            if (ViveInput.GetPressUpEx<HandRole>(HandRole.RightHand, ControllerButton.AKey) &&
                !BetterVRPluginHelper.RightHandGripPress() &&  !BetterVRPluginHelper.RightHandTriggerPress())
            {
                // Toggle player body visibility.
                Manager.Config.HData.Visible = !Manager.Config.HData.Visible;
            }

            HideMonochromeP();
        }

        private static AIChara.ChaControl GetPlayer()
        {
            return Singleton<HSceneManager>.Instance?.Hscene?.GetMales()?[0];
        }

        private static void HideMonochromeP()
        {
            var targetEtc = GetPlayer()?.cmpSimpleBody?.targetEtc;
            if (targetEtc == null) return;
            targetEtc.objDanTop?.SetActive(false);
            targetEtc.objMNPB?.SetActive(false);
            targetEtc.objDanSao?.SetActive(false);
            targetEtc.objDanTama?.SetActive(false);
        }
    }
}
