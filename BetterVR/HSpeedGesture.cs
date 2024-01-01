using HTC.UnityPlugin.Vive;
using System.Text.RegularExpressions;
using UnityEngine;

namespace BetterVR
{
    public class HSpeedGesture : MonoBehaviour
    {
        private const float BASE_SPEED = 0.01f;
        private const float SPEED_FACTOR = 2f;
        private const float LOOP_CHANGE_THRESHOLD = 0.5f;
        private static readonly Regex INTERACTING_COLLIDER_NAME_MATCHER =
            new Regex(@"mune|Mune|Chest|chest|agina|okan");

        internal ViveRoleProperty roleProperty;
        internal Transform capsuleStart;
        internal Transform capsuleEnd;
        internal float radius = 0.25f;

        private float smoothTargetSpeed = 0;
        private float acceleration = 0;
        private bool isEffective;

        void FixedUpdate()
        {
            if (!BetterVRPlugin.UseHandSpeedForHSpeed.Value) return;
            if (!capsuleStart || !capsuleEnd) return;
            var hCtrl = Singleton<HSceneFlagCtrl>.Instance;
            if (!hCtrl) return;

            float deviceSpeed = VivePose.GetVelocity(roleProperty).magnitude;
            if (!isEffective) smoothTargetSpeed = hCtrl.speed;
            isEffective = ShouldBeEffective(hCtrl, deviceSpeed);
            if (!isEffective) return;

            float targetSpeed = deviceSpeed * SPEED_FACTOR + BASE_SPEED;
            if (hCtrl.loopType == 1) targetSpeed *= 2;
            targetSpeed = Mathf.Clamp(targetSpeed, 0, 2);
            
            float speedDivider = 1f;

            float damper = 0.25f;
            if (hCtrl.isGaugeHit)
            {
                // Allow staying in gauge hit zone longer to avoid voice flickering.
                damper = 1.5f;
            }
            else if (hCtrl.loopType == 1 && smoothTargetSpeed < speedDivider)
            {
                // Attempting to go from fast mode to slow mode, increase damper to enforce delay.
                damper = 0.75f;
            }

            smoothTargetSpeed = Mathf.SmoothDamp(
                smoothTargetSpeed, targetSpeed, ref acceleration, smoothTime:damper);

            switch (hCtrl.loopType)
            {
                case -1:
                    smoothTargetSpeed = 0;
                    break;
                case 0:
                    hCtrl.speed = Mathf.Clamp(smoothTargetSpeed, 0, speedDivider - 0.01f);
                    // Check whether to move onto loop stage 1
                    if (smoothTargetSpeed > speedDivider + LOOP_CHANGE_THRESHOLD) hCtrl.speed = speedDivider + LOOP_CHANGE_THRESHOLD;
                    break;
                case 1:
                    hCtrl.speed = Mathf.Clamp(smoothTargetSpeed, speedDivider + 0.01f, 2f);
                    // Check whether to move back to loop stage 0
                    if (smoothTargetSpeed < speedDivider - LOOP_CHANGE_THRESHOLD) hCtrl.speed = speedDivider - LOOP_CHANGE_THRESHOLD;
                    break;
                default:
                    hCtrl.speed = Mathf.Clamp(smoothTargetSpeed, 0, 2f);
                    break;
            }

            // BetterVRPlugin.Logger.LogWarning(
            //    "H Loop type: " +  hCtrl.loopType + " current speed: " + hCtrl.speed +
            //    " smooth target speed: " + smoothTargetSpeed + " target speed: " + targetSpeed);

            BetterVRPluginHelper.gaugeHitIndicator.ShowIfGaugeIsHit();
        }

        private bool ShouldBeEffective(HSceneFlagCtrl ctrl, float deviceSpeed)
        {
            // Keep control active when gauge is hit even if the device leaves the collider.
            if (isEffective && ctrl.isGaugeHit) return true;

            // Device movement is too slow to start activity.
            if (!isEffective && deviceSpeed < 0.25f) return false;

            Collider[] colliders = Physics.OverlapCapsule(capsuleStart.position, capsuleEnd.position, radius);
            foreach (var collider in colliders)
            {
                if (INTERACTING_COLLIDER_NAME_MATCHER.IsMatch(collider.name)) return true;
            }
            return false;
        }
    }
}
