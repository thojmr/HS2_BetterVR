using UnityEngine;
using HTC.UnityPlugin.Vive;

namespace BetterVR
{
    public static class VRControllerInput
    {

        internal static ViveRoleProperty roleR = ViveRoleProperty.New(HandRole.RightHand);
        internal static ViveRoleProperty roleL = ViveRoleProperty.New(HandRole.LeftHand);
        private static bool isDraggingScale;
        private static float scaleDraggingFactor;
        private static Vector3? leftHandPositionDuringGripMovement = null;
        private static Vector3? rightHandPositionDuringGripMovement = null;
        private static Quaternion? lastLeftHandRotation;
        private static Quaternion? lastRightHandRotation;
        private static Vector3? handMidpointDuringGripMovement = null;
        private static Vector3? lastHandPositionDifference = null;
        private static Vector3? lastVrOriginPosition;
        private static Quaternion? lastVrOriginRotation;

        internal static void CheckInputForSqueezeScaling()
        {
            bool shouldDragScale =
                BetterVRPluginHelper.LeftHandTriggerPress() && BetterVRPluginHelper.LeftHandGripPress() &&
                BetterVRPluginHelper.RightHandTriggerPress() && BetterVRPluginHelper.RightHandGripPress();
            if (!shouldDragScale)
            {
                isDraggingScale = false;
                if (BetterVRPluginHelper.LeftHandGripPress()
                    && BetterVRPluginHelper.RightHandGripPress()
                    && ViveInput.GetPressEx<HandRole>(HandRole.LeftHand, ControllerButton.AKey)
                    && ViveInput.GetPressEx<HandRole>(HandRole.RightHand, ControllerButton.AKey))
                {
                    // Reset scale
                    BetterVRPlugin.PlayerLogScale.Value = (float)BetterVRPlugin.PlayerLogScale.DefaultValue;
                }
                return;
            }
            float handDistance = Vector3.Distance(VivePose.GetPose(roleL).pos, VivePose.GetPose(roleR).pos);
            
            if (!isDraggingScale)
            {
                // Start dragging scale
                isDraggingScale = true;
                scaleDraggingFactor = handDistance * BetterVRPlugin.PlayerScale;
                return;
            }

            BetterVRPlugin.PlayerScale = scaleDraggingFactor / handDistance;
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
                // Force restore last known camera transform before animation change
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

            // Check right hand
            bool shouldMoveWithRightHand = BetterVRPluginHelper.RightHandGripPress() && BetterVRPluginHelper.RightHandTriggerPress();
            var rightHandPose = VivePose.GetPose(roleR);
            Vector3 rightHandAngularVelocity =
                lastRightHandRotation == null ?
                Vector3.zero :
                GetAngularVelocityInRoomCoordinates(roleR, rightHandPose.rot * Quaternion.Inverse((Quaternion) lastRightHandRotation));
            lastRightHandRotation = rightHandPose.rot;
            UpdateMovement(
                rightHandPose.pos,
                Quaternion.AngleAxis(-rightHandAngularVelocity.magnitude * 180f / Mathf.PI * Time.deltaTime, rightHandAngularVelocity),
                ref rightHandPositionDuringGripMovement,
                shouldMoveWithRightHand);

            // Check left hand
            bool shouldMoveWithLeftHand = BetterVRPluginHelper.LeftHandGripPress() && BetterVRPluginHelper.LeftHandTriggerPress();
            var leftHandPose = VivePose.GetPose(roleL);
            Vector3 leftHandAngularVelocity =
                lastLeftHandRotation == null ?
                Vector3.zero :
                GetAngularVelocityInRoomCoordinates(roleL, leftHandPose.rot * Quaternion.Inverse((Quaternion)lastLeftHandRotation));
            lastLeftHandRotation = leftHandPose.rot;
            UpdateMovement(
                leftHandPose.pos,
                Quaternion.AngleAxis(-leftHandAngularVelocity.magnitude * 180f / Mathf.PI * Time.deltaTime, leftHandAngularVelocity),
                ref leftHandPositionDuringGripMovement,
                shouldMoveWithLeftHand);

            //Oculus input
            // if (_hand == HandRole.LeftHand)
            // {
            // 	return OVRInput.Get(OVRInput.RawAxis2D.LThumbstick, OVRInput.Controller.Active);
            // }
            // return OVRInput.Get(OVRInput.RawAxis2D.RThumbstick, OVRInput.Controller.Active);
        }

        /// <summary>
        /// When user squeezes the grips, turn the camera via hand movements
        /// </summary>
        internal static void UpdateTwoHandedMovements()
        {
            Vector3 leftHandPos = VivePose.GetPose(roleL).pos;
            Vector3 rightHandPos = VivePose.GetPose(roleR).pos;
            Vector3 currentLocalHandMidpoint = Vector3.Lerp(leftHandPos, rightHandPos, 0.5f);
            Vector3 handPositionDifference = rightHandPos - leftHandPos;
            Quaternion rotationDelta =
                lastHandPositionDifference == null ?
                Quaternion.identity :
                Quaternion.FromToRotation(handPositionDifference, (Vector3)lastHandPositionDifference);
            lastHandPositionDifference = handPositionDifference;

            UpdateMovement(
                currentLocalHandMidpoint,
                rotationDelta,
                ref handMidpointDuringGripMovement,
                BetterVRPluginHelper.LeftHandGripPress() && BetterVRPluginHelper.RightHandGripPress());
        }

        private static void UpdateMovement(Vector3 currentHandLocalPosition, Quaternion localRotationDelta, ref Vector3? handWorldPosition, bool shouldBeActive)
        {
            if (!shouldBeActive)
            {
                handWorldPosition = null;
                return;
            }

            Transform vrOrigin = BetterVRPluginHelper.VROrigin?.transform;
            if (vrOrigin == null) return;

            if (handWorldPosition == null)
            {
                handWorldPosition = vrOrigin.TransformPoint(currentHandLocalPosition);
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
            vrOrigin.Translate((Vector3)handWorldPosition - vrOrigin.TransformPoint(currentHandLocalPosition), Space.World);
        }
    
        private static Vector3 GetAngularVelocityInRoomCoordinates(ViveRoleProperty handRole, Quaternion approximateRotaitonInRoomCoordinates)
        {
            var handPose = VivePose.GetPose(handRole);
            Vector3 angularVelocity = VivePose.GetAngularVelocity(handRole);
            if (approximateRotaitonInRoomCoordinates == null)
            {
                return angularVelocity;
            }
            
            approximateRotaitonInRoomCoordinates.ToAngleAxis(out float angle, out Vector3 approximateAxisInRoomCoordinates);

            // It is uncertain whether the angular velocity is in room coordinates or controller local coordinates
            // since it depends on the platform.
            // Make an educated guess by comparing results from both assuimptions to the actual rotation delta since last update.
            // Why is the actual rotation delta just used instead?
            // Because angular velocity provides better stabilization.
            Vector3 alternativeCandidate = VivePose.GetPose(handRole).rot * angularVelocity;
            bool alternativeIsBetterFit =
                Vector3.Angle(alternativeCandidate, approximateAxisInRoomCoordinates) < Vector3.Angle(angularVelocity, approximateAxisInRoomCoordinates);

            return alternativeIsBetterFit ? alternativeCandidate : angularVelocity;
        }
    }
}
