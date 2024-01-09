using TMPro;
using UnityEngine;

namespace BetterVR
{
    internal class GaugeHitIndicator : MonoBehaviour
    {
        private static readonly Color START_COLOR = new Color(0.5f, 0.25f, 0.25f);
        private static readonly Color FINISH_COLOR = Color.red;
        private const int H_CAMERA_LAYER = 22;

        internal float smoothGaugeHit { get; private set; }  = 0;
        private float gaugeAcceleration;
        private Vector3 velocity = Vector3.zero;

        private static Camera _gaugeCamera;
        private static Camera gaugeCamera
        {
            get
            {
                var vrCamera = BetterVRPluginHelper.VRCamera;
                if (vrCamera == null) return null;
                if (_gaugeCamera == null)
                {
                    _gaugeCamera = new GameObject().AddComponent<Camera>();
                    _gaugeCamera.clearFlags = CameraClearFlags.Depth;
                    BetterVRPlugin.Logger.LogInfo("VRCamera depth: " + vrCamera.depth + " culling mask: " + vrCamera.cullingMask);
                    _gaugeCamera.depth = 1;
                    _gaugeCamera.cullingMask = 1 << H_CAMERA_LAYER;
                    _gaugeCamera.renderingPath = RenderingPath.VertexLit;
                }
                if (_gaugeCamera.transform.parent != vrCamera.transform.parent)
                {
                    // There is some script that automaticallly moves all cameras in the scene with HMD,
                    // so there is no need to parent the UI camera to the VR camera itself.
                    _gaugeCamera.transform.parent = vrCamera.transform.parent;
                    _gaugeCamera.transform.position = vrCamera.transform.position;
                    _gaugeCamera.transform.rotation = vrCamera.transform.rotation;
                    _gaugeCamera.transform.localScale = vrCamera.transform.localScale;
                }
                return _gaugeCamera;
            }
        }

        private TextMeshPro _heartSymbol = null;
        private TextMeshPro _horizontalLines = null;
        private TextMeshPro heartSymbol
        {
            get { return _heartSymbol ?? (_heartSymbol = CreateSymbol("\u2665")); }
        }
        private TextMeshPro horizontalLines
        {
            get { return _horizontalLines ?? (_horizontalLines = CreateSymbol("- -")); }
        }

        void FixedUpdate()
        {
            var ctrl = Singleton<HSceneFlagCtrl>.Instance;
            var camera = BetterVRPluginHelper.VRCamera;
            var vrOrigin = BetterVRPluginHelper.VROrigin;
            if (!camera || !vrOrigin || !ctrl || !heartSymbol || !horizontalLines) return;

            bool isGaugeHit = ctrl.isGaugeHit && ctrl.loopType != -1;

            if (transform.parent != vrOrigin.transform)
            {
                transform.parent = vrOrigin.transform;
                transform.localScale = Vector3.one / 32;
            }

            if (isGaugeHit)
            {
                gaugeCamera.enabled = true;
                smoothGaugeHit = Mathf.SmoothDamp(smoothGaugeHit, 1, ref gaugeAcceleration, 0.125f);
            }
            else
            {
                smoothGaugeHit = Mathf.SmoothDamp(smoothGaugeHit, -0.125f, ref gaugeAcceleration, 0.25f);
                if (smoothGaugeHit < 0)
                {
                    smoothGaugeHit = 0;
                    gaugeAcceleration = 0;
                    gameObject.SetActive(false);
                    return;
                }
            }

            var upDirectionInCamera = camera.transform.TransformVector(
                ((Vector2) camera.transform.InverseTransformVector(vrOrigin.transform.up)).normalized);
            var targetPosition = GetSnapPosition(camera.transform, upDirectionInCamera);
            bool snappedToTarget = (targetPosition != null);

            if (targetPosition == null)
            {
                targetPosition = camera.transform.TransformPoint(0f, 0, 0.5f) + 0.1875f * upDirectionInCamera;
            }

            if (smoothGaugeHit > 1 / 64f)
            {
                transform.position = Vector3.SmoothDamp(
                    transform.position, (Vector3) targetPosition, ref velocity, 1f);
            }
            else
            {
                transform.position = (Vector3)targetPosition;
                velocity = Vector3.zero;
            }

            transform.LookAt(camera.transform.position, upDirectionInCamera);

            UpdateSymbols(ctrl, snappedToTarget);
        }

