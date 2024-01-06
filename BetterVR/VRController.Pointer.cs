using UnityEngine;
using System.Collections;
using System.Reflection;

namespace BetterVR
{    
    public static class VRControllerPointer
    {

        /// <summary>
        /// Sets the angle of the laser pointer after some time to let the game objects settle
        /// </summary>
        public static IEnumerator SetLaserAngleWithDelay(BetterVRPluginHelper.VR_Hand hand)
        {
            yield return new WaitForSeconds(0.01f);
            GetHandAndSetAngle(hand);
        }

        /// <summary>
        /// Sets the angle of the laser pointer on the VR controller to the users configured value
        /// </summary>
        private static void GetHandAndSetAngle(BetterVRPluginHelper.VR_Hand vrHand)
        {
            var controller = BetterVRPluginHelper.GetHand(vrHand);
            var controllerCenter =
                vrHand == BetterVRPluginHelper.VR_Hand.left ?
                BetterVRPluginHelper.leftControllerCenter :
                BetterVRPluginHelper.rightControllerCenter;

            if (controller == null || controllerCenter == null) return;
            var raycaster = controller.GetComponentInChildren<HTC.UnityPlugin.Pointer3D.Pointer3DRaycaster>();
            if (raycaster == null) return;

            // BetterVRPluginHelper.GetRightHand()?.GetComponentInChildren<GuideLineDrawer>();
            // if (raycaster.transform.parent?.parent != controllerModel)

            var laserSync = raycaster.gameObject.GetComponent<LaserSync>();

            // Already patched.
            if (laserSync != null && laserSync.source != null && laserSync.source.parent == controllerCenter) return;
            
            // Leave the unpatched state available as an option in case there is some problem with the patch.
            if (BetterVRPlugin.SetVRControllerPointerAngle.Value == 0) return;

            if (controller.transform.Find("LaserPointer") == null) return;

            // LaserSync not only offsets the laser angle, but also reduces the lag of laser movement.
            laserSync = raycaster.GetOrAddComponent<LaserSync>();
            laserSync.source = new GameObject(raycaster.name + "_originalTransform").transform;

            // Parent the laser sync to the controller model so that its position and rotation is in sync with the controller.
            laserSync.source.parent = controllerCenter;
            laserSync.source.SetPositionAndRotation(raycaster.transform.position, raycaster.transform.rotation);
            
            BetterVRPlugin.Logger.LogInfo("Laser pointer rotation offset applied");
        }
    }

    class LaserSync : MonoBehaviour
    {
        internal Transform source;
        private float offsetAngle = 0;
        private Quaternion rotationOffset = Quaternion.identity;
        
        void Update()
        {
            if (source == null) return;

            if (offsetAngle != BetterVRPlugin.SetVRControllerPointerAngle.Value)
            {
                offsetAngle = BetterVRPlugin.SetVRControllerPointerAngle.Value;
                rotationOffset = Quaternion.Euler(-offsetAngle, 0, 0);
            } 

            // Move the object to the desired positon and rotation relative to the controller model.
            // The vanilla laser pointer stabilization is too aggressive and causes a laggy feel.
            // Updating the laser direction here also reduces soem of the lag.
            transform.SetPositionAndRotation(source.position, source.rotation * rotationOffset);
        }
    }
}
