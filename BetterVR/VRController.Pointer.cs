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
            var laserPointerTf = vrHand.transform.Find("LaserPointer");

            //Find the component one named laser pointer
            if (laserPointerTf == null) return;
            
            SetControllerPointerAngle(userAngle, laserPointerTf.gameObject, vrHand);
        }        


        /// <summary>
        /// Sets the controller laser pointer angle for a single hand
        /// </summary>
        public static void SetControllerPointerAngle(float userAngle, GameObject laserPointerGO, GameObject hand)
        {
            //Subtract the desired angle from the current angle, to get the rotational difference
            var rotateAmount = GetNewAngleDifference(userAngle, laserPointerGO);

            //Get line renderer start position to rotate around
            var lineRenderer = laserPointerGO.GetComponentInChildren<LineRenderer>();
            lineRenderer.useWorldSpace = false;
            if (lineRenderer == null) return;

            //Get the starting position
            var lineRendererStartPos = laserPointerGO.transform.TransformPoint(lineRenderer.GetPosition(0));
            
            if (BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" lineRenderer.StartPos {lineRendererStartPos} laserPointerGO {laserPointerGO.transform.position}");
            if (BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" line comp to start dist {Vector3.Distance(lineRendererStartPos, laserPointerGO.transform.position)}");

            //Rotate from the current position to the desired position
            laserPointerGO.transform.RotateAround(lineRendererStartPos, laserPointerGO.transform.right, rotateAmount);

            BetterVRPluginHelper.DrawSphereAndAttach(laserPointerGO.transform, 0.02f);
            BetterVRPluginHelper.DrawSphereAndAttach(lineRenderer.transform, 0.02f, lineRenderer.transform.position + lineRendererStartPos);
        }


        /// <summary>
        /// Get the differnce in rotation to the new angle
        /// </summary>
        public static float GetNewAngleDifference(float userAngle, GameObject laserPointerGO)
        {
            //Get the current laser pointer angle (0 is default)
            var eulers = laserPointerGO.transform.rotation.eulerAngles;

            if (BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" LaserPointer current {eulers.x} new {userAngle}");

            //Subtract the desired angle from the current angle, to get the rotational difference
            return userAngle - eulers.x;
        }

    }
}