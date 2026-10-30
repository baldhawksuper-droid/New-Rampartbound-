using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using FantasyDefense.Audio;
using FantasyDefense.Combat;
using FantasyDefense.Core;
using FantasyDefense.Data;
using FantasyDefense.Economy;
using FantasyDefense.Metagame;
using FantasyDefense.Wave;
using FantasyDefense.Localization;

namespace FantasyDefense.UI
{
    /// <summary>
    /// Master Controller untuk Antarmuka Tempur (Battle HUD) Fantasy Citadel Defense.
    /// Mengelola Bar HP Benteng, Indikator Gelombang Monster, Saldo Gems, Kontrol Kecepatan Tempur (1x/2x),
    /// Menu Pause, serta Dialog Kemenangan (Victory) dan Kekalahan (Defeat).
    /// Dibangun dengan arsitektur null-safe agar berfungsi mulus baik dengan UI Canvas maupun inisialisasi runtime.
    /// </summary>
    public class UIBattleView : MonoBehaviour
    {
        public static UIBattleView Instance { get; private set; }

        [Header("Castle Barricade HP")]
        [SerializeField] private Slider castleHpSlider;
        [SerializeField] private Text castleHpText;

        [Header("Wave Progress")]
        [SerializeField] private Text waveText;
        [SerializeField] private Text monsterCountText;
        [SerializeField] private GameObject waveProgressBarContainer;
        [SerializeField] private Slider waveProgressBar;
        [SerializeField] private Image waveProgressFill;
        [SerializeField] private Image midBossMarker;
        [SerializeField] private Image finalBossMarker;
        [SerializeField] private Text waveProgressText;

        [Header("Gems Economy")]
        [SerializeField] private Text gemsText;

        [Header("Combat Speed & Controls")]
        [SerializeField] private Button speedToggleButton;
        [SerializeField] private Text speedToggleText;
        [SerializeField] private Button pauseButton;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button pauseVibrationButton;
        [SerializeField] private Text pauseVibrationText;

        [Header("Dynamic Battle Mode HUD (Sprint 8)")]
        [SerializeField] private GameObject modeBadgeContainer;
        [SerializeField] private Text modeBadgeText;
        [SerializeField] private GameObject puzzlePreparationPanel;
        [SerializeField] private Text movesRemainingText;
        [SerializeField] private Button startDefenseNowButton;

        [SerializeField] private Text upcomingThreatText;
        [SerializeField] private GameObject summonButtonContainer;

        [Header("Inter-Wave Tactical Phase HUD (Sprint 28)")]
        [SerializeField] private GameObject interWaveBannerPanel;
        [SerializeField] private Text interWaveTitleText;
        [SerializeField] private Text interWaveSubText;

        [Header("Boss Fight HUD (Sprint 8)")]
        [SerializeField] private GameObject bossBarContainer;
        [SerializeField] private Slider bossHpSlider;
        [SerializeField] private Text bossNameText;
        [SerializeField] private Text bossTitleText;
        [SerializeField] private Text bossHpText;
        [SerializeField] private GameObject bossEnrageBanner;

        [Header("Battlefield Weather Affixes HUD (Sprint 12)")]
        [SerializeField] private GameObject weatherBadgeContainer;
        [SerializeField] private Text weatherBadgeText;
        [SerializeField] private Text weatherDescText;

        [Header("Auto-Merge Assistant HUD (Sprint 14)")]
        [SerializeField] private Button autoMergeToggleButton;
        [SerializeField] private Text autoMergeStatusText;

        [Header("Victory Modal")]
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private Text victoryStarsText;
        [SerializeField] private Text victoryRewardsText;
        [SerializeField] private Button victoryReturnButton;

        [Header("Defeat Modal")]
        [SerializeField] private GameObject defeatPanel;
        [SerializeField] private Button defeatRetryButton;
        [SerializeField] private Button defeatReturnButton;

        private int currentSpeedMode = 1; // 1 = 1x, 2 = 2x, 3 = 3x (Sprint 14)
        private bool isPaused = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeButtons();
        }

        private void OnEnable()
        {
            CastleBarricade.OnCastleHealthChanged += HandleCastleHealthChanged;
            WaveSpawner.OnWaveStarted += HandleWaveStarted;
            WaveSpawner.OnWaveCompleted += HandleWaveCompleted;
            EconomyManager.OnGemsChanged += HandleGemsChanged;
            GameManager.OnBattleStateChanged += HandleBattleStateChanged;
            GameManager.OnBattleModeSelected += HandleBattleModeSelected;
            GameManager.OnMovesRemainingChanged += HandleMovesRemainingChanged;
            MonsterController.OnMonsterKilled += HandleMonsterCountUpdate;
            MonsterController.OnMonsterSpawned += HandleMonsterCountUpdate;

            BossMonsterController.OnBossSpawned += HandleBossSpawned;
            BossMonsterController.OnBossHealthChanged += HandleBossHealthChanged;
            BossMonsterController.OnBossEnraged += HandleBossEnraged;
            BossMonsterController.OnBossDefeated += HandleBossDefeated;

            BattlefieldWeatherManager.OnWeatherChanged += HandleWeatherChanged;
            AutoMergeAssistant.OnAutoMergeToggled += HandleAutoMergeToggled;
            CitadelLocalizationManager.OnLanguageChanged += HandleLanguageChanged;
            GameManager.OnInterWavePreparationStarted += HandleInterWavePreparationStarted;
            WaveSpawner.OnWaveMonsterProgressChanged += HandleWaveMonsterProgressChanged;
            FantasyDefense.GridSystem.GridMatch3Solver.OnCascadeCombo += HandleCascadeCombo;
            FantasyDefense.Mobile.DevicePerformanceManager.OnHapticsToggled += HandleHapticsToggled;
        }

