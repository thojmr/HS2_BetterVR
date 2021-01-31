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
        public static IEnumerator SetAngleAfterTime(float userAngle, BetterVRPluginHelper.VR_Hand hand = BetterVRPluginHelper.VR_Hand.none)
        {
            yield return new WaitForSeconds(0.01f);
            UpdateOneOrMoreCtrlPointers(userAngle, hand);
        }


        /// <summary>
        /// Determine whether to set both hand angles or just one
        /// </summary>
        public static void UpdateOneOrMoreCtrlPointers(float userAngle, BetterVRPluginHelper.VR_Hand hand = BetterVRPluginHelper.VR_Hand.none)
        {
            if (hand == BetterVRPluginHelper.VR_Hand.none)
            {
                GetHandAndSetAngle(userAngle, BetterVRPluginHelper.VR_Hand.left);
                GetHandAndSetAngle(userAngle, BetterVRPluginHelper.VR_Hand.right);
            }
            else 
            {
                GetHandAndSetAngle(userAngle, hand);
            }
        }


        /// <summary>
        /// Sets the angle of the laser pointer on the VR controller to the users configured value
        /// </summary>
        public static void GetHandAndSetAngle(float userAngle, BetterVRPluginHelper.VR_Hand hand)
        {   
            //Get the correct hand
            var vrHand = BetterVRPluginHelper.GetHand(hand);

            //Get all children components
            var children = vrHand.GetComponentsInChildren<Component>();
            
            //Find the component one named laser pointer
            var laserPointerComponent = children?.FirstOrDefault(x => x.name == "LaserPointer");
            if (laserPointerComponent == null) return;
            
            SetControllerPointerAngle(userAngle, laserPointerComponent);
        }        


        /// <summary>
        /// Sets the controller laser pointer angle for a single hand
        /// </summary>
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