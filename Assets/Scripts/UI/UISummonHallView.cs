using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FantasyDefense.Audio;
using FantasyDefense.Combat;
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
            Font defaultFont = Resources.Load<Font>("Fonts/LilitaOne-Regular");
            if (defaultFont != null)
            {
                if (gemsBalanceText != null) gemsBalanceText.font = defaultFont;
                if (bannerTitleText != null) bannerTitleText.font = defaultFont;
                if (bannerSubtitleText != null) bannerSubtitleText.font = defaultFont;
                if (singleSummonCostText != null) singleSummonCostText.font = defaultFont;
                if (multiSummonCostText != null) multiSummonCostText.font = defaultFont;
                if (heroNameText != null) heroNameText.font = defaultFont;
                if (heroStarRankText != null) heroStarRankText.font = defaultFont;
                if (shardProgressText != null) shardProgressText.font = defaultFont;
                if (promoteStatusText != null) promoteStatusText.font = defaultFont;
                if (resultSummaryText != null) resultSummaryText.font = defaultFont;
                if (singleSummonButton != null)
                {
                    var t = singleSummonButton.GetComponentInChildren<Text>();
                    if (t != null) t.font = defaultFont;
                }
                if (multiSummonButton != null)
                {
                    var t = multiSummonButton.GetComponentInChildren<Text>();
                    if (t != null) t.font = defaultFont;
                }
                if (promoteStarButton != null)
                {
                    var t = promoteStarButton.GetComponentInChildren<Text>();
                    if (t != null) t.font = defaultFont;
                }
                if (closeResultButton != null)
                {
                    var t = closeResultButton.GetComponentInChildren<Text>();
                    if (t != null) t.font = defaultFont;
                }
            }

            // 1. Saldo Gems
            if (gemsBalanceText != null && CitadelLobbyManager.Instance != null)
            {
                gemsBalanceText.text = $"💎 {CitadelLobbyManager.Instance.AccountGems:N0} GEMS";
            }

            // 2. Banner Text
            if (bannerTitleText != null)
            {
                bannerTitleText.text = "★ ALTAR PEMANGGILAN CITADEL ★";
            }
            if (bannerSubtitleText != null)
            {
                bannerSubtitleText.text = "Rate-Up Pahlawan Baru: Wind Ranger (Pemanah Angin)!";
            }

            // 3. Tombol Biaya
            if (singleSummonCostText != null) singleSummonCostText.text = "💎 100 GEMS";
            if (multiSummonCostText != null) multiSummonCostText.text = "💎 900 GEMS (HEMAT 10%)";

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

            // Bersihkan kartu-kartu hasil sebelumnya di dalam container jika ada
            if (resultCardsContainer != null)
            {
                for (int i = resultCardsContainer.childCount - 1; i >= 0; i--)
                {
                    Destroy(resultCardsContainer.GetChild(i).gameObject);
                }

                // Bangun kartu visual dinamis untuk setiap item hasil gacha
                var lilitaFont = Resources.Load<Font>("Fonts/LilitaOne-Regular");
                int cardIndex = 0;
                foreach (var item in results)
                {
                    var card = CreateGachaResultCard(resultCardsContainer, item, lilitaFont, out var frontObj, out var backObj);
                    if (card != null && gameObject.activeInHierarchy)
                    {
                        StartCoroutine(AnimateCardRevealRoutine(card.transform, cardIndex * 0.10f, item, frontObj, backObj));
                    }
                    cardIndex++;
                }
            }

            if (resultSummaryText != null)
            {
                resultSummaryText.text = FantasyDefense.Localization.CitadelLocalizationManager.Text(
                    "✨ HASIL PEMANGGILAN CITADEL ✨",
                    "✨ CITADEL SUMMON RESULTS ✨"
                );
            }

            UpdateAllDisplays();
        }

        private GameObject CreateGachaResultCard(Transform parent, GachaResultItem item, Font font, out GameObject frontObj, out GameObject backObj)
        {
            var cardObj = new GameObject($"Card_{item.unitType}", typeof(RectTransform));
            cardObj.transform.SetParent(parent, false);

            // 1. Tampilan Kartu Belakang (Card Back - Misterius Runic)
            backObj = new GameObject("CardBack", typeof(RectTransform), typeof(Image));
            backObj.transform.SetParent(cardObj.transform, false);
            var backRt = backObj.GetComponent<RectTransform>();
            backRt.anchorMin = Vector2.zero;
            backRt.anchorMax = Vector2.one;
            backRt.offsetMin = Vector2.zero;
            backRt.offsetMax = Vector2.zero;

            var backImg = backObj.GetComponent<Image>();
            var stoneFrame = Resources.Load<Sprite>("Sprites/UI/Frame_StoneBastion_9Slice")
#if UNITY_EDITOR
                          ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Frame_StoneBastion_9Slice.png")
#endif
                          ;
            if (stoneFrame != null)
            {
                backImg.sprite = stoneFrame;
                backImg.type = Image.Type.Sliced;
                backImg.color = new Color(0.85f, 0.90f, 1f, 1f);
            }
            else
            {
                backImg.color = new Color(0.12f, 0.16f, 0.24f, 0.98f);
            }

            // Simbol runik misterius di bagian belakang kartu
            var runeObj = new GameObject("RuneGlyph", typeof(RectTransform), typeof(Text));
            runeObj.transform.SetParent(backObj.transform, false);
            var runeTxt = runeObj.GetComponent<Text>();
            if (font != null) runeTxt.font = font;
            runeTxt.text = "◈\n✦";
            runeTxt.fontSize = 28;
            runeTxt.alignment = TextAnchor.MiddleCenter;
            runeTxt.color = new Color(1f, 0.85f, 0.35f, 0.85f);
            var runeRt = runeObj.GetComponent<RectTransform>();
            runeRt.anchorMin = Vector2.zero;
            runeRt.anchorMax = Vector2.one;
            runeRt.offsetMin = Vector2.zero;
            runeRt.offsetMax = Vector2.zero;

            // 2. Tampilan Kartu Depan (Card Front)
            frontObj = new GameObject("CardFront", typeof(RectTransform), typeof(Image));
            frontObj.transform.SetParent(cardObj.transform, false);
            var frontRt = frontObj.GetComponent<RectTransform>();
            frontRt.anchorMin = Vector2.zero;
            frontRt.anchorMax = Vector2.one;
            frontRt.offsetMin = Vector2.zero;
            frontRt.offsetMax = Vector2.zero;

            var img = frontObj.GetComponent<Image>();
            img.color = new Color(0.08f, 0.10f, 0.16f, 0.95f);

            // Tentukan sprite border & warna kelangkaan
            string borderPath = "Border_Rarity_Common";
            Color rarityColor = new Color(0.7f, 0.7f, 0.7f);
            if (item.rarityLabel.Contains("Legendary") || item.unitType == UnitType.WindRanger)
            {
                borderPath = "Border_Rarity_Legendary";
                rarityColor = new Color(1f, 0.85f, 0.2f);
            }
            else if (item.rarityLabel.Contains("Epic") || item.unitType == UnitType.RabbitMage)
            {
                borderPath = "Border_Rarity_Epic";
                rarityColor = new Color(0.8f, 0.4f, 1f);
            }
            else
            {
                borderPath = "Border_Rarity_Rare";
                rarityColor = new Color(0.25f, 0.75f, 1f);
            }

            var borderSprite = Resources.Load<Sprite>($"Sprites/UI/{borderPath}")
#if UNITY_EDITOR
                            ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/Sprites/UI/{borderPath}.png")
#endif
                            ;
            if (borderSprite != null)
            {
                img.sprite = borderSprite;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
            }

            // Portrait Hero
            var portObj = new GameObject("HeroPortrait", typeof(RectTransform), typeof(Image));
            portObj.transform.SetParent(frontObj.transform, false);
            var portImg = portObj.GetComponent<Image>();
            string sName = item.unitType switch
            {
                UnitType.Cedric => "Cedric_T1",
                UnitType.RoseTanker => "Rose_T1",
                UnitType.RabbitMage => "RabbitMage_T1",
                UnitType.WindRanger => "WindRanger_T1",
                _ => "Cedric_T1"
            };
            var hSprite = Resources.Load<Sprite>($"Sprites/Heroes/{sName}")
#if UNITY_EDITOR
                       ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/Sprites/Heroes/{sName}.png")
#endif
                       ;
            if (hSprite != null)
            {
                portImg.sprite = hSprite;
                portImg.preserveAspect = true;
            }
            var pRt = portObj.GetComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0.1f, 0.32f);
            pRt.anchorMax = new Vector2(0.9f, 0.88f);
            pRt.offsetMin = Vector2.zero;
            pRt.offsetMax = Vector2.zero;

            // Label Rarity & Nama
            var nameObj = new GameObject("HeroName", typeof(RectTransform), typeof(Text));
            nameObj.transform.SetParent(frontObj.transform, false);
            var nameTxt = nameObj.GetComponent<Text>();
            if (font != null) nameTxt.font = font;
            nameTxt.text = item.heroName.Split('(')[0].Trim();
            nameTxt.fontSize = 18;
            nameTxt.fontStyle = FontStyle.Bold;
            nameTxt.alignment = TextAnchor.MiddleCenter;
            nameTxt.color = rarityColor;
            var nRt = nameObj.GetComponent<RectTransform>();
            nRt.anchorMin = new Vector2(0.05f, 0.16f);
            nRt.anchorMax = new Vector2(0.95f, 0.32f);
            nRt.offsetMin = Vector2.zero;
            nRt.offsetMax = Vector2.zero;

            // Status Duplikat / Unlock
            var statObj = new GameObject("CardStatus", typeof(RectTransform), typeof(Text));
            statObj.transform.SetParent(frontObj.transform, false);
            var statTxt = statObj.GetComponent<Text>();
            if (font != null) statTxt.font = font;
            statTxt.text = item.isNewUnlock ? "★ UNLOCKED!" : $"+{item.shardsGained} Shards";
            statTxt.fontSize = 16;
            statTxt.fontStyle = FontStyle.Normal;
            statTxt.alignment = TextAnchor.MiddleCenter;
            statTxt.color = item.isNewUnlock ? new Color(1f, 0.95f, 0.3f) : new Color(0.75f, 0.95f, 0.85f);
            var sRt = statObj.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0.05f, 0.02f);
            sRt.anchorMax = new Vector2(0.95f, 0.16f);
            sRt.offsetMin = Vector2.zero;
            sRt.offsetMax = Vector2.zero;

            // Di awal, kartu depan disembunyikan agar yang tampil kartu belakang
            frontObj.SetActive(false);
            backObj.SetActive(true);

            return cardObj;
        }

        private System.Collections.IEnumerator AnimateCardRevealRoutine(Transform cardTr, float delay, GachaResultItem item, GameObject frontObj, GameObject backObj)
        {
            if (cardTr == null) yield break;

            cardTr.localScale = Vector3.one;
            if (frontObj != null) frontObj.SetActive(false);
            if (backObj != null) backObj.SetActive(true);

            if (delay > 0f)
            {
                yield return new WaitForSecondsRealtime(delay);
            }

            bool isLegendary = item.rarityLabel.Contains("Legendary") || item.unitType == UnitType.WindRanger;
            bool isEpic = item.rarityLabel.Contains("Epic") || item.unitType == UnitType.RabbitMage;

            // Fase 1: Membalik kartu belakang horizontal ke tengah (1.0 -> 0.0)
            float halfDuration = 0.16f;
            float elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                float scaleX = Mathf.Cos(t * Mathf.PI * 0.5f);
                cardTr.localScale = new Vector3(scaleX, 1f, 1f);
                yield return null;
            }

            // Titik balik: Tukar kartu belakang menjadi kartu depan yang terungkap
            if (backObj != null) backObj.SetActive(false);
            if (frontObj != null) frontObj.SetActive(true);

            // Umpan balik audio & partikel kilau sesuai kelangkaan
            if (AudioManager.Instance != null)
            {
                if (isLegendary)
                {
                    AudioManager.Instance.PlaySound(SoundType.VictoryFanfare);
                }
                else if (isEpic)
                {
                    AudioManager.Instance.PlaySound(SoundType.MergeSuccess, 1.35f);
                }
                else
                {
                    AudioManager.Instance.PlaySound(SoundType.ButtonClick, 1.25f);
                }
            }

            if (SkillVfxManager.Instance != null)
            {
                Color burstColor = isLegendary ? new Color(1f, 0.85f, 0.2f) : (isEpic ? new Color(0.8f, 0.4f, 1f) : new Color(0.25f, 0.75f, 1f));
                int particleCount = isLegendary ? 22 : (isEpic ? 16 : 10);
                SkillVfxManager.Instance.SpawnProceduralParticleBurst(cardTr.position, burstColor, Color.white, particleCount, 1.8f);
            }

            // Fase 2: Membuka kartu depan dari 0.0 -> 1.0 dengan pantulan elastis (overshoot bounce)
            float revealDuration = 0.22f;
            elapsed = 0f;
            float overshoot = isLegendary ? 0.35f : (isEpic ? 0.22f : 0.12f);
            while (elapsed < revealDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / revealDuration);
                float scaleX = Mathf.Sin(t * Mathf.PI * 0.5f);
                float bounce = Mathf.Sin(t * Mathf.PI) * overshoot;

                cardTr.localScale = new Vector3(scaleX + bounce * 0.4f, 1f + bounce, 1f);
                yield return null;
            }

            cardTr.localScale = Vector3.one;
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
