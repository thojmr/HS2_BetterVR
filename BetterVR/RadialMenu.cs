using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using TMPro;

namespace BetterVR
{
    public class RadialMenu : MonoBehaviour 
    {
        private const int ITEM_COUNT = 8;
        private const float RADIUS = 1 / 16f;
        private const float DEADZONE_RADIUS = 1 / 32f;
        private const float ACTIVATION_DELAY_SECONDS = 0.33f;

        private GameObject cursor;
        private TextMeshPro caption;
        private List<TextMeshPro> icons;
        private LineRenderer lineRenderer;
        private float activationTime;

        internal int selectedItemIndex { get; private set; }
        internal Transform hand;
        private string[] _captions;
        internal string[] captions
        {
            private get { return _captions; }
            set
            {
                _captions = value;
                if (value == null) return;
                for (int i = 0; i < _captions.Length; i++)
                {
                    icons[i].text = _captions[i] == "" ? "-" : _captions[i].Substring(0, 1);
                }
            }
        }

		void Awake()
        {
            CreateLines();
            CreateCursor();
            CreateCaption();
        }

        void OnEnable()
        {
            transform.parent = hand;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.Euler(90, 0, 0);
            transform.localScale = Vector3.one;
            transform.SetParent(BetterVRPluginHelper.VROrigin?.transform, worldPositionStays: true);
            selectedItemIndex = -1;
            activationTime = 0;
            lineRenderer.enabled = false;
            foreach (var icon in icons) icon.enabled = false;
        }

        void Update()
        {
            transform.position += Vector3.Project(hand.position - transform.position, transform.forward);

            activationTime += Time.deltaTime;

            if (activationTime > ACTIVATION_DELAY_SECONDS)
            {
                lineRenderer.enabled = true;
                foreach (var icon in icons) icon.enabled = true;
            }

            Vector2 handProjection = transform.InverseTransformPoint(hand.position);
            float handOffsetAmount = handProjection.magnitude;
            if (handOffsetAmount < DEADZONE_RADIUS || activationTime < ACTIVATION_DELAY_SECONDS)
            {
                selectedItemIndex = -1;
                cursor.transform.position = hand.position;
                cursor.GetComponent<MeshRenderer>().material.color = Color.gray;
                caption.text = "";
                return;
            }

            var angle = Vector2.SignedAngle(Vector2.right, handProjection);
            int signedIndex = Mathf.RoundToInt(angle / 360 * ITEM_COUNT) % ITEM_COUNT;
            selectedItemIndex = signedIndex >= 0 ? signedIndex : signedIndex + ITEM_COUNT;

            cursor.GetComponent<MeshRenderer>().material.color = Color.yellow;
            cursor.transform.localPosition =
                GetCursorLocalDirection(selectedItemIndex) * Mathf.Clamp(handOffsetAmount, 0, RADIUS);
            caption.text = GetCaption(selectedItemIndex);
        }

        private Vector3 GetCursorLocalDirection(int itemIndex)
        {
            float angle = Mathf.PI * 2 * itemIndex / ITEM_COUNT;
            return new Vector3(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        private string GetCaption(int itemIndex)
        {
            if (itemIndex < 0 || captions == null || captions.Length <= itemIndex) return "";
            return captions[itemIndex];
        }

        private void CreateLines()
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = false;
            lineRenderer.positionCount = ITEM_COUNT * 2;
            lineRenderer.widthMultiplier = 0.01f;
            lineRenderer.material.color = Color.red;

            for (int i = 0; i < ITEM_COUNT; i++)
            {
                var cursorDirection = GetCursorLocalDirection(i);
                lineRenderer.SetPosition(i * 2, Vector3.zero);
                lineRenderer.SetPosition(i * 2 + 1, cursorDirection * RADIUS);
            }
        }

        private void CreateCursor()
        {
            cursor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cursor.transform.parent = transform;
            cursor.transform.localScale = Vector3.one * RADIUS / 4;
            var renderer = cursor.GetComponent<MeshRenderer>();
            renderer.receiveShadows = false;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            Destroy(cursor.GetComponent<Collider>());
        }

        private void CreateCaption()
        {
            var captionCanvas = new GameObject("RadialMenuCaptionCanvas").AddComponent<Canvas>();
            captionCanvas.transform.SetParent(transform, worldPositionStays: false);
            captionCanvas.transform.localScale = Vector3.one * 0.01f;
            captionCanvas.GetComponent<RectTransform>().sizeDelta = Vector3.one * 400 * RADIUS;

            var captionTransform = new GameObject("RadialMenuCaption").AddComponent<RectTransform>();
            captionTransform.transform.SetParent(captionCanvas.transform, worldPositionStays: false);
            captionTransform.localScale = Vector3.one;
            captionTransform.anchorMin = captionTransform.anchorMax = new Vector2(0.5f, 0.875f);
            captionTransform.offsetMin = new Vector2(-0.5f, -0.125f);
            captionTransform.offsetMax = new Vector2(0.5f, 0.125f);

            caption = captionTransform.GetOrAddComponent<TextMeshPro>();
            caption.alignment = TextAlignmentOptions.Center;
            caption.fontSize = 16f;
            caption.color = Color.yellow;
            caption.m_width = captionTransform.sizeDelta.x;

            icons = new List<TextMeshPro>();
            for (int i = 0; i < ITEM_COUNT; i++)
            {
                var iconTransform = new GameObject("RadialMenuIcon" + i).AddComponent<RectTransform>();
                iconTransform.transform.SetParent(captionCanvas.transform, worldPositionStays: false);
                iconTransform.transform.localScale = Vector3.one;
                iconTransform.anchorMin = iconTransform.anchorMax = Vector3.one * 0.5f + GetCursorLocalDirection(i) * 0.3125f;
                var icon = iconTransform.GetOrAddComponent<TextMeshPro>();
                icon.alignment = TextAlignmentOptions.Center;
                icon.fontSize = 12f;
                icon.color = new Color(0.75f, 0.25f, 0);
                icons.Add(icon);
            }
        }
    }
}
