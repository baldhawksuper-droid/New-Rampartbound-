using System;
using System.Collections.Generic;
using UnityEngine;
using FantasyDefense.Core;
using FantasyDefense.Data;
using FantasyDefense.Save;

namespace FantasyDefense.Metagame
{
    /// <summary>
    /// Mengelola state pilar luar pertempuran (Citadel Metagame Lobby):
    /// Level Citadel, Energi Pertempuran (Stamina), Gold Currency,
    /// Tim Pahlawan Aktif, Sistem Upgrade Level & Equipment Hero,
    /// serta integrasi progres Stage World Map dan Team Combat Power (Team CP).
    /// </summary>
    public class CitadelLobbyManager : MonoBehaviour
    {
        public static CitadelLobbyManager Instance { get; private set; }

        [Header("Account Citadel Profile")]
        [SerializeField] private CitadelAccountData accountData = new CitadelAccountData();

        [Header("Battle Energy Configuration")]
        [SerializeField] private int battleEnergyCost = 5;

        public CitadelAccountData AccountData => accountData;
        public int TotalTeamCP => accountData != null ? accountData.CalculateTotalTeamCP() : 0;
        public int CurrentEnergy => accountData.currentEnergy;
        public int GoldCoins => accountData.goldCoins;
        public int AccountGems => accountData.accountGems;
        public int CitadelLevel => accountData.citadelLevel;
        public int CurrentSelectedStageId => accountData.currentSelectedStageId;
        public int ForgeDust => accountData.forgeDust;
        public int RefineStones => accountData.refineStones;
        public int HighestAbyssalFloor => accountData.highestAbyssalFloor;
        public int CitadelCrests => accountData != null ? accountData.citadelCrests : 0;

        // Events untuk pembaruan UI dan sistem eksternal
        public static event Action<int, int> OnEnergyChanged;          // (current, max)
        public static event Action<int> OnGoldChanged;                  // (currentGold)
        public static event Action<int> OnAccountGemsChanged;           // (accountGems)
        public static event Action<int, int> OnForgeMaterialsChanged;   // (forgeDust, refineStones)
        public static event Action<int> OnCitadelCrestsChanged;         // (citadelCrests)
        public static event Action<int> OnAbyssalFloorProgressed;       // (highestFloor)
        public static event Action<int> OnTeamCPUpdated;                // (totalCP)
        public static event Action<int, int> OnCitadelLevelChanged;     // (level, exp)
        public static event Action<HeroLobbyProfile> OnHeroUpgraded;    // (heroProfile)
        public static event Action<HeroLobbyProfile> OnHeroStarPromoted; // (heroProfile)
        public static event Action<UnitType, HeroLobbyProfile> OnHeroEquipmentChanged; // (unitType, heroProfile)
        public static event Action<int, int, bool> OnStageProgressUpdated; // (stageId, stars, isFirstClear)

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadOrInitializeData();
        }

        private void OnEnable()
        {
            GameManager.OnBattleStateChanged += HandleBattleStateChanged;
        }

        private void OnDisable()
        {
            GameManager.OnBattleStateChanged -= HandleBattleStateChanged;
        }

        private void Start()
        {
            BroadcastAllStates();
        }

        private void LoadOrInitializeData()
        {
            // Pastikan SaveManager tersedia
            if (SaveManager.Instance == null)
            {
                GameObject saveObj = new GameObject("SaveManager");
                saveObj.AddComponent<SaveManager>();
            }

            SaveManager.Instance.LoadProfile((loadedData) =>
            {
                if (loadedData != null)
                {
                    // Pemain lama / sesi tersimpan: Muat seluruh progres pemain secara persisten
                    accountData = loadedData;
                    EnsureDefaultRosterIntegrity();
                    Debug.Log($"[CitadelLobbyManager] Profil tersimpan berhasil dimuat! Pemain: {accountData.playerDisplayName} ({accountData.authProvider}), Level: {accountData.citadelLevel}, Gold: {accountData.goldCoins}, Team CP: {TotalTeamCP}");
                }
                else
                {
                    // Pemain baru atau cache/data aplikasi telah dihapus: Mulai baru dari Level 1
                    Debug.Log("[CitadelLobbyManager] Berkas save tidak ditemukan (Game Baru / Cache Terhapus). Membuat profil baru dari Level 1...");
                    InitializeDefaultLobbyData();
                    SaveManager.Instance.SaveProfile(accountData);
                }

                BroadcastAllStates();
            });
        }

