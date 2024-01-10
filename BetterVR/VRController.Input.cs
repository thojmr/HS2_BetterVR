using TMPro;
using HTC.UnityPlugin.Vive;
using Illusion.Extensions;
using System;
using System.Reflection;
using UnityEngine;

namespace BetterVR
{
    public static class VRControllerInput
    {
        internal static ViveRoleProperty roleH { get; private set; } = ViveRoleProperty.New(DeviceRole.Hmd);
        internal static ViveRoleProperty roleR { get; private set; } = ViveRoleProperty.New(HandRole.RightHand);
        internal static ViveRoleProperty roleL { get; private set; } = ViveRoleProperty.New(HandRole.LeftHand);
        internal static ControllerManager controllerManager;
        internal static bool isDraggingScale { get { return twoHandedWorldGrab != null && twoHandedWorldGrab.canScale; } }
        private static TwoHandedWorldGrab _twoHandedWorldGrab;
        private static TwoHandedWorldGrab twoHandedWorldGrab {
            get {
                if (_twoHandedWorldGrab == null || _twoHandedWorldGrab.gameObject == null) {
                    _twoHandedWorldGrab = new GameObject("WorldGrabScale").AddComponent<TwoHandedWorldGrab>();
                    _twoHandedWorldGrab.enabled = false;
                }
                return _twoHandedWorldGrab;
            }
        }
        internal static Vector3 handMidpointLocal
        {
            get { return Vector3.Lerp(VivePose.GetPose(roleL).pos, VivePose.GetPose(roleR).pos, 0.5f); }
        }

        internal static float handDistanceLocal
        {
            get
            {
                return Vector3.Distance(VivePose.GetPose(VRControllerInput.roleL).pos, VivePose.GetPose(VRControllerInput.roleR).pos);
            }
        }

        internal static HandRole GetHandRole(ViveRoleProperty roleProperty)
        {
            if (roleProperty == roleL) return HandRole.LeftHand;
            if (roleProperty == roleR) return HandRole.RightHand;
            return HandRole.Invalid;
        }

        /// <summary>
        /// Handles world scaling, rotation, and locomotion when user squeezes the grip
        /// </summary>
        internal static void UpdateSqueezeMovement()
        {
            Transform vrOrigin = BetterVRPluginHelper.VROrigin?.transform;
            if (!vrOrigin) return;

            bool leftHandTriggerAndGrip =
                ViveInput.GetPressEx<HandRole>(HandRole.LeftHand, ControllerButton.Trigger) &&
                ViveInput.GetPressEx<HandRole>(HandRole.LeftHand, ControllerButton.Grip);
            bool rightHandTriggerAndGrip =
                ViveInput.GetPressEx<HandRole>(HandRole.RightHand, ControllerButton.Trigger) &&
                ViveInput.GetPressEx<HandRole>(HandRole.RightHand, ControllerButton.Grip);
            bool bothGrips =
                ViveInput.GetPressEx<HandRole>(HandRole.LeftHand, ControllerButton.Grip) &&
                ViveInput.GetPressEx<HandRole>(HandRole.RightHand, ControllerButton.Grip);
            
            bool twoHandedTurn = BetterVRPlugin.IsTwoHandedTurnEnabled() && bothGrips;
            bool shouldScale = leftHandTriggerAndGrip && rightHandTriggerAndGrip;

            twoHandedWorldGrab.enabled = shouldScale || twoHandedTurn;
            twoHandedWorldGrab.canScale = shouldScale;

            bool allowOneHandedWorldGrab =
                !twoHandedWorldGrab.enabled && (BetterVRPlugin.IsOneHandedTurnEnabled() || BetterVRPlugin.IsTwoHandedTurnEnabled());

            if (BetterVRPluginHelper.leftControllerCenter)
            {
                BetterVRPluginHelper.leftControllerCenter.GetOrAddComponent<OneHandedWorldGrab>().enabled =
                    leftHandTriggerAndGrip && !rightHandTriggerAndGrip && allowOneHandedWorldGrab;
            }

            if (BetterVRPluginHelper.rightControllerCenter)
            {
                BetterVRPluginHelper.rightControllerCenter.GetOrAddComponent<OneHandedWorldGrab>().enabled =
                    rightHandTriggerAndGrip && !leftHandTriggerAndGrip && allowOneHandedWorldGrab;
            }

            if (!isDraggingScale && bothGrips &&
                ViveInput.GetPressEx<HandRole>(HandRole.LeftHand, ControllerButton.AKey) &&
                ViveInput.GetPressEx<HandRole>(HandRole.RightHand, ControllerButton.AKey))
            {
                twoHandedWorldGrab.enabled = false;
                ResetWorldScale();
            }
        }

