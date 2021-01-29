using UnityEngine;
using System;
using System.Collections;
using System.Linq;
using UniRx;

namespace BetterVR
{    
    public static class VRControllerPointer
    {

        /// <summary>
        /// Sets the angle of the laser pointer after some time to let the game objects settle
        /// </summary>
        public static IEnumerator SetAngleAfterTime(float userAngle, GameObject laserPointerGo = null)
        {
            yield return new WaitForSeconds(0.01f);
            UpdateControllerPointerAngle(userAngle, laserPointerGo);
        }


        /// <summary>
        /// Sets the angle of the laser pointer on the VR controller to the users configured value
        /// </summary>
        public static void UpdateControllerPointerAngle(float userAngle, GameObject laserPointerGo = null)
        {           
            var vrParent = laserPointerGo;
            Component laserPointerComponent;

            //If triggered by config slider, the laser pointer object needs to be found via the headset root
            if (vrParent == null) 
            {
                //Get the VROrigin GO (headset)
                vrParent = GameObject.Find("VROrigin");
                if (vrParent == null) return;
            }

            //Get all children
            var children = vrParent.GetComponentsInChildren<Component>();
// "RenderModel" GO
            //Find the one named laser pointer
            laserPointerComponent = children.FirstOrDefault(x => x.name == "LaserPointer");
            if (laserPointerComponent == null) return;
            
            SetControllerPointerAngle(userAngle, laserPointerComponent);
        }        


        public static void SetControllerPointerAngle(float userAngle, Component laserPointerComponent)
        {
            //Get the current laser pointer angle (0 is default)
            var eulers = laserPointerComponent.transform.rotation.eulerAngles;
            //Subtract the desired angle from the current angle, to get the rotational difference
            var newAngle = userAngle - eulers.x;

            //Get line renderer start position to rotate around
            var lineRenderer = laserPointerComponent.transform.gameObject.GetComponentInChildren<LineRenderer>();
            if (lineRenderer == null) return;

            //Get the starting position
            var lineRendererStartPos = laserPointerComponent.transform.TransformPoint(lineRenderer.GetPosition(0));

            if (BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" LaserPointer current {eulers.x} new {userAngle}");
            if (BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" lineRendererStartPos {lineRendererStartPos} laserPointerComponentPos {laserPointerComponent.transform.position}");

            //Rotate from the current position to the desired position
            laserPointerComponent.transform.RotateAround(lineRendererStartPos, laserPointerComponent.transform.right, newAngle);
        }

    }
}