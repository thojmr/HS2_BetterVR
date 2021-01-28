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
    public static class VRControllerPointer
    {

        /// <summary>
        /// Sets the angle of the laser pointer on the VR controller to the users configured value
        /// </summary>
        public static void SetControllerPointerAngle(float userAngle)
        {           
            //Get the VROrigin GO (headset)
            var VROrigin = GameObject.Find("VROrigin");
            if (VROrigin == null) return;

            //Get all children
            var children = VROrigin.GetComponentsInChildren<Component>();

            //Find the one named laser pointer
            var laserPointer = children.FirstOrDefault(x => x.name == "LaserPointer");
            if (laserPointer == null) return;

            //Get the current laser pointer angle (0 is default)
            var eulers = laserPointer.transform.localRotation.eulerAngles;
            //Subtract the desired angle from the current angle, to get the rotational difference
            var newAngle = userAngle - eulers.x;

            //Rotate from the current position to the desired position
            laserPointer.transform.RotateAround(laserPointer.transform.position, laserPointer.transform.right, newAngle);
        }        

    }
}