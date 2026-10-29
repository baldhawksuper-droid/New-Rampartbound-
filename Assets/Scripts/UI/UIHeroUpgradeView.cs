using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FantasyDefense.Audio;
using FantasyDefense.Core;
using FantasyDefense.Data;
using FantasyDefense.Metagame;

namespace FantasyDefense.UI
{
    /// <summary>
    /// Master UI Controller untuk layar Upgrade Pahlawan & Manajemen Perlengkapan di Citadel Lobby.
    /// Memungkinkan pemain menaikkan level pahlawan menggunakan Gold, melihat peningkatan ATK/HP/Crit/CP,
    /// serta memeriksa status equipment senjata, armor, dan relik.
    /// </summary>
    public class UIHeroUpgradeView : MonoBehaviour
    {
        public static UIHeroUpgradeView Instance { get; private set; }

        [Header("Root & Navigation")]
        [SerializeField] private GameObject rootPanel;
        [SerializeField] private Button closeButton;

        [Header("Hero Selection")]
        [SerializeField] private UnitType selectedHeroType = UnitType.Cedric;
        [SerializeField] private Button cedricTabButton;
        [SerializeField] private Button rabbitTabButton;
        [SerializeField] private Button roseTabButton;
        [SerializeField] private Button windRangerTabButton;

        [Header("Hero Details Display")]
        [SerializeField] private Text heroNameText;
        [SerializeField] private Text heroRoleText;
        [SerializeField] private Text heroLevelText;
        [SerializeField] private Text heroCpText;
        [SerializeField] private Image heroAvatarImage;

        [Header("Hero Avatar Sprites")]
        [SerializeField] private Sprite cedricAvatar;
        [SerializeField] private Sprite roseAvatar;
        [SerializeField] private Sprite rabbitAvatar;
        [SerializeField] private Sprite windRangerAvatar;

        [Header("Stat Panels")]
        [SerializeField] private Text attackStatText;
        [SerializeField] private Text healthStatText;
        [SerializeField] private Text critStatText;

        [Header("Upgrade Action")]
        [SerializeField] private Text upgradeCostText;
        [SerializeField] private Text upgradePreviewText;
        [SerializeField] private Button upgradeHeroButton;

        [Header("Equipment Slots")]
        [SerializeField] private Text weaponSlotText;
        [SerializeField] private Text armorSlotText;
        [SerializeField] private Text relicSlotText;

        [Header("Equipment Actions")]
        [SerializeField] private Button autoEquipButton;
        [SerializeField] private Button unequipAllButton;
        [SerializeField] private Button deployToFormationButton;
        [SerializeField] private Text deployButtonText;

        [Header("Forge Shortcut (Optional)")]
        [SerializeField] private Button openForgeButton;

        [Header("Feedback Banner")]
        [SerializeField] private Text feedbackMessageText;

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
            CitadelLobbyManager.OnGoldChanged += HandleGoldChanged;
            CitadelLobbyManager.OnHeroUpgraded += HandleHeroUpgraded;
            CitadelLobbyManager.OnHeroEquipmentChanged += HandleHeroEquipmentChanged;
            FantasyDefense.Localization.CitadelLocalizationManager.OnLanguageChanged += HandleLanguageChanged;
        }

        private void OnDisable()
        {
            CitadelLobbyManager.OnGoldChanged -= HandleGoldChanged;
            CitadelLobbyManager.OnHeroUpgraded -= HandleHeroUpgraded;
            CitadelLobbyManager.OnHeroEquipmentChanged -= HandleHeroEquipmentChanged;
            FantasyDefense.Localization.CitadelLocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
        }

        private void Start()
        {
            SelectHero(UnitType.Cedric);
        }

        private void HandleLanguageChanged(FantasyDefense.Localization.AppLanguage lang)
        {
            RefreshDisplay();
        }

        private void InitializeButtons()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(CloseView);

            if (cedricTabButton != null)
                cedricTabButton.onClick.AddListener(() => SelectHero(UnitType.Cedric));

            if (rabbitTabButton != null)
                rabbitTabButton.onClick.AddListener(() => SelectHero(UnitType.RabbitMage));

            if (roseTabButton != null)
                roseTabButton.onClick.AddListener(() => SelectHero(UnitType.RoseTanker));