        private void OnDisable()
        {
            CastleBarricade.OnCastleHealthChanged -= HandleCastleHealthChanged;
            WaveSpawner.OnWaveStarted -= HandleWaveStarted;
            WaveSpawner.OnWaveCompleted -= HandleWaveCompleted;
            EconomyManager.OnGemsChanged -= HandleGemsChanged;
            GameManager.OnBattleStateChanged -= HandleBattleStateChanged;
            GameManager.OnBattleModeSelected -= HandleBattleModeSelected;
            GameManager.OnMovesRemainingChanged -= HandleMovesRemainingChanged;
            MonsterController.OnMonsterKilled -= HandleMonsterCountUpdate;
            MonsterController.OnMonsterSpawned -= HandleMonsterCountUpdate;

            BossMonsterController.OnBossSpawned -= HandleBossSpawned;
            BossMonsterController.OnBossHealthChanged -= HandleBossHealthChanged;
            BossMonsterController.OnBossEnraged -= HandleBossEnraged;
            BossMonsterController.OnBossDefeated -= HandleBossDefeated;

            BattlefieldWeatherManager.OnWeatherChanged -= HandleWeatherChanged;
            AutoMergeAssistant.OnAutoMergeToggled -= HandleAutoMergeToggled;
            CitadelLocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
            GameManager.OnInterWavePreparationStarted -= HandleInterWavePreparationStarted;
            WaveSpawner.OnWaveMonsterProgressChanged -= HandleWaveMonsterProgressChanged;
            FantasyDefense.GridSystem.GridMatch3Solver.OnCascadeCombo -= HandleCascadeCombo;
            FantasyDefense.Mobile.DevicePerformanceManager.OnHapticsToggled -= HandleHapticsToggled;
            StopLowHpDangerAlert();
        }

        private void HandleLanguageChanged(AppLanguage lang)
        {
            if (CastleBarricade.Instance != null)
            {
                UpdateCastleHpDisplay(CastleBarricade.Instance.CurrentHealth, CastleBarricade.Instance.MaxHealth);
            }
            UpdateMonsterCountDisplay();
            UpdateAutoMergeDisplay(AutoMergeAssistant.Instance != null && AutoMergeAssistant.Instance.IsAutoMergeEnabled);
            UpdateVibrationButtonDisplay();

            if (startDefenseNowButton != null)
            {
                var txt = startDefenseNowButton.GetComponentInChildren<Text>();
                if (txt != null)
                {
                    txt.text = CitadelLocalizationManager.Text("MULAI BERTAHAN SEKARANG", "COMMENCE DEFENSE NOW");
                }
            }

            if (victoryReturnButton != null)
            {
                var txt = victoryReturnButton.GetComponentInChildren<Text>();
                if (txt != null)
                {
                    txt.text = CitadelLocalizationManager.Text("KEMBALI KE CITADEL", "RETURN TO CITADEL");
                }
            }

            if (defeatRetryButton != null)
            {
                var txt = defeatRetryButton.GetComponentInChildren<Text>();
                if (txt != null)
                {
                    txt.text = CitadelLocalizationManager.Text("COBA LAGI", "RETRY DEFENSE");
                }
            }

            if (defeatReturnButton != null)
            {
                var txt = defeatReturnButton.GetComponentInChildren<Text>();
                if (txt != null)
                {
                    txt.text = CitadelLocalizationManager.Text("KEMBALI KE CITADEL", "RETURN TO CITADEL");
                }
            }

            if (pausePanel != null)
            {
                var titleTxt = pausePanel.transform.Find("Title")?.GetComponent<Text>();
                if (titleTxt != null)
                {
                    titleTxt.text = CitadelLocalizationManager.Text("PERMAINAN DIHENTIKAN", "GAME PAUSED");
                }
                var resumeBtn = pausePanel.transform.Find("ResumeButton")?.GetComponentInChildren<Text>();
                if (resumeBtn != null)
                {
                    resumeBtn.text = CitadelLocalizationManager.Text("LANJUTKAN (RESUME)", "RESUME");
                }
                var surrenderBtn = pausePanel.transform.Find("SurrenderButton")?.GetComponentInChildren<Text>();
                if (surrenderBtn != null)
                {
                    surrenderBtn.text = CitadelLocalizationManager.Text("MENYERAH & KEMBALI KE CITADEL", "SURRENDER & RETURN TO CITADEL");
                }
            }

            if (victoryPanel != null)
            {
                var titleTxt = victoryPanel.transform.Find("Title")?.GetComponent<Text>();
                if (titleTxt != null)
                {
                    titleTxt.text = CitadelLocalizationManager.Text("KEMENANGAN TELAK!", "DECISIVE VICTORY!");
                }
            }

            if (defeatPanel != null)
            {
                var titleTxt = defeatPanel.transform.Find("Title")?.GetComponent<Text>();
                if (titleTxt != null)
                {
                    titleTxt.text = CitadelLocalizationManager.Text("PERTAHANAN GAGAL!", "DEFENSE FAILED!");
                }
            }

            if (GameManager.Instance != null && movesRemainingText != null)
            {
                HandleMovesRemainingChanged(GameManager.Instance.MovesRemaining);
            }
        }

        private void Start()
        {
            ApplyLilitaOneFonts();

            // Sinkronisasi data awal jika komponen sudah tersedia
            if (EconomyManager.Instance != null)
            {
                UpdateGemsDisplay(EconomyManager.Instance.CurrentGems);
            }

            if (CastleBarricade.Instance != null)
            {
                UpdateCastleHpDisplay(CastleBarricade.Instance.CurrentHealth, CastleBarricade.Instance.MaxHealth);
            }

            UpdateAutoMergeDisplay(AutoMergeAssistant.Instance != null && AutoMergeAssistant.Instance.IsAutoMergeEnabled);

            HideAllModals();
        }

        private void ApplyLilitaOneFonts()
        {
            Font lilitaFont = Resources.Load<Font>("Fonts/LilitaOne-Regular");
            if (lilitaFont == null) return;

            Text[] allTexts = GetComponentsInChildren<Text>(true);
            foreach (var t in allTexts)
            {
                if (t != null)
                {
                    t.font = lilitaFont;
                }
            }
        }

        private void InitializeButtons()
        {
            if (speedToggleButton != null)
            {
                speedToggleButton.onClick.AddListener(ToggleGameSpeed);
            }

            if (pauseButton != null)
            {
                pauseButton.onClick.AddListener(TogglePause);
            }

            if (autoMergeToggleButton != null)
            {
                autoMergeToggleButton.onClick.AddListener(ToggleAutoMerge);
            }

            if (victoryReturnButton != null)
            {
                victoryReturnButton.onClick.AddListener(OnReturnToCitadelClicked);
            }

            if (defeatRetryButton != null)
            {
                defeatRetryButton.onClick.AddListener(OnRetryBattleClicked);
            }

            if (defeatReturnButton != null)
            {
                defeatReturnButton.onClick.AddListener(OnReturnToCitadelClicked);
            }

            if (startDefenseNowButton != null)
            {
                startDefenseNowButton.onClick.AddListener(OnStartDefenseNowClicked);
            }

            if (pausePanel != null)
            {
                var resumeBtn = pausePanel.transform.Find("ResumeButton")?.GetComponent<Button>();
                if (resumeBtn != null)
                {
                    resumeBtn.onClick.AddListener(TogglePause);
                }

                var surrenderBtn = pausePanel.transform.Find("SurrenderButton")?.GetComponent<Button>();
                if (surrenderBtn != null)
                {
                    surrenderBtn.onClick.AddListener(OnReturnToCitadelClicked);
                }

                InitializePauseVibrationButton();
            }
        }

