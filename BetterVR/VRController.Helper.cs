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
    public static class VRControllerHelper
    {

        public static void SetControllerPointerAngle(float angle)
        {           
            //Potentially important Hs2 classes
            //ControllerManager  has button input triggers, and the laser pointer
            //ControllerManagerSample   same thing?
            //ShowMenuOnClick   shows controller GUI
            //LaserPointer  -> lineRenderer
            //vrTest




            //  ViveInput.GetAxis
            var VROrigin = GameObject.Find("VROrigin");
            if (VROrigin == null) return;

            var children = VROrigin.GetComponentsInChildren<Component>();
            // foreach(var child in children)
            // {
            //     if (BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" origin childComponent  {child.name}");        
            // }

            // if (BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" "); 

            // var lineRenderers = VROrigin.GetComponentsInChildren<LineRenderer>();
            // foreach(var lr in lineRenderers)
            // {
            //     if (BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" origin lineRenderers  {lr.name}");        
            // }

            //Set the laser pointer angle
            var laserPointer = children.FirstOrDefault(x => x.name == "LaserPointer");
            if (laserPointer == null) return;

            //"LaserPointer"
            var eulers = laserPointer.transform.localRotation.eulerAngles;
            //Subtract the current angle, to get the new difference, since 0 is the default
            var newAngle = angle - eulers.x;
            // laserPointer.transform.Rotate(newAngle, 0, 0, Space.Self);
            // laserPointer.transform.Rotate(Vector3.right, newAngle, Space.Self);
            laserPointer.transform.RotateAround(laserPointer.transform.position, laserPointer.transform.right, newAngle);
            // var eulers = laserPointer.transform.parent.rotation.eulerAngles;
            // laserPointer.transform.rotation = Quaternion.Euler(new Vector3(angle * -1, eulers.y, eulers.z));
            
            if (BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" ");
            if (BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" VROriginPos  {VROrigin.transform.position} LaserPointerPos  {laserPointer.transform.position}");
            if (BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" VROriginLocPos  {VROrigin.transform.localPosition} LaserPointerLocPos  {laserPointer.transform.localPosition}");
            if (BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" VROriginRot  {VROrigin.transform.rotation} LaserPointerRot  {laserPointer.transform.rotation}");
       
        }        






        internal static List<string> GetJoystickNamesContaining(string matchString)
        {
            var matchingNames = new List<string>();

            var names = Input.GetJoystickNames();
            foreach(var name in names)
            {
                if (BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" JoystickNames  {name}");
                if (name.Contains(matchString))
                {
                    matchingNames.Add(name);
                }
            }

            return matchingNames;
        }

    }
}