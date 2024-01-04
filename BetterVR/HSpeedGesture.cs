using HTC.UnityPlugin.Vive;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace BetterVR
{
    public class HSpeedGesture : MonoBehaviour
    {
        private static readonly Regex SENSITIVE_COLLIDER_NAME_MATCHER = new Regex(@"[Mm]une|[Cc]hest|agina|okan");
        private static readonly Regex MILD_COLLIDER_NAME_MATCHER = new Regex(@"[Nn]eck|[Ll]eg|[Ss]iri|[Bb]elly");
        private static readonly Regex SPNKABLE_COLLIDER_NAME_MATCHER = new Regex(@"[Ll]eg|[Ss]iri");

        internal ViveRoleProperty roleProperty;
        internal Transform capsuleStart;
        internal Transform capsuleEnd;
        internal float activationRadius = 0.25f;
        internal float deactivationDistance = 0.5f;
        internal float sensitivityMultiplier = 1;
        internal Collider interactingCollider { get; private set; }

        private bool isTouching;
        private bool isColliderSensitive;
        private static HSpeedGestureReceiver receiver;
        private static FinishHHaptic _leftHandHHaptic;
        private static FinishHHaptic _rightHandHHaptic;
        internal static FinishHHaptic leftHandHHaptic
        {
            get
            {
                if (_leftHandHHaptic == null)
                {
                    _leftHandHHaptic = new GameObject("LeftHandHHaptic").AddComponent<FinishHHaptic>();
                    _leftHandHHaptic.onLeftHand = true;
                    _leftHandHHaptic.onRightHand = false;
                    _leftHandHHaptic.enabled = false;
                }
                return _leftHandHHaptic;
            }
        }
        internal static FinishHHaptic rightHandHHaptic
        {
            get
            {
                if (_rightHandHHaptic == null)
                {
                    _rightHandHHaptic = new GameObject("RightHandHHaptic").AddComponent<FinishHHaptic>();
                    _rightHandHHaptic.onLeftHand = false;
                    _rightHandHHaptic.onRightHand = true;
                    _rightHandHHaptic.enabled = false;
                }
                return _rightHandHHaptic;
            }
        }

        void Awake()
        {
            if (receiver == null) receiver = new GameObject("HSpeedGestureReceiver").AddComponent<HSpeedGestureReceiver>();
        }

        void FixedUpdate()
        {
            if (BetterVRPlugin.HandHSpeedSensitivity.Value == 0 || roleProperty == null) return;
            var hCtrl = Singleton<HSceneFlagCtrl>.Instance;
            if (!hCtrl) return;

            float targetSpeed =
                VivePose.GetVelocity(roleProperty).magnitude * BetterVRPlugin.HandHSpeedSensitivity.Value * sensitivityMultiplier;
            isTouching = ShouldBeTouching(hCtrl, targetSpeed);
            if (!isTouching) return;

            UpdateSmoothTargetSpeed(targetSpeed, hCtrl);

            var hScene = Singleton<Manager.HSceneManager>.Instance?.Hscene;
            var anim = hScene?.GetProcBase();

            if (anim != null && anim is Spnking && interactingCollider != null && targetSpeed > 12 &&
                SPNKABLE_COLLIDER_NAME_MATCHER.IsMatch(interactingCollider.name) &&
                Spnk(hScene, hCtrl))
            {
                BetterVRPlugin.Logger.LogDebug("Spnk speed: " + targetSpeed);
                if (BetterVRPlugin.HapticFeedbackIntensity.Value > 0)
                {
                    if (roleProperty == VRControllerInput.roleL) {
                        leftHandHHaptic.duration = 0.5f;
                        leftHandHHaptic.enabled = true;
                    }
                    else if (roleProperty == VRControllerInput.roleR)
                    {
                        rightHandHHaptic.duration = 0.5f;
                        rightHandHHaptic.enabled = true;
                    }
                }
            } 
            else if (isTouching && targetSpeed > 0 && BetterVRPlugin.HapticFeedbackIntensity.Value > 0)
            {
                ViveInput.TriggerHapticVibration(
                    roleProperty, frequency: hCtrl.isGaugeHit ? 90 : 35,
                    amplitude: targetSpeed / 4 * BetterVRPlugin.HapticFeedbackIntensity.Value);
            }
        }

        private bool Spnk(HScene hScene, HSceneFlagCtrl ctrl)
        {
            return hScene.GetProcBase()?.Proc(HSpeedGestureReceiver.GetModeCtrl(hScene), ctrl.nowAnimationInfo, 1) ?? false;
        }

        private bool ShouldBeTouching(HSceneFlagCtrl ctrl, float speed)
        {
            // Movement is too slow to start activity.
            if (!isTouching && speed < 0.5f) return false;

            if (capsuleStart == null) capsuleStart = transform;
            if (capsuleEnd == null) capsuleEnd = transform;

            float scale = transform.lossyScale.x;

            if (interactingCollider)
            {
                Vector3 capsuleCenter = Vector3.Lerp(capsuleStart.position, capsuleEnd.position, 0.5f);
                if (Vector3.Distance(capsuleCenter, interactingCollider.ClosestPoint(capsuleCenter)) < deactivationDistance * scale)
                {
                    // Staying in the range of the current collider, can stay touching
                    return true;
                }
            }

            interactingCollider = null;
            // bool canInteractWithMildCollider = smoothTargetSpeed < 0.5f;

            // Look for possible interacting colliders
            Collider[] colliders = Physics.OverlapCapsule(capsuleStart.position, capsuleEnd.position, activationRadius * scale);
            foreach (var collider in colliders)
            {
                var stripCollider = collider.GetComponent<StripCollider>();
                if (stripCollider == null || !stripCollider.IsCharacterVisible()) continue;

                if (SENSITIVE_COLLIDER_NAME_MATCHER.IsMatch(collider.name))
                {
                    interactingCollider = collider;
                    isColliderSensitive = true;
                    return true;
                }

                if (MILD_COLLIDER_NAME_MATCHER.IsMatch(collider.name))
                {
                    interactingCollider = collider;
                    isColliderSensitive = false;
                }
            }

            return interactingCollider != null;
        }

        private void UpdateSmoothTargetSpeed(float targetSpeed, HSceneFlagCtrl hCtrl)
        {
            if (!isTouching) return;

            // Do not let touching less sensitive parts affect speed during fast loop.
            if (!isColliderSensitive && hCtrl.loopType > 0) return;
            if (!isColliderSensitive && receiver.smoothTargetSpeed > 0.495f) return;

            // Do not start motion from idle state using hand gesture except in Aibu mode.
            if (hCtrl.loopType == -1 && !HSpeedGestureReceiver.IsAibu()) return;

            targetSpeed = Mathf.Min(targetSpeed, hCtrl.loopType >= 0 ? 2f : 1f);

            if (targetSpeed <= receiver.smoothTargetSpeed) return;

            receiver.smoothTargetSpeed = Mathf.Clamp(
                targetSpeed * Time.fixedDeltaTime * receiver.GetAccelerationFactor(hCtrl) + receiver.smoothTargetSpeed,
                0, targetSpeed);
        }
    }

    public class HSpeedGestureReceiver : MonoBehaviour
    {
        private const float IDLE_SPEED = -8f/64;
        private const float MIN_EFFECTIVE_SPEED = -7f/64;
        private const float LOOP_0_DEACTIVATION_THRESHOLD = -6f/64;
        private const float LOOP_0_ACTIVATION_THRESHOLD = 0.625f;
        private const float LOOP_01_DIVIDER = 1f; // This number is from the vanilla game
        private const float LOOP_1_DEACTIVATION_THRESHOLD = 0.125f;
        private const float LOOP_1_ACTIVATION_THRESHOLD = 1.5f;

        internal static bool shouldAttemptToStartAction { get; private set; }
        private static FieldInfo modeCtrlField;

        internal float smoothTargetSpeed = 0;
        private GaugeHitIndicator gaugeHitIndicator;

        void Awake()
        {
            modeCtrlField = typeof(HScene).GetField("modeCtrl", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        void FixedUpdate()
        {
            bool isEffective = (smoothTargetSpeed > MIN_EFFECTIVE_SPEED);

            (gaugeHitIndicator ?? (gaugeHitIndicator = new GaugeHitIndicator())).UpdateIndicators(isEffective);

            var hCtrl = Singleton<HSceneFlagCtrl>.Instance;
            if (!hCtrl) return;

            // Allow starting action using hand movement in Aibu mode.
            shouldAttemptToStartAction = isEffective && IsAibu() && hCtrl.loopType == -1 && smoothTargetSpeed > LOOP_0_ACTIVATION_THRESHOLD;

            if (!isEffective) return;

            switch (hCtrl.loopType)
            {
                case -1:
                    break;
                case 0:
                    if (IsAibu() && smoothTargetSpeed < LOOP_0_DEACTIVATION_THRESHOLD)
                    {
                        // Allow stopping action with hand motion in Aibu mode.
                        StopMotion(hCtrl);
                        hCtrl.speed = 0;
                    }
                    else if (smoothTargetSpeed > LOOP_1_ACTIVATION_THRESHOLD)
                    {
                        // Increase speed to move onto loop stage 1.
                        hCtrl.speed = LOOP_1_ACTIVATION_THRESHOLD;
                    }
                    else
                    {
                        // Clamp speed to stay in loop stage 0.
                        hCtrl.speed = Mathf.Clamp(smoothTargetSpeed, 0, LOOP_01_DIVIDER - 0.01f);
                    }
                    break;
                case 1:
                    if (smoothTargetSpeed < LOOP_1_DEACTIVATION_THRESHOLD)
                    {
                        // Decrease speed to move back to loop stage 0
                        hCtrl.speed = LOOP_1_DEACTIVATION_THRESHOLD;
                    }
                    else
                    {
                        // Clamp speed to stay in loop stage 1.
                        hCtrl.speed = Mathf.Clamp(smoothTargetSpeed, LOOP_01_DIVIDER + 0.01f, 2f);
                    }
                    break;
                case 2:
                    // Curve speed output to require faster movement.
                    hCtrl.speed = Mathf.Clamp(smoothTargetSpeed * smoothTargetSpeed / 2, 0, 2f);

                    if (hCtrl.isGaugeHit && hCtrl.feel_f > 0.99f && hCtrl.feel_m > 0.75f) BetterVRPluginHelper.TryFinishHSameTime();
                    break;
                case 3:
                    smoothTargetSpeed = 0;
                    break;
            }

            smoothTargetSpeed = Mathf.Lerp(smoothTargetSpeed, IDLE_SPEED, Time.fixedDeltaTime * GetAccelerationFactor(hCtrl));

            // BetterVRPlugin.Logger.LogWarning(
            //    "H Loop type: " +  hCtrl.loopType + " current speed: " + hCtrl.speed +
            //    " smooth target speed: " + smoothTargetSpeed);
        }

        internal float GetAccelerationFactor(HSceneFlagCtrl ctrl)
        {
            // Damp the speed more when it is in gauge hit zone to avoid voice flickering.
            if (ctrl.isGaugeHit && smoothTargetSpeed > 0.25f) return 0.25f / smoothTargetSpeed;

            // Damp the speed to delay starting Aibu.
            if (ctrl.loopType == -1) return 0.375f;

            return smoothTargetSpeed <= 1 ? 1f : 1 / smoothTargetSpeed;
        }

        internal static bool IsAibu()
        {
            var anim = Singleton<Manager.HSceneManager>.Instance?.Hscene?.GetProcBase();
            return anim != null && anim is Aibu;
        }

        internal static int GetModeCtrl(HScene hScene)
        {
            return (int)(modeCtrlField.GetValue(hScene) ?? -1);
        }

        private static void StartMotion(HSceneFlagCtrl ctrl)
        {
            HScene hScene = Singleton<Manager.HSceneManager>.Instance?.Hscene;
            hScene?.GetProcBase()?.SetStartMotion(false, GetModeCtrl(hScene), ctrl.nowAnimationInfo);
        }

        private static void StopMotion(HSceneFlagCtrl ctrl)
        {
            HScene hScene = Singleton<Manager.HSceneManager>.Instance?.Hscene;
            hScene?.GetProcBase()?.SetStartMotion(true, GetModeCtrl(hScene), ctrl.nowAnimationInfo);
        }
    }

    internal class FinishHHaptic : MonoBehaviour
    {
        const float DEFAULT_DURATION = 3;
        private float timePassed = 0;
        internal float duration = DEFAULT_DURATION;
        internal bool onLeftHand = true;
        internal bool onRightHand = true;

        void OnEnable()
        {
            timePassed = 0;
        }

        void FixedUpdate()
        {
            if (timePassed > duration)
            {
                enabled = false;
                return;
            }

            var intensity = BetterVRPlugin.HapticFeedbackIntensity.Value * (1 - Mathf.Pow(timePassed / duration, 4f));

            if (onLeftHand) ViveInput.TriggerHapticVibrationEx<HandRole>(HandRole.LeftHand, amplitude: intensity);
            if (onRightHand) ViveInput.TriggerHapticVibrationEx<HandRole>(HandRole.RightHand, amplitude: intensity);

            timePassed += Time.fixedDeltaTime;
        }
    }
}