        #region Event Handlers

        private void HandleCastleHealthChanged(int current, int max)
        {
            UpdateCastleHpDisplay(current, max);
        }

        private void HandleWaveStarted(int currentWave, int totalWaves)
        {
            if (waveText != null)
            {
                waveText.text = $"<b>WAVE {currentWave}/{totalWaves}</b>";
            }
            if (waveProgressBarContainer != null)
            {
                waveProgressBarContainer.SetActive(true);
            }
            if (waveProgressBar != null)
            {
                waveProgressBar.value = 0;
            }
            UpdateMonsterCountDisplay();

            // Efek Suara Genderang / Terompet Perang Masuk (Sprint 43)
            if (FantasyDefense.Audio.AudioManager.Instance != null)
            {
                FantasyDefense.Audio.AudioManager.Instance.PlaySound(FantasyDefense.Audio.SoundType.WaveIncoming);
            }
        }

        private void HandleWaveCompleted(int completedWave)
        {
            if (waveText != null)
            {
                string completedLabel = CitadelLocalizationManager.Text("CLEARED!", "CLEARED!");
                waveText.text = $"<b>WAVE {completedWave} {completedLabel}</b>";
            }
            if (waveProgressBar != null)
            {
                waveProgressBar.value = waveProgressBar.maxValue;
            }
        }

        private void HandleWaveMonsterProgressChanged(int remaining, int total)
        {
            if (waveProgressBarContainer != null && !waveProgressBarContainer.activeSelf)
            {
                waveProgressBarContainer.SetActive(true);
            }

            if (waveProgressBar != null)
            {
                waveProgressBar.maxValue = Mathf.Max(1, total);
                int defeated = Mathf.Max(0, total - remaining);
                waveProgressBar.value = defeated;
            }

            if (waveProgressText != null)
            {
                int defeated = Mathf.Max(0, total - remaining);
                waveProgressText.text = $"{defeated} / {total}";
            }

            UpdateMonsterCountDisplay();
        }

        private void HandleGemsChanged(int newGems)
        {
            UpdateGemsDisplay(newGems);
        }

        private void HandleMonsterCountUpdate(MonsterController monster)
        {
            UpdateMonsterCountDisplay();
        }

        private void HandleBattleModeSelected(BattleMode mode)
        {
            if (modeBadgeContainer != null) modeBadgeContainer.SetActive(true);
            if (modeBadgeText != null)
            {
                modeBadgeText.text = mode == BattleMode.PuzzlePreparation ? "MODE 1: PUZZLE PREPARATION" : "MODE 2: REAL-TIME DEPLOY";
            }

            if (puzzlePreparationPanel != null)
            {
                puzzlePreparationPanel.SetActive(mode == BattleMode.PuzzlePreparation);
            }

            if (startDefenseNowButton != null)
            {
                startDefenseNowButton.gameObject.SetActive(mode == BattleMode.PuzzlePreparation);
            }

            if (summonButtonContainer != null)
            {
                summonButtonContainer.SetActive(mode == BattleMode.RealTimeDeploy);
            }
        }

        private Coroutine movePunchCoroutine = null;

        private void HandleMovesRemainingChanged(int moves)
        {
            if (movesRemainingText != null)
            {
                if (moves <= 3 && moves > 0)
                {
                    string warningLabel = CitadelLocalizationManager.Text("⚠️ Sisa Langkah", "⚠️ Moves Left");
                    movesRemainingText.text = $"{warningLabel}: {moves}";
                    movesRemainingText.color = new Color(1f, 0.45f, 0.1f);

                    if (movePunchCoroutine != null) StopCoroutine(movePunchCoroutine);
                    movePunchCoroutine = StartCoroutine(PunchScaleRoutine(movesRemainingText.transform, 1.25f, 0.22f));

                    FantasyDefense.Mobile.DevicePerformanceManager.Instance?.TriggerHaptic(FantasyDefense.Mobile.HapticFeedbackType.Light);
                }
                else if (moves <= 0)
                {
                    movesRemainingText.text = CitadelLocalizationManager.Text("⚡ BERTAHAN SEKARANG!", "⚡ DEFEND NOW!");
                    movesRemainingText.color = new Color(1f, 0.25f, 0.25f);

                    if (movePunchCoroutine != null) StopCoroutine(movePunchCoroutine);
                    movePunchCoroutine = StartCoroutine(PunchScaleRoutine(movesRemainingText.transform, 1.35f, 0.28f));

                    FantasyDefense.Mobile.DevicePerformanceManager.Instance?.TriggerHaptic(FantasyDefense.Mobile.HapticFeedbackType.Medium);
                }
                else
                {
                    string movesLabel = CitadelLocalizationManager.Text("⚡ Sisa Langkah", "⚡ Moves");
                    movesRemainingText.text = $"{movesLabel}: {moves}";
                    movesRemainingText.color = new Color(1f, 0.9f, 0.3f);
                }
            }
        }

