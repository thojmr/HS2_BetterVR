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
                  if (BetterVRPlugin.debugLog) BetterVRPluginHelper.LogChildrenComponents(VROrigin, true);
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


        /// <summary>
        /// Visualize the VR object tree and components under each (debug only)
        /// </summary>
        internal static void LogChildrenComponents(GameObject parent, bool recursive = false, int level = 0)
        {            
            var children = parent?.GetComponents<Component>();
            if (children == null) return;

            if (level == 0) 
            {
                if (BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" "); 
                if (BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" [LogChildrenComponents]");
            }

            //Add spaces to each log level to see what the structure looks like
            var spaces = " "; 
            for (var s = 0; s < level; s++)
            {
                spaces += "  ";
            }

            if (BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($"{spaces}{parent.name} {parent.activeSelf}"); 

            //Log all child components
            foreach(var child in children)
            {
                if (BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" {spaces}{child.name}: {child.GetType().Name} {child.transform.position} {child.transform.childCount}:");                   
            }

            if (!recursive) return;
            level++;

            //Loop through each child game object
            for (var i = 0; i <  parent.transform.childCount; i++)
            {
                LogChildrenComponents(parent.transform.GetChild(i).gameObject, recursive, level);
            }
        }


         /// <summary>
        /// Visualize the VR object tree and components under each (debug only)
        /// </summary>
        internal static void LogParents(GameObject currentGo, int maxLevel = 1, int currentLevel = 0)
        {         
            if (currentLevel == 0) 
            {
                if (BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" "); 
                if (BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" [LogParents]");
            }

            //Add spaces to each log level to see what the structure looks like
            var spaces = " "; 
            for (var s = 0; s < currentLevel; s++)
            {
                spaces += "  ";
            }

            if (BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($"{spaces}{currentGo.name} {currentGo.activeSelf}"); 

            var children = currentGo?.GetComponents<Component>();

            if (children != null)
            {
                //Log all child components
                foreach(var child in children)
                {
                    if (BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" {spaces}{child.name}: {child.GetType().Name} {child.transform.position} {child.transform.childCount}:");                   
                }
            }

            //End when max level hit
            if (currentLevel >= maxLevel) return;        

            //Get next parent
            if (currentGo.transform.parent == null) return;
            var parent = currentGo.transform.parent.gameObject;
            if (parent == null) return;    

            currentLevel++;
            //Check for next parent
            LogParents(parent, maxLevel, currentLevel);
            
        }

        
        /// <summary>
        /// Draw a debug shpere
        /// </summary>
        public static GameObject DrawSphere(float radius = 1, Vector3 position = new Vector3())
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            // sphere.GetComponent<Renderer>().material = new Material(Shader.Find("Transparent/Diffuse")); // assign the selected material to it
             
            sphere.name = "DebugSphere";
            sphere.position = position;
            sphere.localScale = new Vector3(radius, radius, radius);
            sphere.GetComponent<Renderer>().enabled = true; // show it

            return sphere.gameObject;
        }


        
        /// <summary>
        /// Draw shphere and attach to some parent transform
        /// </summary>
        public static void DrawSphereAndAttach(Transform parent, float radius = 1, Vector3 localPosition = new Vector3())
        {
            var sphere = DrawSphere(radius);
            //Attach and move to parent position
            sphere.transform.SetParent(parent, false);

            sphere.transform.localPosition = localPosition;
        }
    }
}