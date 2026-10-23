using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FantasyDefense.Audio;
using FantasyDefense.Data;
using FantasyDefense.Metagame;

namespace FantasyDefense.UI
{
    /// <summary>
    /// Master UI Controller untuk Antarmuka Balai Pemanggilan Pahlawan (Citadel Summoning Hall):
    /// 1. Tampilan Banner Gacha Spesial: Rate-Up Wind Ranger (Pemanah Angin)
    /// 2. Tombol Pemanggilan Tunggal (100 Gems) & 10x Pemanggilan (900 Gems)
    /// 3. Panel Promosi Bintang Pahlawan (Star Promotion 1★ - 5★ via Hero Shards)
    /// 4. Modal Pop-up Hasil Gacha dengan kartu pahlawan dan perolehan Shards
    /// </summary>
    public class UISummonHallView : MonoBehaviour
    {
        public static UISummonHallView Instance { get; private set; }

        [Header("Root & Navigation")]
        [SerializeField] private GameObject rootPanel;
        [SerializeField] private Button closeButton;

        [Header("Citadel Economy Header")]
        [SerializeField] private Text gemsBalanceText;

        [Header("Banner Presentation")]
        [SerializeField] private Text bannerTitleText;
        [SerializeField] private Text bannerSubtitleText;
        [SerializeField] private Image featuredHeroImage;

        [Header("Summon Action Buttons")]
        [SerializeField] private Button singleSummonButton;
        [SerializeField] private Text singleSummonCostText;
        [SerializeField] private Button multiSummonButton;
        [SerializeField] private Text multiSummonCostText;

        [Header("Star Promotion Panel")]
        [SerializeField] private Button[] heroTabButtons;
        [SerializeField] private Text heroNameText;
        [SerializeField] private Text heroStarRankText;
        [SerializeField] private Slider shardProgressSlider;
        [SerializeField] private Text shardProgressText;
        [SerializeField] private Button promoteStarButton;
        [SerializeField] private Text promoteButtonText;

        [Header("Gacha Results Modal")]
        [SerializeField] private GameObject resultModalPanel;
        [SerializeField] private Transform resultCardsContainer;
        [SerializeField] private Text resultSummaryText;
        [SerializeField] private Button closeResultButton;

        private UnitType currentSelectedHero = UnitType.WindRanger;

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
            CitadelLobbyManager.OnAccountGemsChanged += HandleGemsChanged;
            CitadelLobbyManager.OnHeroStarPromoted += HandleHeroStarPromoted;
            CitadelSummonManager.OnGachaPullsCompleted += ShowGachaResults;

            UpdateAllDisplays();
        }

        private void OnDisable()
        {
            CitadelLobbyManager.OnAccountGemsChanged -= HandleGemsChanged;
            CitadelLobbyManager.OnHeroStarPromoted -= HandleHeroStarPromoted;
            CitadelSummonManager.OnGachaPullsCompleted -= ShowGachaResults;
        }

        private void Start()
        {
            UpdateAllDisplays();
            if (resultModalPanel != null)
            {
                resultModalPanel.SetActive(false);
            }
        }

        private void InitializeButtons()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(CloseView);

            if (singleSummonButton != null)
                singleSummonButton.onClick.AddListener(OnSingleSummonClicked);

            if (multiSummonButton != null)
                multiSummonButton.onClick.AddListener(OnMultiSummonClicked);

            if (promoteStarButton != null)
                promoteStarButton.onClick.AddListener(OnPromoteStarClicked);