        private IEnumerator PunchScaleRoutine(Transform targetTransform, float targetScale, float duration)
        {
            if (targetTransform == null) yield break;
            Vector3 original = Vector3.one;
            float elapsed = 0f;
            float half = duration * 0.5f;

            while (elapsed < half)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / half);
                targetTransform.localScale = Vector3.Lerp(original, original * targetScale, t);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / half);
                targetTransform.localScale = Vector3.Lerp(original * targetScale, original, t);
                yield return null;
            }

            targetTransform.localScale = original;
        }

        private void HandleBossSpawned(BossMonsterController boss)
        {
            if (bossBarContainer != null) bossBarContainer.SetActive(true);
            if (bossEnrageBanner != null) bossEnrageBanner.SetActive(false);

            if (bossNameText != null)
            {
                bossNameText.text = boss.Data != null ? boss.Data.monsterName : "BOSS MONSTER";
            }
            if (bossTitleText != null)
            {
                bossTitleText.text = boss.BossTitle;
            }
            if (bossHpSlider != null)
            {
                bossHpSlider.maxValue = boss.MaxHealth;
                bossHpSlider.value = boss.CurrentHealth;
            }
            if (bossHpText != null)
            {
                bossHpText.text = $"{Mathf.RoundToInt(boss.CurrentHealth)} / {Mathf.RoundToInt(boss.MaxHealth)} (100%)";
            }
        }

        private void HandleBossHealthChanged(BossMonsterController boss, float current, float max)
        {
            if (bossHpSlider != null)
            {
                bossHpSlider.value = current;
            }
            if (bossHpText != null)
            {
                float percent = (current / Mathf.Max(1f, max)) * 100f;
                bossHpText.text = $"{Mathf.RoundToInt(current)} / {Mathf.RoundToInt(max)} ({Mathf.RoundToInt(percent)}%)";
            }
        }

        private void HandleBossEnraged(BossMonsterController boss)
        {
            if (bossEnrageBanner != null)
            {
                bossEnrageBanner.SetActive(true);
            }
        }

        private void HandleBossDefeated(BossMonsterController boss)
        {
            if (bossBarContainer != null)
            {
                bossBarContainer.SetActive(false);
            }
            if (bossEnrageBanner != null)
            {
                bossEnrageBanner.SetActive(false);
            }
        }

        private Coroutine interWaveBannerRoutine;

        private void HandleCascadeCombo(int comboChain)
        {
            if (comboChain >= 2)
            {
                if (movesRemainingText != null)
                {
                    if (movePunchCoroutine != null) StopCoroutine(movePunchCoroutine);
                    movePunchCoroutine = StartCoroutine(PunchScaleRoutine(movesRemainingText.transform, 1.25f, 0.22f));
                }

                ShowFloatingComboText(comboChain);
            }
        }

        private void ShowFloatingComboText(int comboChain)
        {
            var lilitaFont = Resources.Load<Font>("Fonts/LilitaOne-Regular");
            var floatingObj = new GameObject("FloatingComboNotice", typeof(RectTransform), typeof(Text));
            floatingObj.transform.SetParent(transform, false);

            var rt = floatingObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.52f);
            rt.anchorMax = new Vector2(0.5f, 0.52f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(500, 80);

            var txt = floatingObj.GetComponent<Text>();
            if (lilitaFont != null) txt.font = lilitaFont;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.fontSize = comboChain >= 4 ? 38 : 32;
            txt.fontStyle = FontStyle.Bold;

            if (comboChain >= 4)
            {
                txt.text = $"🔥 MEGA CASCADE x{comboChain}! 🔥";
                txt.color = new Color(1f, 0.35f, 0.15f);
            }
            else if (comboChain == 3)
            {
                txt.text = $"⚡ TRIPLE COMBO x3! ⚡";
                txt.color = new Color(1f, 0.85f, 0.2f);
            }
            else
            {
                txt.text = $"✨ TACTICAL COMBO x2! ✨";
                txt.color = new Color(0.35f, 0.95f, 0.85f);
            }

            StartCoroutine(FloatingComboRoutine(floatingObj));
        }

        private IEnumerator FloatingComboRoutine(GameObject obj)
        {
            if (obj == null) yield break;

            var rt = obj.GetComponent<RectTransform>();
            var txt = obj.GetComponent<Text>();
            Vector2 startPos = rt.anchoredPosition;
            Vector3 startScale = Vector3.one * 0.6f;
            float duration = 0.85f;
            float elapsed = 0f;
            Color origColor = txt.color;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Naiknya teks secara melayang ke atas
                rt.anchoredPosition = startPos + new Vector2(0f, Mathf.Sin(t * Mathf.PI * 0.5f) * 65f);

                // Bounce pembesar di awal
                float bounce = Mathf.Sin(t * Mathf.PI) * 0.45f;
                rt.localScale = startScale * (1f + bounce);

                // Pudar transparan di akhir
                if (t > 0.5f)
                {
                    float alpha = (1f - t) / 0.5f;
                    txt.color = new Color(origColor.r, origColor.g, origColor.b, alpha);
                }

                yield return null;
            }

            Destroy(obj);
        }

        private void HandleInterWavePreparationStarted(int nextWaveNumber, int bonusMoves)
        {
            int totalWaves = WaveSpawner.Instance != null ? WaveSpawner.Instance.TotalWaves : 3;
            if (waveText != null)
            {
                waveText.text = $"<b>WAVE {nextWaveNumber}/{totalWaves}</b>";
            }
            if (waveProgressBar != null)
            {
                waveProgressBar.value = 0;
            }
            if (waveProgressText != null)
            {
                waveProgressText.text = "0 / -";
            }

            if (puzzlePreparationPanel != null)
            {
                puzzlePreparationPanel.SetActive(true);
            }

            if (startDefenseNowButton != null)
            {
                startDefenseNowButton.gameObject.SetActive(true);
                var btnTxt = startDefenseNowButton.GetComponentInChildren<Text>();
                if (btnTxt != null)
                {
                    btnTxt.text = CitadelLocalizationManager.Text($"MULAI WAVE {nextWaveNumber} SEKARANG", $"COMMENCE WAVE {nextWaveNumber} NOW");
                }
            }

            HandleMovesRemainingChanged(bonusMoves);



            if (upcomingThreatText != null && WaveSpawner.Instance != null)

            {

                string preview = WaveSpawner.Instance.GetUpcomingWavePreviewSummary(nextWaveNumber);

                upcomingThreatText.gameObject.SetActive(!string.IsNullOrEmpty(preview));

                upcomingThreatText.text = preview;

            }

            if (interWaveBannerRoutine != null)
            {
                StopCoroutine(interWaveBannerRoutine);
            }
            interWaveBannerRoutine = StartCoroutine(ShowInterWaveBannerRoutine(nextWaveNumber, bonusMoves));

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.WaveIncoming);
            }
        }

        private IEnumerator ShowInterWaveBannerRoutine(int nextWaveNumber, int bonusMoves)
        {
            if (interWaveBannerPanel != null)
            {
                interWaveBannerPanel.SetActive(true);

                if (interWaveTitleText != null)
                {
                    interWaveTitleText.text = CitadelLocalizationManager.Text(
                        $"⚔️ GELOMBANG {nextWaveNumber} BERSIAP!",
                        $"⚔️ WAVE {nextWaveNumber} INCOMING!"
                    );
                }

                if (interWaveSubText != null)
                {
                    interWaveSubText.text = CitadelLocalizationManager.Text(
                        $"Bonus +{bonusMoves} Langkah Taktis! Atur posisi & tingkatkan hero sekarang!",
                        $"Bonus +{bonusMoves} Tactical Moves! Reposition & merge heroes now!"
                    );
                }

                var canvasGroup = interWaveBannerPanel.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                    float elapsed = 0f;
                    float fadeIn = 0.25f;
                    while (elapsed < fadeIn)
                    {
                        elapsed += Time.deltaTime;
                        canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeIn);
                        yield return null;
                    }
                    canvasGroup.alpha = 1f;

                    yield return new WaitForSeconds(2.8f);

                    elapsed = 0f;
                    float fadeOut = 0.35f;
                    while (elapsed < fadeOut)
                    {
                        elapsed += Time.deltaTime;
                        canvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / fadeOut));
                        yield return null;
                    }
                    canvasGroup.alpha = 0f;
                }
                else
                {
                    yield return new WaitForSeconds(3.0f);
                }

                interWaveBannerPanel.SetActive(false);
            }
            interWaveBannerRoutine = null;
        }

        private void OnStartDefenseNowClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartBattleWavesNow();
            }
            if (puzzlePreparationPanel != null)
            {
                puzzlePreparationPanel.SetActive(false);
            }
            if (interWaveBannerPanel != null)
            {
                interWaveBannerPanel.SetActive(false);
            }
            if (Audio.AudioManager.Instance != null)
            {
                Audio.AudioManager.Instance.PlaySound(Audio.SoundType.ButtonClick);
            }
        }

        private void HandleBattleStateChanged(BattleState state)
        {
            switch (state)
            {
                case BattleState.Preparation:
                    StopLowHpDangerAlert();
                    bool isPuzzleMode = GameManager.Instance != null && GameManager.Instance.CurrentBattleMode == BattleMode.PuzzlePreparation;
                    if (puzzlePreparationPanel != null)
                    {
                        puzzlePreparationPanel.SetActive(isPuzzleMode);
                    }
                    if (startDefenseNowButton != null)
                    {
                        startDefenseNowButton.gameObject.SetActive(isPuzzleMode);
                    }
                    if (summonButtonContainer != null)
                    {
                        summonButtonContainer.SetActive(!isPuzzleMode);
                    }
                    break;
                case BattleState.WaveActive:
                    if (puzzlePreparationPanel != null)
                    {
                        puzzlePreparationPanel.SetActive(false);
                    }
                    if (interWaveBannerPanel != null)
                    {
                        interWaveBannerPanel.SetActive(false);
                    }
                    if (summonButtonContainer != null && GameManager.Instance != null)
                    {
                        summonButtonContainer.SetActive(GameManager.Instance.CurrentBattleMode == BattleMode.RealTimeDeploy);
                    }
                    break;
                case BattleState.Victory:
                    StopLowHpDangerAlert();
                    if (interWaveBannerPanel != null) interWaveBannerPanel.SetActive(false);
                    ShowVictoryModal();
                    break;
                case BattleState.Defeat:
                    StopLowHpDangerAlert();
                    if (interWaveBannerPanel != null) interWaveBannerPanel.SetActive(false);
                    ShowDefeatModal();
                    break;
            }
        }

        private void HandleWeatherChanged(WeatherType weather)
        {
            if (weatherBadgeContainer != null)
            {
                weatherBadgeContainer.SetActive(weather != WeatherType.None);
            }

            if (weatherBadgeText != null && BattlefieldWeatherManager.Instance != null)
            {
                weatherBadgeText.text = BattlefieldWeatherManager.Instance.GetWeatherName(weather);
            }

            if (weatherDescText != null && BattlefieldWeatherManager.Instance != null)
            {
                weatherDescText.text = BattlefieldWeatherManager.Instance.GetWeatherDescription(weather);
            }
        }

        #endregion

        #region HUD Display Updates

        private Coroutine castleHpTweenCoroutine;
        private Coroutine hpBarShakeCoroutine;
        private Vector3 hpBarOriginalScale = Vector3.one;

        private void UpdateCastleHpDisplay(int current, int max)
        {
            if (castleHpSlider != null)
            {
                int prevVal = Mathf.RoundToInt(castleHpSlider.value);
                castleHpSlider.maxValue = max;
                if (castleHpTweenCoroutine != null)
                {
                    StopCoroutine(castleHpTweenCoroutine);
                }
                castleHpTweenCoroutine = StartCoroutine(SmoothCastleHpRoutine(current));

                // Shake & punch-scale pada slider saat menerima damage
                if (current < prevVal && gameObject.activeInHierarchy)
                {
                    if (hpBarShakeCoroutine != null) StopCoroutine(hpBarShakeCoroutine);
                    hpBarShakeCoroutine = StartCoroutine(PunchScaleHpBarRoutine());
                }
            }

            if (castleHpText != null)
            {
                float percent = (float)current / Mathf.Max(1, max) * 100f;
                if (percent <= 30f)
                {
                    castleHpText.text = $"<b><color=#FF4444>⚠️ {current:N0} / {max:N0}</color></b>";
                    castleHpText.color = new Color(1f, 0.35f, 0.35f);
                }
                else
                {
                    castleHpText.text = $"<b><color=#00FF66>💚 {current:N0} / {max:N0}</color></b>";
                    castleHpText.color = Color.white;
                }
            }

            // Peringatan Ketegangan Barikade Kritis: Pulsing Danger Vignette (Sprint 43)
            float hpPct = (float)current / Mathf.Max(1, max) * 100f;
            if (hpPct <= 30f && current > 0)
            {
                TriggerLowHpDangerAlert();
            }
            else
            {
                StopLowHpDangerAlert();
            }
        }

        private IEnumerator PunchScaleHpBarRoutine()
        {
            if (castleHpSlider == null) yield break;
            Transform tr = castleHpSlider.transform;
            if (hpBarOriginalScale == Vector3.one && tr.localScale != Vector3.zero)
            {
                hpBarOriginalScale = tr.localScale;
            }

            // Kilatan merah pada fill slider saat terkena damage
            Image fillImg = castleHpSlider.fillRect != null ? castleHpSlider.fillRect.GetComponent<Image>() : null;
            Color origFillCol = fillImg != null ? fillImg.color : Color.green;

            float duration = 0.22f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                float punch = Mathf.Sin(t * Mathf.PI) * 0.18f;
                tr.localScale = new Vector3(hpBarOriginalScale.x * (1f + punch), hpBarOriginalScale.y * (1f - punch * 0.5f), hpBarOriginalScale.z);

                if (fillImg != null)
                {
                    float flash = Mathf.Sin(t * Mathf.PI);
                    fillImg.color = Color.Lerp(origFillCol, Color.white, flash * 0.6f);
                }

                yield return null;
            }

            if (fillImg != null) fillImg.color = origFillCol;
            tr.localScale = hpBarOriginalScale;
            hpBarShakeCoroutine = null;
        }

        private IEnumerator SmoothCastleHpRoutine(float targetVal)
        {
            if (castleHpSlider == null) yield break;

            float startVal = castleHpSlider.value;
            float duration = 0.35f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easeOut = 1f - Mathf.Pow(1f - t, 3f);
                castleHpSlider.value = Mathf.Lerp(startVal, targetVal, easeOut);
                yield return null;
            }

            castleHpSlider.value = targetVal;
            castleHpTweenCoroutine = null;
        }

        private void UpdateGemsDisplay(int gems)
        {
            if (gemsText != null)
            {
                gemsText.text = $"{gems} Gems";
            }
        }

        private void UpdateMonsterCountDisplay()
        {
            if (monsterCountText != null)
            {
                int activeCount = MonsterController.ActiveMonsters.Count;
                string enemiesLabel = CitadelLocalizationManager.Text("Musuh Aktif", "Active Monsters");
                monsterCountText.text = $"{enemiesLabel}: {activeCount}";
            }
        }

        #endregion

        #region Low HP Danger Alert & Vignette (Point 1 - Citadel Danger Pulse)

        [Header("Critical Barricade HP Danger Alert (Sprint 43)")]
        [SerializeField] private Image dangerVignetteImage;
        private Coroutine dangerVignetteCoroutine = null;
        private bool isDangerAlertActive = false;
        private static Sprite cachedVignetteSprite;

        private void EnsureDangerVignette()
        {
            if (dangerVignetteImage != null) return;

            GameObject vigObj = new GameObject("LowHpDangerVignette", typeof(RectTransform), typeof(Image));
            vigObj.transform.SetParent(transform, false);
            vigObj.transform.SetAsFirstSibling();

            RectTransform rt = vigObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            dangerVignetteImage = vigObj.GetComponent<Image>();
            dangerVignetteImage.sprite = GetOrCreateVignetteSprite();
            dangerVignetteImage.color = new Color(0.85f, 0.08f, 0.08f, 0f);
            dangerVignetteImage.raycastTarget = false;
        }

        private static Sprite GetOrCreateVignetteSprite()
        {
            if (cachedVignetteSprite != null) return cachedVignetteSprite;

            int w = 128;
            int h = 128;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                float ny = (y - (h - 1) * 0.5f) / ((h - 1) * 0.5f);
                for (int x = 0; x < w; x++)
                {
                    float nx = (x - (w - 1) * 0.5f) / ((w - 1) * 0.5f);
                    float dist = Mathf.Sqrt(nx * nx + ny * ny);
                    // Lingkar tengah transparan, tepi layar berdenyut merah tua
                    float alpha = Mathf.Clamp01((dist - 0.40f) / 0.60f);
                    alpha = alpha * alpha; // Quad falloff halus
                    pixels[y * w + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            cachedVignetteSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
            return cachedVignetteSprite;
        }

        private void TriggerLowHpDangerAlert()
        {
            EnsureDangerVignette();
            if (isDangerAlertActive) return;

            isDangerAlertActive = true;
            if (dangerVignetteCoroutine != null) StopCoroutine(dangerVignetteCoroutine);
            dangerVignetteCoroutine = StartCoroutine(PulsingVignetteRoutine());

            FantasyDefense.Mobile.DevicePerformanceManager.Instance?.TriggerHaptic(FantasyDefense.Mobile.HapticFeedbackType.Warning);
        }

        private void StopLowHpDangerAlert()
        {
            if (!isDangerAlertActive && (dangerVignetteImage == null || dangerVignetteImage.color.a <= 0.01f)) return;

            isDangerAlertActive = false;
            if (dangerVignetteCoroutine != null)
            {
                StopCoroutine(dangerVignetteCoroutine);
                dangerVignetteCoroutine = null;
            }

            if (dangerVignetteImage != null && gameObject.activeInHierarchy)
            {
                StartCoroutine(FadeVignetteOutRoutine());
            }
            else if (dangerVignetteImage != null)
            {
                Color c = dangerVignetteImage.color;
                c.a = 0f;
                dangerVignetteImage.color = c;
            }
        }

        private IEnumerator PulsingVignetteRoutine()
        {
            while (isDangerAlertActive && dangerVignetteImage != null)
            {
                float pulse = (Mathf.Sin(Time.unscaledTime * 4.5f) + 1f) * 0.5f;
                float alpha = Mathf.Lerp(0.08f, 0.32f, pulse);
                dangerVignetteImage.color = new Color(0.85f, 0.08f, 0.08f, alpha);
                yield return null;
            }
        }

        private IEnumerator FadeVignetteOutRoutine()
        {
            if (dangerVignetteImage == null) yield break;
            float startAlpha = dangerVignetteImage.color.a;
            float elapsed = 0f;
            float duration = 0.35f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float a = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
                dangerVignetteImage.color = new Color(0.85f, 0.08f, 0.08f, a);
                yield return null;
            }

            Color finalC = dangerVignetteImage.color;
            finalC.a = 0f;
            dangerVignetteImage.color = finalC;
        }

        #endregion

        #region In-Battle Vibration Control (Enable/Disable Vibration)

        private void InitializePauseVibrationButton()
        {
            if (pausePanel == null) return;

            if (pauseVibrationButton == null)
            {
                Transform vibTransform = pausePanel.transform.Find("VibrationButton");
                if (vibTransform != null)
                {
                    pauseVibrationButton = vibTransform.GetComponent<Button>();
                    pauseVibrationText = vibTransform.GetComponentInChildren<Text>();
                }
                else
                {
                    Transform resumeTransform = pausePanel.transform.Find("ResumeButton");
                    Transform surrenderTransform = pausePanel.transform.Find("SurrenderButton");

                    if (resumeTransform != null && surrenderTransform != null)
                    {
                        GameObject vibObj = Instantiate(resumeTransform.gameObject, pausePanel.transform);
                        vibObj.name = "VibrationButton";
                        pauseVibrationButton = vibObj.GetComponent<Button>();
                        pauseVibrationText = vibObj.GetComponentInChildren<Text>();

                        RectTransform rtResume = resumeTransform.GetComponent<RectTransform>();
                        RectTransform rtSurrender = surrenderTransform.GetComponent<RectTransform>();
                        RectTransform rtVib = vibObj.GetComponent<RectTransform>();

                        if (rtResume != null && rtSurrender != null && rtVib != null)
                        {
                            rtResume.anchorMin = new Vector2(0.12f, 0.60f);
                            rtResume.anchorMax = new Vector2(0.88f, 0.75f);

                            rtVib.anchorMin = new Vector2(0.12f, 0.39f);
                            rtVib.anchorMax = new Vector2(0.88f, 0.54f);

                            rtSurrender.anchorMin = new Vector2(0.12f, 0.18f);
                            rtSurrender.anchorMax = new Vector2(0.88f, 0.33f);
                        }

                        Image vibImg = vibObj.GetComponent<Image>();
                        if (vibImg != null)
                        {
                            vibImg.color = new Color(0.20f, 0.35f, 0.52f, 1f);
                        }
                    }
                }
            }

            if (pauseVibrationButton != null)
            {
                pauseVibrationButton.onClick.RemoveAllListeners();
                pauseVibrationButton.onClick.AddListener(OnPauseVibrationToggleClicked);
                UpdateVibrationButtonDisplay();
            }
        }

        private void OnPauseVibrationToggleClicked()
        {
            if (FantasyDefense.Mobile.DevicePerformanceManager.Instance != null)
            {
                FantasyDefense.Mobile.DevicePerformanceManager.Instance.ToggleHaptics();
            }
            UpdateVibrationButtonDisplay();

            if (Audio.AudioManager.Instance != null)
            {
                Audio.AudioManager.Instance.PlaySound(Audio.SoundType.ButtonClick);
            }
        }

        private void HandleHapticsToggled(bool isEnabled)
        {
            UpdateVibrationButtonDisplay();
        }

        private void UpdateVibrationButtonDisplay()
        {
            if (pauseVibrationText == null) return;

            bool isHaptic = FantasyDefense.Mobile.DevicePerformanceManager.Instance == null ||
                            FantasyDefense.Mobile.DevicePerformanceManager.Instance.IsHapticsEnabled;

            string txt = isHaptic ?
                CitadelLocalizationManager.Text("📳 GETARAN: AKTIF", "📳 VIBRATION: ON") :
                CitadelLocalizationManager.Text("📴 GETARAN: NONAKTIF", "📴 VIBRATION: OFF");

            pauseVibrationText.text = txt;
            pauseVibrationText.color = isHaptic ? new Color(0.35f, 1f, 0.55f) : new Color(0.85f, 0.85f, 0.85f);
        }

        #endregion

        #region Game Controls (Speed, Pause & Auto-Merge)

        public void ToggleGameSpeed()
        {
            currentSpeedMode++;
            if (currentSpeedMode > 3) currentSpeedMode = 1;

            float targetScale = currentSpeedMode == 1 ? 1.0f : (currentSpeedMode == 2 ? 2.0f : 3.0f);
            if (!isPaused)
            {
                Time.timeScale = targetScale;
            }

            if (speedToggleText != null)
            {
                speedToggleText.text = $"{currentSpeedMode}x Speed";
            }

            if (Audio.AudioManager.Instance != null)
            {
                Audio.AudioManager.Instance.PlaySound(Audio.SoundType.ButtonClick);
            }

            Debug.Log($"[UIBattleView] Kecepatan Tempur Diubah: {currentSpeedMode}x (timeScale: {targetScale})");
        }

        public void TogglePause()
        {
            isPaused = !isPaused;
            float currentScale = currentSpeedMode == 1 ? 1.0f : (currentSpeedMode == 2 ? 2.0f : 3.0f);
            Time.timeScale = isPaused ? 0f : currentScale;

            if (pausePanel != null)
            {
                pausePanel.SetActive(isPaused);
                if (isPaused)
                {
                    UpdateVibrationButtonDisplay();
                }
            }

            if (Audio.AudioManager.Instance != null)
            {
                Audio.AudioManager.Instance.PlaySound(Audio.SoundType.ButtonClick);
            }
        }

        public void ToggleAutoMerge()
        {
            if (AutoMergeAssistant.Instance != null)
            {
                AutoMergeAssistant.Instance.ToggleAutoMerge();
            }
            else
            {
                Debug.LogWarning("[UIBattleView] AutoMergeAssistant belum terpasang di Scene!");
            }
        }

        private void HandleAutoMergeToggled(bool isEnabled)
        {
            UpdateAutoMergeDisplay(isEnabled);
        }

        private void UpdateAutoMergeDisplay(bool isEnabled)
        {
            if (autoMergeStatusText != null)
            {
                string onTxt = CitadelLocalizationManager.Text("Auto-Merge: AKTIF (ON)", "Auto-Merge: ACTIVE (ON)");
                string offTxt = CitadelLocalizationManager.Text("Auto-Merge: NONAKTIF (OFF)", "Auto-Merge: INACTIVE (OFF)");
                autoMergeStatusText.text = isEnabled ? onTxt : offTxt;
                autoMergeStatusText.color = isEnabled ? new Color(0.25f, 1f, 0.45f) : Color.white;
            }
        }

        #endregion

        #region Victory & Defeat Modals

        private void ShowVictoryModal()
        {
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(true);
            }

            // Hitung rating bintang berdasarkan sisa persentase HP benteng
            int currentHp = CastleBarricade.Instance != null ? CastleBarricade.Instance.CurrentHealth : 100;
            int maxHp = CastleBarricade.Instance != null ? CastleBarricade.Instance.MaxHealth : 100;
            float hpPercent = (float)currentHp / Mathf.Max(1, maxHp);

            string stars = "★☆☆";
            string victoryPraise = CitadelLocalizationManager.Text("BENTENG BERTAHAN!", "CITADEL DEFENDED!");
            if (hpPercent >= 0.85f)
            {
                stars = "★★★";
                victoryPraise = CitadelLocalizationManager.Text("KEMENANGAN SEMPURNA!", "PERFECT VICTORY!");
            }
            else if (hpPercent >= 0.45f)
            {
                stars = "★★☆";
                victoryPraise = CitadelLocalizationManager.Text("KEMENANGAN TELAK!", "DECISIVE VICTORY!");
            }

            int starCount = (hpPercent >= 0.85f) ? 3 : ((hpPercent >= 0.45f) ? 2 : 1);

            if (victoryStarsText != null)
            {
                StopCoroutine("AnimateVictoryStarsRoutine");
                StartCoroutine(AnimateVictoryStarsRoutine(starCount));
            }

            var titleTxt = victoryPanel != null ? victoryPanel.transform.Find("Title")?.GetComponent<Text>() : null;
            if (titleTxt != null)
            {
                titleTxt.text = victoryPraise;
            }

            // Integrasi ke StageManager untuk progres kampanye, bintang & rewards
            if (StageManager.Instance != null)
            {
                StageDataSO stage = StageManager.Instance.GetSelectedStage();
                bool isFirstClear = false;
                if (CitadelLobbyManager.Instance != null && stage != null)
                {
                    StageRecord rec = CitadelLobbyManager.Instance.AccountData.GetStageRecord(stage.stageId);
                    isFirstClear = !rec.isCleared;
                }

                StageManager.Instance.CompleteBattleStage(currentHp, maxHp);

                if (victoryRewardsText != null && stage != null)
                {
                    if (isFirstClear)
                    {
                        string firstClearHeader = CitadelLocalizationManager.Text("🎉 BONUS PENAKLUKAN PERDANA!", "🎉 FIRST-CLEAR BONUS!");
                        string shardInfo = stage.firstClearShards > 0 ? $"\n💎 +{stage.firstClearShards} Shards ({stage.firstClearShardHero})" : "";
                        string gearInfo = stage.firstClearGear != null ? $"\n🎁 Drop: [{stage.firstClearGear.itemName}]" : "";
                        victoryRewardsText.text = $"{stage.stageCode}: {stage.stageTitle}\n{firstClearHeader}\n+{stage.firstClearGold} Gold  |  +{stage.firstClearExp} EXP  |  +{stage.firstClearGems} Gems{shardInfo}{gearInfo}";
                    }
                    else
                    {
                        string repeatHeader = CitadelLocalizationManager.Text("⚔️ STAGE SELESAI (REPEAT CLEAR)", "⚔️ STAGE CLEARED");
                        victoryRewardsText.text = $"{stage.stageCode}: {stage.stageTitle}\n{repeatHeader}\n+{stage.repeatClearGold} Gold  |  +{stage.repeatClearExp} EXP  |  +{stage.repeatClearGems} Gems";
                    }
                }
            }
            else if (victoryRewardsText != null)
            {
                victoryRewardsText.text = "+150 Gold  |  +50 Citadel EXP  |  +30 Gems";
            }

            Debug.Log($"[UIBattleView] Victory Dialog Muncul! Rating: {stars}");
        }

        private void ShowDefeatModal()
        {
            if (defeatPanel != null)
            {
                defeatPanel.SetActive(true);
            }

            Debug.Log("[UIBattleView] Defeat Dialog Muncul! Pertahanan benteng gagal.");
        }

        private IEnumerator AnimateVictoryStarsRoutine(int starCount)
        {
            if (victoryStarsText == null) yield break;

            victoryStarsText.supportRichText = true;
            victoryStarsText.text = "<color=#3D4A5E><size=36>☆  ☆  ☆</size></color>";

            yield return new WaitForSecondsRealtime(0.35f);

            for (int i = 1; i <= 3; i++)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                for (int j = 1; j <= 3; j++)
                {
                    if (j <= i && j <= starCount)
                    {
                        sb.Append("<color=#FFD700><size=46>★</size></color>  ");
                    }
                    else
                    {
                        sb.Append("<color=#3D4A5E><size=36>☆</size></color>  ");
                    }
                }

                victoryStarsText.text = sb.ToString().TrimEnd();

                if (i <= starCount)
                {
                    if (AudioManager.Instance != null)
                    {
                        if (i == 3)
                        {
                            AudioManager.Instance.PlaySound(SoundType.VictoryFanfare);
                        }
                        else
                        {
                            float pitch = (i == 1) ? 1.05f : 1.30f;
                            AudioManager.Instance.PlaySound(SoundType.MergeSuccess, pitch);
                        }
                    }

                    if (SkillVfxManager.Instance != null)
                    {
                        Color starColor = i == 3 ? new Color(1f, 0.85f, 0.2f) : new Color(0.35f, 0.90f, 1f);
                        SkillVfxManager.Instance.SpawnProceduralParticleBurst(victoryStarsText.transform.position, starColor, Color.white, 12, 1.8f);
                    }

                    // Punch scale bintang dengan overshoot elastis
                    float punchDuration = 0.22f;
                    float elapsed = 0f;
                    Vector3 origScale = Vector3.one;
                    while (elapsed < punchDuration)
                    {
                        elapsed += Time.unscaledDeltaTime;
                        float t = elapsed / punchDuration;
                        float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.45f;
                        victoryStarsText.transform.localScale = origScale * scale;
                        yield return null;
                    }
                    victoryStarsText.transform.localScale = origScale;
                }

                yield return new WaitForSecondsRealtime(0.28f);
            }
        }

        private void HideAllModals()
        {
            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (defeatPanel != null) defeatPanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
            if (bossBarContainer != null) bossBarContainer.SetActive(false);
            if (bossEnrageBanner != null) bossEnrageBanner.SetActive(false);
        }

        private void OnReturnToCitadelClicked()
        {
            Time.timeScale = 1.0f;
            currentSpeedMode = 1;
            if (speedToggleText != null) speedToggleText.text = "1x Speed";

            if (Audio.AudioManager.Instance != null)
            {
                Audio.AudioManager.Instance.PlaySound(Audio.SoundType.ButtonClick);
            }

            Debug.Log("[UIBattleView] Kembali ke Lobby Citadel...");
            HideAllModals();

            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.ReturnToLobbyFromBattle();
            }
            else if (GameManager.Instance != null)
            {
                GameManager.Instance.ReturnToLobby();
            }
        }

        public void OpenView()
        {
            gameObject.SetActive(true);
            HideAllModals();
        }

        public void CloseView()
        {
            HideAllModals();
            gameObject.SetActive(false);
        }

        private void OnRetryBattleClicked()
        {
            Time.timeScale = 1.0f;
            currentSpeedMode = 1;
            if (speedToggleText != null) speedToggleText.text = "1x Speed";

            if (Audio.AudioManager.Instance != null)
            {
                Audio.AudioManager.Instance.PlaySound(Audio.SoundType.ButtonClick);
            }

            Debug.Log("[UIBattleView] Mengulang pertempuran (Retry)...");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.InitializeBattle();
            }
            HideAllModals();
        }

        #endregion
    }
}
