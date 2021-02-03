using UnityEngine;
using System.Collections;

namespace BetterVR
{    
    public static class BetterVRPluginHelper
    {     
        public static GameObject VROrigin;
        public static bool init = true;//False once headset GameObject found
        internal static bool isRunning = false;//True while actively searching for headset GameObject


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


        internal static void CheckForVROrigin(BetterVRPlugin instance)
        {
            if (BetterVRPluginHelper.isRunning) return;
            instance.StartCoroutine(BetterVRPluginHelper.Init());
        }


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


        /// <summary>
        /// Enlarge the VR camera, to make the world appear to shrink by 15%
        /// </summary>
        public static void FixWorldScale(bool enable = true)
        {
            var viveRig = GameObject.Find("ViveRig");
            if (viveRig != null)
            {
                viveRig.transform.localScale = Vector3.one * (enable ? 1.15f : 1);
            }
        }

    }
}