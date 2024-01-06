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
                if (!_leftHandIndicator) _leftHandIndicator = CreateIndicator(BetterVRPluginHelper.leftControllerCenter);
                return _leftHandIndicator;
            }
        }
        private TextMeshPro rightHandIndicator
        {
            get
            {
                if (!_rightHandIndicator) _rightHandIndicator = CreateIndicator(BetterVRPluginHelper.rightControllerCenter);
                return _rightHandIndicator;
            }
        }

        internal void UpdateIndicators(bool isHSpeedGestureEffective)
        {
            if (!leftHandIndicator || !rightHandIndicator || !headIndicator) return;

            if (IsGaugeHit() && (isHSpeedGestureEffective || smoothGaugeHit > 0))
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

            UpdateIndicatorSizeAndColor();

            var camera = BetterVRPluginHelper.VRCamera;
            var vrOrigin = BetterVRPluginHelper.VROrigin;

            if (camera && vrOrigin)
            {
                leftHandIndicator.transform.localPosition =
                    leftHandIndicator.transform.parent.InverseTransformDirection(vrOrigin.transform.up) * 0.0625f;
                rightHandIndicator.transform.localPosition =
                    rightHandIndicator.transform.parent.InverseTransformDirection(vrOrigin.transform.up) * 0.0625f;
                headIndicator.transform.position =
                    Vector3.SmoothDamp(
                        headIndicator.transform.position,
                        camera.transform.TransformPoint(0, 0.25f, 0.75f),
                        ref headIndicatorVelocity,
                        1f);

                leftHandIndicator.transform.LookAt(camera.transform.position, vrOrigin.transform.up);
                rightHandIndicator.transform.LookAt(camera.transform.position, vrOrigin.transform.up);
                headIndicator.transform.LookAt(camera.transform.position, vrOrigin.transform.up);

            }
        }

        private bool IsGaugeHit()
        {
            var ctrl = Singleton<HSceneFlagCtrl>.Instance;
            return ctrl != null && ctrl.isGaugeHit && ctrl.loopType != -1;
        }

        private float GetFeelLevel()
        {
            return Singleton<HSceneFlagCtrl>.Instance?.feel_f ?? 0;
        }

        private void UpdateIndicatorSizeAndColor()
        {
            var feelLevel = GetFeelLevel();
            Color color;
            if (feelLevel > 0.98 || (feelLevel > 0.74f && feelLevel < 0.75f))
            {
                // Pulsing color
                color = Color.Lerp(FINISH_COLOR, Color.white, Mathf.Abs((Time.time / 0.25f) % 2 - 1));
            }
            else
            {
                color = Color.Lerp(START_COLOR, FINISH_COLOR, feelLevel);
            }

            if (headIndicator)
            {
                headIndicator.transform.localScale = Vector3.one * smoothGaugeHit / 32;
                headIndicator.color = color;
            }
            if (leftHandIndicator) {
                leftHandIndicator.transform.localScale = Vector3.one * smoothGaugeHit / 64;
                leftHandIndicator.color = color;
            }
            if (rightHandIndicator)
            {
                rightHandIndicator.transform.localScale = Vector3.one * smoothGaugeHit / 64;
                rightHandIndicator.color = color;
            }
        }

        private static TextMeshPro CreateIndicator(Transform cursorAttach)
        {
            if (!cursorAttach) return null;
            var textMesh =
                new GameObject().AddComponent<Canvas>().gameObject.AddComponent<TextMeshPro>();
            textMesh.transform.SetParent(cursorAttach);
            textMesh.text = "\u2665";
            textMesh.fontSize = 16;
            textMesh.color = new Color(1, 0.25f, 0.25f);
            textMesh.alignment = TextAlignmentOptions.Center;
            // var material = textMesh.fontMaterial;
            // material.renderQueue = (int) UnityEngine.Rendering.RenderQueue.Overlay;
            // textMesh.fontMaterial = material;
            // textMesh.sort
            return textMesh;
        }
    }
}
