using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using FantasyDefense.Audio;
using FantasyDefense.Mobile;

namespace FantasyDefense.UI
{
    /// <summary>
    /// Mengubah uGUI Toggle menjadi Sakelar Geser Fantasi Tactile (Smooth Sliding Switch):
    /// Menggeser knob permata emas secara halus dari kiri (OFF) ke kanan (ON),
    /// serta mengatur intensitas warna/cahaya knob dan getaran haptic saat status berubah.
    /// </summary>
    [RequireComponent(typeof(Toggle))]
    [DisallowMultipleComponent]
    public class UISlidingToggleSwitch : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform knobRect;
        [SerializeField] private Image knobImage;

        [Header("Animation Settings")]
        [SerializeField] private float slideDuration = 0.16f;
        [SerializeField] private float offAnchorX = 0.28f;
        [SerializeField] private float onAnchorX = 0.72f;

        [Header("Color States")]
        [SerializeField] private Color onColor = Color.white;
        [SerializeField] private Color offColor = new Color(0.65f, 0.70f, 0.78f, 0.88f);

        private Toggle toggle;
        private Coroutine slideRoutine;
        private bool isInitialized = false;

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (isInitialized) return;
            toggle = GetComponent<Toggle>();
            if (toggle == null) return;

            // Cari knob child jika belum diatur
            if (knobRect == null)
            {
                var check = transform.Find("Background/Checkmark");
                if (check != null)
                {
                    knobRect = check.GetComponent<RectTransform>();
                    knobImage = check.GetComponent<Image>();
                }
            }

            // Lepaskan toggle.graphic agar Unity tidak menyembunyikan knob saat OFF
            if (toggle.graphic != null && knobRect != null && toggle.graphic.transform == knobRect)
            {
                toggle.graphic = null;
            }

            toggle.onValueChanged.AddListener(OnToggleChanged);
            ApplyInstantState(toggle.isOn);
            isInitialized = true;
        }

        private void OnDestroy()
        {
            if (toggle != null)
            {
                toggle.onValueChanged.RemoveListener(OnToggleChanged);
            }
        }

        private void OnEnable()
        {
            if (toggle != null)
            {
                ApplyInstantState(toggle.isOn);
            }
        }

        public void SetKnob(RectTransform rect, Image img)
        {
            knobRect = rect;
            knobImage = img;
            if (toggle != null)
            {
                if (toggle.graphic != null && toggle.graphic.transform == knobRect)
                {
                    toggle.graphic = null;
                }
                ApplyInstantState(toggle.isOn);
            }
        }

        private void OnToggleChanged(bool isOn)
        {
            if (!gameObject.activeInHierarchy)
            {
                ApplyInstantState(isOn);
                return;
            }

            if (slideRoutine != null) StopCoroutine(slideRoutine);
            slideRoutine = StartCoroutine(SlideRoutine(isOn));

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.ButtonClick);
            }

            if (DevicePerformanceManager.Instance != null)
            {
                DevicePerformanceManager.Instance.TriggerHaptic(HapticFeedbackType.Light);
            }
        }

        private void ApplyInstantState(bool isOn)
        {
            if (knobRect == null) return;

            float targetX = isOn ? onAnchorX : offAnchorX;
            Vector2 aMin = knobRect.anchorMin;
            Vector2 aMax = knobRect.anchorMax;
            float width = aMax.x - aMin.x;
            aMin.x = targetX - width * 0.5f;
            aMax.x = targetX + width * 0.5f;
            knobRect.anchorMin = aMin;
            knobRect.anchorMax = aMax;
            knobRect.anchoredPosition = Vector2.zero;

            if (knobImage != null)
            {
                knobImage.color = isOn ? onColor : offColor;
                knobImage.canvasRenderer.SetAlpha(1f);
            }
        }

        private IEnumerator SlideRoutine(bool isOn)
        {
            if (knobRect == null) yield break;

            float startX = (knobRect.anchorMin.x + knobRect.anchorMax.x) * 0.5f;
            float targetX = isOn ? onAnchorX : offAnchorX;
            Color startColor = knobImage != null ? knobImage.color : Color.white;
            Color targetColor = isOn ? onColor : offColor;
            float width = knobRect.anchorMax.x - knobRect.anchorMin.x;

            float elapsed = 0f;
            while (elapsed < slideDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / slideDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                float curX = Mathf.Lerp(startX, targetX, smoothT);
                knobRect.anchorMin = new Vector2(curX - width * 0.5f, knobRect.anchorMin.y);
                knobRect.anchorMax = new Vector2(curX + width * 0.5f, knobRect.anchorMax.y);
                knobRect.anchoredPosition = Vector2.zero;

                if (knobImage != null)
                {
                    knobImage.color = Color.Lerp(startColor, targetColor, smoothT);
                    knobImage.canvasRenderer.SetAlpha(1f);
                }

                yield return null;
            }

            ApplyInstantState(isOn);
            slideRoutine = null;
        }
    }
}
