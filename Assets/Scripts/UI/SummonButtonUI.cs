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
        [Header("Button References")]
        [SerializeField] private Button button;
        [SerializeField] private Text costText;

        [Header("Visual Feedback")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.7f);

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();

            button.onClick.AddListener(OnSummonButtonClicked);
        }

        private void OnEnable()
        {
            EconomyManager.OnGemsChanged += HandleGemsChanged;
            EconomyManager.OnSummonCostChanged += HandleSummonCostChanged;
            HeroSummoner.OnHeroSummoned += HandleHeroSummoned;
            GridManager.OnGridGenerated += UpdateButtonState;
            CitadelLocalizationManager.OnLanguageChanged += HandleLanguageChanged;

            UpdateButtonState();
        }

        private void OnDisable()
        {
            EconomyManager.OnGemsChanged -= HandleGemsChanged;
            EconomyManager.OnSummonCostChanged -= HandleSummonCostChanged;
            HeroSummoner.OnHeroSummoned -= HandleHeroSummoned;
            GridManager.OnGridGenerated -= UpdateButtonState;
            CitadelLocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
        }

        private void HandleLanguageChanged(AppLanguage lang)
        {
            UpdateButtonState();
        }

        private void OnSummonButtonClicked()
        {
            if (HeroSummoner.Instance != null)
            {
                HeroSummoner.Instance.TrySummonHero();
            }
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
