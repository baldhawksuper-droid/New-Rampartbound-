using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FantasyDefense.Audio;
using FantasyDefense.Combat;
using FantasyDefense.Data;
using FantasyDefense.Economy;
using FantasyDefense.Localization;

namespace FantasyDefense.UI
{
    /// <summary>
    /// Master Controller untuk Dock Aksi Bawah Layar Pertempuran (Battle Bottom Action Dock - "Thumb Zone").
    /// Mengimplementasikan Opsi B (Penyatuan Sinergis):
    /// 1. Tiga/Empat Kartu Pahlawan (Rabbit, Cedric, Rose, Wind Ranger) untuk Peningkatan Arketipe Global dengan Royal Gold.
    /// 2. Tombol Kubus Emas Runic ("SUMMON") di pojok kanan:
    ///    - Pada Mode 1 (Swap & Merge): Berfungsi sebagai Katalis Papan Taktis (Transmutasi Tier / Pemecah Kebuntuan).
    ///    - Pada Mode 2 (Real-Time Deploy): Berfungsi memanggil hero baru ke petak kosong.
    /// </summary>
    public class UIBattleBottomDockView : MonoBehaviour
    {
        public static UIBattleBottomDockView Instance { get; private set; }

        [Header("Containers & Anchors")]
        [SerializeField] private RectTransform dockRoot;
        [SerializeField] private RectTransform heroCardsContainer;
        [SerializeField] private RectTransform summonCubeContainer;

        [Header("Hero Card UI Elements")]
        private readonly Dictionary<UnitType, HeroCardWidgets> heroCards = new Dictionary<UnitType, HeroCardWidgets>();

        private class HeroCardWidgets
        {
            public GameObject rootObj;
            public Image portraitImg;
            public Text nameText;
            public Text levelText;
            public Button upgradeButton;
            public Text costText;
            public RectTransform starsContainer;
            public readonly List<Image> starImages = new List<Image>();
            public Image glowOverlayImg;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureDockBuilt();
        }

        private void OnEnable()
        {
            EconomyManager.OnBattleGoldChanged += HandleBattleGoldChanged;
            BattleArchetypeManager.OnArchetypeUpgraded += HandleArchetypeUpgraded;
            BattleArchetypeManager.OnArchetypesReset += RefreshAllCards;
            CitadelLocalizationManager.OnLanguageChanged += HandleLanguageChanged;

            RefreshAllCards();
        }

        private void OnDisable()
        {
            EconomyManager.OnBattleGoldChanged -= HandleBattleGoldChanged;
            BattleArchetypeManager.OnArchetypeUpgraded -= HandleArchetypeUpgraded;
            BattleArchetypeManager.OnArchetypesReset -= RefreshAllCards;
            CitadelLocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
        }

        private void HandleLanguageChanged(AppLanguage lang)
        {
            RefreshAllCards();
        }

        private void HandleBattleGoldChanged(int newGold)
        {
            UpdateCardButtonStates();
        }

