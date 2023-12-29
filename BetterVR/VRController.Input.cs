using BepInEx.Configuration;
using UnityEngine;
using HTC.UnityPlugin.Vive;
using IllusionUtility.GetUtility;

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

        public static bool repositioningHand { get; private set; }  = false;

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

        internal static void CheckInputForHandReposition() {
            var leftHandOffset = BetterVRPlugin.LeftHandOffset;
            var leftHandRotation = BetterVRPlugin.LeftHandRotation;
            var rightHandOffset = BetterVRPlugin.RightHandOffset;
            var rightHandRotation = BetterVRPlugin.RightHandRotation;
            var leftHandRepositioning =
                ViveInput.GetPressEx<HandRole>(HandRole.RightHand, ControllerButton.AKey) &&
                ViveInput.GetPressEx<HandRole>(HandRole.RightHand, ControllerButton.BKey) &&
                ViveInput.GetPressEx<HandRole>(HandRole.LeftHand, ControllerButton.Trigger);
            var rightHandRepositioning =
                ViveInput.GetPressEx<HandRole>(HandRole.LeftHand, ControllerButton.AKey) &&
                ViveInput.GetPressEx<HandRole>(HandRole.LeftHand, ControllerButton.BKey) &&
                ViveInput.GetPressEx<HandRole>(HandRole.RightHand, ControllerButton.Trigger);
            repositioningHand = leftHandRepositioning || rightHandRepositioning;

            if (BetterVRPluginHelper.leftGlove && BetterVRPluginHelper.leftGlove.activeInHierarchy)
            {
                UpdateHandTransform(
                    BetterVRPluginHelper.leftGlove.transform,
                    BetterVRPluginHelper.GetLeftHand(),
                    leftHandRepositioning,
                    ref leftHandOffset,
                    ref leftHandRotation);
            }

            if (BetterVRPluginHelper.rightGlove && BetterVRPluginHelper.rightGlove.activeInHierarchy)
            {
                UpdateHandTransform(
                BetterVRPluginHelper.rightGlove.transform,
                BetterVRPluginHelper.GetRightHand(),
                rightHandRepositioning,
                ref rightHandOffset,
                ref rightHandRotation);
            }
        }

        private static void UpdateHandTransform(
            Transform hand, GameObject controller, bool isRepositioning,
            ref ConfigEntry<Vector3> offset, ref ConfigEntry<Quaternion> localRotation)
        {
            if (hand == null) return;

            if (isRepositioning)
            {
                if (hand.parent != null) hand.SetParent(null, worldPositionStays: true);
            }
            else if (hand.parent == null)
            {
                var controllerRenderModel = BetterVRPluginHelper.FindControllerRenderModel(controller, out Vector3 center);
                if (controllerRenderModel == null) return;
                hand.SetParent(controllerRenderModel.parent);
                localRotation.Value = hand.localRotation;
                offset.Value = controllerRenderModel.InverseTransformVector(hand.position - center);
                BetterVRPlugin.Logger.LogInfo("Set hand offset: " + hand.localRotation + " rotation: " + hand.localRotation.eulerAngles);
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

        internal class FingerPoseUpdater : MonoBehaviour
        {
            private HandRole handRole;
            private float rotationFactor = 1;
            private Transform thumb;
            public Transform index { get; private set; }
            public Transform middle { get; private set; }
            public Transform ring { get; private set; }
            private Transform pinky;

            public DynamicBoneCollider indexCollider { get; private set; }

            internal void Init(HandRole handRole, float rotationFactor = 1)
            {
                this.handRole = handRole;
                this.rotationFactor = rotationFactor;
            }

            void Awake()
            {
                thumb = FindFirstMatchingTransform(transform, "umb");
                index = FindFirstMatchingTransform(transform, "ndex");
                middle = FindFirstMatchingTransform(transform, "iddle");
                ring = FindFirstMatchingTransform(transform, "ing");
                pinky = FindFirstMatchingTransform(transform, "ittle");

                if (index) {
                    indexCollider = new GameObject(name + "_indexCollider").AddComponent<DynamicBoneCollider>();
                    indexCollider.m_Radius = 0.01f;
                    indexCollider.m_Height = 0.075f;
                    indexCollider.m_Direction = DynamicBoneColliderBase.Direction.X;
                    var colliderParent = index.childCount > 0 ? index.GetChild(0) : index;
                    if (colliderParent.childCount > 0) colliderParent = colliderParent.GetChild(0);
                    indexCollider.transform.parent = colliderParent;
                    indexCollider.transform.localPosition = Vector3.zero;
                    indexCollider.transform.localRotation = Quaternion.identity;
                }
            }

            void Update()
            {
                float thumbAngle = 35;
                if (ViveInput.GetPressEx<HandRole>(handRole, ControllerButton.AKeyTouch) ||
                    ViveInput.GetPressEx<HandRole>(handRole, ControllerButton.BkeyTouch) ||
                    ViveInput.GetPressEx<HandRole>(handRole, ControllerButton.PadTouch) ||
                    ViveInput.GetPressEx<HandRole>(handRole, ControllerButton.MenuTouch)) thumbAngle = 15;
                if (thumb && thumb.childCount > 0) thumb.GetChild(0).localRotation = Quaternion.Euler(0, 0, thumbAngle * rotationFactor);

                float indexCurl = ViveInput.GetAxisEx<HandRole>(handRole, ControllerAxis.IndexCurl);
                float middleCurl = ViveInput.GetAxisEx<HandRole>(handRole, ControllerAxis.MiddleCurl);
                float ringCurl = ViveInput.GetAxisEx<HandRole>(handRole, ControllerAxis.RingCurl);
                float pinkyCurl = ViveInput.GetAxisEx<HandRole>(handRole, ControllerAxis.PinkyCurl);

                if (indexCurl != 0 || middleCurl != 0 || ringCurl != 0 || pinkyCurl != 0)
                {
                    UpdateAngle(index, indexCurl * 35);
                    UpdateAngle(middle, middleCurl * 60);
                    UpdateAngle(ring, ringCurl * 60);
                    UpdateAngle(pinky, pinkyCurl * 60);
                    return;
                }
                
                float indexAngle = 10;
                if (ViveInput.GetPressEx<HandRole>(handRole, ControllerButton.TriggerTouch)) indexAngle = 30;
                indexAngle += ViveInput.GetAxisEx<HandRole>(handRole, ControllerAxis.Trigger) * 5;
                
                float gripAngle = 10;
                if (ViveInput.GetPressEx<HandRole>(handRole, ControllerButton.TriggerTouch) ||
                    ViveInput.GetPressEx<HandRole>(handRole, ControllerButton.GripTouch) ||
                    ViveInput.GetPressEx<HandRole>(handRole, ControllerButton.CapSenseGripTouch)) gripAngle = 70;
                gripAngle += ViveInput.GetAxisEx<HandRole>(handRole, ControllerAxis.CapSenseGrip) * 3;

                UpdateAngle(index, indexAngle);
                UpdateAngle(middle, gripAngle);
                UpdateAngle(ring, gripAngle * 1.15f);
                UpdateAngle(pinky, gripAngle * 1.4f);
            }

            private void UpdateAngle(Transform finger, float angle)
            {
                if (finger == null) return;
                finger.localRotation = Quaternion.Euler(0, 0, angle * rotationFactor);
                if (finger.childCount > 0) UpdateAngle(finger.GetChild(0), angle * 1.0625f);
            }

            private static Transform FindFirstMatchingTransform(Transform transform, string partialName)
            {
                if (transform.name.Contains(partialName)) return transform;

                for (int i = 0; i < transform.childCount; i++)
                {
                    Transform result = FindFirstMatchingTransform(transform.GetChild(i), partialName);
                    if (result) return result;
                }
                return null;
            }
        }
    }
}
