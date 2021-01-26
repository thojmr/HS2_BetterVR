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
using Valve;
using Valve.VR.InteractionSystem;

namespace BetterVR
{
    public static class VRControllerCollider
    {        
        internal const string Left_Controller_DB_Name = "Left_Controller_DB";
        internal const string Right_Controller_DB_Name = "Right_Controller_DB";

        internal static void SetVRControllerColliderToDynamicBones() 
        {         
            //Get all dynamic bones
            DynamicBone_Ver02[] dynamicBonesV2 = GameObject.FindObjectsOfType<DynamicBone_Ver02>();
            DynamicBone[] dynamicBones = GameObject.FindObjectsOfType<DynamicBone>();
            if (dynamicBonesV2.Length == 0 && dynamicBones.Length == 0) return;

            // ViveInput
            
            var controllerRight = GameObject.Find("Controller (right)");
            var controllerLeft = GameObject.Find("Controller (left)");
            if (controllerRight == null && controllerLeft == null) return;

            //For each vr controller add dynamic bone collider to it
            DynamicBoneCollider leftControllerCollider = GetControllerCollider(controllerLeft, Left_Controller_DB_Name);
            DynamicBoneCollider rightControllerCollider = GetControllerCollider(controllerRight, Right_Controller_DB_Name);
            if (leftControllerCollider == null && rightControllerCollider == null) return;

            //For each controller, make it collidable with all dynaic bones (Did I miss any?)
            AddControllerColliderToDBv2(leftControllerCollider, dynamicBonesV2);
            AddControllerColliderToDBv2(rightControllerCollider, dynamicBonesV2);
            AddControllerColliderToDB(leftControllerCollider, dynamicBones);
            AddControllerColliderToDB(rightControllerCollider, dynamicBones);
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

        internal static DynamicBoneCollider AddDBCollider(GameObject controllerGameObject, string colliderName, float colliderRadius = 0.05f, float collierHeight = 0f, Vector3 colliderCenter = new Vector3(), DynamicBoneCollider.Direction colliderDirection = default)
        {
            //Build the dynamic bone collider
            GameObject colliderObject = new GameObject(colliderName);
            DynamicBoneCollider collider = colliderObject.AddComponent<DynamicBoneCollider>();
            collider.m_Radius = colliderRadius;
            collider.m_Height = collierHeight;
            collider.m_Center = colliderCenter;
            collider.m_Direction = colliderDirection;
            colliderObject.transform.SetParent(controllerGameObject.transform, false);
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