using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FantasyDefense.Economy;
using FantasyDefense.GridSystem;
using FantasyDefense.Summon;
using FantasyDefense.Localization;
using FantasyDefense.Data;

namespace FantasyDefense.UI
{
    /// <summary>
    /// Mengontrol tombol UI "Summon Hero" pada HUD pertempuran layar mobile/tablet.
    /// Menampilkan biaya gems dinamis, auto-disable jika gems kurang atau grid penuh,
    /// dan memberikan respon sentuhan responsif.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class SummonButtonUI : MonoBehaviour
    {
        public static SummonButtonUI Instance { get; private set; }

        [Header("Button References")]
        [SerializeField] private Button button;
        [SerializeField] private Text costText;
        [SerializeField] private Image cubeIconImage;
        [SerializeField] private Image glowOutlineImage;
        private readonly List<RectTransform> orbitMotes = new List<RectTransform>();

        [Header("Auto-Summon Badge")]
        [SerializeField] private Button autoSummonBadgeButton;
        [SerializeField] private Text autoSummonBadgeText;
        [SerializeField] private Image autoSummonBadgeBg;

        [Header("Visual Feedback")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.7f);

        private Coroutine pulseCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (button == null)
                button = GetComponent<Button>();

            button.onClick.AddListener(OnSummonButtonClicked);
            EnsureCubeVisuals();
        }

        private void EnsureCubeVisuals()
        {
            var cubeSprite = Resources.Load<Sprite>("Sprites/UI/UI_Summon_Cube_Runic")
#if UNITY_EDITOR
                          ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/UI_Summon_Cube_Runic.png")
#endif
                          ;

            if (cubeIconImage == null)
            {
                var iconObj = transform.Find("CubeIcon")?.gameObject;
                if (iconObj == null)
                {
                    iconObj = new GameObject("CubeIcon", typeof(RectTransform), typeof(Image));
                    iconObj.transform.SetParent(transform, false);
                    iconObj.transform.SetAsFirstSibling(); // Di belakang teks cost
                    var rt = iconObj.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.12f, 0.32f);
                    rt.anchorMax = new Vector2(0.88f, 0.94f);
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                }
                cubeIconImage = iconObj.GetComponent<Image>();
                cubeIconImage.raycastTarget = false;
            }

            if (cubeSprite != null && cubeIconImage != null)
            {
                cubeIconImage.sprite = cubeSprite;
                cubeIconImage.preserveAspect = true;
            }

            if (glowOutlineImage == null)
            {
                var glowObj = transform.Find("GlowOutline")?.gameObject;
                if (glowObj == null)
                {
                    glowObj = new GameObject("GlowOutline", typeof(RectTransform), typeof(Image));
                    glowObj.transform.SetParent(transform, false);
                    glowObj.transform.SetAsFirstSibling();
                    var rt = glowObj.GetComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = new Vector2(-4, -4);
                    rt.offsetMax = new Vector2(4, 4);
                }
                glowOutlineImage = glowObj.GetComponent<Image>();
                glowOutlineImage.raycastTarget = false;
                var frameSprite = Resources.Load<Sprite>("Sprites/UI/Frame_StoneBastion_9Slice");
                if (frameSprite != null)
                {
                    glowOutlineImage.sprite = frameSprite;
                    glowOutlineImage.type = Image.Type.Sliced;
                }
                glowOutlineImage.color = new Color(1f, 0.85f, 0.2f, 0f);
            }

            if (orbitMotes.Count == 0)
            {
                var motesParent = transform.Find("OrbitMotesContainer")?.gameObject;
                if (motesParent == null)
                {
                    motesParent = new GameObject("OrbitMotesContainer", typeof(RectTransform));
                    motesParent.transform.SetParent(transform, false);
                    motesParent.transform.SetSiblingIndex(cubeIconImage != null ? cubeIconImage.transform.GetSiblingIndex() + 1 : 1);
                    var mRt = motesParent.GetComponent<RectTransform>();
                    mRt.anchorMin = new Vector2(0.5f, 0.62f);
                    mRt.anchorMax = new Vector2(0.5f, 0.62f);
                    mRt.sizeDelta = Vector2.zero;
                }

                var starSprite = Resources.Load<Sprite>("Sprites/UI/UI_Star_Badge_Gold")
#if UNITY_EDITOR
                              ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/UI_Star_Badge_Gold.png")
#endif
                              ;

                for (int i = 0; i < 3; i++)
                {
                    var moteObj = new GameObject($"Mote_{i + 1}", typeof(RectTransform), typeof(Image));
                    moteObj.transform.SetParent(motesParent.transform, false);
                    var mRt = moteObj.GetComponent<RectTransform>();
                    mRt.sizeDelta = new Vector2(10, 10);
                    var img = moteObj.GetComponent<Image>();
                    img.sprite = starSprite;
                    img.preserveAspect = true;
                    img.raycastTarget = false;
                    img.color = new Color(1f, 0.92f, 0.45f, 0.85f);
                    orbitMotes.Add(mRt);
                }
            }