        private void EnsureDefaultRosterIntegrity()
        {
            if (accountData.activeRoster == null || accountData.activeRoster.Count == 0)
            {
                InitializeDefaultLobbyData();
            }
            else
            {
                // Hitung ulang seluruh CP hero
                accountData.CalculateTotalTeamCP();
            }
        }

        private void InitializeDefaultLobbyData()
        {
            accountData.activeRoster.Clear();
            accountData.equipmentInventory.Clear();
            accountData.stageRecords.Clear();

            // 1. Cedric (The Red Knight) - Melee DPS (Level 1 Awal)
            var cedric = new HeroLobbyProfile(
                type: UnitType.Cedric,
                name: "Cedric (Ksatria Merah)",
                lvl: 1,
                cp: 0,
                atk: 35f,
                hp: 150f,
                crit: 0.08f,
                star: 1,
                unlocked: true
            );
            cedric.CalculateCombatPower();
            accountData.activeRoster.Add(cedric);

            // 2. Rabbit Mage (Penyihir Kelinci) - Magic AoE (Level 1 Awal)
            var rabbit = new HeroLobbyProfile(
                type: UnitType.RabbitMage,
                name: "Rabbit Mage (Penyihir Kelinci)",
                lvl: 1,
                cp: 0,
                atk: 30f,
                hp: 110f,
                crit: 0.10f,
                star: 1,
                unlocked: true
            );
            rabbit.CalculateCombatPower();
            accountData.activeRoster.Add(rabbit);

            // 3. Rose Tanker (Ksatria Rambut Pink) - Heavy Tank & CC (Level 1 Awal)
            var rose = new HeroLobbyProfile(
                type: UnitType.RoseTanker,
                name: "Rose Tanker (Ksatria Pink)",
                lvl: 1,
                cp: 0,
                atk: 22f,
                hp: 220f,
                crit: 0.04f,
                star: 1,
                unlocked: true
            );
            rose.CalculateCombatPower();
            accountData.activeRoster.Add(rose);

            // 4. Wind Ranger (Pemanah Angin) - Roster Hero ke-4 (Level 1 Awal)
            var wind = new HeroLobbyProfile(
                type: UnitType.WindRanger,
                name: "Wind Ranger (Pemanah Angin)",
                lvl: 1,
                cp: 0,
                atk: 28f,
                hp: 120f,
                crit: 0.12f,
                star: 1,
                unlocked: true
            );
            wind.CalculateCombatPower();
            accountData.activeRoster.Add(wind);

            // Konfigurasi awal akun pemain baru (Guest Mode)
            accountData.citadelLevel = 1;
            accountData.citadelExperience = 0;
            accountData.expToNextLevel = 100;
            accountData.currentEnergy = 30;
            accountData.maxEnergy = 30;
            accountData.goldCoins = 500;
            accountData.accountGems = 50;
            accountData.highestStageCleared = 0;
            accountData.currentSelectedStageId = 101;
            accountData.saveVersion = "1.2.3";
            accountData.isGuestAccount = true;
            accountData.authProvider = "Guest";
            accountData.cloudUserId = "GUEST_" + Guid.NewGuid().ToString().Substring(0, 8);
            accountData.playerDisplayName = "Komandan Citadel";

            // Perlengkapan awal komandan (Starter Gear)
            cedric.EquipItem(new EquipmentItem("EQ_CED_01", "Pedang Ksatria Pemula", EquipmentSlot.Weapon, EquipmentRarity.Common, 1, 10f, 0f));
            cedric.CalculateCombatPower();

            accountData.equipmentInventory.Add(new EquipmentItem("EQ_STA_01", "Pedang Tempa Besi", EquipmentSlot.Weapon, EquipmentRarity.Common, 1, 12f, 0f));
            accountData.equipmentInventory.Add(new EquipmentItem("EQ_STA_02", "Zirah Pelindung Kulit", EquipmentSlot.Armor, EquipmentRarity.Common, 1, 0f, 50f));
            accountData.equipmentInventory.Add(new EquipmentItem("EQ_STA_03", "Cincin Harapan Purba", EquipmentSlot.Relic, EquipmentRarity.Common, 1, 0f, 0f, 0.04f));

            // Default stage awal terbuka (Stage 1-1, 0 bintang)
            accountData.GetStageRecord(101);
            accountData.CalculateTotalTeamCP();
        }

