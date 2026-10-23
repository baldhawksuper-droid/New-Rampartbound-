using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FantasyDefense.Audio;
using FantasyDefense.Core;
using FantasyDefense.Data;
using FantasyDefense.Metagame;

namespace FantasyDefense.UI
{
    public enum ForgeTab
    {
        Refine,
        UpgradeRarity,
        Salvage
    }

    /// <summary>
    /// Master UI Controller untuk Citadel Relic Forge & Gear Crafting.
    /// Memungkinkan pemain menempa perlengkapan (+0 -> +5), menaikkan kelangkaan (Common -> Legendary),
    /// serta melebur gear duplikat untuk mendapatkan Forge Dust dan Refine Stones.
    /// </summary>
    public class UIRelicForgeView : MonoBehaviour
    {
        public static UIRelicForgeView Instance { get; private set; }

        [Header("Root Panel")]
        [SerializeField] private GameObject forgeRootPanel;

        [Header("Currency Counters")]
        [SerializeField] private Text forgeDustText;
        [SerializeField] private Text refineStonesText;
        [SerializeField] private Text goldText;

        [Header("Tab Buttons")]
        [SerializeField] private Button refineTabButton;
        [SerializeField] private Button rarityTabButton;
        [SerializeField] private Button salvageTabButton;
        [SerializeField] private ForgeTab currentTab = ForgeTab.Refine;

        [Header("Selected Item Display")]
        [SerializeField] private Text itemNameText;
        [SerializeField] private Text itemSlotText;
        [SerializeField] private Text itemRarityText;
        [SerializeField] private Text currentStatText;
        [SerializeField] private Text upgradedStatPreviewText;
        [SerializeField] private Text actionCostText;

        [Header("Action Buttons")]
        [SerializeField] private Button actionButton;
        [SerializeField] private Text actionButtonLabel;
        [SerializeField] private Button closeButton;

        [Header("Feedback Message")]
        [SerializeField] private Text feedbackMessageText;

        private EquipmentItem selectedItem;
        private UnitType? selectedHeroOwner;

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
            CitadelLobbyManager.OnForgeMaterialsChanged += HandleForgeMaterialsChanged;
            CitadelLobbyManager.OnGoldChanged += HandleGoldChanged;
            RelicForgeManager.OnEquipmentRefined += HandleEquipmentRefined;
            RelicForgeManager.OnEquipmentRarityUpgraded += HandleRarityUpgraded;
            RelicForgeManager.OnEquipmentSalvaged += HandleEquipmentSalvaged;
            RelicForgeManager.OnForgeActionFailed += HandleActionFailed;
        }

        private void OnDisable()
        {
            CitadelLobbyManager.OnForgeMaterialsChanged -= HandleForgeMaterialsChanged;
            CitadelLobbyManager.OnGoldChanged -= HandleGoldChanged;
            RelicForgeManager.OnEquipmentRefined -= HandleEquipmentRefined;
            RelicForgeManager.OnEquipmentRarityUpgraded -= HandleRarityUpgraded;
            RelicForgeManager.OnEquipmentSalvaged -= HandleEquipmentSalvaged;
            RelicForgeManager.OnForgeActionFailed -= HandleActionFailed;
        }

        private void InitializeButtons()
        {
            if (refineTabButton != null)
                refineTabButton.onClick.AddListener(() => SwitchTab(ForgeTab.Refine));

            if (rarityTabButton != null)
                rarityTabButton.onClick.AddListener(() => SwitchTab(ForgeTab.UpgradeRarity));

            if (salvageTabButton != null)
                salvageTabButton.onClick.AddListener(() => SwitchTab(ForgeTab.Salvage));

            if (actionButton != null)
                actionButton.onClick.AddListener(OnActionButtonClicked);

            if (closeButton != null)
                closeButton.onClick.AddListener(CloseForge);
        }

        public void OpenView() => OpenForge();
        public void CloseView() => CloseForge();

        public void OpenForge(EquipmentItem targetItem = null, UnitType? owner = null)
        {
            transform.SetAsLastSibling();
            if (forgeRootPanel != null)
            {
                forgeRootPanel.transform.SetAsLastSibling();
                forgeRootPanel.SetActive(true);
            }
            else
                gameObject.SetActive(true);

            CanvasGroup cg = GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }

            selectedItem = targetItem;
            selectedHeroOwner = owner;

            // Jika item belum dipilih, pilih item pertama dari inventaris
            if (selectedItem == null && CitadelLobbyManager.Instance != null)
            {
                var inv = CitadelLobbyManager.Instance.AccountData.equipmentInventory;
                if (inv.Count > 0)
                {
                    selectedItem = inv[0];
                }
            }

