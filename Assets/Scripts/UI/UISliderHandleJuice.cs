using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using FantasyDefense.Mobile;

namespace FantasyDefense.UI
{
    /// <summary>
    /// Memberikan feedback tactile dan pembesaran dinamis pada Handle Slider UI:
    /// Saat pemain menekan/menggeser knob permata, knob membesar 1.15x agar terlihat jelas
    /// di bawah jari pengguna, disertai haptic tick halus secara berkala saat nilai berubah.
    /// </summary>
    [DisallowMultipleComponent]
    public class UISliderHandleJuice : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private float pressedScale = 1.15f;
        [SerializeField] private float animDuration = 0.12f;

        private Vector3 originalScale = Vector3.one;
        private Coroutine activeRoutine;
        private Slider parentSlider;
        private float lastHapticValue = -1f;

        private void Awake()
        {
            originalScale = transform.localScale;
            parentSlider = GetComponentInParent<Slider>();
            if (parentSlider != null)
            {
                parentSlider.onValueChanged.AddListener(OnSliderValueChanged);
            }
        }

        private void OnDestroy()
        {
            if (parentSlider != null)
            {
                parentSlider.onValueChanged.RemoveListener(OnSliderValueChanged);
            }
        }

        private void OnEnable()
        {
            transform.localScale = originalScale;
            if (parentSlider != null) lastHapticValue = parentSlider.value;
        }

        private void OnDisable()
        {
            if (activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
                activeRoutine = null;
            }
            transform.localScale = originalScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (activeRoutine != null) StopCoroutine(activeRoutine);
            activeRoutine = StartCoroutine(ScaleRoutine(transform.localScale, originalScale * pressedScale, animDuration));

            if (DevicePerformanceManager.Instance != null)
            {
                DevicePerformanceManager.Instance.TriggerHaptic(HapticFeedbackType.Selection);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (activeRoutine != null) StopCoroutine(activeRoutine);
            activeRoutine = StartCoroutine(ScaleRoutine(transform.localScale, originalScale, animDuration));
        }

        private void OnSliderValueChanged(float val)
        {
            // Micro-haptic tick setiap perubahan nilai melompat 5%
            if (Mathf.Abs(val - lastHapticValue) >= 0.05f)
            {
                lastHapticValue = val;
                if (DevicePerformanceManager.Instance != null)
                {
                    DevicePerformanceManager.Instance.TriggerHaptic(HapticFeedbackType.Selection);
                }
            }
        }

        private IEnumerator ScaleRoutine(Vector3 from, Vector3 to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                transform.localScale = Vector3.LerpUnclamped(from, to, smoothT);
                yield return null;
            }
            transform.localScale = to;
            activeRoutine = null;
        }
    }
}
