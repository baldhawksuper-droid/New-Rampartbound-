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
            CitadelLobbyManager.OnTurretConfigChanged += RefreshTurretWorkshopDisplay;
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
            CitadelLobbyManager.OnTurretConfigChanged -= RefreshTurretWorkshopDisplay;
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
            string rarityTag = type switch
            {
                UnitType.WindRanger => "<color=#FFD700>★ LEGENDARY</color>\n",
                UnitType.RabbitMage => "<color=#C070FF>★ EPIC</color>\n",
                _ => "<color=#40C0FF>★ RARE</color>\n"
            };

            if (profile != null)
            {
                infoText.text = $"{rarityTag}<b>Lv.{profile.level} | CP {profile.CalculateHeroCP():N0}</b>";
            }
            else
            {
                infoText.text = $"{rarityTag}<b>Lv.{defaultLvl} | CP 1,500</b>";
            }

            if (btn != null)
            {
                var img = btn.transform.Find("HeroPortrait")?.GetComponent<Image>();
                if (img != null)
                {
                    img.color = Color.white;
                    int heroTier = profile != null ? Mathf.Clamp(profile.starRank, 1, 5) : 1;
                    Sprite heroSprite = GetHeroSpriteForType(type, heroTier);
                    if (heroSprite != null)
                    {
                        img.sprite = heroSprite;
                    }
                }
            }
        }

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

            // Spawn micro sparkle burst
            SpawnTapSparkleBurst(tr);

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
