using AIChara;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using HTC.UnityPlugin.Vive;

namespace BetterVR
{

    public class StripUpdater
    {
        private const float STRIP_START_RANGE = 0.5f;
        private const float STRIP_MIN_DRAG_RANGE = 0.75f;
        private static readonly Color[] STRIP_INDICATOR_COLORS =
            new Color[] { Color.blue, Color.red, Color.cyan, Color.magenta, Color.yellow, Color.green, Color.white, Color.black };
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

            if (BetterVRPluginHelper.VROrigin == null) return;

            Vector3 handPos =
                BetterVRPluginHelper.VROrigin.transform.TransformPoint(VivePose.GetPose(handRole).pos);

            if (!enable ||
                ViveInput.GetPress(handRole, ControllerButton.Grip)) // Disable stripping during possible movements
            {
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
                stripIndicator?.gameObject.SetActive(false);
                return;
            }
            
            if (ViveInput.GetPress(handRole, ControllerButton.Trigger))
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
                return;
            }

            grabbedStripCollider = FindClosestStripCollider(handPos, STRIP_START_RANGE * BetterVRPlugin.PlayerScale, 0, 1);
            UpdateStripIndicator();
            stripStartPos = handPos;
            canClothe = (grabbedStripCollider == null);
        }

        private void LoadClothIcons()
        {
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
                clothIconCanvas.transform.SetParent(stripIndicator.transform, false);
                clothIconCanvas.transform.localPosition = Vector3.zero;
                clothIconCanvas.transform.localRotation = Quaternion.Euler(120, 0, 0);
                clothIconCanvas.transform.localScale = Vector3.one * 6;
            }

            bool waitingForIcons = false;
            for (int i = 0; i < 8; i++)
            {
                if (clothIcons[i] != null)
                {
                    continue;
                }

                var allClothButtons = hSceneSprite.objCloth.objs;
                if (allClothButtons == null || allClothButtons.Count <= i)
                {
                    waitingForIcons = true;
                    continue;
                }
                var buttons = allClothButtons[i].buttons;
                var buttonIndex = (i == 4 || i >= 6) ? 0 : 1;
                if (buttons.Length <= buttonIndex)
                {
                    waitingForIcons = true;
                    continue;
                }

                GameObject clothIcon = GameObject.Instantiate(buttons[buttonIndex].gameObject);
                Object.Destroy(clothIcon.GetComponent<Button>());
                Object.Destroy(clothIcon.GetComponent<SceneAssist.PointerDownAction>());

                clothIcon.transform.SetParent(clothIconCanvas.transform, false);
                clothIcon.transform.localPosition = Vector3.zero;
                clothIcon.transform.localRotation = Quaternion.identity;
                clothIcon.transform.localScale = Vector3.one;

                var rectTransform = clothIcon.GetComponent<RectTransform>();
                rectTransform.anchorMin = rectTransform.anchorMax = Vector2.one * 0.5f;
                rectTransform.offsetMin = Vector2.one * -0.5f;
                rectTransform.offsetMax = Vector3.one * 0.5f;
                clothIcons[i] = clothIcon;
            }

            finishedLoadingClothIcons = !waitingForIcons;
        }

        private void UpdateStripIndicator()
        {
            Transform vrOrigin = BetterVRPluginHelper.VROrigin?.transform;
            if (vrOrigin == null) return;

            if (stripIndicator == null || stripIndicator.gameObject == null)
            {
                bool isLeftHand = handRole == VRControllerInput.roleL;
                stripIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere).GetComponent<MeshRenderer>();
                stripIndicator.transform.parent =
                    isLeftHand ?  BetterVRPluginHelper.leftCursorAttach :  BetterVRPluginHelper.rightCursorAttach;
                stripIndicator.transform.localScale = Vector3.one / 256f;
                stripIndicator.transform.localPosition = Vector3.back * 0.08f;
                stripIndicator.transform.localRotation = Quaternion.identity;
                stripIndicator.receiveShadows = false;
                stripIndicator.shadowCastingMode = ShadowCastingMode.Off;
                stripIndicator.reflectionProbeUsage = ReflectionProbeUsage.Off;
                stripIndicator.gameObject.SetActive(false);
            }

            if (grabbedStripCollider == null)
            {
                stripIndicator.gameObject.SetActive(false);
                return;
            }

            byte clothType = grabbedStripCollider.clothType;
            if (clothType < 0 || clothType > STRIP_INDICATOR_COLORS.Length)
            {
                stripIndicator.gameObject.SetActive(false);
                return;
            }

            // BetterVRPlugin.Logger.LogWarning("C: " + clothType);
            stripIndicator.gameObject.SetActive(true);
            stripIndicator.material.color = STRIP_INDICATOR_COLORS[clothType];
            for (int i = 0; i < clothIcons.Count; i++)
            {
                clothIcons[i].SetActive(i == clothType);
            }
            // if (BetterVRPluginHelper.VRCamera) clothIconCanvas.transform.LookAt(BetterVRPluginHelper.VRCamera.transform);
        }

        private static StripCollider FindClosestStripCollider(Vector3 position, float range, byte minStripLevel = 0, byte maxStripLevel = 2)
        {
            Collider[] colliders = Physics.OverlapSphere(position, range);
            StripCollider closestStripCollider = null;
            float closestDistance = Mathf.Infinity;
            foreach (Collider collider in colliders)
            {
                StripCollider stripCollider = collider.GetComponent<StripCollider>();
                if (stripCollider == null || !stripCollider.IsClothAvaiable()) continue;
                if (stripCollider.stripLevel < minStripLevel || stripCollider.stripLevel > maxStripLevel) continue;
                if (stripCollider.character == null || !stripCollider.character.isActiveAndEnabled || !stripCollider.character.visibleAll) continue;
                float distance = collider.transform.InverseTransformPoint(position).magnitude;
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestStripCollider = stripCollider;
                }
            }
            return closestStripCollider;
        }
    }

    public class StripColliderUpdater : MonoBehaviour
    {
        // TODO maybe use regex instead of substring
        private static readonly Dictionary<Regex, byte> NAME_MATCHER_TO_CLOTH_TYPE = new Dictionary<Regex, byte>
            {
                { new Regex(@"Spine02|Belly_MidHigh"), 0 }, // Top
                { new Regex(@"Kosi01|LegUp00_L|LegUp00_R"), 1 }, // Bottom
                { new Regex(@"Mune_Nip01_[LR]|N_Chest"), 2 }, // Bra
                // { "Spine03", 2 }, // Bra
                { new Regex(@"Kokan|agina_root"), 3 }, // Panties
                { new Regex(@"Wrist_dam"), 4}, // Gloves
                { new Regex(@"LegLow01_[LR]"), 5 }, // Pants
                { new Regex(@"LegLowRoll"), 6 }, // Socks
                { new Regex(@"Foot01"), 7 } // Shoes
            };

        private static readonly float[] COLLIDER_SIZE = new float[] { 1, 1, 1, 1, 0.25f, 1, 1, 1 };

        private bool hasAddedColliders = false;
        private AIChara.ChaControl character;
        private List<StripCollider> colliders = new List<StripCollider>();

        internal void Init(AIChara.ChaControl chaControl)
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
                AddColliderInChildren(character.transform);
                hasAddedColliders = true;
            }

            foreach (StripCollider collider in colliders)
            {
                if (!collider || !collider.transform) continue;
                collider.transform.localPosition = Vector3.zero;
            }
        }

        void OnDestroy()
        {
            RemoveColliders();
        }

        public void RemoveColliderIfInvalid(StripCollider collider)
        {
            if (collider.character != null && collider.gameObject && collider.transform.parent) return;
            colliders.Remove(collider);
            if (collider.gameObject) Destroy(collider.gameObject);
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

            // Remove duplicate dynamic bones.
            // var currentDbs = transform.GetComponents<DynamicBone>();
            // for (int i = 1; i < currentDbs.Length; i++) Destroy(currentDbs[i]);
            // var currentDbs2 = transform.GetComponents<DynamicBone_Ver02>();
            // for (int i = 1; i < currentDbs2.Length; i++) Destroy(currentDbs2[i]);

            foreach (Regex key in NAME_MATCHER_TO_CLOTH_TYPE.Keys)
            {
                if (key.IsMatch(transform.name))
                {
                    var clothType = NAME_MATCHER_TO_CLOTH_TYPE[key];
                    GameObject colliderSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    colliderSphere.name = transform.name + "_colliderSphere";
                    colliderSphere.transform.localScale = Vector3.one * COLLIDER_SIZE[clothType];
                    colliderSphere.transform.parent = transform;
                    Object.Destroy(colliderSphere.GetComponent<MeshRenderer>());
                    colliderSphere.GetOrAddComponent<Collider>().isTrigger = true;
                    StripCollider collider = colliderSphere.GetOrAddComponent<StripCollider>();
                    collider.Init(character, clothType);
                    colliders.Add(collider);
                    BetterVRPlugin.Logger.LogDebug("Added strip collider for " + transform.name + " on " + character.name);
                    break;
                }
            }
        }
    }

    public class StripCollider : MonoBehaviour
    {
        public byte clothType { get; private set; }
        public byte stripLevel { get { return character.fileStatus.clothesState[clothType]; } }
        public ChaControl character { get; private set; }

        internal void Init(ChaControl character, byte clothType)
        {
            this.character = character;
            this.clothType = clothType;
        }

        internal bool IsClothAvaiable()
        {
            return character.IsClothes(clothType);
        }

        internal void StripMore()
        {
            if (stripLevel < 2) character.SetClothesStateNext(clothType);
        }

        internal void StripLess()
        {
            if (stripLevel > 0) character.SetClothesStatePrev(clothType);
        }

        internal void Clothe()
        {
            character.SetClothesState(clothType, 0);
        }
    }
}
