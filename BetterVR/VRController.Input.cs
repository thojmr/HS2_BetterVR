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
        internal static bool isDraggingScale { get { return worldGrabScale != null && worldGrabScale.enabled; } }
        private static Vector3? handMidpointDuringGripMovement = null;
        private static Vector3? lastHandPositionDifference = null;
        private static Vector3? lastVrOriginPosition;
        private static Quaternion? lastVrOriginRotation;
        private static WorldGrabScale _worldGrabScale;
        private static WorldGrabScale worldGrabScale {
            get {
                if (_worldGrabScale == null || _worldGrabScale.gameObject == null) {
                    _worldGrabScale = new GameObject("WorldGrabScale").AddComponent<WorldGrabScale>();
                    _worldGrabScale.enabled = false;
                }
                return _worldGrabScale;
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

        internal static void CheckInputForSqueezeScaling()
        {
            if (!worldGrabScale) return;

            worldGrabScale.enabled =
                BetterVRPlugin.FixWorldSizeScale.Value &&
                BetterVRPluginHelper.LeftHandTriggerPress() && BetterVRPluginHelper.LeftHandGripPress() &&
                BetterVRPluginHelper.RightHandTriggerPress() && BetterVRPluginHelper.RightHandGripPress();

            if (!worldGrabScale.enabled &&
                BetterVRPluginHelper.LeftHandGripPress() &&
                BetterVRPluginHelper.RightHandGripPress() &&
                ViveInput.GetPressEx<HandRole>(HandRole.LeftHand, ControllerButton.AKey) &&
                ViveInput.GetPressEx<HandRole>(HandRole.RightHand, ControllerButton.AKey))
            {
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
        /// When user squeezes the grip, turn the camera via wrists angular veolcity
        /// </summary>
        internal static void UpdateOneHandedMovements()
        {
            Transform vrOrigin = BetterVRPluginHelper.VROrigin?.transform;
            if (!vrOrigin) return;

            bool leftHandGrabbing = BetterVRPluginHelper.LeftHandGripPress() && BetterVRPluginHelper.LeftHandTriggerPress();
            bool rightHandGrabbing = BetterVRPluginHelper.RightHandGripPress() && BetterVRPluginHelper.RightHandTriggerPress();

            // Check right hand
            var rightControllerModel = BetterVRPluginHelper.FindRightControllerRenderModel(out var rCenter);
            if (rightControllerModel)
            {
                rightControllerModel.GetOrAddComponent<WorldGrabReposition>().enabled = rightHandGrabbing && !leftHandGrabbing;
            }

            // Check left hand
            var leftControllerModel = BetterVRPluginHelper.FindLeftControllerRenderModel(out var lCenter);
            if (leftControllerModel)
            {
                leftControllerModel.GetOrAddComponent<WorldGrabReposition>().enabled = leftHandGrabbing && !rightHandGrabbing;
            }
        }

        /// <summary>
        /// When user squeezes the grips, turn the camera via hand movements
        /// </summary>
        internal static void UpdateTwoHandedMovements()
        {
            Transform vrOrigin = BetterVRPluginHelper.VROrigin?.transform;
            if (vrOrigin == null) return;

            Vector3 currentLocalHandMidpoint = handMidpointLocal;
            Vector3 handPositionDifference = VivePose.GetPose(roleR).pos - VivePose.GetPose(roleL).pos;
            Quaternion localRotationDelta =
                lastHandPositionDifference == null ?
                Quaternion.identity :
                Quaternion.FromToRotation(handPositionDifference, (Vector3)lastHandPositionDifference);
            lastHandPositionDifference = handPositionDifference;

            var shouldBeActive = BetterVRPluginHelper.LeftHandGripPress() && BetterVRPluginHelper.RightHandGripPress();

            if (!shouldBeActive)
            {
                handMidpointDuringGripMovement = null;
                return;
            }

            if (handMidpointDuringGripMovement == null)
            {
                handMidpointDuringGripMovement = vrOrigin.TransformPoint(currentLocalHandMidpoint);
                return;
            }

            if (BetterVRPlugin.AllowVerticalRotation.Value)
            {
                vrOrigin.rotation = vrOrigin.rotation * localRotationDelta;
            }
            else
            {
                vrOrigin.Rotate(0, localRotationDelta.eulerAngles.y, 0, Space.Self);
            }

            // Translate the VR origin so that the hand position in the game world stays constant.
            RestoreHandMidpointWorldPosition(handMidpointDuringGripMovement);
        }

        internal static void RestoreHandMidpointWorldPosition(Vector3? desiredWorldPosition)
        {
            var vrOrigin = BetterVRPluginHelper.VROrigin?.transform;
            if (desiredWorldPosition == null || vrOrigin == null) return;
            vrOrigin.Translate((Vector3)desiredWorldPosition - vrOrigin.TransformPoint(handMidpointLocal), Space.World);
        }
    }

    public class WorldGrabScale : MonoBehaviour {
        internal bool isDraggingScale { get; private set; }
        private float scaleDraggingFactor;
        private static TextMeshPro _scaleIndicator;
        private static TextMeshPro scaleIndicator
        {
            get
            {
                if (!_scaleIndicator || !_scaleIndicator.gameObject) _scaleIndicator = CreateScaleIndicator();
                return _scaleIndicator;
            }
        }
        private Vector3? desiredHandMidpointWorldCoordinate;

        void OnEnable()
        {
            if (scaleIndicator) scaleIndicator.enabled = true;
            scaleDraggingFactor = VRControllerInput.handDistanceLocal * BetterVRPlugin.PlayerScale;
            var vrOrigin = BetterVRPluginHelper.VROrigin?.transform;
            if (vrOrigin == null)
            {
                desiredHandMidpointWorldCoordinate = null;
            }
            else
            {
                desiredHandMidpointWorldCoordinate = vrOrigin.TransformPoint(VRControllerInput.handMidpointLocal);
            }
        }

        void OnDisable()
        {
            if (scaleIndicator) scaleIndicator.enabled = false;
        }

        void OnRenderObject()
        {
            var scale = scaleDraggingFactor / VRControllerInput.handDistanceLocal;
            BetterVRPlugin.PlayerScale = scale;
            VRControllerInput.RestoreHandMidpointWorldPosition(desiredHandMidpointWorldCoordinate);
            scaleIndicator?.SetText("" + String.Format("{0:0.000}", scale));
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

    public class WorldGrabReposition : MonoBehaviour {
        Transform worldPivot;
        Transform worldPlacer;
        Transform vrOrginPlacer;

        void Awake()
        {
            (worldPivot = new GameObject().transform).parent = transform;
            (worldPlacer = new GameObject().transform).SetParent(worldPivot, worldPositionStays: true);
            (vrOrginPlacer = new GameObject().transform).parent = worldPlacer;
        }

        void OnEnable()
        {
            // Place both the world pivot and the world placer at world rotation.
            worldPivot.rotation = Quaternion.identity;
            worldPivot.localPosition = Vector3.zero;
            // Use world placer to record the current world transform relative to the controller.
            worldPlacer.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        void OnRenderObject()
        {
            var vrOrigin = BetterVRPluginHelper.VROrigin;
            if (!vrOrigin) return;

            if (!BetterVRPlugin.AllowVerticalRotation.Value)
            {
                // Remove vertical rotation.
                var angles = worldPivot.rotation.eulerAngles;
                worldPivot.rotation = Quaternion.Euler(0, angles.y, 0);
            }

            // Use vrOrginPlacer to record the current vrOrigin rotation and position
            vrOrginPlacer.transform.SetPositionAndRotation(vrOrigin.transform.position, vrOrigin.transform.rotation);

            // Reset the world placer to where the world is and see how that affects vrOrigin placer.
            worldPlacer.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            // Restore the relative relation between the world and the controller by moving and rotating vrOrgin.
            vrOrigin.transform.SetPositionAndRotation(vrOrginPlacer.position, vrOrginPlacer.rotation);

            // Use world placer to record the current world transform relative to the controller so it can be used in the next frame.
            worldPlacer.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        }
    }

}
