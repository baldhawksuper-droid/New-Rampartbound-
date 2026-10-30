using System;
using System.Collections.Generic;
using UnityEngine;
using FantasyDefense.Data;

namespace FantasyDefense.Metagame
{
    [Serializable]
    public class HeroLobbyProfile
    {
        public UnitType unitType;
        public string heroName;
        public int level = 1;
        public int combatPower;
        public float baseAttack;
        public float baseHealth;
        public float baseCritChance = 0.05f;

        public int starRank = 1; // Tingkat Bintang Hero (1★ hingga 5★)
        public int heroShards = 0; // Keping pecahan pahlawan untuk promosi bintang
        public bool isUnlocked = false; // Status keterbukaan hero dalam akun

        // Slot perlengkapan tempur (Weapon, Armor, Relic)
        public List<EquipmentItem> equippedItems = new List<EquipmentItem>();

        public HeroLobbyProfile() { }

        public HeroLobbyProfile(UnitType type, string name, int lvl, int cp, float atk, float hp, float crit = 0.05f, int star = 1, bool unlocked = false)
        {
            unitType = type;
            heroName = name;
            level = lvl;
            baseAttack = atk;
            baseHealth = hp;
            baseCritChance = crit;
            starRank = Mathf.Clamp(star, 1, 5);
            isUnlocked = unlocked;
            combatPower = cp > 0 ? cp : CalculateCombatPower();
        }

        /// <summary>
        /// Biaya Gold untuk menaikkan hero ke level berikutnya:
        /// Biaya = 120 + (level * 45) Gold.
        /// </summary>
        public int GetUpgradeGoldCost()
        {
            return 120 + (level * 45);
        }

        /// <summary>
        /// Pengali stat berbasis level (Menaikkan +7.5% per level di atas Lv 1).
        /// </summary>
        public float GetLevelMultiplier()
        {
            return 1f + ((level - 1) * 0.075f);
        }

        /// <summary>
        /// Pengali stat permanen berdasarkan Bintang (Star Rank 1★ s.d 5★).
        /// 1★ = 1.0x, 2★ = 1.15x (+15%), 3★ = 1.35x (+35%), 4★ = 1.60x (+60%), 5★ = 1.90x (+90%).
        /// </summary>
        public float GetStarMultiplier()
        {
            switch (starRank)
            {
                case 1: return 1.0f;
                case 2: return 1.15f;
                case 3: return 1.35f;
                case 4: return 1.60f;
                case 5: return 1.90f;
                default: return 1.0f;
            }
        }

        /// <summary>
        /// Jumlah Shards yang dibutuhkan untuk mempromosikan hero ke bintang berikutnya.
        /// </summary>
        public int GetShardsRequiredForPromotion()
        {
            switch (starRank)
            {
                case 1: return 20;  // 1★ -> 2★ (20 Shards)
                case 2: return 50;  // 2★ -> 3★ (50 Shards)
                case 3: return 100; // 3★ -> 4★ (100 Shards)
                case 4: return 200; // 4★ -> 5★ (200 Shards)
                default: return 0;  // 5★ Maksimal
            }
        }

        public bool CanPromoteStar()
        {
            return starRank < 5 && heroShards >= GetShardsRequiredForPromotion();
        }

        public bool TryPromoteStar(out string resultMessage)
        {
            if (starRank >= 5)
            {
                resultMessage = $"{heroName} telah mencapai Bintang Maksimal (★★★★★ 5★)!";
                return false;
            }

            int reqShards = GetShardsRequiredForPromotion();
            if (heroShards < reqShards)
            {
                resultMessage = $"Shards belum mencukupi! Butuh {reqShards} Shards (Kamu memiliki: {heroShards}).";
                return false;
            }

            heroShards -= reqShards;
            starRank++;
            CalculateCombatPower();

            resultMessage = $"Luar biasa! {heroName} berhasil dipromosikan ke {starRank} Bintang (★★★★★)! CP meningkat tajam!";
            return true;
        }