        internal static void ResetWorldScale()
        {
            var vrOrigin = BetterVRPluginHelper.VROrigin?.transform;
            if (!vrOrigin) return;

            var handMidpoint = vrOrigin.TransformPoint(handMidpointLocal);

            BetterVRPlugin.PlayerLogScale.Value = (float)BetterVRPlugin.PlayerLogScale.DefaultValue;

            RestoreHandMidpointWorldPosition(handMidpoint);
        }

        internal static void RestoreHandMidpointWorldPosition(Vector3? desiredWorldPosition)
        {
            var vrOrigin = BetterVRPluginHelper.VROrigin?.transform;
            if (desiredWorldPosition == null || vrOrigin == null) return;
            vrOrigin.Translate((Vector3)desiredWorldPosition - vrOrigin.TransformPoint(handMidpointLocal), Space.World);
        }

        public class TwoHandedWorldGrab : MonoBehaviour
        {
            private float scaleDraggingFactor;
            private Vector3? desiredHandMidpointWorldCoordinates;
            private static Vector3? lastHandPositionDifference = null;
            private static TextMeshPro _scaleIndicator;
            private static TextMeshPro scaleIndicator
            {
                get
                {
                    if (!_scaleIndicator || !_scaleIndicator.gameObject) _scaleIndicator = CreateScaleIndicator();
                    return _scaleIndicator;
                }
            }
            private bool _canScale = false;
            internal bool canScale
            {
                get { return _canScale; }
                set
                {
                    if (_canScale != value) InitializeScaleDraggingFactor();
                    _canScale = value;
                }
            }

            void OnEnable()
            {
                if (canScale) InitializeScaleDraggingFactor();

                var vrOrigin = BetterVRPluginHelper.VROrigin;
                if (vrOrigin == null)
                {
                    desiredHandMidpointWorldCoordinates = null;
                    lastHandPositionDifference = null;
                }
                else
                {
                    desiredHandMidpointWorldCoordinates = vrOrigin.transform.TransformPoint(VRControllerInput.handMidpointLocal);
                    lastHandPositionDifference = VivePose.GetPose(roleR).pos - VivePose.GetPose(roleL).pos;
                }
            }

            void OnDisable()
            {
                _canScale = false;
                if (scaleIndicator) scaleIndicator.enabled = false;
            }

            void OnRenderObject()
            {
                var vrOrigin = BetterVRPluginHelper.VROrigin?.transform;
                if (!vrOrigin) return;

                if (BetterVRPlugin.IsTwoHandedTurnEnabled())
                {
                    Vector3 handPositionDifference = VivePose.GetPose(roleR).pos - VivePose.GetPose(roleL).pos;
                    if (lastHandPositionDifference != null)
                    {
                        Quaternion localRotationDelta =
                            Quaternion.FromToRotation(handPositionDifference, (Vector3)lastHandPositionDifference);

                        if (BetterVRPlugin.AllowVerticalRotation.Value)
                        {
                            vrOrigin.rotation = vrOrigin.rotation * localRotationDelta;
                        }
                        else
                        {
                            vrOrigin.Rotate(0, localRotationDelta.eulerAngles.y, 0, Space.Self);
                        }
                    }
                    lastHandPositionDifference = handPositionDifference;
                }

                scaleIndicator.enabled = canScale;

                if (canScale)
                {
                    var scale = scaleDraggingFactor / VRControllerInput.handDistanceLocal;
                    BetterVRPlugin.PlayerScale = scale;
                    scaleIndicator?.SetText("" + String.Format("{0:0.000}", scale));
                }

                VRControllerInput.RestoreHandMidpointWorldPosition(desiredHandMidpointWorldCoordinates);
            }

            private void InitializeScaleDraggingFactor()
            {
                scaleDraggingFactor = handDistanceLocal * BetterVRPlugin.PlayerScale;
            }

