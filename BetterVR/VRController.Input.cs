using UnityEngine;
using System.Collections;
using HTC.UnityPlugin.Vive;

namespace BetterVR
{    
    public static class VRControllerInput
    {
        internal static bool isRunning = false;

        internal static ViveRoleProperty roleR = ViveRoleProperty.New(HandRole.RightHand);
        internal static ViveRoleProperty roleL = ViveRoleProperty.New(HandRole.LeftHand);


        /// <summary>
        /// Lazy wait for VR headset origin to exists
        /// </summary>
        internal static IEnumerator Init()
        {
            isRunning = true;

            while (BetterVRPluginHelper.VROrigin == null) 
            {                
                BetterVRPluginHelper.GetVROrigin();
                yield return new WaitForSeconds(1);
            }            

            BetterVRPluginHelper.FixWorldScale();

            isRunning = false;
        }

        internal static void CheckVROrigin(BetterVRPlugin instance)
        {
            if (isRunning) return;
            instance.StartCoroutine(VRControllerInput.Init());
        }

        
        /// <summary>
        /// When user presses joystick (index) left or right, turn the camera
        /// </summary>
        internal static void CheckInputForSqueezeTurn()
        {
            //When squeezing the grip, apply hand rotation to the headset
            if (ViveInput.GetPressEx<HandRole>(HandRole.LeftHand, ControllerButton.Grip))
            {
                BetterVRPluginHelper.GetVROrigin();
                if (BetterVRPluginHelper.VROrigin == null) return;

                //Hand velocity along Y axis
                var velocity = VivePose.GetAngularVelocity(roleL);
                BetterVRPluginHelper.VROrigin.transform.Rotate(0f, -velocity.y/1.5f, 0f, Space.Self);
            }

            //Same for either hand
            if (ViveInput.GetPressEx<HandRole>(HandRole.RightHand, ControllerButton.Grip))
            {
                BetterVRPluginHelper.GetVROrigin();
                if (BetterVRPluginHelper.VROrigin == null) return;

                var velocity = VivePose.GetAngularVelocity(roleR);
                BetterVRPluginHelper.VROrigin.transform.Rotate(0f, -velocity.y/1.5f, 0f, Space.Self);
            }

                //Oculus input
                // if (_hand == HandRole.LeftHand)
				// {
				// 	return OVRInput.Get(OVRInput.RawAxis2D.LThumbstick, OVRInput.Controller.Active);
				// }
				// return OVRInput.Get(OVRInput.RawAxis2D.RThumbstick, OVRInput.Controller.Active);
        }
    }
}