        /// <summary>
        /// Total Attack efektif setelah memperhitungkan level hero, equipment, dan pengali bintang.
        /// </summary>
        public float GetTotalAttack()
        {
            float bonusAtk = 0f;
            foreach (var item in equippedItems)
            {
                if (item != null) bonusAtk += item.EffectiveAttack;
            }
            return ((baseAttack * GetLevelMultiplier()) + bonusAtk) * GetStarMultiplier();
        }

        /// <summary>
        /// Total Health efektif setelah memperhitungkan level hero, equipment, dan pengali bintang.
        /// </summary>
        public float GetTotalHealth()
        {
            float bonusHp = 0f;
            foreach (var item in equippedItems)
            {
                if (item != null) bonusHp += item.EffectiveHealth;
            }
            return ((baseHealth * (1f + ((level - 1) * 0.09f))) + bonusHp) * GetStarMultiplier();
        }

        /// <summary>
        /// Peluang critical strike total (Base + Relic).
        /// </summary>
        public float GetTotalCritChance()
        {
            float bonusCrit = 0f;
            foreach (var item in equippedItems)
            {
                if (item != null) bonusCrit += item.EffectiveCritChance;
            }
            return Mathf.Clamp01(baseCritChance + bonusCrit);
        }

        /// <summary>
        /// Menghitung ulang nilai Combat Power (CP) individual pahlawan secara komprehensif.
        /// </summary>
        public int CalculateCombatPower()
        {
            float totalAtk = GetTotalAttack();
            float totalHp = GetTotalHealth();
            float totalCrit = GetTotalCritChance();

            float calculatedCp = (totalAtk * 25f) + (totalHp * 6.5f) + (totalCrit * 1200f) + (level * 40f) + ((starRank - 1) * 350f);
            combatPower = Mathf.RoundToInt(calculatedCp);
            return combatPower;
        }

        public int CalculateHeroCP() => CalculateCombatPower();

        /// <summary>
        /// Memasang perlengkapan ke pahlawan (menggantikan item lama pada slot yang sama jika ada).
        /// </summary>
        public EquipmentItem EquipItem(EquipmentItem newItem)
        {
            if (newItem == null) return null;

            EquipmentItem oldItem = equippedItems.Find(i => i.slot == newItem.slot);
            if (oldItem != null)
            {
                equippedItems.Remove(oldItem);
            }

            equippedItems.Add(newItem);
            CalculateCombatPower();
            return oldItem;
        }

        /// <summary>
        /// Melepas perlengkapan dari slot tertentu.
        /// </summary>
        public EquipmentItem UnequipItem(EquipmentSlot slot)
        {
            EquipmentItem item = equippedItems.Find(i => i.slot == slot);
            if (item != null)
            {
                equippedItems.Remove(item);
                CalculateCombatPower();
            }
            return item;
        }

        public EquipmentItem GetEquippedItem(EquipmentSlot slot)
        {
            return equippedItems.Find(i => i.slot == slot);
        }
    }

    [Serializable]
    public class StageRecord
    {
        public int stageId;           // Contoh: 101 untuk Stage 1-1, 110 untuk Stage 1-10
        public int starsEarned;       // 0, 1, 2, atau 3 bintang
        public bool isCleared;        // Apakah sudah pernah ditaklukkan
        public int timesCleared;      // Berapa kali di-clear (untuk grinding)
        public int bestScore;

        public StageRecord() { }

        public StageRecord(int id, int stars)
        {
            stageId = id;
            starsEarned = stars;
            isCleared = stars > 0;
            timesCleared = isCleared ? 1 : 0;
            bestScore = stars * 1000;
        }
    }

    [Serializable]
    public class QuestRecord
    {
        public string questId;
        public int currentAmount;
        public bool isClaimed;

        public QuestRecord() { }

        public QuestRecord(string id, int amount, bool claimed)
        {
            questId = id;
            currentAmount = amount;
            isClaimed = claimed;
        }
    }

    [Serializable]
    public class CitadelTalentProgress
    {
        public string talentId;
        public int currentRank;

        public CitadelTalentProgress() { }

