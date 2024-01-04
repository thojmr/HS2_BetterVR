using HTC.UnityPlugin.Vive;
using UnityEngine;

namespace BetterVR
{
    public class HandHeldToy : MonoBehaviour
    {
        private const float HAND_HELD_RADIUS = 0.15f;
        private const float BODY_HELD_RADIUS = 0.5f;
        private const float HEIGHT = 3f;
        private const float GRAB_RANGE = 0.1f;
        private const float RECENTER_THRESHOLD = 0.5f;

        private static Transform bodyAttach;
        private Vector3 bodyAttachAngularVelocity;
        private Vector3 bodyAttachVelocity;

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

            if (VRControllerInput.isDraggingScale)
            {
                transform.SetParent(null, worldPositionStays: true);
                hSpeedGesture.enabled = false;
                return;
            }

            if (ViveInput.GetPressDownEx<HandRole>(HandRole.LeftHand, ControllerButton.Grip))
            {
                var controllerModel = BetterVRPluginHelper.FindLeftControllerRenderModel(out var center);
                if (controllerModel != null & GrabDistance(center) < controllerModel.transform.lossyScale.x * GRAB_RANGE)
                {
                    hSpeedGesture.roleProperty = VRControllerInput.roleL;
                    AttachAndBringToRangeOf(controllerModel);
                }
            }
            else if (ViveInput.GetPressDownEx<HandRole>(HandRole.RightHand, ControllerButton.Grip))
            {
                var controllerModel = BetterVRPluginHelper.FindRightControllerRenderModel(out var center);
                if (controllerModel != null & GrabDistance(center) < controllerModel.transform.lossyScale.x * GRAB_RANGE)
                {
                    hSpeedGesture.roleProperty = VRControllerInput.roleR;
                    AttachAndBringToRangeOf(controllerModel);
                }
               
            }
            else if (
                !IsAttachedToBody() &&
                !ViveInput.GetPressEx<HandRole>(HandRole.LeftHand, ControllerButton.Grip) &&
                !ViveInput.GetPressEx<HandRole>(HandRole.RightHand, ControllerButton.Grip))
            {
                transform.SetParent(null, worldPositionStays: true);
                hSpeedGesture.enabled = false;
            }

            if (ShouldAttachToBody()) AttachToBody();

            if (IsAttachedToBody())
            {
                CorrectBodyAttach(smooth: true);
                RotateModelsTowardTarget();
            }

            hSpeedGesture.enabled = transform.parent != null && mode != 0;
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

        private float GrabDistance(Vector3 grabPosition)
        {
            var grabOffset = transform.InverseTransformPoint(grabPosition);
            grabOffset.y = Mathf.Max(0, Mathf.Abs(grabOffset.y) - HEIGHT / 2);
            return transform.TransformVector(grabOffset).magnitude;
        }

        private bool ShouldAttachToBody()
        {
            if (transform.parent == null || mode == 0 || !hSpeedGesture) return false;

            HandRole currentHand = HandRole.Invalid;
            if (hSpeedGesture.roleProperty == VRControllerInput.roleL)
            {
                currentHand = HandRole.LeftHand;
            }
            else if (hSpeedGesture.roleProperty == VRControllerInput.roleR)
            {
                currentHand = HandRole.RightHand;
            }
            else { 
                return false;
            }

            return ViveInput.GetPressEx<HandRole>(currentHand, ControllerButton.Grip) &&
                ViveInput.GetPressEx<HandRole>(currentHand, ControllerButton.AKey);
        }

        private void AttachToBody()
        {
            if (bodyAttach == null || bodyAttach.gameObject == null)
            {
                bodyAttach = new GameObject("ToyBodyAttach").transform;
            }

            bodyAttach.parent = BetterVRPluginHelper.VROrigin?.transform;
            if (bodyAttach.parent == null) return;

            CorrectBodyAttach(smooth: false);

            hSpeedGesture.roleProperty = VRControllerInput.roleH;
            hSpeedGesture.activationRadius = BODY_HELD_RADIUS;

            transform.SetParent(bodyAttach, worldPositionStays: true);
        }

