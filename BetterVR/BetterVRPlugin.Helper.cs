using HTC.UnityPlugin.Vive;
using HS2VR;
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

        internal static Vector2 GetRightHandPadOrStickAxis()
        {
            return ViveInput.GetPadAxisEx<HandRole>(HandRole.RightHand);
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

    }
}
