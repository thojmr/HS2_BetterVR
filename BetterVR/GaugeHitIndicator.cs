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
                if (!_headIndicator) _headIndicator = CreateIndicator(BetterVRPluginHelper.VROrigin?.transform);
                return _headIndicator;
            }
        }
        private TextMeshPro leftHandIndicator
        {
            get
            {
                if (!_leftHandIndicator) _leftHandIndicator = CreateIndicator(BetterVRPluginHelper.leftCursorAttach);
                return _leftHandIndicator;
            }
        }
        private TextMeshPro rightHandIndicator
        {
            get
            {
                if (!_rightHandIndicator) _rightHandIndicator = CreateIndicator(BetterVRPluginHelper.rightCursorAttach);
                return _rightHandIndicator;
            }
        }

        internal void ShowIfGaugeIsHit()
        {
            if (!IsGaugeHit()) return;
            if (!leftHandIndicator || !rightHandIndicator || !headIndicator) return;

            smoothGaugeHit = Mathf.SmoothDamp(smoothGaugeHit, 1, ref acceleration, 0.125f);
            leftHandIndicator.gameObject.SetActive(true);
            rightHandIndicator.gameObject.SetActive(true);
            headIndicator.gameObject.SetActive(true);

            UpdateIndicatorSizeAndColor();
            var camera = BetterVRPluginHelper.VRCamera;
            var vrOrigin = BetterVRPluginHelper.VROrigin;
            if (camera == null || vrOrigin == null) return;
            leftHandIndicator.transform.LookAt(camera.transform.position, vrOrigin.transform.up);
            rightHandIndicator.transform.LookAt(camera.transform.position, vrOrigin.transform.up);
            headIndicator.transform.LookAt(camera.transform.position, vrOrigin.transform.up);
        }

        internal void UpdateIndicators()
        {
            var camera = BetterVRPluginHelper.VRCamera;
            var vrOrigin = BetterVRPluginHelper.VROrigin;
            if (camera && vrOrigin)
            {
                headIndicator.transform.position =
                Vector3.SmoothDamp(
                    headIndicator.transform.position,
                    camera.transform.TransformPoint(0, 0.3125f, 0.75f),
                    ref headIndicatorVelocity,
                    1f);
            }

            if (IsGaugeHit()) return;
            if (!leftHandIndicator || !rightHandIndicator || !headIndicator) return;

            if (!leftHandIndicator.isActiveAndEnabled && !rightHandIndicator.isActiveAndEnabled && !headIndicator.isActiveAndEnabled)
            {
                smoothGaugeHit = 0;
                return;
            }

            smoothGaugeHit = Mathf.SmoothDamp(smoothGaugeHit, -0.125f, ref acceleration, 0.25f);
            if (smoothGaugeHit < 0)
            {
                smoothGaugeHit = 0;
                headIndicator.gameObject.SetActive(false);
                leftHandIndicator.gameObject.SetActive(false);
                rightHandIndicator.gameObject.SetActive(false);
            }
            else
            {
                UpdateIndicatorSizeAndColor();
            }
        }

        private bool IsGaugeHit()
        {
            return Singleton<HSceneFlagCtrl>.Instance?.isGaugeHit ?? false;
        }

        private float GetFeelLevel()
        {
            return Singleton<HSceneFlagCtrl>.Instance?.feel_f ?? 0;
        }

        private void UpdateIndicatorSizeAndColor()
        {
            Color color = Color.Lerp(START_COLOR, FINISH_COLOR, GetFeelLevel());
            if (headIndicator)
            {
                headIndicator.transform.localScale = Vector3.one * 0.03f * smoothGaugeHit;
                headIndicator.color = color;
            }
            if (leftHandIndicator) {
                leftHandIndicator.transform.localScale = Vector3.one * 0.02f * smoothGaugeHit;
                leftHandIndicator.color = color;
            }
            if (rightHandIndicator)
            {
                rightHandIndicator.transform.localScale = Vector3.one * 0.02f * smoothGaugeHit;
                rightHandIndicator.color = color;
            }
        }

        private static TextMeshPro CreateIndicator(Transform cursorAttach)
        {
            if (!cursorAttach) return null;
            var textMesh =
                new GameObject().AddComponent<Canvas>().gameObject.AddComponent<TextMeshPro>();
            textMesh.transform.SetParent(cursorAttach);
            textMesh.transform.localPosition = Vector3.back * 0.0625f;
            textMesh.text = "\u2665";
            textMesh.fontSize = 16;
            textMesh.color = new Color(1, 0.25f, 0.25f);
            textMesh.alignment = TextAlignmentOptions.Center;
            return textMesh;
        }
    }
}
