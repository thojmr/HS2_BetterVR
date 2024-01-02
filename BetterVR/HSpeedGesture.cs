using HTC.UnityPlugin.Vive;
using System.Text.RegularExpressions;
using UnityEngine;

namespace BetterVR
{
    public class HSpeedGesture : MonoBehaviour
    {
        private const float LOOP_SLOW_FAST_DIVIDER = 1f;
        private const float SLOW_MODE_ACTIVATION_THRESHOLD = 0.25f;
        private const float FAST_MODE_ACTIVATION_THRESHOLD = 1.25f;
        private static readonly Regex INTERACTING_COLLIDER_NAME_MATCHER =
            new Regex(@"Mune|mune|Chest|chest|agina|okan");

        internal ViveRoleProperty roleProperty;
        internal Transform capsuleStart;
        internal Transform capsuleEnd;
        internal float activationRadius = 0.25f;
        internal float deactivationDistance = 0.5f;
        internal float sensitivityMultiplier = 0.75f;

        private Collider interactingCollider;
        private float smoothTargetSpeed = 0;
        private bool isTouching;

        void FixedUpdate()
        {
            if (BetterVRPlugin.HandHSpeedSensitivity.Value == 0 || roleProperty == null) return;
            var hCtrl = Singleton<HSceneFlagCtrl>.Instance;
            if (!hCtrl) return;

            float targetSpeed =
                VivePose.GetVelocity(roleProperty).magnitude * BetterVRPlugin.HandHSpeedSensitivity.Value * sensitivityMultiplier;
            isTouching = ShouldBeTouching(hCtrl, targetSpeed);
            if (!isTouching && smoothTargetSpeed < 0.0625f)
            {
                smoothTargetSpeed = 0;
                return;
            }

            UpdateSmoothTargetSpeed(hCtrl, isTouching ? targetSpeed : 0);

            switch (hCtrl.loopType)
            {
                case -1:
                    smoothTargetSpeed = 0;
                    break;
                case 0:
                    if (smoothTargetSpeed > FAST_MODE_ACTIVATION_THRESHOLD)
                    {
                        // Increase speed to move onto loop stage 1
                        hCtrl.speed = FAST_MODE_ACTIVATION_THRESHOLD;
                        break;
                    }
                    // Clamp speed to stay in loop stage 0.
                    hCtrl.speed = Mathf.Clamp(smoothTargetSpeed, 0, LOOP_SLOW_FAST_DIVIDER - 0.0625f);
                    break;
                case 1:
                    if (smoothTargetSpeed < SLOW_MODE_ACTIVATION_THRESHOLD)
                    {
                        // Decrease speed to move back to loop stage 0
                        hCtrl.speed = SLOW_MODE_ACTIVATION_THRESHOLD;
                        break;
                    }
                    // Clamp speed to stay in loop stage 1.
                    hCtrl.speed = Mathf.Clamp(smoothTargetSpeed, LOOP_SLOW_FAST_DIVIDER + 0.0625f, 2f);
                    break;
                default:
                    // Curve speed output to require faster movement.
                    hCtrl.speed = Mathf.Clamp(smoothTargetSpeed * smoothTargetSpeed / 2, 0, 2f);
                    break;
            }

            BetterVRPluginHelper.gaugeHitIndicator.ShowIfGaugeIsHit();
        }

        private bool ShouldBeTouching(HSceneFlagCtrl ctrl, float speed)
        {
            // Movement is too slow to start activity.
            if (!isTouching && speed < 0.5f) return false;

            if (capsuleStart == null) capsuleStart = transform;
            if (capsuleEnd == null) capsuleEnd = transform;

            float scale = transform.TransformVector(Vector3.right).magnitude;

            if (interactingCollider) {
                Vector3 capsuleCenter = Vector3.Lerp(capsuleStart.position, capsuleEnd.position, 0.5f);
                if (Vector3.Distance(capsuleCenter, interactingCollider.ClosestPoint(capsuleCenter)) < deactivationDistance * scale)
                {
                    return true;
                }
            }

            interactingCollider = null;
            Collider[] colliders = Physics.OverlapCapsule(capsuleStart.position, capsuleEnd.position, activationRadius * scale);
            foreach (var collider in colliders)
            {
                if (INTERACTING_COLLIDER_NAME_MATCHER.IsMatch(collider.name))
                {
                    interactingCollider = collider;
                    return true;
                }
            }
            return false;
        }

        private void UpdateSmoothTargetSpeed(HSceneFlagCtrl hCtrl, float targetSpeed)
        {
            if (smoothTargetSpeed == targetSpeed) return;

            targetSpeed = Mathf.Clamp(targetSpeed, 0, 2);

            float accelerationFactor = 1f;
            if (hCtrl.isGaugeHit)
            {
                // Damp the speed more to allow staying in gauge hit zone longer to avoid voice flickering.
                accelerationFactor = 0.5f;
            }

            smoothTargetSpeed = Mathf.Lerp(smoothTargetSpeed, targetSpeed, Time.fixedDeltaTime * accelerationFactor);

            // BetterVRPlugin.Logger.LogWarning(
            //    "H Loop type: " +  hCtrl.loopType + " current speed: " + hCtrl.speed +
            //    " smooth target speed: " + smoothTargetSpeed + " target speed: " + targetSpeed);
        }
    }
}