            if (closeResultButton != null)
                closeResultButton.onClick.AddListener(OnCloseResultsClicked);
        }

        public void OpenView()
        {
            if (rootPanel != null) rootPanel.SetActive(true);
            else gameObject.SetActive(true);

            transform.SetAsLastSibling();

            var cg = GetComponent<CanvasGroup>() ?? rootPanel?.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 1f;
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }

            UpdateAllDisplays();
        }

        public void CloseView()
        {
            if (rootPanel != null) rootPanel.SetActive(false);
            else gameObject.SetActive(false);

            if (FantasyDefense.Core.GameFlowManager.Instance != null)
            {
                FantasyDefense.Core.GameFlowManager.Instance.CloseCurrentPanel();
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.ButtonClick);
            }
        }

        public void SelectHeroTab(int heroIndex)
        {
            switch (heroIndex)
            {
                case 0: currentSelectedHero = UnitType.Cedric; break;
                case 1: currentSelectedHero = UnitType.RoseTanker; break;
                case 2: currentSelectedHero = UnitType.RabbitMage; break;
                case 3: currentSelectedHero = UnitType.WindRanger; break;
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.ButtonClick);
            }

            UpdateStarPromotionDisplay();
        }

        private void UpdateAllDisplays()
        {
            // 1. Saldo Gems
            if (gemsBalanceText != null && CitadelLobbyManager.Instance != null)
            {
                gemsBalanceText.text = $"{CitadelLobbyManager.Instance.AccountGems} Gems";
            }

            // 2. Banner Text
            if (bannerTitleText != null)
            {
                bannerTitleText.text = "★ PANGGILAN BADAI PURBA ★";
            }
            if (bannerSubtitleText != null)
            {
                bannerSubtitleText.text = "Rate-Up Pahlawan Baru: Wind Ranger (Pemanah Angin)!";
            }

            // 3. Tombol Biaya
            if (singleSummonCostText != null) singleSummonCostText.text = "100 Gems";
            if (multiSummonCostText != null) multiSummonCostText.text = "900 Gems (Hemat 10%)";

            UpdateStarPromotionDisplay();
        }

        private void UpdateStarPromotionDisplay()
        {
            if (CitadelLobbyManager.Instance == null) return;

            var profile = CitadelLobbyManager.Instance.GetHeroProfile(currentSelectedHero);
            if (profile == null) return;

            if (heroNameText != null)
            {
                heroNameText.text = profile.heroName;
            }

            if (heroStarRankText != null)
            {
                string starString = new string('★', profile.starRank) + new string('☆', 5 - profile.starRank);
                heroStarRankText.text = $"Tingkat: {starString} ({profile.starRank}★) [Stat Multiplier: x{profile.GetStarMultiplier():0.00}]";
            }

            int reqShards = profile.GetShardsRequiredForPromotion();
            bool isMaxStar = profile.starRank >= 5;

            if (shardProgressSlider != null)
            {
                shardProgressSlider.maxValue = isMaxStar ? 1 : reqShards;
                shardProgressSlider.value = isMaxStar ? 1 : profile.heroShards;
            }

            if (shardProgressText != null)
            {
                shardProgressText.text = isMaxStar ? "BINTANG MAKSIMAL" : $"{profile.heroShards} / {reqShards} Shards";
            }

            if (promoteStarButton != null)
            {
                promoteStarButton.interactable = profile.CanPromoteStar();
            }

            if (promoteButtonText != null)
            {
                promoteButtonText.text = isMaxStar ? "Maksimal" : $"Promosikan ke {profile.starRank + 1}★";
            }
        }

        private void OnSingleSummonClicked()
        {
            if (CitadelSummonManager.Instance != null)
            {
                CitadelSummonManager.Instance.TrySingleSummon(out _);
            }
        }

        private void OnMultiSummonClicked()
        {
            if (CitadelSummonManager.Instance != null)
            {
                CitadelSummonManager.Instance.TryMultiSummon(out _);
            }
        }

        private void OnPromoteStarClicked()
        {
            if (CitadelLobbyManager.Instance != null)
            {
                bool success = CitadelLobbyManager.Instance.TryPromoteHeroStar(currentSelectedHero, out string msg);
                Debug.Log($"[UISummonHallView] {msg}");
                UpdateStarPromotionDisplay();
            }
        }

        private void ShowGachaResults(List<GachaResultItem> results)
        {
            if (resultModalPanel != null)
            {
                resultModalPanel.SetActive(true);
            }

            if (resultSummaryText != null)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine("=== HASIL PEMANGGILAN CITADEL ===");
                foreach (var item in results)
                {
                    string status = item.isNewUnlock ? "[HERO BARU DIBUKA!]" : $"(+{item.shardsGained} Shards)";
                    sb.AppendLine($"• {item.heroName} [{item.rarityLabel}] {status}");
                }
                resultSummaryText.text = sb.ToString();
            }

            UpdateAllDisplays();
        }

        private void OnCloseResultsClicked()
        {
            if (resultModalPanel != null)
            {
                resultModalPanel.SetActive(false);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.ButtonClick);
            }
        }

        private void HandleGemsChanged(int newGems)
        {
            if (gemsBalanceText != null)
            {
                gemsBalanceText.text = $"{newGems} Gems";
            }
        }

        private void HandleHeroStarPromoted(HeroLobbyProfile profile)
        {
            UpdateStarPromotionDisplay();
        }
    }
}