        private void HandleArchetypeUpgraded(UnitType type, int newLevel, float newMultiplier)
        {
            if (heroCards.TryGetValue(type, out var widgets))
            {
                int bonusPct = Mathf.RoundToInt((newMultiplier - 1f) * 100f);
                widgets.levelText.text = $"Lv.{newLevel} (+{bonusPct}%)";
                if (BattleArchetypeManager.Instance != null)
                {
                    widgets.costText.text = $"💰 {BattleArchetypeManager.Instance.GetUpgradeCost(type)}G";
                }

                UpdateStarBadges(widgets, newLevel);

                // Elemental card border glow flash & punch animation
                Color elemColor = type switch
                {
                    UnitType.RabbitMage => new Color(0.35f, 0.88f, 1f, 0.95f),   // Arcane Cyan
                    UnitType.Cedric => new Color(1f, 0.45f, 0.25f, 0.95f),       // Fiery Crimson
                    UnitType.RoseTanker => new Color(1f, 0.82f, 0.20f, 0.95f),   // Golden Amber
                    UnitType.WindRanger => new Color(0.35f, 0.95f, 0.55f, 0.95f),// Emerald Gale
                    _ => Color.white
                };
                StartCoroutine(CardGlowFlashRoutine(widgets.glowOverlayImg, elemColor, 0.38f));
                StartCoroutine(PunchScaleRoutine(widgets.rootObj.transform, 1.18f, 0.20f));

                // Audio denting koin emas khusus pembelian upgrade pertempuran
                if (Audio.AudioManager.Instance != null)
                {
                    Audio.AudioManager.Instance.PlaySound(Audio.SoundType.GoldCoinSpend);
                }

                // Respons Haptic Getaran Taktil (Hanya aktif jika pemain menyalakan getaran di pengaturan)
                if (FantasyDefense.Mobile.DevicePerformanceManager.Instance != null &&
                    FantasyDefense.Mobile.DevicePerformanceManager.Instance.IsHapticsEnabled)
                {
                    FantasyDefense.Mobile.DevicePerformanceManager.Instance.TriggerHaptic(FantasyDefense.Mobile.HapticFeedbackType.Light);
                }

                // Pemicu pendaran cincin aura buff di bawah kaki semua hero bertipe ini di arena
                if (SkillVfxManager.Instance != null)
                {
                    var activeUnits = UnitController.GetActiveUnitsSnapshot();
                    foreach (var unit in activeUnits)
                    {
                        if (unit != null && unit.UnitType == type)
                        {
                            SkillVfxManager.Instance.SpawnHeroBuffAura(unit.transform.position, elemColor);
                        }
                    }
                }
            }

            UpdateCardButtonStates();
        }

        public void RefreshAllCards()
        {
            if (BattleArchetypeManager.Instance == null) return;

            UnitType[] types = { UnitType.RabbitMage, UnitType.Cedric, UnitType.RoseTanker, UnitType.WindRanger };
            foreach (var type in types)
            {
                if (heroCards.TryGetValue(type, out var widgets))
                {
                    int lvl = BattleArchetypeManager.Instance.GetLevel(type);
                    float mult = BattleArchetypeManager.Instance.GetDamageMultiplier(type);
                    int bonusPct = Mathf.RoundToInt((mult - 1f) * 100f);
                    int cost = BattleArchetypeManager.Instance.GetUpgradeCost(type);

                    widgets.levelText.text = $"Lv.{lvl} (+{bonusPct}%)";
                    widgets.costText.text = $"💰 {cost}G";
                    UpdateStarBadges(widgets, lvl);
                }
            }

            UpdateCardButtonStates();
        }

        public void UpdateCardButtonStates()
        {
            int gold = EconomyManager.Instance != null ? EconomyManager.Instance.CurrentBattleGold : 0;
            if (BattleArchetypeManager.Instance == null) return;

            foreach (var kvp in heroCards)
            {
                int cost = BattleArchetypeManager.Instance.GetUpgradeCost(kvp.Key);
                bool canAfford = gold >= cost;
                if (kvp.Value.upgradeButton != null)
                {
                    kvp.Value.upgradeButton.interactable = canAfford;
                }
                if (kvp.Value.costText != null)
                {
                    kvp.Value.costText.color = canAfford ? new Color(1f, 0.9f, 0.3f) : new Color(0.7f, 0.7f, 0.7f, 0.6f);
                }
            }
        }

