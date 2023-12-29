using UnityEngine;
using System.Collections;

namespace BetterVR
{    
    public static class VRControllerColliderHelper
    {
        internal static bool coroutineActive = false;
        internal static BetterVRPlugin pluginInstance;


        internal static void TriggerHelperCoroutine() 
        {
            //Only trigger if not already running, and in main game
            if (coroutineActive) return;
            coroutineActive = true;

            pluginInstance.StartCoroutine(LoopEveryXSeconds());
        }


        internal static void StopHelperCoroutine() 
        {
            pluginInstance.StopCoroutine(LoopEveryXSeconds());
            coroutineActive = false;
        }

        
        /// <summary>
        /// Got tired of searching for the correct hooks, just check for new dynamic bones on a loop.  Genious! (Should be able to use CharCustFunCtrl for this later)
        /// </summary>
        internal static IEnumerator LoopEveryXSeconds()
        {            
            while (coroutineActive) 
            {                
                VRControllerCollider.UpdateDynamicBoneColliders();
                yield return new WaitForSeconds(3);
            }
        }

    }
}
