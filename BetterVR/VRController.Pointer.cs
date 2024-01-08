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

            laserSync = raycaster.GetOrAddComponent<LaserSync>();
            laserSync.source = new GameObject(raycaster.name + "_originalTransform").transform;
            laserSync.source.parent = controllerCenter;
            laserSync.source.SetPositionAndRotation(raycaster.transform.position, raycaster.transform.rotation);
            
            BetterVRPlugin.Logger.LogInfo("Laser pointer rotation offset applied");
        }
    }

    class LaserSync : MonoBehaviour
    {
        internal Transform source; // TODO: remove
        private float offsetAngle = 0;
        
        void Update()
        {
            if (offsetAngle == BetterVRPlugin.SetVRControllerPointerAngle.Value) return;
            offsetAngle = BetterVRPlugin.SetVRControllerPointerAngle.Value;

            var oldAngles = transform.localRotation.eulerAngles;

            // Rotate the laser pointer to the desired angle.
            transform.localRotation = Quaternion.Euler(-offsetAngle, 0, 0);

            BetterVRPlugin.Logger.LogInfo("Updated laser pointer rotation: " + oldAngles + " -> " + transform.localRotation.eulerAngles);

            var stabilizer = gameObject.GetComponentInParent<HTC.UnityPlugin.PoseTracker.PoseStablizer>();
            if (stabilizer)
            {
                // The vanilla laser pointer stabilization is too aggressive and causes a laggy feel.
                // Reduce the thresholds for a better balance between stability and responsiveness.
                stabilizer.positionThreshold = 0;
                stabilizer.rotationThreshold = 0.5f;
            }          
        }
    }
}
