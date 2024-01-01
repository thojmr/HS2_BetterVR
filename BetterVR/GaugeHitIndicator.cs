using TMPro;
using UnityEngine;

namespace BetterVR
{
    internal class GaugeHitIndicator
    {
        private float smoothGaugeHit = 0;
        private float acceleration;
        private Vector3 headIndicatorVelocity = Vector3.zero;
        private GameObject _headIndicator = null;
        private GameObject _leftHandIndicator = null;
        private GameObject _rightHandIndicator = null;
        private GameObject headIndicator
        {
            get
            {
                if (!_headIndicator) _headIndicator = CreateIndicator(BetterVRPluginHelper.VROrigin?.transform);
                return _headIndicator;
            }
        }
        private GameObject leftHandIndicator
        {
            get
            {
                if (!_leftHandIndicator) _leftHandIndicator = CreateIndicator(BetterVRPluginHelper.leftCursorAttach);
                return _leftHandIndicator;
            }
        }
        private GameObject rightHandIndicator
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
            leftHandIndicator.SetActive(true);
            rightHandIndicator.SetActive(true);
            headIndicator.SetActive(true);

            UpdateIndicatorSize();
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

            if (!leftHandIndicator.activeSelf && !rightHandIndicator.activeSelf && !headIndicator.activeSelf)
            {
                smoothGaugeHit = 0;
                return;
            }

            smoothGaugeHit = Mathf.SmoothDamp(smoothGaugeHit, -0.125f, ref acceleration, 0.25f);
            if (smoothGaugeHit < 0)
            {
                smoothGaugeHit = 0;
                headIndicator.SetActive(false);
                leftHandIndicator.SetActive(false);
                rightHandIndicator.SetActive(false);
            }
            else
            {
                UpdateIndicatorSize();
            }
        }

        private bool IsGaugeHit()
        {
            return Singleton<HSceneFlagCtrl>.Instance?.isGaugeHit ?? false;
        }

        private void UpdateIndicatorSize()
        {
            if (headIndicator) headIndicator.transform.localScale =
                    Vector3.one * 0.03f * smoothGaugeHit;
            if (leftHandIndicator) leftHandIndicator.transform.localScale =
                    Vector3.one * 0.02f * smoothGaugeHit;
            if (rightHandIndicator) rightHandIndicator.transform.localScale =
                    Vector3.one * 0.02f * smoothGaugeHit;
        }

        private static GameObject CreateIndicator(Transform cursorAttach)
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
            return textMesh.gameObject;
        }
    }
}