        private bool IsAttachedToBody()
        {
            return bodyAttach != null && transform.parent == bodyAttach;
        }

        private void CorrectBodyAttach(bool smooth = false)
        {
            var camera = BetterVRPluginHelper.VRCamera;
            if (bodyAttach == null || bodyAttach.parent == null || camera == null) return;
            
            // Make body attach face forward horizontally.
            var targetForward = Vector3.ProjectOnPlane(camera.transform.forward, bodyAttach.up);

            // Move body attach to the horizontal position of the camera.
            var targetPosition =
                bodyAttach.parent.position + Vector3.ProjectOnPlane(camera.transform.position - bodyAttach.parent.position, bodyAttach.parent.up);

            if (smooth) {
                targetForward = Vector3.SmoothDamp(bodyAttach.forward, targetForward, ref bodyAttachAngularVelocity, 0.25f);
                targetPosition = Vector3.SmoothDamp(bodyAttach.position, targetPosition, ref bodyAttachVelocity, 0.0625f);
            }
            else
            {
                bodyAttachVelocity = bodyAttachAngularVelocity = Vector3.zero;
            }

            bodyAttach.SetPositionAndRotation(targetPosition, Quaternion.LookRotation(targetForward, bodyAttach.parent.up));
        }

        private void ResetModelOrientation()
        {
            collider.transform.localRotation = Quaternion.identity;
            collider.transform.localPosition = Vector3.zero;
            simpleModel.transform.localRotation = Quaternion.identity;
            simpleModel.transform.localPosition = Vector3.zero;
            if (fullModel)
            {
                fullModel.transform.localRotation = Quaternion.identity;
                fullModel.transform.position += transform.position - fullModel.GetComponent<MeshRenderer>().bounds.center;
            }
        }

        private void RotateModelsTowardTarget()
        {
            var target = hSpeedGesture?.interactingCollider;

            var rotation = Quaternion.identity;
            var localPivot = Vector3.down * HEIGHT / 4;
            if (target != null && (target.name.Contains("agina") || target.name.Contains("okan")))
            {
                rotation = Quaternion.LookRotation(
                    transform.InverseTransformPoint(target.transform.position) - localPivot, Vector3.back);
                rotation = rotation * Quaternion.Euler(90, 0, 0);
            }
            
            simpleModel.transform.localRotation = Quaternion.Slerp(simpleModel.transform.localRotation, rotation, Time.deltaTime * 4);
            simpleModel.transform.localPosition = localPivot - simpleModel.transform.localRotation * localPivot;
            collider.transform.localRotation = simpleModel.transform.localRotation;
            collider.transform.localPosition = simpleModel.transform.localPosition;
            if (fullModel)
            {
                fullModel.transform.localRotation = simpleModel.transform.localRotation;
                fullModel.transform.position += simpleModel.transform.position - fullModel.GetComponent<MeshRenderer>().bounds.center;
            }
        }

        private void CreateSimpleModel()
        {
            simpleModel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            simpleModel.transform.parent = transform;
            simpleModel.transform.localScale = new Vector3(HAND_HELD_RADIUS * 2, HEIGHT / 2 - HAND_HELD_RADIUS, HAND_HELD_RADIUS * 2);
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
            hSpeedGesture.activationRadius = HAND_HELD_RADIUS;
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
            collider = new GameObject("HandHeldToyCollider").AddComponent<DynamicBoneCollider>();
            collider.m_Direction = DynamicBoneColliderBase.Direction.Y;
            collider.m_Radius = HAND_HELD_RADIUS;
            collider.m_Height = HEIGHT;
            collider.transform.parent = transform;
            collider.transform.localPosition = Vector3.zero;
            collider.transform.localRotation = Quaternion.identity;
        }

        private void AttachAndBringToRangeOf(Transform parent)
        {
            transform.SetParent(parent, worldPositionStays: true);
            if (parent != null && transform.localPosition.magnitude > RECENTER_THRESHOLD)
            {
                transform.localPosition = Vector3.zero;
            }
            hSpeedGesture.activationRadius = HAND_HELD_RADIUS;
            ResetModelOrientation();
        }
    }
}
