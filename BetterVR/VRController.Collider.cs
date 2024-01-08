using System.Linq;
using System.Text.RegularExpressions;
using Manager;
using UnityEngine;

namespace BetterVR
{
	public static class VRControllerCollider
	{
		private static readonly Regex INDEX_COLLIDING_BONE_MATCHER = new Regex(@"agina|okan|[Aa]na");
		private static DynamicBoneCollider leftControllerCollider;
		private static DynamicBoneCollider rightControllerCollider;
		private static DynamicBoneCollider floorCollider;
		private static DynamicBoneCollider mouthCollider;
		internal static Transform characterForHeightReference;

		internal static void UpdateDynamicBoneColliders()
		{
			var females = Singleton<HSceneManager>.Instance?.Hscene?.GetFemales();
			if (females == null || females.Length == 0) return;

			UpdateControllerColliders();
			UpdateIndexColliders();
			UpdateHandHeldToyCollider();
			UpdateFloorCollider();
			UpdateMouthCollider();

			foreach (var character in females)
            {
				if (character == null) continue;
				var dynamicBones = character.GetComponentsInChildren<DynamicBone>(true);
				foreach (var bone in dynamicBones) AddCollidersToBone(bone);
				var dynamicBonesV2 = character.GetComponentsInChildren<DynamicBone_Ver02>(true);
				foreach (var bone in dynamicBonesV2) AddCollidersToBone(bone);
			}
		}

		private static void UpdateControllerColliders()
		{
			UpdateControllerCollider(
				ref leftControllerCollider,
				"LeftControllerCollider",
				BetterVRPluginHelper.leftControllerCenter,
				BetterVRPluginHelper.leftGlove?.GetComponent<FingerPoseUpdater>(),
				-1);
			UpdateControllerCollider(
				ref rightControllerCollider,
				"RightControllerCollider",
				BetterVRPluginHelper.rightControllerCenter,
				BetterVRPluginHelper.rightGlove?.GetComponent<FingerPoseUpdater>(),
				1);
		}

 		private static void UpdateControllerCollider(
			ref DynamicBoneCollider collider, string name,
			Transform controllerCenter, FingerPoseUpdater fingerPoses, float lateralFactor)
        {
			if (!controllerCenter) return;

			if (!collider)
			{
				collider = new GameObject(name).AddComponent<DynamicBoneCollider>();
				collider.m_Direction = DynamicBoneColliderBase.Direction.Z;
			}

			collider.m_Radius = BetterVRPlugin.ControllerColliderRadius.Value;
			// A height too small will cause the collider to be ignored by some dynamic bones.
			collider.m_Height = BetterVRPlugin.ControllerColliderRadius.Value * 5;

			if (collider.transform.parent != controllerCenter) collider.transform.parent = controllerCenter;

			if (fingerPoses?.middle)
			{
				collider.transform.localPosition =
					controllerCenter.InverseTransformPoint(fingerPoses.middle.position) + Vector3.forward * 0.005f;
			}
			else
			{
				collider.transform.localPosition = new Vector3(0.01f * lateralFactor, 0, 0.005f);
			}

			collider.enabled = BetterVRPlugin.EnableControllerColliders.Value;
		}

		private static void UpdateMouthCollider()
		{
			if (mouthCollider == null)
			{
				mouthCollider = new GameObject("MouthCollider").AddComponent<DynamicBoneCollider>();
				mouthCollider.m_Direction = DynamicBoneColliderBase.Direction.Z;
				Transform capsuleStart = new GameObject("MouthColliderCapsuleRear").transform;
				Transform capsuleEnd = new GameObject("MouthColliderCapsuleFront").transform;
				capsuleStart.parent = capsuleEnd.parent = mouthCollider.transform;
				capsuleStart.localPosition = Vector3.back * 0.1f;
				capsuleEnd.localPosition = Vector3.forward * 0.1f;
				var h = mouthCollider.GetOrAddComponent<HSpeedGesture>();
				h.capsuleStart = capsuleStart;
				h.capsuleEnd = capsuleEnd;
				h.roleProperty = VRControllerInput.roleH;
				h.sensitivityMultiplier = 3;
			}

			mouthCollider.m_Radius = BetterVRPlugin.ControllerColliderRadius.Value;
			// A height too small will cause the collider to be ignored by some dynamic bones.
			mouthCollider.m_Height = mouthCollider.m_Radius * 3;

			var camera = BetterVRPluginHelper.VRCamera;
			if (!camera) return;

			if (mouthCollider.transform.parent != camera.transform) mouthCollider.transform.parent = camera.transform;
			mouthCollider.transform.localRotation = Quaternion.identity;
			mouthCollider.transform.localPosition = new Vector3(0, -0.08f, 0.03f);

			mouthCollider.enabled = BetterVRPlugin.EnableControllerColliders.Value;
		}