            private static TextMeshPro CreateScaleIndicator()
            {
                var camera = BetterVRPluginHelper.VRCamera;
                if (!camera) return null;
                var textMesh =
                    new GameObject().AddComponent<Canvas>().gameObject.AddComponent<TextMeshPro>();
                textMesh.transform.SetParent(camera.transform);
                textMesh.transform.localPosition = new Vector3(0, 0.25f, 0.75f);
                textMesh.transform.localRotation = Quaternion.identity;
                textMesh.transform.localScale = Vector3.one * 0.1f;
                textMesh.fontSize = 16;
                textMesh.color = Color.blue;
                textMesh.alignment = TextAlignmentOptions.Center;
                return textMesh;
            }
        }

        public class OneHandedWorldGrab : MonoBehaviour
        {
            Transform worldPivot;
            Transform vrOrginPlacer;
            Transform stabilizer;
            Vector3 desiredControllerPosition;

            void Awake()
            {
                (worldPivot = new GameObject().transform).parent = new GameObject("RotationStabilizedController").transform;
                worldPivot.parent.parent = transform;
                worldPivot.parent.localPosition = Vector3.zero;
                worldPivot.parent.localRotation = Quaternion.identity;
                (vrOrginPlacer = new GameObject().transform).parent = new GameObject().transform;
            }

            void OnEnable()
            {
                // stabilizer =
                //    transform.GetComponentInParent<ViveRoleSetter>()?.GetComponentInChildren<HTC.UnityPlugin.PoseTracker.PoseStablizer>(true)?.transform;
                stabilizer = null;

                if (stabilizer) worldPivot.parent.rotation = stabilizer.rotation;

                // Place the world pivot at neutral rotation.
                worldPivot.rotation = Quaternion.identity;
                // Pivot the world around the controller.
                worldPivot.localPosition = Vector3.zero;

                desiredControllerPosition = worldPivot.position;
            }

            void OnRenderObject()
            {
                if (stabilizer) worldPivot.parent.rotation = stabilizer.rotation;

                var vrOrigin = BetterVRPluginHelper.VROrigin;
                if (!vrOrigin) return;

                if (!BetterVRPlugin.IsOneHandedTurnEnabled())
                {
                    worldPivot.rotation = Quaternion.identity;
                }
                else if (!BetterVRPlugin.AllowVerticalRotation.Value)
                {
                    // Remove vertical rotation.
                    var angles = worldPivot.rotation.eulerAngles;
                    worldPivot.rotation = Quaternion.Euler(0, angles.y, 0);
                }

                // Make sure the position and rotation of the vrOriginPlacer's parent is the same as teh world pivot.
                vrOrginPlacer.parent.SetPositionAndRotation(worldPivot.transform.position, worldPivot.transform.rotation);

                // Use vrOrginPlacer to record the current vrOrigin rotation and position
                vrOrginPlacer.SetPositionAndRotation(vrOrigin.transform.position, vrOrigin.transform.rotation);

                // Move the vrOriginPlacer's parent to where the controller should be to see how that affects vrOriginPlacer.
                vrOrginPlacer.parent.SetPositionAndRotation(desiredControllerPosition, Quaternion.identity);

                // Move and rotate vrOrgin to restore the original position and rotation of the controller.
                vrOrigin.transform.SetPositionAndRotation(vrOrginPlacer.position, vrOrginPlacer.rotation);
            }
        }

        public class PreventMovement : MonoBehaviour
        {
            private const float EXPIRATION_TIME = 16;
            private Vector3 persistentPosition;
            private Quaternion persitionRotation;
            private float activeTime;

            void OnEnable()
            {
                persistentPosition = transform.position;
                persitionRotation = transform.rotation;
                activeTime = 0;
            }

            void Update()
            {
                activeTime += Time.deltaTime;

                // Stop attempting to restore camera transform if there is any input that might move the camera.
                if (activeTime > EXPIRATION_TIME ||
                    ViveInput.GetPressEx<HandRole>(HandRole.LeftHand, ControllerButton.Grip) ||
                    ViveInput.GetPressEx<HandRole>(HandRole.RightHand, ControllerButton.Grip) ||
                    Mathf.Abs(BetterVRPluginHelper.GetLeftHandPadStickCombinedOutput().x) > 0.25f ||
                    Mathf.Abs(BetterVRPluginHelper.GetRightHandPadStickCombinedOutput().x) > 0.25f ||
                    !Manager.HSceneManager.isHScene)
                {
                    this.enabled = false;
                    return;
                }

                // Force restoring last known camera transform before animation change
                // since vanilla game erroneously resets camera after changing animation
                // even if the camera init option is toggled off.
                transform.SetPositionAndRotation(persistentPosition, persitionRotation);
            }
        }

