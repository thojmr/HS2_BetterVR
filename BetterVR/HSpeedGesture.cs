using HTC.UnityPlugin.Vive;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace BetterVR
{
    public class HSpeedGesture : MonoBehaviour
    {
        private static readonly Regex SPNKABLE_COLLIDER_NAME_MATCHER = new Regex(@"[Ll]eg|[Ss]iri");
        private static readonly Regex MOUTH_MATCHER = new Regex(@"[Mm]outh");

        internal float speed { get; private set; }
        internal ViveRoleProperty roleProperty;
        internal Transform capsuleStart;
        internal Transform capsuleEnd;
        internal float activationRadius = 0.25f;
        internal float deactivationDistance = 0.5f;
        internal float sensitivityMultiplier = 1;
        internal InteractionCollider interactingCollider { get; private set; }

        // TODO: remove
        internal static Vector2 hitArea;

        internal bool isTouching { get; private set; }
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
            receiver.AddSourceGesture(this);
        }

        void OnDestroy()
        {
            receiver.RemoveSourceGesture(this);
        }

        void FixedUpdate()
        {
            if (!BetterVRPlugin.IsHandHSpeedGestureEnabled() || roleProperty == null) return;
            var hCtrl = Singleton<HSceneFlagCtrl>.Instance;
            if (!hCtrl) return;

            speed =
                VivePose.GetVelocity(roleProperty).magnitude * BetterVRPlugin.HandHSpeedSensitivity.Value * sensitivityMultiplier
                + VivePose.GetAngularVelocity(roleProperty).magnitude * 0.0625f;
            isTouching = ShouldBeTouching(speed);
            if (!isTouching) return;
            receiver.enabled = true;

            var hScene = Singleton<Manager.HSceneManager>.Instance?.Hscene;
            var anim = hScene?.GetProcBase();

            if (anim != null && anim is Spnking && interactingCollider != null && speed > 12 &&
                SPNKABLE_COLLIDER_NAME_MATCHER.IsMatch(interactingCollider.name) &&
                receiver.Spnk(hScene, hCtrl))
            {
                BetterVRPlugin.Logger.LogDebug("Spnk speed: " + speed);
                TriggerSpnkHaptic();
            }
            else if (isTouching && speed > 0)
            {
                TriggerTouchingHaptic(hCtrl);
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
                var collider = interactingCollider.GetComponent<Collider>();
                if (collider != null && Vector3.Distance(capsuleCenter, collider.ClosestPoint(capsuleCenter)) < deactivationDistance * scale)
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

                if (interactionCollider.sensitivityLevel >= 2)
                {
                    interactingCollider = interactionCollider;
                    return true;
                }

                if (interactionCollider.sensitivityLevel == 1)
                {
                    interactingCollider = interactionCollider;
                    continue;
                }
                
                if (roleProperty == VRControllerInput.roleH && MOUTH_MATCHER.IsMatch(collider.name))
                {
                    interactingCollider = interactionCollider;
                }
            }

            return interactingCollider != null;
        }

        private void TriggerSpnkHaptic()
        {
            if (BetterVRPlugin.HapticFeedbackIntensity.Value == 0) return;
            if (roleProperty == VRControllerInput.roleL)
            {
                leftHandFadingHaptic.duration = 0.375f;
                leftHandFadingHaptic.enabled = true;
            }
            else if (roleProperty == VRControllerInput.roleR)
            {
                rightHandFadingHaptic.duration = 0.375f;
                rightHandFadingHaptic.enabled = true;
            } 
        }

        private void TriggerTouchingHaptic(HSceneFlagCtrl ctrl)
        {
            if (BetterVRPlugin.HapticFeedbackIntensity.Value == 0) return;

            var amplitude = BetterVRPlugin.HapticFeedbackIntensity.Value;
            if (receiver?.gaugeHitIndicator && receiver.gaugeHitIndicator.smoothGaugeHit > 0.25f)
            {
                // Emulate heart beats
                float strength = (1 - GaugeHitIndicator.GetPulsePhase(ctrl)) % 1;
                if (ctrl.feel_f >= 0.75f)
                {
                    amplitude *= strength;
                }
                else
                {
                    amplitude *= Mathf.Lerp(0.5f, 0.0625f, strength);
                }
            }
            else
            {
                // Emulate touch feel
                amplitude *= speed / 4;
            }
            var frequency = 35;
            if (roleProperty != VRControllerInput.roleH)
            {
                ViveInput.TriggerHapticVibration(roleProperty, frequency: frequency, amplitude: amplitude);
                return;
            }

            // HMD has no haptic, trigger haptic on the hands unless the hands are already touching.
            var leftGesture = BetterVRPluginHelper.GetLeftHand()?.GetComponentInChildren<HSpeedGesture>();
            if (leftGesture == null || !leftGesture.isTouching)
            {
                ViveInput.TriggerHapticVibration(VRControllerInput.roleL, frequency: frequency, amplitude: amplitude / 4);
            }

            var rightGesture = BetterVRPluginHelper.GetRightHand()?.GetComponentInChildren<HSpeedGesture>();
            if (rightGesture == null || !rightGesture.isTouching)
            {
                ViveInput.TriggerHapticVibration(VRControllerInput.roleR, frequency: frequency, amplitude: amplitude / 4);
            }
        }
    }

    public class HSpeedGestureReceiver : MonoBehaviour
    {
        internal const float IDLE_SPEED = -8f / 64;
        internal const float MIN_EFFECTIVE_SPEED = -7f / 64;
        internal const float LOOP_0_DEACTIVATION_THRESHOLD = -6f / 64;
        internal const float LOOP_0_ACTIVATION_THRESHOLD = 0.5f;
        internal const float LOOP_01_DIVIDER = 1f; // This number is from the vanilla game
        internal const float LOOP_1_DEACTIVATION_THRESHOLD = 0.125f;
        internal const float LOOP_1_ACTIVATION_THRESHOLD = 1.5f;
        internal const float ORIGINAL_SPEED_GAUGE_RATE = 0.01f;
        internal const float CUSTOM_SPEED_GAUGE_RATE = 1f / 256;

        internal static float outputY { get; private set; }

        private static FieldInfo modeCtrlField;

        internal float smoothTargetSpeed = 0;
        internal GaugeHitIndicator gaugeHitIndicator { get; private set; }
        private HashSet<HSpeedGesture> sourceGestures = new HashSet<HSpeedGesture>();

        void Awake()
        {
            modeCtrlField = typeof(HScene).GetField("modeCtrl", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        void OnDisable()
        {
            outputY = 0;
            var ctrl = Singleton<HSceneFlagCtrl>.Instance;
            if (ctrl) ctrl.speedGuageRate = ORIGINAL_SPEED_GAUGE_RATE;
        }

        void FixedUpdate()
        {
            var hCtrl = Singleton<HSceneFlagCtrl>.Instance;
            if (!hCtrl) return;

            UpdateSmoothTargetSpeed(hCtrl, Time.fixedDeltaTime);

            if (smoothTargetSpeed < MIN_EFFECTIVE_SPEED)
            {
                enabled = false;
                return;
            }

            // Reduce feel increase rate for realism.
            hCtrl.speedGuageRate = hCtrl.feel_f < 0.75f ? CUSTOM_SPEED_GAUGE_RATE : ORIGINAL_SPEED_GAUGE_RATE;

            if (hCtrl.isGaugeHit && Manager.Config.HData.FeelingGauge)
            {
                if (gaugeHitIndicator == null)
                {
                    gaugeHitIndicator = new GameObject("GaugeHitIndicator").AddComponent<GaugeHitIndicator>();
                }
                gaugeHitIndicator.gameObject.SetActive(true);
            }

            UpdateOutput(hCtrl);

            if (IsAibu() && !hCtrl.isGaugeHit && hCtrl.loopType >= 0 && hCtrl.loopType <= 2 &&
                smoothTargetSpeed < LOOP_0_DEACTIVATION_THRESHOLD)
            {
                // Allow stopping action with hand motion in Aibu mode.
                StopMotion(hCtrl);
            }

            if (hCtrl.isGaugeHit && hCtrl.feel_f > 0.998f && hCtrl.feel_m > 0.75f) BetterVRPluginHelper.TryFinishHSameTime();
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

        private void UpdateOutput(HSceneFlagCtrl hCtrl)
        {
            if (hCtrl.loopType == -1)
            {
                if (IsAibu() && smoothTargetSpeed > LOOP_0_ACTIVATION_THRESHOLD)
                {
                    // Allow starting action using hand movement in Aibu mode.
                    outputY = 1;
                }
                else
                {
                    outputY = 0;
                }
                return;
            }

            outputY = 0;

            // No-turning-back point, stay in gauge hit area
            if (hCtrl.feel_f > 0.965f && hCtrl.isGaugeHit) return;

            float bufferRadius = hCtrl.wheelActionCount;
            var clampedSpeed = Mathf.Clamp(smoothTargetSpeed, 0, 2);
            if (hCtrl.speed < LOOP_01_DIVIDER)
            {
                if (clampedSpeed < LOOP_1_ACTIVATION_THRESHOLD) clampedSpeed = Mathf.Min(clampedSpeed, LOOP_01_DIVIDER - bufferRadius);
            }
            else
            {
                if (clampedSpeed > LOOP_1_DEACTIVATION_THRESHOLD) clampedSpeed = Mathf.Max(clampedSpeed, LOOP_01_DIVIDER + bufferRadius);
            }

            // if (clampedSpeed >= hCtrl.speed + bufferRadius) outputY = 1;
            //if (clampedSpeed <= hCtrl.speed - bufferRadius) outputY = -1;

            hCtrl.speed = clampedSpeed;
        }

        internal bool Spnk(HScene hScene, HSceneFlagCtrl ctrl)
        {
            var anim = hScene.GetProcBase();
            if (anim == null || !(anim is Spnking)) return false;
            return anim.Proc(GetModeCtrl(hScene), ctrl.nowAnimationInfo, 1);
        }

        internal void AddSourceGesture(HSpeedGesture gesture)
        {
            sourceGestures.Add(gesture);
        }

        internal void RemoveSourceGesture(HSpeedGesture gesture)
        {
            sourceGestures.Remove(gesture);
        }

        private void UpdateSmoothTargetSpeed(HSceneFlagCtrl hCtrl, float deltaTime)
        {
            float targetSpeed = IDLE_SPEED;
            bool hasMildTouching = false;

            foreach (var gesture in sourceGestures)
            {
                if (!gesture.isTouching) continue;

                targetSpeed = Mathf.Max(targetSpeed, 0);

                if (gesture.interactingCollider.sensitivityLevel == 1)
                {
                    if (gesture.speed > 0 && gesture.speed < 1.75f) hasMildTouching = true;
                    if (hCtrl.loopType > 0 || smoothTargetSpeed > HSpeedGestureReceiver.LOOP_01_DIVIDER - hCtrl.wheelActionCount)
                    {
                        // Do not let touching less sensitive parts affect speed during fast movement.
                        if (!IsHoushi()) continue;
                    }
                }

                var speedInput = Mathf.Clamp(gesture.speed - 0.125f, 0, 2f);

                if (hCtrl.loopType == 2)
                {
                    // Curve speed down to require faster movement.
                    speedInput *= speedInput / 2;
                }
                else if (gesture.interactingCollider.sensitivityLevel >= 3)
                {
                    // Curve speed up to increase sensitivity.
                    speedInput = Mathf.Sqrt(speedInput * 2);
                }

                targetSpeed = Mathf.Max(targetSpeed, speedInput);
            }

            float accelerationFactor = 1;
            if (hCtrl.isGaugeHit) {
                // Damp the speed more when it is in gauge hit zone to avoid voice flickering;
                // Reward mild collider touching in fast loops by stabilizing gauge hit more.
                accelerationFactor = hasMildTouching && hCtrl.loopType > 0 ? 0.125f : 0.25f;
            }
            else if (hCtrl.loopType == -1)
            {
                // Damp the speed to delay starting Aibu.
                accelerationFactor = 0.25f;
            }

            smoothTargetSpeed = Mathf.Lerp(smoothTargetSpeed, targetSpeed, deltaTime * accelerationFactor);

            // BetterVRPlugin.Logger.LogWarning(
            //    "H Loop type: " +  hCtrl.loopType + " current speed: " + hCtrl.speed +
            //    " smooth target speed: " + smoothTargetSpeed);
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
