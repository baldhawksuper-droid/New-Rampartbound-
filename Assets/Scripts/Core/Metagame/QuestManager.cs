using System;
using System.Collections.Generic;
using UnityEngine;
using FantasyDefense.Audio;
using FantasyDefense.Combat;
using FantasyDefense.Core;
using FantasyDefense.Data;
using FantasyDefense.GridSystem;
using FantasyDefense.Save;
using FantasyDefense.Summon;

namespace FantasyDefense.Metagame
{
    /// <summary>
    /// Master Orchestrator untuk Sistem Misi Harian (Daily Quests) dan Pencapaian Kerajaan (Achievements).
    /// Mengelola tracking progres secara otomatis, reset harian (24 jam / UTC), reward claim,
    /// dan persistensi data akun pemain ke penyimpanan lokal & cloud.
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        [Header("Runtime Quest Data")]
        [SerializeField] private List<QuestItem> activeQuests = new List<QuestItem>();

        public List<QuestItem> ActiveQuests => activeQuests;

        public static event Action<QuestItem> OnQuestProgressUpdated;
        public static event Action<QuestItem> OnQuestCompleted;
        public static event Action<QuestItem> OnQuestClaimed;
        public static event Action OnDailyQuestsReset;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeDefaultQuests();
        }

        private void Start()
        {
            SynchronizeWithSaveData();
            CheckDailyReset();
            CheckTeamCPProgress();
        }

        private void OnEnable()
        {
            HeroSummoner.OnHeroSummoned += HandleHeroSummoned;
            HeroMergeController.OnMergeSuccess += HandleMergeSuccess;
            MonsterController.OnMonsterDied += HandleMonsterDied;
            GameManager.OnBattleStateChanged += HandleBattleStateChanged;
            CitadelLobbyManager.OnHeroUpgraded += HandleHeroUpgraded;
        }

        private void OnDisable()
        {
            HeroSummoner.OnHeroSummoned -= HandleHeroSummoned;
            HeroMergeController.OnMergeSuccess -= HandleMergeSuccess;
            MonsterController.OnMonsterDied -= HandleMonsterDied;
            GameManager.OnBattleStateChanged -= HandleBattleStateChanged;
            CitadelLobbyManager.OnHeroUpgraded -= HandleHeroUpgraded;
        }

        /// <summary>
        /// Inisialisasi daftar master 5 Misi Harian dan 5 Pencapaian Kerajaan.
        /// </summary>
        private void InitializeDefaultQuests()
        {
            activeQuests.Clear();

            // === 5 MISI HARIAN (DAILY QUESTS) TERKALIBRASI D1 ===
            // Total: 3,350 Gold, 165 Gems, 80 Shards (Menjamin minimal 2x Star Promotion di Hari ke-1)
            activeQuests.Add(new QuestItem(
                "daily_summon_5",
                "Panggilan Pasukan",
                "Panggil 5 Hero di arena pertempuran.",
                QuestCategory.Daily,
                QuestObjectiveType.SummonHeroes,
                5, 500, 25, 10, 15
            ));

            activeQuests.Add(new QuestItem(
                "daily_merge_3",
                "Sinergi Pasukan",
                "Lakukan 3x penggabungan (Merge) Hero di arena.",
                QuestCategory.Daily,
                QuestObjectiveType.MergeHeroes,
                3, 600, 30, 10, 15
            ));

            activeQuests.Add(new QuestItem(
                "daily_defeat_25",
                "Garda Terdepan",
                "Kalahkan 25 monster yang menyerbu benteng.",
                QuestCategory.Daily,
                QuestObjectiveType.DefeatMonsters,
                25, 750, 35, 10, 20
            ));

            activeQuests.Add(new QuestItem(
                "daily_upgrade_1",
                "Pelatihan Ksatria",
                "Tingkatkan level pahlawan 1x di Citadel Lobby.",
                QuestCategory.Daily,
                QuestObjectiveType.UpgradeHero,
                1, 500, 25, 10, 10
            ));

            activeQuests.Add(new QuestItem(
                "daily_clear_2",
                "Penjaga Benteng",
                "Menangkan 2 pertempuran stage pertahanan.",
                QuestCategory.Daily,
                QuestObjectiveType.ClearStages,
                2, 1000, 50, 20, 20
            ));

            // === 5 PENCAPAIAN KERAJAAN (ACHIEVEMENTS) ===
            activeQuests.Add(new QuestItem(
                "ach_cp_2000",
                "Kekuatan Pemula",
                "Capai total Team Combat Power sebesar 2,000 CP.",
                QuestCategory.Achievement,
                QuestObjectiveType.ReachTeamCP,
                2000, 500, 50
            ));

            activeQuests.Add(new QuestItem(
                "ach_cp_4500",
                "Kekuatan Elit Citadel",
                "Capai total Team Combat Power sebesar 4,500 CP.",
                QuestCategory.Achievement,
                QuestObjectiveType.ReachTeamCP,
                4500, 1000, 100
            ));

            activeQuests.Add(new QuestItem(
                "ach_tier3_3",
                "Master Fusi Legendaris",
                "Evolusikan hero hingga mencapai Tier 3 sebanyak 3 kali.",
                QuestCategory.Achievement,
                QuestObjectiveType.CreateTier3,
                3, 800, 80
            ));

            activeQuests.Add(new QuestItem(
                "ach_defeat_100",
                "Pembasmi Monster Abyssal",
                "Kalahkan total 100 monster di seluruh arena.",
                QuestCategory.Achievement,
                QuestObjectiveType.DefeatMonsters,
                100, 1200, 120
            ));

            activeQuests.Add(new QuestItem(
                "ach_clear_10",
                "Benteng Tak Tertembus",
                "Selesaikan total 10 kemenangan pertempuran stage.",
                QuestCategory.Achievement,
                QuestObjectiveType.ClearStages,
                10, 1500, 150
            ));
        }

        #region Progress Tracking & Save Synchronization

        public void SynchronizeWithSaveData()
        {
            if (SaveManager.Instance == null || SaveManager.Instance.CurrentData == null) return;

            var data = SaveManager.Instance.CurrentData;
            foreach (var quest in activeQuests)
            {
                var record = data.GetQuestRecord(quest.questId);
                if (record != null)
                {
                    quest.currentAmount = record.currentAmount;
                    quest.isClaimed = record.isClaimed;
                }
            }
        }

        public void CheckDailyReset()
        {
            if (SaveManager.Instance == null || SaveManager.Instance.CurrentData == null) return;

            var data = SaveManager.Instance.CurrentData;
            long nowUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // Jika belum ada catatan atau selisih waktu sudah lebih dari 24 jam (86,400 detik)
            if (data.lastDailyQuestResetUtc == 0 || (nowUtc - data.lastDailyQuestResetUtc) >= 86400)
            {
                data.lastDailyQuestResetUtc = nowUtc;

                foreach (var quest in activeQuests)
                {
                    if (quest.category == QuestCategory.Daily)
                    {
                        quest.currentAmount = 0;
                        quest.isClaimed = false;

                        var rec = data.GetQuestRecord(quest.questId);
                        rec.currentAmount = 0;
                        rec.isClaimed = false;
                    }
                }

                SaveManager.Instance.SaveProfile(data);
                OnDailyQuestsReset?.Invoke();
                Debug.Log("[QuestManager] Misi Harian berhasil di-reset untuk siklus harian baru!");
            }
        }

        public void ProgressQuest(QuestObjectiveType objType, int amount = 1)
        {
            bool anyChanged = false;

            foreach (var quest in activeQuests)
            {
                if (quest.objectiveType == objType && !quest.isClaimed)
                {
                    int prev = quest.currentAmount;
                    quest.currentAmount = Mathf.Min(quest.targetAmount, quest.currentAmount + amount);

                    if (prev != quest.currentAmount)
                    {
                        anyChanged = true;
                        SyncQuestToSave(quest);
                        OnQuestProgressUpdated?.Invoke(quest);

                        if (quest.IsCompleted && prev < quest.targetAmount)
                        {
                            OnQuestCompleted?.Invoke(quest);
                            Debug.Log($"[QuestManager] Misi Selesai: {quest.title}!");
                        }
                    }
                }
            }

            if (anyChanged && SaveManager.Instance != null && SaveManager.Instance.CurrentData != null)
            {
                SaveManager.Instance.SaveProfile(SaveManager.Instance.CurrentData);
            }
        }

        public void SetQuestAbsolute(QuestObjectiveType objType, int absoluteVal)
        {
            bool anyChanged = false;

            foreach (var quest in activeQuests)
            {
                if (quest.objectiveType == objType && !quest.isClaimed)
                {
                    int prev = quest.currentAmount;
                    quest.currentAmount = Mathf.Min(quest.targetAmount, Mathf.Max(quest.currentAmount, absoluteVal));

                    if (prev != quest.currentAmount)
                    {
                        anyChanged = true;
                        SyncQuestToSave(quest);
                        OnQuestProgressUpdated?.Invoke(quest);

                        if (quest.IsCompleted && prev < quest.targetAmount)
                        {
                            OnQuestCompleted?.Invoke(quest);
                        }
                    }
                }
            }

            if (anyChanged && SaveManager.Instance != null && SaveManager.Instance.CurrentData != null)
            {
                SaveManager.Instance.SaveProfile(SaveManager.Instance.CurrentData);
            }
        }

        private void CheckTeamCPProgress()
        {
            if (CitadelLobbyManager.Instance != null)
            {
                SetQuestAbsolute(QuestObjectiveType.ReachTeamCP, CitadelLobbyManager.Instance.TotalTeamCP);
            }
        }

        private void SyncQuestToSave(QuestItem quest)
        {
            if (SaveManager.Instance != null && SaveManager.Instance.CurrentData != null)
            {
                var rec = SaveManager.Instance.CurrentData.GetQuestRecord(quest.questId);
                rec.currentAmount = quest.currentAmount;
                rec.isClaimed = quest.isClaimed;
            }
        }

        #endregion

        #region Reward Claiming

        /// <summary>
        /// Mengklaim hadiah misi: memberikan Gold, Gems, Stamina Energy, memicu SFX, dan menyimpan data.
        /// </summary>
        public bool TryClaimReward(string questId, out string message)
        {
            QuestItem quest = activeQuests.Find(q => q.questId == questId);
            if (quest == null)
            {
                message = "Misi tidak ditemukan!";
                return false;
            }

            if (!quest.IsCompleted)
            {
                message = $"Misi belum selesai ({quest.currentAmount}/{quest.targetAmount})!";
                return false;
            }

            if (quest.isClaimed)
            {
                message = "Hadiah misi sudah pernah diklaim!";
                return false;
            }

            quest.isClaimed = true;
            SyncQuestToSave(quest);

            // Berikan hadiah ke pemain
            if (CitadelLobbyManager.Instance != null)
            {
                if (quest.rewardGold > 0) CitadelLobbyManager.Instance.AddGold(quest.rewardGold);
                if (quest.rewardGems > 0) CitadelLobbyManager.Instance.AddGems(quest.rewardGems);
                if (quest.rewardEnergy > 0) CitadelLobbyManager.Instance.RestoreEnergy(quest.rewardEnergy);
                if (quest.rewardShards > 0) CitadelLobbyManager.Instance.AddHeroShards(UnitType.Cedric, quest.rewardShards);
            }

            // Integrasi Season Pass (Sprint 14): Misi harian memberikan +35 Pass EXP, Pencapaian +80 Pass EXP
            if (BattlePassManager.Instance != null)
            {
                int passExp = quest.category == QuestCategory.Daily ? 35 : 80;
                BattlePassManager.Instance.AddPassExp(passExp);
            }

            // SFX Fanfare Klaim
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.MergeSuccess);
            }

            if (SaveManager.Instance != null && SaveManager.Instance.CurrentData != null)
            {
                SaveManager.Instance.SaveProfile(SaveManager.Instance.CurrentData, syncToCloud: true);
            }

            OnQuestClaimed?.Invoke(quest);
            string shardMsg = quest.rewardShards > 0 ? $", +{quest.rewardShards} Shards" : "";
            string energyMsg = quest.rewardEnergy > 0 ? $", +{quest.rewardEnergy} Energi!" : "!";
            message = $"Berhasil mengklaim: +{quest.rewardGold} Gold, +{quest.rewardGems} Gems{shardMsg}{energyMsg}";
            return true;
        }

        public bool HasUnclaimedRewards()
        {
            return activeQuests != null && activeQuests.Exists(q => q.IsCompleted && !q.isClaimed);
        }

        public List<QuestItem> GetQuestsByCategory(QuestCategory category)
        {
            return activeQuests.FindAll(q => q.category == category);
        }

        public int GetUnclaimedCount(QuestCategory category)
        {
            return activeQuests.FindAll(q => q.category == category && q.IsCompleted && !q.isClaimed).Count;
        }

        public TimeSpan GetTimeUntilDailyReset()
        {
            if (SaveManager.Instance == null || SaveManager.Instance.CurrentData == null)
                return TimeSpan.FromHours(24);

            long lastReset = SaveManager.Instance.CurrentData.lastDailyQuestResetUtc;
            if (lastReset == 0) return TimeSpan.FromHours(24);

            long nextReset = lastReset + 86400;
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long diff = Math.Max(0L, nextReset - now);

            return TimeSpan.FromSeconds(diff);
        }

        #endregion

        #region Event Subscriptions

        private void HandleHeroSummoned(UnitController unit, GridSystem.TileCell cell)
        {
            ProgressQuest(QuestObjectiveType.SummonHeroes, 1);
        }

        private void HandleMergeSuccess(int combo, UnitTier tier)
        {
            ProgressQuest(QuestObjectiveType.MergeHeroes, 1);
            if (tier >= UnitTier.Tier3)
            {
                ProgressQuest(QuestObjectiveType.CreateTier3, 1);
            }
        }

        private void HandleMonsterDied(MonsterController monster)
        {
            ProgressQuest(QuestObjectiveType.DefeatMonsters, 1);
        }

        private void HandleBattleStateChanged(BattleState state)
        {
            if (state == BattleState.Victory)
            {
                ProgressQuest(QuestObjectiveType.ClearStages, 1);
            }
        }

        private void HandleHeroUpgraded(HeroLobbyProfile hero)
        {
            ProgressQuest(QuestObjectiveType.UpgradeHero, 1);
            CheckTeamCPProgress();
        }

        #endregion
    }
}