        public CitadelTalentProgress(string id, int rank)
        {
            talentId = id;
            currentRank = rank;
        }
    }

    [Serializable]
    public class CitadelAccountData
    {
        public int citadelLevel = 1;
        public int currentEnergy = 30;
        public int maxEnergy = 30;
        public int goldCoins = 500;
        public int citadelExperience = 0;
        public int expToNextLevel = 100;

        public int accountGems = 50;
        public int highestStageCleared = 0; // Stage 1-1 awal sudah terbuka, belum terselesaikan
        public int currentSelectedStageId = 101; // Stage yang saat ini dipilih
        public long lastSaveTimestampUtc = 0;
        public string saveVersion = "1.2.3";
        public string cloudUserId = "GUEST_OFFLINE";
        public bool isGuestAccount = true;
        public string authProvider = "Guest";
        public string playerDisplayName = "Komandan Citadel";
        public string playerName => playerDisplayName;
        public float bgmVolume = 1.0f;
        public float sfxVolume = 1.0f;
        public long lastDailyQuestResetUtc = 0;

        // Endless Abyssal Trial & Forge Crafting Currency
        public int highestAbyssalFloor = 0;
        public long lastWeeklyAbyssalResetUtc = 0;
        public List<int> claimedWeeklyMilestones = new List<int>();

        public int forgeDust = 0;
        public int refineStones = 0;

        // Citadel Talents & Blessings Tree (Sprint 12)
        public int citadelCrests = 0;
        public List<CitadelTalentProgress> talentProgressList = new List<CitadelTalentProgress>();

        // Roster pahlawan aktif & inventaris equipment
        public List<HeroLobbyProfile> activeRoster = new List<HeroLobbyProfile>();
        public List<EquipmentItem> equipmentInventory = new List<EquipmentItem>();

        // Formasi 4 Slot Pahlawan Tempur (Sprint 50)
        // Menyimpan urutan pahlawan yang dipasang di pelataran Lobby dan arena pertempuran
        public List<UnitType> deployedFormation = new List<UnitType>
        {
            UnitType.RabbitMage,
            UnitType.Cedric,
            UnitType.RoseTanker,
            UnitType.WindRanger
        };

        // Citadel Artifacts & Relic Set Synergies (Sprint 13)
        public List<string> equippedArtifactIds = new List<string>();
        public List<string> ownedArtifactIds = new List<string>();

        // Guild Citadel, Battle Pass & Store (Sprint 14)
        public string playerGuildId = "";
        public int playerGuildTokens = 0;
        public int battlePassSeason = 1;
        public int battlePassLevel = 1;
        public int battlePassExp = 0;
        public bool isElitePassUnlocked = false;
        public bool hasPurchasedStarterPack = false;
        public List<string> validatedTransactionIds = new List<string>();

        // Rekor riwayat stage & perolehan bintang
        public List<StageRecord> stageRecords = new List<StageRecord>();

        // Manajemen Artileri Benteng Citadel (Modular Balista & Meriam)
        public int leftTurretTypeIndex = 1;  // 0 = Meriam (Cannon), 1 = Balista (Archer) (Default: Balista kembar)
        public int rightTurretTypeIndex = 1; // 0 = Meriam (Cannon), 1 = Balista (Archer) (Default: Balista kembar)
        public int leftTurretTier = 1;
        public int rightTurretTier = 1;

        // Rekor misi harian & pencapaian
        public List<QuestRecord> questRecords = new List<QuestRecord>();

        public int teamCombatPower => CalculateTotalTeamCP();

        public int CalculateTotalTeamCP()
        {
            int total = 0;
            foreach (var hero in activeRoster)
            {
                if (hero != null)
                {
                    total += hero.CalculateCombatPower();
                }
            }
            return total;
        }

        public StageRecord GetStageRecord(int stageId)
        {
            StageRecord record = stageRecords.Find(r => r.stageId == stageId);
            if (record == null)
            {
                record = new StageRecord(stageId, 0);
                stageRecords.Add(record);
            }
            return record;
        }

