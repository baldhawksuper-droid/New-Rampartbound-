using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using FantasyDefense.Audio;
using FantasyDefense.Core;
using FantasyDefense.Mobile;

namespace FantasyDefense.UI
{
    /// <summary>
    /// Master UI Controller untuk Layar Pembuka / Layar Judul (Title Screen).
    /// Menampilkan Logo Rampartbound: Merge Defense 3D, sub-judul, backdrop kastil megah,
    /// tombol 3D emas 'Tap to Start' timbul bebas frame kosong,
    /// serta kursor tangan interaktif (Hand Cursor) yang menepuk berirama saat idle
    /// dan dapat digeser / didrag mengikuti sentuhan jari/kursor pemain.
    /// </summary>
    public class UITitleScreenView : MonoBehaviour
    {
        public static UITitleScreenView Instance { get; private set; }

        [Header("Root Panel & Backdrop")]
        [SerializeField] private GameObject titleScreenPanel;
        [SerializeField] private Image titleBackgroundImage;

        [Header("Typography & Branding")]
        [SerializeField] private Image gameTitleLogoImage;
        [SerializeField] private Text gameTitleText;
        [SerializeField] private Text subtitleText;
        [SerializeField] private Text versionText;
        [SerializeField] private Text pulsingHintText;

        [Header("Start Action Button")]
        [SerializeField] private Button enterCitadelButton;
        [SerializeField] private Image enterCitadelButtonImage;
        [SerializeField] private Text enterCitadelButtonText;

        [Header("Title Screen Dual Heroes (Pippin & Cedric)")]
        [SerializeField] private Image pippinHeroImage;
        [SerializeField] private Image cedricHeroImage;
        [SerializeField] private RectTransform pippinHeroRt;
        [SerializeField] private RectTransform cedricHeroRt;
        [SerializeField] private RectTransform pippinShadowRt;
        [SerializeField] private RectTransform cedricShadowRt;
        [SerializeField] private Image pippinShadowImage;
        [SerializeField] private Image cedricShadowImage;

        [Header("Interactive Hand Cursor")]
        [SerializeField] private RectTransform handCursorRt;
        [SerializeField] private Image handCursorImage;
        [SerializeField] private Sprite handCursorSprite;

        [Header("Preload & Asset Prewarm System (Point 6)")]
        [SerializeField] private Slider loadingProgressBar;
        [SerializeField] private Text loadingStatusText;
        private bool isAssetsPreloaded = false;
        public bool IsAssetsPreloaded => isAssetsPreloaded;

        private float pulseTimer = 0f;
        private bool isHandBeingDragged = false;
        private Vector2 handHomeAnchoredPos;
        private Coroutine handTapRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (enterCitadelButton != null)
            {
                enterCitadelButton.onClick.AddListener(OnEnterCitadelClicked);
            }

            if (enterCitadelButtonText == null && enterCitadelButton != null)
            {
                enterCitadelButtonText = enterCitadelButton.GetComponentInChildren<Text>();
            }

            if (enterCitadelButtonImage == null && enterCitadelButton != null)
            {
                enterCitadelButtonImage = enterCitadelButton.GetComponent<Image>();
            }

            // Temukan bayangan kontak tanah secara otomatis jika belum diatur di inspector
            if (pippinShadowRt == null && pippinHeroRt != null && pippinHeroRt.parent != null)
            {
                var shadow = pippinHeroRt.parent.Find("Pippin_Ground_Shadow");
                if (shadow != null)
                {
                    pippinShadowRt = shadow.GetComponent<RectTransform>();
                    pippinShadowImage = shadow.GetComponent<Image>();
                }
            }
            if (pippinShadowImage != null && pippinShadowImage.sprite == null)
            {
                pippinShadowImage.sprite = Resources.Load<Sprite>("Sprites/UI/UI_Shadow_Oval");
            }

            if (cedricShadowRt == null && cedricHeroRt != null && cedricHeroRt.parent != null)
            {
                var shadow = cedricHeroRt.parent.Find("Cedric_Ground_Shadow");
                if (shadow != null)
                {
                    cedricShadowRt = shadow.GetComponent<RectTransform>();
                    cedricShadowImage = shadow.GetComponent<Image>();
                }
            }
            if (cedricShadowImage != null && cedricShadowImage.sprite == null)
            {
                cedricShadowImage.sprite = Resources.Load<Sprite>("Sprites/UI/UI_Shadow_Oval");
            }

            if (handCursorRt != null)
            {
                handHomeAnchoredPos = handCursorRt.anchoredPosition;
            }
        }

