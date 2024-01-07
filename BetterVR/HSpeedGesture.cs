using HTC.UnityPlugin.Vive;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace BetterVR
{
    public class HSpeedGesture : MonoBehaviour
    {
        private static readonly Regex ULTRA_SENSITIVE_COLLIDER_NAME_MATCHER = new Regex(@"agina|okan");
        private static readonly Regex SENSITIVE_COLLIDER_NAME_MATCHER = new Regex(@"[Mm]une|[Cc]hest|agina|okan");
        private static readonly Regex MILD_COLLIDER_NAME_MATCHER = new Regex(@"[Nn]eck|[Ll]eg|[Ss]iri|[Bb]elly|[Mm]outh");
        private static readonly Regex SPNKABLE_COLLIDER_NAME_MATCHER = new Regex(@"[Ll]eg|[Ss]iri");
        private static readonly Regex MOUTH_MATCHER = new Regex(@"[Mm]outh");

        internal ViveRoleProperty roleProperty;
        internal Transform capsuleStart;
        internal Transform capsuleEnd;
        internal float activationRadius = 0.25f;
        internal float deactivationDistance = 0.5f;
        internal float sensitivityMultiplier = 1;
        internal Collider interactingCollider { get; private set; }

        internal static Vector2 hitArea;

        private bool isTouching;
        private bool isColliderSensitive;
        private static HSpeedGestureReceiver receiver;
        private static FadingHaptic _leftHandHHaptic;
        private static FadingHaptic _rightHandHHaptic;
        private static FadingHaptic leftHandFadingHaptic
        {
            get
            {
                if (_leftHandHHaptic == null)
                {
                    _leftHandHHaptic = new GameObject("LeftHandHHaptic").AddComponent<FadingHaptic>();
                    _leftHandHHaptic.onLeftHand = true;
                    _leftHandHHaptic.onRightHand = false;
                    _leftHandHHaptic.enabled = false;
                }
                return _leftHandHHaptic;
            }
        }
        private static FadingHaptic rightHandFadingHaptic
        {
            get
            {
                if (_rightHandHHaptic == null)
                {
                    _rightHandHHaptic = new GameObject("RightHandHHaptic").AddComponent<FadingHaptic>();
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
            if (!BetterVRPlugin.IsHandHSpeedGestureEnabled() || roleProperty == null) return;
            var hCtrl = Singleton<HSceneFlagCtrl>.Instance;
            if (!hCtrl) return;

            float speed =
                VivePose.GetVelocity(roleProperty).magnitude * BetterVRPlugin.HandHSpeedSensitivity.Value * sensitivityMultiplier;
            isTouching = ShouldBeTouching(speed);
            if (!isTouching) return;

            UpdateSmoothTargetSpeed(speed, hCtrl);

            var hScene = Singleton<Manager.HSceneManager>.Instance?.Hscene;
            var anim = hScene?.GetProcBase();

            if (anim != null && anim is Spnking && interactingCollider != null && speed > 12 &&
                SPNKABLE_COLLIDER_NAME_MATCHER.IsMatch(interactingCollider.name) &&
                receiver.Spnk(hScene, hCtrl))
            {
                BetterVRPlugin.Logger.LogDebug("Spnk speed: " + speed);
                if (BetterVRPlugin.HapticFeedbackIntensity.Value > 0)
                {
                    if (roleProperty == VRControllerInput.roleL) {
                        leftHandFadingHaptic.duration = 0.375f;
                        leftHandFadingHaptic.enabled = true;
                    }
                    else if (roleProperty == VRControllerInput.roleR)
                    {
                        rightHandFadingHaptic.duration = 0.375f;
                        rightHandFadingHaptic.enabled = true;
                    }
                }
            } 
            else if (isTouching && speed > 0 && BetterVRPlugin.HapticFeedbackIntensity.Value > 0)
            {
                var amplitude = speed / 4 * BetterVRPlugin.HapticFeedbackIntensity.Value;
                var frequency = hCtrl.isGaugeHit ? 120 : 35;
                if (roleProperty == VRControllerInput.roleH)
                {
                    if (!(BetterVRPluginHelper.GetLeftHand()?.GetComponentInChildren<HSpeedGesture>()?.isTouching ?? false))
                    {
                        ViveInput.TriggerHapticVibration(VRControllerInput.roleL, frequency: frequency, amplitude: amplitude / 4);
                    }
                    if (!(BetterVRPluginHelper.GetRightHand()?.GetComponentInChildren<HSpeedGesture>()?.isTouching ?? false))
                    {
                        ViveInput.TriggerHapticVibration(VRControllerInput.roleR, frequency: frequency, amplitude: amplitude / 4);
                    }

                }
                else
                {
                    ViveInput.TriggerHapticVibration(roleProperty, frequency: frequency, amplitude: amplitude);
                }
            }
        }

        private bool ShouldBeTouching(float speed)
        {
            var handRole = VRControllerInput.GetHandRole(roleProperty);
            bool triggerOrGrip =
                ViveInput.GetPressEx<HandRole>(handRole, ControllerButton.Trigger) ||
                ViveInput.GetPressEx<HandRole>(handRole, ControllerButton.Grip);

            // Movement is too slow to start activity.
            if (!isTouching && speed < 0.5f && !triggerOrGrip) return false;
            
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

            if (BetterVRPlugin.HandHSpeedGestureRequiresButtonPress() &&
                    handRole != HandRole.Invalid && !triggerOrGrip)
            {
                return false;
            }

            // Look for possible interacting colliders
            Collider[] colliders = Physics.OverlapCapsule(capsuleStart.position, capsuleEnd.position, activationRadius * scale);
            foreach (var collider in colliders)
            {
                var interactionCollider = collider.GetComponent<InteractionCollider>();
                if (interactionCollider == null || !interactionCollider.IsCharacterVisible()) continue;

                if (SENSITIVE_COLLIDER_NAME_MATCHER.IsMatch(collider.name))
                {
                    interactingCollider = collider;
                    isColliderSensitive = true;
                    return true;
                }

                if (MILD_COLLIDER_NAME_MATCHER.IsMatch(collider.name))
                {
                    if (roleProperty != VRControllerInput.roleH && MOUTH_MATCHER.IsMatch(collider.name)) continue;
                    interactingCollider = collider;
                    isColliderSensitive = false;
                }
            }

            return interactingCollider != null;
        }

        private void UpdateSmoothTargetSpeed(float speedInput, HSceneFlagCtrl hCtrl)
        {
            if (!isTouching) return;

            if (!isColliderSensitive && !HSpeedGestureReceiver.IsHoushi()) {
                if (hCtrl.loopType > 0)
                {
                    if (hCtrl.isGaugeHit && speedInput > 0 && speedInput < 1.75f && hitArea != null)
                    {
                        // Reward mild collider touching is fast loops by stabilizing gauge hit.
                        if (Random.Range(0f, 1f) < Time.fixedDeltaTime * 4) receiver.smoothTargetSpeed = Mathf.Lerp(hitArea.x, hitArea.y, 0.5f);
                    }
                    return;
                }
                else if (receiver.smoothTargetSpeed > HSpeedGestureReceiver.LOOP_01_DIVIDER - hCtrl.wheelActionCount)
                {
                    // Do not let touching less sensitive parts affect speed during fast movement.
                    return;
                }
            }

            var targetSpeed = Mathf.Clamp(speedInput - 0.125f, 0, 2f);
            
            if (hCtrl.loopType == 2)
            {
                // Curve speed output to require faster movement.
                targetSpeed *= (targetSpeed / 2);
            }
            else if (ULTRA_SENSITIVE_COLLIDER_NAME_MATCHER.IsMatch(interactingCollider.name)) {
                // Curve speed output to require slower movement.
                targetSpeed = Mathf.Sqrt(targetSpeed * 2);
            }

            if (targetSpeed <= receiver.smoothTargetSpeed) return;

            receiver.smoothTargetSpeed = Mathf.Clamp(
                targetSpeed * Time.fixedDeltaTime * receiver.GetAccelerationFactor(hCtrl) + receiver.smoothTargetSpeed,
                0, targetSpeed);
        }
    }

    public class HSpeedGestureReceiver : MonoBehaviour
    {
        internal const float IDLE_SPEED = -8f/64;
        internal const float MIN_EFFECTIVE_SPEED = -7f/64;
        internal const float LOOP_0_DEACTIVATION_THRESHOLD = -6f/64;
        internal const float LOOP_0_ACTIVATION_THRESHOLD = 0.5f;
        internal const float LOOP_01_DIVIDER = 1f; // This number is from the vanilla game
        internal const float LOOP_1_DEACTIVATION_THRESHOLD = 0.125f;
        internal const float LOOP_1_ACTIVATION_THRESHOLD = 1.5f;
        internal const float ORIGINAL_SPEED_GAUGE_RATE = 0.01f;
        internal const float CUSTOM_SPEED_GAUGE_RATE = 1f / 256;

        internal static float outputY { get; private set; }
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

            // Reduce feel increase rate for realism.
            hCtrl.speedGuageRate = isEffective ? CUSTOM_SPEED_GAUGE_RATE : ORIGINAL_SPEED_GAUGE_RATE;

            if (!isEffective)
            {
                outputY = 0;
                return;
            }

            outputY = GetOutput(hCtrl);
            smoothTargetSpeed = Mathf.Lerp(smoothTargetSpeed, IDLE_SPEED, Time.fixedDeltaTime * GetAccelerationFactor(hCtrl));

            if (IsAibu() && smoothTargetSpeed < LOOP_0_DEACTIVATION_THRESHOLD && hCtrl.loopType >= 0 && hCtrl.loopType <= 2)
            {
                // Allow stopping action with hand motion in Aibu mode.
                StopMotion(hCtrl);
            }

            if (hCtrl.isGaugeHit && hCtrl.feel_f > 0.99f && hCtrl.feel_m > 0.75f) BetterVRPluginHelper.TryFinishHSameTime();

            // BetterVRPlugin.Logger.LogWarning(
            //    "H Loop type: " +  hCtrl.loopType + " current speed: " + hCtrl.speed +
            //    " smooth target speed: " + smoothTargetSpeed);
        }

        internal float GetOutput(HSceneFlagCtrl hCtrl)
        {
            if (hCtrl.loopType == -1)
            {
                // Allow starting action using hand movement in Aibu mode.
                if (IsAibu() && smoothTargetSpeed > LOOP_0_ACTIVATION_THRESHOLD) return 1;
                return 0;
            }

            float bufferRadius = hCtrl.wheelActionCount;
            var clampedSpeed = smoothTargetSpeed;
            if (hCtrl.speed < LOOP_01_DIVIDER)
            {
                if (clampedSpeed < LOOP_1_ACTIVATION_THRESHOLD) clampedSpeed = Mathf.Min(clampedSpeed, LOOP_01_DIVIDER - bufferRadius);
            }
            else
            {
                if (clampedSpeed > LOOP_1_DEACTIVATION_THRESHOLD) clampedSpeed = Mathf.Max(clampedSpeed, LOOP_01_DIVIDER + bufferRadius);
            }

            if (clampedSpeed >= hCtrl.speed + bufferRadius) return 1;
            if (clampedSpeed <= hCtrl.speed - bufferRadius) return -1;

            return 0;
        }

        internal float GetAccelerationFactor(HSceneFlagCtrl ctrl)
        {
            // Damp the speed more when it is in gauge hit zone to avoid voice flickering.
            if (ctrl.isGaugeHit) return smoothTargetSpeed <= 1 ? 0.125f : 0.125f / smoothTargetSpeed;

            // Damp the speed to delay starting Aibu.
            if (ctrl.loopType == -1) return 0.375f;

            return smoothTargetSpeed <= 1 ? 1f : 1 / smoothTargetSpeed;
        }

        internal static bool IsAibu()
        {
            var anim = Singleton<Manager.HSceneManager>.Instance?.Hscene?.GetProcBase();
            return anim != null && anim is Aibu;
        }

        internal static bool IsHoushi()
        {
            var anim = Singleton<Manager.HSceneManager>.Instance?.Hscene?.GetProcBase();
            return anim != null && anim is Houshi;
        }

        internal bool Spnk(HScene hScene, HSceneFlagCtrl ctrl)
        {
            var anim = hScene.GetProcBase();
            if (anim == null || !(anim is Spnking)) return false;
            return anim.Proc(GetModeCtrl(hScene), ctrl.nowAnimationInfo, 1);
        }

        private static int GetModeCtrl(HScene hScene)
        {
            return (int)(modeCtrlField.GetValue(hScene) ?? -1);
        }

        private static void StopMotion(HSceneFlagCtrl ctrl)
        {
            HScene hScene = Singleton<Manager.HSceneManager>.Instance?.Hscene;
            hScene?.GetProcBase()?.SetStartMotion(true, GetModeCtrl(hScene), ctrl.nowAnimationInfo);
        }
    }

    internal class FadingHaptic : MonoBehaviour
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