        public int GetTotalStars()
        {
            int total = 0;
            foreach (var record in stageRecords)
            {
                if (record != null)
                {
                    total += record.starsEarned;
                }
            }
            return total;
        }

        public bool RecordStageClear(int stageId, int stars, out bool isFirstClear)
        {
            StageRecord record = GetStageRecord(stageId);
            isFirstClear = !record.isCleared;

            record.isCleared = true;
            record.timesCleared++;
            if (stars > record.starsEarned)
            {
                record.starsEarned = stars;
            }

            if (stageId >= highestStageCleared)
            {
                highestStageCleared = stageId + 1; // Membuka stage selanjutnya
            }

            return isFirstClear;
        }

        public QuestRecord GetQuestRecord(string questId)
        {
            var record = questRecords.Find(q => q.questId == questId);
            if (record == null)
            {
                record = new QuestRecord(questId, 0, false);
                questRecords.Add(record);
            }
            return record;
        }

        public bool RecordAbyssalFloor(int floor)
        {
            if (floor > highestAbyssalFloor)
            {
                highestAbyssalFloor = floor;
                return true;
            }
            return false;
        }

        public bool IsMilestoneClaimed(int milestone)
        {
            return claimedWeeklyMilestones != null && claimedWeeklyMilestones.Contains(milestone);
        }

        public bool ClaimWeeklyMilestone(int milestone)
        {
            if (claimedWeeklyMilestones == null) claimedWeeklyMilestones = new List<int>();
            if (!claimedWeeklyMilestones.Contains(milestone))
            {
                claimedWeeklyMilestones.Add(milestone);
                return true;
            }
            return false;
        }

        public void ResetWeeklyAbyssalTrial()
        {
            if (claimedWeeklyMilestones == null) claimedWeeklyMilestones = new List<int>();
            claimedWeeklyMilestones.Clear();
            lastWeeklyAbyssalResetUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        // --- Talent Tree Helpers (Sprint 12) ---
        public int GetTalentRank(string talentId)
        {
            if (talentProgressList == null) talentProgressList = new List<CitadelTalentProgress>();
            var item = talentProgressList.Find(t => t.talentId == talentId);
            return item != null ? item.currentRank : 0;
        }

        public void SetTalentRank(string talentId, int rank)
        {
            if (talentProgressList == null) talentProgressList = new List<CitadelTalentProgress>();
            var item = talentProgressList.Find(t => t.talentId == talentId);
            if (item != null)
            {
                item.currentRank = rank;
            }
            else
            {
                talentProgressList.Add(new CitadelTalentProgress(talentId, rank));
            }
        }

        public void AddCitadelCrests(int amount)
        {
            if (amount <= 0) return;
            citadelCrests += amount;
        }

        public bool SpendCitadelCrests(int amount)
        {
            if (amount <= 0) return true;
            if (citadelCrests >= amount)
            {
                citadelCrests -= amount;
                return true;
            }
            return false;
        }

        // --- Artifact Set Helpers (Sprint 13) ---
        public bool HasArtifact(string artifactId)
        {
            return ownedArtifactIds != null && ownedArtifactIds.Contains(artifactId);
        }

        public void AddArtifact(string artifactId)
        {
            if (ownedArtifactIds == null) ownedArtifactIds = new List<string>();
            if (!ownedArtifactIds.Contains(artifactId))
            {
                ownedArtifactIds.Add(artifactId);
            }
        }

        public bool IsArtifactEquipped(string artifactId)
        {
            return equippedArtifactIds != null && equippedArtifactIds.Contains(artifactId);
        }

        public void EquipArtifact(string artifactId)
        {
            if (equippedArtifactIds == null) equippedArtifactIds = new List<string>();
            if (!equippedArtifactIds.Contains(artifactId))
            {
                equippedArtifactIds.Add(artifactId);
            }
        }

        public void UnequipArtifact(string artifactId)
        {
            if (equippedArtifactIds != null)
            {
                equippedArtifactIds.Remove(artifactId);
            }
        }
    }
}
