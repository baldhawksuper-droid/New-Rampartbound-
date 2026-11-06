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
            public Image cardFrameImg;
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
            hasTriggeredAllMaxHarmony = false;
            EconomyManager.OnBattleGoldChanged += HandleBattleGoldChanged;
            BattleArchetypeManager.OnArchetypeUpgraded += HandleArchetypeUpgraded;
            BattleArchetypeManager.OnArchetypesReset += HandleArchetypesReset;
            CitadelLocalizationManager.OnLanguageChanged += HandleLanguageChanged;

            RefreshAllCards();
        }

        private void OnDisable()
        {
            EconomyManager.OnBattleGoldChanged -= HandleBattleGoldChanged;
            BattleArchetypeManager.OnArchetypeUpgraded -= HandleArchetypeUpgraded;
            BattleArchetypeManager.OnArchetypesReset -= HandleArchetypesReset;
            CitadelLocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
        }

        private void HandleArchetypesReset()
        {
            hasTriggeredAllMaxHarmony = false;
            RefreshAllCards();
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
                bool isMax = (BattleArchetypeManager.Instance != null && BattleArchetypeManager.Instance.IsMaxLevel(type));
                widgets.levelText.text = isMax ? $"Lv.{newLevel} (MAX)" : $"Lv.{newLevel} (+{bonusPct}%)";

                if (BattleArchetypeManager.Instance != null)
                {
                    if (isMax)
                    {
                        widgets.costText.text = "⭐ MAX";
                        widgets.costText.color = new Color(0.85f, 0.95f, 1f, 0.9f);
                        if (widgets.cardFrameImg != null)
                        {
                            widgets.cardFrameImg.color = new Color(0.92f, 0.95f, 1.0f, 1.0f);
                        }
                    }
                    else
                    {
                        widgets.costText.text = $"💰 {BattleArchetypeManager.Instance.GetUpgradeCost(type)}G";
                    }
                }

                // Floating gold spend indicator (-XXG)
                int costPaid = (type == UnitType.RoseTanker ? 60 : 50) + (newLevel - 2) * (type == UnitType.RoseTanker ? 30 : 25);
                SpawnFloatingGoldCost(widgets.rootObj.GetComponent<RectTransform>(), costPaid);

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
                StartCoroutine(CardGlowFlashRoutine(widgets.glowOverlayImg, elemColor, isMax ? 0.65f : 0.38f));
                StartCoroutine(PunchScaleRoutine(widgets.rootObj.transform, isMax ? 1.28f : 1.18f, 0.22f));

                // Audio denting koin emas dan lonceng kristal max level
                if (Audio.AudioManager.Instance != null)
                {
                    Audio.AudioManager.Instance.PlaySound(Audio.SoundType.GoldCoinSpend);
                    if (isMax)
                    {
                        Audio.AudioManager.Instance.PlaySound(Audio.SoundType.ArchetypeMaxChime, 1.15f);
                    }
                }

                // Respons Haptic Getaran Taktil (Hanya aktif jika pemain menyalakan getaran di pengaturan)
                if (FantasyDefense.Mobile.DevicePerformanceManager.Instance != null &&
                    FantasyDefense.Mobile.DevicePerformanceManager.Instance.IsHapticsEnabled)
                {
                    var haptic = isMax ? FantasyDefense.Mobile.HapticFeedbackType.Success : FantasyDefense.Mobile.HapticFeedbackType.Light;
                    FantasyDefense.Mobile.DevicePerformanceManager.Instance.TriggerHaptic(haptic);
                }

                if (isMax && SkillVfxManager.Instance != null)
                {
                    SkillVfxManager.Instance.SpawnFloatingText(
                        widgets.rootObj.transform.position,
                        "⭐ Lv.5 MAX! ⭐",
                        new Color(0.95f, 0.98f, 1f),
                        1.5f
                    );
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
            CheckAllMaxHarmony();
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
                    bool isMax = BattleArchetypeManager.Instance.IsMaxLevel(type);
                    float mult = BattleArchetypeManager.Instance.GetDamageMultiplier(type);
                    int bonusPct = Mathf.RoundToInt((mult - 1f) * 100f);
                    int cost = BattleArchetypeManager.Instance.GetUpgradeCost(type);

                    widgets.levelText.text = isMax ? $"Lv.{lvl} (MAX)" : $"Lv.{lvl} (+{bonusPct}%)";
                    widgets.costText.text = isMax ? "⭐ MAX" : $"💰 {cost}G";
                    if (isMax && widgets.cardFrameImg != null)
                    {
                        widgets.cardFrameImg.color = new Color(0.92f, 0.95f, 1.0f, 1.0f);
                    }
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
                bool isMax = BattleArchetypeManager.Instance.IsMaxLevel(kvp.Key);
                int cost = BattleArchetypeManager.Instance.GetUpgradeCost(kvp.Key);
                bool canAfford = !isMax && (gold >= cost);

                if (kvp.Value.upgradeButton != null)
                {
                    kvp.Value.upgradeButton.interactable = canAfford;
                }
                if (kvp.Value.costText != null)
                {
                    if (isMax)
                    {
                        kvp.Value.costText.text = "⭐ MAX";
                        kvp.Value.costText.color = new Color(0.85f, 0.95f, 1f, 0.9f);
                    }
                    else
                    {
                        kvp.Value.costText.text = $"💰 {cost}G";
                        kvp.Value.costText.color = canAfford ? new Color(1f, 0.9f, 0.3f) : new Color(0.7f, 0.7f, 0.7f, 0.6f);
                    }
                }
                if (isMax && kvp.Value.cardFrameImg != null)
                {
                    kvp.Value.cardFrameImg.color = new Color(0.92f, 0.95f, 1.0f, 1.0f);
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
                var existingSummonBtn = transform.Find("SummonButton") ?? (transform.parent != null ? transform.parent.Find("SummonButton") : null);
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

            // Pastikan Hero Recycle Bin (Tempat Sampah / Daur Ulang Pahlawan) ada di sisi atas dock
            if (HeroRecycleBin.Instance == null)
            {
                var binObj = new GameObject("HeroRecycleBin", typeof(RectTransform), typeof(CanvasGroup), typeof(HeroRecycleBin));
                binObj.transform.SetParent(dockRoot, false);
                var binRt = binObj.GetComponent<RectTransform>();
                // Diposisikan tepat di atas sudut kanan dock ibu jari dengan rasio proporsional
                binRt.anchorMin = new Vector2(0.72f, 1.06f);
                binRt.anchorMax = new Vector2(0.98f, 2.05f);
                binRt.offsetMin = Vector2.zero;
                binRt.offsetMax = Vector2.zero;
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
            widgets.cardFrameImg = cardImg;

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

            // Portrait Tap Button for Stat Tooltip
            var portBtn = portObj.AddComponent<Button>();
            portBtn.transition = Selectable.Transition.None;
            portBtn.onClick.AddListener(() => ToggleHeroStatTooltip(type, widgets));

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
            var btnGoldSprite = Resources.Load<Sprite>("Sprites/UI/Btn_Primary_Gold");
            if (btnGoldSprite != null)
            {
                bImg.sprite = btnGoldSprite;
                bImg.type = Image.Type.Sliced;
                bImg.color = Color.white;
            }
            else
            {
                bImg.color = new Color(0.20f, 0.26f, 0.38f, 0.95f);
            }
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
                bool success = BattleArchetypeManager.Instance.TryUpgradeArchetype(type);
                if (!success && heroCards.TryGetValue(type, out var widgets))
                {
                    // Insufficient Gold / Locked feedback: Wiggle & Red Text Flash
                    StartCoroutine(CardWiggleRoutine(widgets.rootObj.transform, 5.5f, 0.24f));
                    StartCoroutine(CostTextFlashRedRoutine(widgets.costText, 0.40f));

                    if (FantasyDefense.Mobile.DevicePerformanceManager.Instance != null &&
                        FantasyDefense.Mobile.DevicePerformanceManager.Instance.IsHapticsEnabled)
                    {
                        FantasyDefense.Mobile.DevicePerformanceManager.Instance.TriggerHaptic(FantasyDefense.Mobile.HapticFeedbackType.Warning);
                    }
                }
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

        private void SpawnFloatingGoldCost(RectTransform parentCard, int cost)
        {
            if (parentCard == null) return;
            var floatObj = new GameObject("FloatingGoldCost", typeof(RectTransform), typeof(Text));
            floatObj.transform.SetParent(parentCard, false);
            var rt = floatObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.22f);
            rt.anchorMax = new Vector2(0.5f, 0.22f);
            rt.sizeDelta = new Vector2(90, 26);
            rt.anchoredPosition = Vector2.zero;

            var txt = floatObj.GetComponent<Text>();
            var font = Resources.Load<Font>("Fonts/LilitaOne-Regular");
            if (font != null) txt.font = font;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.fontSize = 14;
            txt.fontStyle = FontStyle.Bold;
            txt.text = $"-{cost}G";
            txt.color = new Color(1f, 0.85f, 0.25f, 1f);
            txt.raycastTarget = false;

            StartCoroutine(FloatingGoldCostRoutine(floatObj, rt, txt, 0.55f));
        }

        private System.Collections.IEnumerator FloatingGoldCostRoutine(GameObject obj, RectTransform rt, Text txt, float duration)
        {
            float elapsed = 0f;
            Vector2 startPos = (rt != null) ? rt.anchoredPosition : Vector2.zero;
            Vector2 endPos = startPos + new Vector2(0f, 38f);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / duration;
                if (rt != null)
                {
                    rt.anchoredPosition = Vector2.Lerp(startPos, endPos, Mathf.SmoothStep(0f, 1f, progress));
                }
                if (txt != null)
                {
                    float alpha = 1f - (progress * progress);
                    txt.color = new Color(1f, 0.85f, 0.25f, alpha);
                }
                yield return null;
            }
            if (obj != null) Destroy(obj);
        }

        private System.Collections.IEnumerator CardWiggleRoutine(Transform tr, float magnitude, float duration)
        {
            if (tr == null) yield break;
            Vector3 origPos = tr.localPosition;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / duration;
                float offset = Mathf.Sin(progress * Mathf.PI * 6f) * magnitude * (1f - progress);
                tr.localPosition = origPos + new Vector3(offset, 0f, 0f);
                yield return null;
            }
            tr.localPosition = origPos;
        }

        private System.Collections.IEnumerator CostTextFlashRedRoutine(Text txt, float duration)
        {
            if (txt == null) yield break;
            Color origColor = txt.color;
            Color redColor = new Color(1f, 0.35f, 0.35f, 1f);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / duration;
                txt.color = Color.Lerp(redColor, origColor, progress);
                yield return null;
            }
            txt.color = origColor;
        }

        private GameObject activeTooltipObj;
        private UnitType activeTooltipType;
        private Coroutine tooltipCloseCoroutine;
        private float lastPortraitTapTime = 0f;
        private UnitType lastPortraitTapType;

        private void ToggleHeroStatTooltip(UnitType type, HeroCardWidgets widgets)
        {
            float now = Time.unscaledTime;
            bool isDoubleTap = (lastPortraitTapType == type && (now - lastPortraitTapTime) < 0.35f);
            lastPortraitTapTime = now;
            lastPortraitTapType = type;

            bool isRealTimeDeploy = FantasyDefense.Core.GameManager.Instance != null &&
                                    FantasyDefense.Core.GameManager.Instance.CurrentBattleMode == FantasyDefense.Data.BattleMode.RealTimeDeploy;

            // Jika double-tap di Mode 2 (Real-Time Deploy), lakukan fast-deploy pahlawan spesifik langsung ke arena!
            if (isDoubleTap && isRealTimeDeploy)
            {
                if (activeTooltipObj != null) Destroy(activeTooltipObj);
                activeTooltipObj = null;

                if (Summon.HeroSummoner.Instance != null)
                {
                    bool spawned = Summon.HeroSummoner.Instance.TrySummonSpecificHero(type);
                    if (spawned)
                    {
                        StartCoroutine(PunchScaleRoutine(widgets.rootObj.transform, 1.25f, 0.18f));
                    }
                }
                return;
            }

            if (activeTooltipObj != null)
            {
                bool isSame = (activeTooltipType == type);
                Destroy(activeTooltipObj);
                activeTooltipObj = null;
                if (tooltipCloseCoroutine != null) StopCoroutine(tooltipCloseCoroutine);
                if (isSame) return;
            }

            activeTooltipType = type;
            var tooltip = new GameObject("HeroStatTooltip", typeof(RectTransform), typeof(Image));
            tooltip.transform.SetParent(widgets.rootObj.transform, false);
            var rt = tooltip.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 6f);
            rt.sizeDelta = isRealTimeDeploy ? new Vector2(138f, 116f) : new Vector2(130f, 88f);

            var img = tooltip.GetComponent<Image>();
            var frameSprite = Resources.Load<Sprite>("Sprites/UI/Frame_StoneBastion_9Slice")
