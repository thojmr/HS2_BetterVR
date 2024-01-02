using System.Text.RegularExpressions;
using UnityEngine;

namespace BetterVR
{
	public static class VRControllerCollider
	{
		private static readonly Regex INDEX_COLLIDING_BONE_MATCHER = new Regex(@"agina|okan");
		private static DynamicBoneCollider leftControllerCollider;
		private static DynamicBoneCollider rightControllerCollider;
		private static DynamicBoneCollider floorCollider;
		private static DynamicBoneCollider mouthCollider;
		internal static Transform characterForHeightReference;

		internal static void UpdateDynamicBoneColliders()
		{
			//Get all dynamic bones
			var dynamicBonesV2 = GameObject.FindObjectsOfType<DynamicBone_Ver02>();
			var dynamicBones = GameObject.FindObjectsOfType<DynamicBone>();
			if (dynamicBonesV2.Length == 0 && dynamicBones.Length == 0)
			{
				return;
			}

			UpdateControllerColliders(dynamicBones, dynamicBonesV2);
			UpdateIndexColliders(dynamicBones, dynamicBonesV2);
			UpdateHandHeldToyCollider(dynamicBones, dynamicBonesV2);
			UpdateFloorCollider(dynamicBones, dynamicBonesV2);
			UpdateMouthCollider(dynamicBones, dynamicBonesV2);
		}

		private static void UpdateControllerColliders(DynamicBone[] dynamicBones, DynamicBone_Ver02[] dynamicBonesV2)
		{
			UpdateControllerCollider(
				ref leftControllerCollider,
				"LeftControllerCollider",
				BetterVRPluginHelper.GetLeftHand(),
				BetterVRPluginHelper.leftGlove?.GetComponent<FingerPoseUpdater>(),
				-1,
				dynamicBones,
				dynamicBonesV2);
			UpdateControllerCollider(
				ref rightControllerCollider,
				"RightControllerCollider",
				BetterVRPluginHelper.GetRightHand(),
				BetterVRPluginHelper.rightGlove?.GetComponent<FingerPoseUpdater>(),
				1,
				dynamicBones,
				dynamicBonesV2);
		}

 		private static void UpdateControllerCollider(
			ref DynamicBoneCollider collider, string name,
			GameObject controller, FingerPoseUpdater fingerPoses, float lateralFactor,
			DynamicBone[] dynamicBones, DynamicBone_Ver02[] dynamicBonesV2)
        {
			var renderModel = BetterVRPluginHelper.FindControllerRenderModel(controller, out Vector3 center);
			if (!renderModel) return;

			if (!collider)
			{
				collider = new GameObject(name).AddComponent<DynamicBoneCollider>();
				collider.m_Direction = DynamicBoneColliderBase.Direction.Z;
			}

			collider.m_Radius = BetterVRPlugin.ControllerColliderRadius.Value;
			// A height too small will cause the collider to be ignored by some dynamic bones.
			collider.m_Height = BetterVRPlugin.ControllerColliderRadius.Value * 5;

			if (collider.transform.parent != renderModel) collider.transform.parent = renderModel;

			if (fingerPoses?.middle)
			{
				collider.transform.position =
					fingerPoses.middle.position + renderModel.TransformVector(Vector3.forward * 0.005f);
			}
			else
			{
				collider.transform.position =
					center + renderModel.TransformVector(new Vector3(0.01f * lateralFactor, 0, 0.005f));
			}

			collider.enabled = BetterVRPlugin.EnableControllerColliders.Value;

			AddColliderToDB(collider, dynamicBones);
			AddColliderToDBv2(collider, dynamicBonesV2);
		}

		private static void UpdateMouthCollider(DynamicBone[] dynamicBones, DynamicBone_Ver02[] dynamicBonesV2)
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

			AddColliderToDB(mouthCollider, dynamicBones);
			AddColliderToDBv2(mouthCollider, dynamicBonesV2);
		}

		private static void UpdateFloorCollider(DynamicBone[] dynamicBones, DynamicBone_Ver02[] dynamicBonesV2)
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

