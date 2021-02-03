using UnityEngine;
using IllusionUtility.GetUtility;

namespace BetterVR
{
    public static class VRControllerCollider
    {        

        /// <summary>
        /// Searches for dynamic bones, and when found links them to the colliders set on the controllers
        /// </summary>
        internal static void SetVRControllerColliderToDynamicBones() 
        {         
            //Get all dynamic bones
            var dynamicBonesV2 = GameObject.FindObjectsOfType<DynamicBone_Ver02>();
            var dynamicBones = GameObject.FindObjectsOfType<DynamicBone>();
            if (dynamicBonesV2.Length == 0 && dynamicBones.Length == 0) return;

            //Get the top level VR game object
            var VROrigin = BetterVRPluginHelper.GetVROrigin();
            if (VROrigin == null) return;

            //Get the controller objects we want to attach colliders to.transform.gameObject
            var leftHand = BetterVRPluginHelper.GetLeftHand();
            var rightHand = BetterVRPluginHelper.GetRightHand();
            if (leftHand == null && rightHand == null) return;

            if (BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" Found hand for collider");
                    
            //Attach a dynamic bone collider to each, then link that to all dynamic bones
            if (leftHand) AttachToControllerAndLink(leftHand, leftHand.GetInstanceID().ToString(), dynamicBones, dynamicBonesV2);            
            if (rightHand) AttachToControllerAndLink(rightHand, rightHand.GetInstanceID().ToString(), dynamicBones, dynamicBonesV2);         
        }


        /// <summary>
        /// Gets the transform of the squeeze button
        /// </summary>
        internal static Transform GetColliderPosition(GameObject hand) 
        {
            //render location
            var renderTf = hand.transform.FindLoop("OpenVRRenderModel");
            if (renderTf == null) return null;        
             
            return renderTf;
        }


        /// <summary>
        /// Adds the colliders to the controllers, and then links the dynamic bones
        /// </summary>
        internal static void AttachToControllerAndLink(GameObject controller, string name, DynamicBone[] dynamicBones, DynamicBone_Ver02[] dynamicBonesV2) 
        {
            //For each vr controller add dynamic bone collider to it
            var controllerCollider = GetOrAttachCollider(controller, name);
            if (controllerCollider == null) return;

            //For each controller, make it collidable with all dynaic bones (Did I miss any?)
            AddControllerColliderToDBv2(controllerCollider, dynamicBonesV2);
            AddControllerColliderToDB(controllerCollider, dynamicBones);
        }


        /// <summary>
        /// Checks for existing controller collider, or creates them
        /// </summary>
        internal static DynamicBoneCollider GetOrAttachCollider(GameObject controllerGameObject, string colliderName) 
        {
            if (controllerGameObject == null) return null;

            //Check for existing DB collider that may have been attached earlier
            var existingDBCollider = controllerGameObject.GetComponentInChildren<DynamicBoneCollider>();
            if (existingDBCollider == null) 
            {
                //Add a DB collider to the controller
                return AddDBCollider(controllerGameObject, colliderName);
            }

            return existingDBCollider;
        }


        /// <summary>
        /// Adds a dynamic bone collider to a controller GO (Thanks Anon11)
        /// </summary>
        internal static DynamicBoneCollider AddDBCollider(GameObject controllerGameObject, string colliderName, float colliderRadius = 0.1f, float collierHeight = 0f, 
                                                          Vector3 colliderCenter = new Vector3(), DynamicBoneCollider.Direction colliderDirection = default)
        {
            var renderModelTf = GetColliderPosition(controllerGameObject);
            if (renderModelTf == null) return null;

            //Build the dynamic bone collider
            var colliderObject = new GameObject(colliderName);
            var collider = colliderObject.AddComponent<DynamicBoneCollider>();
            collider.m_Radius = colliderRadius;
            collider.m_Height = collierHeight;
            collider.m_Center = colliderCenter;
            collider.m_Direction = colliderDirection;
            colliderObject.transform.SetParent(renderModelTf.transform, false);
            
            //Move the collider more into the hand for the index controller
            var localPos = colliderObject.transform.up * 0.8f + colliderObject.transform.forward * -0.075f;
            colliderObject.transform.localPosition = localPos; 

            if (BetterVRPlugin.debugLog) DebugTools.DrawSphereAndAttach(renderModelTf.transform, colliderRadius, localPos);            

            return collider;
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

            if (newDBCount > 0 &&  BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" Linked {newDBCount} new V2 colliders");
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

            if (newDBCount > 0 &&  BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" Linked {newDBCount} new V1 colliders");
        }

    }    
}