        /// <summary>
        /// Menghubungkan akun tamu (Guest) ke platform Google Play Games atau Apple Game Center.
        /// </summary>
        public void BindAccountToPlatform(string providerName, string platformUserId, string displayName)
        {
            if (accountData == null) return;

            accountData.isGuestAccount = false;
            accountData.authProvider = providerName;
            accountData.cloudUserId = platformUserId;
            if (!string.IsNullOrEmpty(displayName))
            {
                accountData.playerDisplayName = displayName;
            }

            Debug.Log($"[CitadelLobbyManager] Akun berhasil dihubungkan ke {providerName}! ID: {platformUserId}, Nama: {accountData.playerDisplayName}");

            SaveManager.Instance?.SaveProfile(accountData, syncToCloud: true);
            BroadcastAllStates();
        }

        /// <summary>
        /// Beralih kembali ke mode akun Tamu (Guest).
        /// </summary>
        public void SwitchToGuest()
        {
            if (accountData == null) return;

            accountData.isGuestAccount = true;
            accountData.authProvider = "Guest";
            accountData.cloudUserId = "GUEST_" + Guid.NewGuid().ToString().Substring(0, 8);
            accountData.playerDisplayName = "Komandan Citadel (Tamu)";

            SaveManager.Instance?.SaveProfile(accountData, syncToCloud: false);
            BroadcastAllStates();
        }