        private void Start()
        {
            RefreshLocalization();

            // Putar musik tema megah pembuka judul kerajaan (Title Screen Theme)
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayBGM(BgmTrack.TitleTheme);
            }

            // Jalankan pre-warming seluruh aset sejak awal game terbuka (Point 6)
            StartCoroutine(PreloadAllGameAssetsRoutine());

            if (handCursorRt != null)
            {
                handHomeAnchoredPos = handCursorRt.anchoredPosition;
                StartHandTapping();
            }
        }

        private IEnumerator PreloadAllGameAssetsRoutine()
        {
            if (isAssetsPreloaded) yield break;

            if (enterCitadelButton != null)
            {
                enterCitadelButton.interactable = false;
            }

            if (loadingStatusText != null)
            {
                loadingStatusText.gameObject.SetActive(true);
                loadingStatusText.text = "🛡️ Menyiapkan Aset Benteng...";
            }

            // 1. Preload Hero Pool & Gems T0
            if (FantasyDefense.Summon.HeroSummoner.Instance != null)
            {
                FantasyDefense.Summon.HeroSummoner.Instance.EnsureHeroPoolLoaded();
            }
            if (loadingProgressBar != null) loadingProgressBar.value = 0.25f;
            yield return null;

            // 2. Preload Monster Sprites
            Resources.Load<Sprite>("Sprites/Monsters/Monster_Creep");
            Resources.Load<Sprite>("Sprites/Monsters/Monster_Brute");
            Resources.Load<Sprite>("Sprites/Monsters/Monster_Boss");
            if (loadingProgressBar != null) loadingProgressBar.value = 0.50f;
            yield return null;

            // 3. Preload Tile Sprites
            Resources.Load<Sprite>("Sprites/Grid/Tile_Empty");
            Resources.Load<Sprite>("Sprites/Grid/Tile_Hover");
            if (loadingProgressBar != null) loadingProgressBar.value = 0.75f;
            yield return null;

            // 4. Preload Local Save Data
            if (FantasyDefense.Save.SaveManager.Instance != null)
            {
                FantasyDefense.Save.SaveManager.Instance.LoadProfile((loadedData) =>
                {
                    Debug.Log($"[TitleScreen] Profil akun berhasil dipre-warm: {(loadedData != null ? loadedData.playerDisplayName : "Akun Baru")}.");
                });
            }
            if (loadingProgressBar != null) loadingProgressBar.value = 1.0f;
            yield return new WaitForSeconds(0.2f);

            isAssetsPreloaded = true;
            if (loadingStatusText != null)
            {
                loadingStatusText.text = "✨ Aset Siap!";
                yield return new WaitForSeconds(0.15f);
                loadingStatusText.gameObject.SetActive(false);
            }

            if (loadingProgressBar != null)
            {
                loadingProgressBar.gameObject.SetActive(false);
            }

            if (enterCitadelButton != null)
            {
                enterCitadelButton.interactable = true;
            }

            RefreshLocalization();
            Debug.Log("[UITitleScreenView] Seluruh aset game berhasil di-prewarm & dimuat. Siap dimainkan tanpa jeda / race condition!");
        }

        private void OnEnable()
        {
            FantasyDefense.Localization.CitadelLocalizationManager.OnLanguageChanged += HandleLanguageChanged;
            RefreshLocalization();
            StartHandTapping();
        }

        private void OnDisable()
        {
            FantasyDefense.Localization.CitadelLocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
            if (handTapRoutine != null)
            {
                StopCoroutine(handTapRoutine);
                handTapRoutine = null;
            }
        }

        private void HandleLanguageChanged(FantasyDefense.Localization.AppLanguage lang)
        {
            RefreshLocalization();
        }

        public void RefreshLocalization()
        {
            bool hasLogo = (gameTitleLogoImage != null && gameTitleLogoImage.sprite != null) 
                        || Resources.Load<Sprite>("Sprites/UI/Title_Game_Logo_Rampartbound") != null;

            if (hasLogo)
            {
                // Jika sprite logo resmi terpasang, sembunyikan teks mentah agar tampilan mewah tanpa artefak garis
                if (gameTitleText != null) 
                {
                    gameTitleText.text = "";
                    gameTitleText.gameObject.SetActive(false);
                }
                if (subtitleText != null) 
                {
                    subtitleText.text = "";
                    subtitleText.gameObject.SetActive(false);
                }
            }
            else
            {
                if (gameTitleText != null)
                {
                    gameTitleText.gameObject.SetActive(true);
                    gameTitleText.text = "RAMPARTBOUND\nMERGE DEFENSE";
                }
                if (subtitleText != null)
                {
                    subtitleText.gameObject.SetActive(true);
                    subtitleText.text = FantasyDefense.Localization.CitadelLocalizationManager.Get(
                        "TITLE_SUBTITLE",
                        "Pertahankan Gerbang Kerajaan dari Serbuan Abyssal",
                        "Defend the Royal Gates from the Abyssal Incursion"
                    );
                }
            }

            if (enterCitadelButtonText != null)
            {
                enterCitadelButtonText.text = FantasyDefense.Localization.CitadelLocalizationManager.Get(
                    "TITLE_ENTER_CITADEL",
                    "🏰 TAP TO START",
                    "🏰 TAP TO START"
                );
            }
            if (pulsingHintText != null)
            {
                pulsingHintText.text = FantasyDefense.Localization.CitadelLocalizationManager.Get(
                    "TITLE_HINT_TAP",
                    "Ketuk Layar untuk Memulai Pertahanan",
                    "Tap Screen to Start Defense"
                );
            }
            if (versionText != null)
            {
                versionText.text = "Versi 1.2.11 (Google Play Edition)";
                versionText.fontStyle = FontStyle.Bold;
                versionText.color = new Color(0.95f, 0.97f, 1.0f, 0.95f);
            }
        }

        private void Update()
        {
            pulseTimer += Time.unscaledDeltaTime * 3f;

            // 1. Text hint pulse
            if (pulsingHintText != null && pulsingHintText.gameObject.activeInHierarchy)
            {
                float alpha = 0.5f + (Mathf.Sin(pulseTimer) * 0.5f);
                Color c = pulsingHintText.color;
                c.a = alpha;
                pulsingHintText.color = c;
            }

            // 2. Denyut lembut & pendaran emas pada tombol masuk Citadel
            if (enterCitadelButton != null)
            {
                float btnPulse = 1f + (Mathf.Sin(pulseTimer * 0.8f) * 0.025f);
                enterCitadelButton.transform.localScale = new Vector3(btnPulse, btnPulse, 1f);
            }

            if (enterCitadelButtonImage != null)
            {
                float glowAlpha = 0.90f + (Mathf.Sin(pulseTimer * 0.8f) * 0.10f);
                enterCitadelButtonImage.color = new Color(1f, 1f, 1f, glowAlpha);
            }

            // 3. Interactive Hand Cursor Draggable Logic
            UpdateHandCursorInteraction();

            // 4. Dual Heroes Gentle Breathing Idle Animation on Title Screen
            UpdateDualHeroesIdleAnimation();
        }

        private void UpdateDualHeroesIdleAnimation()
        {
            float idleTime = Time.unscaledTime;
            if (pippinHeroRt != null)
            {
                // Pippin gentle floating breathing
                float pippinOffsetY = Mathf.Sin(idleTime * 2.2f) * 6f;
                float pippinScaleY = 1f + (Mathf.Sin(idleTime * 2.2f) * 0.02f);
                pippinHeroRt.anchoredPosition = new Vector2(pippinHeroRt.anchoredPosition.x, -20f + pippinOffsetY);
                pippinHeroRt.localScale = new Vector3(1f, pippinScaleY, 1f);

                if (pippinShadowRt != null)
                {
                    float shadowScale = 1f - (pippinOffsetY * 0.012f);
                    pippinShadowRt.localScale = new Vector3(shadowScale, shadowScale, 1f);
                    if (pippinShadowImage != null)
                    {
                        float shadowAlpha = 0.55f - (pippinOffsetY * 0.015f);
                        pippinShadowImage.color = new Color(0f, 0f, 0f, Mathf.Clamp(shadowAlpha, 0.42f, 0.65f));
                    }
                }
            }

            if (cedricHeroRt != null)
            {
                // Cedric grounded rhythmic breathing with slight phase delay (menghadap ke kiri ke arah Pippin: X = -1)
                float cedricOffsetY = Mathf.Sin((idleTime * 2.0f) + 0.8f) * 5f;
                float cedricScaleY = 1f + (Mathf.Sin((idleTime * 2.0f) + 0.8f) * 0.02f);
                cedricHeroRt.anchoredPosition = new Vector2(cedricHeroRt.anchoredPosition.x, -20f + cedricOffsetY);
                cedricHeroRt.localScale = new Vector3(-1f, cedricScaleY, 1f); // Tetap mirrored horizontal!

                if (cedricShadowRt != null)
                {
                    float shadowScale = 1f - (cedricOffsetY * 0.012f);
                    cedricShadowRt.localScale = new Vector3(shadowScale, shadowScale, 1f);
                    if (cedricShadowImage != null)
                    {
                        float shadowAlpha = 0.55f - (cedricOffsetY * 0.015f);
                        cedricShadowImage.color = new Color(0f, 0f, 0f, Mathf.Clamp(shadowAlpha, 0.42f, 0.65f));
                    }
                }
            }
        }

        private void UpdateHandCursorInteraction()
        {
            if (handCursorRt == null || !handCursorRt.gameObject.activeInHierarchy) return;

            bool isTouching = Input.GetMouseButton(0) || (Input.touchCount > 0);

            if (isTouching)
            {
                isHandBeingDragged = true;
                Vector3 pointerPos = Input.touchCount > 0 ? (Vector3)Input.GetTouch(0).position : Input.mousePosition;

                // Konversi pointer screen ke local position canvas
                Canvas canvas = GetComponentInParent<Canvas>();
                if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    handCursorRt.position = Vector3.Lerp(handCursorRt.position, pointerPos, Time.unscaledDeltaTime * 25f);
                }
                else
                {
                    handCursorRt.position = Vector3.Lerp(handCursorRt.position, pointerPos, Time.unscaledDeltaTime * 25f);
                }

                handCursorRt.SetAsLastSibling();
            }
            else
            {
                if (isHandBeingDragged)
                {
                    isHandBeingDragged = false;
                }

                // Smoothly return to home anchored position if not dragged
                if (Vector2.Distance(handCursorRt.anchoredPosition, handHomeAnchoredPos) > 1.5f && handTapRoutine == null)
                {
                    handCursorRt.anchoredPosition = Vector2.Lerp(handCursorRt.anchoredPosition, handHomeAnchoredPos, Time.unscaledDeltaTime * 10f);
                }
            }
        }

        private void StartHandTapping()
        {
            if (handTapRoutine != null) StopCoroutine(handTapRoutine);
            if (handCursorRt != null)
            {
                handTapRoutine = StartCoroutine(AnimateHandTappingRoutine());
            }
        }

        private IEnumerator AnimateHandTappingRoutine()
        {
            float timer = 0f;
            Vector3 baseScale = Vector3.one;

            while (true)
            {
                if (!isHandBeingDragged && handCursorRt != null)
                {
                    timer += Time.unscaledDeltaTime * 3.5f;
                    float cycle = Mathf.Repeat(timer, 1f);

                    float tapOffset = 0f;
                    float scaleMod = 1f;

                    if (cycle < 0.35f)
                    {
                        // Pressing down
                        float sub = cycle / 0.35f;
                        tapOffset = Mathf.Sin(sub * Mathf.PI) * -16f;
                        scaleMod = 1f - (Mathf.Sin(sub * Mathf.PI) * 0.12f);
                    }

                    Vector2 targetPos = handHomeAnchoredPos + new Vector2(tapOffset * 0.35f, tapOffset);
                    handCursorRt.anchoredPosition = Vector2.Lerp(handCursorRt.anchoredPosition, targetPos, Time.unscaledDeltaTime * 14f);
                    handCursorRt.localScale = baseScale * scaleMod;
                }
                yield return null;
            }
        }

        public void OpenView()
        {
            if (titleScreenPanel != null)
            {
                titleScreenPanel.SetActive(true);
            }
            else
            {
                gameObject.SetActive(true);
            }

            StartHandTapping();
        }

        public void CloseView()
        {
            if (titleScreenPanel != null)
            {
                titleScreenPanel.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void OnEnterCitadelClicked()
        {
            if (!isAssetsPreloaded) return;

            if (AudioManager.Instance != null)
            {
                try
                {
                    AudioManager.Instance.PlaySound(SoundType.ButtonClick);
                }
                catch (Exception) { }
            }

            if (DevicePerformanceManager.Instance != null)
            {
                try
                {
                    DevicePerformanceManager.Instance.TriggerHaptic(HapticFeedbackType.Medium);
                }
                catch (Exception) { }
            }

            // Micro-bounce button juice on start button
            if (enterCitadelButton != null)
            {
                StartCoroutine(ButtonSquashRoutine(enterCitadelButton.transform));
            }
            else
            {
                TransitionToLobby();
            }
        }

        private IEnumerator ButtonSquashRoutine(Transform btnTransform)
        {
            Vector3 origScale = Vector3.one;
            float dur = 0.15f;
            float elapsed = 0f;

            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / dur;
                float s = 1f - Mathf.Sin(t * Mathf.PI) * 0.08f;
                btnTransform.localScale = origScale * s;
                yield return null;
            }

            btnTransform.localScale = origScale;
            TransitionToLobby();
        }

        private void TransitionToLobby()
        {
            Debug.Log("[UITitleScreenView] Masuk ke Pusat Benteng Citadel...");

            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.ShowLobby();
            }
            else
            {
                CloseView();
            }
        }
    }
}
