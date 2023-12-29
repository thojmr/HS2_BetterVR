using BepInEx;
using BepInEx.Logging;
using Manager;
using HTC.UnityPlugin.Vive;
using HarmonyLib;
using UnityEngine;

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

        private static StripUpdater leftHandStripUpdater;
        private static StripUpdater rightsHandStripUpdater;
        private static GameObject simplePClone;

        public static int pDisplayMode { get; private set; }  = 1; // 0: invisible, 1: full, 2: silhouette


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
            if (leftHandStripUpdater == null) leftHandStripUpdater = new StripUpdater(VRControllerInput.roleL);
            leftHandStripUpdater?.CheckStrip(BetterVRPlugin.GestureStrip.Value == "Left hand");

            if (rightsHandStripUpdater == null) rightsHandStripUpdater = new StripUpdater(VRControllerInput.roleR);
            rightsHandStripUpdater?.CheckStrip(BetterVRPlugin.GestureStrip.Value == "Right hand");

            // if (BetterVRPlugin.debugLog && Time.frameCount % 10 == 0) BetterVRPlugin.Logger.LogInfo($" SqueezeToTurn {SqueezeToTurn.Value} VRControllerInput.VROrigin {VRControllerInput.VROrigin}");        

            VRControllerInput.MaybeRestoreVrOriginTransform();

            VRControllerInput.CheckInputForSqueezeScaling();

            VRControllerInput.CheckInputForHandReposition();

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
                    BetterVRPluginHelper.UpdateControllersVisibilty();
                }
                else
                {
                    // Sync display mode before changing it.
                    if (!Manager.Config.HData.Son) pDisplayMode = 0;
                    // Cycle player part display mode.
                    pDisplayMode = (pDisplayMode + 1) % 3;
                    // Toggle player part visibility.
                    Manager.Config.HData.Son = (pDisplayMode != 0);
                    BetterVRPluginHelper.UpdateControllersVisibilty();
                }
            }

            if (ViveInput.GetPressUpEx<HandRole>(HandRole.RightHand, ControllerButton.AKey) &&
                !BetterVRPluginHelper.RightHandGripPress() &&  !BetterVRPluginHelper.RightHandTriggerPress())
            {
                // Toggle player body visibility.
                Manager.Config.HData.Visible = !Manager.Config.HData.Visible;
                BetterVRPluginHelper.UpdateControllersVisibilty();
            }

            UpdateMonochromeP();

            BetterVRPluginHelper.UpdateHandsVisibility();
        }

        internal static AIChara.ChaControl GetPlayer()
        {
            return Singleton<HSceneManager>.Instance?.Hscene?.GetMales()?[0];
        }

        private static void UpdateMonochromeP()
        {
            var player = GetPlayer();
            if (!player || !player.loadEnd) return;

            bool shouldUseSimpleP = Manager.Config.HData.Son && pDisplayMode == 2;
            bool shouldUseRegularP = Manager.Config.HData.Son && pDisplayMode == 1;


            var simpleBodyEtc = player.cmpSimpleBody?.targetEtc;
            GameObject simpleBody = simpleBodyEtc?.objBody;
            if (simplePClone != null)
            {
                if (!shouldUseSimpleP || simpleBody == null || simplePClone.transform.parent != simpleBody.transform.parent)
                {
                    GameObject.Destroy(simplePClone);
                    simplePClone = null;
                }

            }

            if (shouldUseSimpleP && simplePClone == null)
            {
                GameObject simpleP = simpleBodyEtc?.objDanTop;
                if (simpleBody && simpleP)
                {
                    simplePClone = GameObject.Instantiate(simpleP, simpleP.transform.parent);
                    simplePClone.transform.SetPositionAndRotation(simpleP.transform.position, simpleP.transform.rotation);
                    simplePClone.transform.localScale = simpleP.transform.localScale;
                    // Reparent so that it is a sibling instead of a child of simpleBody and
                    // can be displayed even if simpleBody is hidden.
                    simplePClone.transform.SetParent(simpleBody.transform.parent, worldPositionStays: true);
                    var renderers = simplePClone.GetComponentsInChildren<SkinnedMeshRenderer>();
                    foreach (var renderer in renderers)
                    {
                        renderer.enabled = true;
                        renderer.GetOrAddComponent<BetterVRPluginHelper.SilhouetteMaterialSetter>();
                    }
                }
            }
            
            simplePClone?.SetActive(shouldUseSimpleP);

            // Hide the original part now that there is a clone.
            var tamaRenderer = simpleBodyEtc?.objDanTama?.GetComponentInChildren<SkinnedMeshRenderer>();
            if (tamaRenderer) tamaRenderer.enabled = false;

            var saoRenderer = simpleBodyEtc?.objDanSao?.GetComponentInChildren<SkinnedMeshRenderer>();
            if (saoRenderer) saoRenderer.enabled = false;

            var regularBodyEtc = GetPlayer()?.cmpBody?.targetEtc;
            if (regularBodyEtc != null)
            {
                regularBodyEtc.objMNPB?.SetActive(shouldUseRegularP);
                regularBodyEtc.objDanTop?.SetActive(shouldUseRegularP);
                regularBodyEtc.objDanSao?.SetActive(shouldUseRegularP);
                regularBodyEtc.objDanTama?.SetActive(shouldUseRegularP);
            }
        }
    }
}
