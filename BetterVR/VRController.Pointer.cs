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
            SetControllerPointerAngle(userAngle, laserPointerGo);
        }


        /// <summary>
        /// Sets the angle of the laser pointer on the VR controller to the users configured value
        /// </summary>
        public static void SetControllerPointerAngle(float userAngle, GameObject laserPointerGo = null)
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
            
            //Get the current laser pointer angle (0 is default)
            var eulers = laserPointerComponent.transform.localRotation.eulerAngles;
            //Subtract the desired angle from the current angle, to get the rotational difference
            var newAngle = userAngle - eulers.x;

            if (BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" LaserPointer current {eulers.x} new {userAngle}");

            //Rotate from the current position to the desired position
            laserPointerComponent.transform.RotateAround(laserPointerComponent.transform.position, laserPointerComponent.transform.right, newAngle);
        }        

    }
}