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
        [SerializeField] private Text skipButtonText;

        private TutorialStepData currentStepData;

        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            // Memastikan Canvas Tutorial selalu memiliki sorting order terdepan (950) di atas seluruh panel gameplay
            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
            }
            if (canvas != null)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = 950;
            }
            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            EnsureTutorialVisualsBuilt();

            if (tapToContinueButton != null)
                tapToContinueButton.onClick.AddListener(OnTapContinue);

            if (skipTutorialButton != null)
                skipTutorialButton.onClick.AddListener(OnSkipClicked);

            // Pastikan overlay tutorial benar-benar tertutup dan tidak memblokir raycast saat inisialisasi awal
            ShowOverlay(false);
        }

        private void EnsureTutorialVisualsBuilt()
        {
            var lilitaFont = Resources.Load<Font>("Fonts/LilitaOne-Regular");
            var frame9Slice = Resources.Load<Sprite>("Sprites/UI/Frame_StoneBastion_9Slice")
#if UNITY_EDITOR
                          ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Frame_StoneBastion_9Slice.png")
#endif
                          ;
            var btnSecondary = Resources.Load<Sprite>("Sprites/UI/Btn_Secondary_Stone")
#if UNITY_EDITOR
                            ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Btn_Secondary_Stone.png")
#endif
                            ;
            var pointerSprite = Resources.Load<Sprite>("Sprites/UI/UI_Pointer_Hand")
#if UNITY_EDITOR
                          ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/UI_Pointer_Hand.png")
#endif
                          ;
            var mentorAvatarSprite = Resources.Load<Sprite>("Sprites/UI/UI_Mentor_BaldHawk_Avatar")
#if UNITY_EDITOR
                          ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/UI_Mentor_BaldHawk_Avatar.png")
#endif
                          ;

            // Pastikan Pointer Hand memiliki Sprite grafis tangan penunjuk arcade yang jelas
            if (pointerHand != null)
            {
                var ptrImg = pointerHand.GetComponent<Image>();
                if (ptrImg == null) ptrImg = pointerHand.gameObject.AddComponent<Image>();
                if (pointerSprite != null)
                {
                    ptrImg.sprite = pointerSprite;
                    ptrImg.color = Color.white;
                    ptrImg.preserveAspect = true;
                }
                pointerHand.sizeDelta = new Vector2(64f, 64f);
            }

            // Pastikan tombol Skip Tutorial selalu ada, aktif, dan memiliki styling jelas berbingkai batu fantasi
            if (skipTutorialButton == null)
            {
                var skipObj = new GameObject("DynamicSkipButton", typeof(RectTransform), typeof(Image), typeof(Button));
                skipObj.transform.SetParent(transform, false);

                var rt = skipObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(1f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 1f);
                rt.anchoredPosition = new Vector2(-20f, -25f);
                rt.sizeDelta = new Vector2(140f, 44f);

                var img = skipObj.GetComponent<Image>();
                img.sprite = btnSecondary != null ? btnSecondary : frame9Slice;
                img.type = Image.Type.Sliced;
                img.color = Color.white;

                skipTutorialButton = skipObj.GetComponent<Button>();
                skipTutorialButton.onClick.AddListener(OnSkipClicked);

                var txtObj = new GameObject("SkipText", typeof(RectTransform), typeof(Text));
                txtObj.transform.SetParent(skipObj.transform, false);
                var textRt = txtObj.GetComponent<RectTransform>();
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.offsetMin = Vector2.zero;
                textRt.offsetMax = Vector2.zero;

                skipButtonText = txtObj.GetComponent<Text>();
                skipButtonText.font = lilitaFont ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                skipButtonText.fontSize = 14;
                skipButtonText.fontStyle = FontStyle.Bold;
                skipButtonText.alignment = TextAnchor.MiddleCenter;
                skipButtonText.color = new Color(1f, 0.92f, 0.70f, 1f);
                skipButtonText.text = FantasyDefense.Localization.CitadelLocalizationManager.Text("LEWATI (SKIP) ✕", "SKIP ✕");
                skipButtonText.raycastTarget = false;
            }
            else if (skipButtonText == null)
            {
                skipButtonText = skipTutorialButton.GetComponentInChildren<Text>();
            }

            // Pastikan Portrait Avatar Mentor BaldHawk terpasang di samping dialog
            var avatarTr = transform.Find("MentorAvatar");
            if (avatarTr == null && mentorAvatarSprite != null)
            {
                var avObj = new GameObject("MentorAvatar", typeof(RectTransform), typeof(Image));
                avObj.transform.SetParent(transform, false);
                var avRt = avObj.GetComponent<RectTransform>();
                avRt.anchorMin = new Vector2(0.04f, 0.28f);
                avRt.anchorMax = new Vector2(0.04f, 0.28f);
                avRt.pivot = new Vector2(0f, 0.5f);
                avRt.sizeDelta = new Vector2(72f, 72f);

                var avImg = avObj.GetComponent<Image>();
                avImg.sprite = mentorAvatarSprite;
                avImg.preserveAspect = true;
                avImg.raycastTarget = false;
            }
        }

        private void OnEnable()
        {
            TutorialGuideManager.OnTutorialStepStarted += HandleStepStarted;
            TutorialGuideManager.OnTutorialCompleted += HandleTutorialCompleted;
            FantasyDefense.Localization.CitadelLocalizationManager.OnLanguageChanged += HandleLanguageChanged;
            RefreshLocalizedTexts();
        }

        private void OnDisable()
        {
            TutorialGuideManager.OnTutorialStepStarted -= HandleStepStarted;
            TutorialGuideManager.OnTutorialCompleted -= HandleTutorialCompleted;
            FantasyDefense.Localization.CitadelLocalizationManager.OnLanguageChanged -= HandleLanguageChanged;

            // Pastikan raycast blocker dibersihkan saat objek dinonaktifkan / berganti scene
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }
        }

        private void HandleLanguageChanged(FantasyDefense.Localization.AppLanguage newLang)
        {
            RefreshLocalizedTexts();
        }

        private void RefreshLocalizedTexts()
        {
            if (currentStepData != null)
            {
                if (speakerNameText != null) speakerNameText.text = currentStepData.GetLocalizedSpeaker();
                if (dialogueBodyText != null) dialogueBodyText.text = currentStepData.GetLocalizedDialogue();
            }

            if (skipButtonText != null)
            {
                skipButtonText.text = FantasyDefense.Localization.CitadelLocalizationManager.Text("LEWATI (SKIP) ✕", "SKIP ✕");
            }

            if (tapHintText != null)
            {
                tapHintText.text = FantasyDefense.Localization.CitadelLocalizationManager.Text("Ketuk di mana saja untuk lanjut...", "Tap anywhere to continue...");
            }
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

            if (speakerNameText != null) speakerNameText.text = stepData.GetLocalizedSpeaker();
            if (dialogueBodyText != null) dialogueBodyText.text = stepData.GetLocalizedDialogue();

            // Posisikan pointer jika ada target UI
            bool targetFound = PositionSpotlightAndPointer(stepData.targetUiElementName);

            // Jika step butuh aksi klik tombol spesifik tapi objek tidak ditemukan di hierarki aktif,
            // aktifkan tombol tap-to-continue sebagai fail-safe agar pemain tidak mengalami softlock.
            bool requireAction = stepData.requireSpecificAction && targetFound;

            if (tapHintText != null)
            {
                tapHintText.gameObject.SetActive(!requireAction);
                tapHintText.text = FantasyDefense.Localization.CitadelLocalizationManager.Text("Ketuk di mana saja untuk lanjut...", "Tap anywhere to continue...");
            }

            if (tapToContinueButton != null)
            {
                tapToContinueButton.gameObject.SetActive(!requireAction);
            }

            if (skipButtonText != null)
            {
                skipButtonText.text = FantasyDefense.Localization.CitadelLocalizationManager.Text("LEWATI (SKIP) ✕", "SKIP ✕");
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.DialogueBlipChime, 1.15f);
            }
            else
            {
                AudioManager.Instance?.PlaySfx(AudioSfxType.ButtonClick);
            }
        }

        private bool PositionSpotlightAndPointer(string targetElementName)
        {
            if (string.IsNullOrEmpty(targetElementName))
            {
                if (spotlightCutout != null) spotlightCutout.gameObject.SetActive(false);
                if (pointerHand != null) pointerHand.gameObject.SetActive(false);
                return false;
            }

            // Cari target GameObject di hierarki aktif
            try
            {
                GameObject targetObj = GameObject.Find(targetElementName);
                if (targetObj == null)
                {
                    // Fallback alias lookup jika penamaan hierarchy di editor berbeda
                    string[] aliases = targetElementName switch
                    {
                        "NavHeroes" => new[] { "LobbyHeroMenuBtn", "Nav_Heroes", "HeroesButton" },
                        "LobbyHeroMenuBtn" => new[] { "NavHeroes", "Nav_Heroes", "HeroesButton" },
                        "UpgradeHeroButton" => new[] { "HeroUpgradeBtn", "UpgradeButton", "Btn_Upgrade" },
                        "HeroUpgradeBtn" => new[] { "UpgradeHeroButton", "UpgradeButton", "Btn_Upgrade" },
                        "DefendCitadelButton" => new[] { "StartBattleButton", "BattleButton" },
                        _ => System.Array.Empty<string>()
                    };

                    foreach (var alias in aliases)
                    {
                        targetObj = GameObject.Find(alias);
                        if (targetObj != null) break;
                    }
                }

                if (targetObj != null && targetObj.activeInHierarchy)
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
                        return true;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[UITutorialOverlayView] Gagal memperbarui posisi spotlight: {ex.Message}");
            }

            if (spotlightCutout != null) spotlightCutout.gameObject.SetActive(false);
            if (pointerHand != null) pointerHand.gameObject.SetActive(false);
            return false;
        }

        private void OnTapContinue()
        {
            if (currentStepData == null) return;
            // Cegah tap-continue HANYA jika aksi spesifik dibutuhkan DAN elemen target memang aktif di layar
            if (currentStepData.requireSpecificAction)
            {
                GameObject targetObj = !string.IsNullOrEmpty(currentStepData.targetUiElementName) ? GameObject.Find(currentStepData.targetUiElementName) : null;
                if (targetObj != null && targetObj.activeInHierarchy) return;
            }
            TutorialGuideManager.Instance?.AdvanceStep();
        }

        private void OnSkipClicked()
        {
            if (FantasyDefense.Mobile.DevicePerformanceManager.Instance != null && FantasyDefense.Mobile.DevicePerformanceManager.Instance.IsHapticsEnabled)
            {
                FantasyDefense.Mobile.DevicePerformanceManager.Instance.TriggerHaptic(FantasyDefense.Mobile.HapticFeedbackType.Selection);
            }

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
