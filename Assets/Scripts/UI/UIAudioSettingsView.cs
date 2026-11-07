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
        [SerializeField] private Toggle masterMuteToggle;
        [SerializeField] private Toggle bgmMuteToggle;
        [SerializeField] private Toggle sfxMuteToggle;

        [Header("Audio Mute Icons")]
        private Sprite iconSpeaker;
        private Sprite iconMute;

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
            var sliderTrack = Resources.Load<Sprite>("Sprites/UI/UI_Slider_Track_Stone");
            var sliderFill = Resources.Load<Sprite>("Sprites/UI/UI_Slider_Fill_Cyan");
            var sliderHandle = Resources.Load<Sprite>("Sprites/UI/UI_Slider_Handle_RuneGem");
            var toggleBase = Resources.Load<Sprite>("Sprites/UI/UI_Toggle_Base_Slot");
            var toggleKnob = Resources.Load<Sprite>("Sprites/UI/UI_Toggle_Knob_Switch") ?? sliderHandle;

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

            iconSpeaker = Resources.Load<Sprite>("Sprites/UI/UI_Icon_Audio_Speaker");
            iconMute = Resources.Load<Sprite>("Sprites/UI/UI_Icon_Audio_Mute");

            // Apply tactile 2.5D styling to volume sliders
            ApplySliderStyling(masterSlider, sliderTrack, sliderFill, sliderHandle);
            ApplySliderStyling(bgmSlider, sliderTrack, sliderFill, sliderHandle);
            ApplySliderStyling(sfxSlider, sliderTrack, sliderFill, sliderHandle);

            // Apply tactile 2.5D styling to mute buttons & toggles
            ApplyMuteToggleStyling(masterMuteToggle, masterSlider);
            ApplyMuteToggleStyling(bgmMuteToggle, bgmSlider);
            ApplyMuteToggleStyling(sfxMuteToggle, sfxSlider);
            ApplyToggleStyling(batterySaverToggle, toggleBase, toggleKnob);
            ApplyToggleStyling(hapticFeedbackToggle, toggleBase, toggleKnob);
        }

        private void ApplySliderStyling(Slider slider, Sprite track, Sprite fill, Sprite handle)
        {
            if (slider == null) return;
            if (track != null)
            {
                var bg = slider.transform.Find("Background");
                if (bg != null)
                {
                    var bgImg = bg.GetComponent<Image>();
                    if (bgImg != null)
                    {
                        bgImg.sprite = track;
                        bgImg.type = Image.Type.Sliced;
                        bgImg.color = Color.white;
                    }
                }
            }
            if (fill != null && slider.fillRect != null)
            {
                var fillImg = slider.fillRect.GetComponent<Image>();
                if (fillImg != null)
                {
                    fillImg.sprite = fill;
                    fillImg.type = Image.Type.Sliced;
                    fillImg.color = Color.white;
                }
            }
            if (handle != null && slider.handleRect != null)
            {
                var hImg = slider.handleRect.GetComponent<Image>();
                if (hImg != null)
                {
                    hImg.sprite = handle;
                    hImg.type = Image.Type.Simple;
                    hImg.preserveAspect = true;
                    hImg.color = Color.white;
                }

                if (slider.handleRect.GetComponent<UISliderHandleJuice>() == null)
                {
                    slider.handleRect.gameObject.AddComponent<UISliderHandleJuice>();
                }
            }
        }

        private void ApplyMuteToggleStyling(Toggle toggle, Slider associatedSlider)
        {
            if (toggle == null) return;

            // Bersihkan teks raw "Mute" agar tidak terjadi overlapping visual pada tombol icon
            var lbl = toggle.GetComponentInChildren<Text>();
            if (lbl != null) lbl.text = "";

            var checkTransform = toggle.transform.Find("Background/Checkmark") ?? toggle.transform.Find("Checkmark");
            if (checkTransform != null)
            {
                var checkImg = checkTransform.GetComponent<Image>();
                if (checkImg != null)
                {
                    checkImg.type = Image.Type.Simple;
                    checkImg.preserveAspect = true;
                    if (iconMute != null && iconSpeaker != null)
                    {
                        checkImg.sprite = toggle.isOn ? iconMute : iconSpeaker;
                    }
                    checkImg.color = Color.white;
                }
            }

            if (associatedSlider != null && associatedSlider.fillRect != null)
            {
                var fillImg = associatedSlider.fillRect.GetComponent<Image>();
                if (fillImg != null)
                {
                    fillImg.color = toggle.isOn ? new Color(0.35f, 0.45f, 0.55f, 0.40f) : Color.white;
                }
            }
        }

        private void UpdateMuteVisuals()
        {
            UpdateMuteToggleState(masterMuteToggle, masterSlider, AudioManager.Instance != null && AudioManager.Instance.IsMasterMuted);
            UpdateMuteToggleState(bgmMuteToggle, bgmSlider, AudioManager.Instance != null && (AudioManager.Instance.IsBgmMuted || AudioManager.Instance.IsMasterMuted));
            UpdateMuteToggleState(sfxMuteToggle, sfxSlider, AudioManager.Instance != null && (AudioManager.Instance.IsSfxMuted || AudioManager.Instance.IsMasterMuted));
        }

        private void UpdateMuteToggleState(Toggle toggle, Slider slider, bool isMuted)
        {
            if (toggle != null)
            {
                var checkTransform = toggle.transform.Find("Background/Checkmark") ?? toggle.transform.Find("Checkmark");
                if (checkTransform != null)
                {
                    var checkImg = checkTransform.GetComponent<Image>();
                    if (checkImg != null && iconMute != null && iconSpeaker != null)
                    {
                        checkImg.sprite = isMuted ? iconMute : iconSpeaker;
                    }
                }
            }

            if (slider != null && slider.fillRect != null)
            {
                var fillImg = slider.fillRect.GetComponent<Image>();
                if (fillImg != null)
                {
                    fillImg.color = isMuted ? new Color(0.35f, 0.45f, 0.55f, 0.40f) : Color.white;
                }
            }
        }

        private void ApplyToggleStyling(Toggle toggle, Sprite baseSlot, Sprite knob)
        {
            if (toggle == null) return;
            if (baseSlot != null && toggle.targetGraphic is Image bgImg)
            {
                bgImg.sprite = baseSlot;
                bgImg.type = Image.Type.Sliced;
                bgImg.color = Color.white;
            }

            var checkTransform = toggle.transform.Find("Background/Checkmark");
            if (checkTransform != null)
            {
                var checkImg = checkTransform.GetComponent<Image>();
                if (checkImg != null && knob != null)
                {
                    checkImg.sprite = knob;
                    checkImg.type = Image.Type.Simple;
                    checkImg.preserveAspect = true;
                    checkImg.color = Color.white;
                }

                var slidingSwitch = toggle.GetComponent<UISlidingToggleSwitch>();
                if (slidingSwitch == null)
                {
                    slidingSwitch = toggle.gameObject.AddComponent<UISlidingToggleSwitch>();
                }
                if (checkImg != null)
                {
                    slidingSwitch.SetKnob(checkTransform.GetComponent<RectTransform>(), checkImg);
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

            if (masterMuteToggle != null)
                masterMuteToggle.onValueChanged.AddListener(OnMasterMuteToggleChanged);

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

            if (masterMuteToggle != null) masterMuteToggle.isOn = AudioManager.Instance.IsMasterMuted;
            if (bgmMuteToggle != null) bgmMuteToggle.isOn = AudioManager.Instance.IsBgmMuted;
            if (sfxMuteToggle != null) sfxMuteToggle.isOn = AudioManager.Instance.IsSfxMuted;

            UpdateMuteVisuals();
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

        private void OnMasterMuteToggleChanged(bool isMuted)
        {
            if (AudioManager.Instance != null && AudioManager.Instance.IsMasterMuted != isMuted)
            {
                AudioManager.Instance.ToggleMuteMaster();
            }
            UpdateMuteVisuals();
        }

        private void OnBgmMuteToggleChanged(bool isMuted)
        {
            if (AudioManager.Instance != null && AudioManager.Instance.IsBgmMuted != isMuted)
            {
                AudioManager.Instance.ToggleMuteBgm();
            }
            UpdateMuteVisuals();
        }

        private void OnSfxMuteToggleChanged(bool isMuted)
        {
            if (AudioManager.Instance != null && AudioManager.Instance.IsSfxMuted != isMuted)
            {
                AudioManager.Instance.ToggleMuteSfx();
            }
            UpdateMuteVisuals();
        }

        private int _testSfxCounter = 0;

        private void OnTestSfxClicked()
        {
            if (AudioManager.Instance != null)
            {
                SoundType[] testSounds = new SoundType[]
                {
                    SoundType.MergeSuccess,
                    SoundType.GoldCoinSpend,
                    SoundType.ArchetypeMaxChime,
                    SoundType.VictoryFanfare
                };
                SoundType selectedSound = testSounds[_testSfxCounter % testSounds.Length];
                _testSfxCounter++;
                AudioManager.Instance.PlaySound(selectedSound);
            }

            if (DevicePerformanceManager.Instance != null)
            {
                DevicePerformanceManager.Instance.TriggerHaptic(HapticFeedbackType.Success);
            }

            if (testSfxButton != null)
            {
                StartCoroutine(PunchScaleButtonRoutine(testSfxButton.transform));
            }
        }

        private System.Collections.IEnumerator PunchScaleButtonRoutine(Transform target)
        {
            if (target == null) yield break;
            Vector3 orig = Vector3.one;
            float dur = 0.16f;
            float elapsed = 0f;
            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / dur;
                float s = 1f + Mathf.Sin(t * Mathf.PI) * 0.14f;
                target.localScale = orig * s;
                yield return null;
            }
            target.localScale = orig;
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
