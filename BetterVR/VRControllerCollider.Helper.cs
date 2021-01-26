using UnityEngine;
using System.Collections;
using KKAPI;

namespace BetterVR
{    
    public static class VRControllerColliderHelper
    {
        internal static bool coroutineActive = false;
        internal static BetterVRPlugin pluginInstance;


        internal static void TriggerHelperCoroutine() 
        {
            //Only trigger if not already running, and in main game
            if (coroutineActive || KoikatuAPI.GetCurrentGameMode() != GameMode.MainGame) return;
            coroutineActive = true;

            pluginInstance.StartCoroutine(LoopEveryXSeconds());
        }


        internal static void StopHelperCoroutine() 
        {
            pluginInstance.StopCoroutine(LoopEveryXSeconds());
            coroutineActive = false;
        }


        //Got tired of searching for the correct hooks, just check for new dynamic bones on a loop.  Genious!
        internal static IEnumerator LoopEveryXSeconds()
        {            
            while (coroutineActive) 
            {                
                //If not in the main game, continue
                if (KoikatuAPI.GetCurrentGameMode() != GameMode.MainGame) yield return new WaitForSeconds(3);

                VRControllerCollider.SetVRControllerColliderToDynamicBones();

                // BetterVRPlugin.Logger.Log(LogLevel.Info, $"Camera distance {distance}");

                yield return new WaitForSeconds(3);
            }
        }

    }
}