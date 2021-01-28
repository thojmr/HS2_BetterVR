using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Config;
using Illusion.Game;
using Manager;
using UniRx;
using UniRx.Triggers;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR;
using HTC.UnityPlugin.Vive;
using HS2VR;
using Valve;
using Valve.VR;
using Valve.VR.InteractionSystem;
using Valve.VR.Extras;

namespace BetterVR
{
    public static class VRControllerCollider
    {        

        internal static void SetVRControllerColliderToDynamicBones() 
        {         
            //Get all dynamic bones
            DynamicBone_Ver02[] dynamicBonesV2 = GameObject.FindObjectsOfType<DynamicBone_Ver02>();
            DynamicBone[] dynamicBones = GameObject.FindObjectsOfType<DynamicBone>();
            if (dynamicBonesV2.Length == 0 && dynamicBones.Length == 0) return;

            //Get the top level VR game object
            var VROrigin = GameObject.Find("VROrigin");
            if (VROrigin == null) return;
            
            //Get all sphere Colliders under this parent (controller colliders)
            var childColliders = VROrigin.GetComponentsInChildren<SphereCollider>();
            foreach(var childCollider in childColliders)
            {
                if (BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" origin childCollider  {childCollider.GetInstanceID().ToString()}");        
                //Attach a dynamic bone collider to each, then link that to all dynamic bones
                AttachToControllerAndLink(childCollider.transform.parent.gameObject, childCollider.GetInstanceID().ToString(), dynamicBones, dynamicBonesV2);            
            }

        }

        internal static void AttachToControllerAndLink(GameObject controller, string name, DynamicBone[] dynamicBones, DynamicBone_Ver02[] dynamicBonesV2) 
        {
             //For each vr controller add dynamic bone collider to it
                DynamicBoneCollider controllerCollider = GetControllerCollider(controller, name);
                if (controllerCollider == null) return;

                //For each controller, make it collidable with all dynaic bones (Did I miss any?)
                AddControllerColliderToDBv2(controllerCollider, dynamicBonesV2);
                AddControllerColliderToDB(controllerCollider, dynamicBones);
        }

        internal static DynamicBoneCollider GetControllerCollider(GameObject controllerRender, string controllerDBName) 
        {
            //Create or fetch DynamicBoneColliders for the controller
            DynamicBoneCollider controllerCollider = GetOrAttachCollider(controllerRender, controllerDBName);

            return controllerCollider;
        }

        //Check for existing DB colliders on controller, if not then create one
        internal static DynamicBoneCollider GetOrAttachCollider(GameObject controllerGameObject, string colliderName) 
        {
            if (controllerGameObject == null) return null;

            //Check for existing DB collider that may have been attached earlier
            DynamicBoneCollider existingDBCollider = controllerGameObject.GetComponentInChildren<DynamicBoneCollider>();
            if (existingDBCollider == null) 
            {
                //Add a DB collider to the controller
                return AddDBCollider(controllerGameObject, colliderName);
            }

            return existingDBCollider;
        }

        internal static DynamicBoneCollider AddDBCollider(GameObject controllerGameObject, string colliderName, float colliderRadius = 0.075f, float collierHeight = 0f, Vector3 colliderCenter = new Vector3(), DynamicBoneCollider.Direction colliderDirection = default)
        {
            //Build the dynamic bone collider
            GameObject colliderObject = new GameObject(colliderName);
            DynamicBoneCollider collider = colliderObject.AddComponent<DynamicBoneCollider>();
            collider.m_Radius = colliderRadius;
            collider.m_Height = collierHeight;
            collider.m_Center = colliderCenter;
            collider.m_Direction = colliderDirection;
            colliderObject.transform.SetParent(controllerGameObject.transform, false);

            //Move the collider more into the hand for the index controller
            colliderObject.transform.localPosition = controllerGameObject.transform.up * 0.2f; 
            colliderObject.transform.localPosition = controllerGameObject.transform.forward * -0.1f; 

            return collider;
        }

        internal static void AddControllerColliderToDBv2(DynamicBoneCollider controllerCollider, DynamicBone_Ver02[] dynamicBones) 
        {
            if (controllerCollider == null) return;
            if (dynamicBones.Length == 0) return;

            //For each heroine dynamic bone, add controller collider
            for (int z = 0; z < dynamicBones.Length; z++)
            {
                //Check for existing interaction
                if (!dynamicBones[z].Colliders.Contains(controllerCollider)) {
                    dynamicBones[z].Colliders.Add(controllerCollider);
                }
            } 
        }

        internal static void AddControllerColliderToDB(DynamicBoneCollider controllerCollider, DynamicBone[] dynamicBones) 
        {
            if (controllerCollider == null) return;
            if (dynamicBones.Length == 0) return;

            //For each heroine dynamic bone, add controller collider
            for (int z = 0; z < dynamicBones.Length; z++)
            {
                //Check for existing interaction
                if (!dynamicBones[z].m_Colliders.Contains(controllerCollider)) {
                    dynamicBones[z].m_Colliders.Add(controllerCollider);
                }
            } 
        }

    }    
}