using AIChara;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using HTC.UnityPlugin.Vive;
using System.Collections.Generic;

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
        private static Vector3? handMidpointDuringGripMovement = null;
        private static Vector3? lastVrOriginPosition;
        private static Quaternion? lastVrOriginRotation;

        private static Vector3 handPositionDifference {
            get {
                return VivePose.GetPose(roleR).pos - VivePose.GetPose(roleL).pos;
            }
        }

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
            float handDistance = handPositionDifference.magnitude;
            
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
                Mathf.Abs(BetterVRPluginHelper.GetRightHandPadOrStickAxis().x) > 0.25f || !Manager.HSceneManager.isHScene)
            {
                ClearRecordedVrOriginTransform();
            }
        }

        /// <summary>
        /// When user squeezes the grip, turn the camera via wrists angular veolcity
        /// </summary>
        internal static void UpdateOneHandedMovements()
        {
            // Check right hand
            bool shouldMoveWithRightHand = BetterVRPluginHelper.RightHandGripPress() && BetterVRPluginHelper.RightHandTriggerPress();
            var rightHandPose = VivePose.GetPose(roleR);
            Vector3 rightHandAngularVelocity = VivePose.GetAngularVelocity(roleR);
            UpdateMovement(
                rightHandPose.pos,
                Quaternion.AngleAxis(-rightHandAngularVelocity.magnitude * 180f / Mathf.PI * Time.deltaTime, rightHandAngularVelocity),
                ref rightHandPositionDuringGripMovement,
                shouldMoveWithRightHand);

            // Check left hand
            bool shouldMoveWithLeftHand = BetterVRPluginHelper.LeftHandGripPress() && BetterVRPluginHelper.LeftHandTriggerPress();
            var leftHandPose = VivePose.GetPose(roleL);
            Vector3 leftHandAngularVelocity = VivePose.GetAngularVelocity(roleL);
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
            Vector3 handVelocityDifference = VivePose.GetVelocity(roleR) - VivePose.GetVelocity(roleL);
            Vector3 angularVelocity = Vector3.Cross(handVelocityDifference, handPositionDifference) / handPositionDifference.sqrMagnitude * 180 / Mathf.PI;

            UpdateMovement(
                currentLocalHandMidpoint,
                Quaternion.Euler(angularVelocity * Time.deltaTime),
                ref handMidpointDuringGripMovement,
                BetterVRPluginHelper.LeftHandGripPress() && BetterVRPluginHelper.RightHandGripPress());
        }

        private static void UpdateMovement(Vector3 currentHandLocalPosition, Quaternion rotationDelta, ref Vector3? handWorldPosition, bool shouldBeActive)
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
                vrOrigin.rotation = vrOrigin.rotation * rotationDelta;
            }
            else
            {
                vrOrigin.Rotate(0, rotationDelta.eulerAngles.y, 0, Space.Self);
            }

            // Translate the VR origin so that the hand position in the game world stays constant.
            vrOrigin.Translate((Vector3)handWorldPosition - vrOrigin.TransformPoint(currentHandLocalPosition), Space.World);
        }

        public class StripUpdater
        {
            private const float STRIP_START_RANGE = 1f;
            private const float STRIP_MIN_DRAG_RANGE = 0.75f;
            private static readonly Color[] STRIP_INDICATOR_COLORS =
                new Color[] { Color.red, new Color(1, 0.5f, 0), Color.yellow, Color.green, Color.cyan, Color.blue, Color.magenta, Color.black };
            private Canvas clothIconCanvas;
            private List<GameObject> clothIcons = new List<GameObject>();
            private bool finishedLoadingClothIcons = false;
            private ViveRoleProperty handRole;
            private Vector3 stripStartPos;
            private bool canClothe;
            private StripCollider grabbedStripCollider;
            private MeshRenderer stripIndicator;

            internal StripUpdater(ViveRoleProperty handRole)
            {
                this.handRole = handRole;
            }

            internal void CheckStrip(bool enable)
            {
                LoadClothIcons();

                Transform vrOrigin = BetterVRPluginHelper.VROrigin?.transform;
                if (vrOrigin == null) return;

                Vector3 handPos = vrOrigin.TransformPoint(VivePose.GetPose(handRole).pos);

                if (!enable || ViveInput.GetPress(handRole, ControllerButton.Grip))
                {
                    // Disable stripping during possible movements
                    grabbedStripCollider = null;
                    canClothe = false;
                    stripIndicator?.gameObject.SetActive(false);
                    return;
                }

                if (ViveInput.GetPressUp(handRole, ControllerButton.Trigger))
                {
                    if (grabbedStripCollider != null && canClothe)
                    {
                        if (Vector3.Distance(handPos, stripStartPos) > STRIP_MIN_DRAG_RANGE * BetterVRPlugin.PlayerScale)
                        {
                            grabbedStripCollider.Clothe();
                        }
                    }
                    canClothe = false;
                    grabbedStripCollider = null;
                    stripIndicator.gameObject.SetActive(false);
                }
                else if (ViveInput.GetPress(handRole, ControllerButton.Trigger))
                {
                    if (canClothe)
                    {
                        grabbedStripCollider = FindClosestStripCollider(handPos, STRIP_START_RANGE * BetterVRPlugin.PlayerScale, 1, 2);
                        UpdateStripIndicator();
                    }
                    else if (grabbedStripCollider != null && Vector3.Distance(handPos, stripStartPos) > STRIP_MIN_DRAG_RANGE * BetterVRPlugin.PlayerScale)
                    {
                        if (Vector3.Angle(handPos - stripStartPos, grabbedStripCollider.transform.position - stripStartPos) > 90)
                        {
                            grabbedStripCollider.StripMore();
                        }
                        else
                        {
                            grabbedStripCollider.StripLess();
                        }
                        stripStartPos = handPos;
                    }
                }
                else
                {
                    grabbedStripCollider = FindClosestStripCollider(handPos, STRIP_START_RANGE * BetterVRPlugin.PlayerScale, 0, 1);
                    UpdateStripIndicator();
                    stripStartPos = handPos;
                    canClothe = (grabbedStripCollider == null);
                }
            }

            private void LoadClothIcons()
            {
                // The icons are not actually working for some reason as for now.

                if (finishedLoadingClothIcons || stripIndicator == null)
                {
                    return;
                }

                HSceneSprite hSceneSprite = Singleton<HSceneSprite>.Instance;
                if (!hSceneSprite || !hSceneSprite.objCloth || hSceneSprite.objCloth.objs == null)
                {
                    return;
                }

                while (clothIcons.Count < 8)
                {
                    clothIcons.Add(null);
                }

                if (!clothIconCanvas)
                {
                    clothIconCanvas = new GameObject("ClothIconCanvas").AddComponent<Canvas>();
                    clothIconCanvas.gameObject.AddComponent<CanvasScaler>();
                    clothIconCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    clothIconCanvas.transform.SetParent(stripIndicator.transform, false);
                    clothIconCanvas.transform.localPosition = Vector3.zero;
                    clothIconCanvas.transform.localRotation = Quaternion.identity;
                    clothIconCanvas.transform.localScale = Vector3.one * 256f;
                }

                bool waitingForIcons = false;
                for (int i = 0; i < 8; i++)
                {
                    if (clothIcons[i] != null)
                    {
                        continue;
                    }

                    var clothButtons = hSceneSprite.objCloth.objs;
                    if (clothButtons == null || clothButtons.Count <= i || clothButtons[i].buttons == null || clothButtons[i].buttons.Length == 0 || clothButtons[i].buttons[0] == null)
                    {
                        waitingForIcons = true;
                        continue;
                    }

                    GameObject clothIcon = GameObject.Instantiate(clothButtons[i].buttons[0].gameObject);
                    Object.Destroy(clothIcon.GetComponent<Button>());
                    Object.Destroy(clothIcon.GetComponent<SceneAssist.PointerDownAction>());

                    clothIcon.transform.SetParent(clothIconCanvas.transform, false);
                    clothIcon.transform.localPosition = Vector3.zero;
                    clothIcon.transform.localRotation = Quaternion.identity;
                    clothIcon.transform.localScale = Vector3.one;
                    clothIcons[i] = clothIcon;

                    BetterVRPlugin.Logger.LogWarning("Cloth button found " + i + " " + clothIcon.name + " " + clothIcon.GetComponent<RectTransform>().rect);
                }

                finishedLoadingClothIcons = !waitingForIcons;
            }

            private void UpdateStripIndicator()
            {
                Transform vrOrigin = BetterVRPluginHelper.VROrigin?.transform;
                if (vrOrigin == null)
                {
                    return;
                }

                if (stripIndicator == null)
                {
                    stripIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere).GetComponent<MeshRenderer>();
                    stripIndicator.transform.SetParent(vrOrigin, true);
                    stripIndicator.transform.localScale = Vector3.one / 128f;
                    stripIndicator.receiveShadows = false;
                    stripIndicator.shadowCastingMode = ShadowCastingMode.Off;
                    stripIndicator.reflectionProbeUsage = ReflectionProbeUsage.Off;
                    stripIndicator.GetOrAddComponent<StripIndicatorPositionUpdater>().handRole = handRole;
                    stripIndicator.gameObject.SetActive(false);
                }

                stripIndicator.gameObject.SetActive(grabbedStripCollider != null);
                if (grabbedStripCollider != null)
                {
                    byte clothType = grabbedStripCollider.clothType;
                    if (clothType >= 0 && clothType < STRIP_INDICATOR_COLORS.Length)
                    {
                        stripIndicator.material.color = STRIP_INDICATOR_COLORS[clothType];
                        for (int i = 0; i < clothIcons.Count; i++)
                        {
                            if (i != clothType)
                            {
                                clothIcons[i].SetActive(false);
                                continue;
                            }
                            clothIcons[i].SetActive(true);
                            // if (BetterVRPluginHelper.VRCamera) clothIconCanvas.transform.LookAt(BetterVRPluginHelper.VRCamera.transform);
                        }
                    }
                }
            }

            private static StripCollider FindClosestStripCollider(Vector3 position, float range, byte minStripLevel = 0, byte maxStripLevel = 2)
            {
                Collider[] colliders = Physics.OverlapSphere(position, range);
                StripCollider closestStripCollider = null;
                float closestDistance = Mathf.Infinity;
                foreach (Collider collider in colliders)
                {
                    StripCollider stripCollider = collider.GetComponent<StripCollider>();
                    if (stripCollider == null || stripCollider.stripLevel < minStripLevel || stripCollider.stripLevel > maxStripLevel || !stripCollider.IsClothAvaiable()) continue;
                    float distance = Vector3.Distance(position, collider.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestStripCollider = stripCollider;
                    }
                }
                return closestStripCollider;
            }

            private class StripIndicatorPositionUpdater : MonoBehaviour
            {
                public ViveRoleProperty handRole;

                void OnRenderObject()
                {
                    Transform vrOrigin = BetterVRPluginHelper.VROrigin?.transform;
                    if (vrOrigin == null) return;
                    transform.position = vrOrigin.TransformPoint(VivePose.GetPose(handRole).pos);
                }
            }
        }

        public class StripColliderUpdater : MonoBehaviour
        {
            // TODO maybe use regex instead of substring
            private static readonly Dictionary<string, byte> PARTIAL_NAME_TO_CLOTH_TYPE = new Dictionary<string, byte>
            {
                { "Kosi01", 0 },
                { "Spine02", 0 },
                { "LegUp00_L", 1 },
                { "LegUp00_R", 1 },
                { "N_Chest", 2 },
                { "Spine03", 2 },
                { "Kokan", 3 },
                { "agina_root", 3 },
                { "ArmLow01_L", 4},
                { "ArmLow01_R" +
                    "", 4},
                { "LegLow01_L", 5 },
                { "LegLow01_R", 5 },
                { "LegLowRoll", 6 },
                { "Foot01", 7 }
            };

            private bool hasAddedColliders = false;
            private AIChara.ChaControl character;
            private List<StripCollider> colliders = new List<StripCollider>();

            public void Init(AIChara.ChaControl chaControl)
            {
                this.character = chaControl;
                hasAddedColliders = false;
                RemoveColliders();
            }

            void Update()
            {
                if (!character || character.isPlayer || !character.loadEnd)
                {
                    return;
                }

                if (!hasAddedColliders)
                {
                    // LogChildTree(transform);
                    AddColliderInChildren(character.transform);
                    hasAddedColliders = true;
                }

                foreach (StripCollider collider in colliders)
                {
                    if (!collider?.transform) continue;
                    collider.transform.localPosition = Vector3.zero;
                }
            }

            void OnDestroy()
            {
                RemoveColliders();
            }

            private void RemoveColliders()
            {
                foreach (StripCollider collider in colliders)
                {
                    if (collider == null || collider.gameObject == null) continue;
                    GameObject.Destroy(collider.gameObject);
                }
                colliders.Clear();
            }

            private void AddColliderInChildren(Transform transform)
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    AddColliderInChildren(transform.GetChild(i));
                }

                foreach (string key in PARTIAL_NAME_TO_CLOTH_TYPE.Keys)
                {
                    if (transform.name.Contains(key))
                    {
                        GameObject colliderSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        colliderSphere.name = transform.name + "_colliderSphere";
                        colliderSphere.transform.localScale = Vector3.one;
                        colliderSphere.transform.parent = transform;
                        Object.Destroy(colliderSphere.GetComponent<MeshRenderer>());
                        colliderSphere.GetOrAddComponent<Collider>().isTrigger = true;
                        StripCollider collider = colliderSphere.GetOrAddComponent<StripCollider>();
                        collider.Init(character, PARTIAL_NAME_TO_CLOTH_TYPE[key]);
                        colliders.Add(collider);
                        BetterVRPlugin.Logger.LogDebug("Added strip collider for " + transform.name + " on " + character.name);
                        break;
                    }
                }

            }
        }

        private class StripCollider : MonoBehaviour
        {
            public byte clothType { get; private set;  }
            private ChaControl character;

            public byte stripLevel { get { return character.fileStatus.clothesState[clothType]; } }

            public void Init(ChaControl character, byte clothType)
            {
                this.character = character;
                this.clothType = clothType;
            }

            public bool IsClothAvaiable()
            {
                return character.IsClothes(clothType);
            }

            public bool IsStripped()
            {
                return stripLevel == 2;
            }

            public void StripMore()
            {
                if (stripLevel < 2) character.SetClothesStateNext(clothType);
            }

            public void StripLess()
            {
                if (stripLevel > 0) character.SetClothesStatePrev(clothType);
            }

            public void Clothe()
            {
                character.SetClothesState(clothType, 0);
            }
        }
    }
}
