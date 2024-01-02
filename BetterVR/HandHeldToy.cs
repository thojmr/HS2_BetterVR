using HTC.UnityPlugin.Vive;
using UnityEngine;

namespace BetterVR
{
    public class HandHeldToy : MonoBehaviour
    {
        private const float RADIUS = 0.15f;
        private const float HEIGHT = 3f;
        private const float GRAB_RANGE = 0.5f;

        private int mode = 0;
        private GameObject simpleModel;
        private GameObject fullModel;
        private HSpeedGesture hSpeedGesture;

        internal DynamicBoneCollider collider { get; private set; }

        void Awake()
        {
            CreateSimpleModel();
            TryCreateFullModel();
            CreateCollider();
            simpleModel.SetActive(false);
            fullModel?.SetActive(false);
            collider.enabled = false;
            hSpeedGesture.enabled = false;
        }

        void Update()
        {
            if (mode == 0) return;

            if (ViveInput.GetPressDownEx<HandRole>(HandRole.LeftHand, ControllerButton.Grip) && !VRControllerInput.isDraggingScale)
            {
                AttachAndBringToRangeOf(BetterVRPluginHelper.FindLeftControllerRenderModel(out var center));
                hSpeedGesture.roleProperty = VRControllerInput.roleL;
            }
            else if (ViveInput.GetPressDownEx<HandRole>(HandRole.RightHand, ControllerButton.Grip) && !VRControllerInput.isDraggingScale)
            {
                hSpeedGesture.roleProperty = VRControllerInput.roleR;
                AttachAndBringToRangeOf(BetterVRPluginHelper.FindRightControllerRenderModel(out var center));
            }
            else if (
                 VRControllerInput.isDraggingScale ||
                (!ViveInput.GetPressEx<HandRole>(HandRole.LeftHand, ControllerButton.Grip) &&
                !ViveInput.GetPressEx<HandRole>(HandRole.RightHand, ControllerButton.Grip)))
            {
                transform.SetParent(null, worldPositionStays: true);
                hSpeedGesture.enabled = false;
            } 
            else
            {
                hSpeedGesture.enabled = (mode != 0);
            }
        }

        internal void CycleMode(bool isRightHand)
        {
            mode = (mode + 1) % 3;

            if (mode == 1)
            {
                // Bring the newly appeared object into range of hand.
                if (isRightHand)
                {
                    AttachAndBringToRangeOf(BetterVRPluginHelper.FindRightControllerRenderModel(out var center));
                }
                else
                {
                    AttachAndBringToRangeOf(BetterVRPluginHelper.FindLeftControllerRenderModel(out var center));
                }
                transform.SetParent(null, worldPositionStays: true);
            }

            TryCreateFullModel();
            if (!fullModel && mode == 1) mode = 2;

            fullModel?.SetActive(mode == 1);
            simpleModel.SetActive(mode == 2);
            collider.enabled = (mode != 0);

            BetterVRPluginHelper.UpdatePlayerColliderActivity();
        }

        private void CreateSimpleModel()
        {
            simpleModel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            simpleModel.transform.parent = transform;
            simpleModel.transform.localScale = new Vector3(RADIUS * 2, HEIGHT / 2 - RADIUS, RADIUS * 2);
            simpleModel.transform.localPosition = Vector3.zero;
            simpleModel.GetOrAddComponent<MeshRenderer>().GetOrAddComponent<BetterVRPluginHelper.SilhouetteMaterialSetter>();
            Destroy(simpleModel.GetComponent<Collider>());

            var frontCap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            frontCap.transform.parent = simpleModel.transform;
            frontCap.transform.localScale = new Vector3(1, simpleModel.transform.localScale.x / simpleModel.transform.localScale.y, 1);
            frontCap.transform.localPosition = Vector3.up;
            frontCap.GetOrAddComponent<MeshRenderer>().GetOrAddComponent<BetterVRPluginHelper.SilhouetteMaterialSetter>();
            Destroy(frontCap.GetComponent<Collider>());

            var rearCap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rearCap.transform.parent = simpleModel.transform;
            rearCap.transform.localScale = new Vector3(1, simpleModel.transform.localScale.x / simpleModel.transform.localScale.y, 1);
            rearCap.transform.localPosition = Vector3.down;
            rearCap.GetOrAddComponent<MeshRenderer>().GetOrAddComponent<BetterVRPluginHelper.SilhouetteMaterialSetter>();
            Destroy(rearCap.GetComponent<Collider>());

            hSpeedGesture = gameObject.AddComponent<HSpeedGesture>();
            hSpeedGesture.capsuleStart = rearCap.transform;
            hSpeedGesture.capsuleEnd = frontCap.transform;
            hSpeedGesture.activationRadius = RADIUS;
        }

        private void TryCreateFullModel()
        {
            if (fullModel) return;

            GameObject prefab = null;
            try
            {
                prefab = AssetBundleManager.LoadAssetBundle(AssetBundleNames.HH_Item01)?.Bundle?.LoadAsset<GameObject>(
                    "assets/illusion/assetbundle/hscene/h/h_item/01/p_item_vibe.prefab");
            } catch {
                BetterVRPlugin.Logger.LogWarning("Cannot find toy prefab");
                return;
            }
            
            if (!prefab) return;

            var renderer = prefab.GetComponentInChildren<MeshRenderer>();
            if (renderer != null)
            {
                fullModel = GameObject.Instantiate(renderer.gameObject);
            }
            else
            {
                var skinnedMeshRenderer = prefab.GetComponentInChildren<SkinnedMeshRenderer>();
                if (!skinnedMeshRenderer) return;

                fullModel = GameObject.Instantiate(skinnedMeshRenderer.gameObject);
                var mesh = skinnedMeshRenderer.sharedMesh;
                var materials = skinnedMeshRenderer.materials;
                Destroy(fullModel.GetComponent<SkinnedMeshRenderer>());
                fullModel.AddComponent<MeshFilter>().mesh = mesh;
                fullModel.AddComponent<MeshRenderer>().materials = materials;
            }

            fullModel.transform.parent = transform;
            fullModel.transform.position += transform.position - fullModel.GetComponent<MeshRenderer>().bounds.center;
        }

        private void CreateCollider()
        {
            collider = gameObject.AddComponent<DynamicBoneCollider>();
            collider.m_Direction = DynamicBoneColliderBase.Direction.Y;
            collider.m_Radius = RADIUS;
            collider.m_Height = HEIGHT;
        }

        private void AttachAndBringToRangeOf(Transform parent)
        {
            transform.SetParent(parent, worldPositionStays: true);
            if (parent != null && transform.localPosition.magnitude > GRAB_RANGE)
            {
                transform.localPosition = Vector3.zero;
            }
        }
    }
}