#if UNITY_EDITOR
                           ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/Frame_StoneBastion_9Slice.png")
#endif
                           ;
            if (frameSprite != null)
            {
                img.sprite = frameSprite;
                img.type = Image.Type.Sliced;
            }
            img.color = new Color(0.07f, 0.11f, 0.17f, 0.98f);

            var contentObj = new GameObject("Content", typeof(RectTransform), typeof(Text));
            contentObj.transform.SetParent(tooltip.transform, false);
            var cRt = contentObj.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0f, isRealTimeDeploy ? 0.26f : 0f);
            cRt.anchorMax = Vector2.one;
            cRt.offsetMin = new Vector2(6f, 2f);
            cRt.offsetMax = new Vector2(-6f, -4f);

            var txt = contentObj.GetComponent<Text>();
            var font = Resources.Load<Font>("Fonts/LilitaOne-Regular");
            if (font != null) txt.font = font;
            txt.alignment = TextAnchor.UpperLeft;
            txt.fontSize = 11;
            txt.lineSpacing = 1.05f;

            string heroName = type switch
            {
                UnitType.Cedric => "<color=#FFA233>⚔️ Cedric</color>",
                UnitType.RabbitMage => "<color=#59E0FF>🔮 Rabbit</color>",
                UnitType.RoseTanker => "<color=#FFDD44>🛡️ Rose</color>",
                UnitType.WindRanger => "<color=#59F28F>🏹 Wind</color>",
                _ => type.ToString()
            };

            string role = type switch
            {
                UnitType.Cedric => "Vanguard Melee",
                UnitType.RabbitMage => "Arcane Artillery",
                UnitType.RoseTanker => "Bastion Defender",
                UnitType.WindRanger => "Piercing CC",
                _ => "Hero"
            };

            float mult = BattleArchetypeManager.Instance != null ? BattleArchetypeManager.Instance.GetDamageMultiplier(type) : 1f;
            int bonus = Mathf.RoundToInt((mult - 1f) * 100f);

            string skill = type switch
            {
                UnitType.Cedric => "Dual Slash",
                UnitType.RabbitMage => "Mystic Burst",
                UnitType.RoseTanker => "Shield Slam",
                UnitType.WindRanger => "Piercing Gale",
                _ => "Active Skill"
            };

            txt.text = $"{heroName}\n<color=#B3BFCC>{role}</color>\n<color=#FFE659>DPS: +{bonus}%</color>\n<color=#FFFFFF>Skill: {skill}</color>";

            // Tombol Targeted Summon di Mode 2 (Real-Time Deploy)
            if (isRealTimeDeploy)
            {
                var deployBtnObj = new GameObject("DeployHeroBtn", typeof(RectTransform), typeof(Image), typeof(Button));
                deployBtnObj.transform.SetParent(tooltip.transform, false);
                var dRt = deployBtnObj.GetComponent<RectTransform>();
                dRt.anchorMin = new Vector2(0.08f, 0.05f);
                dRt.anchorMax = new Vector2(0.92f, 0.28f);
                dRt.offsetMin = Vector2.zero;
                dRt.offsetMax = Vector2.zero;

                var dImg = deployBtnObj.GetComponent<Image>();
                var btnSprite = Resources.Load<Sprite>("Sprites/UI/Btn_Primary_Gold");
                if (btnSprite != null)
                {
                    dImg.sprite = btnSprite;
                    dImg.type = Image.Type.Sliced;
                }
                dImg.color = new Color(0.25f, 0.65f, 0.35f, 0.95f);

                var dTxtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
                dTxtObj.transform.SetParent(deployBtnObj.transform, false);
                var dtRt = dTxtObj.GetComponent<RectTransform>();
                dtRt.anchorMin = Vector2.zero;
                dtRt.anchorMax = Vector2.one;
                dtRt.offsetMin = Vector2.zero;
                dtRt.offsetMax = Vector2.zero;

                var dTxt = dTxtObj.GetComponent<Text>();
                if (font != null) dTxt.font = font;
                dTxt.text = "⚔️ PANGGIL HERO";
                dTxt.fontSize = 11;
                dTxt.fontStyle = FontStyle.Bold;
                dTxt.alignment = TextAnchor.MiddleCenter;
                dTxt.color = Color.white;

                var dBtn = deployBtnObj.GetComponent<Button>();
                dBtn.onClick.AddListener(() =>
                {
                    if (Summon.HeroSummoner.Instance != null)
                    {
                        bool ok = Summon.HeroSummoner.Instance.TrySummonSpecificHero(type);
                        if (ok)
                        {
                            StartCoroutine(PunchScaleRoutine(widgets.rootObj.transform, 1.25f, 0.18f));
                        }
                    }
                    if (activeTooltipObj != null) Destroy(activeTooltipObj);
                    activeTooltipObj = null;
                });
            }

            StartCoroutine(PunchScaleRoutine(tooltip.transform, 1.15f, 0.18f));
            activeTooltipObj = tooltip;

            if (tooltipCloseCoroutine != null) StopCoroutine(tooltipCloseCoroutine);
            tooltipCloseCoroutine = StartCoroutine(AutoDismissTooltipRoutine(tooltip, isRealTimeDeploy ? 4.5f : 3.2f));
        }

        private System.Collections.IEnumerator AutoDismissTooltipRoutine(GameObject tooltip, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            if (tooltip != null)
            {
                Destroy(tooltip);
                if (activeTooltipObj == tooltip) activeTooltipObj = null;
            }
        }

        #region Dock Slide In / Out (Point 2)

        private Coroutine slideCoroutine;

        public void AnimateSlideOut(float duration = 0.35f)
        {
            if (dockRoot == null) dockRoot = GetComponent<RectTransform>();
            if (slideCoroutine != null) StopCoroutine(slideCoroutine);
            slideCoroutine = StartCoroutine(SlideDockRoutine(dockRoot.anchoredPosition, new Vector2(0f, -180f), duration, false));
        }

        public void AnimateSlideIn(float duration = 0.35f)
        {
            if (dockRoot == null) dockRoot = GetComponent<RectTransform>();
            if (slideCoroutine != null) StopCoroutine(slideCoroutine);
            gameObject.SetActive(true);
            slideCoroutine = StartCoroutine(SlideDockRoutine(dockRoot.anchoredPosition, Vector2.zero, duration, true));
        }

        private System.Collections.IEnumerator SlideDockRoutine(Vector2 startPos, Vector2 targetPos, float duration, bool activateAtEnd)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                if (dockRoot != null)
                {
                    dockRoot.anchoredPosition = Vector2.Lerp(startPos, targetPos, smoothT);
                }
                yield return null;
            }
            if (dockRoot != null) dockRoot.anchoredPosition = targetPos;
            if (!activateAtEnd)
            {
                gameObject.SetActive(false);
            }
        }

        #endregion

        #region All 4 Classes Max Bastion Harmony (Point 3)

        private bool hasTriggeredAllMaxHarmony = false;

        private void CheckAllMaxHarmony()
        {
            if (hasTriggeredAllMaxHarmony || BattleArchetypeManager.Instance == null) return;

            UnitType[] types = { UnitType.RabbitMage, UnitType.Cedric, UnitType.RoseTanker, UnitType.WindRanger };
            foreach (var type in types)
            {
                if (!BattleArchetypeManager.Instance.IsMaxLevel(type))
                {
                    return;
                }
            }

            hasTriggeredAllMaxHarmony = true;
            StartCoroutine(AllMaxHarmonyCelebrationRoutine());
        }

        private System.Collections.IEnumerator AllMaxHarmonyCelebrationRoutine()
        {
            if (Audio.AudioManager.Instance != null)
            {
                Audio.AudioManager.Instance.PlaySound(Audio.SoundType.GachaLegendary, 1.20f);
            }

            // Prismatic rainbow shimmer on all cards
            Color[] rainbowColors = {
                new Color(0.35f, 0.95f, 1f, 0.95f),   // Cyan
                new Color(1f, 0.45f, 0.25f, 0.95f),   // Crimson
                new Color(1f, 0.85f, 0.20f, 0.95f),   // Amber Gold
                new Color(0.40f, 0.95f, 0.60f, 0.95f) // Emerald
            };

            int cIdx = 0;
            foreach (var kvp in heroCards)
            {
                if (kvp.Value != null)
                {
                    Color rColor = rainbowColors[cIdx % rainbowColors.Length];
                    if (kvp.Value.glowOverlayImg != null)
                    {
                        StartCoroutine(CardGlowFlashRoutine(kvp.Value.glowOverlayImg, rColor, 0.85f));
                    }
                    if (kvp.Value.rootObj != null)
                    {
                        StartCoroutine(PunchScaleRoutine(kvp.Value.rootObj.transform, 1.25f, 0.30f));
                    }
                    cIdx++;
                }
            }

            if (SkillVfxManager.Instance != null)
            {
                SkillVfxManager.Instance.SpawnFloatingText(
                    Vector3.zero,
                    "🌟 HARMONI BASTION PURNA: SEMUA KELAS MAX! 🌟",
                    new Color(1f, 0.92f, 0.35f),
                    2.2f
                );
            }

            if (FantasyDefense.Mobile.DevicePerformanceManager.Instance != null &&
                FantasyDefense.Mobile.DevicePerformanceManager.Instance.IsHapticsEnabled)
            {
                FantasyDefense.Mobile.DevicePerformanceManager.Instance.TriggerHaptic(FantasyDefense.Mobile.HapticFeedbackType.Success);
            }

            yield return null;
        }

        #endregion
    }
}
