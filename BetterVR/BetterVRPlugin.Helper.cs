using HTC.UnityPlugin.Vive;
using HS2VR;
using IllusionUtility.GetUtility;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace BetterVR
{    
    public static class BetterVRPluginHelper
    {
        public enum VR_Hand
        {
            left,
            right,
            none
        }

        private static readonly Regex HAND_NAME_MATCHER = new Regex("Hand|hand");
        private static readonly Regex P_NAME_MATCHER = new Regex("Dan|dan");

        public static GameObject VROrigin;
        public static UnityEvent recenterVR { set; private get; }

        private static Camera _VRCamera;
        private static GameObject simplePClone;
        private static int pDisplayMode = 1; // 0: invisible, 1: full, 2: silhouette

        public static Camera VRCamera
        {
            get
            {
                if (_VRCamera == null) _VRCamera = GameObject.Find("Camera (eye)")?.GetComponent<Camera>();
                if (_VRCamera == null) _VRCamera = GameObject.Find("rCamera (eye)")?.GetComponent<Camera>();
                if (_VRCamera == null) _VRCamera = GameObject.Find("rCamera")?.GetComponent<Camera>();
                if (_VRCamera == null) _VRCamera = GameObject.Find("Camera")?.GetComponent<Camera>();
                if (_VRCamera == null)
                {
                    BetterVRPlugin.Logger.LogWarning("VR Camera not found, may try again later");
                    var cameras = GameObject.FindObjectsOfType<Camera>();
                    string cameraNames = "";
                    foreach (var camera in cameras) cameraNames += cameraNames + "; ";
                    BetterVRPlugin.Logger.LogDebug("Current cameras in scene: " + cameraNames);
                }
                return _VRCamera;
            }
        }

        private static Image privacyScreen;
        private static VRGlove _leftGlove;
        private static VRGlove _rightGlove;

        internal static VRGlove leftGlove { 
            get {
                TryInitializeGloves();
                return _leftGlove;
            }
        }
        internal static VRGlove rightGlove
        {
            get
            {
                TryInitializeGloves();
                return _rightGlove;
            }
        }

        private static Transform _leftCursorAttach;
        private static Transform _rightCursorAttach;
        internal static Transform leftCursorAttach
        {
            get
            {
                if (!_leftCursorAttach) _leftCursorAttach = new GameObject("LeftCursorAttach").transform;
                var controllerModel = FindLeftControllerRenderModel(out var center);
                if (controllerModel)
                {
                    if (_leftCursorAttach.parent != controllerModel.parent)
                    {
                        _leftCursorAttach.parent = controllerModel.parent;
                        _leftCursorAttach.localScale = Vector3.one;
                        _leftCursorAttach.localRotation = Quaternion.identity;
                    }
                    _leftCursorAttach.position = center + controllerModel.TransformVector(Vector3.forward * 0.1f);
                }
                return _leftCursorAttach;
            }
        }
        internal static Transform rightCursorAttach
        {
            get
            {
                if (!_rightCursorAttach) _rightCursorAttach = new GameObject("RightCursorAttach").transform;
                var controllerModel = FindRightControllerRenderModel(out var center);
                if (controllerModel)
                {
                    if (_rightCursorAttach.parent != controllerModel.parent)
                    {
                        _rightCursorAttach.parent = controllerModel.parent;
                        _rightCursorAttach.localScale = Vector3.one;
                        _rightCursorAttach.localRotation = Quaternion.identity;
                    }
                    _rightCursorAttach.position = center + controllerModel.TransformVector(Vector3.forward * 0.125f);
                }
                return _rightCursorAttach;
            }
        }

        private static RadialMenu _leftRadialMenu;
        private static RadialMenu _rightRadialMenu;
        internal static RadialMenu leftRadialMenu {
            get
            {
                if (!_leftRadialMenu)
                {
                    _leftRadialMenu = new GameObject("LeftRadialMenu").AddComponent<RadialMenu>();
                    _leftRadialMenu.gameObject.SetActive(false);
                }
                _leftRadialMenu.hand = leftCursorAttach;
                return _leftRadialMenu;
            }
        }
        internal static RadialMenu rightRadialMenu
        {
            get
            {
                if (!_rightRadialMenu)
                {
                    _rightRadialMenu = new GameObject("RightRadialMenu").AddComponent<RadialMenu>();
                    _rightRadialMenu.gameObject.SetActive(false);
                }
                _rightRadialMenu.hand = rightCursorAttach;
                return _rightRadialMenu;
            }
        }

        private static HandHeldToy _handHeldToy;
        internal static HandHeldToy handHeldToy
        {
            get { return (_handHeldToy && _handHeldToy.gameObject) ? _handHeldToy : (_handHeldToy = new GameObject("BetterVRHandHeldToy").AddComponent<HandHeldToy>()); }
        }

        private static GaugeHitIndicator _gaugeHitIndicator;
        internal static GaugeHitIndicator gaugeHitIndicator
        {
            get { return _gaugeHitIndicator ?? (_gaugeHitIndicator = new GaugeHitIndicator()); }
        }

        /// Use an enum to get the correct hand
        /// </summary>
        internal static GameObject GetHand(VR_Hand hand)
        {
            if (hand == VR_Hand.left) return GetLeftHand();
            if (hand == VR_Hand.right) return GetRightHand();

            return null;
        }

        /// <summary>
        /// Get The left hand controller vr game object
        /// </summary>
        internal static GameObject GetLeftHand()
        {
            var leftHand = GameObject.Find("ViveControllers/Left") ?? GameObject.Find("Controller (left)");
            if (leftHand && BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" GetLeftHand id {leftHand.GetInstanceID()}");
            return leftHand;
        }

        internal static GameObject GetRightHand()
        {
            var rightHand = GameObject.Find("ViveControllers/Right") ?? GameObject.Find("Controller (right)");
            if (rightHand && BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" GetRightHand id {rightHand.GetInstanceID()}");
            return rightHand;
        }

        /// <summary>
        /// Lazy wait for VR headset origin to exists
        /// </summary>
        internal static void Init(GameObject VROrigin)
        {
            BetterVRPluginHelper.VROrigin = VROrigin;
            FixWorldScale();
        }

        /// <summary>
        /// Enlarge the VR camera, to make the world appear to shrink by xx%
        /// </summary>
        internal static void FixWorldScale(bool enable = true)
        {
            var viveRig = GameObject.Find("ViveRig");
            if (viveRig == null) return;
            viveRig.transform.localScale = Vector3.one * (enable ? BetterVRPlugin.PlayerScale : 1);
        }

        // Moves VR camera to the player's head.
        internal static void ResetView()
        {
            VRControllerInput.ClearRecordedVrOriginTransform();

            if (VROrigin)
            {
                // Remove any vertical rotation.
                Quaternion rotation = VROrigin.transform.rotation;
                VROrigin.transform.rotation = Quaternion.Euler(0, rotation.y, 0);
            }

            recenterVR?.Invoke();
            VRSettingUI.CameraInitAction?.Invoke();
        }

        internal static void CyclePlayerPDisplayMode()
        {
            // Sync display mode before changing it.
            if (!Manager.Config.HData.Son) pDisplayMode = 0;
            // Cycle player part display mode.
            pDisplayMode = (pDisplayMode + 1) % 3;
            // Toggle player part visibility.
            UpdatePDisplay();
            UpdatePlayerColliderActivity();
        }

        internal static void UpdatePlayerColliderActivity()
        {
            var player = BetterVRPlugin.GetPlayer();
            if (player == null) return;
            var colliders = player.objTop.GetComponentsInChildren<DynamicBoneCollider>();
            foreach (var collider in colliders)
            {
                if (HAND_NAME_MATCHER.IsMatch(collider.name))
                {
                    collider.enabled = Manager.Config.HData.Visible;
                }
                else if (P_NAME_MATCHER.IsMatch(collider.name))
                {
                    collider.enabled = Manager.Config.HData.Son;
                }
            }
        }

        private static void UpdatePDisplay()
        {
            Manager.Config.HData.Son = (pDisplayMode != 0);

            var player = BetterVRPlugin.GetPlayer();
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
                        renderer.GetOrAddComponent<SilhouetteMaterialSetter>();
                    }
                }
            }

            simplePClone?.SetActive(shouldUseSimpleP);

            // Hide the original part now that there is a clone.
            var tamaRenderer = simpleBodyEtc?.objDanTama?.GetComponentInChildren<SkinnedMeshRenderer>();
            if (tamaRenderer) tamaRenderer.enabled = false;

            var saoRenderer = simpleBodyEtc?.objDanSao?.GetComponentInChildren<SkinnedMeshRenderer>();
            if (saoRenderer) saoRenderer.enabled = false;

            var regularBodyEtc = player.cmpBody?.targetEtc;
            if (regularBodyEtc != null)
            {
                regularBodyEtc.objMNPB?.SetActive(shouldUseRegularP);
                regularBodyEtc.objDanTop?.SetActive(shouldUseRegularP);
                regularBodyEtc.objDanSao?.SetActive(shouldUseRegularP);
                regularBodyEtc.objDanTama?.SetActive(shouldUseRegularP);
            }
        }

        internal static void FinishH()
        {
            var fCtrl = Singleton<HSceneFlagCtrl>.Instance;
            var sprite = Singleton<HSceneSprite>.Instance;
            var anim = Singleton<Manager.HSceneManager>.Instance?.Hscene?.GetProcBase();
            if (!fCtrl || !sprite || anim == null || fCtrl.loopType < 0) return;

            if (fCtrl.loopType == 0 ||
                anim is Aibu || anim is Houshi || anim is Spnking || anim is Masturbation || anim is Peeping || anim is Les)
            {
                sprite.OnClickFinish();
            }
            else
            {
                sprite.OnClickFinishSame();
            }
        }

        internal static void TryInitializeGloves()
        {
            if (!_leftGlove) _leftGlove = VRGlove.CreateLeftGlove();
            if (!_rightGlove) _rightGlove = VRGlove.CreateRightGlove();
        }

        internal static bool LeftHandTriggerPress()
        {
            return ViveInput.GetPressEx<HandRole>(HandRole.LeftHand, ControllerButton.Trigger);
        }

        internal static bool LeftHandGripPress()
        {
            return ViveInput.GetPressEx<HandRole>(HandRole.LeftHand, ControllerButton.Grip);
        }

        internal static bool RightHandTriggerPress()
        {
            return ViveInput.GetPressEx<HandRole>(HandRole.RightHand, ControllerButton.Trigger);
        }

        internal static bool RightHandGripPress()
        {
            return ViveInput.GetPressEx<HandRole>(HandRole.RightHand, ControllerButton.Grip);
        }

        internal static void UpdatePrivacyScreen(Color? color = null)
        {
            EnsurePrivacyScreen().gameObject.SetActive(BetterVRPlugin.UsePrivacyScreen.Value);
            if (color != null) privacyScreen.color = (Color) color;
        }

        internal static Vector2 GetRightHandPadStickCombinedOutput()
        {
            Vector2 output = ViveInput.GetPadAxisEx<HandRole>(HandRole.RightHand);
            output.x += ViveInput.GetAxisEx<HandRole>(HandRole.RightHand, ControllerAxis.JoystickX);
            output.y += ViveInput.GetAxisEx<HandRole>(HandRole.RightHand, ControllerAxis.JoystickY);
            return output;
        }

        internal static Transform FindLeftControllerRenderModel(out Vector3 center)
        {
            return FindControllerRenderModel(GetLeftHand(), out center);
        }

        internal static Transform FindRightControllerRenderModel(out Vector3 center)
        {
            return FindControllerRenderModel(GetRightHand(), out center);
        }

        internal static Transform FindControllerRenderModel(GameObject hand, out Vector3 center)
        {
            center = Vector3.zero;

            if (hand == null) return null;

            Transform renderModel = hand.transform.FindLoop("Model") ?? hand.transform.FindLoop("OpenVRRenderModel");
            if (!renderModel) return null;
            
            var meshFilter = renderModel.GetComponentInChildren<MeshFilter>();
            center =
                meshFilter ? meshFilter.transform.TransformPoint(meshFilter.mesh.bounds.center) : hand.transform.position;

            return renderModel;
        }

        internal static void UpdateControllersVisibilty()
        {
            UpdateControllerVisibilty(FindControllerRenderModel(GetLeftHand(), out var lCenter));
            UpdateControllerVisibilty(FindControllerRenderModel(GetRightHand(), out var rCenter));
        }

        private static void UpdateControllerVisibilty(Transform renderModel)
        {
            if (!renderModel) return;

            bool shouldShowController =
                !VRGlove.isShowingGloves ||
                BetterVRPlugin.HandDisplay.Value == "Controllers" ||
                BetterVRPlugin.HandDisplay.Value == "GlovesAndControllers";
            
            var renderers = renderModel.GetComponentsInChildren<MeshRenderer>();
            foreach (var renderer in renderers) renderer.enabled = shouldShowController;
        }

        private static Image EnsurePrivacyScreen() {
            if (privacyScreen && privacyScreen.gameObject && privacyScreen.transform.parent)
            {
                return privacyScreen;
            }
            
            Canvas privacyCanvas = new GameObject("PrivacyCanvas").AddComponent<Canvas>();
            privacyCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            privacyCanvas.sortingOrder = 30000;
            GameObject privacyOverlay = new GameObject("PrivacySreen");
            privacyOverlay.transform.SetParent(privacyCanvas.transform);
            privacyScreen = privacyOverlay.AddComponent<Image>();
            privacyScreen.transform.SetParent(privacyCanvas.transform);
            privacyScreen.rectTransform.sizeDelta = new Vector2((float)(Screen.width * 4), (float)(Screen.height * 4));
            privacyScreen.color = Color.black;
            Object.DontDestroyOnLoad(privacyCanvas.gameObject);

            return privacyScreen;
        }

        internal class SilhouetteMaterialSetter : MonoBehaviour
        {
            void Update()
            {
                var player = BetterVRPlugin.GetPlayer();
                if (!player || !player.loadEnd) return;

                var source = player.cmpSimpleBody?.targetEtc?.objBody?.GetComponentInChildren<SkinnedMeshRenderer>();
                if (!source) return;

                var meshRenderer = GetComponent<MeshRenderer>();
                if (meshRenderer) meshRenderer.sharedMaterials = source.sharedMaterials;

                var skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
                if (skinnedMeshRenderer) skinnedMeshRenderer.sharedMaterials = source.sharedMaterials;

                UpdateControllersVisibilty();

                Destroy(this);
            }
        }
    }
}
