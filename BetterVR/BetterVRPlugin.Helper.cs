using HTC.UnityPlugin.Vive;
using HS2VR;
using IllusionUtility.GetUtility;
using System.Text.RegularExpressions;
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

        private static GameObject simplePClone;
        private static int pDisplayMode = 1; // 0: invisible, 1: full, 2: silhouette

        private static Camera _VRCamera;
        public static Camera VRCamera
        {
            get
            {
                if (_VRCamera == null) _VRCamera = GameObject.Find("Camera (eye)")?.GetComponent<Camera>();
                if (_VRCamera == null) _VRCamera = GameObject.Find("rCamera (eye)")?.GetComponent<Camera>();
                if (_VRCamera == null) _VRCamera = GameObject.Find("rCamera")?.GetComponent<Camera>();
                if (_VRCamera == null) _VRCamera = GameObject.Find("Camera")?.GetComponent<Camera>();
                if (_VRCamera == null) _VRCamera = Camera.main;
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

        private static Transform _leftControllerCenter;
        private static Transform _rightControllerCenter;
        internal static Transform leftControllerCenter
        {
            get
            {
                if (CreateTransformIfNotPresent(ref _leftControllerCenter, parent: FindLeftControllerRenderModel(out var center)?.parent))
                {
                    _leftControllerCenter.name = "LeftControllerCenter";
                    _leftControllerCenter.position = center;
                }
                return _leftControllerCenter;
            }
        }
        internal static Transform rightControllerCenter
        {
            get
            {
                if (CreateTransformIfNotPresent(ref _rightControllerCenter, parent: FindRightControllerRenderModel(out var center)?.parent))
                {
                    _rightControllerCenter.name = "RightControllerCenter";
                    _rightControllerCenter.position = center;
                }
                return _rightControllerCenter;
            }
        }

        private static Transform _leftCursorAttach;
        private static Transform _rightCursorAttach;
        internal static Transform leftCursorAttach
        {
            get
            {
                if (CreateTransformIfNotPresent(ref _leftCursorAttach, parent: leftControllerCenter))
                {
                    _leftCursorAttach.name = "LeftCursorAttach";
                    _leftCursorAttach.localPosition = Vector3.forward * 0.1f;
                }
                return _leftCursorAttach;
            }
        }
        internal static Transform rightCursorAttach
        {
            get
            {
                if (CreateTransformIfNotPresent(ref _rightCursorAttach, parent: rightControllerCenter))
                {
                    _rightCursorAttach.name = "RightCursorAttach";
                    _rightCursorAttach.localPosition = Vector3.forward * 0.1f;
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
            // if (leftHand && BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" GetLeftHand id {leftHand.GetInstanceID()}");
            return leftHand;
        }

        internal static GameObject GetRightHand()
        {
            var rightHand = GameObject.Find("ViveControllers/Right") ?? GameObject.Find("Controller (right)");
            // if (rightHand && BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" GetRightHand id {rightHand.GetInstanceID()}");
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
            var colliders = player.objTop.GetComponentsInChildren<DynamicBoneCollider>(true);
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
            bool shouldUseFullP = Manager.Config.HData.Son && pDisplayMode == 1;

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
                    var renderers = simplePClone.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                    foreach (var renderer in renderers)
                    {
                        renderer.enabled = true;
                        renderer.GetOrAddComponent<SilhouetteMaterialSetter>();
                    }
                }
            }

            simplePClone?.SetActive(shouldUseSimpleP);

            // Hide the original part now that there is a clone.
            var tamaRenderer = simpleBodyEtc?.objDanTama?.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (tamaRenderer) tamaRenderer.enabled = false;

            var saoRenderer = simpleBodyEtc?.objDanSao?.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (saoRenderer) saoRenderer.enabled = false;

            var regularBodyEtc = player.cmpBody?.targetEtc;
            if (regularBodyEtc != null)
            {
                regularBodyEtc.objMNPB?.SetActive(shouldUseFullP);
                regularBodyEtc.objDanTop?.SetActive(shouldUseFullP);
                regularBodyEtc.objDanSao?.SetActive(shouldUseFullP);
                regularBodyEtc.objDanTama?.SetActive(shouldUseFullP);
            }
        }

        internal static void FinishH()
        {
            bool FinishedSameTime = TryFinishHSameTime();
            if (!FinishedSameTime) Singleton<HSceneSprite>.Instance?.OnClickFinish();
        }

        internal static bool TryFinishHSameTime()
        {
            var fCtrl = Singleton<HSceneFlagCtrl>.Instance;
            var sprite = Singleton<HSceneSprite>.Instance;
            var anim = Singleton<Manager.HSceneManager>.Instance?.Hscene?.GetProcBase();

            if (!fCtrl || !sprite || anim == null || fCtrl.loopType < 2) return false;
            if (anim is Aibu || anim is Houshi || anim is Spnking || anim is Masturbation || anim is Peeping || anim is Les) return false;

            sprite.OnClickFinishSame();
            return true;
        }

        internal static void TryInitializeGloves()
        {
            if (!_leftGlove) _leftGlove = VRGlove.CreateLeftGlove();
            if (!_rightGlove) _rightGlove = VRGlove.CreateRightGlove();
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

        internal static void UpdateControllersVisibilty()
        {
            UpdateControllerVisibilty(FindControllerRenderModel(GetLeftHand(), out var lCenter));
            UpdateControllerVisibilty(FindControllerRenderModel(GetRightHand(), out var rCenter));
        }

        private static bool CreateTransformIfNotPresent(ref Transform transform, Transform parent)
        {
            if (parent == null)
            {
                if (transform != null) GameObject.Destroy(transform.gameObject);
                transform = null;
                return false;
            }

            if (transform != null && transform.parent == parent) return false;

            if (transform != null) GameObject.Destroy(transform.gameObject);

            transform = new GameObject().transform;
            transform.parent = parent;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            return true;
        }

        private static Transform FindLeftControllerRenderModel(out Vector3 center)
        {
            return FindControllerRenderModel(GetLeftHand(), out center);
        }

        private static Transform FindRightControllerRenderModel(out Vector3 center)
        {
            return FindControllerRenderModel(GetRightHand(), out center);
        }

        private static Transform FindControllerRenderModel(GameObject controller, out Vector3 center)
        {
            center = Vector3.zero;

            if (controller == null) return null;

            Transform renderModel = controller.transform.FindLoop("Model") ?? controller.transform.FindLoop("OpenVRRenderModel");
            if (!renderModel) return null;
            
            var meshFilter = renderModel.GetComponentInChildren<MeshFilter>(true);
            center =
                meshFilter ? meshFilter.transform.TransformPoint(meshFilter.mesh.bounds.center) : controller.transform.position;

            return renderModel;
        }

        private static void UpdateControllerVisibilty(Transform renderModel)
        {
            if (!renderModel) return;
            bool shouldShowController = !VRGlove.isShowingGloves || !BetterVRPlugin.IsHidingControllersEnabled();
            var renderers = renderModel.GetComponentsInChildren<MeshRenderer>(true);
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

                var source = player.cmpSimpleBody?.targetEtc?.objBody?.GetComponentInChildren<SkinnedMeshRenderer>(true);
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
