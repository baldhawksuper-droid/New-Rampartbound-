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
        }

        private void OnEnable()
        {
            RefreshAllSettings();
            AudioManager.OnVolumeChanged += HandleVolumeChanged;
            DevicePerformanceManager.OnFpsLimitChanged += HandleFpsChanged;
            DevicePerformanceManager.OnBatterySaverToggled += HandleBatterySaverChanged;
            DevicePerformanceManager.OnHapticsToggled += HandleHapticsChanged;
        }

        private void OnDisable()
        {
            AudioManager.OnVolumeChanged -= HandleVolumeChanged;
            DevicePerformanceManager.OnFpsLimitChanged -= HandleFpsChanged;
            DevicePerformanceManager.OnBatterySaverToggled -= HandleBatterySaverChanged;
            DevicePerformanceManager.OnHapticsToggled -= HandleHapticsChanged;
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
            RefreshAudioUI();
            RefreshPerformanceUI();
            RefreshLanguageDisplay();
        }

        private void OnLanguageToggleClicked()
        {
            if (FantasyDefense.Localization.CitadelLocalizationManager.Instance != null)
            {
                FantasyDefense.Localization.CitadelLocalizationManager.Instance.ToggleLanguage();
            }
            RefreshLanguageDisplay();
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
                currentFpsText.text = $"FPS: {targetFps}";
            }

            if (batterySaverToggle != null)
            {
                batterySaverToggle.isOn = DevicePerformanceManager.Instance.IsBatterySaverEnabled;
            }

            if (hapticFeedbackToggle != null)
            {
                hapticFeedbackToggle.isOn = DevicePerformanceManager.Instance.IsHapticsEnabled;
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
            if (masterPercentText != null)
                masterPercentText.text = $"{Mathf.RoundToInt(master * 100f)}%";

            if (bgmPercentText != null)
                bgmPercentText.text = $"{Mathf.RoundToInt(bgm * 100f)}%";

            if (sfxPercentText != null)
                sfxPercentText.text = $"{Mathf.RoundToInt(sfx * 100f)}%";
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
