using System.Collections.Generic;
using System.Linq;
using IllusionUtility.GetUtility;
using UnityEngine;

namespace BetterVR
{
	public static class VRControllerCollider
	{
		internal static Transform characterForHeightReference;
		internal static DynamicBoneCollider floorCollider;

        /// <summary>
        /// Searches for dynamic bones, and when found links them to the colliders set on the controllers
        /// </summary>
		internal static void SetVRControllerColliderToDynamicBones()
		{
			//Get all dynamic bones
			var dynamicBonesV2 = GameObject.FindObjectsOfType<DynamicBone_Ver02>();
			var dynamicBones = GameObject.FindObjectsOfType<DynamicBone>();
			Cloth[] cloths = GameObject.FindObjectsOfType<Cloth>();
			if (dynamicBonesV2.Length == 0 && dynamicBones.Length == 0 && cloths.Length == 0)
			{
				return;
			}

			UpdateFloorCollider(dynamicBones, dynamicBonesV2);

			//Get the controller objects we want to attach colliders to.transform.gameObject
			var leftHand = BetterVRPluginHelper.GetLeftHand();
			var rightHand = BetterVRPluginHelper.GetRightHand();
			if (leftHand == null && rightHand == null) return;

			if (BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" Found hand for collider");

			//Attach a dynamic bone collider to each, then link that to all dynamic bones
			if (leftHand) AttachToControllerAndLink(leftHand, leftHand.GetInstanceID().ToString(), dynamicBones, dynamicBonesV2, cloths);
			if (rightHand) AttachToControllerAndLink(rightHand, rightHand.GetInstanceID().ToString(), dynamicBones, dynamicBonesV2, cloths);
		}

        /// <summary>
        /// Gets the transform of the squeeze button
        /// </summary>
		internal static Transform GetColliderPosition(GameObject hand)
		{
			//render location
			var renderTf =
				hand.transform.FindLoop("Model") ??
				hand.transform.FindLoop("OpenVRRenderModel") ??
				hand.GetComponentInChildren<MeshFilter>()?.transform;
			if (renderTf == null) return null;
			
			return renderTf;
		}

        /// <summary>
        /// Adds the colliders to the controllers, and then links the dynamic bones
        /// </summary>
		internal static void AttachToControllerAndLink(GameObject controller, string name, DynamicBone[] dynamicBones, DynamicBone_Ver02[] dynamicBonesV2, Cloth[] cloths)
		{
			//For each vr controller add dynamic bone collider to it
			var controllerCollider = GetOrAttachCollider(controller, name);
			if (controllerCollider == null) return;

			CapsuleCollider capsuleCollider = GetOrAttachCapsuleCollider(controller, name);
			if (capsuleCollider == null) return;

			//For each controller, make it collidable with all dynaic bones (Did I miss any?)

			AddControllerColliderToDB(controllerCollider, dynamicBones);
			AddControllerColliderToDBv2(controllerCollider, dynamicBonesV2);
			AddControllerColliderToCloth(capsuleCollider, cloths);
		}

        /// <summary>
        /// Checks for existing controller collider, or creates them
        /// </summary>
		internal static DynamicBoneCollider GetOrAttachCollider(GameObject controllerGameObject, string colliderName)
		{
			if (controllerGameObject == null) return null;

			float radius = BetterVRPlugin.ControllerColliderRadius.Value;
			float height = radius * 3;

			//Check for existing DB collider that may have been attached earlier
			var existingDBCollider = controllerGameObject.GetComponentInChildren<DynamicBoneCollider>();
			if (existingDBCollider == null)
			{
				//Add a DB collider to the controller
				return AddDBCollider(controllerGameObject, colliderName, radius, height);
			}
			existingDBCollider.m_Radius = radius;
			existingDBCollider.m_Height = height;

			return existingDBCollider;
		}

		internal static CapsuleCollider GetOrAttachCapsuleCollider(GameObject controllerGameObject, string colliderName)
		{
			if (controllerGameObject == null)
			{
				return null;
			}

			float radius = BetterVRPlugin.ControllerColliderRadius.Value;
			float height = radius * 3;

			CapsuleCollider collider = controllerGameObject.GetComponentInChildren<CapsuleCollider>();
			if (collider == null)
			{
				return VRControllerCollider.AddCapsuleCollider(controllerGameObject, colliderName, radius, height);
			}
			collider.radius = radius;
			collider.height = height;

			return collider;
		}

		internal static void UpdateFloorCollider(DynamicBone[] dynamicBones, DynamicBone_Ver02[] dynamicBonesV2)
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

			for (int z = 0; z < dynamicBones.Length; z++)
			{
				if (!dynamicBones[z].m_Colliders.Contains(floorCollider))
				{
					dynamicBones[z].m_Colliders.Add(floorCollider);
				}
			}

