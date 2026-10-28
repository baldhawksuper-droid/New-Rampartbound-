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
            if (navStageMapButton != null) navStageMapButton.gameObject.SetActive(true);
            if (navHeroesButton != null) navHeroesButton.gameObject.SetActive(true);
            if (navSummonButton != null) navSummonButton.gameObject.SetActive(true);
            if (navStoreButton != null) navStoreButton.gameObject.SetActive(true);

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
            CitadelLobbyManager.OnFormationChanged += HandleFormationChanged;
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
            CitadelLobbyManager.OnFormationChanged -= HandleFormationChanged;
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
                    txt.text = FantasyDefense.Localization.CitadelLocalizationManager.Get("LOBBY_DEFEND_BTN", "⚔️ BATTLE (5 ⚡)", "⚔️ BATTLE (5 ⚡)");
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
                energyText.text = $"{data.currentEnergy}/{data.maxEnergy}";
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
                        infoTexts[i].text = $"<b><color=#AAAAAA>[SLOT {i + 1}]</color></b>\n<color=#FFD700>🔒 Gacha to Unlock</color>";
                    }
                    if (standeeButtons[i] != null)
                    {
                        var img = standeeButtons[i].transform.Find("HeroPortrait")?.GetComponent<Image>();
                        if (img != null)
                        {
                            img.color = new Color(0.25f, 0.28f, 0.38f, 0.45f); // Siluet terkunci
                        }
                    }
                }
            }
        }

        private void UpdateStandeeDisplay(UnitType type, Text infoText, Button btn, string defaultName, int defaultLvl)
        {
            if (infoText == null) return;

            var lobby = CitadelLobbyManager.Instance;
            var profile = lobby != null ? lobby.GetHeroProfile(type) : null;
            if (profile != null)
            {
                infoText.text = $"<b>Lv.{profile.level} | CP {profile.CalculateHeroCP():N0}</b>";
            }
            else
            {
                infoText.text = $"<b>Lv.{defaultLvl} | CP 1,500</b>";
            }

            if (btn != null)
            {
                var img = btn.transform.Find("HeroPortrait")?.GetComponent<Image>();
                if (img != null)
                {
                    img.color = Color.white;
                    Sprite heroSprite = GetHeroSpriteForType(type);
                    if (heroSprite != null)
                    {
                        img.sprite = heroSprite;
                    }
                }
            }
        }

        private Sprite GetHeroSpriteForType(UnitType type)
        {
            string spriteName = type switch
            {
                UnitType.Cedric => "Cedric_T1",
                UnitType.RoseTanker => "Rose_T1",
                UnitType.RabbitMage => "Pippin_T1",
                UnitType.WindRanger => "WindRanger_T1",
                _ => "Cedric_T1"
            };

            var sp = Resources.Load<Sprite>($"Sprites/Heroes/{spriteName}") 
                  ?? Resources.Load<Sprite>($"Art/Sprites/Heroes/{spriteName}")
                  ?? Resources.Load<Sprite>($"Sprites/Heroes/Wind_Ranger_T1")
                  ?? Resources.Load<Sprite>($"Sprites/Heroes/RabbitMage_T1");
            return sp;
        }

        private void OnStandeeSlotTapped(Button btn, int slotIndex)
        {
            var lobby = CitadelLobbyManager.Instance;
            var formation = lobby != null ? lobby.GetDeployedFormation() : null;
            UnitType currentType = (formation != null && slotIndex < formation.Count) ? formation[slotIndex] : (UnitType)slotIndex;

            OnStandeeTapped(btn, currentType);
        }

        private void OnStandeeTapped(Button btn, UnitType type)
        {
            if (btn != null && btn.gameObject.activeInHierarchy)
            {
                StartCoroutine(StandeeTapJuiceRoutine(btn.transform));
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.ButtonClick);
            }

            var lobby = CitadelLobbyManager.Instance;
            var profile = lobby != null ? lobby.GetHeroProfile(type) : null;
            if (profile != null && !profile.isUnlocked)
            {
                string lockedMsg = FantasyDefense.Localization.CitadelLocalizationManager.Text(
                    "🔒 Slot Pahlawan ke-4 ini masih terkunci!\nDapatkan Wind Ranger melalui Balai Pemanggilan (Gacha).",
                    "🔒 The 4th Hero Slot is currently locked!\nSummon Wind Ranger in the Summoning Hall."
                );
                string btnStore = FantasyDefense.Localization.CitadelLocalizationManager.Text("🔮 GACHA SEKARANG", "🔮 SUMMON NOW");
                CitadelToastNotice.ShowWithAction(lockedMsg, btnStore, () => OpenPanel(GamePanelType.SummonHall), 3.5f);
                return;
            }

            OpenHeroUpgrade(type);
        }

        private IEnumerator StandeeTapJuiceRoutine(Transform tr)
        {
            Vector3 originalScale = tr.localScale;
            float duration = 0.22f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                float bounce = Mathf.Sin(t * Mathf.PI) * 0.16f;
                tr.localScale = new Vector3(originalScale.x * (1f + bounce * 0.8f), originalScale.y * (1f + bounce), originalScale.z);
                yield return null;
            }

            tr.localScale = originalScale;
        }

        private void OpenHeroUpgrade(UnitType heroType)
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
                UIHeroUpgradeView.Instance.SelectHero(heroType);
            }
        }

        private void OnDefendCitadelClicked()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.ButtonClick);
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
    }
}
