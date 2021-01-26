using System;
using UnityEngine;

namespace BetterVR 
{
    public static class VRCameraHelper 
    {
        //If the new position is 0, or < minDistance from the last position, the position hasn't changed
        internal static Boolean IsNewPosition(Vector3 oldPosition, Vector3 newPosition, float minDistance = 0.3f) 
        {
            if (oldPosition == null) return true;
            bool notAtZero = Math.Abs(Vector3.Distance(newPosition, new Vector3(0, 0, 0))) > minDistance;
            bool hasMoved = Math.Abs(Vector3.Distance(newPosition, oldPosition)) > minDistance;
            
            return (hasMoved && notAtZero);
        }

        internal static Transform ConvertPositionToTransform(Vector3 position, Quaternion rotation) 
        {
            //TODO does this add up over time?
            GameObject camGameObject = new GameObject();
            Transform newTransform = camGameObject.transform;
            newTransform.position = position;
            newTransform.rotation = rotation;

            return newTransform;
        }
    }
    
}