        // Attaches a small menu to the hand after long pressing Y/B
        public class MenuAutoGrab : MonoBehaviour
        {
            private static FieldInfo cgMenuField;
            private float BUTTON_PRESS_TIME_THRESHOLD = 0.5f;
            private float leftButtonPressTime = 0;
            private float rightButtonPressTime = 0;
            private Transform hand;
            private Vector3? originalScale;
            private CanvasGroup menu;
            internal HandRole handRole { get; private set; } = HandRole.Invalid;

            void Awake()
            {
                if (cgMenuField == null) cgMenuField = typeof(HS2VR.OpenUICrtl).GetField("cgMenu", BindingFlags.Instance | BindingFlags.NonPublic);
            }

            void Update()
            {
                if (menu == null)
                {
                    var ctrl = GetComponent<HS2VR.OpenUICrtl>();
                    if (ctrl != null) menu = (CanvasGroup)cgMenuField.GetValue(ctrl);
                }

                var camera = BetterVRPluginHelper.VRCamera;

                if (menu == null || camera == null) return;


                if (ViveInput.GetPressEx<HandRole>(HandRole.LeftHand, ControllerButton.Menu))
                {
                    leftButtonPressTime += Time.deltaTime;
                }
                else
                {
                    leftButtonPressTime = 0;
                }

                if (ViveInput.GetPressEx<HandRole>(HandRole.RightHand, ControllerButton.Menu))
                {
                    rightButtonPressTime += Time.deltaTime;
                }
                else
                {
                    rightButtonPressTime = 0;
                }


                if (handRole != HandRole.Invalid && ViveInput.GetPressDownEx<HandRole>(handRole, ControllerButton.Menu))
                {
                    // Reset menu scale to vanilla size and close it.
                    if (originalScale != null) menu.transform.localScale = (Vector3)originalScale;
                    menu.Enable(false, true, false);
                    // Activate the laser on the menu hand so that the vanilla game will hide it upon button release;
                    // Hide the laser on the pointing hand directly.
                    controllerManager?.SetLeftLaserPointerActive(handRole == HandRole.LeftHand);
                    controllerManager?.SetRightLaserPointerActive(handRole == HandRole.RightHand);
                    handRole = HandRole.Invalid;
                    return;
                }

                var previousHandRole = handRole;
                if (leftButtonPressTime >= BUTTON_PRESS_TIME_THRESHOLD)
                {
                    handRole = HandRole.LeftHand;
                    hand = BetterVRPluginHelper.leftControllerCenter;
                }
                else if (rightButtonPressTime >= BUTTON_PRESS_TIME_THRESHOLD)
                {
                    handRole = HandRole.RightHand;
                    hand = BetterVRPluginHelper.rightControllerCenter;
                }

                if (handRole == HandRole.Invalid || !hand) return;

                if (handRole != previousHandRole)
                {
                    // Open the menu.
                    menu.Enable(true, true, false);

                    // Scale to the right size.
                    if (originalScale == null) originalScale = menu.transform.localScale;
                    Vector3 newScale = hand.lossyScale / 4096f;
                    if (menu.transform.parent) newScale /= menu.transform.parent.lossyScale.x;
                    menu.transform.localScale = newScale;

                    if (controllerManager == null) controllerManager = GameObject.FindObjectOfType<ControllerManager>();
                    // Activate both laser pointers and the vanilla game logic will hide the laser on the menu hand upon button release.
                    controllerManager?.SetLeftLaserPointerActive(true);
                    controllerManager?.SetRightLaserPointerActive(true);
                    controllerManager?.UpdateActivity();
                }

                // Move the menu with the hand.
                menu.transform.SetPositionAndRotation(hand.TransformPoint(0, 1f / 32, 3f / 16), hand.rotation * Quaternion.Euler(90, 0, 0));
            }
        }
    }
}