            EnsureAutoSummonBadge();
        }

        private void EnsureAutoSummonBadge()
        {
            if (autoSummonBadgeButton != null) return;

            var existing = transform.Find("AutoSummonBadge");
            if (existing != null)
            {
                autoSummonBadgeButton = existing.GetComponent<Button>();
                autoSummonBadgeBg = existing.GetComponent<Image>();
                autoSummonBadgeText = existing.GetComponentInChildren<Text>();
                return;
            }

            var badgeObj = new GameObject("AutoSummonBadge", typeof(RectTransform), typeof(Image), typeof(Button));
            badgeObj.transform.SetParent(transform, false);

            var rt = badgeObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.55f, 0.72f);
            rt.anchorMax = new Vector2(1.08f, 1.04f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            autoSummonBadgeBg = badgeObj.GetComponent<Image>();
            var pillSprite = Resources.Load<Sprite>("Sprites/UI/Btn_Secondary_Stone");
            if (pillSprite != null)
            {
                autoSummonBadgeBg.sprite = pillSprite;
                autoSummonBadgeBg.type = Image.Type.Sliced;
            }
            autoSummonBadgeBg.color = new Color(0.18f, 0.22f, 0.32f, 0.95f);

            var txtObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
            txtObj.transform.SetParent(badgeObj.transform, false);
            var tRt = txtObj.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.offsetMin = Vector2.zero;
            tRt.offsetMax = Vector2.zero;

            autoSummonBadgeText = txtObj.GetComponent<Text>();
            var lilita = Resources.Load<Font>("Fonts/LilitaOne-Regular");
            if (lilita != null) autoSummonBadgeText.font = lilita;
            autoSummonBadgeText.fontSize = 13;
            autoSummonBadgeText.fontStyle = FontStyle.Bold;
            autoSummonBadgeText.alignment = TextAnchor.MiddleCenter;
            autoSummonBadgeText.text = "AUTO";
            autoSummonBadgeText.color = Color.white;

            autoSummonBadgeButton = badgeObj.GetComponent<Button>();
            autoSummonBadgeButton.onClick.AddListener(OnAutoSummonBadgeClicked);

            UpdateAutoSummonBadgeVisual(Combat.AutoSummonAssistant.Instance != null && Combat.AutoSummonAssistant.Instance.IsAutoSummonEnabled);
        }

        private void OnAutoSummonBadgeClicked()
        {
            if (Combat.AutoSummonAssistant.Instance != null)
            {
                Combat.AutoSummonAssistant.Instance.ToggleAutoSummon();
            }
        }

        private void HandleAutoSummonToggled(bool isEnabled)
        {
            UpdateAutoSummonBadgeVisual(isEnabled);
        }

        private void UpdateAutoSummonBadgeVisual(bool isEnabled)
        {
            if (autoSummonBadgeText != null)
            {
                autoSummonBadgeText.text = isEnabled ? "AUTO ON" : "AUTO";
                autoSummonBadgeText.color = isEnabled ? new Color(0.2f, 1f, 0.5f) : new Color(0.85f, 0.9f, 0.98f);
            }
            if (autoSummonBadgeBg != null)
            {
                autoSummonBadgeBg.color = isEnabled ? new Color(0.12f, 0.45f, 0.25f, 0.98f) : new Color(0.18f, 0.22f, 0.32f, 0.95f);
            }
        }

        private void OnEnable()
        {
            EconomyManager.OnGemsChanged += HandleGemsChanged;
            EconomyManager.OnSummonCostChanged += HandleSummonCostChanged;
            HeroSummoner.OnHeroSummoned += HandleHeroSummoned;
            HeroSummoner.OnSummonFailed += HandleSummonFailed;
            GridManager.OnGridGenerated += UpdateButtonState;
            CitadelLocalizationManager.OnLanguageChanged += HandleLanguageChanged;
            Combat.AutoSummonAssistant.OnAutoSummonToggled += HandleAutoSummonToggled;

            UpdateButtonState();
            UpdateAutoSummonBadgeVisual(Combat.AutoSummonAssistant.Instance != null && Combat.AutoSummonAssistant.Instance.IsAutoSummonEnabled);
            if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
            pulseCoroutine = StartCoroutine(PulseGlowRoutine());
        }

        private void OnDisable()
        {
            EconomyManager.OnGemsChanged -= HandleGemsChanged;
            EconomyManager.OnSummonCostChanged -= HandleSummonCostChanged;
            HeroSummoner.OnHeroSummoned -= HandleHeroSummoned;
            HeroSummoner.OnSummonFailed -= HandleSummonFailed;
            GridManager.OnGridGenerated -= UpdateButtonState;
            CitadelLocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
            Combat.AutoSummonAssistant.OnAutoSummonToggled -= HandleAutoSummonToggled;

            if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
        }

        private void HandleSummonFailed(SummonFailureReason reason)
        {
            StartCoroutine(ShakeButtonRoutine());
        }

        private System.Collections.IEnumerator ShakeButtonRoutine()
        {
            Vector3 originalPos = transform.localPosition;
            float elapsed = 0f;
            float duration = 0.22f;
            float magnitude = 10f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float offsetX = Mathf.Sin(elapsed * 45f) * magnitude * (1f - elapsed / duration);
                transform.localPosition = originalPos + new Vector3(offsetX, 0, 0);
                yield return null;
            }

            transform.localPosition = originalPos;
        }

        private System.Collections.IEnumerator PulseGlowRoutine()
        {
            Vector3 baseScale = Vector3.one;
            float orbitSpeed = 1.9f;
            float radiusX = 36f;
            float radiusY = 22f;

            while (true)
            {
                if (button != null && button.interactable)
                {
                    float sine = Mathf.Sin(Time.unscaledTime * 3.5f);
                    float scaleFactor = 1.0f + sine * 0.035f;
                    if (cubeIconImage != null)
                    {
                        cubeIconImage.transform.localScale = baseScale * scaleFactor;
                    }
                    if (glowOutlineImage != null)
                    {
                        float alpha = 0.35f + sine * 0.25f;
                        glowOutlineImage.color = new Color(1f, 0.85f, 0.2f, Mathf.Clamp01(alpha));
                    }

                    // Rotating starlight motes orbiting the cube
                    for (int i = 0; i < orbitMotes.Count; i++)
                    {
                        var mote = orbitMotes[i];
                        if (mote != null)
                        {
                            mote.gameObject.SetActive(true);
                            float phase = i * (Mathf.PI * 2f / 3f);
                            float angle = Time.unscaledTime * orbitSpeed + phase;
                            float x = Mathf.Cos(angle) * radiusX;
                            float y = Mathf.Sin(angle) * radiusY;
                            mote.anchoredPosition = new Vector2(x, y);

                            float scale = 0.85f + 0.35f * Mathf.Sin(angle);
                            mote.localScale = Vector3.one * scale;
                            var img = mote.GetComponent<Image>();
                            if (img != null)
                            {
                                float moteAlpha = 0.45f + 0.45f * Mathf.Sin(angle + Mathf.PI * 0.5f);
                                img.color = new Color(1f, 0.95f, 0.50f, Mathf.Clamp01(moteAlpha));
                            }
                        }
                    }
                }
                else
                {
                    if (cubeIconImage != null) cubeIconImage.transform.localScale = baseScale;
                    if (glowOutlineImage != null) glowOutlineImage.color = new Color(1f, 0.85f, 0.2f, 0f);
                    for (int i = 0; i < orbitMotes.Count; i++)
                    {
                        if (orbitMotes[i] != null) orbitMotes[i].gameObject.SetActive(false);
                    }
                }
                yield return null;
            }
        }

        private void HandleLanguageChanged(AppLanguage lang)
        {
            UpdateButtonState();
        }

        private void OnSummonButtonClicked()
        {
            if (EconomyManager.Instance == null) return;

            int currentCost = EconomyManager.Instance.GetCurrentSummonCost();
            int currentGems = EconomyManager.Instance.CurrentGems;

            bool isPuzzle = FantasyDefense.Core.GameManager.Instance != null &&
                            FantasyDefense.Core.GameManager.Instance.CurrentBattleMode == BattleMode.PuzzlePreparation;

            if (currentGems < currentCost)
            {
                StartCoroutine(ShakeButtonRoutine());
                if (SkillVfxManager.Instance != null)
                {
                    SkillVfxManager.Instance.SpawnFloatingText(
                        transform.position + Vector3.up * 0.9f,
                        CitadelLocalizationManager.Text($"⚠️ Butuh {currentCost} Gems!", $"⚠️ Need {currentCost} Gems!"),
                        new Color(1f, 0.4f, 0.4f),
                        1.4f,
                        true
                    );
                }
                if (Audio.AudioManager.Instance != null)
                {
                    Audio.AudioManager.Instance.PlaySfx(Audio.AudioSfxType.ButtonClick);
                }
                return;
            }

            if (!isPuzzle && GridManager.Instance != null && GridManager.Instance.IsGridFull())
            {
                StartCoroutine(ShakeButtonRoutine());
                if (SkillVfxManager.Instance != null)
                {
                    SkillVfxManager.Instance.SpawnFloatingText(
                        transform.position + Vector3.up * 0.9f,
                        CitadelLocalizationManager.Text("⚠️ Arena Penuh! Gabungkan Hero!", "⚠️ Arena Full! Merge Heroes!"),
                        new Color(1f, 0.55f, 0.2f),
                        1.4f,
                        true
                    );
                }
                return;
            }

            if (isPuzzle && HeroSummoner.Instance != null && !HeroSummoner.Instance.HasCatalystCandidate())
            {
                StartCoroutine(ShakeButtonRoutine());
                if (SkillVfxManager.Instance != null)
                {
                    SkillVfxManager.Instance.SpawnFloatingText(
                        transform.position + Vector3.up * 0.9f,
                        CitadelLocalizationManager.Text("⭐ Semua Token Telah Tier 5 (MAX)!", "⭐ All Tokens at Max Tier!"),
                        new Color(1f, 0.85f, 0.2f),
                        1.4f,
                        true
                    );
                }
                return;
            }

            StartCoroutine(PunchScaleRoutine(transform, 1.15f, 0.18f));

            if (HeroSummoner.Instance != null)
            {
                HeroSummoner.Instance.TrySummonHero();
            }
        }

        private float nextStateCheckTime = 0f;

        private void Update()
        {
            if (Time.unscaledTime >= nextStateCheckTime)
            {
                nextStateCheckTime = Time.unscaledTime + 0.5f;
                UpdateButtonState();
            }
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

        private void HandleGemsChanged(int newGems)
        {
            UpdateButtonState();
        }

        private void HandleSummonCostChanged(int newCost)
        {
            UpdateCostDisplay(newCost);
            UpdateButtonState();
        }

        private void HandleHeroSummoned(Combat.UnitController unit, TileCell cell)
        {
            UpdateButtonState();
        }

        public void UpdateButtonState()
        {
            if (EconomyManager.Instance == null) return;

            int currentCost = EconomyManager.Instance.GetCurrentSummonCost();
            int currentGems = EconomyManager.Instance.CurrentGems;

            bool isPuzzle = FantasyDefense.Core.GameManager.Instance != null &&
                            FantasyDefense.Core.GameManager.Instance.CurrentBattleMode == BattleMode.PuzzlePreparation;

            bool canAfford = currentGems >= currentCost;
            bool gridHasSpace = isPuzzle
                ? (HeroSummoner.Instance == null || HeroSummoner.Instance.HasCatalystCandidate())
                : (GridManager.Instance == null || !GridManager.Instance.IsGridFull());

            // Tetap izinkan sentuhan tombol aktif agar pemain mendapatkan feedback suara & teks melayang edukatif
            if (button != null)
            {
                button.interactable = true;
                var img = button.GetComponent<Image>();
                if (img != null)
                {
                    img.color = (canAfford && gridHasSpace) ? Color.white : new Color(0.85f, 0.85f, 0.90f, 0.95f);
                }
            }

            UpdateCostDisplay(currentCost);
        }

        private void UpdateCostDisplay(int cost)
        {
            if (costText != null)
            {
                bool isPuzzle = FantasyDefense.Core.GameManager.Instance != null &&
                                FantasyDefense.Core.GameManager.Instance.CurrentBattleMode == BattleMode.PuzzlePreparation;

                if (isPuzzle)
                {
                    bool hasCandidates = (HeroSummoner.Instance == null || HeroSummoner.Instance.HasCatalystCandidate());
                    if (!hasCandidates)
                    {
                        costText.text = CitadelLocalizationManager.Text("⭐ TOKEN MAX", "⭐ MAX TIER");
                    }
                    else
                    {
                        string label = CitadelLocalizationManager.Text("KATALIS SUMMON", "SUMMON CUBE");
                        costText.text = $"✦ {label} ✦\n({cost} GEMS)";
                    }
                }
                else if (GridManager.Instance != null && GridManager.Instance.IsGridFull())
                {
                    costText.text = CitadelLocalizationManager.Text("GRID PENUH", "GRID FULL");
                }
                else
                {
                    string summonLabel = CitadelLocalizationManager.Text("PANGGIL HERO", "SUMMON HERO");
                    costText.text = $"✦ {summonLabel} ✦\n({cost} GEMS)";
                }
            }
        }
    }
}
