using UnityEngine;
using UnityEngine.UI;
using FantasyDefense.Audio;
using FantasyDefense.Mobile;
using FantasyDefense.Save;

namespace FantasyDefense.UI
{
    /// <summary>
    /// Master UI Controller untuk Pengaturan Audio & Performa Perangkat (System Settings).
    /// Mengelola volume BGM/SFX, Mute, Sakelar 30/60 FPS, Mode Hemat Baterai (Battery Saver),
    /// serta Haptic Vibration Feedback ponsel.
    /// </summary>
    public class UIAudioSettingsView : MonoBehaviour
    {
        [Header("Root Panel")]
        [SerializeField] private GameObject settingsPanel;

        [Header("Volume Sliders")]
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider bgmSlider;
        [SerializeField] private Slider sfxSlider;

        [Header("Volume Percent Texts")]
        [SerializeField] private Text masterPercentText;
        [SerializeField] private Text bgmPercentText;
        [SerializeField] private Text sfxPercentText;

        [Header("Mute Toggles")]
        [SerializeField] private Toggle bgmMuteToggle;
        [SerializeField] private Toggle sfxMuteToggle;

        [Header("Mobile Device Performance Controls")]
        [SerializeField] private Button fps30Button;
        [SerializeField] private Button fps60Button;
        [SerializeField] private Text currentFpsText;
        [SerializeField] private Toggle batterySaverToggle;
        [SerializeField] private Toggle hapticFeedbackToggle;

        [SerializeField] private Button closeButton;
        [SerializeField] private Button testSfxButton;

        [Header("Localization Language Controls")]
        [SerializeField] private Button languageToggleButton;
        [SerializeField] private Text languageStatusText;

        private void Awake()
        {
            InitializeActionButtons();
            ApplyButtonStyling();
        }

        private void ApplyButtonStyling()
        {
            var btnClose = Resources.Load<Sprite>("Sprites/UI/Btn_Close_X");
            var btnGold = Resources.Load<Sprite>("Sprites/UI/Btn_Primary_Gold");
            var btnStone = Resources.Load<Sprite>("Sprites/UI/Btn_Secondary_Stone");

            if (closeButton != null && btnClose != null)
            {
                var img = closeButton.GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = btnClose;
                    img.type = Image.Type.Simple;
                    img.preserveAspect = true;
                    img.color = Color.white;
                }
                var txt = closeButton.GetComponentInChildren<Text>();
                if (txt != null) txt.text = "";
            }

            if (testSfxButton != null && btnGold != null)
            {
                var img = testSfxButton.GetComponent<Image>();
                if (img != null && (img.sprite == null || img.sprite.name.Contains("UISprite")))
                {
                    img.sprite = btnGold;
                    img.type = Image.Type.Sliced;
                    img.color = Color.white;
                }
            }

            if (languageToggleButton != null && btnStone != null)
            {
                var img = languageToggleButton.GetComponent<Image>();
                if (img != null && (img.sprite == null || img.sprite.name.Contains("UISprite")))
                {
                    img.sprite = btnStone;
                    img.type = Image.Type.Sliced;
                    img.color = Color.white;
                }
            }
        }

        private void OnEnable()
        {
            RefreshAllSettings();
            AudioManager.OnVolumeChanged += HandleVolumeChanged;
            DevicePerformanceManager.OnFpsLimitChanged += HandleFpsChanged;
            DevicePerformanceManager.OnBatterySaverToggled += HandleBatterySaverChanged;
            DevicePerformanceManager.OnHapticsToggled += HandleHapticsChanged;
            FantasyDefense.Localization.CitadelLocalizationManager.OnLanguageChanged += HandleLanguageChanged;
        }

        private void OnDisable()
        {
            AudioManager.OnVolumeChanged -= HandleVolumeChanged;
            DevicePerformanceManager.OnFpsLimitChanged -= HandleFpsChanged;
            DevicePerformanceManager.OnBatterySaverToggled -= HandleBatterySaverChanged;
            DevicePerformanceManager.OnHapticsToggled -= HandleHapticsChanged;
            FantasyDefense.Localization.CitadelLocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
        }

        private void HandleLanguageChanged(FantasyDefense.Localization.AppLanguage lang)
        {
            RefreshAllSettings();
        }

        private void InitializeActionButtons()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(CloseSettings);

            if (testSfxButton != null)
                testSfxButton.onClick.AddListener(OnTestSfxClicked);

            if (masterSlider != null)
                masterSlider.onValueChanged.AddListener(OnMasterSliderChanged);

