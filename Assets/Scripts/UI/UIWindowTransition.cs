using System;
using System.Collections;
using UnityEngine;
using FantasyDefense.Mobile;

namespace FantasyDefense.UI
{
    /// <summary>
    /// Mengelola animasi transisi fluid saat membuka dan menutup sub-panel:
    /// Efek transparan membesar (Fade In + Scale 0.85x ke 1.0x) saat dibuka,
    /// serta Fade Out memudar saat ditutup, disertai haptic feedback.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UIWindowTransition : MonoBehaviour
    {
        [Header("Transition Settings")]
        [SerializeField] private float openDuration = 0.22f;
        [SerializeField] private float closeDuration = 0.16f;
        [SerializeField] private float initialScale = 0.85f;
        [SerializeField] private bool triggerHaptic = true;

        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;
        private Coroutine activeRoutine;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            rectTransform = GetComponent<RectTransform>();
        }

        /// <summary>
        /// Membuka jendela secara fluid dengan animasi Fade In dan Scale Up.
        /// </summary>
        public void OpenWindow(Action onComplete = null)
        {
            if (activeRoutine != null) StopCoroutine(activeRoutine);

            gameObject.SetActive(true);
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            rectTransform.localScale = Vector3.one * initialScale;

            if (triggerHaptic && DevicePerformanceManager.Instance != null)
            {
                DevicePerformanceManager.Instance.TriggerHaptic(HapticFeedbackType.Light);
            }

            activeRoutine = StartCoroutine(AnimateOpenRoutine(onComplete));
        }

        /// <summary>
        /// Menutup jendela secara fluid dengan animasi Fade Out sebelum me-nonaktifkan GameObject.
        /// </summary>
        public void CloseWindow(Action onComplete = null)
        {
            if (activeRoutine != null) StopCoroutine(activeRoutine);

            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            if (triggerHaptic && DevicePerformanceManager.Instance != null)
            {
                DevicePerformanceManager.Instance.TriggerHaptic(HapticFeedbackType.Light);
            }

            activeRoutine = StartCoroutine(AnimateCloseRoutine(onComplete));
        }

        private IEnumerator AnimateOpenRoutine(Action onComplete)
        {
            float elapsed = 0f;
            Vector3 startScale = Vector3.one * initialScale;
            Vector3 targetScale = Vector3.one;

            while (elapsed < openDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / openDuration);
                // Ease-out back overshoot curve
                float ease = 1f + 2.70158f * Mathf.Pow(t - 1f, 3f) + 1.70158f * Mathf.Pow(t - 1f, 2f);
                ease = Mathf.Clamp(ease, 0f, 1.05f);

                canvasGroup.alpha = Mathf.Lerp(0f, 1f, t * 1.2f);
                rectTransform.localScale = Vector3.LerpUnclamped(startScale, targetScale, ease);
                yield return null;
            }

            canvasGroup.alpha = 1f;
            rectTransform.localScale = Vector3.one;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            activeRoutine = null;
            onComplete?.Invoke();
        }

        private IEnumerator AnimateCloseRoutine(Action onComplete)
        {
            float elapsed = 0f;
            Vector3 startScale = rectTransform.localScale;
            Vector3 targetScale = Vector3.one * 0.90f;
            float startAlpha = canvasGroup.alpha;

            while (elapsed < closeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / closeDuration);

                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                rectTransform.localScale = Vector3.Lerp(startScale, targetScale, t);
                yield return null;
            }

            canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
            rectTransform.localScale = Vector3.one;

            activeRoutine = null;
            onComplete?.Invoke();
        }
    }
}
