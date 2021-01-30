using UnityEngine;

namespace BetterVR
{    
    public static class BetterVRPluginHelper
    {     
        public static GameObject VROrigin;
        public static bool init = true;


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
                  if (BetterVRPlugin.debugLog) BetterVRPluginHelper.LogChildrenComponents(VROrigin, true);
                } 
            }            

            // var origin = SteamVR_Render.Top()?.origin;  //The headset rig render
            // if (BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" SteamVR Origin {origin?.gameObject}");

            return VROrigin;
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


        /// <summary>
        /// Visualize the VR object tree and components under each (debug only)
        /// </summary>
        internal static void LogChildrenComponents(GameObject parent, bool recursive = false, int level = 0)
        {            
            var children = parent?.GetComponents<Component>();
            if (children == null) return;

            //Add spaces to each log level to see what the structure looks like
            var spaces = " "; 
            for (var s = 0; s < level; s++)
            {
                spaces += "  ";
            }

            if (BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($"{spaces}{parent.name}"); 

            //Log all child components
            foreach(var child in children)
            {
                if (BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" {spaces}{child.name}:{child.GetType().Name} {child.transform.position} {child.transform.childCount}:");                   
            }

            if (!recursive) return;
            level++;

            //Loop through each child game object
            for (var i = 0; i <  parent.transform.childCount; i++)
            {
                LogChildrenComponents(parent.transform.GetChild(i).gameObject, recursive, level);
            }
        }
    }
}