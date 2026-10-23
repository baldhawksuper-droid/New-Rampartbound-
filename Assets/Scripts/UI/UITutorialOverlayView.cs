using System;
using UnityEngine;
using UnityEngine.UI;
using FantasyDefense.Audio;
using FantasyDefense.Metagame;

namespace FantasyDefense.UI
{
    /// <summary>
    /// View Controller untuk UI Layar Panduan Onboarding FTUE (Tutorial Overlay).
    /// Mengontrol masking spotlight fokus, kotak dialog BaldHawk / Mentor, animasi kursor penunjuk,
    /// serta tombol tap-to-continue dan lewati tutorial.
    /// </summary>
    public class UITutorialOverlayView : MonoBehaviour
    {
        [Header("Root & Panels")]
        [SerializeField] private GameObject tutorialRootPanel;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Dialogue Box")]
        [SerializeField] private Text speakerNameText;
        [SerializeField] private Text dialogueBodyText;
        [SerializeField] private Button tapToContinueButton;
        [SerializeField] private Text tapHintText;

        [Header("Spotlight & Pointer")]
        [SerializeField] private RectTransform spotlightCutout;
        [SerializeField] private RectTransform pointerHand;
        [SerializeField] private bool animatePointer = true;

        [Header("Skip Option")]
        [SerializeField] private Button skipTutorialButton;

        private TutorialStepData currentStepData;

        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (tapToContinueButton != null)
                tapToContinueButton.onClick.AddListener(OnTapContinue);

            if (skipTutorialButton != null)
                skipTutorialButton.onClick.AddListener(OnSkipClicked);

            // Pastikan overlay tutorial benar-benar tertutup dan tidak memblokir raycast saat inisialisasi awal
            ShowOverlay(false);
        }

        private void OnEnable()
        {
            TutorialGuideManager.OnTutorialStepStarted += HandleStepStarted;
            TutorialGuideManager.OnTutorialCompleted += HandleTutorialCompleted;
        }

        private void OnDisable()
        {
            TutorialGuideManager.OnTutorialStepStarted -= HandleStepStarted;
            TutorialGuideManager.OnTutorialCompleted -= HandleTutorialCompleted;
        }

        private void Update()
        {
            // Animasi melayang halus (bobbing) untuk ikon tangan penunjuk
            if (animatePointer && pointerHand != null && pointerHand.gameObject.activeSelf)
            {
                float bobbing = Mathf.Sin(Time.unscaledTime * 5f) * 12f;
                pointerHand.anchoredPosition = new Vector2(pointerHand.anchoredPosition.x, pointerHand.anchoredPosition.y + (bobbing * Time.unscaledDeltaTime));
            }
        }

        private void HandleStepStarted(TutorialStepData stepData)
        {
            currentStepData = stepData;
            ShowOverlay(true);

            if (speakerNameText != null) speakerNameText.text = stepData.speakerName;
            if (dialogueBodyText != null) dialogueBodyText.text = stepData.dialogueText;

            // Jika step butuh aksi klik tombol spesifik, hilangkan hint tap-anywhere
            if (tapHintText != null)
            {
                tapHintText.gameObject.SetActive(!stepData.requireSpecificAction);
            }

            // Posisikan pointer jika ada target UI
            PositionSpotlightAndPointer(stepData.targetUiElementName);

            AudioManager.Instance?.PlaySfx(AudioSfxType.ButtonClick);
        }

        private void PositionSpotlightAndPointer(string targetElementName)
        {
            if (string.IsNullOrEmpty(targetElementName))
            {
                if (spotlightCutout != null) spotlightCutout.gameObject.SetActive(false);
                if (pointerHand != null) pointerHand.gameObject.SetActive(false);
                return;
            }

            // Cari target GameObject di hierarki aktif
            GameObject targetObj = GameObject.Find(targetElementName);
            if (targetObj != null)
            {
                RectTransform targetRect = targetObj.GetComponent<RectTransform>();
                if (targetRect != null)
                {
                    if (spotlightCutout != null)
                    {
                        spotlightCutout.gameObject.SetActive(true);
                        spotlightCutout.position = targetRect.position;
                        spotlightCutout.sizeDelta = targetRect.sizeDelta + new Vector2(20f, 20f);
                    }

                    if (pointerHand != null)
                    {
                        pointerHand.gameObject.SetActive(true);
                        pointerHand.position = targetRect.position + new Vector3(0, 50f, 0);
                    }
                    return;
                }
            }

            if (spotlightCutout != null) spotlightCutout.gameObject.SetActive(false);
            if (pointerHand != null) pointerHand.gameObject.SetActive(false);
        }

        private void OnTapContinue()
        {
            if (currentStepData == null) return;
            TutorialGuideManager.Instance?.AdvanceStep();
        }

        private void OnSkipClicked()
        {
            TutorialGuideManager.Instance?.CompleteTutorial();
        }

        private void HandleTutorialCompleted()
        {
            ShowOverlay(false);
        }

        private void ShowOverlay(bool show)
        {
            if (tutorialRootPanel != null) tutorialRootPanel.SetActive(show);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = show ? 1f : 0f;
                canvasGroup.interactable = show;
                canvasGroup.blocksRaycasts = show;
            }

            if (tapToContinueButton != null)
            {
                tapToContinueButton.gameObject.SetActive(show);
            }
        }
    }
}