		private static void UpdateFloorCollider()
		{
			if (floorCollider == null)
			{
				floorCollider = new GameObject("FloorCollider").AddComponent<DynamicBoneCollider>();
				floorCollider.m_Radius = 50f;
				floorCollider.m_Height = 150f;
				floorCollider.m_Center = Vector3.down * 75;
				floorCollider.m_Direction = DynamicBoneColliderBase.Direction.Y;
			}

			floorCollider.transform.rotation = Quaternion.identity;
			Transform vrOrgin = BetterVRPluginHelper.VROrigin?.transform;
			if (vrOrgin)
			{
				floorCollider.transform.position =
					new Vector3(
						vrOrgin.position.x,
						characterForHeightReference == null ? vrOrgin.position.y : characterForHeightReference.position.y,
						vrOrgin.position.z);
			}
		}

		private static void UpdateIndexColliders()
		{
			if (VRGlove.isShowingGloves && BetterVRPluginHelper.leftGlove)
            {
				var leftIndexCollider = BetterVRPluginHelper.leftGlove.GetComponent<FingerPoseUpdater>()?.indexCollider;
				if (leftIndexCollider) leftIndexCollider.enabled = BetterVRPlugin.EnableControllerColliders.Value;
			}

			if (VRGlove.isShowingGloves && BetterVRPluginHelper.rightGlove)
            {
				var rightIndexCollider = BetterVRPluginHelper.rightGlove.GetComponent<FingerPoseUpdater>()?.indexCollider;
				if (rightIndexCollider) rightIndexCollider.enabled = BetterVRPlugin.EnableControllerColliders.Value;
			}
		}

		private static void UpdateHandHeldToyCollider()
		{
			var collider = BetterVRPluginHelper.handHeldToy?.collider;
			if (collider) collider.enabled = BetterVRPlugin.EnableControllerColliders.Value;
		}

		private static void AddCollidersToBone(Component bone)
		{
			AddColliderToBone(leftControllerCollider, bone);
			AddColliderToBone(rightControllerCollider, bone);
			AddColliderToBone(mouthCollider, bone);
			AddColliderToBone(floorCollider, bone);
			AddColliderToBone(BetterVRPluginHelper.handHeldToy?.collider, bone);
			AddColliderToBone(BetterVRPluginHelper.leftGlove?.GetComponent<FingerPoseUpdater>()?.indexCollider, bone, INDEX_COLLIDING_BONE_MATCHER);
			AddColliderToBone(BetterVRPluginHelper.rightGlove?.GetComponent<FingerPoseUpdater>()?.indexCollider, bone, INDEX_COLLIDING_BONE_MATCHER);
		}

		private static void AddColliderToBone(DynamicBoneCollider collider, Component bone, Regex boneNameMatcher = null)
        {
			if (collider == null) return;
			if (boneNameMatcher != null && !boneNameMatcher.IsMatch(bone.name)) return;

			if (bone is DynamicBone)
			{
				var colliders = ((DynamicBone) bone).m_Colliders;
				if (!colliders.Contains(collider)) colliders.Add(collider);
			}
			if (bone is DynamicBone_Ver02)
            {
				var colliders = ((DynamicBone_Ver02)bone).Colliders;
				if (!colliders.Contains(collider)) colliders.Add(collider);

			}
		}
	}
}