            RefreshMaterialCounters();
            RefreshDisplay();

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.ButtonClick);
            }
        }

        public void CloseForge()
        {
            if (forgeRootPanel != null)
                forgeRootPanel.SetActive(false);
            else
                gameObject.SetActive(false);

            CanvasGroup cg = GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.blocksRaycasts = false;
            }

            if (GameFlowManager.Instance != null && GameFlowManager.Instance.CurrentActivePanel == GamePanelType.RelicForge)
            {
                GameFlowManager.Instance.CloseCurrentPanel();
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.ButtonClick);
            }
        }

        public void SwitchTab(ForgeTab tab)
        {
            currentTab = tab;
            RefreshDisplay();

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.ButtonClick);
            }
        }

        public void SetSelectedItem(EquipmentItem item, UnitType? owner = null)
        {
            selectedItem = item;
            selectedHeroOwner = owner;
            RefreshDisplay();
        }

        public void RefreshDisplay()
        {
            RefreshMaterialCounters();

            if (selectedItem == null)
            {
                if (itemNameText != null) itemNameText.text = "Pilih Perlengkapan";
                if (itemSlotText != null) itemSlotText.text = "-";
                if (itemRarityText != null) itemRarityText.text = "-";
                if (currentStatText != null) currentStatText.text = "Tidak ada item terpilih.";
                if (upgradedStatPreviewText != null) upgradedStatPreviewText.text = "";
                if (actionCostText != null) actionCostText.text = "";
                if (actionButton != null) actionButton.interactable = false;
                return;
            }

            // Tampilkan identitas item
            if (itemNameText != null)
            {
                string refineStr = selectedItem.refineLevel > 0 ? $" +{selectedItem.refineLevel}" : "";
                itemNameText.text = $"{selectedItem.itemName}{refineStr}";
            }

            if (itemSlotText != null)
                itemSlotText.text = $"Slot: {selectedItem.slot}";

            if (itemRarityText != null)
            {
                itemRarityText.text = $"Kualitas: {selectedItem.rarity}";
                itemRarityText.color = selectedItem.GetRarityColor();
            }

            // Update berdasarkan Tab aktif
            switch (currentTab)
            {
                case ForgeTab.Refine:
                    UpdateRefineTabDisplay();
                    break;
                case ForgeTab.UpgradeRarity:
                    UpdateRarityTabDisplay();
                    break;
                case ForgeTab.Salvage:
                    UpdateSalvageTabDisplay();
                    break;
            }
        }

        private void UpdateRefineTabDisplay()
        {
            if (actionButtonLabel != null) actionButtonLabel.text = "TEMPA SEKARANG";

            if (currentStatText != null)
                currentStatText.text = $"Stat Sekarang:\nATK: {selectedItem.EffectiveAttack:F1} | HP: {selectedItem.EffectiveHealth:F0} | CP: {selectedItem.CalculateItemCP()}";

            if (selectedItem.refineLevel >= 5)
            {
                if (upgradedStatPreviewText != null) upgradedStatPreviewText.text = "★ Level Tempa Maksimal (+5) ★";
                if (actionCostText != null) actionCostText.text = "Maksimal";
                if (actionButton != null) actionButton.interactable = false;
                return;
            }

            float nextMul = 1.0f + ((selectedItem.refineLevel + 1) * 0.10f);
            float nextAtk = selectedItem.bonusAttack * nextMul;
            float nextHp = selectedItem.bonusHealth * nextMul;

            if (upgradedStatPreviewText != null)
                upgradedStatPreviewText.text = $"Prediksi (+{selectedItem.refineLevel + 1}):\nATK: {nextAtk:F1} (+10%) | HP: {nextHp:F0} (+10%)";

            selectedItem.GetRefineCost(out int goldCost, out int dustCost, out int stonesCost);
            if (actionCostText != null)
            {
                string stoneStr = stonesCost > 0 ? $", {stonesCost} Stones" : "";
                actionCostText.text = $"Biaya: {goldCost:N0} Gold, {dustCost} Dust{stoneStr}";
            }

            var lobby = CitadelLobbyManager.Instance;
            bool canAfford = lobby != null && lobby.GoldCoins >= goldCost && lobby.ForgeDust >= dustCost && lobby.RefineStones >= stonesCost;
            if (actionButton != null) actionButton.interactable = canAfford;
        }

        private void UpdateRarityTabDisplay()
        {
            if (actionButtonLabel != null) actionButtonLabel.text = "PROMOSI KUALITAS";

            if (currentStatText != null)
                currentStatText.text = $"Tingkat: {selectedItem.rarity}\nATK: {selectedItem.EffectiveAttack:F1} | HP: {selectedItem.EffectiveHealth:F0}";

            if (selectedItem.rarity == EquipmentRarity.Legendary)
            {
                if (upgradedStatPreviewText != null) upgradedStatPreviewText.text = "★ Kualitas Tertinggi (Legendary) ★";
                if (actionCostText != null) actionCostText.text = "Maksimal";
                if (actionButton != null) actionButton.interactable = false;
                return;
            }

            EquipmentRarity nextRarity = (EquipmentRarity)((int)selectedItem.rarity + 1);
            if (upgradedStatPreviewText != null)
                upgradedStatPreviewText.text = $"Promosi ke -> {nextRarity}\nStat Meningkat +30% Permanen!";

            selectedItem.GetRarityUpgradeCost(out int goldCost, out int dustCost, out int stonesCost);
            if (actionCostText != null)
                actionCostText.text = $"Biaya: {goldCost:N0} Gold, {dustCost} Dust, {stonesCost} Stones";

            var lobby = CitadelLobbyManager.Instance;
            bool canAfford = lobby != null && lobby.GoldCoins >= goldCost && lobby.ForgeDust >= dustCost && lobby.RefineStones >= stonesCost;
            if (actionButton != null) actionButton.interactable = canAfford;
        }

        private void UpdateSalvageTabDisplay()
        {
            if (actionButtonLabel != null) actionButtonLabel.text = "LEBUR (SALVAGE)";

            if (currentStatText != null)
                currentStatText.text = $"Item Tas: {selectedItem.itemName}\nPeleburan akan menghancurkan item ini secara permanen.";

            selectedItem.GetSalvageYield(out int dustYield, out int stonesYield);
            if (upgradedStatPreviewText != null)
            {
                string stoneStr = stonesYield > 0 ? $"\n+{stonesYield} Refine Stones" : "";
                upgradedStatPreviewText.text = $"Hasil Peleburan:\n+{dustYield} Forge Dust{stoneStr}";
            }

            if (actionCostText != null)
                actionCostText.text = "Biaya: Gratis";

            // Pastikan item ada di inventaris (bukan sedang dipakai)
            bool isInInventory = CitadelLobbyManager.Instance != null &&
                                 CitadelLobbyManager.Instance.AccountData.equipmentInventory.Contains(selectedItem);

            if (actionButton != null) actionButton.interactable = isInInventory;
        }

        private void OnActionButtonClicked()
        {
            if (RelicForgeManager.Instance == null || selectedItem == null) return;

            switch (currentTab)
            {
                case ForgeTab.Refine:
                    RelicForgeManager.Instance.TryRefineEquipment(selectedItem, selectedHeroOwner);
                    break;
                case ForgeTab.UpgradeRarity:
                    RelicForgeManager.Instance.TryUpgradeEquipmentRarity(selectedItem, selectedHeroOwner);
                    break;
                case ForgeTab.Salvage:
                    RelicForgeManager.Instance.TrySalvageEquipment(selectedItem, out _, out _);
                    selectedItem = null;
                    break;
            }

            RefreshDisplay();
        }

        private void RefreshMaterialCounters()
        {
            if (CitadelLobbyManager.Instance == null) return;

            if (forgeDustText != null)
                forgeDustText.text = $"{CitadelLobbyManager.Instance.ForgeDust}";

            if (refineStonesText != null)
                refineStonesText.text = $"{CitadelLobbyManager.Instance.RefineStones}";

            if (goldText != null)
                goldText.text = $"{CitadelLobbyManager.Instance.GoldCoins:N0}";
        }

        private void HandleForgeMaterialsChanged(int dust, int stones)
        {
            RefreshMaterialCounters();
            RefreshDisplay();
        }

        private void HandleGoldChanged(int newGold)
        {
            RefreshMaterialCounters();
            RefreshDisplay();
        }

        private void HandleEquipmentRefined(EquipmentItem item)
        {
            ShowFeedback($"Sukses! {item.itemName} ditempa menjadi +{item.refineLevel}!");
            RefreshDisplay();
        }

        private void HandleRarityUpgraded(EquipmentItem item)
        {
            ShowFeedback($"Luar biasa! {item.itemName} berhasil dipromosikan ke {item.rarity}!");
            RefreshDisplay();
        }

        private void HandleEquipmentSalvaged(EquipmentItem item, int dust, int stones)
        {
            ShowFeedback($"Item dilebur! Memperoleh +{dust} Dust dan +{stones} Stones.");
            RefreshDisplay();
        }

        private void HandleActionFailed(string err)
        {
            ShowFeedback($"Peringatan: {err}");
        }

        private void ShowFeedback(string msg)
        {
            if (feedbackMessageText != null)
            {
                feedbackMessageText.text = msg;
            }
        }
    }
}