        /// <summary>
        /// Membangun hierarki visual dock bawah jika belum dikonfigurasi via prefab.
        /// Menjamin tersedianya 4 kartu arketipe dan kubus pemanggilan emas secara procedural.
        /// </summary>
        public void EnsureDockBuilt()
        {
            if (dockRoot == null)
            {
                dockRoot = GetComponent<RectTransform>();
            }

            var lilitaFont = Resources.Load<Font>("Fonts/LilitaOne-Regular");
            var frame9Slice = Resources.Load<Sprite>("Sprites/UI/Frame_StoneBastion_9Slice")
#if UNITY_EDITOR
                          ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Frame_StoneBastion_9Slice.png")
#endif
                          ;

            // Pastikan Hero Cards Container ada
            if (heroCardsContainer == null)
            {
                var cardsObj = new GameObject("HeroCardsContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                cardsObj.transform.SetParent(dockRoot, false);
                heroCardsContainer = cardsObj.GetComponent<RectTransform>();
                heroCardsContainer.anchorMin = new Vector2(0.02f, 0.05f);
                heroCardsContainer.anchorMax = new Vector2(0.72f, 0.95f);
                heroCardsContainer.offsetMin = Vector2.zero;
                heroCardsContainer.offsetMax = Vector2.zero;

                var hlg = cardsObj.GetComponent<HorizontalLayoutGroup>();
                hlg.spacing = 8f;
                hlg.childControlWidth = true;
                hlg.childControlHeight = true;
                hlg.childForceExpandWidth = true;
                hlg.childForceExpandHeight = true;
            }

            // Bangun 4 kartu hero (Rabbit, Cedric, Rose, Wind)
            UnitType[] roster = { UnitType.RabbitMage, UnitType.Cedric, UnitType.RoseTanker, UnitType.WindRanger };
            foreach (var type in roster)
            {
                if (!heroCards.ContainsKey(type))
                {
                    var cardWidgets = BuildSingleHeroCard(heroCardsContainer, type, lilitaFont, frame9Slice);
                    heroCards[type] = cardWidgets;
                }
            }

            // Hubungkan Summon Cube yang sudah ada di HUD jika ada
            if (summonCubeContainer == null)
            {
                var existingSummonBtn = transform.parent != null ? transform.parent.Find("SummonButton") : null;
                if (existingSummonBtn != null)
                {
                    existingSummonBtn.SetParent(dockRoot, false);
                    summonCubeContainer = existingSummonBtn.GetComponent<RectTransform>();
                    summonCubeContainer.anchorMin = new Vector2(0.74f, 0.05f);
                    summonCubeContainer.anchorMax = new Vector2(0.98f, 0.95f);
                    summonCubeContainer.offsetMin = Vector2.zero;
                    summonCubeContainer.offsetMax = Vector2.zero;
                }
            }
        }

        private HeroCardWidgets BuildSingleHeroCard(Transform parent, UnitType type, Font font, Sprite frameSprite)
        {
            var widgets = new HeroCardWidgets();

            string heroTitle = type switch
            {
                UnitType.RabbitMage => "MAGE",
                UnitType.Cedric => "KNIGHT",
                UnitType.RoseTanker => "TANKER",
                UnitType.WindRanger => "RANGER",
                _ => type.ToString()
            };

            string spriteName = type switch
            {
                UnitType.RabbitMage => "RabbitMage_T1",
                UnitType.Cedric => "Cedric_T1",
                UnitType.RoseTanker => "Rose_T1",
                UnitType.WindRanger => "WindRanger_T1",
                _ => "Cedric_T1"
            };

            Color themeColor = type switch
            {
                UnitType.RabbitMage => new Color(0.25f, 0.65f, 1f),
                UnitType.Cedric => new Color(0.95f, 0.45f, 0.25f),
                UnitType.RoseTanker => new Color(0.95f, 0.75f, 0.25f),
                UnitType.WindRanger => new Color(0.35f, 0.92f, 0.55f),
                _ => Color.white
            };

            // Root Card
            var cardObj = new GameObject($"Card_{type}", typeof(RectTransform), typeof(Image));
            cardObj.transform.SetParent(parent, false);
            widgets.rootObj = cardObj;

            var cardImg = cardObj.GetComponent<Image>();
            if (frameSprite != null)
            {
                cardImg.sprite = frameSprite;
                cardImg.type = Image.Type.Sliced;
                cardImg.color = new Color(0.85f, 0.90f, 1f, 0.95f);
            }
            else
            {
                cardImg.color = new Color(0.10f, 0.14f, 0.22f, 0.95f);
            }

            // Glow Overlay for elemental level-up flash
            var glowObj = new GameObject("GlowOverlay", typeof(RectTransform), typeof(Image));
            glowObj.transform.SetParent(cardObj.transform, false);
            var gRt = glowObj.GetComponent<RectTransform>();
            gRt.anchorMin = Vector2.zero;
            gRt.anchorMax = Vector2.one;
            gRt.offsetMin = new Vector2(-2, -2);
            gRt.offsetMax = new Vector2(2, 2);
            var gImg = glowObj.GetComponent<Image>();
            if (frameSprite != null)
            {
                gImg.sprite = frameSprite;
                gImg.type = Image.Type.Sliced;
            }
            gImg.color = new Color(1f, 1f, 1f, 0f);
            gImg.raycastTarget = false;
            widgets.glowOverlayImg = gImg;

            // Avatar Portrait
            var portObj = new GameObject("HeroPortrait", typeof(RectTransform), typeof(Image));
            portObj.transform.SetParent(cardObj.transform, false);
            var pRt = portObj.GetComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0.1f, 0.44f);
            pRt.anchorMax = new Vector2(0.9f, 0.92f);
            pRt.offsetMin = Vector2.zero;
            pRt.offsetMax = Vector2.zero;
            widgets.portraitImg = portObj.GetComponent<Image>();
            var hSprite = Resources.Load<Sprite>($"Sprites/Heroes/{spriteName}")
#if UNITY_EDITOR
                       ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/Sprites/Heroes/{spriteName}.png")
#endif
                       ;
            if (hSprite != null)
            {
                widgets.portraitImg.sprite = hSprite;
                widgets.portraitImg.preserveAspect = true;
            }

            // Star Badges Container (Pojok Kiri Atas Kartu)
            var starsObj = new GameObject("StarsContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            starsObj.transform.SetParent(cardObj.transform, false);
            var sRt = starsObj.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0.06f, 0.74f);
            sRt.anchorMax = new Vector2(0.85f, 0.94f);
            sRt.offsetMin = Vector2.zero;
            sRt.offsetMax = Vector2.zero;
            var hlg = starsObj.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 2f;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            widgets.starsContainer = sRt;

            UpdateStarBadges(widgets, 1);

            // Name Text
            var nameObj = new GameObject("NameText", typeof(RectTransform), typeof(Text));
            nameObj.transform.SetParent(cardObj.transform, false);
            var nRt = nameObj.GetComponent<RectTransform>();
            nRt.anchorMin = new Vector2(0.05f, 0.32f);
            nRt.anchorMax = new Vector2(0.95f, 0.48f);
            nRt.offsetMin = Vector2.zero;
            nRt.offsetMax = Vector2.zero;
            widgets.nameText = nameObj.GetComponent<Text>();
            if (font != null) widgets.nameText.font = font;
            widgets.nameText.text = heroTitle;
            widgets.nameText.fontSize = 14;
            widgets.nameText.fontStyle = FontStyle.Bold;
            widgets.nameText.alignment = TextAnchor.MiddleCenter;
            widgets.nameText.color = themeColor;

            // Level Text
            var lvlObj = new GameObject("LevelText", typeof(RectTransform), typeof(Text));
            lvlObj.transform.SetParent(cardObj.transform, false);
            var lRt = lvlObj.GetComponent<RectTransform>();
            lRt.anchorMin = new Vector2(0.05f, 0.20f);
            lRt.anchorMax = new Vector2(0.95f, 0.34f);
            lRt.offsetMin = Vector2.zero;
            lRt.offsetMax = Vector2.zero;
            widgets.levelText = lvlObj.GetComponent<Text>();
            if (font != null) widgets.levelText.font = font;
            widgets.levelText.text = "Lv.1 (+15%)";
            widgets.levelText.fontSize = 12;
            widgets.levelText.alignment = TextAnchor.MiddleCenter;
            widgets.levelText.color = Color.white;

            // Upgrade Button
            var btnObj = new GameObject("UpgradeBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(cardObj.transform, false);
            var bRt = btnObj.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.06f, 0.04f);
            bRt.anchorMax = new Vector2(0.94f, 0.22f);
            bRt.offsetMin = Vector2.zero;
            bRt.offsetMax = Vector2.zero;

            var bImg = btnObj.GetComponent<Image>();
            bImg.color = new Color(0.20f, 0.26f, 0.38f, 0.95f);
            widgets.upgradeButton = btnObj.GetComponent<Button>();
            widgets.upgradeButton.onClick.AddListener(() => OnCardUpgradeClicked(type));

            // Button Cost Text
            var costObj = new GameObject("CostText", typeof(RectTransform), typeof(Text));
            costObj.transform.SetParent(btnObj.transform, false);
            var cRt = costObj.GetComponent<RectTransform>();
            cRt.anchorMin = Vector2.zero;
            cRt.anchorMax = Vector2.one;
            cRt.offsetMin = Vector2.zero;
            cRt.offsetMax = Vector2.zero;
            widgets.costText = costObj.GetComponent<Text>();
            if (font != null) widgets.costText.font = font;
            widgets.costText.text = "💰 50G";
            widgets.costText.fontSize = 13;
            widgets.costText.fontStyle = FontStyle.Bold;
            widgets.costText.alignment = TextAnchor.MiddleCenter;
            widgets.costText.color = new Color(1f, 0.9f, 0.3f);

            return widgets;
        }

