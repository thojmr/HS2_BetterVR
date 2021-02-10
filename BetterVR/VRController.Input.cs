using UnityEngine;
using HTC.UnityPlugin.Vive;

namespace BetterVR
{    
    public static class VRControllerInput
    {

        internal static ViveRoleProperty roleR = ViveRoleProperty.New(HandRole.RightHand);
        internal static ViveRoleProperty roleL = ViveRoleProperty.New(HandRole.LeftHand);

        
        /// <summary>
        /// When user squeezes the grip, turn the camera via wrists angular veolcity
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
                BetterVRPluginHelper.VROrigin.transform.Rotate(0f, -(velocity.y * Time.deltaTime * 100)/3f, 0f, Space.Self);
            }

            //Do for both hands
            if (ViveInput.GetPressEx<HandRole>(HandRole.RightHand, ControllerButton.Grip))
            {
                BetterVRPluginHelper.GetVROrigin();
                if (BetterVRPluginHelper.VROrigin == null) return;

                var velocity = VivePose.GetAngularVelocity(roleR);
                BetterVRPluginHelper.VROrigin.transform.Rotate(0f, -(velocity.y * Time.deltaTime * 100)/3f, 0f, Space.Self);
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