            if (windRangerTabButton != null)
                windRangerTabButton.onClick.AddListener(() => SelectHero(UnitType.WindRanger));

            if (upgradeHeroButton != null)
                upgradeHeroButton.onClick.AddListener(OnUpgradeButtonClicked);

            if (autoEquipButton != null)
                autoEquipButton.onClick.AddListener(AutoEquipBestItems);

            if (unequipAllButton != null)
                unequipAllButton.onClick.AddListener(UnequipAllItems);

            if (deployToFormationButton != null)
                deployToFormationButton.onClick.AddListener(OnDeployButtonClicked);

            if (openForgeButton != null)
                openForgeButton.onClick.AddListener(OnOpenForgeClicked);
        }

        public void OpenView()
        {
            if (rootPanel != null) rootPanel.SetActive(true);
            else gameObject.SetActive(true);

            // Bawa panel ke tumpukan terdepan
            transform.SetAsLastSibling();

            var cg = GetComponent<CanvasGroup>() ?? rootPanel?.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 1f;
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }

            RefreshDisplay();
        }

        public void CloseView()
        {
            if (rootPanel != null) rootPanel.SetActive(false);
            else gameObject.SetActive(false);

            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.CloseCurrentPanel();
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.ButtonClick);
            }
        }

        public void SelectHero(UnitType heroType)
        {
            selectedHeroType = heroType;
            RefreshDisplay();

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.ButtonClick);
            }
        }

        public void RefreshDisplay()
        {
            if (CitadelLobbyManager.Instance == null) return;

            HeroLobbyProfile profile = CitadelLobbyManager.Instance.GetHeroProfile(selectedHeroType);
            if (profile == null) return;

            Font defaultFont = Resources.Load<Font>("Fonts/LilitaOne-Regular");
            if (defaultFont != null)
            {
                if (heroNameText != null) heroNameText.font = defaultFont;
                if (heroRoleText != null) heroRoleText.font = defaultFont;
                if (heroLevelText != null) heroLevelText.font = defaultFont;
                if (heroCpText != null) heroCpText.font = defaultFont;
                if (attackStatText != null) attackStatText.font = defaultFont;
                if (healthStatText != null) healthStatText.font = defaultFont;
                if (critStatText != null) critStatText.font = defaultFont;
                if (upgradeCostText != null) upgradeCostText.font = defaultFont;
                if (upgradePreviewText != null) upgradePreviewText.font = defaultFont;
                if (weaponSlotText != null) weaponSlotText.font = defaultFont;
                if (armorSlotText != null) armorSlotText.font = defaultFont;
                if (relicSlotText != null) relicSlotText.font = defaultFont;
                if (feedbackMessageText != null) feedbackMessageText.font = defaultFont;
                if (upgradeHeroButton != null)
                {
                    var uTxt = upgradeHeroButton.GetComponentInChildren<Text>();
                    if (uTxt != null) uTxt.font = defaultFont;
                }
                if (autoEquipButton != null)
                {
                    var aeTxt = autoEquipButton.GetComponentInChildren<Text>();
                    if (aeTxt != null) aeTxt.font = defaultFont;
                }
                if (unequipAllButton != null)
                {
                    var ueTxt = unequipAllButton.GetComponentInChildren<Text>();
                    if (ueTxt != null) ueTxt.font = defaultFont;
                }
                if (deployToFormationButton != null)
                {
                    var dpTxt = deployToFormationButton.GetComponentInChildren<Text>();
                    if (dpTxt != null) dpTxt.font = defaultFont;
                }
            }

            // Update Teks Informasi
            if (heroNameText != null) heroNameText.text = profile.heroName;
            if (heroRoleText != null) heroRoleText.text = GetHeroRoleDescription(selectedHeroType);
            if (heroLevelText != null) heroLevelText.text = $"Level {profile.level}";
            if (heroCpText != null) heroCpText.text = $"Combat Power: {profile.combatPower:N0}";

            // Update Avatar Sprite
            if (heroAvatarImage != null)
            {
                Sprite avatar = GetHeroAvatarSprite(selectedHeroType);
                if (avatar != null)
                {
                    heroAvatarImage.sprite = avatar;
                    heroAvatarImage.enabled = true;
                }
            }

            // Update Statistik Pertempuran
            if (attackStatText != null)
                attackStatText.text = $"ATK: {profile.GetTotalAttack():F1}";

            if (healthStatText != null)
                healthStatText.text = $"HP: {profile.GetTotalHealth():F0}";

            if (critStatText != null)
                critStatText.text = $"Crit: {profile.GetTotalCritChance() * 100f:F1}%";

            // Update Tombol Upgrade & Biaya
            int upgradeCost = profile.GetUpgradeGoldCost();
            int currentGold = CitadelLobbyManager.Instance.GoldCoins;
            bool canAfford = currentGold >= upgradeCost;

            if (upgradeCostText != null)
            {
                upgradeCostText.text = $"Upgrade: {upgradeCost:N0} Gold";
            }

            // Update Tombol Action & Localization
            if (autoEquipButton != null)
            {
                var txt = autoEquipButton.GetComponentInChildren<Text>();
                if (txt != null) txt.text = FantasyDefense.Localization.CitadelLocalizationManager.Text("⚔️ PASANG TERBAIK", "⚔️ AUTO-EQUIP");
            }

            if (unequipAllButton != null)
            {
                var txt = unequipAllButton.GetComponentInChildren<Text>();
                if (txt != null) txt.text = FantasyDefense.Localization.CitadelLocalizationManager.Text("📦 LEPAS SEMUA", "📦 UNEQUIP ALL");
            }

            // Status Pemasangan ke Formasi (Slot 1 - 4)
            var currentFormation = CitadelLobbyManager.Instance.GetDeployedFormation();
            int slotIdx = currentFormation.IndexOf(selectedHeroType);
            bool isDeployed = slotIdx != -1;

            if (deployToFormationButton != null)
            {
                deployToFormationButton.interactable = profile.isUnlocked;
                var txt = deployButtonText != null ? deployButtonText : deployToFormationButton.GetComponentInChildren<Text>();
                if (txt != null)
                {
                    if (!profile.isUnlocked)
                    {
                        txt.text = FantasyDefense.Localization.CitadelLocalizationManager.Text("🔒 TERKUNCI (GACHA)", "🔒 LOCKED (GACHA)");
                    }
                    else if (isDeployed)
                    {
                        txt.text = FantasyDefense.Localization.CitadelLocalizationManager.Text($"🛡️ DIPASANG (SLOT {slotIdx + 1})", $"🛡️ DEPLOYED (SLOT {slotIdx + 1})");
                    }
                    else
                    {
                        txt.text = FantasyDefense.Localization.CitadelLocalizationManager.Text("➕ PASANG KE FORMASI", "➕ DEPLOY TO FORMATION");
                    }
                }
            }

            if (upgradeHeroButton != null)
            {
                upgradeHeroButton.interactable = canAfford;
                var txt = upgradeHeroButton.GetComponentInChildren<Text>();
                if (txt != null) txt.text = FantasyDefense.Localization.CitadelLocalizationManager.Text("TINGKATKAN LEVEL", "LEVEL UP HERO");
            }

            if (upgradePreviewText != null)
            {
                float nextAtk = (profile.baseAttack * (1f + (profile.level * 0.075f))) + GetBonusAtk(profile);
                float nextHp = (profile.baseHealth * (1f + (profile.level * 0.09f))) + GetBonusHp(profile);
                string previewPrefix = FantasyDefense.Localization.CitadelLocalizationManager.Text("Naik Level:", "Next Level:");
                upgradePreviewText.text = $"{previewPrefix} ATK +{(nextAtk - profile.GetTotalAttack()):F1} | HP +{(nextHp - profile.GetTotalHealth()):F0}";
            }

            // Sembunyikan shortcut Tempa karena menu Tempa sedang dinonaktifkan
            if (openForgeButton != null)
            {
                openForgeButton.gameObject.SetActive(false);
            }

            // Update Slot Equipment
            UpdateEquipmentSlotDisplay(profile);
        }

        private void OnDeployButtonClicked()
        {
            if (CitadelLobbyManager.Instance == null) return;

            var profile = CitadelLobbyManager.Instance.GetHeroProfile(selectedHeroType);
            if (profile == null) return;

            if (!profile.isUnlocked)
            {
                string msg = FantasyDefense.Localization.CitadelLocalizationManager.Text(
                    $"🔒 {profile.heroName} masih terkunci! Dapatkan melalui Balai Gacha.",
                    $"🔒 {profile.heroName} is locked! Summon in the Gacha Hall."
                );
                CitadelToastNotice.Show(msg, 3.0f);
                return;
            }

            // Cari slot yang kosong atau ganti slot ke-4 / slot rotasi
            var formation = CitadelLobbyManager.Instance.GetDeployedFormation();
            int currentSlot = formation.IndexOf(selectedHeroType);

            int targetSlot = 3; // Default ke slot ke-4 jika belum terpasang
            if (currentSlot != -1)
            {
                // Jika sudah terpasang di salah satu slot, rotasikan ke slot berikutnya
                targetSlot = (currentSlot + 1) % 4;
            }

            bool success = CitadelLobbyManager.Instance.TryAssignFormationSlot(targetSlot, selectedHeroType, out string resultMsg);
            if (success)
            {
                CitadelToastNotice.Show($"✅ {resultMsg}", 2.8f);
                RefreshDisplay();
            }
            else
            {
                CitadelToastNotice.Show($"⚠️ {resultMsg}", 3.0f);
            }
        }

        private float GetBonusAtk(HeroLobbyProfile profile)
        {
            float bonus = 0f;
            foreach (var item in profile.equippedItems) if (item != null) bonus += item.bonusAttack;
            return bonus;
        }

        private float GetBonusHp(HeroLobbyProfile profile)
        {
            float bonus = 0f;
            foreach (var item in profile.equippedItems) if (item != null) bonus += item.bonusHealth;
            return bonus;
        }

        private void UpdateEquipmentSlotDisplay(HeroLobbyProfile profile)
        {
            var weapon = profile.GetEquippedItem(EquipmentSlot.Weapon);
            var armor = profile.GetEquippedItem(EquipmentSlot.Armor);
            var relic = profile.GetEquippedItem(EquipmentSlot.Relic);

            string emptyWeapon = FantasyDefense.Localization.CitadelLocalizationManager.Text("[Senjata] Kosong", "[Weapon] Empty");
            string emptyArmor = FantasyDefense.Localization.CitadelLocalizationManager.Text("[Zirah] Kosong", "[Armor] Empty");
            string emptyRelic = FantasyDefense.Localization.CitadelLocalizationManager.Text("[Relik] Kosong", "[Relic] Empty");

            string weaponLabel = FantasyDefense.Localization.CitadelLocalizationManager.Text("[Senjata]", "[Weapon]");
            string armorLabel = FantasyDefense.Localization.CitadelLocalizationManager.Text("[Zirah]", "[Armor]");
            string relicLabel = FantasyDefense.Localization.CitadelLocalizationManager.Text("[Relik]", "[Relic]");

            if (weaponSlotText != null)
                weaponSlotText.text = weapon != null ? $"{weaponLabel} {weapon.itemName} (+{weapon.bonusAttack} ATK)" : emptyWeapon;

            if (armorSlotText != null)
                armorSlotText.text = armor != null ? $"{armorLabel} {armor.itemName} (+{armor.bonusHealth} HP)" : emptyArmor;

            if (relicSlotText != null)
                relicSlotText.text = relic != null ? $"{relicLabel} {relic.itemName} (+{relic.bonusCritChance * 100}% Crit)" : emptyRelic;
        }

        public void AutoEquipBestItems()
        {
            if (CitadelLobbyManager.Instance == null) return;
            var profile = CitadelLobbyManager.Instance.GetHeroProfile(selectedHeroType);
            if (profile == null) return;

            var inv = CitadelLobbyManager.Instance.AccountData.equipmentInventory;
            if (inv == null || inv.Count == 0)
            {
                if (feedbackMessageText != null)
                    feedbackMessageText.text = "Inventaris tas kosong. Taklukkan stage pertempuran untuk meraih perlengkapan!";
                return;
            }

            int equippedCount = 0;
            EquipmentSlot[] slots = { EquipmentSlot.Weapon, EquipmentSlot.Armor, EquipmentSlot.Relic };

            foreach (var slot in slots)
            {
                var currentItem = profile.GetEquippedItem(slot);
                EquipmentItem bestCandidate = null;
                float currentScore = currentItem != null ? currentItem.CalculateItemCP() : -1f;
                float highestScore = currentScore;

                foreach (var item in inv)
                {
                    if (item != null && item.slot == slot)
                    {
                        float score = item.CalculateItemCP();
                        if (score > highestScore)
                        {
                            highestScore = score;
                            bestCandidate = item;
                        }
                    }
                }

                if (bestCandidate != null)
                {
                    CitadelLobbyManager.Instance.TryEquipHeroItem(selectedHeroType, bestCandidate);
                    equippedCount++;
                }
            }

            if (equippedCount > 0)
            {
                if (feedbackMessageText != null)
                    feedbackMessageText.text = $"Berhasil memasang {equippedCount} perlengkapan terbaik untuk {profile.heroName}!";

                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlaySound(SoundType.MergeSuccess);
            }
            else
            {
                if (feedbackMessageText != null)
                    feedbackMessageText.text = $"{profile.heroName} telah mengenakan perlengkapan terkuat yang tersedia!";
            }

            RefreshDisplay();
        }

        public void UnequipAllItems()
        {
            if (CitadelLobbyManager.Instance == null) return;
            var profile = CitadelLobbyManager.Instance.GetHeroProfile(selectedHeroType);
            if (profile == null) return;

            bool anyRemoved = false;
            EquipmentSlot[] slots = { EquipmentSlot.Weapon, EquipmentSlot.Armor, EquipmentSlot.Relic };

            foreach (var slot in slots)
            {
                if (profile.GetEquippedItem(slot) != null)
                {
                    CitadelLobbyManager.Instance.TryUnequipHeroItem(selectedHeroType, slot);
                    anyRemoved = true;
                }
            }

            if (anyRemoved)
            {
                if (feedbackMessageText != null)
                    feedbackMessageText.text = $"Seluruh perlengkapan {profile.heroName} telah disimpan kembali ke tas!";

                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlaySound(SoundType.ButtonClick);
            }

            RefreshDisplay();
        }

        private string GetHeroRoleDescription(UnitType type)
        {
            switch (type)
            {
                case UnitType.Cedric: return "Frontline Melee DPS Striker (Tebasan Ganda & Badai Pedang)";
                case UnitType.RabbitMage: return "Ranged Magic AoE & Kontrol Massa (Meteor Siang & Api Purba)";
                case UnitType.RoseTanker: return "Heavy Frontline Tank & Crowd Control (Hantaman Palu & Stun)";
                case UnitType.WindRanger: return "Long-Range Physical DPS & Crowd Control (Panah Badai & Tornado Zephyr)";
                default: return "Pahlawan Kerajaan";
            }
        }

        private void OnOpenForgeClicked()
        {
            if (UIRelicForgeView.Instance != null && CitadelLobbyManager.Instance != null)
            {
                var profile = CitadelLobbyManager.Instance.GetHeroProfile(selectedHeroType);
                var weapon = profile != null ? profile.GetEquippedItem(EquipmentSlot.Weapon) : null;
                UIRelicForgeView.Instance.OpenForge(weapon, selectedHeroType);
            }
        }

        private void OnUpgradeButtonClicked()
        {
            if (CitadelLobbyManager.Instance == null) return;

            bool success = CitadelLobbyManager.Instance.TryUpgradeHero(selectedHeroType, out string message);
            if (feedbackMessageText != null)
            {
                feedbackMessageText.text = message;
            }

            if (success && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.MergeSuccess);
            }

            RefreshDisplay();
        }

        private void HandleGoldChanged(int newGold)
        {
            RefreshDisplay();
        }

        private void HandleHeroUpgraded(HeroLobbyProfile profile)
        {
            if (profile != null && profile.unitType == selectedHeroType)
            {
                RefreshDisplay();
            }
        }

        private void HandleHeroEquipmentChanged(UnitType type, HeroLobbyProfile profile)
        {
            if (type == selectedHeroType)
            {
                RefreshDisplay();
            }
        }

        private Sprite GetHeroAvatarSprite(UnitType type)
        {
            switch (type)
            {
                case UnitType.Cedric: return cedricAvatar;
                case UnitType.RoseTanker: return roseAvatar;
                case UnitType.RabbitMage: return rabbitAvatar;
                case UnitType.WindRanger: return windRangerAvatar;
                default: return null;
            }
        }
    }
}
