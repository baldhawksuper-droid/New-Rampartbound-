using System;
using UnityEngine;
using UnityEngine.UI;
using FantasyDefense.Audio;
using FantasyDefense.Core;
using FantasyDefense.Data;
using FantasyDefense.Metagame;

namespace FantasyDefense.UI
{
    /// <summary>
    /// Master UI Controller untuk Pusat Benteng (Citadel Lobby).
    /// Menghubungkan seluruh status metagame: Saldo Gold, Gems, Stamina/Energi, Level Citadel, Team CP,
    /// Showcase Kuartet Pahlawan (Cedric, Rose Tanker, Rabbit Mage, Wind Ranger),
    /// Tombol Utama 'Pertahankan Benteng (Battle)', serta Dermaga Navigasi Bawah (Shop, Gacha, Forge, Talenta, Quests, Guild, Pass, dsb).
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

        [Header("Hero Quartet Showcase Cards")]
        [SerializeField] private Button cedricCardButton;
        [SerializeField] private Text cedricInfoText;
        [SerializeField] private Button roseCardButton;
        [SerializeField] private Text roseInfoText;
        [SerializeField] private Button rabbitCardButton;
        [SerializeField] private Text rabbitInfoText;
        [SerializeField] private Button windRangerCardButton;
        [SerializeField] private Text windRangerInfoText;