        private Vector3? GetSnapPosition(Transform camera, Vector3 upDirection)
        {
            var characters = Singleton<Manager.HSceneManager>.Instance?.Hscene?.GetFemales();

            if (characters != null)
            {
                foreach (var character in characters)
                {
                    if (character == null || !character.isActiveAndEnabled || !character.visibleAll) continue;
                    var head = character.objHeadBone.transform.position;
                    var target = head + upDirection * 0.25f;
                    if (isTargetInSnapRange(target, camera)) return target; 
                }
            }

            return null;
        }

        private TextMeshPro CreateSymbol(string text)
        {
            var textMesh = new GameObject().AddComponent<Canvas>().gameObject.AddComponent<TextMeshPro>();
            textMesh.transform.SetParent(transform);
            textMesh.transform.localPosition = Vector3.zero;
            textMesh.transform.localRotation = Quaternion.identity;
            textMesh.text = text;
            textMesh.fontSize = 16;
            textMesh.color = new Color(1, 0.25f, 0.25f);
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.gameObject.layer = H_CAMERA_LAYER;

            textMesh.renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            textMesh.renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            textMesh.renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

            return textMesh;
        }

        private void UpdateSymbols(HSceneFlagCtrl ctrl, bool snappedToTarget)
        {
            var feelLevel = ctrl.feel_f;
            Color color =
                ShouldUsePulsingColor(ctrl.isGaugeHit, feelLevel) ?
                Color.Lerp(FINISH_COLOR, Color.white, Mathf.Abs(GetPulsePhase(Singleton<HSceneFlagCtrl>.Instance))) :
                Color.Lerp(START_COLOR, FINISH_COLOR, feelLevel * feelLevel);

            heartSymbol.transform.localScale = Vector3.one * smoothGaugeHit;
            heartSymbol.color = color;
            heartSymbol.transform.localRotation = GetRotationPulse(Singleton<HSceneFlagCtrl>.Instance);

            float h = horizontalLines.transform.localScale.y;
            h = Mathf.Lerp(h, snappedToTarget ? -0.5f : 1.25f, Time.deltaTime * 2);
            horizontalLines.transform.localScale = new Vector3(smoothGaugeHit * 5, Mathf.Clamp(h, 0, smoothGaugeHit), 1);
            horizontalLines.color = color;
        }

        internal static float GetPulsePhase(HSceneFlagCtrl ctrl)
        {
            if (ctrl == null) return 0;
            if (ctrl.feel_f >= 0.96f) return (Time.time * 3.5f) % 2 - 1;
            if (ctrl.feel_f >= 0.75f) return (Time.time * 2) % 2 - 1;
            return (Time.time * 1.5f) % 2 - 1;
        }

        private static bool ShouldUsePulsingColor(bool isGaugeHit, float feelLevel)
        {
            if (!isGaugeHit) return false;
            if (feelLevel > 0.74f && feelLevel < 0.75f) return true;
            return feelLevel > 0.97f;
        }

        private static Quaternion GetRotationPulse(HSceneFlagCtrl ctrl)
        {
            return Quaternion.Euler(0, 0, Mathf.Lerp(-15, 15, Mathf.Abs(GetPulsePhase(ctrl))));
        }

        private static bool isTargetInSnapRange(Vector3 target, Transform camera)
        {
            var localPoint = camera.InverseTransformPoint(target);

            // If the target is too near or too far, do not snap the indicator to it.
            if (localPoint.z < 0.125f || localPoint.z > 0.75f) return false;

            // If the target is too much off the center of view, do not snap the indicator to it.
            if (Mathf.Abs(localPoint.x) > localPoint.z * 0.375f) return false;
            if (localPoint.y < localPoint.z * (-0.5f) || localPoint.y > localPoint.z * 0.25f) return false;

            return true;
        }
    }
}
