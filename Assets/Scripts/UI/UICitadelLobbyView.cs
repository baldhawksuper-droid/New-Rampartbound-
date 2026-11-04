using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FantasyDefense.Audio;
using FantasyDefense.Core;
using FantasyDefense.Data;
using FantasyDefense.Metagame;

namespace FantasyDefense.UI
{
    /// <summary>
    /// Master UI Controller untuk Pusat Benteng (Citadel Lobby) berdasarkan Master Spec Bab 4.2:
    /// 1. Top Header Zone: Level Citadel, Kas Emas, Saldo Gems, Meteran Stamina 30/30, & Total Team CP.
    /// 2. Citadel Courtyard Assembly: Formasi 4 slot pahlawan Chibi (Pippin, Cedric, Rose, & Slot ke-4 Wind Ranger / Locked).
    ///    Mendukung interaksi sentuh (bounce juice), bayangan tanah oval, serta kartu lencana info mengambang.
    /// 3. Primary Action Zone: Tombol besar terpusat 3D "⚔️ BERTAHAN (5 ⚡) / BATTLE".
    /// 4. Bottom Navigation Dock: 4 Tab Utama responsif [🗺️ Peta] [🛡️ Hero] [🔮 Gacha] [💎 Toko].
    /// </summary>
    public class UICitadelLobbyView : MonoBehaviour
    {
        public static UICitadelLobbyView Instance { get; private set; }

        [Header("Root Panel")]
        [SerializeField] private GameObject lobbyPanel;

        [Header("Top Header & Currencies")]
        [SerializeField] private Text commanderNameText;
        [SerializeField] private Text citadelLevelText;
        [SerializeField] private Text energyText;
        [SerializeField] private Text goldText;
        [SerializeField] private Text gemsText;
        [SerializeField] private Text teamCpText;
        [SerializeField] private Button settingsButton;

        private int _displayedGold = -1;
        private int _displayedEnergy = -1;
        private int _maxEnergy = 30;
        private int _displayedGems = -1;
        private Coroutine _goldTickerRoutine;
        private Coroutine _energyTickerRoutine;
        private Coroutine _gemsTickerRoutine;

        [Header("Courtyard Hero Assembly (Max 4 Slots)")]
        [SerializeField] private Transform courtyardContainer;
        [SerializeField] private Button cedricCardButton;
        [SerializeField] private Text cedricInfoText;
        [SerializeField] private Button roseCardButton;
        [SerializeField] private Text roseInfoText;
        [SerializeField] private Button rabbitCardButton;
        [SerializeField] private Text rabbitInfoText;
        [SerializeField] private Button windRangerCardButton;
        [SerializeField] private Text windRangerInfoText;

        [Header("Hero Tier Preview Selector")]
        [SerializeField] private GameObject tierSelectorBar;
        [SerializeField] private Button[] tierSelectorButtons;
        [SerializeField] private bool showTierSelector = false; // Simpan preview tier selector; sembunyikan dari lobi sesuai preferensi user
        private int currentPreviewTier = 1;

        [Header("Core Battle Action Buttons")]
        [SerializeField] private Button defendCitadelButton;
        [SerializeField] private Button defenseTurretWorkshopButton;
        [SerializeField] private Button abyssalTrialButton;
        [SerializeField] private Button featurePreviewButton;

        [Header("Bottom Navigation Dock")]
        [SerializeField] private Button navStageMapButton;
        [SerializeField] private Button navHeroesButton;
        [SerializeField] private Button navSummonButton;
        [SerializeField] private Button navForgeButton;
        [SerializeField] private Button navTalentsButton;
        [SerializeField] private Button navStoreButton;
        [SerializeField] private Button navQuestsButton;
        [SerializeField] private Button navGuildButton;
        [SerializeField] private Button navBattlePassButton;
        [SerializeField] private Button navEventArenaButton;
        [SerializeField] private Button navArtifactsButton;

        [Header("Modular Menu Visibility (Responsive Lobby)")]
        [SerializeField] private bool showAbyssalTrial = false;
        [SerializeField] private bool showTalents = false;
        [SerializeField] private bool showQuests = false;
        [SerializeField] private bool showGuild = false;
        [SerializeField] private bool showBattlePass = false;
        [SerializeField] private bool showEventArena = false;
        [SerializeField] private bool showArtifacts = false;
        [SerializeField] private bool showForge = true; // 4-Tab Kanonikal Gambar 2: Citadel, Forge, Heroes, Bag
        [SerializeField] private bool showFeaturePreview = false;

        [Header("Citadel Courtyard Atmosphere & Standee VFX")]
        [SerializeField] private bool enableCourtyardAtmosphere = true;
        [SerializeField] private bool enableStandeeIdleBobbing = true;
        private RectTransform pippinPortraitRt, pippinShadowRt;
        private RectTransform cedricPortraitRt, cedricShadowRt;
        private RectTransform rosePortraitRt, roseShadowRt;
        private RectTransform windPortraitRt, windShadowRt;
        private Image valleyMistOverlay;
        private Image godRaysOverlay;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeButtons();
            InitializeTierSelectorBar();
            InitializeAtmosphereAndBobbing();
            ApplyLilitaOneFonts();
            ApplyResponsiveLayout();
        }