        public void ResetProfileToFreshStart()
        {
            InitializeDefaultLobbyData();
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.SaveProfile(accountData);
            }
            BroadcastAllStates();
            Debug.Log("[CitadelLobbyManager] Profil akun berhasil di-reset ke status awal pemain baru (Level 1).");
        }

        public void BroadcastAllStates()
        {
            OnEnergyChanged?.Invoke(accountData.currentEnergy, accountData.maxEnergy);
            OnGoldChanged?.Invoke(accountData.goldCoins);
            OnAccountGemsChanged?.Invoke(accountData.accountGems);
            OnForgeMaterialsChanged?.Invoke(accountData.forgeDust, accountData.refineStones);
            OnCitadelCrestsChanged?.Invoke(accountData.citadelCrests);
            OnAbyssalFloorProgressed?.Invoke(accountData.highestAbyssalFloor);
            OnTeamCPUpdated?.Invoke(TotalTeamCP);
            OnCitadelLevelChanged?.Invoke(accountData.citadelLevel, accountData.citadelExperience);
        }

        #region Hero Level Upgrades & Equipment System

        /// <summary>
        /// Meningkatkan level pahlawan aktif menggunakan Gold Citadel.
        /// Meningkatkan ATK, HP, Crit, dan Combat Power (CP) secara permanen.
        /// </summary>
        public bool TryUpgradeHero(UnitType type, out string resultMessage)
        {
            HeroLobbyProfile hero = GetHeroProfile(type);
            if (hero == null)
            {
                resultMessage = "Pahlawan tidak ditemukan dalam roster!";
                return false;
            }

            int upgradeCost = hero.GetUpgradeGoldCost();
            if (accountData.goldCoins < upgradeCost)
            {
                resultMessage = $"Gold tidak mencukupi! Butuh {upgradeCost} Gold (Kamu punya: {accountData.goldCoins}).";
                return false;
            }

            // Potong Gold & Naikkan Level
            accountData.goldCoins -= upgradeCost;
            int prevLevel = hero.level;
            hero.level++;
            int newCP = hero.CalculateCombatPower();

            resultMessage = $"Sukses! {hero.heroName} naik ke Level {hero.level} (CP: {newCP})!";
            Debug.Log($"[CitadelLobbyManager] {resultMessage}");

            // Notifikasi & Simpan
            OnGoldChanged?.Invoke(accountData.goldCoins);
            OnHeroUpgraded?.Invoke(hero);
            OnTeamCPUpdated?.Invoke(TotalTeamCP);
            SaveManager.Instance?.SaveProfile(accountData);

            return true;
        }

        /// <summary>
        /// Mempromosikan bintang pahlawan (Star Rank 1★ -> 5★) menggunakan Shards yang dikumpulkan dari Gacha Hall.
        /// </summary>
        public bool TryPromoteHeroStar(UnitType type, out string resultMessage)
        {
            HeroLobbyProfile hero = GetHeroProfile(type);
            if (hero == null)
            {
                resultMessage = "Pahlawan tidak ditemukan!";
                return false;
            }

            bool success = hero.TryPromoteStar(out resultMessage);
            if (success)
            {
                OnHeroStarPromoted?.Invoke(hero);
                OnTeamCPUpdated?.Invoke(TotalTeamCP);
                SaveManager.Instance?.SaveProfile(accountData);

                if (Audio.AudioManager.Instance != null)
                {
                    Audio.AudioManager.Instance.PlaySound(Audio.SoundType.MergeSuccess);
                }
            }

            return success;
        }

        public void AddHeroShards(UnitType type, int shards)
        {
            HeroLobbyProfile hero = GetHeroProfile(type);
            if (hero != null)
            {
                hero.heroShards += shards;
                SaveManager.Instance?.SaveProfile(accountData);
                Debug.Log($"[CitadelLobbyManager] +{shards} Shards untuk {hero.heroName} (Total: {hero.heroShards})");
            }
        }

        public bool SpendAccountGems(int amount)
        {
            if (accountData.accountGems >= amount)
            {
                accountData.accountGems -= amount;
                OnAccountGemsChanged?.Invoke(accountData.accountGems);
                SaveManager.Instance?.SaveProfile(accountData);
                return true;
            }
            return false;
        }

        public bool SpendGems(int amount) => SpendAccountGems(amount);

        public bool SpendGold(int amount)
        {
            if (accountData.goldCoins >= amount)
            {
                accountData.goldCoins -= amount;
                OnGoldChanged?.Invoke(accountData.goldCoins);
                SaveManager.Instance?.SaveProfile(accountData);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Memasang perlengkapan ke pahlawan tertentu.
        /// </summary>
        public bool TryEquipHeroItem(UnitType type, EquipmentItem item)
        {
            HeroLobbyProfile hero = GetHeroProfile(type);
            if (hero == null || item == null) return false;

            accountData.equipmentInventory.Remove(item);
            EquipmentItem oldItem = hero.EquipItem(item);
            if (oldItem != null)
            {
                accountData.equipmentInventory.Add(oldItem);
            }
            OnHeroEquipmentChanged?.Invoke(type, hero);
            OnTeamCPUpdated?.Invoke(TotalTeamCP);
            SaveManager.Instance?.SaveProfile(accountData);
            return true;
        }

        /// <summary>
        /// Melepas perlengkapan dari pahlawan.
        /// </summary>
        public bool TryUnequipHeroItem(UnitType type, EquipmentSlot slot)
        {
            HeroLobbyProfile hero = GetHeroProfile(type);
            if (hero == null) return false;

            EquipmentItem unequipped = hero.UnequipItem(slot);
            if (unequipped != null)
            {
                accountData.equipmentInventory.Add(unequipped);
                OnHeroEquipmentChanged?.Invoke(type, hero);
                OnTeamCPUpdated?.Invoke(TotalTeamCP);
                SaveManager.Instance?.SaveProfile(accountData);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Mengambil base profile hero dari lobby untuk scaling di arena.
        /// </summary>
        public HeroLobbyProfile GetHeroProfile(UnitType type)
        {
            return accountData.activeRoster.Find(h => h.unitType == type);
        }

        #endregion

        #region Stage Selection & World Map Progression

        /// <summary>
        /// Memilih stage yang akan dimainkan di pertempuran arena.
        /// </summary>
        public void SelectStage(int stageId)
        {
            accountData.currentSelectedStageId = stageId;
            SaveManager.Instance?.SaveProfile(accountData);
            Debug.Log($"[CitadelLobbyManager] Stage aktif dipilih: Stage ID {stageId}");
        }

        /// <summary>
        /// Mencatat kemenangan stage, memberikan reward bintang, gold, exp, dan gems.
        /// </summary>
        public void RecordStageVictory(int stageId, int stars, int goldReward, int expReward, int gemsReward, EquipmentItem bonusGear = null)
        {
            bool isFirstClear = accountData.RecordStageClear(stageId, stars, out bool firstClear);

            AddGold(goldReward);
            AddCitadelExp(expReward);
            AddGems(gemsReward);

            if (bonusGear != null)
            {
                accountData.equipmentInventory.Add(bonusGear);
                Debug.Log($"[CitadelLobbyManager] Hadiah Equipment diperoleh: {bonusGear.itemName} ({bonusGear.rarity})!");
            }

            OnStageProgressUpdated?.Invoke(stageId, stars, firstClear);
            SaveManager.Instance?.SaveProfile(accountData, syncToCloud: true);
            Debug.Log($"[CitadelLobbyManager] Stage {stageId} berhasil diselesaikan! Bintang: {stars}, First Clear: {firstClear}");
        }

        #endregion

        #region Battle Energy & Economy Mechanics

        public bool CanStartBattle()
        {
            return accountData.currentEnergy >= battleEnergyCost;
        }

        public bool ConsumeBattleEnergy()
        {
            if (CanStartBattle())
            {
                accountData.currentEnergy -= battleEnergyCost;
                OnEnergyChanged?.Invoke(accountData.currentEnergy, accountData.maxEnergy);
                SaveManager.Instance?.SaveProfile(accountData);
                return true;
            }
            return false;
        }

        private void HandleBattleStateChanged(BattleState state)
        {
            if (state == BattleState.Victory)
            {
                // Default fallback jika tidak dipicu melalui StageManager
                AddGold(150);
                AddCitadelExp(50);
                SaveManager.Instance?.SaveProfile(accountData, syncToCloud: true);
            }
        }

        public void AddGold(int amount)
        {
            accountData.goldCoins += amount;
            OnGoldChanged?.Invoke(accountData.goldCoins);
            SaveManager.Instance?.SaveProfile(accountData);
        }

        public void AddGems(int amount)
        {
            accountData.accountGems += amount;
            OnAccountGemsChanged?.Invoke(accountData.accountGems);
            SaveManager.Instance?.SaveProfile(accountData);
        }

        public void AddCitadelExp(int amount)
        {
            accountData.citadelExperience += amount;
            if (accountData.citadelExperience >= accountData.expToNextLevel)
            {
                accountData.citadelExperience -= accountData.expToNextLevel;
                accountData.citadelLevel++;
                accountData.expToNextLevel += 150;
            }
            OnCitadelLevelChanged?.Invoke(accountData.citadelLevel, accountData.citadelExperience);
            SaveManager.Instance?.SaveProfile(accountData);
        }

        public void RestoreEnergy(int amount)
        {
            accountData.currentEnergy = Mathf.Min(accountData.maxEnergy, accountData.currentEnergy + amount);
            OnEnergyChanged?.Invoke(accountData.currentEnergy, accountData.maxEnergy);
            SaveManager.Instance?.SaveProfile(accountData);
        }

        #endregion

        #region Relic Forge & Endless Abyssal Mechanics

        public void AddForgeMaterials(int dust, int stones)
        {
            accountData.forgeDust += dust;
            accountData.refineStones += stones;
            OnForgeMaterialsChanged?.Invoke(accountData.forgeDust, accountData.refineStones);
            SaveManager.Instance?.SaveProfile(accountData);
        }

        public bool SpendForgeMaterials(int dust, int stones)
        {
            if (accountData.forgeDust >= dust && accountData.refineStones >= stones)
            {
                accountData.forgeDust -= dust;
                accountData.refineStones -= stones;
                OnForgeMaterialsChanged?.Invoke(accountData.forgeDust, accountData.refineStones);
                SaveManager.Instance?.SaveProfile(accountData);
                return true;
            }
            return false;
        }

        public void RecordAbyssalFloorCleared(int floor)
        {
            if (accountData.RecordAbyssalFloor(floor))
            {
                OnAbyssalFloorProgressed?.Invoke(accountData.highestAbyssalFloor);
                SaveManager.Instance?.SaveProfile(accountData, syncToCloud: true);
                Debug.Log($"[CitadelLobbyManager] Rekor Lantai Abyssal baru tercapai: Lantai {floor}!");
            }
        }

        public bool TryClaimAbyssalMilestone(int milestoneFloor, int goldReward, int gemsReward, int dustReward, int stoneReward)
        {
            if (accountData.highestAbyssalFloor < milestoneFloor) return false;
            if (accountData.IsMilestoneClaimed(milestoneFloor)) return false;

            accountData.ClaimWeeklyMilestone(milestoneFloor);
            AddGold(goldReward);
            AddGems(gemsReward);
            AddForgeMaterials(dustReward, stoneReward);
            SaveManager.Instance?.SaveProfile(accountData);
            return true;
        }

        public void AddCitadelCrests(int amount)
        {
            accountData.AddCitadelCrests(amount);
            OnCitadelCrestsChanged?.Invoke(accountData.citadelCrests);
            SaveManager.Instance?.SaveProfile(accountData);
        }

        public bool SpendCitadelCrests(int amount)
        {
            if (accountData.SpendCitadelCrests(amount))
            {
                OnCitadelCrestsChanged?.Invoke(accountData.citadelCrests);
                SaveManager.Instance?.SaveProfile(accountData);
                return true;
            }
            return false;
        }

        #endregion
    }
}
