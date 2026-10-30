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
                }
                else
                {
                    if (cubeIconImage != null) cubeIconImage.transform.localScale = baseScale;
                    if (glowOutlineImage != null) glowOutlineImage.color = new Color(1f, 0.85f, 0.2f, 0f);
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
