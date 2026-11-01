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
        [SerializeField] private Text pityProgressText;
        [SerializeField] private Button gachaRateInfoButton;

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
        [SerializeField] private Button skipAnimationButton;

        [Header("Gacha Drop Rate Modal")]
        [SerializeField] private GameObject rateModalPanel;

        private UnitType currentSelectedHero = UnitType.WindRanger;
        private bool isSkippingRevealAnimation = false;
        private List<Coroutine> activeRevealCoroutines = new List<Coroutine>();
        private List<System.Action> cardInstantRevealers = new List<System.Action>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeButtons();
            ApplySummonVisualStyling();
        }

        private void ApplySummonVisualStyling()
        {
            var bannerSprite = Resources.Load<Sprite>("Sprites/UI/UI_Gacha_Banner_WindRanger");
            var btnSingleSprite = Resources.Load<Sprite>("Sprites/UI/UI_Btn_Gacha_Single");
            var btnMultiSprite = Resources.Load<Sprite>("Sprites/UI/UI_Btn_Gacha_Multi");

            // 1. Upgrade Altar Banner Panel
            var bannerPanel = transform.Find("ContentContainer/AltarBannerPanel")
                           ?? transform.Find("AltarBannerPanel");
            if (bannerPanel != null)
            {
                var bImg = bannerPanel.GetComponent<Image>();
                if (bImg != null && bannerSprite != null)
                {
                    bImg.sprite = bannerSprite;
                    bImg.color = Color.white;
                }
                var bOutline = bannerPanel.GetComponent<Outline>() ?? bannerPanel.gameObject.AddComponent<Outline>();
                bOutline.effectColor = new Color(0.85f, 0.72f, 0.28f, 0.75f);
                bOutline.effectDistance = new Vector2(2f, -2f);
            }

            // 2. Upgrade Single Summon Button
            if (singleSummonButton != null)
            {
                var sImg = singleSummonButton.GetComponent<Image>();
                if (sImg != null && btnSingleSprite != null)
                {
                    sImg.sprite = btnSingleSprite;
                    sImg.type = Image.Type.Sliced;
                    sImg.color = Color.white;
                }
                var sOutline = singleSummonButton.GetComponent<Outline>() ?? singleSummonButton.gameObject.AddComponent<Outline>();
                sOutline.effectColor = new Color(0.35f, 0.65f, 0.85f, 0.90f);
                sOutline.effectDistance = new Vector2(1.5f, -1.5f);
            }

            // 3. Upgrade Multi Summon Button (Crown CTA)
            if (multiSummonButton != null)
            {
                var mImg = multiSummonButton.GetComponent<Image>();
                if (mImg != null && btnMultiSprite != null)
                {
                    mImg.sprite = btnMultiSprite;
                    mImg.type = Image.Type.Sliced;
                    mImg.color = Color.white;
                }
                var mOutline = multiSummonButton.GetComponent<Outline>() ?? multiSummonButton.gameObject.AddComponent<Outline>();
                mOutline.effectColor = new Color(1.0f, 0.85f, 0.25f, 1.0f);
                mOutline.effectDistance = new Vector2(2.0f, -2.0f);
            }

            // 4. Upgrade Rate Info Button
            if (gachaRateInfoButton != null)
            {
                var rOutline = gachaRateInfoButton.GetComponent<Outline>() ?? gachaRateInfoButton.gameObject.AddComponent<Outline>();
                rOutline.effectColor = new Color(0.9f, 0.75f, 0.25f, 0.85f);
                rOutline.effectDistance = new Vector2(1.2f, -1.2f);
            }
        }

        private void OnEnable()
        {
            CitadelLobbyManager.OnAccountGemsChanged += HandleGemsChanged;
            CitadelLobbyManager.OnHeroStarPromoted += HandleHeroStarPromoted;
            CitadelSummonManager.OnGachaPullsCompleted += ShowGachaResults;
            CitadelSummonManager.OnPityCounterChanged += HandlePityCounterChanged;
            FantasyDefense.Localization.CitadelLocalizationManager.OnLanguageChanged += HandleLanguageChanged;

            UpdateAllDisplays();
        }

        private void OnDisable()
        {
            CitadelLobbyManager.OnAccountGemsChanged -= HandleGemsChanged;
            CitadelLobbyManager.OnHeroStarPromoted -= HandleHeroStarPromoted;
            CitadelSummonManager.OnGachaPullsCompleted -= ShowGachaResults;
            CitadelSummonManager.OnPityCounterChanged -= HandlePityCounterChanged;
            FantasyDefense.Localization.CitadelLocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
        }

        private void HandleLanguageChanged(FantasyDefense.Localization.AppLanguage lang)
        {
            UpdateAllDisplays();
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
            if (closeButton == null)
            {
                closeButton = transform.Find("HeaderBanner/CloseButton")?.GetComponent<Button>()
                           ?? transform.Find("CloseButton")?.GetComponent<Button>()
                           ?? GetComponentInChildren<Button>(true);
            }
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(CloseView);
                closeButton.onClick.AddListener(CloseView);
                closeButton.transform.SetAsLastSibling();
            }

            if (singleSummonButton != null)
                singleSummonButton.onClick.AddListener(OnSingleSummonClicked);

            if (multiSummonButton != null)
                multiSummonButton.onClick.AddListener(OnMultiSummonClicked);

            if (promoteStarButton != null)
                promoteStarButton.onClick.AddListener(OnPromoteStarClicked);

            if (closeResultButton != null)
                closeResultButton.onClick.AddListener(OnCloseResultsClicked);

            if (skipAnimationButton != null)
                skipAnimationButton.onClick.AddListener(OnSkipRevealClicked);

            if (gachaRateInfoButton != null)
                gachaRateInfoButton.onClick.AddListener(ToggleRateModal);
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

            ApplySummonVisualStyling();
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
                if (promoteButtonText != null) promoteButtonText.font = defaultFont;
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
                bannerTitleText.text = FantasyDefense.Localization.CitadelLocalizationManager.Text(
                    "★ ALTAR PEMANGGILAN CITADEL ★",
                    "★ CITADEL SUMMONING ALTAR ★"
                );
            }
            if (bannerSubtitleText != null)
            {
                bannerSubtitleText.text = FantasyDefense.Localization.CitadelLocalizationManager.Text(
                    "Rate-Up Pahlawan Baru: Wind Ranger (Pemanah Angin)!",
                    "New Hero Rate-Up: Wind Ranger (Wind Archer)!"
                );
            }

            // 2b. Pity Counter Display
            UpdatePityDisplay();

            // 3. Tombol Biaya & Label
            string singleLabel = FantasyDefense.Localization.CitadelLocalizationManager.Text("PANGGIL 1x", "SUMMON 1x");
            string multiLabel = FantasyDefense.Localization.CitadelLocalizationManager.Text("★ PANGGIL 10x ★", "★ SUMMON 10x ★");
            string discountTag = FantasyDefense.Localization.CitadelLocalizationManager.Text("HEMAT 10%", "SAVE 10%");

            if (singleSummonCostText != null)
            {
                singleSummonCostText.text = $"<b>{singleLabel}</b>\n<size=17><color=#60E6FF>💎 100 GEMS</color></size>";
            }
            if (multiSummonCostText != null)
            {
                multiSummonCostText.text = $"<b>{multiLabel}</b>\n<size=17><color=#FFF066>💎 900 GEMS</color> <color=#FFD54F><i>({discountTag})</i></color></size>";
            }

            UpdateStarPromotionDisplay();
        }

        private void UpdatePityDisplay()
        {
            if (pityProgressText == null) return;

            int currentPity = CitadelSummonManager.Instance != null ? CitadelSummonManager.Instance.CurrentPityCount : 0;
            int threshold = CitadelSummonManager.PITY_GUARANTEE_THRESHOLD;
            int remaining = Mathf.Max(0, threshold - currentPity);

            pityProgressText.text = FantasyDefense.Localization.CitadelLocalizationManager.Text(
                $"🛡️ <b>JAMINAN LEGENDARY</b>: {remaining} tarikan lagi ({currentPity}/{threshold})",
                $"🛡️ <b>GUARANTEED LEGENDARY</b>: {remaining} pulls left ({currentPity}/{threshold})"
            );
            pityProgressText.color = new Color(1.0f, 0.90f, 0.40f, 1f);

            var pityOutline = pityProgressText.GetComponent<Outline>() ?? pityProgressText.gameObject.AddComponent<Outline>();
            pityOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            pityOutline.effectDistance = new Vector2(1.2f, -1.2f);
        }

        private void HandlePityCounterChanged(int newPity)
        {
            UpdatePityDisplay();
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
                string rankLabel = FantasyDefense.Localization.CitadelLocalizationManager.Text("Tingkat", "Rank");
                heroStarRankText.text = $"{rankLabel}: {starString} ({profile.starRank}★) [Stat Multiplier: x{profile.GetStarMultiplier():0.00}]";
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
                string maxLabel = FantasyDefense.Localization.CitadelLocalizationManager.Text("BINTANG MAKSIMAL", "MAX RANK");
                shardProgressText.text = isMaxStar ? maxLabel : $"{profile.heroShards} / {reqShards} Shards";
            }

            if (promoteStarButton != null)
            {
                promoteStarButton.interactable = profile.CanPromoteStar();
            }

            if (promoteButtonText != null)
            {
                string maxBtn = FantasyDefense.Localization.CitadelLocalizationManager.Text("Maksimal", "Max Rank");
                string promoPrefix = FantasyDefense.Localization.CitadelLocalizationManager.Text("Promosikan ke", "Promote to");
                promoteButtonText.text = isMaxStar ? maxBtn : $"{promoPrefix} {profile.starRank + 1}★";
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
            EnsureResultsModalComponents();

            isSkippingRevealAnimation = false;
            foreach (var c in activeRevealCoroutines)
            {
                if (c != null) StopCoroutine(c);
            }
            activeRevealCoroutines.Clear();
            cardInstantRevealers.Clear();

            if (resultModalPanel != null)
            {
                resultModalPanel.SetActive(true);
            }

            if (skipAnimationButton != null)
            {
                skipAnimationButton.gameObject.SetActive(results.Count > 1);
            }

            // Bersihkan kartu-kartu hasil sebelumnya di dalam container jika ada
            if (resultCardsContainer != null)
            {
                for (int i = resultCardsContainer.childCount - 1; i >= 0; i--)
                {
                    Destroy(resultCardsContainer.GetChild(i).gameObject);
                }

                // Penyesuaian grid responsif
                var gridLayout = resultCardsContainer.GetComponent<GridLayoutGroup>();
                if (gridLayout != null)
                {
                    if (results.Count == 1)
                    {
                        gridLayout.constraint = GridLayoutGroup.Constraint.FixedRowCount;
                        gridLayout.constraintCount = 1;
                        gridLayout.cellSize = new Vector2(240, 340);
                    }
                    else
                    {
                        gridLayout.constraint = GridLayoutGroup.Constraint.FixedRowCount;
                        gridLayout.constraintCount = 2;
                        gridLayout.cellSize = new Vector2(160, 230);
                    }
                }

                // Bangun kartu visual dinamis untuk setiap item hasil gacha
                var lilitaFont = Resources.Load<Font>("Fonts/LilitaOne-Regular");
                int cardIndex = 0;
                foreach (var item in results)
                {
                    var card = CreateGachaResultCard(resultCardsContainer, item, lilitaFont, out var frontObj, out var backObj);
                    if (card != null && gameObject.activeInHierarchy)
                    {
                        var coroutine = StartCoroutine(AnimateCardRevealRoutine(card.transform, cardIndex * 0.12f, item, frontObj, backObj));
                        activeRevealCoroutines.Add(coroutine);
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

        private void OnSkipRevealClicked()
        {
            if (isSkippingRevealAnimation) return;
            isSkippingRevealAnimation = true;

            foreach (var c in activeRevealCoroutines)
            {
                if (c != null) StopCoroutine(c);
            }
            activeRevealCoroutines.Clear();

            foreach (var revealer in cardInstantRevealers)
            {
                revealer?.Invoke();
            }
            cardInstantRevealers.Clear();

            if (skipAnimationButton != null)
            {
                skipAnimationButton.gameObject.SetActive(false);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.ButtonClick, 1.2f);
            }
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
            Color rarityColor = CitadelRarityColors.Common;
            if (item.rarityLabel.Contains("Legendary") || item.unitType == UnitType.WindRanger)
            {
                borderPath = "Border_Rarity_Legendary";
                rarityColor = CitadelRarityColors.Legendary;
            }
            else if (item.rarityLabel.Contains("Epic") || item.unitType == UnitType.RabbitMage)
            {
                borderPath = "Border_Rarity_Epic";
                rarityColor = CitadelRarityColors.Epic;
            }
            else
            {
                borderPath = "Border_Rarity_Rare";
                rarityColor = CitadelRarityColors.Rare;
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

            // Daftarkan aksi instant reveal jika tombol Skip ditekan
            var localCard = cardObj;
            var localBack = backObj;
            var localFront = frontObj;
            cardInstantRevealers.Add(() =>
            {
                if (localCard != null)
                {
                    localCard.transform.localScale = Vector3.one;
                    if (localBack != null) localBack.SetActive(false);
                    if (localFront != null) localFront.SetActive(true);
                }
            });

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
                Color burstColor = isLegendary ? CitadelRarityColors.Legendary : (isEpic ? CitadelRarityColors.Epic : CitadelRarityColors.Rare);
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

        public void ToggleRateModal()
        {
            EnsureRateModalBuilt();
            if (rateModalPanel != null)
            {
                bool nextState = !rateModalPanel.activeSelf;
                rateModalPanel.SetActive(nextState);
                if (nextState) rateModalPanel.transform.SetAsLastSibling();
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.ButtonClick);
            }
        }

        private void EnsureRateModalBuilt()
        {
            if (rateModalPanel != null) return;

            var lilitaFont = Resources.Load<Font>("Fonts/LilitaOne-Regular");
            var parentCanvas = transform.Find("ContentContainer") ?? transform;

            rateModalPanel = new GameObject("GachaRateModalPanel", typeof(RectTransform), typeof(Image));
            rateModalPanel.transform.SetParent(parentCanvas, false);

            var rt = rateModalPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.08f, 0.18f);
            rt.anchorMax = new Vector2(0.92f, 0.82f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = rateModalPanel.GetComponent<Image>();
            var stoneFrame = Resources.Load<Sprite>("Sprites/UI/Frame_StoneBastion_9Slice")
#if UNITY_EDITOR
                          ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Frame_StoneBastion_9Slice.png")
#endif
                          ;
            if (stoneFrame != null)
            {
                img.sprite = stoneFrame;
                img.type = Image.Type.Sliced;
                img.color = new Color(0.95f, 0.95f, 1f, 1f);
            }
            else
            {
                img.color = new Color(0.08f, 0.11f, 0.18f, 0.98f);
            }

            // Header Title
            var titleObj = new GameObject("RateModalTitle", typeof(RectTransform), typeof(Text));
            titleObj.transform.SetParent(rateModalPanel.transform, false);
            var tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0.05f, 0.84f);
            tRt.anchorMax = new Vector2(0.95f, 0.96f);
            tRt.offsetMin = Vector2.zero;
            tRt.offsetMax = Vector2.zero;
            var tTxt = titleObj.GetComponent<Text>();
            if (lilitaFont != null) tTxt.font = lilitaFont;
            tTxt.text = FantasyDefense.Localization.CitadelLocalizationManager.Text(
                "📜 PELUANG PEMANGGILAN CITADEL",
                "📜 CITADEL SUMMON PROBABILITIES"
            );
            tTxt.fontSize = 24;
            tTxt.fontStyle = FontStyle.Bold;
            tTxt.alignment = TextAnchor.MiddleCenter;
            tTxt.color = new Color(1f, 0.85f, 0.35f);

            // Rate Details Body
            var bodyObj = new GameObject("RateModalBody", typeof(RectTransform), typeof(Text));
            bodyObj.transform.SetParent(rateModalPanel.transform, false);
            var bRt = bodyObj.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.08f, 0.18f);
            bRt.anchorMax = new Vector2(0.92f, 0.82f);
            bRt.offsetMin = Vector2.zero;
            bRt.offsetMax = Vector2.zero;
            var bTxt = bodyObj.GetComponent<Text>();
            if (lilitaFont != null) bTxt.font = lilitaFont;
            bTxt.text = FantasyDefense.Localization.CitadelLocalizationManager.Text(
                "• Wind Ranger [Legendary Rate-Up] : 25.0%\n" +
                "• Rabbit Mage [Epic Class] : 25.0%\n" +
                "• Rose Tanker [Rare Class] : 25.0%\n" +
                "• Cedric Knight [Rare Class] : 25.0%\n\n" +
                "🛡️ ATURAN JAMINAN (PITY SYSTEM):\n" +
                "- Pemain DIJAMIN memperoleh hero Legendary pada tarikan ke-100 jika belum mendapatkannya.\n" +
                "- Penghitung Pity direset kembali ke 0 setiap kali hero Legendary didapatkan atau saat Altar berganti Banner baru.\n" +
                "- Duplikat pahlawan otomatis dikonversi menjadi Shards untuk promosi bintang.",

                "• Wind Ranger [Legendary Rate-Up] : 25.0%\n" +
                "• Rabbit Mage [Epic Class] : 25.0%\n" +
                "• Rose Tanker [Rare Class] : 25.0%\n" +
                "• Cedric Knight [Rare Class] : 25.0%\n\n" +
                "🛡️ GUARANTEE RULES (PITY SYSTEM):\n" +
                "- Players are GUARANTEED to receive a Legendary hero on their 100th pull if not acquired earlier.\n" +
                "- Pity counter resets back to 0 whenever a Legendary hero is obtained or when a new Banner begins.\n" +
                "- Duplicate heroes automatically convert into Shards for star promotions."
            );
            bTxt.fontSize = 17;
            bTxt.lineSpacing = 1.18f;
            bTxt.alignment = TextAnchor.UpperLeft;
            bTxt.color = new Color(0.92f, 0.94f, 0.98f);

            // Close Rate Modal Button
            var closeBtnObj = new GameObject("CloseRateBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            closeBtnObj.transform.SetParent(rateModalPanel.transform, false);
            var cRt = closeBtnObj.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.28f, 0.04f);
            cRt.anchorMax = new Vector2(0.72f, 0.15f);
            cRt.offsetMin = Vector2.zero;
            cRt.offsetMax = Vector2.zero;

            var cImg = closeBtnObj.GetComponent<Image>();
            cImg.color = new Color(0.22f, 0.55f, 0.85f);
            var btnSprite = Resources.Load<Sprite>("Sprites/UI/Btn_Primary_Gold")
#if UNITY_EDITOR
                          ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Btn_Primary_Gold.png")
#endif
                          ;
            if (btnSprite != null)
            {
                cImg.sprite = btnSprite;
                cImg.type = Image.Type.Sliced;
                cImg.color = Color.white;
            }

            var btnTxtObj = new GameObject("BtnText", typeof(RectTransform), typeof(Text));
            btnTxtObj.transform.SetParent(closeBtnObj.transform, false);
            var btRt = btnTxtObj.GetComponent<RectTransform>();
            btRt.anchorMin = Vector2.zero;
            btRt.anchorMax = Vector2.one;
            btRt.offsetMin = Vector2.zero;
            btRt.offsetMax = Vector2.zero;
            var btTxt = btnTxtObj.GetComponent<Text>();
            if (lilitaFont != null) btTxt.font = lilitaFont;
            btTxt.text = "MENGERTI";
            btTxt.fontSize = 20;
            btTxt.fontStyle = FontStyle.Bold;
            btTxt.alignment = TextAnchor.MiddleCenter;
            btTxt.color = Color.white;

            var cBtn = closeBtnObj.GetComponent<Button>();
            cBtn.onClick.AddListener(() => ToggleRateModal());

            rateModalPanel.SetActive(false);
        }

        private void EnsureResultsModalComponents()
        {
            if (resultModalPanel == null) return;

            var lilitaFont = Resources.Load<Font>("Fonts/LilitaOne-Regular");

            // Pastikan tombol Skip Animation tersedia di pojok kanan atas modal hasil
            if (skipAnimationButton == null)
            {
                var existingSkip = resultModalPanel.transform.Find("SkipAnimationBtn");
                if (existingSkip != null)
                {
                    skipAnimationButton = existingSkip.GetComponent<Button>();
                }
                else
                {
                    var skipObj = new GameObject("SkipAnimationBtn", typeof(RectTransform), typeof(Image), typeof(Button));
                    skipObj.transform.SetParent(resultModalPanel.transform, false);

                    var rt = skipObj.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.72f, 0.87f);
                    rt.anchorMax = new Vector2(0.95f, 0.95f);
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;

                    var img = skipObj.GetComponent<Image>();
                    var stoneBtn = Resources.Load<Sprite>("Sprites/UI/Btn_Secondary_Stone")
#if UNITY_EDITOR
                                  ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Btn_Secondary_Stone.png")
#endif
                                  ;
                    if (stoneBtn != null)
                    {
                        img.sprite = stoneBtn;
                        img.type = Image.Type.Sliced;
                        img.color = Color.white;
                    }
                    else
                    {
                        img.color = new Color(0.25f, 0.28f, 0.38f, 0.95f);
                    }

                    var txtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
                    txtObj.transform.SetParent(skipObj.transform, false);
                    var tRt = txtObj.GetComponent<RectTransform>();
                    tRt.anchorMin = Vector2.zero;
                    tRt.anchorMax = Vector2.one;
                    tRt.offsetMin = Vector2.zero;
                    tRt.offsetMax = Vector2.zero;

                    var txt = txtObj.GetComponent<Text>();
                    if (lilitaFont != null) txt.font = lilitaFont;
                    txt.text = "⏩ LEWATI";
                    txt.fontSize = 18;
                    txt.fontStyle = FontStyle.Bold;
                    txt.alignment = TextAnchor.MiddleCenter;
                    txt.color = new Color(1f, 0.92f, 0.45f);

                    skipAnimationButton = skipObj.GetComponent<Button>();
                    skipAnimationButton.onClick.AddListener(OnSkipRevealClicked);
                }
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
