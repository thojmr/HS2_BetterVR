using UnityEngine;

namespace BetterVR
{    
    public static class BetterVRPluginHelper
    {     
        public static GameObject VROrigin;
        public static bool init = true;


        public enum VR_Hand
        {
            left,
            right,
            none
        }


        /// <summary>
        /// Get the top level VR game object
        /// </summary>
        internal static GameObject GetVROrigin()
        {
            if (VROrigin == null)
            {
                VROrigin = GameObject.Find("VROrigin");

                //Show vr controler GO tree
                if (VROrigin != null && init) {
                  init = false;  
                  if (BetterVRPlugin.debugLog) DebugTools.LogChildrenComponents(VROrigin, true);
                } 
            }            

            // var origin = SteamVR_Render.Top()?.origin;  //The headset rig render
            // if (BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" SteamVR Origin {origin?.gameObject}");

            return VROrigin;
        }


        /// <summary>
        /// Use an enum to get the correct hand
        /// </summary>
        internal static GameObject GetHand(VR_Hand hand)
        {
            if (hand == VR_Hand.left) return GetLeftHand();
            if (hand == VR_Hand.right) return GetRightHand();

            return null;
        }


        /// <summary>
        /// Get The left hand controller vr game object
        /// </summary>
        internal static GameObject GetLeftHand()
        {
            var leftHand = GameObject.Find("ViveControllers/Left");
            if (leftHand == null) return null;

            if (BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" GetLeftHand id {leftHand.GetInstanceID()}");

            return leftHand.gameObject;
        }


        internal static GameObject GetRightHand()
        {
            var rightHand = GameObject.Find("ViveControllers/Right");
            if (rightHand == null) return null;

            if (BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" GetRightHand id {rightHand.GetInstanceID()}");

            return rightHand.gameObject;
        }

    }
}