        public void ApplyResponsiveLayout()
        {
            ApplyLilitaOneFonts();

            if (abyssalTrialButton != null)
                abyssalTrialButton.gameObject.SetActive(showAbyssalTrial);

            if (featurePreviewButton != null)
                featurePreviewButton.gameObject.SetActive(showFeaturePreview);

            if (navTalentsButton != null) navTalentsButton.gameObject.SetActive(showTalents);
            if (navQuestsButton != null) navQuestsButton.gameObject.SetActive(showQuests);
            if (navGuildButton != null) navGuildButton.gameObject.SetActive(showGuild);
            if (navBattlePassButton != null) navBattlePassButton.gameObject.SetActive(showBattlePass);
            if (navEventArenaButton != null) navEventArenaButton.gameObject.SetActive(showEventArena);
            if (navArtifactsButton != null) navArtifactsButton.gameObject.SetActive(showArtifacts);
            if (navForgeButton != null) navForgeButton.gameObject.SetActive(showForge);
            if (navStageMapButton != null) navStageMapButton.gameObject.SetActive(true);
            if (navHeroesButton != null) navHeroesButton.gameObject.SetActive(true);
            if (navSummonButton != null) navSummonButton.gameObject.SetActive(false); // 4-Tab Dock Kanonikal (Citadel, Forge, Heroes, Bag)
            if (navStoreButton != null) navStoreButton.gameObject.SetActive(true);

            // Jika quick action bar semua disembunyikan, sembunyikan bar container-nya
            if (navTalentsButton != null && navTalentsButton.transform.parent != null)
            {
                var quickBar = navTalentsButton.transform.parent.gameObject;
                bool anyQuickActive = showTalents || showQuests || showGuild || showBattlePass || showEventArena || showArtifacts;
                quickBar.SetActive(anyQuickActive);
            }

            // Formasi Sejajar Teras Batu (Tinggi Sama, Skala Chibi Seragam, Menapak Penuh di Atas Lantai Batu Pelataran)
            if (rabbitCardButton != null)
            {
                var rt = rabbitCardButton.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.02f, 0.04f);
                    rt.anchorMax = new Vector2(0.25f, 0.72f);
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                }
            }
            if (cedricCardButton != null)
            {
                var rt = cedricCardButton.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.26f, 0.04f);
                    rt.anchorMax = new Vector2(0.49f, 0.72f);
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                }
            }
            if (roseCardButton != null)
            {
                var rt = roseCardButton.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.51f, 0.04f);
                    rt.anchorMax = new Vector2(0.74f, 0.72f);
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                }
            }
            if (windRangerCardButton != null)
            {
                windRangerCardButton.gameObject.SetActive(true);
                var rt = windRangerCardButton.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.75f, 0.04f);
                    rt.anchorMax = new Vector2(0.98f, 0.72f);
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                }
            }

            // Sembunyikan Tier Preview Selector Bar sesuai preferensi user (disimpan di kode tapi tidak tampil)
            if (tierSelectorBar != null)
            {
                tierSelectorBar.SetActive(showTierSelector);
            }

            // Redesign Dock Navigasi Bawah Gambar 2: Kompak (~0.082f screen), tembus pandang lembut (navy glass alpha 0.78), dan rel emas atas
            if (navStageMapButton != null && navStageMapButton.transform.parent != null)
            {
                var dock = navStageMapButton.transform.parent.gameObject;
                var dockRt = dock.GetComponent<RectTransform>();
                if (dockRt != null)
                {
                    dockRt.anchorMin = new Vector2(0f, 0f);
                    dockRt.anchorMax = new Vector2(1f, 0.082f);
                    dockRt.offsetMin = Vector2.zero;
                    dockRt.offsetMax = Vector2.zero;
                }

                // Background sebagian tembus (navy glass)
                var dockImg = dock.GetComponent<Image>();
                if (dockImg != null)
                {
                    dockImg.color = new Color(0.04f, 0.08f, 0.16f, 0.78f);
                }

                // Golden top rail (garis emas halus di sisi atas dock)
                var topRailTr = dock.transform.Find("GoldenTopRail");
                if (topRailTr == null)
                {
                    var railObj = new GameObject("GoldenTopRail", typeof(RectTransform), typeof(Image));
                    railObj.transform.SetParent(dock.transform, false);
                    var railRt = railObj.GetComponent<RectTransform>();
                    railRt.anchorMin = new Vector2(0f, 1f);
                    railRt.anchorMax = new Vector2(1f, 1f);
                    railRt.sizeDelta = new Vector2(0f, 2.5f);
                    railRt.anchoredPosition = new Vector2(0f, -1.25f);
                    var railImg = railObj.GetComponent<Image>();
                    railImg.color = new Color(0.92f, 0.76f, 0.28f, 0.85f);
                    railImg.raycastTarget = false;
                }

                var hlg = dock.GetComponent<HorizontalLayoutGroup>();
                if (hlg == null)
                {
                    hlg = dock.AddComponent<HorizontalLayoutGroup>();
                }
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.childControlWidth = true;
                hlg.childControlHeight = true;
                hlg.childForceExpandWidth = true;
                hlg.childForceExpandHeight = true;
                hlg.spacing = 6f;
                hlg.padding = new RectOffset(8, 8, 4, 4);
                if (dockRt != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(dockRt);
                }
            }

            // Anti-Menumpuk: Posisi dinamis tombol Defend Citadel (Crimson 3D Pill) melayang di atas dock kompak
            if (defendCitadelButton != null)
            {
                var rt = defendCitadelButton.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.18f, 0.096f);
                    rt.anchorMax = new Vector2(0.82f, 0.165f);
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                }
            }
        }

        private void Update()
        {
            // Animasi berdenyut halus (breathe animation) pada tombol Battle Crimson agar memandu mata pemain (skala 1.00f hingga 1.03f)
            if (defendCitadelButton != null && defendCitadelButton.gameObject.activeInHierarchy)
            {
                float pulse = 1.0f + (Mathf.Sin(Time.unscaledTime * 3.0f) * 0.5f + 0.5f) * 0.03f;
                defendCitadelButton.transform.localScale = new Vector3(pulse, pulse, 1f);
            }

            UpdateAtmosphereAndStandeeBobbing();
        }

        private void InitializeAtmosphereAndBobbing()
        {
            if (rabbitCardButton != null)
            {
                pippinPortraitRt = rabbitCardButton.transform.Find("HeroPortrait")?.GetComponent<RectTransform>();
                pippinShadowRt = rabbitCardButton.transform.Find("GroundShadow")?.GetComponent<RectTransform>();
            }
            if (cedricCardButton != null)
            {
                cedricPortraitRt = cedricCardButton.transform.Find("HeroPortrait")?.GetComponent<RectTransform>();
                cedricShadowRt = cedricCardButton.transform.Find("GroundShadow")?.GetComponent<RectTransform>();
            }
            if (roseCardButton != null)
            {
                rosePortraitRt = roseCardButton.transform.Find("HeroPortrait")?.GetComponent<RectTransform>();
                roseShadowRt = roseCardButton.transform.Find("GroundShadow")?.GetComponent<RectTransform>();
            }
            if (windRangerCardButton != null)
            {
                windPortraitRt = windRangerCardButton.transform.Find("HeroPortrait")?.GetComponent<RectTransform>();
                windShadowRt = windRangerCardButton.transform.Find("GroundShadow")?.GetComponent<RectTransform>();
            }

            // Inisialisasi Lapisan Atmosfer Lembah & God Rays jika belum ada
            if (enableCourtyardAtmosphere && valleyMistOverlay == null)
            {
                Transform parentTr = courtyardContainer != null ? courtyardContainer.parent : transform;
                if (parentTr != null)
                {
                    var existingAtmosphere = parentTr.Find("LobbyAtmosphereLayer");
                    GameObject atmoObj;
                    if (existingAtmosphere != null)
                    {
                        atmoObj = existingAtmosphere.gameObject;
                    }
                    else
                    {
                        atmoObj = new GameObject("LobbyAtmosphereLayer", typeof(RectTransform));
                        atmoObj.transform.SetParent(parentTr, false);
                        var rt = atmoObj.GetComponent<RectTransform>();
                        rt.anchorMin = Vector2.zero;
                        rt.anchorMax = Vector2.one;
                        rt.offsetMin = Vector2.zero;
                        rt.offsetMax = Vector2.zero;

                        // Tempatkan tepat di belakang wadah pahlawan agar menyatu dengan pemandangan lembah
                        if (courtyardContainer != null)
                        {
                            int courtIndex = courtyardContainer.GetSiblingIndex();
                            atmoObj.transform.SetSiblingIndex(Mathf.Max(0, courtIndex));
                        }
                    }

                    // 1. Kabut Lembah Lembut (Valley Mist)
                    var mistTr = atmoObj.transform.Find("ValleyMist");
                    if (mistTr == null)
                    {
                        var mistObj = new GameObject("ValleyMist", typeof(RectTransform), typeof(Image));
                        mistObj.transform.SetParent(atmoObj.transform, false);
                        var mRt = mistObj.GetComponent<RectTransform>();
                        mRt.anchorMin = new Vector2(0f, 0.45f);
                        mRt.anchorMax = new Vector2(1f, 0.68f);
                        mRt.offsetMin = Vector2.zero;
                        mRt.offsetMax = Vector2.zero;
                        valleyMistOverlay = mistObj.GetComponent<Image>();
                        valleyMistOverlay.color = new Color(0.85f, 0.92f, 1.0f, 0.15f);
                        valleyMistOverlay.raycastTarget = false;
                    }
                    else
                    {
                        valleyMistOverlay = mistTr.GetComponent<Image>();
                    }

                    // 2. Berkas Sinar Mentari Lembut (God Rays Shimmer)
                    var rayTr = atmoObj.transform.Find("GodRays");
                    if (rayTr == null)
                    {
                        var rayObj = new GameObject("GodRays", typeof(RectTransform), typeof(Image));
                        rayObj.transform.SetParent(atmoObj.transform, false);
                        var rRt = rayObj.GetComponent<RectTransform>();
                        rRt.anchorMin = new Vector2(0.40f, 0.50f);
                        rRt.anchorMax = new Vector2(1f, 0.95f);
                        rRt.offsetMin = Vector2.zero;
                        rRt.offsetMax = Vector2.zero;
                        godRaysOverlay = rayObj.GetComponent<Image>();
                        godRaysOverlay.color = new Color(1.0f, 0.95f, 0.80f, 0.08f);
                        godRaysOverlay.raycastTarget = false;
                    }
                    else
                    {
                        godRaysOverlay = rayTr.GetComponent<Image>();
                    }
                }
            }
        }

        private void UpdateAtmosphereAndStandeeBobbing()
        {
            float t = Time.unscaledTime;

            // 1. Shimmer kabut lembah & god rays
            if (valleyMistOverlay != null && valleyMistOverlay.gameObject.activeInHierarchy)
            {
                float mistAlpha = 0.12f + (Mathf.Sin(t * 0.8f) * 0.04f);
                valleyMistOverlay.color = new Color(0.85f, 0.92f, 1.0f, mistAlpha);
            }
            if (godRaysOverlay != null && godRaysOverlay.gameObject.activeInHierarchy)
            {
                float rayAlpha = 0.07f + (Mathf.Sin(t * 0.6f + 1.2f) * 0.03f);
                godRaysOverlay.color = new Color(1.0f, 0.95f, 0.80f, rayAlpha);
            }

            // 2. Subtle Idle Breathing Bobbing pada Standee Hero di Teras
            if (enableStandeeIdleBobbing)
            {
                // Pippin (Mage lembut mengambang)
                if (pippinPortraitRt != null)
                {
                    float yPippin = Mathf.Sin(t * 2.2f) * 3.5f;
                    float sPippin = 1.0f + (Mathf.Sin(t * 2.2f) * 0.015f);
                    pippinPortraitRt.anchoredPosition = new Vector2(0f, yPippin);
                    pippinPortraitRt.localScale = new Vector3(1f, sPippin, 1f);
                    if (pippinShadowRt != null)
                    {
                        float shadowScale = 1.0f - (yPippin * 0.012f);
                        pippinShadowRt.localScale = new Vector3(shadowScale, shadowScale, 1f);
                    }
                }

                // Cedric (Ksatria tegap menapak)
                if (cedricPortraitRt != null)
                {
                    float yCedric = Mathf.Sin(t * 2.0f + 0.85f) * 2.8f;
                    float sCedric = 1.0f + (Mathf.Sin(t * 2.0f + 0.85f) * 0.012f);
                    cedricPortraitRt.anchoredPosition = new Vector2(0f, yCedric);
                    cedricPortraitRt.localScale = new Vector3(1f, sCedric, 1f);
                    if (cedricShadowRt != null)
                    {
                        float shadowScale = 1.0f - (yCedric * 0.012f);
                        cedricShadowRt.localScale = new Vector3(shadowScale, shadowScale, 1f);
                    }
                }

                // Rose (Paladin tangguh berbaju zirah)
                if (rosePortraitRt != null)
                {
                    float yRose = Mathf.Sin(t * 1.9f + 1.70f) * 2.2f;
                    float sRose = 1.0f + (Mathf.Sin(t * 1.9f + 1.70f) * 0.010f);
                    rosePortraitRt.anchoredPosition = new Vector2(0f, yRose);
                    rosePortraitRt.localScale = new Vector3(1f, sRose, 1f);
                    if (roseShadowRt != null)
                    {
                        float shadowScale = 1.0f - (yRose * 0.010f);
                        roseShadowRt.localScale = new Vector3(shadowScale, shadowScale, 1f);
                    }
                }
            }
        }

        private void PlayLobbyAtmosphericAudio()
        {
            if (FantasyDefense.Audio.AudioManager.Instance != null)
            {
                FantasyDefense.Audio.AudioManager.Instance.PlayBGM(FantasyDefense.Audio.BgmTrack.LobbyTheme);
            }
        }

        private void OnEnable()
        {
            CitadelLobbyManager.OnEnergyChanged += HandleEnergyChanged;
            CitadelLobbyManager.OnGoldChanged += HandleGoldChanged;
            CitadelLobbyManager.OnAccountGemsChanged += HandleGemsChanged;
            CitadelLobbyManager.OnTeamCPUpdated += HandleTeamCpUpdated;
            CitadelLobbyManager.OnCitadelLevelChanged += HandleLevelChanged;
            CitadelLobbyManager.OnHeroUpgraded += HandleHeroUpgraded;
            CitadelLobbyManager.OnHeroStarPromoted += HandleHeroStarPromoted;
            CitadelLobbyManager.OnFormationChanged += HandleFormationChanged;
            CitadelLobbyManager.OnTurretConfigChanged += RefreshTurretWorkshopDisplay;
            CitadelSummonManager.OnGachaPullsCompleted += HandleGachaPullsCompleted;
            FantasyDefense.Localization.CitadelLocalizationManager.OnLanguageChanged += HandleLanguageChanged;
            GameFlowManager.OnPanelChanged += HandlePanelChanged;

            ApplyResponsiveLayout();
            RefreshLocalization();
            RefreshDisplays();
            PlayLobbyAtmosphericAudio();
        }

        private void OnDisable()
        {
            if (_goldTickerRoutine != null) { StopCoroutine(_goldTickerRoutine); _goldTickerRoutine = null; }
            if (_energyTickerRoutine != null) { StopCoroutine(_energyTickerRoutine); _energyTickerRoutine = null; }
            if (_gemsTickerRoutine != null) { StopCoroutine(_gemsTickerRoutine); _gemsTickerRoutine = null; }

            CitadelLobbyManager.OnEnergyChanged -= HandleEnergyChanged;
            CitadelLobbyManager.OnGoldChanged -= HandleGoldChanged;
            CitadelLobbyManager.OnAccountGemsChanged -= HandleGemsChanged;
            CitadelLobbyManager.OnTeamCPUpdated -= HandleTeamCpUpdated;
            CitadelLobbyManager.OnCitadelLevelChanged -= HandleLevelChanged;
            CitadelLobbyManager.OnHeroUpgraded -= HandleHeroUpgraded;
            CitadelLobbyManager.OnHeroStarPromoted -= HandleHeroStarPromoted;
            CitadelLobbyManager.OnFormationChanged -= HandleFormationChanged;
            CitadelLobbyManager.OnTurretConfigChanged -= RefreshTurretWorkshopDisplay;
            CitadelSummonManager.OnGachaPullsCompleted -= HandleGachaPullsCompleted;
            FantasyDefense.Localization.CitadelLocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
            GameFlowManager.OnPanelChanged -= HandlePanelChanged;
        }

        private void Start()
        {
            EnsureLobbyBackdropVisual();
            ApplyResponsiveLayout();
            RefreshLocalization();
            RefreshDisplays();
        }

        private void EnsureLobbyBackdropVisual()
        {
            var lobbyImg = GetComponent<Image>();
            if (lobbyImg != null)
            {
                if (lobbyImg.sprite == null)
                {
                    lobbyImg.sprite = Resources.Load<Sprite>("Sprites/UI/Citadel_Lobby_Backdrop")
                                   ?? Resources.Load<Sprite>("Art/Sprites/UI/Citadel_Lobby_Backdrop");
                }
                lobbyImg.color = Color.white;
                lobbyImg.type = Image.Type.Simple;
                lobbyImg.preserveAspect = false;
            }
        }

        private void HandleLanguageChanged(FantasyDefense.Localization.AppLanguage lang)
        {
            RefreshLocalization();
            RefreshDisplays();
        }

        public void RefreshLocalization()
        {
            if (commanderNameText != null)
            {
                commanderNameText.text = FantasyDefense.Localization.CitadelLocalizationManager.Get("LOBBY_COMMANDER", "Komandan Benteng", "Citadel Commander");
            }

            if (defendCitadelButton != null)
            {
                var txt = defendCitadelButton.GetComponentInChildren<Text>();
                if (txt != null)
                {
                    txt.text = FantasyDefense.Localization.CitadelLocalizationManager.Get("LOBBY_DEFEND_BTN", "⚔️ BATTLE  (5 ⚡)", "⚔️ BATTLE  (5 ⚡)");
                }
            }

            if (featurePreviewButton != null)
            {
                var txt = featurePreviewButton.GetComponentInChildren<Text>();
                if (txt != null)
                {
                    txt.text = FantasyDefense.Localization.CitadelLocalizationManager.Get("LOBBY_PREVIEW_BTN", "🏛️ PRATINJAU FITUR CITADEL", "🏛️ CITADEL FEATURE PREVIEW");
                }
            }

            if (defenseTurretWorkshopButton != null)
            {
                var txt = defenseTurretWorkshopButton.GetComponentInChildren<Text>();
                if (txt != null)
                {
                    txt.text = FantasyDefense.Localization.CitadelLocalizationManager.Get("LOBBY_TURRET_BTN", "🏰 ARTILERI BENTENG", "🏰 CITADEL TURRETS");
                }
            }

            if (navStageMapButton != null)
            {
                var txt = navStageMapButton.GetComponentInChildren<Text>();
                if (txt != null) txt.text = "<b>< Citadel ></b>";
            }

            if (navForgeButton != null)
            {
                var txt = navForgeButton.GetComponentInChildren<Text>();
                if (txt != null) txt.text = $"<b>{FantasyDefense.Localization.CitadelLocalizationManager.Get("NAV_FORGE", "Forge", "Forge")}</b>";
            }

            if (navHeroesButton != null)
            {
                var txt = navHeroesButton.GetComponentInChildren<Text>();
                if (txt != null) txt.text = $"<b>{FantasyDefense.Localization.CitadelLocalizationManager.Get("NAV_HEROES", "Heroes", "Heroes")}</b>";
            }

            if (navSummonButton != null)
            {
                var txt = navSummonButton.GetComponentInChildren<Text>();
                if (txt != null) txt.text = $"<b>{FantasyDefense.Localization.CitadelLocalizationManager.Get("NAV_SUMMON", "Gacha", "Summon")}</b>";
            }

            if (navStoreButton != null)
            {
                var txt = navStoreButton.GetComponentInChildren<Text>();
                if (txt != null) txt.text = $"<b>{FantasyDefense.Localization.CitadelLocalizationManager.Get("NAV_BAG", "Bag", "Bag")}</b>";
            }
        }

        private void InitializeButtons()
        {
            // Settings
            if (settingsButton != null)
                settingsButton.onClick.AddListener(() => OpenPanel(GamePanelType.AudioSettings));

            // Courtyard Standees (Mendukung Tap Emote Juice & Hero Upgrade)
            if (rabbitCardButton != null)
                rabbitCardButton.onClick.AddListener(() => OnStandeeSlotTapped(rabbitCardButton, 0));
            if (cedricCardButton != null)
                cedricCardButton.onClick.AddListener(() => OnStandeeSlotTapped(cedricCardButton, 1));
            if (roseCardButton != null)
                roseCardButton.onClick.AddListener(() => OnStandeeSlotTapped(roseCardButton, 2));
            if (windRangerCardButton != null)
                windRangerCardButton.onClick.AddListener(() => OnStandeeSlotTapped(windRangerCardButton, 3));

            // Core Actions
            if (defendCitadelButton != null)
                defendCitadelButton.onClick.AddListener(OnDefendCitadelClicked);
            if (abyssalTrialButton != null)
                abyssalTrialButton.onClick.AddListener(() => OpenPanel(GamePanelType.EndlessTrial));
            if (featurePreviewButton != null)
                featurePreviewButton.onClick.AddListener(() => {
                    if (UIFeaturePreviewHubView.Instance != null)
                        UIFeaturePreviewHubView.Instance.OpenView();
                });
            if (defenseTurretWorkshopButton != null)
                defenseTurretWorkshopButton.onClick.AddListener(OpenTurretWorkshopModal);

            // Bottom Dock
            if (navStageMapButton != null)
                navStageMapButton.onClick.AddListener(() => OpenPanel(GamePanelType.StageMap));
            if (navHeroesButton != null)
                navHeroesButton.onClick.AddListener(() => OpenPanel(GamePanelType.HeroUpgrade));
            if (navSummonButton != null)
                navSummonButton.onClick.AddListener(() => OpenPanel(GamePanelType.SummonHall));
            if (navForgeButton != null)
                navForgeButton.onClick.AddListener(() => OpenPanel(GamePanelType.RelicForge));
            if (navTalentsButton != null)
                navTalentsButton.onClick.AddListener(() => OpenPanel(GamePanelType.CitadelTalents));
            if (navStoreButton != null)
                navStoreButton.onClick.AddListener(() => OpenPanel(GamePanelType.Store));
            if (navQuestsButton != null)
                navQuestsButton.onClick.AddListener(() => OpenPanel(GamePanelType.Quests));
            if (navGuildButton != null)
                navGuildButton.onClick.AddListener(() => OpenPanel(GamePanelType.Guild));
            if (navBattlePassButton != null)
                navBattlePassButton.onClick.AddListener(() => OpenPanel(GamePanelType.BattlePass));
            if (navEventArenaButton != null)
                navEventArenaButton.onClick.AddListener(() => OpenPanel(GamePanelType.EventArena));
            if (navArtifactsButton != null)
                navArtifactsButton.onClick.AddListener(() => OpenPanel(GamePanelType.Artifacts));
        }

        public void OpenView()
        {
            if (lobbyPanel != null)
            {
                lobbyPanel.SetActive(true);
            }
            else
            {
                gameObject.SetActive(true);
            }
            RefreshDisplays();
        }

        public void CloseView()
        {
            if (lobbyPanel != null)
            {
                lobbyPanel.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
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

        public void RefreshDisplays()
        {
            ApplyLilitaOneFonts();

            var lobby = CitadelLobbyManager.Instance;
            if (lobby == null) return;

            var data = lobby.AccountData;
            if (data == null) return;

            if (commanderNameText != null)
            {
                commanderNameText.text = "Komandan Benteng";
            }
            if (citadelLevelText != null)
            {
                citadelLevelText.text = $"Citadel Lv.{data.citadelLevel}";
            }
            var lilitaFont = Resources.Load<Font>("Fonts/LilitaOne-Regular");
            if (energyText != null)
            {
                if (lilitaFont != null) energyText.font = lilitaFont;
                energyText.color = new Color(0.35f, 0.85f, 1.0f);
                energyText.fontStyle = FontStyle.Bold;

                _maxEnergy = data.maxEnergy;
                if (_displayedEnergy < 0)
                {
                    _displayedEnergy = data.currentEnergy;
                    energyText.text = $"{_displayedEnergy}/{_maxEnergy}";
                }
                else if (_displayedEnergy != data.currentEnergy && gameObject.activeInHierarchy)
                {
                    if (_energyTickerRoutine != null) StopCoroutine(_energyTickerRoutine);
                    _energyTickerRoutine = StartCoroutine(AnimateRollingEnergy(_displayedEnergy, data.currentEnergy, _maxEnergy));
                }
                else
                {
                    energyText.text = $"{_displayedEnergy}/{_maxEnergy}";
                }
            }
            if (goldText != null)
            {
                if (lilitaFont != null) goldText.font = lilitaFont;
                goldText.color = new Color(1.0f, 0.88f, 0.25f);
                goldText.fontStyle = FontStyle.Bold;

                if (_displayedGold < 0)
                {
                    _displayedGold = data.goldCoins;
                    goldText.text = $"{_displayedGold:N0}";
                }
                else if (_displayedGold != data.goldCoins && gameObject.activeInHierarchy)
                {
                    if (_goldTickerRoutine != null) StopCoroutine(_goldTickerRoutine);
                    _goldTickerRoutine = StartCoroutine(AnimateRollingGold(_displayedGold, data.goldCoins));
                }
                else
                {
                    goldText.text = $"{_displayedGold:N0}";
                }
            }
            if (gemsText != null)
            {
                if (lilitaFont != null) gemsText.font = lilitaFont;
                gemsText.color = new Color(0.40f, 0.95f, 1.0f);
                gemsText.fontStyle = FontStyle.Bold;

                if (_displayedGems < 0)
                {
                    _displayedGems = data.accountGems;
                    gemsText.text = $"{_displayedGems:N0}";
                }
                else if (_displayedGems != data.accountGems && gameObject.activeInHierarchy)
                {
                    if (_gemsTickerRoutine != null) StopCoroutine(_gemsTickerRoutine);
                    _gemsTickerRoutine = StartCoroutine(AnimateRollingGems(_displayedGems, data.accountGems));
                }
                else
                {
                    gemsText.text = $"{_displayedGems:N0}";
                }
            }
            if (teamCpText != null)
            {
                teamCpText.text = $"⚡ TEAM CP: {lobby.TotalTeamCP:N0}";
            }

            // Update Standee Badges Sesuai Formasi Terkini (Slot 0 - 3)
            var formation = lobby.GetDeployedFormation();
            Button[] standeeButtons = { rabbitCardButton, cedricCardButton, roseCardButton, windRangerCardButton };
            Text[] infoTexts = { rabbitInfoText, cedricInfoText, roseInfoText, windRangerInfoText };

            for (int i = 0; i < 4; i++)
            {
                if (i >= standeeButtons.Length || standeeButtons[i] == null) continue;
                UnitType heroType = i < formation.Count ? formation[i] : (UnitType)i;
                var heroProfile = lobby.GetHeroProfile(heroType);

                if (heroProfile != null && heroProfile.isUnlocked)
                {
                    UpdateStandeeDisplay(heroType, infoTexts[i], standeeButtons[i], heroProfile.heroName, heroProfile.level);
                }
                else
                {
                    if (infoTexts[i] != null)
                    {
                        infoTexts[i].text = $"<b><color=#AAAAAA>SLOT {i + 1}</color></b>\n<color=#FFD700>🔒 Gacha</color>";
                        infoTexts[i].fontSize = 13;
                        infoTexts[i].alignment = TextAnchor.MiddleCenter;
                    }
                    if (standeeButtons[i] != null)
                    {
                        var namePlate = standeeButtons[i].transform.Find("HeroNamePlate");
                        if (namePlate != null)
                        {
                            namePlate.gameObject.SetActive(false);
                        }
                        var img = standeeButtons[i].transform.Find("HeroPortrait")?.GetComponent<Image>();
                        if (img != null)
                        {
                            img.color = new Color(0.12f, 0.16f, 0.25f, 0.60f); // Siluet pahlawan berarsir elegan
                            Sprite lockedHeroSprite = GetHeroSpriteForType(heroType, 1);
                            if (lockedHeroSprite != null)
                            {
                                img.sprite = lockedHeroSprite;
                            }
                        }
                    }
                }
            }

            UpdateNavigationTabsVisual(GameFlowManager.Instance != null ? GameFlowManager.Instance.CurrentActivePanel : GamePanelType.None);
        }

        private void HandlePanelChanged(GamePanelType panelType)
        {
            UpdateNavigationTabsVisual(panelType);
        }

        public void UpdateNavigationTabsVisual(GamePanelType activePanel)
        {
            SetTabVisual(navStageMapButton, activePanel == GamePanelType.StageMap);
            SetTabVisual(navForgeButton, activePanel == GamePanelType.RelicForge);
            SetTabVisual(navHeroesButton, activePanel == GamePanelType.HeroUpgrade);
            SetTabVisual(navSummonButton, activePanel == GamePanelType.SummonHall);
            SetTabVisual(navStoreButton, activePanel == GamePanelType.Store);
        }

        private void SetTabVisual(Button btn, bool isActive)
        {
            if (btn == null) return;

            var bgImg = btn.GetComponent<Image>();
            if (bgImg != null)
            {
                // Active: kilau kapsul emas royal tembus Gambar 2; Inactive: tembus pandang bersih (Color.clear) menyatu dengan dock kaca
                bgImg.color = isActive 
                    ? new Color(0.96f, 0.75f, 0.18f, 0.24f) 
                    : Color.clear;
            }

            var podiumTr = btn.transform.Find("ActivePodiumFrame");
            if (podiumTr != null)
            {
                podiumTr.gameObject.SetActive(isActive);
            }

            var outline = btn.GetComponent<Outline>();
            if (outline != null)
            {
                outline.enabled = isActive;
                if (isActive)
                {
                    outline.effectColor = new Color(1.0f, 0.85f, 0.35f, 0.85f);
                    outline.effectDistance = new Vector2(1.5f, -1.5f);
                }
            }

            var iconTr = btn.transform.Find("TabIcon");
            if (iconTr != null)
            {
                var iconRt = iconTr.GetComponent<RectTransform>();
                if (iconRt != null)
                {
                    iconRt.anchorMin = new Vector2(0.24f, 0.40f);
                    iconRt.anchorMax = new Vector2(0.76f, 0.90f);
                    iconRt.offsetMin = Vector2.zero;
                    iconRt.offsetMax = Vector2.zero;
                }
                var iconImg = iconTr.GetComponent<Image>();
                if (iconImg != null)
                {
                    iconImg.preserveAspect = true;
                    iconImg.color = isActive ? Color.white : new Color(0.88f, 0.92f, 0.98f, 0.90f);
                }
                iconTr.localScale = isActive ? new Vector3(1.08f, 1.08f, 1f) : Vector3.one;
            }

            var txt = btn.GetComponentInChildren<Text>();
            if (txt != null)
            {
                txt.fontSize = 12;
                txt.color = isActive 
                    ? new Color(1.0f, 0.95f, 0.60f, 1.0f) 
                    : new Color(0.90f, 0.93f, 0.98f, 0.90f);
                var txtRt = txt.GetComponent<RectTransform>();
                if (txtRt != null)
                {
                    txtRt.anchorMin = new Vector2(0.02f, 0.05f);
                    txtRt.anchorMax = new Vector2(0.98f, 0.38f);
                    txtRt.offsetMin = Vector2.zero;
                    txtRt.offsetMax = Vector2.zero;
                }
            }
        }

        private void UpdateStandeeDisplay(UnitType type, Text infoText, Button btn, string defaultName, int defaultLvl)
        {
            if (infoText == null) return;

            var lobby = CitadelLobbyManager.Instance;
            var profile = lobby != null ? lobby.GetHeroProfile(type) : null;
            string heroName = profile?.heroName ?? defaultName;
            int lvl = profile?.level ?? defaultLvl;
            int cp = profile != null ? profile.CalculateHeroCP() : 1500;

            bool isLocked = profile != null && !profile.isUnlocked;

            int heroTier = currentPreviewTier > 0 ? currentPreviewTier : (profile != null ? Mathf.Clamp(profile.starRank, 1, 5) : 1);
            int displayCp = cp * heroTier;

            // Format floating stat pill kanonikal (Lv.X | CP Y,YYY) dengan dot hijau aktif
            if (isLocked)
            {
                infoText.text = "<b><color=#FFAA00>🔒 TERKUNCI</color></b>";
            }
            else
            {
                infoText.text = $"<color=#00FFAA>●</color> Lv.{lvl} | CP {displayCp:N0}";
            }
            infoText.fontSize = 12;
            infoText.alignment = TextAnchor.MiddleCenter;

            if (btn != null)
            {
                var namePlate = btn.transform.Find("HeroNamePlate")?.GetComponent<Text>();
                if (namePlate != null)
                {
                    namePlate.gameObject.SetActive(!isLocked);
                    namePlate.text = $"<b>{heroName}</b>";
                }

                var img = btn.transform.Find("HeroPortrait")?.GetComponent<Image>();
                if (img != null)
                {
                    if (isLocked)
                    {
                        var lockedSprite = Resources.Load<Sprite>("Sprites/UI/UI_Hero_Locked_Silhouette");
                        if (lockedSprite != null)
                        {
                            img.sprite = lockedSprite;
                            img.color = Color.white;
                        }
                        else
                        {
                            img.color = new Color(0.35f, 0.38f, 0.45f, 0.90f);
                        }
                    }
                    else
                    {
                        img.color = Color.white;
                        Sprite heroSprite = GetHeroSpriteForType(type, heroTier);
                        if (heroSprite != null)
                        {
                            img.sprite = heroSprite;
                        }
                    }
                }

                // Penyelarasan jarak: Rapatkan hero portrait, ground shadow, dan badge pill tanpa celah renggang
                var portraitRt = btn.transform.Find("HeroPortrait")?.GetComponent<RectTransform>();
                if (portraitRt != null)
                {
                    portraitRt.anchorMin = new Vector2(0.02f, 0.12f);
                    portraitRt.anchorMax = new Vector2(0.98f, 0.98f);
                    portraitRt.offsetMin = Vector2.zero;
                    portraitRt.offsetMax = Vector2.zero;
                }
                var shadowRt = btn.transform.Find("GroundShadow")?.GetComponent<RectTransform>();
                if (shadowRt != null)
                {
                    shadowRt.anchorMin = new Vector2(0.08f, 0.10f);
                    shadowRt.anchorMax = new Vector2(0.92f, 0.17f);
                    shadowRt.offsetMin = Vector2.zero;
                    shadowRt.offsetMax = Vector2.zero;

                    var shadowImg = shadowRt.GetComponent<Image>();
                    if (shadowImg != null)
                    {
                        shadowImg.color = new Color(0.03f, 0.06f, 0.12f, 0.70f);
                    }
                }
                var badgeRt = btn.transform.Find("BadgePill")?.GetComponent<RectTransform>();
                if (badgeRt != null)
                {
                    badgeRt.anchorMin = new Vector2(0.02f, 0.00f);
                    badgeRt.anchorMax = new Vector2(0.98f, 0.13f);
                    badgeRt.offsetMin = Vector2.zero;
                    badgeRt.offsetMax = Vector2.zero;

                    var badgeImg = badgeRt.GetComponent<Image>();
                    if (badgeImg != null && badgeImg.sprite == null)
                    {
                        badgeImg.color = isLocked 
                            ? new Color(0.14f, 0.10f, 0.06f, 0.92f) 
                            : new Color(0.06f, 0.09f, 0.16f, 0.94f);
                    }
                }
                if (infoText != null)
                {
                    var textRt = infoText.GetComponent<RectTransform>();
                    if (textRt != null)
                    {
                        textRt.anchorMin = new Vector2(0.02f, 0.00f);
                        textRt.anchorMax = new Vector2(0.98f, 0.13f);
                        textRt.offsetMin = Vector2.zero;
                        textRt.offsetMax = Vector2.zero;
                    }

                    var textShadow = infoText.GetComponent<Shadow>();
                    if (textShadow == null)
                    {
                        textShadow = infoText.gameObject.AddComponent<Shadow>();
                    }
                    textShadow.effectColor = new Color(0f, 0f, 0f, 0.95f);
                    textShadow.effectDistance = new Vector2(1.5f, -1.5f);
                }
            }
        }

        #region Hero Tier Preview Selector

        private void InitializeTierSelectorBar()
        {
            if (tierSelectorBar == null)
            {
                Transform parentTr = courtyardContainer != null ? courtyardContainer : (rabbitCardButton != null ? rabbitCardButton.transform.parent : null);
                if (parentTr != null)
                {
                    var found = parentTr.Find("TierSelectorBar");
                    if (found != null)
                    {
                        tierSelectorBar = found.gameObject;
                    }
                    else
                    {
                        tierSelectorBar = CreateDynamicTierSelectorBar(parentTr);
                    }
                }
            }

            if (tierSelectorBar != null && (tierSelectorButtons == null || tierSelectorButtons.Length == 0))
            {
                tierSelectorButtons = tierSelectorBar.GetComponentsInChildren<Button>(true);
            }

            if (tierSelectorButtons != null)
            {
                for (int i = 0; i < tierSelectorButtons.Length; i++)
                {
                    int tier = i + 1;
                    tierSelectorButtons[i].onClick.RemoveAllListeners();
                    tierSelectorButtons[i].onClick.AddListener(() => SetPreviewTier(tier));
                }
            }

            UpdateTierSelectorVisuals();
        }

        private GameObject CreateDynamicTierSelectorBar(Transform parentTr)
        {
            var barObj = new GameObject("TierSelectorBar", typeof(RectTransform), typeof(Image));
            barObj.transform.SetParent(parentTr, false);

            var rt = barObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.20f, 0.73f);
            rt.anchorMax = new Vector2(0.80f, 0.79f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var bgImg = barObj.GetComponent<Image>();
            bgImg.color = new Color(0.05f, 0.08f, 0.14f, 0.85f);

            var font = Resources.Load<Font>("Fonts/LilitaOne-Regular");

            var buttonsList = new System.Collections.Generic.List<Button>();
            float segmentW = 1.0f / 5.0f;
            for (int i = 0; i < 5; i++)
            {
                int tierNum = i + 1;
                var btnObj = new GameObject($"TierBtn_{tierNum}", typeof(RectTransform), typeof(Image), typeof(Button));
                btnObj.transform.SetParent(barObj.transform, false);

                var btnRt = btnObj.GetComponent<RectTransform>();
                btnRt.anchorMin = new Vector2(i * segmentW + 0.015f, 0.10f);
                btnRt.anchorMax = new Vector2((i + 1) * segmentW - 0.015f, 0.90f);
                btnRt.offsetMin = Vector2.zero;
                btnRt.offsetMax = Vector2.zero;

                var btnImg = btnObj.GetComponent<Image>();
                btnImg.color = (tierNum == currentPreviewTier) ? new Color(0.96f, 0.76f, 0.22f, 1.0f) : new Color(0.10f, 0.15f, 0.24f, 0.88f);

                var textObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
                textObj.transform.SetParent(btnObj.transform, false);
                var textRt = textObj.GetComponent<RectTransform>();
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.offsetMin = Vector2.zero;
                textRt.offsetMax = Vector2.zero;

                var txt = textObj.GetComponent<Text>();
                txt.text = $"T{tierNum}";
                txt.alignment = TextAnchor.MiddleCenter;
                txt.fontSize = 14;
                txt.color = (tierNum == currentPreviewTier) ? new Color(0.12f, 0.08f, 0.02f, 1.0f) : new Color(0.75f, 0.85f, 0.95f, 1.0f);
                if (font != null) txt.font = font;

                buttonsList.Add(btnObj.GetComponent<Button>());
            }
            tierSelectorButtons = buttonsList.ToArray();
            return barObj;
        }

        private void UpdateTierSelectorVisuals()
        {
            if (tierSelectorButtons == null) return;

            for (int i = 0; i < tierSelectorButtons.Length; i++)
            {
                var btn = tierSelectorButtons[i];
                if (btn == null) continue;
                bool isActive = (i + 1) == currentPreviewTier;

                var img = btn.GetComponent<Image>();
                if (img != null)
                {
                    img.color = isActive 
                        ? new Color(0.96f, 0.76f, 0.22f, 1.0f) 
                        : new Color(0.10f, 0.15f, 0.24f, 0.88f);
                }

                var txt = btn.GetComponentInChildren<Text>();
                if (txt != null)
                {
                    txt.color = isActive 
                        ? new Color(0.12f, 0.08f, 0.02f, 1.0f) 
                        : new Color(0.75f, 0.85f, 0.95f, 1.0f);
                }
            }
        }

        /// <summary>
        /// Mengatur tier pahlawan yang ditampilkan pada standee pelataran lobi (T1 s/d T5).
        /// </summary>
        public void SetPreviewTier(int tier)
        {
            currentPreviewTier = Mathf.Clamp(tier, 1, 5);
            UpdateTierSelectorVisuals();
            RefreshDisplays();

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.ButtonClick, 1.15f);
            }

            if (FantasyDefense.Mobile.DevicePerformanceManager.Instance != null && FantasyDefense.Mobile.DevicePerformanceManager.Instance.IsHapticsEnabled)
            {
                FantasyDefense.Mobile.DevicePerformanceManager.Instance.TriggerHaptic(FantasyDefense.Mobile.HapticFeedbackType.Selection);
            }
        }

        #endregion

        private Sprite GetHeroSpriteForType(UnitType type, int tier = 1)
        {
            tier = Mathf.Clamp(tier, 1, 5);
            string baseName = type switch
            {
                UnitType.Cedric => "Cedric",
                UnitType.RoseTanker => "Rose",
                UnitType.RabbitMage => "RabbitMage",
                UnitType.WindRanger => "WindRanger",
                _ => "Cedric"
            };

            // Coba muat sprite sesuai tier aktif pahlawan (T1 hingga T5)
            Sprite sp = null;
            if (tier > 1)
            {
                sp = Resources.Load<Sprite>($"Sprites/Heroes/{baseName}_T{tier}")
                  ?? Resources.Load<Sprite>($"Art/Sprites/Heroes/{baseName}_T{tier}");
            }

            // Fallback ke Tier 1 jika spesifik tier belum termuat
            if (sp == null)
            {
                sp = Resources.Load<Sprite>($"Sprites/Heroes/{baseName}_T1") 
                  ?? Resources.Load<Sprite>($"Art/Sprites/Heroes/{baseName}_T1")
                  ?? Resources.Load<Sprite>($"Sprites/Heroes/Wind_Ranger_T1")
                  ?? Resources.Load<Sprite>($"Sprites/Heroes/RabbitMage_T1");
            }
            return sp;
        }

        private void OnStandeeSlotTapped(Button btn, int slotIndex)
        {
            var lobby = CitadelLobbyManager.Instance;
            var formation = lobby != null ? lobby.GetDeployedFormation() : null;
            UnitType currentType = (formation != null && slotIndex < formation.Count) ? formation[slotIndex] : (UnitType)slotIndex;

            OnStandeeTapped(btn, currentType);
        }

        private UnitType lastTappedStandee = (UnitType)(-1);
        private float lastStandeeTapTime = -1f;
        private GameObject activeGreetingBubble;

        private void OnStandeeTapped(Button btn, UnitType type)
        {
            if (btn != null && btn.gameObject.activeInHierarchy)
            {
                StartCoroutine(StandeeTapJuiceRoutine(btn.transform));
            }

            var lobby = CitadelLobbyManager.Instance;
            var profile = lobby != null ? lobby.GetHeroProfile(type) : null;
            if (profile != null && !profile.isUnlocked)
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySound(SoundType.ButtonClick);
                }
                string lockedMsg = FantasyDefense.Localization.CitadelLocalizationManager.Text(
                    "🔒 Slot Pahlawan ke-4 ini masih terkunci!\nDapatkan Wind Ranger melalui Balai Pemanggilan (Gacha).",
                    "🔒 The 4th Hero Slot is currently locked!\nSummon Wind Ranger in the Summoning Hall."
                );
                string btnStore = FantasyDefense.Localization.CitadelLocalizationManager.Text("🔮 GACHA SEKARANG", "🔮 SUMMON NOW");
                CitadelToastNotice.ShowWithAction(lockedMsg, btnStore, () => OpenPanel(GamePanelType.SummonHall), 3.5f);
                return;
            }

            // Standee Tap Juice: Ucapan Hero & Mikro-partikel
            SpawnHeroGreetingBubble(btn != null ? btn.transform : transform, type);

            if (AudioManager.Instance != null)
            {
                // Thematic chibi voice / weapon sound effect per archetype
                SoundType heroSound = type switch
                {
                    UnitType.Cedric => SoundType.Slash, // Ksatria berpedang: tebasan pedang taktil
                    UnitType.RabbitMage => SoundType.MagicSpark, // Kelinci penyihir: kilau mistis peri
                    UnitType.RoseTanker => SoundType.ShieldSlam, // Paladin palu: dentuman perisai kokoh
                    UnitType.WindRanger => SoundType.PiercingGale, // Pemanah angin: desau panah angin
                    _ => SoundType.ButtonClick
                };
                AudioManager.Instance.PlaySound(heroSound, 1.25f);
                AudioManager.Instance.PlaySound(SoundType.DialogueBlipChime, 1.15f);
            }

            if (FantasyDefense.Mobile.DevicePerformanceManager.Instance != null && FantasyDefense.Mobile.DevicePerformanceManager.Instance.IsHapticsEnabled)
            {
                FantasyDefense.Mobile.DevicePerformanceManager.Instance.TriggerHaptic(FantasyDefense.Mobile.HapticFeedbackType.Selection);
            }

            float now = Time.unscaledTime;
            if (lastTappedStandee == type && (now - lastStandeeTapTime) < 1.1f)
            {
                // Double tap: Buka layar Hero Upgrade
                lastStandeeTapTime = -1f;
                OpenHeroUpgrade(type);
            }
            else
            {
                lastTappedStandee = type;
                lastStandeeTapTime = now;
            }
        }

        private void SpawnHeroGreetingBubble(Transform targetTr, UnitType type)
        {
            if (targetTr == null) return;
            if (activeGreetingBubble != null)
            {
                Destroy(activeGreetingBubble);
            }

            var lilitaFont = Resources.Load<Font>("Fonts/LilitaOne-Regular");
            var canvas = GetComponentInParent<Canvas>();
            Transform parentTr = canvas != null ? canvas.transform : (targetTr.parent != null ? targetTr.parent : targetTr);

            activeGreetingBubble = new GameObject("StandeeGreetingBubble", typeof(RectTransform), typeof(Image));
            activeGreetingBubble.transform.SetParent(parentTr, false);

            var rt = activeGreetingBubble.GetComponent<RectTransform>();
            var cardRt = targetTr.GetComponent<RectTransform>();
            float bubbleYOffset = (cardRt != null && cardRt.rect.height > 10f) 
                ? (cardRt.rect.height * 0.52f + 48f) 
                : 260f;
            Vector3 worldTargetPos = targetTr.position + new Vector3(0f, bubbleYOffset, 0f);

            // Batasi agar gelembung ucapan berada rapi di atas kepala hero tanpa menabrak Header Panel atas
            if (canvas != null)
            {
                var canvasRt = canvas.GetComponent<RectTransform>();
                if (canvasRt != null)
                {
                    float maxY = canvas.transform.position.y + (canvasRt.rect.height * 0.5f) - 130f;
                    if (worldTargetPos.y > maxY) worldTargetPos.y = maxY;
                }
            }
            rt.position = worldTargetPos;
            rt.sizeDelta = new Vector2(250, 75);

            var img = activeGreetingBubble.GetComponent<Image>();
            img.color = new Color(0.08f, 0.12f, 0.20f, 0.94f);

            var outline = activeGreetingBubble.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.82f, 0.25f, 0.85f);
            outline.effectDistance = new Vector2(2, -2);

            var textObj = new GameObject("BubbleText", typeof(RectTransform), typeof(Text));
            textObj.transform.SetParent(activeGreetingBubble.transform, false);
            var textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(10, 6);
            textRt.offsetMax = new Vector2(-10, -6);

            var txt = textObj.GetComponent<Text>();
            if (lilitaFont != null) txt.font = lilitaFont;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.fontSize = 15;
            txt.color = Color.white;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Truncate;

            string quote = type switch
            {
                UnitType.Cedric => FantasyDefense.Localization.CitadelLocalizationManager.Text(
                    "⚔️ \"Pedangku siap melindungi gerbang benteng, Komandan!\"",
                    "⚔️ \"My blade defends the citadel gates, Commander!\""
                ),
                UnitType.RabbitMage => FantasyDefense.Localization.CitadelLocalizationManager.Text(
                    "🔮 \"Sihir bintang dan mantraku siap basmi monster!\"",
                    "🔮 \"My astral magic is ready to blast the monsters!\""
                ),
                UnitType.RoseTanker => FantasyDefense.Localization.CitadelLocalizationManager.Text(
                    "🛡️ \"Perisai mawar takkan pernah membiarkan monster lewat!\"",
                    "🛡️ \"The Rose Aegis shall never let a fiend pass!\""
                ),
                UnitType.WindRanger => FantasyDefense.Localization.CitadelLocalizationManager.Text(
                    "🏹 \"Hembusan angin utara memandu setiap bidikan panahku!\"",
                    "🏹 \"The zephyr gale guides every arrow to its mark!\""
                ),
                _ => "⚔️ \"Siap bertempur!\""
            };

            txt.text = quote;

            StartCoroutine(GreetingBubbleRoutine(activeGreetingBubble));
        }

        private IEnumerator GreetingBubbleRoutine(GameObject bubble)
        {
            if (bubble == null) yield break;

            var rt = bubble.GetComponent<RectTransform>();
            var img = bubble.GetComponent<Image>();
            var txt = bubble.GetComponentInChildren<Text>();
            Vector3 targetPos = rt.position;
            Vector3 startPos = targetPos - new Vector3(0f, 15f, 0f);

            float popDur = 0.20f;
            float elapsed = 0f;
            while (elapsed < popDur)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / popDur;
                float s = Mathf.Sin(t * Mathf.PI * 0.5f);
                rt.position = Vector3.Lerp(startPos, targetPos, s);
                rt.localScale = Vector3.one * (0.4f + s * 0.6f);
                yield return null;
            }

            rt.position = targetPos;
            rt.localScale = Vector3.one;

            yield return new WaitForSecondsRealtime(1.6f);

            if (bubble == null) yield break;

            float fadeDur = 0.35f;
            elapsed = 0f;
            Color origImgCol = img.color;
            Color origTxtCol = txt != null ? txt.color : Color.white;

            while (elapsed < fadeDur)
            {
                if (bubble == null) yield break;
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / fadeDur;
                rt.position = targetPos + new Vector3(0f, t * 18f, 0f);
                img.color = new Color(origImgCol.r, origImgCol.g, origImgCol.b, origImgCol.a * (1f - t));
                if (txt != null)
                {
                    txt.color = new Color(origTxtCol.r, origTxtCol.g, origTxtCol.b, origTxtCol.a * (1f - t));
                }
                yield return null;
            }

            if (bubble != null) Destroy(bubble);
        }

        private IEnumerator StandeeTapJuiceRoutine(Transform tr)
        {
            if (tr == null) yield break;
            Vector3 originalScale = tr.localScale;
            float duration = 0.28f;
            float elapsed = 0f;

            // Spawn micro sparkle burst
            SpawnTapSparkleBurst(tr);

            // Squash-and-stretch respons lembut: 1.0 -> 0.95 (squash) -> 1.02 (stretch) -> 1.0 (settle)
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float scaleFactor;

                if (t < 0.35f)
                {
                    // Phase 1: Squash lembut ke 0.95f
                    float p = t / 0.35f;
                    scaleFactor = Mathf.Lerp(1.0f, 0.95f, Mathf.Sin(p * Mathf.PI * 0.5f));
                }
                else if (t < 0.70f)
                {
                    // Phase 2: Overshoot stretch ke 1.02f
                    float p = (t - 0.35f) / 0.35f;
                    scaleFactor = Mathf.Lerp(0.95f, 1.02f, Mathf.Sin(p * Mathf.PI * 0.5f));
                }
                else
                {
                    // Phase 3: Settle kembali ke 1.00f
                    float p = (t - 0.70f) / 0.30f;
                    scaleFactor = Mathf.Lerp(1.02f, 1.0f, Mathf.Sin(p * Mathf.PI * 0.5f));
                }

                tr.localScale = new Vector3(originalScale.x * scaleFactor, originalScale.y * scaleFactor, originalScale.z);
                yield return null;
            }

            tr.localScale = originalScale;
        }

        private void SpawnTapSparkleBurst(Transform targetTr)
        {
            if (targetTr == null) return;

            var lilitaFont = Resources.Load<Font>("Fonts/LilitaOne-Regular");
            var sparkleObj = new GameObject("StandeeTapSparkle", typeof(RectTransform), typeof(Text));
            sparkleObj.transform.SetParent(targetTr.parent != null ? targetTr.parent : targetTr, false);

            var rt = sparkleObj.GetComponent<RectTransform>();
            rt.position = targetTr.position + new Vector3(0f, 40f, 0f);
            rt.sizeDelta = new Vector2(160, 60);

            var txt = sparkleObj.GetComponent<Text>();
            if (lilitaFont != null) txt.font = lilitaFont;
            txt.text = "✨ ✦ ✨";
            txt.alignment = TextAnchor.MiddleCenter;
            txt.fontSize = 24;
            txt.fontStyle = FontStyle.Bold;
            txt.color = new Color(1f, 0.90f, 0.35f, 1f);

            StartCoroutine(SparkleFloatRoutine(sparkleObj));
        }

        private IEnumerator SparkleFloatRoutine(GameObject obj)
        {
            if (obj == null) yield break;

            var rt = obj.GetComponent<RectTransform>();
            var txt = obj.GetComponent<Text>();
            Vector3 startPos = rt.position;
            float duration = 0.45f;
            float elapsed = 0f;
            Color origColor = txt.color;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                rt.position = startPos + new Vector3(0f, Mathf.Sin(t * Mathf.PI * 0.5f) * 35f, 0f);
                float scale = Mathf.Sin(t * Mathf.PI) * 1.35f;
                rt.localScale = Vector3.one * Mathf.Max(0.1f, scale);

                if (t > 0.4f)
                {
                    float a = (1f - t) / 0.6f;
                    txt.color = new Color(origColor.r, origColor.g, origColor.b, a);
                }

                yield return null;
            }

            Destroy(obj);
        }

        private IEnumerator AnimateRollingGold(int fromVal, int toVal)
        {
            float duration = 0.40f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easeOut = Mathf.Sin(t * Mathf.PI * 0.5f);
                int current = Mathf.RoundToInt(Mathf.Lerp(fromVal, toVal, easeOut));
                _displayedGold = current;
                if (goldText != null)
                {
                    goldText.text = $"{current:N0}";
                }
                yield return null;
            }
            _displayedGold = toVal;
            if (goldText != null)
            {
                goldText.text = $"{toVal:N0}";
            }
            _goldTickerRoutine = null;
        }

        private IEnumerator AnimateRollingEnergy(int fromVal, int toVal, int maxVal)
        {
            float duration = 0.35f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easeOut = Mathf.Sin(t * Mathf.PI * 0.5f);
                int current = Mathf.RoundToInt(Mathf.Lerp(fromVal, toVal, easeOut));
                _displayedEnergy = current;
                if (energyText != null)
                {
                    energyText.text = $"{current}/{maxVal}";
                }
                yield return null;
            }
            _displayedEnergy = toVal;
            if (energyText != null)
            {
                energyText.text = $"{toVal}/{maxVal}";
            }
            _energyTickerRoutine = null;
        }

        private IEnumerator AnimateRollingGems(int fromVal, int toVal)
        {
            float duration = 0.40f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easeOut = Mathf.Sin(t * Mathf.PI * 0.5f);
                int current = Mathf.RoundToInt(Mathf.Lerp(fromVal, toVal, easeOut));
                _displayedGems = current;
                if (gemsText != null)
                {
                    gemsText.text = $"{current:N0}";
                }
                yield return null;
            }
            _displayedGems = toVal;
            if (gemsText != null)
            {
                gemsText.text = $"{toVal:N0}";
            }
            _gemsTickerRoutine = null;
        }

        private void OpenHeroUpgrade(UnitType heroType)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.ButtonClick);
            }

            if (FantasyDefense.Mobile.DevicePerformanceManager.Instance != null &&
                FantasyDefense.Mobile.DevicePerformanceManager.Instance.IsHapticsEnabled)
            {
                FantasyDefense.Mobile.DevicePerformanceManager.Instance.TriggerHaptic(FantasyDefense.Mobile.HapticFeedbackType.Light);
            }

            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.OpenPanel(GamePanelType.HeroUpgrade);
            }

            if (UIHeroUpgradeView.Instance != null)
            {
                UIHeroUpgradeView.Instance.SelectHero(heroType);
            }
        }

        private void OnDefendCitadelClicked()
        {
            if (AudioManager.Instance != null)
            {
                // SFX denting pedang bersilang taktil khas paladin citadel & button click
                AudioManager.Instance.PlaySound(SoundType.DualSlash, 1.25f);
                AudioManager.Instance.PlaySound(SoundType.ButtonClick, 1.05f);
            }

            if (FantasyDefense.Mobile.DevicePerformanceManager.Instance != null &&
                FantasyDefense.Mobile.DevicePerformanceManager.Instance.IsHapticsEnabled)
            {
                FantasyDefense.Mobile.DevicePerformanceManager.Instance.TriggerHaptic(FantasyDefense.Mobile.HapticFeedbackType.Heavy);
            }

            if (CitadelLobbyManager.Instance != null && !CitadelLobbyManager.Instance.CanStartBattle())
            {
                int currentEnergy = CitadelLobbyManager.Instance.CurrentEnergy;
                string toastMsg = FantasyDefense.Localization.CitadelLocalizationManager.Text(
                    $"⚡ Stamina Tidak Cukup!\nMembutuhkan 5 Stamina untuk bertempur. (Tersedia: {currentEnergy})",
                    $"⚡ Not Enough Energy!\nRequires 5 Stamina to commence defense. (Available: {currentEnergy})"
                );
                string btnLabel = FantasyDefense.Localization.CitadelLocalizationManager.Text("💎 BUKA TOKO (REFILL)", "💎 OPEN STORE (REFILL)");
                CitadelToastNotice.ShowWithAction(toastMsg, btnLabel, () => {
                    if (GameFlowManager.Instance != null)
                    {
                        GameFlowManager.Instance.OpenPanel(GamePanelType.Store);
                    }
                }, 4.2f);

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySound(SoundType.DefeatChime);
                }
                return;
            }

            // Buka World Map untuk memilih Stage atau langsung battle
            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.OpenPanel(GamePanelType.StageMap);
            }
        }

        private void OpenPanel(GamePanelType panelType)
        {
            if (FantasyDefense.Audio.AudioManager.Instance != null)
            {
                // SFX Taktil Khas RPG Fantasi sesuai karakter tab navigasi
                FantasyDefense.Audio.SoundType sfx = panelType switch
                {
                    GamePanelType.RelicForge => FantasyDefense.Audio.SoundType.GoblinBucklerClank, // Logam & tempaan pandai besi
                    GamePanelType.HeroUpgrade => FantasyDefense.Audio.SoundType.ButtonClick, // Ksatria & zirah taktil
                    GamePanelType.Store => FantasyDefense.Audio.SoundType.TileSlideSwish, // Gesekan kulit tas petualang
                    GamePanelType.SummonHall => FantasyDefense.Audio.SoundType.SummonCard, // Kristal & resonansi altar magis
                    GamePanelType.StageMap => FantasyDefense.Audio.SoundType.DualSlash, // Denting pedang ekspedisi
                    _ => FantasyDefense.Audio.SoundType.ButtonClick
                };
                FantasyDefense.Audio.AudioManager.Instance.PlaySound(sfx, 1.10f);
            }

            if (FantasyDefense.Mobile.DevicePerformanceManager.Instance != null && FantasyDefense.Mobile.DevicePerformanceManager.Instance.IsHapticsEnabled)
            {
                FantasyDefense.Mobile.DevicePerformanceManager.Instance.TriggerHaptic(FantasyDefense.Mobile.HapticFeedbackType.Selection);
            }

            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.OpenPanel(panelType);
            }
        }

        private void HandleEnergyChanged(int cur, int max) => RefreshDisplays();
        private void HandleGoldChanged(int g) => RefreshDisplays();
        private void HandleGemsChanged(int gems) => RefreshDisplays();
        private void HandleTeamCpUpdated(int cp) => RefreshDisplays();
        private void HandleLevelChanged(int lvl, int exp) => RefreshDisplays();
        private void HandleHeroUpgraded(HeroLobbyProfile p) => RefreshDisplays();
        private void HandleHeroStarPromoted(HeroLobbyProfile p) => RefreshDisplays();
        private void HandleFormationChanged(List<UnitType> formation) => RefreshDisplays();
        private void HandleGachaPullsCompleted(List<GachaResultItem> results) => RefreshDisplays();

        #region Citadel Defense Turret Workshop Modal

        private GameObject turretWorkshopModalObj;
        private Image leftTurretIcon;
        private Text leftTurretTitleText;
        private Text leftTurretStatsText;
        private Button leftTurretSwitchBtn;
        private Button leftTurretUpgradeBtn;
        private Text leftTurretUpgradeText;

        private Image rightTurretIcon;
        private Text rightTurretTitleText;
        private Text rightTurretStatsText;
        private Button rightTurretSwitchBtn;
        private Button rightTurretUpgradeBtn;
        private Text rightTurretUpgradeText;

        public void OpenTurretWorkshopModal()
        {
            if (turretWorkshopModalObj == null)
            {
                BuildTurretWorkshopModal();
            }
            turretWorkshopModalObj.SetActive(true);
            RefreshTurretWorkshopDisplay();
        }

        public void CloseTurretWorkshopModal()
        {
            if (turretWorkshopModalObj != null)
            {
                turretWorkshopModalObj.SetActive(false);
            }
        }

        private void BuildTurretWorkshopModal()
        {
            Font lilitaFont = Resources.Load<Font>("Fonts/LilitaOne-Regular");

            // Modal Dimmer Backdrop
            turretWorkshopModalObj = new GameObject("TurretWorkshopModal", typeof(RectTransform), typeof(Image));
            turretWorkshopModalObj.transform.SetParent(transform, false);
            var modalRt = turretWorkshopModalObj.GetComponent<RectTransform>();
            modalRt.anchorMin = Vector2.zero;
            modalRt.anchorMax = Vector2.one;
            modalRt.offsetMin = Vector2.zero;
            modalRt.offsetMax = Vector2.zero;
            var modalBg = turretWorkshopModalObj.GetComponent<Image>();
            modalBg.color = new Color(0.02f, 0.04f, 0.08f, 0.88f);

            // Center Dialog Card
            var cardObj = new GameObject("WorkshopCard", typeof(RectTransform), typeof(Image));
            cardObj.transform.SetParent(turretWorkshopModalObj.transform, false);
            var cardRt = cardObj.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.06f, 0.15f);
            cardRt.anchorMax = new Vector2(0.94f, 0.85f);
            cardRt.offsetMin = Vector2.zero;
            cardRt.offsetMax = Vector2.zero;
            var cardImg = cardObj.GetComponent<Image>();
            cardImg.color = new Color(0.08f, 0.12f, 0.20f, 0.98f);

            // Header Title
            var headerObj = new GameObject("HeaderTitle", typeof(RectTransform), typeof(Text));
            headerObj.transform.SetParent(cardObj.transform, false);
            var headerRt = headerObj.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0.05f, 0.88f);
            headerRt.anchorMax = new Vector2(0.95f, 0.98f);
            headerRt.offsetMin = Vector2.zero;
            headerRt.offsetMax = Vector2.zero;
            var headerTxt = headerObj.GetComponent<Text>();
            headerTxt.text = "🏰 BENGKEL ARTILERI BENTENG CITADEL";
            headerTxt.font = lilitaFont;
            headerTxt.fontSize = 26;
            headerTxt.alignment = TextAnchor.MiddleCenter;
            headerTxt.color = new Color(1f, 0.88f, 0.35f, 1f);

            // Subtitle
            var subObj = new GameObject("Subtitle", typeof(RectTransform), typeof(Text));
            subObj.transform.SetParent(cardObj.transform, false);
            var subRt = subObj.GetComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0.05f, 0.82f);
            subRt.anchorMax = new Vector2(0.95f, 0.88f);
            subRt.offsetMin = Vector2.zero;
            subRt.offsetMax = Vector2.zero;
            var subTxt = subObj.GetComponent<Text>();
            subTxt.text = "Konfigurasi tipe artileri & tingkatkan tier pertahanan tembok benteng";
            subTxt.font = lilitaFont;
            subTxt.fontSize = 15;
            subTxt.alignment = TextAnchor.MiddleCenter;
            subTxt.color = new Color(0.7f, 0.8f, 0.9f, 1f);

            // Container Menara Kiri
            var leftSlot = CreateTurretSlotUI(cardObj.transform, "Slot_LeftTurret", new Vector2(0.04f, 0.14f), new Vector2(0.48f, 0.80f),
                out leftTurretIcon, out leftTurretTitleText, out leftTurretStatsText, out leftTurretSwitchBtn, out leftTurretUpgradeBtn, out leftTurretUpgradeText);

            leftTurretSwitchBtn.onClick.AddListener(() =>
            {
                var lobby = CitadelLobbyManager.Instance;
                if (lobby != null && lobby.AccountData != null)
                {
                    int nextType = lobby.AccountData.leftTurretTypeIndex == 0 ? 1 : 0;
                    lobby.SetLeftTurretTypeIndex(nextType);
                }
            });

            leftTurretUpgradeBtn.onClick.AddListener(() =>
            {
                var lobby = CitadelLobbyManager.Instance;
                if (lobby != null)
                {
                    if (lobby.TryUpgradeTurret(true, out string msg))
                    {
                        if (UIToastManager.Instance != null) UIToastManager.Instance.ShowToast(msg);
                    }
                    else
                    {
                        if (UIToastManager.Instance != null) UIToastManager.Instance.ShowToast(msg);
                    }
                }
            });

            // Container Menara Kanan
            var rightSlot = CreateTurretSlotUI(cardObj.transform, "Slot_RightTurret", new Vector2(0.52f, 0.14f), new Vector2(0.96f, 0.80f),
                out rightTurretIcon, out rightTurretTitleText, out rightTurretStatsText, out rightTurretSwitchBtn, out rightTurretUpgradeBtn, out rightTurretUpgradeText);

            rightTurretSwitchBtn.onClick.AddListener(() =>
            {
                var lobby = CitadelLobbyManager.Instance;
                if (lobby != null && lobby.AccountData != null)
                {
                    int nextType = lobby.AccountData.rightTurretTypeIndex == 0 ? 1 : 0;
                    lobby.SetRightTurretTypeIndex(nextType);
                }
            });

            rightTurretUpgradeBtn.onClick.AddListener(() =>
            {
                var lobby = CitadelLobbyManager.Instance;
                if (lobby != null)
                {
                    if (lobby.TryUpgradeTurret(false, out string msg))
                    {
                        if (UIToastManager.Instance != null) UIToastManager.Instance.ShowToast(msg);
                    }
                    else
                    {
                        if (UIToastManager.Instance != null) UIToastManager.Instance.ShowToast(msg);
                    }
                }
            });

            // Close Button
            var closeObj = new GameObject("CloseBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            closeObj.transform.SetParent(cardObj.transform, false);
            var closeRt = closeObj.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(0.35f, 0.025f);
            closeRt.anchorMax = new Vector2(0.65f, 0.10f);
            closeRt.offsetMin = Vector2.zero;
            closeRt.offsetMax = Vector2.zero;
            var closeImg = closeObj.GetComponent<Image>();
            closeImg.color = new Color(0.25f, 0.30f, 0.40f, 1f);
            var closeBtn = closeObj.GetComponent<Button>();
            closeBtn.onClick.AddListener(CloseTurretWorkshopModal);

            var closeTxtObj = new GameObject("CloseTxt", typeof(RectTransform), typeof(Text));
            closeTxtObj.transform.SetParent(closeObj.transform, false);
            var closeTxtRt = closeTxtObj.GetComponent<RectTransform>();
            closeTxtRt.anchorMin = Vector2.zero;
            closeTxtRt.anchorMax = Vector2.one;
            closeTxtRt.offsetMin = Vector2.zero;
            closeTxtRt.offsetMax = Vector2.zero;
            var closeTxt = closeTxtObj.GetComponent<Text>();
            closeTxt.text = "TUTUP (✕)";
            closeTxt.font = lilitaFont;
            closeTxt.fontSize = 18;
            closeTxt.alignment = TextAnchor.MiddleCenter;
            closeTxt.color = Color.white;
        }

        private GameObject CreateTurretSlotUI(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            out Image iconImg,
            out Text titleTxt,
            out Text statsTxt,
            out Button switchBtn,
            out Button upgradeBtn,
            out Text upgradeTxt)
        {
            Font lilitaFont = Resources.Load<Font>("Fonts/LilitaOne-Regular");

            var slotObj = new GameObject(name, typeof(RectTransform), typeof(Image));
            slotObj.transform.SetParent(parent, false);
            var slotRt = slotObj.GetComponent<RectTransform>();
            slotRt.anchorMin = anchorMin;
            slotRt.anchorMax = anchorMax;
            slotRt.offsetMin = Vector2.zero;
            slotRt.offsetMax = Vector2.zero;
            var slotImg = slotObj.GetComponent<Image>();
            slotImg.color = new Color(0.12f, 0.16f, 0.25f, 0.95f);

            // Icon Avatar
            var iconObj = new GameObject("TurretIcon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(slotObj.transform, false);
            var iconRt = iconObj.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.15f, 0.54f);
            iconRt.anchorMax = new Vector2(0.85f, 0.94f);
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;
            iconImg = iconObj.GetComponent<Image>();
            iconImg.preserveAspect = true;

            // Title
            var tObj = new GameObject("TurretTitle", typeof(RectTransform), typeof(Text));
            tObj.transform.SetParent(slotObj.transform, false);
            var tRt = tObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0.05f, 0.44f);
            tRt.anchorMax = new Vector2(0.95f, 0.54f);
            tRt.offsetMin = Vector2.zero;
            tRt.offsetMax = Vector2.zero;
            titleTxt = tObj.GetComponent<Text>();
            titleTxt.font = lilitaFont;
            titleTxt.fontSize = 16;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.color = new Color(1f, 0.88f, 0.35f, 1f);

            // Stats
            var sObj = new GameObject("TurretStats", typeof(RectTransform), typeof(Text));
            sObj.transform.SetParent(slotObj.transform, false);
            var sRt = sObj.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0.05f, 0.22f);
            sRt.anchorMax = new Vector2(0.95f, 0.44f);
            sRt.offsetMin = Vector2.zero;
            sRt.offsetMax = Vector2.zero;
            statsTxt = sObj.GetComponent<Text>();
            statsTxt.font = lilitaFont;
            statsTxt.fontSize = 13;
            statsTxt.alignment = TextAnchor.UpperCenter;
            statsTxt.color = new Color(0.85f, 0.90f, 0.95f, 1f);

            // Switch Button
            var swObj = new GameObject("SwitchBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            swObj.transform.SetParent(slotObj.transform, false);
            var swRt = swObj.GetComponent<RectTransform>();
            swRt.anchorMin = new Vector2(0.08f, 0.11f);
            swRt.anchorMax = new Vector2(0.92f, 0.20f);
            swRt.offsetMin = Vector2.zero;
            swRt.offsetMax = Vector2.zero;
            var swImg = swObj.GetComponent<Image>();
            swImg.color = new Color(0.18f, 0.35f, 0.55f, 1f);
            switchBtn = swObj.GetComponent<Button>();

            var swTxtObj = new GameObject("Txt", typeof(RectTransform), typeof(Text));
            swTxtObj.transform.SetParent(swObj.transform, false);
            var swTxtRt = swTxtObj.GetComponent<RectTransform>();
            swTxtRt.anchorMin = Vector2.zero;
            swTxtRt.anchorMax = Vector2.one;
            swTxtRt.offsetMin = Vector2.zero;
            swTxtRt.offsetMax = Vector2.zero;
            var swTxt = swTxtObj.GetComponent<Text>();
            swTxt.text = "GANTI TIPE";
            swTxt.font = lilitaFont;
            swTxt.fontSize = 13;
            swTxt.alignment = TextAnchor.MiddleCenter;
            swTxt.color = Color.white;

            // Upgrade Button
            var upObj = new GameObject("UpgradeBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            upObj.transform.SetParent(slotObj.transform, false);
            var upRt = upObj.GetComponent<RectTransform>();
            upRt.anchorMin = new Vector2(0.08f, 0.015f);
            upRt.anchorMax = new Vector2(0.92f, 0.10f);
            upRt.offsetMin = Vector2.zero;
            upRt.offsetMax = Vector2.zero;
            var upImg = upObj.GetComponent<Image>();
            upImg.color = new Color(0.20f, 0.55f, 0.25f, 1f);
            upgradeBtn = upObj.GetComponent<Button>();

            var upTxtObj = new GameObject("Txt", typeof(RectTransform), typeof(Text));
            upTxtObj.transform.SetParent(upObj.transform, false);
            var upTxtRt = upTxtObj.GetComponent<RectTransform>();
            upTxtRt.anchorMin = Vector2.zero;
            upTxtRt.anchorMax = Vector2.one;
            upTxtRt.offsetMin = Vector2.zero;
            upTxtRt.offsetMax = Vector2.zero;
            upgradeTxt = upTxtObj.GetComponent<Text>();
            upgradeTxt.text = "TINGKATKAN (🪙)";
            upgradeTxt.font = lilitaFont;
            upgradeTxt.fontSize = 13;
            upgradeTxt.alignment = TextAnchor.MiddleCenter;
            upgradeTxt.color = Color.white;

            return slotObj;
        }

        public void RefreshTurretWorkshopDisplay()
        {
            if (turretWorkshopModalObj == null || !turretWorkshopModalObj.activeSelf) return;

            var lobby = CitadelLobbyManager.Instance;
            if (lobby == null || lobby.AccountData == null) return;
            var data = lobby.AccountData;

            // 1. Menara Kiri
            bool isLeftCannon = data.leftTurretTypeIndex == 0;
            int leftTier = data.leftTurretTier;
            if (leftTurretIcon != null)
            {
                leftTurretIcon.sprite = isLeftCannon
                    ? (Resources.Load<Sprite>("Sprites/Combat/cannon_turret") ?? Resources.Load<Sprite>("Sprites/Combat/bastion_cannon_left"))
                    : Resources.Load<Sprite>("Sprites/Combat/ballista_turret");
            }
            if (leftTurretTitleText != null)
            {
                leftTurretTitleText.text = isLeftCannon ? $"MERIAM BENTENG (Tier {leftTier})" : $"BALISTA SNIPER (Tier {leftTier})";
            }
            if (leftTurretStatsText != null)
            {
                float tierMult = 1f + (leftTier - 1) * 0.25f;
                float spdMult = Mathf.Max(0.6f, 1f - (leftTier - 1) * 0.06f);
                if (isLeftCannon)
                    leftTurretStatsText.text = $"• DMG Area: {60 * tierMult:F0}\n• Interval: {2.2f * spdMult:F1}s | Radius: 1.3m\n• Efek: 15% Slow Mob\n• Peluru: Incendiary Bomb";
                else
                    leftTurretStatsText.text = $"• DMG Satuan: {35 * tierMult:F0}\n• Interval: {1.2f * spdMult:F1}s | Range: 8.5m\n• Efek: Tembus Armor Cepat\n• Peluru: Chisel Crystal Bolt";
            }
            if (leftTurretSwitchBtn != null)
            {
                var txt = leftTurretSwitchBtn.GetComponentInChildren<Text>();
                if (txt != null) txt.text = isLeftCannon ? "Tukar ke Balista 🏹" : "Tukar ke Meriam 💣";
            }
            if (leftTurretUpgradeText != null)
            {
                int cost = leftTier switch { 1 => 200, 2 => 450, 3 => 800, 4 => 1500, _ => 0 };
                leftTurretUpgradeText.text = leftTier >= 5 ? "TIER MAKSIMUM (5★)" : $"Tingkatkan ({cost} 🪙)";
                if (leftTurretUpgradeBtn != null) leftTurretUpgradeBtn.interactable = leftTier < 5 && data.goldCoins >= cost;
            }

            // 2. Menara Kanan
            bool isRightCannon = data.rightTurretTypeIndex == 0;
            int rightTier = data.rightTurretTier;
            if (rightTurretIcon != null)
            {
                rightTurretIcon.sprite = isRightCannon
                    ? (Resources.Load<Sprite>("Sprites/Combat/cannon_turret") ?? Resources.Load<Sprite>("Sprites/Combat/bastion_cannon_left"))
                    : Resources.Load<Sprite>("Sprites/Combat/ballista_turret");
            }
            if (rightTurretTitleText != null)
            {
                rightTurretTitleText.text = isRightCannon ? $"MERIAM BENTENG (Tier {rightTier})" : $"BALISTA SNIPER (Tier {rightTier})";
            }
            if (rightTurretStatsText != null)
            {
                float tierMult = 1f + (rightTier - 1) * 0.25f;
                float spdMult = Mathf.Max(0.6f, 1f - (rightTier - 1) * 0.06f);
                if (isRightCannon)
                    rightTurretStatsText.text = $"• DMG Area: {60 * tierMult:F0}\n• Interval: {2.2f * spdMult:F1}s | Radius: 1.3m\n• Efek: 15% Slow Mob\n• Peluru: Incendiary Bomb";
                else
                    rightTurretStatsText.text = $"• DMG Satuan: {35 * tierMult:F0}\n• Interval: {1.2f * spdMult:F1}s | Range: 8.5m\n• Efek: Tembus Armor Cepat\n• Peluru: Chisel Crystal Bolt";
            }
            if (rightTurretSwitchBtn != null)
            {
                var txt = rightTurretSwitchBtn.GetComponentInChildren<Text>();
                if (txt != null) txt.text = isRightCannon ? "Tukar ke Balista 🏹" : "Tukar ke Meriam 💣";
            }
            if (rightTurretUpgradeText != null)
            {
                int cost = rightTier switch { 1 => 200, 2 => 450, 3 => 800, 4 => 1500, _ => 0 };
                rightTurretUpgradeText.text = rightTier >= 5 ? "TIER MAKSIMUM (5★)" : $"Tingkatkan ({cost} 🪙)";
                if (rightTurretUpgradeBtn != null) rightTurretUpgradeBtn.interactable = rightTier < 5 && data.goldCoins >= cost;
            }
        }

        #endregion
    }
}