			for (int z = 0; z < dynamicBonesV2.Length; z++)
			{
				if (!dynamicBonesV2[z].Colliders.Contains(floorCollider))
				{
					dynamicBonesV2[z].Colliders.Add(floorCollider);
				}
			}
		}

		/// <summary>
		/// Adds a dynamic bone collider to a controller GO (Thanks Anon11)
		/// </summary>
		internal static DynamicBoneCollider AddDBCollider(
			GameObject controllerGameObject,
			string colliderName,
			float colliderRadius = 0.05f,
			float colliderHeight = 0.15f,
			Vector3 colliderCenter = default(Vector3),
			DynamicBoneCollider.Direction colliderDirection = default)
		{
			var renderModelTf = GetColliderPosition(controllerGameObject);
			if (renderModelTf == null) return null;

			//Build the dynamic bone collider
			var colliderObject = new GameObject(colliderName);
			var collider = colliderObject.AddComponent<DynamicBoneCollider>();
			collider.m_Radius = colliderRadius;
			collider.m_Height = colliderHeight;
			collider.m_Center = colliderCenter;
			collider.m_Direction = colliderDirection;
			colliderObject.transform.SetParent(renderModelTf, false);

			//Move the collider more into the hand for the index controller
			// var localPos = renderModelTf.up * -0.09f + renderModelTf.forward * -0.075f;
			// var localPos = renderModelTf.forward * -0.075f;
			// colliderObject.transform.localPosition = localPos; 

			// if (BetterVRPlugin.debugLog) DebugTools.DrawSphereAndAttach(renderModelTf, colliderRadius);
			// if (BetterVRPlugin.debugLog) DebugTools.DrawLineAndAttach(renderModelTf, renderModelTf.TransformPoint(localPos), renderModelTf.position, localPos);

			return collider;
		}

		internal static CapsuleCollider AddCapsuleCollider(GameObject controllerGameObject, string colliderName, float colliderRadius = 0.05f, float collierHeight = 0.15f)
		{
			Transform colliderPosition = GetColliderPosition(controllerGameObject);
			if (colliderPosition == null)
			{
				return null;
			}
			GameObject gameObject = new GameObject(colliderName);
			CapsuleCollider capsuleCollider = gameObject.AddComponent<CapsuleCollider>();
			capsuleCollider.radius = colliderRadius;
			capsuleCollider.height = collierHeight;
			capsuleCollider.center = Vector3.zero;
			capsuleCollider.direction = 2;
			gameObject.transform.SetParent(colliderPosition, false);
			return capsuleCollider;
		}

		internal static void AddControllerColliderToCloth(CapsuleCollider controllerCollider, Cloth[] cloths)
		{
			if (controllerCollider == null || cloths.Length == 0) return;
			int newClothColliderCount = 0;
			for (int i = 0; i < cloths.Length; i++)
			{
				List<CapsuleCollider> colliders = Enumerable.ToList<CapsuleCollider>(cloths[i].capsuleColliders);
				if (!colliders.Contains(controllerCollider))
				{
					colliders.Add(controllerCollider);
					newClothColliderCount++;
				}
				cloths[i].capsuleColliders = colliders.ToArray();
			}

			if (newClothColliderCount > 0 && BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" Linked {newClothColliderCount} new cloths");
		}

		/// <summary>
		/// Links V2 dynamic bones to a controller collider
		/// </summary>
		internal static void AddControllerColliderToDBv2(DynamicBoneCollider controllerCollider, DynamicBone_Ver02[] dynamicBones)
		{
			if (controllerCollider == null) return;
			if (dynamicBones.Length == 0) return;
			
			int newDBCount = 0;

			//For each heroine dynamic bone, add controller collider
			for (int z = 0; z < dynamicBones.Length; z++)
			{
				//Check for existing interaction
				if (!dynamicBones[z].Colliders.Contains(controllerCollider))
				{
					dynamicBones[z].Colliders.Add(controllerCollider);
					newDBCount++;
				}
			}

			if (newDBCount > 0 && BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" Linked {newDBCount} new V2 colliders");
		}

		/// <summary>
		/// Links V1 dynamic bones to a controller collider
		/// </summary>
		internal static void AddControllerColliderToDB(DynamicBoneCollider controllerCollider, DynamicBone[] dynamicBones)
		{
			if (controllerCollider == null) return;
			if (dynamicBones.Length == 0) return;

			int newDBCount = 0;

			//For each heroine dynamic bone, add controller collider
			for (int z = 0; z < dynamicBones.Length; z++)
			{
				//Check for existing interaction
				if (!dynamicBones[z].m_Colliders.Contains(controllerCollider))
				{
					dynamicBones[z].m_Colliders.Add(controllerCollider);
					newDBCount++;
				}
			}

			if (newDBCount > 0 && BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" Linked {newDBCount} new V1 colliders");

		}
	}
}
