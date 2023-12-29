using HTC.UnityPlugin.Vive;
using HS2VR;
using IllusionUtility.GetUtility;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace BetterVR
{    
    public static class BetterVRPluginHelper
    {     
        public static GameObject VROrigin;
        public static UnityEvent recenterVR { set; private get; }

        private static Camera _VRCamera;

        public static Camera VRCamera
        {
            get
            {
                if (_VRCamera == null)
                {
                    _VRCamera = (GameObject.Find("Camera (eye)") ?? GameObject.Find("rCamera (eye)"))?.GetComponent<Camera>();
                }
                return _VRCamera;
            }
        }

        private static GameObject privacyScreen;
        private static GameObject gloves;
        public static GameObject leftGlove;
        public static GameObject rightGlove;

        public enum VR_Hand
        {
            left,
            right,
            none
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
            var leftHand = GameObject.Find("ViveControllers/Left");
            if (leftHand == null) leftHand = GameObject.Find("Controller (left)");
            if (leftHand == null) return null;

            if (BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" GetLeftHand id {leftHand.GetInstanceID()}");

            return leftHand.gameObject;
        }


        internal static GameObject GetRightHand()
        {
            var rightHand = GameObject.Find("ViveControllers/Right");
            if (rightHand == null) rightHand = GameObject.Find("Controller (right)");
            if (rightHand == null) return null;

            if (BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" GetRightHand id {rightHand.GetInstanceID()}");

            return rightHand.gameObject;
        }

        /// <summary>
        /// Lazy wait for VR headset origin to exists
        /// </summary>
        internal static void Init(GameObject VROrigin)
        {
            BetterVRPluginHelper.VROrigin = VROrigin;
            BetterVRPluginHelper.FixWorldScale();
            BetterVRPluginHelper.UpdatePrivacyScreen();
        }


        /// <summary>
        /// Enlarge the VR camera, to make the world appear to shrink by xx%
        /// </summary>
        internal static void FixWorldScale(bool enable = true)
        {
            var viveRig = GameObject.Find("ViveRig");
            if (viveRig != null)
            {
                viveRig.transform.localScale = Vector3.one * (enable ? BetterVRPlugin.PlayerScale : 1);
            }
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

        private static void LoadGloves()
        {
            if (gloves != null) return;
            var glovesPrefab = AssetBundleManager.LoadAssetBundle(AssetBundleNames.Chara00Mo_Gloves_00)?.Bundle?.LoadAsset<GameObject>(
                "assets/illusion/assetbundle/prefabs/chara/male/00/mo_gloves_00/p_cm_glove_gunte.prefab");
            if (!glovesPrefab) return;
            gloves = GameObject.Instantiate(glovesPrefab);
            gloves.GetComponentInChildren<SkinnedMeshRenderer>()?.GetOrAddComponent<SilhouetteMaterialSetter>();
        }

        public static GameObject GetLeftGlove()
        {
            LoadGloves();
            if (gloves == null) return null;
            var glove = gloves.transform.FindLoop("cf_J_ArmLow01_L")?.gameObject;
            if (!glove) return null;
            glove.GetOrAddComponent<VRControllerInput.FingerPoseUpdater>().Init(HandRole.LeftHand);
            return glove;
        }

        public static GameObject GetRightGlove()
        {
            LoadGloves();
            if (gloves == null) return null;
            var glove = gloves.transform.FindLoop("cf_J_ArmLow01_R")?.gameObject;
            if (!glove) return null;
            glove.GetOrAddComponent<VRControllerInput.FingerPoseUpdater>().Init(HandRole.RightHand, -1);
            return glove;
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

        internal static void UpdatePrivacyScreen()
        {
            EnsurePrivacyScreen().SetActive(BetterVRPlugin.UsePrivacyScreen.Value);
        }

        internal static Vector2 GetRightHandPadStickCombinedOutput()
        {
            Vector2 output = ViveInput.GetPadAxisEx<HandRole>(HandRole.RightHand);
            output.x += ViveInput.GetAxisEx<HandRole>(HandRole.RightHand, ControllerAxis.JoystickX);
            output.y += ViveInput.GetAxisEx<HandRole>(HandRole.RightHand, ControllerAxis.JoystickY);
            return output;
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

        internal static void UpdateHandsVisibility()
        {
            UpdateHandVisibility(BetterVRPluginHelper.GetLeftHand(), ref leftGlove);
            UpdateHandVisibility(BetterVRPluginHelper.GetRightHand(), ref rightGlove);
        }

        private static void UpdateControllerVisibilty(Transform renderModel)
        {
            if (!renderModel) return;

            bool shouldShowController =
                leftGlove == null || !leftGlove.activeSelf ||
                rightGlove == null || !rightGlove.activeSelf ||
                (Manager.Config.HData.Visible && !Manager.Config.HData.SimpleBody) ||
                !BetterVRPlugin.GetPlayer();
            
            var renderers = renderModel.GetComponentsInChildren<MeshRenderer>();
            if (renderers == null) return;
            
            foreach (var renderer in renderers) renderer.enabled = shouldShowController;
        }

        private static void UpdateHandVisibility(GameObject hand, ref GameObject glove)
        {
            Transform renderModel = BetterVRPluginHelper.FindControllerRenderModel(hand, out Vector3 center);
            if (glove != null)
            {
                if (renderModel == null || glove.gameObject == null) glove = null;
            }

            if (glove == null && renderModel != null)
            {
                glove = hand.name.Contains("ight") ? GetRightGlove() : GetLeftGlove();
                if (glove)
                {
                    glove.name = hand.name + "_simpleGlove";
                }
                else
                {
                    glove = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    glove.GetOrAddComponent<MeshRenderer>();
                    glove.AddComponent<SilhouetteMaterialSetter>();
                    glove.name = hand.name + "_simpleSphere";
                }
                glove.gameObject.SetActive(false);
                GameObject.DontDestroyOnLoad(glove.gameObject);
            }

            bool shouldShowHand = BetterVRPlugin.ShowHand.Value && glove != null && renderModel != null;
            bool isShowingHand = glove != null && glove.activeSelf;

            if (shouldShowHand != isShowingHand) UpdateControllerVisibilty(renderModel);

            if (!glove) return;

            glove.gameObject.SetActive(shouldShowHand);

            if (shouldShowHand)
            {
                if (glove.transform.parent != renderModel.parent && !VRControllerInput.repositioningHand)
                {
                    glove.transform.parent = renderModel.parent;
                }

                if (glove.transform.parent != null)
                {
                    // The render model may have been changed by the system so the simple renderer may need to be repositioned too.
                    bool isRightHand = glove.name.Contains("ight");
                    Vector3 offsetFromCenter;
                    if (glove.name == hand.name + "_simpleSphere")
                    {
                        glove.transform.localScale = new Vector3(0.04f, 0.06f, 0.09f);
                        glove.transform.localRotation = Quaternion.identity;
                        offsetFromCenter = (isRightHand ? Vector3.right : Vector3.left) * 0.04f;
                    }
                    else
                    {
                        glove.transform.localScale = Vector3.one * BetterVRPlugin.HandScale.Value;
                        glove.transform.localRotation =
                            isRightHand ? BetterVRPlugin.RightHandRotation.Value : BetterVRPlugin.LeftHandRotation.Value;
                        offsetFromCenter = isRightHand ? BetterVRPlugin.RightHandOffset.Value : BetterVRPlugin.LeftHandOffset.Value;
                    }
                    glove.transform.position = center + renderModel.transform.TransformVector(offsetFromCenter);
                }
            }
        }

        private static GameObject EnsurePrivacyScreen() {
            if (privacyScreen != null)
            {
                return privacyScreen;
            }
            
            privacyScreen = new GameObject("PrivacyMode");
            Canvas privacyCanvas = privacyScreen.AddComponent<Canvas>();
            privacyCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            privacyCanvas.sortingOrder = 30000;
            GameObject privacyOverlay = new GameObject("Overlay");
            privacyOverlay.transform.SetParent(privacyScreen.transform);
            Image image = privacyOverlay.AddComponent<Image>();
            image.rectTransform.sizeDelta = new Vector2((float)(Screen.width * 4), (float)(Screen.height * 4));
            image.color = Color.black;
            UnityEngine.Object.DontDestroyOnLoad(privacyScreen);

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

                GameObject.Destroy(this);
            }
        }
    }
}
