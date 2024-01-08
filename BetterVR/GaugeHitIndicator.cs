using TMPro;
using UnityEngine;

namespace BetterVR
{
    internal class GaugeHitIndicator
    {
        private static readonly Color START_COLOR = new Color(0.5f, 0.25f, 0.25f);
        private static readonly Color FINISH_COLOR = new Color(1, 0.125f, 0.125f);
        private float smoothGaugeHit = 0;
        private float acceleration;
        private Vector3 headIndicatorVelocity = Vector3.zero;
        private TextMeshPro _headIndicator = null;
        private TextMeshPro _leftHandIndicator = null;
        private TextMeshPro _rightHandIndicator = null;
        private TextMeshPro headIndicator
        {
            get
            {
                if (!_headIndicator) _headIndicator = CreateIndicator();
                return _headIndicator;
            }
        }
        private TextMeshPro leftHandIndicator
        {
            get
            {
                if (!_leftHandIndicator || _leftHandIndicator.gameObject == null) _leftHandIndicator = CreateIndicator();
                return _leftHandIndicator;
            }
        }
        private TextMeshPro rightHandIndicator
        {
            get
            {
                if (!_rightHandIndicator || _rightHandIndicator.gameObject == null) _rightHandIndicator = CreateIndicator();
                return _rightHandIndicator;
            }
        }

        internal void UpdateIndicators(bool isHSpeedGestureEffective)
        {
            if (!leftHandIndicator || !rightHandIndicator || !headIndicator) return;

            bool isGaugeHit = IsGaugeHit();

            if (isGaugeHit && (isHSpeedGestureEffective || smoothGaugeHit > 0))
            {
                leftHandIndicator.gameObject.SetActive(true);
                rightHandIndicator.gameObject.SetActive(true);
                headIndicator.gameObject.SetActive(true);
                smoothGaugeHit = Mathf.SmoothDamp(smoothGaugeHit, 1, ref acceleration, 0.125f);
            }
            else if (!leftHandIndicator.isActiveAndEnabled && !rightHandIndicator.isActiveAndEnabled && !headIndicator.isActiveAndEnabled)
            {
                return;
            }
            else
            {
                smoothGaugeHit = Mathf.SmoothDamp(smoothGaugeHit, -0.125f, ref acceleration, 0.25f);
                if (smoothGaugeHit < 0)
                {
                    smoothGaugeHit = 0;
                    acceleration = 0;
                    headIndicator.gameObject.SetActive(false);
                    leftHandIndicator.gameObject.SetActive(false);
                    rightHandIndicator.gameObject.SetActive(false);
                    return;
                }
            }

            var camera = BetterVRPluginHelper.VRCamera;
            var vrOrigin = BetterVRPluginHelper.VROrigin;

            if (camera == null || vrOrigin == null) return;

            var upDirectionInCamera = camera.transform.TransformVector(
                ((Vector2) camera.transform.InverseTransformVector(vrOrigin.transform.up)).normalized);

            var targetPosition = GetHeadIndicatorTargetPosition(camera.transform, upDirectionInCamera * 0.375f);
            if (smoothGaugeHit < 1 / 64f)
            {
                headIndicator.transform.position = targetPosition;
                headIndicatorVelocity = Vector3.zero;
            }
            else
            {
                headIndicator.transform.position = Vector3.SmoothDamp(
                    headIndicator.transform.position,
                    targetPosition,
                    ref headIndicatorVelocity,
                    1f);
            }
            headIndicator.transform.LookAt(camera.transform.position, vrOrigin.transform.up);

            var offsetFromHand = upDirectionInCamera * 0.0625f;

            if (BetterVRPluginHelper.leftControllerCenter != null) {
                leftHandIndicator.transform.position = BetterVRPluginHelper.leftControllerCenter.position + offsetFromHand;
                leftHandIndicator.transform.LookAt(camera.transform.position, vrOrigin.transform.up);
            }

            if (BetterVRPluginHelper.rightControllerCenter != null)
            {
                rightHandIndicator.transform.position = BetterVRPluginHelper.rightControllerCenter.position + offsetFromHand;
                rightHandIndicator.transform.LookAt(camera.transform.position, vrOrigin.transform.up);
            }

            UpdateSizeAndColor(isGaugeHit);
        }

        private bool IsGaugeHit()
        {
            var ctrl = Singleton<HSceneFlagCtrl>.Instance;
            return ctrl != null && ctrl.isGaugeHit && ctrl.loopType != -1;
        }

        private Vector3 GetHeadIndicatorTargetPosition(Transform camera, Vector3 offset)
        {
            var characters = Singleton<Manager.HSceneManager>.Instance?.Hscene?.GetFemales();

            // Default position of the gauge hit indicator relative to the camera.
            var p = new Vector3(0, 0.25f, 1f);

            if (characters != null)
            {
                foreach (var character in characters)
                {
                    if (character == null || !character.isActiveAndEnabled || !character.visibleAll) continue;
                    // A spot above the character's head.
                    var target =
                        camera.InverseTransformPoint(character.objHeadBone.transform.TransformPoint(Vector3.up * 0.125f) + offset);
                    
                    // If the target is too near or too far, do not use it.
                    if (target.z < 0.125f || target.z > 1) continue;

                    // If the target is too much off the center of view, do not use it.
                    if (Mathf.Abs(target.x) > target.z * 0.75f || Mathf.Abs(target.y) > target.z * 0.75f) continue;
                    
                    // Snap gauge hit indicator above the character's head.
                    p = target;
                    break;
                }
            }
            return camera.TransformPoint(p);
        }

        private void UpdateSizeAndColor(bool isGaugeHit)
        {
            var feelLevel = Singleton<HSceneFlagCtrl>.Instance?.feel_f ?? 0;
            Color color =
                ShouldUsePulsingColor(isGaugeHit, feelLevel) ?
                Color.Lerp(FINISH_COLOR, Color.white, Mathf.Abs((Time.time * 4) % 2 - 1)) :
                Color.Lerp(START_COLOR, FINISH_COLOR, feelLevel * feelLevel);

            if (headIndicator)
            {
                headIndicator.transform.localScale = Vector3.one * smoothGaugeHit / 32;
                headIndicator.color = color;
            }
            if (leftHandIndicator)
            {
                leftHandIndicator.transform.localScale = Vector3.one * smoothGaugeHit / 64;
                leftHandIndicator.color = color;
            }
            if (rightHandIndicator)
            {
                rightHandIndicator.transform.localScale = Vector3.one * smoothGaugeHit / 64;
                rightHandIndicator.color = color;
            }
        }

        private static bool ShouldUsePulsingColor(bool isGaugeHit, float feelLevel)
        {
            if (!isGaugeHit) return false;
            if (feelLevel > 0.735f && feelLevel < 0.75f) return true;
            return feelLevel > 0.97f;
        }

        private static TextMeshPro CreateIndicator()
        {
            Transform parent = BetterVRPluginHelper.VROrigin?.transform;
            if (parent == null) return null;
            var textMesh =
                new GameObject().AddComponent<Canvas>().gameObject.AddComponent<TextMeshPro>();
     
            textMesh.transform.SetParent(parent);
            textMesh.text = "\u2665";
            textMesh.fontSize = 16;
            textMesh.color = new Color(1, 0.25f, 0.25f);
            textMesh.alignment = TextAlignmentOptions.Center;

            return textMesh;
        }
    }
}
