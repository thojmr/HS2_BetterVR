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

        internal static ViveRoleProperty roleR = ViveRoleProperty.New(HandRole.RightHand);
        internal static ViveRoleProperty roleL = ViveRoleProperty.New(HandRole.LeftHand);


        //Lazy wait for VR headset origin to exists
        internal static IEnumerator Init()
        {
            while (VROrigin == null) 
            {                
                VROrigin = GameObject.Find("VROrigin");

                yield return new WaitForSeconds(3);
            }            
        }


        //When user presses joystick (index) left or right, turn the camera
        internal static void CheckInputForSqueezeTurn()
        {
            //When squeezing the grip, apply hand rotation to the headset
            if (ViveInput.GetPressEx<HandRole>(HandRole.LeftHand, ControllerButton.Grip))
            {
                //Hand velocity along Y axis
                var velocity = VivePose.GetAngularVelocity(roleL);
                VROrigin.transform.Rotate(0f, velocity.y, 0f, Space.Self);
            }

            //Same for either hand
            if (ViveInput.GetPressEx<HandRole>(HandRole.RightHand, ControllerButton.Grip))
            {
                var velocity = VivePose.GetAngularVelocity(roleR);
                VROrigin.transform.Rotate(0f, velocity.y, 0f, Space.Self);
            }
        }
    }
}