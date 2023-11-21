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
            //When squeezing both grips, apply hand rotation to the headset
            if (ViveInput.GetPressEx<HandRole>(HandRole.LeftHand, ControllerButton.Grip) && ViveInput.GetPressEx<HandRole>(HandRole.RightHand, ControllerButton.Grip))
            {
                if (!BetterVRPluginHelper.VROrigin)
                {
                    return;
                }
                var handVelocityDifference = VivePose.GetVelocity(roleR) - VivePose.GetVelocity(roleL);
                var handPositionDifference = VivePose.GetPose(roleR).TransformPoint(Vector3.zero)  - VivePose.GetPose(roleL).TransformPoint(Vector3.zero);
                handVelocityDifference.y = 0;
                handPositionDifference.y = 0;
                var angularSpeed = Vector3.Cross(handPositionDifference, handVelocityDifference).y / handPositionDifference.sqrMagnitude;

                BetterVRPluginHelper.VROrigin.transform.Rotate(0f, -(angularSpeed * Time.deltaTime * 100)/3f, 0f, Space.Self);
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
