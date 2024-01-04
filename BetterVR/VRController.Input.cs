using TMPro;
using HTC.UnityPlugin.Vive;
using System;
using UnityEngine;

namespace BetterVR
{
    public static class VRControllerInput
    {
        internal static ViveRoleProperty roleH { get; private set; } = ViveRoleProperty.New(DeviceRole.Hmd);
        internal static ViveRoleProperty roleR { get; private set; } = ViveRoleProperty.New(HandRole.RightHand);
        internal static ViveRoleProperty roleL { get; private set; } = ViveRoleProperty.New(HandRole.LeftHand);
        internal static bool isDraggingScale { get { return twoHandedWorldGrab != null && twoHandedWorldGrab.canScale; } }
        private static Vector3? lastVrOriginPosition;
        private static Quaternion? lastVrOriginRotation;
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

        internal static void RecordVrOriginTransform()
        {
            Transform vrOrigin = BetterVRPluginHelper.VROrigin?.transform;

            if (vrOrigin == null) return;

            lastVrOriginPosition = vrOrigin.position;
            lastVrOriginRotation = vrOrigin.rotation;
        }

        internal static void ClearRecordedVrOriginTransform()
        {
            lastVrOriginPosition = null;
            lastVrOriginRotation = null;
        }

        internal static void MaybeRestoreVrOriginTransform()
        {
            if (Manager.Config.HData.InitCamera)
            {
                return;
            }

            Transform vrOrigin = BetterVRPluginHelper.VROrigin?.transform;
            if (vrOrigin == null) return;

            if (lastVrOriginPosition != null && lastVrOriginRotation != null)
            {
                // Force restoring last known camera transform before animation change
                // since vanilla game erroneously resets camera after changing animation
                // even if the camera init option is toggled off.
                vrOrigin.SetPositionAndRotation((Vector3)lastVrOriginPosition, (Quaternion)lastVrOriginRotation);
            }

            // Stop attempting to restore camera transform if there is any input that might move the camera.
            if (BetterVRPluginHelper.LeftHandGripPress() || BetterVRPluginHelper.RightHandGripPress() ||
                Mathf.Abs(BetterVRPluginHelper.GetRightHandPadStickCombinedOutput().x) > 0.25f || !Manager.HSceneManager.isHScene)
            {
                ClearRecordedVrOriginTransform();
            }
        }

        /// <summary>
        /// Handles world scaling, rotation, and locomotion when user squeezes the grip
        /// </summary>
        internal static void UpdateSqueezeMovement()
        {
            Transform vrOrigin = BetterVRPluginHelper.VROrigin?.transform;
            if (!vrOrigin) return;

            bool leftHandFullGrab = BetterVRPluginHelper.LeftHandGripPress() && BetterVRPluginHelper.LeftHandTriggerPress();
            bool rightHandFullGrab = BetterVRPluginHelper.RightHandGripPress() && BetterVRPluginHelper.RightHandTriggerPress();
            bool bothGrips = BetterVRPluginHelper.LeftHandGripPress() && BetterVRPluginHelper.RightHandGripPress();
            
            bool twoHandedTurn = BetterVRPlugin.IsTwoHandedTurnEnabled() && bothGrips;
            bool shouldScale = leftHandFullGrab && rightHandFullGrab;

            twoHandedWorldGrab.enabled = shouldScale || twoHandedTurn;
            twoHandedWorldGrab.canScale = shouldScale;

            bool allowOneHandedWorldGrab =
                !twoHandedWorldGrab.enabled && (BetterVRPlugin.IsOneHandedTurnEnabled() || BetterVRPlugin.IsTwoHandedTurnEnabled());

            // Check right hand
            var rightControllerModel = BetterVRPluginHelper.FindRightControllerRenderModel(out var rCenter);
            if (rightControllerModel)
            {
                rightControllerModel.GetOrAddComponent<OneHandedWorldGrab>().enabled =
                    rightHandFullGrab && !leftHandFullGrab && allowOneHandedWorldGrab;
            }

            // Check left hand
            var leftControllerModel = BetterVRPluginHelper.FindLeftControllerRenderModel(out var lCenter);
            if (leftControllerModel)
            {
                leftControllerModel.GetOrAddComponent<OneHandedWorldGrab>().enabled =
                    leftHandFullGrab && !rightHandFullGrab && allowOneHandedWorldGrab;
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
            Vector3 desiredControllerPosition;

            void Awake()
            {
                (worldPivot = new GameObject().transform).parent = transform;
                (vrOrginPlacer = new GameObject().transform).parent = new GameObject().transform;
            }

            void OnEnable()
            {
                // Place the world pivot at neutral rotation.
                worldPivot.rotation = Quaternion.identity;
                // Pivot the world around the controller.
                worldPivot.localPosition = Vector3.zero;

                desiredControllerPosition = worldPivot.position;
            }

            void OnRenderObject()
            {
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
    }
}
