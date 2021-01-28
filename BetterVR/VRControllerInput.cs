using UnityEngine;
using System.Collections;
using HTC.UnityPlugin.Vive;
using System;
using System.Collections.Generic;
using HTC.UnityPlugin.Utility;
using UnityEngine.XR;
using Valve.VR;
using UnityEngine.VR;

namespace BetterVR
{    
    public static class VRControllerInput
    {
        internal static GameObject VROrigin;
        internal static bool isRunning = false;

        internal static ViveRoleProperty roleR = ViveRoleProperty.New(HandRole.RightHand);
        internal static ViveRoleProperty roleL = ViveRoleProperty.New(HandRole.LeftHand);


        //Lazy wait for VR headset origin to exists
        internal static IEnumerator Init()
        {
            isRunning = true;

            while (VROrigin == null) 
            {                
                GetVROrigin();
                yield return new WaitForSeconds(1);
            }            

            isRunning = false;
        }

        internal static void CheckVROrigin(BetterVRPlugin instance)
        {
            if (isRunning) return;
            instance.StartCoroutine(VRControllerInput.Init());
        }


        internal static void GetVROrigin()
        {
            if (VROrigin == null)
            {
                VROrigin = GameObject.Find("VROrigin");
            }
        }


        //When user presses joystick (index) left or right, turn the camera
        internal static void CheckInputForSqueezeTurn()
        {
            //When squeezing the grip, apply hand rotation to the headset
            if (ViveInput.GetPressEx<HandRole>(HandRole.LeftHand, ControllerButton.Grip))
            {
                GetVROrigin();
                if (VROrigin == null) return;

                //Hand velocity along Y axis
                var velocity = VivePose.GetAngularVelocity(roleL);
                VROrigin.transform.Rotate(0f, -velocity.y/1.5f, 0f, Space.Self);
            }

            //Same for either hand
            if (ViveInput.GetPressEx<HandRole>(HandRole.RightHand, ControllerButton.Grip))
            {
                GetVROrigin();
                if (VROrigin == null) return;

                var velocity = VivePose.GetAngularVelocity(roleR);
                VROrigin.transform.Rotate(0f, -velocity.y/1.5f, 0f, Space.Self);
            }
        }
    }
}