        private void OnCardUpgradeClicked(UnitType type)
        {
            if (BattleArchetypeManager.Instance != null)
            {
                BattleArchetypeManager.Instance.TryUpgradeArchetype(type);
            }
        }

        private void UpdateStarBadges(HeroCardWidgets widgets, int level)
        {
            if (widgets.starsContainer == null) return;

            var starSprite = Resources.Load<Sprite>("Sprites/UI/UI_Star_Badge_Gold")
#if UNITY_EDITOR
                          ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/UI_Star_Badge_Gold.png")
#endif
                          ;

            int clampedLevel = Mathf.Clamp(level, 1, 5);

            while (widgets.starImages.Count < clampedLevel)
            {
                var starObj = new GameObject($"Star_{widgets.starImages.Count + 1}", typeof(RectTransform), typeof(Image));
                starObj.transform.SetParent(widgets.starsContainer, false);
                var rt = starObj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(16, 16);
                var img = starObj.GetComponent<Image>();
                img.sprite = starSprite;
                img.preserveAspect = true;
                img.raycastTarget = false;
                widgets.starImages.Add(img);

                // Punch scale pada bintang baru yang terbuka
                StartCoroutine(PunchScaleRoutine(starObj.transform, 1.45f, 0.22f));
            }

            for (int i = 0; i < widgets.starImages.Count; i++)
            {
                widgets.starImages[i].gameObject.SetActive(i < clampedLevel);
            }
        }

        private System.Collections.IEnumerator CardGlowFlashRoutine(Image glowImg, Color flashColor, float duration)
        {
            if (glowImg == null) yield break;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / duration;
                float alpha = Mathf.Sin(progress * Mathf.PI) * flashColor.a;
                glowImg.color = new Color(flashColor.r, flashColor.g, flashColor.b, alpha);
                yield return null;
            }
            glowImg.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
        }

        private System.Collections.IEnumerator PunchScaleRoutine(Transform tr, float targetScale, float duration)
        {
            if (tr == null) yield break;
            Vector3 orig = Vector3.one;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                float s = 1f + Mathf.Sin(t * Mathf.PI) * (targetScale - 1f);
                tr.localScale = orig * s;
                yield return null;
            }
            tr.localScale = orig;
        }
    }
}