			AddColliderToDB(floorCollider, dynamicBones);
			AddColliderToDBv2(floorCollider, dynamicBonesV2);
		}

		private static void UpdateIndexColliders(DynamicBone[] dynamicBones, DynamicBone_Ver02[] dynamicBonesV2)
		{
			if (VRGlove.isShowingGloves && BetterVRPluginHelper.leftGlove)
            {
				var leftIndexCollider = BetterVRPluginHelper.leftGlove.GetComponent<FingerPoseUpdater>()?.indexCollider;
				if (leftIndexCollider) leftIndexCollider.enabled = BetterVRPlugin.EnableControllerColliders.Value;
				AddColliderToDB(leftIndexCollider, dynamicBones, INDEX_COLLIDING_BONE_MATCHER);
				AddColliderToDBv2(leftIndexCollider, dynamicBonesV2, INDEX_COLLIDING_BONE_MATCHER);
			}

			if (VRGlove.isShowingGloves && BetterVRPluginHelper.rightGlove)
            {
				var rightIndexCollider = BetterVRPluginHelper.rightGlove.GetComponent<FingerPoseUpdater>()?.indexCollider;
				if (rightIndexCollider) rightIndexCollider.enabled = BetterVRPlugin.EnableControllerColliders.Value;
				AddColliderToDB(rightIndexCollider, dynamicBones, INDEX_COLLIDING_BONE_MATCHER);
				AddColliderToDBv2(rightIndexCollider, dynamicBonesV2, INDEX_COLLIDING_BONE_MATCHER);
			}
		}

		private static void UpdateHandHeldToyCollider(DynamicBone[] dynamicBones, DynamicBone_Ver02[] dynamicBonesV2)
		{
			var collider = BetterVRPluginHelper.handHeldToy?.collider;
			if (!collider || !collider.isActiveAndEnabled) return;
			AddColliderToDB(collider, dynamicBones);
			AddColliderToDBv2(collider, dynamicBonesV2);
		}

		/// <summary>
		/// Links V2 dynamic bones to a controller collider
		/// </summary>
		internal static void AddColliderToDBv2(DynamicBoneCollider collider, DynamicBone_Ver02[] dynamicBones, Regex boneNameMatcher = null)
		{
			if (collider == null) return;
			if (dynamicBones.Length == 0) return;
			
			int newDBCount = 0;

			//For each heroine dynamic bone, add controller collider
			for (int z = 0; z < dynamicBones.Length; z++)
			{
				// var toLog = "Bone " + dynamicBones[z].name + " colliders: ";
				// foreach (var c in dynamicBones[z].Colliders) toLog += c.name;
				// BetterVRPlugin.Logger.LogInfo(toLog);
                
				//Check for existing interaction
				if (dynamicBones[z].Colliders.Contains(collider)) continue;
				if (boneNameMatcher != null && !boneNameMatcher.IsMatch(dynamicBones[z].name)) continue;
				dynamicBones[z].Colliders.Add(collider);
				newDBCount++;
			}

			if (newDBCount > 0 && BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" Linked {newDBCount} new V2 colliders");
		}

		/// <summary>
		/// Links V1 dynamic bones to a controller collider
		/// </summary>
		internal static void AddColliderToDB(DynamicBoneCollider collider, DynamicBone[] dynamicBones, Regex boneNameMatcher = null)
		{
			if (collider == null) return;
			if (dynamicBones.Length == 0) return;

			int newDBCount = 0;

			//For each heroine dynamic bone, add controller collider
			for (int z = 0; z < dynamicBones.Length; z++)
			{
				// var toLog = "Bone " + dynamicBones[z].name + " colliders: ";
				// foreach (var c in dynamicBones[z].m_Colliders) toLog += c.name;
				// BetterVRPlugin.Logger.LogInfo(toLog);

				//Check for existing interaction
				if (dynamicBones[z].m_Colliders.Contains(collider)) continue;
				if (boneNameMatcher != null && !boneNameMatcher.IsMatch(dynamicBones[z].name)) continue;
				dynamicBones[z].m_Colliders.Add(collider);
				newDBCount++;
			}

			if (newDBCount > 0 && BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" Linked {newDBCount} new V1 colliders");
		}
	}
}