            if (bgmSlider != null)
                bgmSlider.onValueChanged.AddListener(OnBgmSliderChanged);

            if (sfxSlider != null)
                sfxSlider.onValueChanged.AddListener(OnSfxSliderChanged);

            if (bgmMuteToggle != null)
                bgmMuteToggle.onValueChanged.AddListener(OnBgmMuteToggleChanged);

            if (sfxMuteToggle != null)
                sfxMuteToggle.onValueChanged.AddListener(OnSfxMuteToggleChanged);

            if (fps30Button != null)
                fps30Button.onClick.AddListener(() => SetFps(30));

            if (fps60Button != null)
                fps60Button.onClick.AddListener(() => SetFps(60));

            if (batterySaverToggle != null)
                batterySaverToggle.onValueChanged.AddListener(OnBatterySaverToggleChanged);

            if (hapticFeedbackToggle != null)
                hapticFeedbackToggle.onValueChanged.AddListener(OnHapticToggleChanged);

            if (languageToggleButton != null)
                languageToggleButton.onClick.AddListener(OnLanguageToggleClicked);
        }

        public void OpenSettings()
        {
            transform.SetAsLastSibling();
            if (settingsPanel != null)
            {
                settingsPanel.transform.SetAsLastSibling();
                settingsPanel.SetActive(true);
            }
            else
                gameObject.SetActive(true);

            CanvasGroup cg = GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }

            RefreshAllSettings();

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.ButtonClick);
            }
        }

        public void CloseSettings()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(false);
            else
                gameObject.SetActive(false);

            CanvasGroup cg = GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.blocksRaycasts = false;
            }

            if (FantasyDefense.Core.GameFlowManager.Instance != null && FantasyDefense.Core.GameFlowManager.Instance.CurrentActivePanel == FantasyDefense.Core.GamePanelType.AudioSettings)
            {
                FantasyDefense.Core.GameFlowManager.Instance.CloseCurrentPanel();
            }

            // Simpan preferensi pemain ke Cloud & Local Save
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.SaveGame();
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.ButtonClick);
            }
        }

        public void OpenView() => OpenSettings();
        public void CloseView() => CloseSettings();

        public void RefreshAllSettings()
        {
            RefreshLocalization();
            RefreshAudioUI();
            RefreshPerformanceUI();
        }

        private void RefreshLocalization()
        {
            // Update Header Title
            var headerTitle = transform.Find("HeaderBanner/TitleText")?.GetComponent<Text>();
            if (headerTitle != null)
            {
                headerTitle.text = FantasyDefense.Localization.CitadelLocalizationManager.Text(
                    "PENGATURAN AUDIO & SISTEM",
                    "AUDIO & SYSTEM SETTINGS");
            }

            // Update FPS Buttons
            if (fps30Button != null)
            {
                var txt = fps30Button.GetComponentInChildren<Text>();
                if (txt != null) txt.text = "30 FPS";
            }

            if (fps60Button != null)
            {
                var txt = fps60Button.GetComponentInChildren<Text>();
                if (txt != null) txt.text = FantasyDefense.Localization.CitadelLocalizationManager.Text("60 FPS (Halus)", "60 FPS (Smooth)");
            }

            // Update Toggles
            if (batterySaverToggle != null)
            {
                var txt = batterySaverToggle.GetComponentInChildren<Text>();
                if (txt != null) txt.text = FantasyDefense.Localization.CitadelLocalizationManager.Text("Mode Hemat Baterai", "Battery Saver Mode");
            }

            if (hapticFeedbackToggle != null)
            {
                var txt = hapticFeedbackToggle.GetComponentInChildren<Text>();
                if (txt != null) txt.text = FantasyDefense.Localization.CitadelLocalizationManager.Text("Getaran Haptic Ponsel", "Haptic Vibration");
            }

            if (bgmMuteToggle != null)
            {
                var txt = bgmMuteToggle.GetComponentInChildren<Text>();
                if (txt != null) txt.text = FantasyDefense.Localization.CitadelLocalizationManager.Text("Mute", "Mute");
            }

            if (sfxMuteToggle != null)
            {
                var txt = sfxMuteToggle.GetComponentInChildren<Text>();
                if (txt != null) txt.text = FantasyDefense.Localization.CitadelLocalizationManager.Text("Mute", "Mute");
            }

            // Update Test SFX Button
            if (testSfxButton != null)
            {
                var txt = testSfxButton.GetComponentInChildren<Text>();
                if (txt != null) txt.text = FantasyDefense.Localization.CitadelLocalizationManager.Text("🔊 UJI COBA SUARA EFEK (TEST SFX)", "🔊 TEST SOUND EFFECTS (SFX)");
            }

            // Update Account Status Text
            var setContent = transform.Find("ContentContainer") ?? transform;
            var accTxt = setContent.Find("AccountStatusText")?.GetComponent<Text>();
            if (accTxt != null)
            {
                accTxt.text = FantasyDefense.Localization.CitadelLocalizationManager.Text(
                    "☁️ STATUS AKUN: MODE TAMU (GUEST) • AUTO-SAVE AKTIF\n<size=15><color=#AAAAAA>Progres tersimpan otomatis di perangkat secara aman. Hapus Cache / Data Aplikasi di Pengaturan Ponsel jika ingin memulai dari awal.</color></size>",
                    "☁️ ACCOUNT STATUS: GUEST MODE • AUTO-SAVE ACTIVE\n<size=15><color=#AAAAAA>Progress is safely auto-saved to device storage. Clear App Data in Phone Settings to reset.</color></size>");
            }

            RefreshLanguageDisplay();
        }

        private void OnLanguageToggleClicked()
        {
            if (FantasyDefense.Localization.CitadelLocalizationManager.Instance != null)
            {
                FantasyDefense.Localization.CitadelLocalizationManager.Instance.ToggleLanguage();
            }
            RefreshAllSettings();
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.ButtonClick);
            }
        }

        private void RefreshLanguageDisplay()
        {
            if (languageStatusText != null)
            {
                bool isId = FantasyDefense.Localization.CitadelLocalizationManager.Instance == null ||
                            FantasyDefense.Localization.CitadelLocalizationManager.Instance.CurrentLanguage == FantasyDefense.Localization.AppLanguage.Indonesian;
                languageStatusText.text = isId ? "Bahasa: [ ID ] (Indonesia)" : "Language: [ EN ] (English)";
            }

            if (languageToggleButton != null)
            {
                var btnTxt = languageToggleButton.GetComponentInChildren<Text>();
                if (btnTxt != null)
                {
                    btnTxt.text = FantasyDefense.Localization.CitadelLocalizationManager.Text(
                        "🌐 GANTI BAHASA (SWITCH)",
                        "🌐 SWITCH LANGUAGE (ID/EN)"
                    );
                }
            }
        }

        private void RefreshAudioUI()
        {
            if (AudioManager.Instance == null) return;

            float master = AudioManager.Instance.MasterVolume;
            float bgm = AudioManager.Instance.BgmVolume;
            float sfx = AudioManager.Instance.SfxVolume;

            if (masterSlider != null) masterSlider.value = master;
            if (bgmSlider != null) bgmSlider.value = bgm;
            if (sfxSlider != null) sfxSlider.value = sfx;

            if (bgmMuteToggle != null) bgmMuteToggle.isOn = AudioManager.Instance.IsBgmMuted;
            if (sfxMuteToggle != null) sfxMuteToggle.isOn = AudioManager.Instance.IsSfxMuted;

            UpdatePercentLabels(master, bgm, sfx);
        }

        private void RefreshPerformanceUI()
        {
            if (DevicePerformanceManager.Instance == null) return;

            int targetFps = DevicePerformanceManager.Instance.CurrentTargetFps;
            if (currentFpsText != null)
            {
                string targetLabel = FantasyDefense.Localization.CitadelLocalizationManager.Text("Target", "Target");
                currentFpsText.text = $"{targetLabel}: {targetFps} FPS";
            }

            if (batterySaverToggle != null)
            {
                batterySaverToggle.isOn = DevicePerformanceManager.Instance.IsBatterySaverEnabled;
            }

            if (hapticFeedbackToggle != null)
            {
                hapticFeedbackToggle.isOn = DevicePerformanceManager.Instance.IsHapticsEnabled;
            }

            UpdateFpsButtonsVisual(targetFps);
        }

        private void UpdateFpsButtonsVisual(int activeFps)
        {
            Sprite tabActive = Resources.Load<Sprite>("Sprites/UI/UI_Tab_Hero_Active");
            Sprite tabInactive = Resources.Load<Sprite>("Sprites/UI/UI_Tab_Hero_Inactive");

            if (fps30Button != null)
            {
                var img = fps30Button.GetComponent<Image>();
                if (img != null)
                {
                    if (activeFps == 30)
                    {
                        if (tabActive != null) { img.sprite = tabActive; img.type = Image.Type.Sliced; }
                        img.color = Color.white;
                    }
                    else
                    {
                        if (tabInactive != null) { img.sprite = tabInactive; img.type = Image.Type.Sliced; }
                        img.color = new Color(0.7f, 0.7f, 0.7f, 0.9f);
                    }
                }
            }

            if (fps60Button != null)
            {
                var img = fps60Button.GetComponent<Image>();
                if (img != null)
                {
                    if (activeFps == 60)
                    {
                        if (tabActive != null) { img.sprite = tabActive; img.type = Image.Type.Sliced; }
                        img.color = Color.white;
                    }
                    else
                    {
                        if (tabInactive != null) { img.sprite = tabInactive; img.type = Image.Type.Sliced; }
                        img.color = new Color(0.7f, 0.7f, 0.7f, 0.9f);
                    }
                }
            }
        }

        private void SetFps(int fps)
        {
            if (DevicePerformanceManager.Instance != null)
            {
                DevicePerformanceManager.Instance.SetTargetFps(fps);
            }
        }

        private void OnBatterySaverToggleChanged(bool isEnabled)
        {
            if (DevicePerformanceManager.Instance != null)
            {
                DevicePerformanceManager.Instance.ToggleBatterySaver(isEnabled);
            }
        }

        private void OnHapticToggleChanged(bool isEnabled)
        {
            if (DevicePerformanceManager.Instance != null)
            {
                DevicePerformanceManager.Instance.ToggleHaptics(isEnabled);
            }
        }

        private void UpdatePercentLabels(float master, float bgm, float sfx)
        {
            Font defaultFont = Resources.Load<Font>("Fonts/LilitaOne-Regular");
            if (defaultFont != null)
            {
                if (masterPercentText != null) masterPercentText.font = defaultFont;
                if (bgmPercentText != null) bgmPercentText.font = defaultFont;
                if (sfxPercentText != null) sfxPercentText.font = defaultFont;
                if (currentFpsText != null) currentFpsText.font = defaultFont;
                if (languageStatusText != null) languageStatusText.font = defaultFont;
            }

            if (masterPercentText != null)
            {
                string label = FantasyDefense.Localization.CitadelLocalizationManager.Text("MASTER", "MASTER");
                masterPercentText.text = $"{label}: {Mathf.RoundToInt(master * 100f)}%";
            }

            if (bgmPercentText != null)
            {
                string label = FantasyDefense.Localization.CitadelLocalizationManager.Text("MUSIK BGM", "BGM MUSIC");
                bgmPercentText.text = $"{label}: {Mathf.RoundToInt(bgm * 100f)}%";
            }

            if (sfxPercentText != null)
            {
                string label = FantasyDefense.Localization.CitadelLocalizationManager.Text("EFEK SFX", "SFX AUDIO");
                sfxPercentText.text = $"{label}: {Mathf.RoundToInt(sfx * 100f)}%";
            }
        }

        private void OnMasterSliderChanged(float value)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMasterVolume(value);
            }
        }

        private void OnBgmSliderChanged(float value)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetBgmVolume(value);
            }
        }

        private void OnSfxSliderChanged(float value)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetSfxVolume(value);
            }
        }

        private void OnBgmMuteToggleChanged(bool isMuted)
        {
            if (AudioManager.Instance != null && AudioManager.Instance.IsBgmMuted != isMuted)
            {
                AudioManager.Instance.ToggleMuteBgm();
            }
        }

        private void OnSfxMuteToggleChanged(bool isMuted)
        {
            if (AudioManager.Instance != null && AudioManager.Instance.IsSfxMuted != isMuted)
            {
                AudioManager.Instance.ToggleMuteSfx();
            }
        }

        private void OnTestSfxClicked()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.MergeSuccess);
            }

            if (DevicePerformanceManager.Instance != null)
            {
                DevicePerformanceManager.Instance.TriggerHaptic(HapticFeedbackType.Success);
            }
        }

        private void HandleVolumeChanged(float master, float bgm, float sfx)
        {
            UpdatePercentLabels(master, bgm, sfx);
        }

        private void HandleFpsChanged(int fps)
        {
            if (currentFpsText != null) currentFpsText.text = $"FPS: {fps}";
        }

        private void HandleBatterySaverChanged(bool enabled)
        {
            if (batterySaverToggle != null) batterySaverToggle.isOn = enabled;
        }

        private void HandleHapticsChanged(bool enabled)
        {
            if (hapticFeedbackToggle != null) hapticFeedbackToggle.isOn = enabled;
        }
    }
}
