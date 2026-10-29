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
    /// Master UI Controller untuk Layar Pemilihan Stage & Peta Dunia (Stage Progression Map).
    /// Menampilkan daftar 10 stage Chapter 1 (Stage 1-1 sampai Stage 1-10 Boss), perolehan bintang 1-3,
    /// perbandingan Team CP vs Rekomendasi CP, konsumsi stamina, serta detail hadiah First-Clear.
    /// </summary>
    public class UIStageMapView : MonoBehaviour
    {
        public static UIStageMapView Instance { get; private set; }

        [Header("Root & Navigation")]
        [SerializeField] private GameObject rootPanel;
        [SerializeField] private Button closeButton;

        [Header("Chapter Overview")]
        [SerializeField] private Text chapterTitleText;
        [SerializeField] private Text totalStarsText;
        [SerializeField] private Slider chapterStarSlider;

        [Header("Selected Stage Detail Panel")]
        [SerializeField] private GameObject stageDetailPanel;
        [SerializeField] private Text stageCodeText;
        [SerializeField] private Text stageTitleText;
        [SerializeField] private Text stageDescriptionText;
        [SerializeField] private Text battleModeText;
        [SerializeField] private Text recommendedCpText;
        [SerializeField] private Text difficultyStatusText;
        [SerializeField] private Text energyCostText;
        [SerializeField] private Text firstClearRewardsText;
        [SerializeField] private Text repeatRewardsText;
        [SerializeField] private Button startBattleButton;

        [Header("Milestone Rewards")]
        [SerializeField] private Button claim10StarButton;
        [SerializeField] private Button claim20StarButton;
        [SerializeField] private Button claim30StarButton;
        [SerializeField] private Text milestoneFeedbackText;

        [Header("Stage Node Buttons (Optional Inspector Slots)")]
        [SerializeField] private List<Button> stageNodeButtons = new List<Button>();

        private StageDataSO currentStage;

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
            StageManager.OnStageSelected += HandleStageSelected;
            StageManager.OnStageCompleted += HandleStageCompleted;
            CitadelLobbyManager.OnEnergyChanged += HandleEnergyChanged;
            CitadelLobbyManager.OnTeamCPUpdated += HandleTeamCPUpdated;
            FantasyDefense.Localization.CitadelLocalizationManager.OnLanguageChanged += HandleLanguageChanged;
        }

        private void OnDisable()
        {
            StageManager.OnStageSelected -= HandleStageSelected;
            StageManager.OnStageCompleted -= HandleStageCompleted;
            CitadelLobbyManager.OnEnergyChanged -= HandleEnergyChanged;
            CitadelLobbyManager.OnTeamCPUpdated -= HandleTeamCPUpdated;
            FantasyDefense.Localization.CitadelLocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
        }

        private void HandleLanguageChanged(FantasyDefense.Localization.AppLanguage lang)
        {
            RefreshMapView();
            if (currentStage != null)
            {
                SelectStage(currentStage);
            }
        }

        private void Start()
        {
            EnsureStageManagerExists();
            RefreshMapView();

            if (StageManager.Instance != null)
            {
                SelectStage(StageManager.Instance.GetSelectedStage());
            }
        }

        private void EnsureStageManagerExists()
        {
            if (StageManager.Instance == null)
            {
                GameObject stageObj = new GameObject("StageManager");
                stageObj.AddComponent<StageManager>();
            }
        }

        [Header("Chapter Progression")]
        [SerializeField] private int activeChapter = 1;

        public int ActiveChapter => activeChapter;

        public void SetChapter(int chapter)
        {
            activeChapter = 1; // Terkunci pada Chapter 1 sesuai arahan Komandan: Menuntaskan dan menyempurnakan Chapter 1
            BindStageNodeButtons();
            RefreshMapView();

            if (StageManager.Instance != null)
            {
                var stages = StageManager.Instance.GetStagesByChapter(activeChapter);
                if (stages.Count > 0)
                {
                    SelectStage(stages[0]);
                }
            }
        }

        private void BindStageNodeButtons()
        {
            for (int i = 0; i < stageNodeButtons.Count; i++)
            {
                int stageIdx = i + 1;
                int stageId = (activeChapter * 100) + stageIdx;
                int cachedId = stageId;
                stageNodeButtons[i].onClick.RemoveAllListeners();
                stageNodeButtons[i].onClick.AddListener(() => OnStageNodeClicked(cachedId));
            }
        }

        private void InitializeButtons()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(CloseView);

            if (startBattleButton != null)
            {
                startBattleButton.onClick.AddListener(OnStartBattleClicked);
            }

            if (claim10StarButton != null)
                claim10StarButton.onClick.AddListener(() => ClaimMilestone(10));

            if (claim20StarButton != null)
                claim20StarButton.onClick.AddListener(() => ClaimMilestone(20));

            if (claim30StarButton != null)
                claim30StarButton.onClick.AddListener(() => ClaimMilestone(30));

            BindStageNodeButtons();
        }

        public void OpenView()
        {
            if (rootPanel != null) rootPanel.SetActive(true);
            else gameObject.SetActive(true);

            // Bawa panel ke tumpukan terdepan agar tidak tertutup elemen lain
            transform.SetAsLastSibling();

            var cg = GetComponent<CanvasGroup>() ?? rootPanel?.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 1f;
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }

            EnsureStageManagerExists();
            BindStageNodeButtons();
            RefreshMapView();

            StageDataSO targetStage = null;
            if (StageManager.Instance != null)
            {
                targetStage = StageManager.Instance.GetSelectedStage();
                if (targetStage == null && StageManager.Instance.ChapterStages != null && StageManager.Instance.ChapterStages.Count > 0)
                {
                    targetStage = StageManager.Instance.ChapterStages[0];
                }
            }
            if (targetStage != null)
            {
                SelectStage(targetStage);
            }
        }

        public void CloseView()
        {
            if (rootPanel != null) rootPanel.SetActive(false);
            else gameObject.SetActive(false);

            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.CloseCurrentPanel();
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.ButtonClick);
            }
        }

        public void RefreshMapView()
        {
            if (StageManager.Instance == null || CitadelLobbyManager.Instance == null) return;

            int totalStars = StageManager.Instance.GetTotalChapterStars();

            if (chapterTitleText != null)
            {
                chapterTitleText.text = FantasyDefense.Localization.CitadelLocalizationManager.Text(
                    "Chapter 1: Pertahanan Pinggiran Kerajaan",
                    "Chapter 1: Outskirts Citadel Defense");
            }

            if (totalStarsText != null)
            {
                string starsLabel = FantasyDefense.Localization.CitadelLocalizationManager.Text("Total Bintang", "Total Stars");
                totalStarsText.text = $"{starsLabel}: {totalStars} / 30 ★";
            }

            if (chapterStarSlider != null)
            {
                chapterStarSlider.maxValue = 30;
                chapterStarSlider.value = totalStars;
            }

            UpdateMilestoneButtons(totalStars);
            UpdateStageNodesVisuals();
        }

        private void UpdateStageNodesVisuals()
        {
            if (StageManager.Instance == null || CitadelLobbyManager.Instance == null) return;
            var accData = CitadelLobbyManager.Instance.AccountData;
            Font defaultFont = Resources.Load<Font>("Fonts/LilitaOne-Regular");

            for (int i = 0; i < stageNodeButtons.Count; i++)
            {
                int stageIdx = i + 1;
                int stageId = (activeChapter * 100) + stageIdx;
                var btn = stageNodeButtons[i];
                if (btn == null) continue;

                var txt = btn.GetComponentInChildren<Text>();
                bool isUnlocked = (StageManager.Instance != null && StageManager.Instance.IsStageUnlocked(stageId)) ||
                                  stageIdx == 1 ||
                                  (accData.GetStageRecord(stageId - 1)?.isCleared ?? false);
                var record = accData.GetStageRecord(stageId);
                bool isCleared = record != null && record.isCleared;
                int stars = record != null ? record.starsEarned : 0;

                btn.interactable = isUnlocked;

                if (txt != null)
                {
                    string starStr;
                    if (isCleared)
                    {
                        if (stars >= 3)
                        {
                            starStr = "<color=#FFD700>★★★</color>"; // Radiant gold 3-star
                        }
                        else if (stars == 2)
                        {
                            starStr = "<color=#FFD700>★★</color><color=#555555>★</color>";
                        }
                        else
                        {
                            starStr = "<color=#FFD700>★</color><color=#555555>★★</color>";
                        }
                    }
                    else if (isUnlocked)
                    {
                        starStr = "<color=#00FFFF>READY</color>";
                    }
                    else
                    {
                        starStr = "<color=#666666>🔒 LOCK</color>";
                    }

                    txt.font = defaultFont;
                    string bossPrefix = (stageIdx == 5) ? "👑 1-5" : ((stageIdx == 10) ? "💀 1-10" : $"{stageIdx}");
                    txt.text = $"<b>{bossPrefix}</b>\n{starStr}";
                    txt.fontSize = (stageIdx == 5 || stageIdx == 10) ? 17 : 16;
                    txt.color = (stageIdx == 10) ? new Color(1f, 0.4f, 0.4f) : ((stageIdx == 5) ? new Color(1f, 0.85f, 0.3f) : Color.white);
                }
            }
        }

        private void UpdateMilestoneButtons(int currentStars)
        {
            if (claim10StarButton != null)
            {
                claim10StarButton.interactable = currentStars >= 10;
                var img = claim10StarButton.GetComponent<Image>();
                if (img != null && img.sprite != null)
                {
                    img.color = currentStars >= 10 ? Color.white : new Color(0.6f, 0.6f, 0.6f, 0.85f);
                }
            }
            if (claim20StarButton != null)
            {
                claim20StarButton.interactable = currentStars >= 20;
                var img = claim20StarButton.GetComponent<Image>();
                if (img != null && img.sprite != null)
                {
                    img.color = currentStars >= 20 ? Color.white : new Color(0.6f, 0.6f, 0.6f, 0.85f);
                }
            }
            if (claim30StarButton != null)
            {
                claim30StarButton.interactable = currentStars >= 30;
                var img = claim30StarButton.GetComponent<Image>();
                if (img != null && img.sprite != null)
                {
                    img.color = currentStars >= 30 ? Color.white : new Color(0.6f, 0.6f, 0.6f, 0.85f);
                }
            }
        }

        public void OnStageNodeClicked(int stageId)
        {
            if (StageManager.Instance == null) return;

            if (StageManager.Instance.TrySelectStage(stageId))
            {
                StageDataSO stage = StageManager.Instance.GetStageById(stageId);
                SelectStage(stage);

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySound(SoundType.ButtonClick);
                }
            }
            else
            {
                Debug.LogWarning($"[UIStageMapView] Stage {stageId} belum terbuka!");
            }
        }

        public void SelectStage(StageDataSO stage)
        {
            if (stage == null) return;
            currentStage = stage;

            if (stageDetailPanel != null)
            {
                stageDetailPanel.SetActive(true);
            }

            int playerCP = CitadelLobbyManager.Instance != null ? CitadelLobbyManager.Instance.TotalTeamCP : 0;
            int currentEnergy = CitadelLobbyManager.Instance != null ? CitadelLobbyManager.Instance.CurrentEnergy : 0;

            Font defaultFont = Resources.Load<Font>("Fonts/LilitaOne-Regular");
            if (defaultFont != null)
            {
                if (stageCodeText != null) stageCodeText.font = defaultFont;
                if (stageTitleText != null) stageTitleText.font = defaultFont;
                if (battleModeText != null) battleModeText.font = defaultFont;
                if (stageDescriptionText != null) stageDescriptionText.font = defaultFont;
                if (recommendedCpText != null) recommendedCpText.font = defaultFont;
                if (difficultyStatusText != null) difficultyStatusText.font = defaultFont;
                if (energyCostText != null) energyCostText.font = defaultFont;
                if (firstClearRewardsText != null) firstClearRewardsText.font = defaultFont;
                if (repeatRewardsText != null) repeatRewardsText.font = defaultFont;
                if (chapterTitleText != null) chapterTitleText.font = defaultFont;
                if (totalStarsText != null) totalStarsText.font = defaultFont;
                if (startBattleButton != null)
                {
                    var bTxt = startBattleButton.GetComponentInChildren<Text>();
                    if (bTxt != null) bTxt.font = defaultFont;
                }
            }

            if (stageCodeText != null) stageCodeText.text = $"<b>STAGE {stage.stageCode}</b>";
            if (stageTitleText != null) stageTitleText.text = $"<b>{stage.stageTitle.ToUpper()}</b>";

            if (battleModeText != null)
            {
                string modeIcon = (stage.battleMode == BattleModeType.BossRush) ? "💀" : ((stage.battleMode == BattleModeType.PuzzleOnly) ? "🧩" : "⚔️");
                battleModeText.text = $"{modeIcon} <color=#FFD700><b>{stage.GetBattleModeLabel().ToUpper()}</b></color>";
            }

            if (stageDescriptionText != null)
            {
                string modeTag = battleModeText == null ? $"\n<color=#FFD700><b>Mode:</b> {stage.GetBattleModeLabel()}</color>" : "";
                stageDescriptionText.text = $"{stage.stageDescription}{modeTag}";
            }

            if (recommendedCpText != null)
            {
                string superiorTag = FantasyDefense.Localization.CitadelLocalizationManager.Text("UNGGUL!", "SUPERIOR!");
                string alertTag = FantasyDefense.Localization.CitadelLocalizationManager.Text("WASPADA!", "WARNING!");
                string cpComparison = playerCP >= stage.recommendedCP 
                    ? $"<color=#00FF66><b>{playerCP:N0}</b> ({superiorTag})</color>" 
                    : $"<color=#FF7777><b>{playerCP:N0}</b> ({alertTag})</color>";

                string recLabel = FantasyDefense.Localization.CitadelLocalizationManager.Text("🛡️ CP BENTENG", "🛡️ CITADEL CP");
                string teamLabel = FantasyDefense.Localization.CitadelLocalizationManager.Text("TIM KAMU", "YOUR TEAM");
                recommendedCpText.text = $"{recLabel}: <b>{stage.recommendedCP:N0}</b>  |  {teamLabel}: {cpComparison}";
            }

            if (difficultyStatusText != null)
            {
                string diffTag = stage.GetDifficultyLabel(playerCP).ToUpper();
                string diffLabel = FantasyDefense.Localization.CitadelLocalizationManager.Text("⚡ KESULITAN", "⚡ DIFFICULTY");
                difficultyStatusText.text = $"{diffLabel}: <b>{diffTag}</b>";
                difficultyStatusText.color = stage.GetDifficultyColor(playerCP);
            }

            if (energyCostText != null)
            {
                string energyLabel = FantasyDefense.Localization.CitadelLocalizationManager.Text("⚡ BIAYA ENERGI", "⚡ ENERGY COST");
                string availLabel = FantasyDefense.Localization.CitadelLocalizationManager.Text("TERSEDIA", "AVAILABLE");
                energyCostText.text = $"{energyLabel}: <color=#00FFFF><b>{stage.staminaCost} STAMINA</b></color> ({availLabel}: <b>{currentEnergy}/30</b>)";
            }

            if (firstClearRewardsText != null)
            {
                var accData = CitadelLobbyManager.Instance != null ? CitadelLobbyManager.Instance.AccountData : null;
                bool isCleared = accData != null && accData.GetStageRecord(stage.stageId)?.isCleared == true;

                if (isCleared)
                {
                    string claimedLabel = FantasyDefense.Localization.CitadelLocalizationManager.Text("🎁 HADIAH PERTAMA: [SUDAH DIKLAIM]", "🎁 FIRST CLEAR REWARD: [CLAIMED]");
                    firstClearRewardsText.text = $"<color=#888888>{claimedLabel}</color>";
                }
                else
                {
                    string firstLabel = FantasyDefense.Localization.CitadelLocalizationManager.Text("🎁 HADIAH PERTAMA", "🎁 FIRST REWARD");
                    firstClearRewardsText.text = $"{firstLabel}: <color=#FFD700>🪙 +{stage.firstClearGoldReward:N0}</color>  |  <color=#00FFFF>💎 +{stage.firstClearGemReward:N0}</color>";
                }
            }

            if (repeatRewardsText != null)
            {
                string repeatLabel = FantasyDefense.Localization.CitadelLocalizationManager.Text("REWARD GRINDING", "REPEAT REWARD");
                repeatRewardsText.text = $"{repeatLabel}: 🪙 +{stage.repeatClearGold}  |  ⭐ +{stage.repeatClearExp} EXP  |  💎 +{stage.repeatClearGems}";
            }

            if (startBattleButton != null)
            {
                bool hasEnoughEnergy = currentEnergy >= stage.staminaCost;
                startBattleButton.interactable = true; // Selalu aktif agar pemain mendapat feedback jika stamina habis
                var txt = startBattleButton.GetComponentInChildren<Text>();
                if (txt != null)
                {
                    if (hasEnoughEnergy)
                    {
                        txt.text = FantasyDefense.Localization.CitadelLocalizationManager.Get("MAP_START_BATTLE", "⚔️ MULAI PERTEMPURAN SEKARANG", "⚔️ COMMENCE DEFENSE NOW");
                    }
                    else
                    {
                        txt.text = FantasyDefense.Localization.CitadelLocalizationManager.Text("⚡ STAMINA TIDAK CUKUP", "⚡ NOT ENOUGH ENERGY");
                    }
                }
            }
        }

        private void OnStartBattleClicked()
        {
            if (CitadelLobbyManager.Instance == null) return;

            // Fallback jika currentStage belum terisi
            if (currentStage == null && StageManager.Instance != null && StageManager.Instance.ChapterStages != null && StageManager.Instance.ChapterStages.Count > 0)
            {
                currentStage = StageManager.Instance.ChapterStages[0];
            }

            if (currentStage == null)
            {
                Debug.LogWarning("[UIStageMapView] Tidak ada stage yang dipilih untuk memulai pertempuran!");
                return;
            }

            if (!CitadelLobbyManager.Instance.CanStartBattle())
            {
                int currentEnergy = CitadelLobbyManager.Instance.CurrentEnergy;
                int cost = currentStage != null ? currentStage.staminaCost : 5;
                Debug.LogWarning($"[UIStageMapView] Stamina tidak cukup! {currentEnergy}/{cost}");

                string toastMsg = FantasyDefense.Localization.CitadelLocalizationManager.Text(
                    $"⚡ Stamina Tidak Cukup!\nMembutuhkan {cost} Stamina untuk bertempur. (Tersedia: {currentEnergy})",
                    $"⚡ Not Enough Energy!\nRequires {cost} Stamina to commence defense. (Available: {currentEnergy})"
                );
                string btnLabel = FantasyDefense.Localization.CitadelLocalizationManager.Text("💎 BUKA TOKO (REFILL)", "💎 OPEN STORE (REFILL)");
                CitadelToastNotice.ShowWithAction(toastMsg, btnLabel, () => {
                    if (FantasyDefense.Core.GameFlowManager.Instance != null)
                    {
                        FantasyDefense.Core.GameFlowManager.Instance.OpenPanel(FantasyDefense.Core.GamePanelType.Store);
                    }
                }, 4.2f);

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySound(SoundType.DefeatChime);
                }
                return;
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.ButtonClick);
            }

            Debug.Log($"[UIStageMapView] Meluncur ke pertempuran arena {currentStage.stageCode} ({currentStage.stageTitle})!");

            // Memulai inisialisasi arena battle
            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.StartBattle(currentStage);
            }
            else if (GameManager.Instance != null)
            {
                GameManager.Instance.StartBattleWithStage(currentStage);
            }

            if (stageDetailPanel != null)
            {
                stageDetailPanel.SetActive(false);
            }
        }

        private void ClaimMilestone(int milestone)
        {
            if (StageManager.Instance == null) return;

            bool success = StageManager.Instance.TryClaimStarMilestone(milestone, out string message);
            if (milestoneFeedbackText != null)
            {
                milestoneFeedbackText.text = message;
            }

            if (success && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.MergeSuccess);
            }

            RefreshMapView();
        }

        private void HandleStageSelected(StageDataSO stage)
        {
            SelectStage(stage);
        }

        private void HandleStageCompleted(StageDataSO stage, int stars, bool isFirstClear)
        {
            RefreshMapView();
        }

        private void HandleEnergyChanged(int current, int max)
        {
            if (currentStage != null)
            {
                SelectStage(currentStage);
            }
        }

        private void HandleTeamCPUpdated(int totalCP)
        {
            if (currentStage != null)
            {
                SelectStage(currentStage);
            }
        }
    }
}
