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
        internal static readonly Color[] STRIP_INDICATOR_COLORS =
            new Color[] { Color.blue, Color.red, Color.cyan, Color.magenta, Color.yellow, Color.green, Color.white, Color.black };
        private const float STRIP_START_RANGE = 0.5f;
        private const float STRIP_MIN_DRAG_RANGE = 0.75f;
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
                        if (BetterVRPlugin.HapticFeedbackIntensity.Value > 0)
                        {
                            ViveInput.TriggerHapticVibration(handRole, amplitude: BetterVRPlugin.HapticFeedbackIntensity.Value);
                        }
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
                        if (grabbedStripCollider.StripMore() && BetterVRPlugin.HapticFeedbackIntensity.Value > 0)
                        {
                            ViveInput.TriggerHapticVibration(handRole, amplitude: BetterVRPlugin.HapticFeedbackIntensity.Value);
                        }
                    }
                    else
                    {
                        if (grabbedStripCollider.StripLess() && BetterVRPlugin.HapticFeedbackIntensity.Value > 0)
                        {
                            ViveInput.TriggerHapticVibration(handRole, amplitude: BetterVRPlugin.HapticFeedbackIntensity.Value);
                        }
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
                var buttonIndex = (i == 3 || i == 5) ? 1 : 0;
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

            stripIndicator.gameObject.SetActive(true);
            stripIndicator.material.color = STRIP_INDICATOR_COLORS[clothType];
 
            if (clothIcons == null) finishedLoadingClothIcons = false;
            if (!finishedLoadingClothIcons) return;
            for (int i = 0; i < clothIcons.Count; i++)
            {
                if (clothIcons[i] == null)
                {
                    finishedLoadingClothIcons = false;
                    return;
                }
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
                if (stripCollider == null || !stripCollider.IsInteractable()) continue;
                if (stripCollider.stripLevel < minStripLevel || stripCollider.stripLevel > maxStripLevel) continue;
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

    internal struct ColliderAnatomy
    {
        public ColliderAnatomy(byte clothType, Vector3? scale = null, Vector3? offset = null)
        {
            this.clothType = clothType;
            this.scale = scale ?? Vector3.one;
            this.offset = offset ?? Vector3.zero;
        }

        internal byte clothType;
        internal Vector3 scale;
        internal Vector3 offset;
    }

    public class StripColliderRegistry : MonoBehaviour
    {
        const string COLLIDER_SUFFIX = "_colliderSphere";
        private static readonly Dictionary<Regex, ColliderAnatomy> NAME_MATCHER_TO_ANATOMY = new Dictionary<Regex, ColliderAnatomy>
            {
                { new Regex(@"Spine02$"), new ColliderAnatomy(0, scale: Vector3.one * 1.5f) }, // Top
                { new Regex(@"Kosi01$|Siri_[LR]$"), new ColliderAnatomy(1, Vector3.one * 1.25f) }, // Bottom
                { new Regex(@"Belly_Mid_High"), new ColliderAnatomy(1, new Vector3(1f, 0.75f, 0.5f)) }, // Bottom
                { new Regex(@"Mune_Nip01_[LR]$"), new ColliderAnatomy(2) }, // Top inner
                { new Regex(@"Kokan$|agina_root$"), new ColliderAnatomy(3) }, // Under
                { new Regex(@"Wrist_dam_[LR]$"), new ColliderAnatomy(4, scale: Vector3.one * 0.5f) }, // Gloves
                { new Regex(@"LegUp01_[LR]$"), new ColliderAnatomy(5, scale: new Vector3(1.5f, 2f, 1.5f), offset: Vector3.down * 2f) }, // Pants
                { new Regex(@"LegLow01_[LR]$"), new ColliderAnatomy(5, scale: Vector3.one * 1.25f) }, // Pants
                { new Regex(@"LegLowRoll_[LR]$"), new ColliderAnatomy(6) }, // Socks
                { new Regex(@"Foot01_[LR]$"), new ColliderAnatomy(7) }, // Shoes
                { new Regex(@"cf_J_Neck$"), new ColliderAnatomy(8, Vector3.one * 1.25f) } // No cloth, for touching only
            };

        private bool hasAddedColliders = false;
        private ChaControl character;
        private List<StripCollider> colliders = new List<StripCollider>();

        internal void Init(ChaControl chaControl)
        {
            this.character = chaControl;
            hasAddedColliders = false;
            RemoveColliders();
        }

        void Update()
        {
            if (hasAddedColliders) return;
            if (!character || character.isPlayer || !character.loadEnd) return;

            AddColliderInChildren(character.transform);
            hasAddedColliders = true;
        }

        void OnDestroy()
        {
            RemoveColliders();
        }

        public void RemoveAndDestroyCollider(StripCollider collider)
        {
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

            foreach (Regex key in NAME_MATCHER_TO_ANATOMY.Keys)
            {
                if (!key.IsMatch(transform.name) || transform.name.Contains(COLLIDER_SUFFIX)) continue;

                var anatomy = NAME_MATCHER_TO_ANATOMY[key];
                GameObject colliderObject;
                if (StripCollider.IsValidClothType(anatomy.clothType))
                {
                    var collider = StripCollider.Create(character, anatomy, transform, null);
                    colliders.Add(collider);
                    colliderObject = collider.gameObject;
                    BetterVRPlugin.Logger.LogDebug("Added strip collider for " + transform.name + " on " + character.name);
                }
                else
                {
                    colliderObject = StripCollider.CreateSphere(anatomy, transform, null);
                }
                colliderObject.name = transform.name + COLLIDER_SUFFIX;
                break;
        
            }
        }
    }

    public class StripCollider : MonoBehaviour
    {
        public byte clothType { get; private set; }
        public byte stripLevel { get { return character.fileStatus.clothesState[clothType]; } }
        private ChaControl character;

        internal static bool IsValidClothType(byte i) { return 0 <= i && i < 8;  }

        internal static StripCollider Create(
            ChaControl character, ColliderAnatomy anatomy,
            Transform parent, Transform scaleReference, bool shouldRender = false)
        {
            StripCollider collider = CreateSphere(anatomy, parent, scaleReference, shouldRender).GetOrAddComponent<StripCollider>();
            collider.Init(character, anatomy.clothType);
            return collider;
        }

        internal static GameObject CreateSphere(
            ColliderAnatomy anatomy, Transform parent, Transform scaleReference, bool shouldRender = false)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.parent = scaleReference;
            sphere.transform.localScale = anatomy.scale;
            sphere.transform.parent = parent;
            sphere.transform.localPosition = anatomy.offset;
            sphere.transform.localRotation = Quaternion.identity;
            sphere.GetOrAddComponent<Collider>().isTrigger = true;
            var renderer = sphere.GetComponent<MeshRenderer>();
            if (shouldRender)
            {
                renderer.material.color =
                    IsValidClothType(anatomy.clothType) ? StripUpdater.STRIP_INDICATOR_COLORS[anatomy.clothType] : Color.gray;
            }
            else
            {
                Object.Destroy(renderer);
            }
            return sphere;
        }

        internal void Init(ChaControl character, byte clothType)
        {
            this.character = character;
            this.clothType = clothType;
        }

        internal bool IsCharacterVisible()
        {
            return character != null && character.isActiveAndEnabled && character.visibleAll;
        }

        internal bool IsInteractable()
        {
            return IsCharacterVisible() && character.IsClothes(clothType);
        }

        internal bool StripMore()
        {
            if (stripLevel >= 2) return false;
            character.SetClothesStateNext(clothType);
            return true;
        }

        internal bool StripLess()
        {
            if (stripLevel == 0) return false;
            character.SetClothesStatePrev(clothType);
            return true;
        }

        internal void Clothe()
        {
            character.SetClothesState(clothType, 0);
        }
    }
}