        [Header("Core Battle Action Buttons")]
        [SerializeField] private Button defendCitadelButton;
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
        [SerializeField] private bool showForge = false;
        [SerializeField] private bool showFeaturePreview = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeButtons();
            ApplyResponsiveLayout();
        }

        public void ApplyResponsiveLayout()
        {
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

            // Jika quick action bar semua disembunyikan, sembunyikan bar container-nya
            if (navTalentsButton != null && navTalentsButton.transform.parent != null)
            {
                var quickBar = navTalentsButton.transform.parent.gameObject;
                bool anyQuickActive = showTalents || showQuests || showGuild || showBattlePass || showEventArena || showArtifacts;
                quickBar.SetActive(anyQuickActive);
            }

            // Pastikan dock bawah memiliki HorizontalLayoutGroup agar 4 tombol tersisa otomatis merenggang penuh
            if (navStageMapButton != null && navStageMapButton.transform.parent != null)
            {
                var dock = navStageMapButton.transform.parent.gameObject;
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
                hlg.spacing = 14f;
                hlg.padding = new RectOffset(16, 16, 8, 8);
                LayoutRebuilder.ForceRebuildLayoutImmediate(dock.GetComponent<RectTransform>());
            }

            // Anti-Menumpuk: Posisi dinamis tombol Defend Citadel agar tidak bertabrakan dengan elemen lain
            if (defendCitadelButton != null)
            {
                var rt = defendCitadelButton.GetComponent<RectTransform>();
                if (rt != null)
                {
                    bool anyQuickActive = showTalents || showQuests || showGuild || showBattlePass || showEventArena || showArtifacts;
                    if (!anyQuickActive && !showAbyssalTrial)
                    {
                        rt.anchorMin = new Vector2(0.08f, 0.145f);
                        rt.anchorMax = new Vector2(0.92f, 0.280f);
                        rt.offsetMin = Vector2.zero;
                        rt.offsetMax = Vector2.zero;
                    }
                }
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
            CitadelSummonManager.OnGachaPullsCompleted += HandleGachaPullsCompleted;
            FantasyDefense.Localization.CitadelLocalizationManager.OnLanguageChanged += HandleLanguageChanged;

            ApplyResponsiveLayout();
            RefreshLocalization();
            RefreshDisplays();
        }

        private void OnDisable()
        {
            CitadelLobbyManager.OnEnergyChanged -= HandleEnergyChanged;
            CitadelLobbyManager.OnGoldChanged -= HandleGoldChanged;
            CitadelLobbyManager.OnAccountGemsChanged -= HandleGemsChanged;
            CitadelLobbyManager.OnTeamCPUpdated -= HandleTeamCpUpdated;
            CitadelLobbyManager.OnCitadelLevelChanged -= HandleLevelChanged;
            CitadelLobbyManager.OnHeroUpgraded -= HandleHeroUpgraded;
            CitadelLobbyManager.OnHeroStarPromoted -= HandleHeroStarPromoted;
            CitadelSummonManager.OnGachaPullsCompleted -= HandleGachaPullsCompleted;
            FantasyDefense.Localization.CitadelLocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
        }

        private void Start()
        {
            ApplyResponsiveLayout();
            RefreshLocalization();
            RefreshDisplays();
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
                    txt.text = FantasyDefense.Localization.CitadelLocalizationManager.Get("LOBBY_DEFEND_BTN", "⚔️ PERTAHANKAN BENTENG (BATTLE)", "⚔️ DEFEND CITADEL (BATTLE)");
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

            if (navStageMapButton != null)
            {
                var txt = navStageMapButton.GetComponentInChildren<Text>();
                if (txt != null) txt.text = FantasyDefense.Localization.CitadelLocalizationManager.Get("NAV_MAP", "🗺️\nPeta", "🗺️\nMap");
            }

            if (navHeroesButton != null)
            {
                var txt = navHeroesButton.GetComponentInChildren<Text>();
                if (txt != null) txt.text = FantasyDefense.Localization.CitadelLocalizationManager.Get("NAV_HEROES", "🛡️\nHero", "🛡️\nHeroes");
            }

            if (navSummonButton != null)
            {
                var txt = navSummonButton.GetComponentInChildren<Text>();
                if (txt != null) txt.text = FantasyDefense.Localization.CitadelLocalizationManager.Get("NAV_SUMMON", "🔮\nGacha", "🔮\nSummon");
            }

            if (navStoreButton != null)
            {
                var txt = navStoreButton.GetComponentInChildren<Text>();
                if (txt != null) txt.text = FantasyDefense.Localization.CitadelLocalizationManager.Get("NAV_STORE", "💎\nToko", "💎\nStore");
            }

            if (navForgeButton != null)
            {
                var txt = navForgeButton.GetComponentInChildren<Text>();
                if (txt != null) txt.text = FantasyDefense.Localization.CitadelLocalizationManager.Get("NAV_FORGE", "🔨\nTempa", "🔨\nForge");
            }
        }

        private void InitializeButtons()
        {
            // Settings
            if (settingsButton != null)
                settingsButton.onClick.AddListener(() => OpenPanel(GamePanelType.AudioSettings));

            // Hero Quartet Shortcuts
            if (cedricCardButton != null)
                cedricCardButton.onClick.AddListener(() => OpenHeroUpgrade(UnitType.Cedric));
            if (roseCardButton != null)
                roseCardButton.onClick.AddListener(() => OpenHeroUpgrade(UnitType.RoseTanker));
            if (rabbitCardButton != null)
                rabbitCardButton.onClick.AddListener(() => OpenHeroUpgrade(UnitType.RabbitMage));
            if (windRangerCardButton != null)
                windRangerCardButton.onClick.AddListener(() => OpenHeroUpgrade(UnitType.WindRanger));

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

        public void RefreshDisplays()
        {
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
            if (energyText != null)
            {
                energyText.text = $"{data.currentEnergy} / {data.maxEnergy}";
            }
            if (goldText != null)
            {
                goldText.text = $"{data.goldCoins:N0}";
            }
            if (gemsText != null)
            {
                gemsText.text = $"{data.accountGems:N0}";
            }
            if (teamCpText != null)
            {
                teamCpText.text = $"CP: {lobby.TotalTeamCP:N0}";
            }

            UpdateHeroCard(UnitType.Cedric, cedricInfoText, "Cedric", "Melee Striker");
            UpdateHeroCard(UnitType.RoseTanker, roseInfoText, "Rose Tanker", "Heavy Tanker");
            UpdateHeroCard(UnitType.RabbitMage, rabbitInfoText, "Rabbit Mage", "Magic AoE");
            var windProfile = lobby.GetHeroProfile(UnitType.WindRanger);
            if (windProfile != null && windProfile.isUnlocked)
            {
                UpdateHeroCard(UnitType.WindRanger, windRangerInfoText, "Wind Ranger", "Physical Gale");
            } else if (windRangerInfoText != null)
            {
                windRangerInfoText.text = "<b>Wind Ranger</b>\n<color=#FFD700>Summon to unlock</color>";
            }
            if (windRangerCardButton != null)
            {
                windRangerCardButton.interactable = windProfile != null && windProfile.isUnlocked;
            }
        }

        private void UpdateHeroCard(UnitType type, Text infoText, string defaultName, string role)
        {
            if (infoText == null) return;

            var lobby = CitadelLobbyManager.Instance;
            var profile = lobby != null ? lobby.GetHeroProfile(type) : null;
            if (profile != null)
            {
                string stars = new string('★', profile.starRank);
                infoText.text = $"<b>{profile.heroName}</b>\nLv.{profile.level} {stars}\n<color=#FFD700>CP: {profile.CalculateHeroCP():N0}</color>";
            }
            else
            {
                infoText.text = $"<b>{defaultName}</b>\n{role}\n<color=#AAAAAA>Siap Bertarung</color>";
            }
        }

        private void OpenHeroUpgrade(UnitType type)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.ButtonClick);
            }

            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.OpenPanel(GamePanelType.HeroUpgrade);
            }

            if (UIHeroUpgradeView.Instance != null)
            {
                UIHeroUpgradeView.Instance.SelectHero(type);
            }
        }

        private void OnDefendCitadelClicked()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.ButtonClick);
            }

            // Buka World Map untuk memilih Stage atau langsung battle
            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.OpenPanel(GamePanelType.StageMap);
            }
        }

        private void OpenPanel(GamePanelType panelType)
        {
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
        private void HandleGachaPullsCompleted(System.Collections.Generic.List<GachaResultItem> results) => RefreshDisplays();
    }
}
