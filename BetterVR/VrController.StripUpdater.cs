using AIChara;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using HTC.UnityPlugin.Vive;
using System.Collections.Generic;

namespace BetterVR
{

    public class StripUpdater
    {
        private const float STRIP_START_RANGE = 1f;
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
            // LoadClothIcons();

            Transform vrOrigin = BetterVRPluginHelper.VROrigin?.transform;
            if (vrOrigin == null) return;

            Vector3 handPos = vrOrigin.TransformPoint(VivePose.GetPose(handRole).pos);

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
            if (vrOrigin == null) return;

            if (stripIndicator == null || stripIndicator.gameObject == null)
            {
                stripIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere).GetComponent<MeshRenderer>();
                stripIndicator.transform.SetParent(vrOrigin, true);
                stripIndicator.transform.localScale = Vector3.one / 128f;
                stripIndicator.receiveShadows = false;
                stripIndicator.shadowCastingMode = ShadowCastingMode.Off;
                stripIndicator.reflectionProbeUsage = ReflectionProbeUsage.Off;
                stripIndicator.GetOrAddComponent<StripIndicatorPositionUpdater>().isLeftHand = (handRole == VRControllerInput.roleL);
                stripIndicator.gameObject.SetActive(false);
            }

            stripIndicator.gameObject.SetActive(grabbedStripCollider != null);
            if (grabbedStripCollider != null)
            {
                byte clothType = grabbedStripCollider.clothType;
                if (clothType >= 0 && clothType < STRIP_INDICATOR_COLORS.Length)
                {
                    stripIndicator.material.color = STRIP_INDICATOR_COLORS[clothType];
                    //for (int i = 0; i < clothIcons.Count; i++)
                    // {
                    //    if (i != clothType)
                    //    {
                    //        clothIcons[i].SetActive(false);
                    //        continue;
                    //    }
                    //    clothIcons[i].SetActive(true);
                    //    // if (BetterVRPluginHelper.VRCamera) clothIconCanvas.transform.LookAt(BetterVRPluginHelper.VRCamera.transform);
                    //}
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
            public bool isLeftHand;

            void Update()
            {
                Transform renderModel = BetterVRPluginHelper.FindControllerRenderModel(isLeftHand ? BetterVRPluginHelper.GetLeftHand() : BetterVRPluginHelper.GetRightHand(), out Vector3 center);
                transform.position = center + renderModel.transform.TransformVector(Vector3.forward) * 0.05f;
                if (renderModel != null && renderModel.parent != null) transform.SetParent(renderModel.parent, worldPositionStays: true);
            }
        }
    }

    public class StripColliderUpdater : MonoBehaviour
    {
        // TODO maybe use regex instead of substring
        private static readonly Dictionary<string, byte> PARTIAL_NAME_TO_CLOTH_TYPE = new Dictionary<string, byte>
            {
                { "Kosi01", 0 }, // Top
                { "Spine02", 0 }, // Top
                { "LegUp00_L", 1 }, // Bottom
                { "LegUp00_R", 1 }, // Bottom
                { "N_Chest", 2 }, // Bra
                { "Spine03", 2 }, // Bra
                { "Kokan", 3 }, // Panties
                { "agina_root", 3 }, // Panties
                { "ArmLow01_L", 4}, // Gloves
                { "ArmLow01_R", 4}, // Gloves
                { "LegLow01_L", 5 }, // Pants
                { "LegLow01_R", 5 }, // Pants
                { "LegLowRoll", 6 }, // Socks
                { "Foot01", 7 } // Shoes
            };

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

    class StripCollider : MonoBehaviour
    {
        public byte clothType { get; private set; }
        public byte stripLevel { get { return character.fileStatus.clothesState[clothType]; } }
        private ChaControl character;

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
