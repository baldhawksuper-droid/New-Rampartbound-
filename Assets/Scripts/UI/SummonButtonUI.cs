using UnityEngine;
using UnityEngine.UI;
using FantasyDefense.Economy;
using FantasyDefense.GridSystem;
using FantasyDefense.Summon;
using FantasyDefense.Localization;

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
        }

        private void OnEnable()
        {
            EconomyManager.OnGemsChanged += HandleGemsChanged;
            EconomyManager.OnSummonCostChanged += HandleSummonCostChanged;
            HeroSummoner.OnHeroSummoned += HandleHeroSummoned;
            GridManager.OnGridGenerated += UpdateButtonState;
            CitadelLocalizationManager.OnLanguageChanged += HandleLanguageChanged;

            UpdateButtonState();
            if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
            pulseCoroutine = StartCoroutine(PulseGlowRoutine());
        }

        private void OnDisable()
        {
            EconomyManager.OnGemsChanged -= HandleGemsChanged;
            EconomyManager.OnSummonCostChanged -= HandleSummonCostChanged;
            HeroSummoner.OnHeroSummoned -= HandleHeroSummoned;
            GridManager.OnGridGenerated -= UpdateButtonState;
            CitadelLocalizationManager.OnLanguageChanged -= HandleLanguageChanged;

            if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
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
            StartCoroutine(PunchScaleRoutine(transform, 1.15f, 0.18f));

            if (HeroSummoner.Instance != null)
            {
                HeroSummoner.Instance.TrySummonHero();
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
            bool gridHasSpace = isPuzzle || (GridManager.Instance == null || !GridManager.Instance.IsGridFull());

            bool isInteractable = canAfford && gridHasSpace;

            if (button != null)
            {
                button.interactable = isInteractable;
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
                    string label = CitadelLocalizationManager.Text("KATALIS SUMMON", "SUMMON CUBE");
                    costText.text = $"✦ {label} ✦\n({cost} GEMS)";
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
