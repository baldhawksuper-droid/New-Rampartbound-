using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using FantasyDefense.Audio;
using FantasyDefense.Mobile;

namespace FantasyDefense.UI
{
    /// <summary>
    /// Memberikan efek Micro-Bounce Juice pada tombol UI mobile:
    /// Animasi squash-and-stretch prosedural (scale 0.95x saat ditekan, membal 1.05x saat dilepas),
    /// disertai getaran haptic lembut (light tick) dan audio klik responsif.
    /// </summary>
    [DisallowMultipleComponent]
    public class UIButtonJuice : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [Header("Juice Parameters")]
        [SerializeField] private float pressedScale = 0.95f;
        [SerializeField] private float bounceScale = 1.05f;
        [SerializeField] private float animationDuration = 0.18f;
        [SerializeField] private bool triggerHaptic = true;
        [SerializeField] private bool triggerSound = true;

        private Vector3 originalScale = Vector3.one;
        private Coroutine activeRoutine;

        private void Awake()
        {
            originalScale = transform.localScale;
        }

        private void OnEnable()
        {
            transform.localScale = originalScale;
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
            activeRoutine = StartCoroutine(ScaleRoutine(transform.localScale, originalScale * pressedScale, 0.08f));

            if (triggerHaptic && DevicePerformanceManager.Instance != null)
            {
                DevicePerformanceManager.Instance.TriggerHaptic(HapticFeedbackType.Light);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (activeRoutine != null) StopCoroutine(activeRoutine);
            activeRoutine = StartCoroutine(BounceBackRoutine());
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (triggerSound && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.ButtonClick);
            }
        }

        private IEnumerator BounceBackRoutine()
        {
            // 1. Overshoot bounce ke 1.05x
            yield return ScaleRoutine(transform.localScale, originalScale * bounceScale, animationDuration * 0.45f);

            // 2. Kembali stabil ke 1.0x
            yield return ScaleRoutine(transform.localScale, originalScale, animationDuration * 0.55f);

            activeRoutine = null;
        }

        private IEnumerator ScaleRoutine(Vector3 fromScale, Vector3 toScale, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Ease-out cubic curve
                float ease = 1f - Mathf.Pow(1f - t, 3);
                transform.localScale = Vector3.LerpUnclamped(fromScale, toScale, ease);
                yield return null;
            }
            transform.localScale = toScale;
        }
    }
}
