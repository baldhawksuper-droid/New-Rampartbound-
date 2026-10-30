using System;
using System.Collections.Generic;
using UnityEngine;
using FantasyDefense.Data;
using FantasyDefense.Save;

namespace FantasyDefense.Metagame
{
    /// <summary>
    /// Master Manager untuk Sistem Peta Dunia & Progres Kampanye Stage Fantasy Citadel Defense.
    /// Mengelola babak Chapter 1 (Stage 1-1 hingga Boss Stage 1-10), sistem bintang 1-3,
    /// First-Clear Rewards, rekomendasi Team CP, dan milestone peti harta bintang.
    /// </summary>
    public class StageManager : MonoBehaviour
    {
        public static StageManager Instance { get; private set; }

        [Header("Chapter 1 Stages Configuration")]
        [SerializeField] private List<StageDataSO> chapterStages = new List<StageDataSO>();

        public List<StageDataSO> ChapterStages => chapterStages;

        public static event Action<StageDataSO> OnStageSelected;
        public static event Action<StageDataSO, int, bool> OnStageCompleted; // (stage, stars, isFirstClear)
        public static event Action<int> OnStarMilestoneClaimed;               // (milestoneStars)

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureDefaultChapterStages();
        }

        private void EnsureDefaultChapterStages()
        {
            if (chapterStages.Count == 0)
            {
                BuildDefaultChapterStages();
            }
        }

        /// <summary>
        /// Inisialisasi prosedural 10 Stage resmi Chapter 1 sesuai Master GDD jika ScriptableObject belum di-assign.
        /// </summary>
        private void BuildDefaultChapterStages()
        {
            string[] titles = {
                "Outpost Meadow (Padang Rumput Depan)",
                "Fortress Siege (Pengepungan Benteng Batu)",
                "Perkemahan Mire Scrapper",
                "Menara Pantau Runtuh",
                "Lembah Obsidian Maw",
                "Persimpangan Terkutuk",
                "Reruntuhan Kuil Kuno",
                "Punggungan Bukit Karang",
                "Gerbang Benteng Kehampaan",
                "Sarang Malakor Void Tyrant [BOSS]"
            };

            int[] recCPs = { 1200, 1500, 1850, 2200, 2600, 3100, 3650, 4200, 4800, 5500 };

            for (int i = 0; i < 10; i++)
            {
                var stage = ScriptableObject.CreateInstance<StageDataSO>();
                stage.chapterNumber = 1;
                stage.stageNumber = i + 1;
                stage.stageId = 100 + (i + 1);
                stage.stageCode = $"Stage 1-{i + 1}";
                stage.stageTitle = titles[i];
                stage.recommendedCP = recCPs[i];
                stage.staminaCost = 5;
                stage.isBossStage = (i == 4 || i == 9);
                if (i == 9)
                {
                    stage.bossName = "Malakor, The Void Tyrant";
                }
                else if (i == 4)
                {
                    stage.bossName = "Abyssal Obsidian Maw (Golek Magma Vulkanik)";
                }
                else
                {
                    stage.bossName = "";
                }

                stage.firstClearGold = 350 + (i * 75);
                stage.firstClearGems = 60 + (i * 15);
                stage.firstClearExp = 120 + (i * 25);
                stage.firstClearShards = i == 9 ? 35 : (i == 4 ? 25 : 15);
                stage.firstClearShardHero = (UnitType)(i % 4);

                if (i == 9)
                {
                    stage.firstClearGear = new EquipmentItem("EQ_LEG_01", "Mahkota Abyssal Purba", EquipmentSlot.Relic, EquipmentRarity.Legendary, 5, 25f, 120f, 0.08f, 0.15f);
                }
                else if (i == 4) // Stage 1-5 Mini-boss checkpoint
                {
                    stage.firstClearGear = new EquipmentItem("EQ_EPC_02", "Pedang Komandan Penjaga", EquipmentSlot.Weapon, EquipmentRarity.Epic, 3, 20f, 0f, 0.05f);
                }
                else if (i == 2) // Stage 1-3: Drop Zirah Pertama Pemain
                {
                    stage.firstClearGear = new EquipmentItem("EQ_RAR_01", "Zirah Pelindung Citadel", EquipmentSlot.Armor, EquipmentRarity.Rare, 1, 0f, 60f, 0f, 0.08f);
                }

                stage.repeatClearGold = 120 + (i * 20);
                stage.repeatClearExp = 40 + (i * 10);
                stage.repeatClearGems = 10 + (i * 2);

                // Konfigurasi Battle Mode spesifik per Stage:
                // Mode 1 (Puzzle Preparation): Stage 1-1, Stage 1-2, Stage 1-5 (Mini-Boss), Stage 1-7
                // Mode 2 (Real-Time Deploy & Merge): Stage 1-3, Stage 1-4, Stage 1-6, Stage 1-8, Stage 1-9, Stage 1-10 (Boss)
                if (i == 0) // Stage 1-1: Tutorial taktis token
                {
                    stage.battleMode = BattleMode.PuzzlePreparation;
                    stage.preparationMoves = 12;
                }
                else if (i == 1) // Stage 1-2: Fusi pahlawan terencana
                {
                    stage.battleMode = BattleMode.PuzzlePreparation;
                    stage.preparationMoves = 14;
                }
                else if (i == 4) // Stage 1-5: Mini-Boss Checkpoint taktis
                {
                    stage.battleMode = BattleMode.PuzzlePreparation;
                    stage.preparationMoves = 18;
                }
                else if (i == 6) // Stage 1-7: Formasi pertahanan reruntuhan kuil
                {
                    stage.battleMode = BattleMode.PuzzlePreparation;
                    stage.preparationMoves = 16;
                }
                else // Stage 1-3, 1-4, 1-6, 1-8, 1-9, 1-10 (Boss)
                {
                    stage.battleMode = BattleMode.RealTimeDeploy;
                    stage.preparationMoves = 15;
                }

                stage.stageWaves = GenerateDefaultWavesForStage(i + 1, stage.isBossStage, stage.bossName);
                chapterStages.Add(stage);
            }

            // Ditangguhkan sesuai instruksi Komandan: Fokus penuh menyempurnakan Chapter 1 terlebih dahulu
            // BuildDefaultChapter2Stages();
        }

        /// <summary>
        /// Inisialisasi prosedural 10 Stage resmi Chapter 2: Bioma Hutan Kristal Abyssal (Stage 2-1 s/d 2-10).
        /// </summary>
        private void BuildDefaultChapter2Stages()
        {
            string[] titles = {
                "Perbatasan Hutan Kristal",
                "Rawa Lumut Bercahaya",
                "Jurang Resonansi Kristal",
                "Gua Kristal Giok",
                "Altar Penjaga Kristal [MINI-BOSS]",
                "Reruntuhan Prismatik",
                "Punggungan Kristal Ungu",
                "Labirin Cahaya Abyssal",
                "Gerbang Takhta Kristal",
                "Sarang Ratu Kristal Abyssal [FINAL BOSS]"
            };

            int[] recCPs = { 6200, 6800, 7500, 8200, 9000, 9800, 10600, 11400, 12200, 13000 };

            for (int i = 0; i < 10; i++)
            {
                var stage = ScriptableObject.CreateInstance<StageDataSO>();
                stage.chapterNumber = 2;
                stage.stageNumber = i + 1;
                stage.stageId = 200 + (i + 1);
                stage.stageCode = $"Stage 2-{i + 1}";
                stage.stageTitle = titles[i];
                stage.recommendedCP = recCPs[i];
                stage.staminaCost = 6;
                stage.isBossStage = (i == 4 || i == 9);
                if (i == 9)
                {
                    stage.bossName = "Crystal Empress Drake (Ratu Naga Kristal Purba)";
                }
                else if (i == 4)
                {
                    stage.bossName = "Crystal Colossus (Raksasa Kristal Purba)";
                }
                else
                {
                    stage.bossName = "";
                }

                stage.firstClearGold = 1200 + (i * 150);
                stage.firstClearGems = 180 + (i * 25);
                stage.firstClearExp = 350 + (i * 50);

                if (i == 9)
                {
                    stage.firstClearGear = new EquipmentItem("EQ_MYTH_01", "Prismatic Heart of Empress", EquipmentSlot.Relic, EquipmentRarity.Legendary, 10, 50f, 250f, 0.12f, 0.25f);
                }
                else if (i == 4)
                {
                    stage.firstClearGear = new EquipmentItem("EQ_LEG_02", "Gada Kristal Resonansi", EquipmentSlot.Weapon, EquipmentRarity.Legendary, 5, 35f, 50f, 0.08f);
                }

                stage.repeatClearGold = 350 + (i * 35);
                stage.repeatClearExp = 120 + (i * 15);
                stage.repeatClearGems = 20 + (i * 3);

                if (i == 0 || i == 2 || i == 4 || i == 7)
                {
                    stage.battleMode = BattleMode.PuzzlePreparation;
                    stage.preparationMoves = 18;
                }
                else
                {
                    stage.battleMode = BattleMode.RealTimeDeploy;
                    stage.preparationMoves = 20;
                }

                stage.stageWaves = GenerateDefaultWavesForStage(i + 11, stage.isBossStage, stage.bossName);
                chapterStages.Add(stage);
            }
        }

        private List<WaveDataSO> GenerateDefaultWavesForStage(int stageNum, bool isBossStage, string bossName)
        {
            var list = new List<WaveDataSO>();
            int waveCount = isBossStage ? (stageNum == 10 ? 5 : 4) : 3;

            for (int w = 1; w <= waveCount; w++)
            {
                var wave = ScriptableObject.CreateInstance<WaveDataSO>();
                wave.waveNumber = w;
                wave.bonusGemsOnClear = 20 + (stageNum * 2);
                wave.restTimeAfterWave = 4.0f;

                bool isFinalWave = (w == waveCount);
                if (isFinalWave && isBossStage)
                {
                    wave.waveTitle = $"Wave {w}: BOSS - {bossName}";

                    var minionData = ScriptableObject.CreateInstance<MonsterDataSO>();
                    minionData.monsterType = MonsterType.Brute;
                    minionData.monsterName = "Pengawal Abyssal";
                    minionData.maxHealth = 480f + (stageNum * 45f);
                    minionData.moveSpeed = 1.15f;
                    minionData.castleDamage = 12;
                    minionData.gemsReward = 8;
                    minionData.monsterSprite = Resources.Load<Sprite>("Sprites/Monsters/Monster_Brute");
                    minionData.visualScale = 0.28f;
                    minionData.themeColor = Color.white;

                    var group1 = new WaveSpawnGroup { monsterData = minionData, count = 4, spawnInterval = 0.9f, initialDelay = 0.2f };

                    var bossData = ScriptableObject.CreateInstance<MonsterDataSO>();
                    bossData.monsterType = MonsterType.Boss;
                    bossData.monsterName = bossName;
                    bossData.maxHealth = stageNum == 10 ? 2800f : (1100f + stageNum * 120f);
                    bossData.moveSpeed = stageNum == 10 ? 0.85f : 1.05f;
                    bossData.castleDamage = stageNum == 10 ? 35 : 22;
                    bossData.gemsReward = stageNum == 10 ? 60 : 30;
                    bossData.visualScale = 0.42f;
                    bossData.themeColor = Color.white;
                    bossData.monsterSprite = Resources.Load<Sprite>("Sprites/Monsters/Monster_Boss");

                    var groupBoss = new WaveSpawnGroup { monsterData = bossData, count = 1, spawnInterval = 1.0f, initialDelay = 2.0f };

                    wave.spawnGroups.Add(group1);
                    wave.spawnGroups.Add(groupBoss);
                }
                else
                {
                    wave.waveTitle = $"Wave {w}: Serbuan Musuh Wilayah {stageNum}";
                    var creepData = ScriptableObject.CreateInstance<MonsterDataSO>();
                    creepData.monsterType = MonsterType.Creep;
                    creepData.monsterName = $"Serangga Abyssal Lv.{stageNum}";
                    creepData.maxHealth = 240f + (stageNum * 35f);
                    creepData.moveSpeed = 1.45f;
                    creepData.castleDamage = 8;
                    creepData.gemsReward = 5;
                    creepData.monsterSprite = Resources.Load<Sprite>("Sprites/Monsters/Monster_Creep");
                    creepData.visualScale = 0.20f;
                    creepData.themeColor = Color.white;

                    // Squad rombongan Creep awal
                    var groupCreeps = new WaveSpawnGroup { monsterData = creepData, count = 6 + (w * 2), spawnInterval = 0.75f, initialDelay = 0.2f };
                    wave.spawnGroups.Add(groupCreeps);

                    // Sesuai Poin 2: Pada penanda tengkorak (Wave 2+ atau akhir gelombang), musuh lebih kuat (Brute) tiba!
                    if (w >= 2)
                    {
                        var bruteData = ScriptableObject.CreateInstance<MonsterDataSO>();
                        bruteData.monsterType = MonsterType.Brute;
                        bruteData.monsterName = $"Panglima Brute Wilayah {stageNum}";
                        bruteData.maxHealth = 620f + (stageNum * 55f);
                        bruteData.moveSpeed = 1.15f;
                        bruteData.castleDamage = 14;
                        bruteData.gemsReward = 12;
                        bruteData.monsterSprite = Resources.Load<Sprite>("Sprites/Monsters/Monster_Brute");
                        bruteData.visualScale = 0.28f;
                        bruteData.themeColor = Color.white;

                        var groupBrute = new WaveSpawnGroup { monsterData = bruteData, count = w, spawnInterval = 1.4f, initialDelay = 2.0f };
                        wave.spawnGroups.Add(groupBrute);
                    }
                }

                list.Add(wave);
            }

            return list;
        }

        /// <summary>
        /// Mengambil data Stage yang saat ini sedang aktif dipilih pemain.
        /// </summary>
        public StageDataSO GetSelectedStage()
        {
            int currentId = CitadelLobbyManager.Instance != null ? CitadelLobbyManager.Instance.CurrentSelectedStageId : 101;
            StageDataSO found = chapterStages.Find(s => s.stageId == currentId);
            return found != null ? found : (chapterStages.Count > 0 ? chapterStages[0] : null);
        }

        public StageDataSO GetStageById(int stageId)
        {
            return chapterStages.Find(s => s.stageId == stageId);
        }

        public List<StageDataSO> GetStagesByChapter(int chapter)
        {
            return chapterStages.FindAll(s => s.chapterNumber == chapter);
        }

        [Header("Development / Review Mode")]
        [Tooltip("Buka seluruh level yang ada di game tanpa syarat progres (untuk review & testing)")]
        [SerializeField] private bool unlockAllStages = true;
        public bool UnlockAllStages => unlockAllStages;

        /// <summary>
        /// Mengecek apakah suatu stage sudah terbuka untuk dimainkan.
        /// Jika unlockAllStages aktif, seluruh stage terbuka langsung.
        /// </summary>
        public bool IsStageUnlocked(int stageId)
        {
            if (unlockAllStages) return true;
            if (stageId <= 101) return true;

            if (CitadelLobbyManager.Instance != null)
            {
                // Jika Stage 2-1 (ID 201), terbuka jika Stage 1-10 telah tuntas (highestStageCleared >= 111)
                if (stageId == 201)
                {
                    return CitadelLobbyManager.Instance.AccountData.highestStageCleared >= 111;
                }
                return CitadelLobbyManager.Instance.AccountData.highestStageCleared >= stageId;
            }
            return false;
        }

        /// <summary>
        /// Memilih stage untuk dimainkan.
        /// </summary>
        public bool TrySelectStage(int stageId)
        {
            if (!IsStageUnlocked(stageId))
            {
                Debug.LogWarning($"[StageManager] Stage {stageId} masih terkunci! Selesaikan stage sebelumnya.");
                return false;
            }

            StageDataSO stage = GetStageById(stageId);
            if (stage != null)
            {
                if (CitadelLobbyManager.Instance != null)
                {
                    CitadelLobbyManager.Instance.SelectStage(stageId);
                }
                
                int teamCp = CitadelLobbyManager.Instance != null ? CitadelLobbyManager.Instance.AccountData.teamCombatPower : 500;
                FantasyDefense.Analytics.CitadelTelemetryManager.Instance?.TrackStageStart(stageId, "StandardCampaign", teamCp);

                OnStageSelected?.Invoke(stage);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Mengevaluasi penyelesaian stage dari hasil pertempuran di arena:
        /// Menghitung perolehan bintang (1-3) berdasarkan sisa HP benteng,
        /// membagikan First-Clear atau Repeat-Clear Rewards, serta membuka stage selanjutnya.
        /// </summary>
        public void CompleteBattleStage(int remainingCastleHp, int maxCastleHp)
        {
            StageDataSO currentStage = GetSelectedStage();
            if (currentStage == null || CitadelLobbyManager.Instance == null) return;

            float hpPercent = (float)remainingCastleHp / Mathf.Max(1, maxCastleHp);

            // Perhitungan bintang resmi Fantasy Citadel Defense
            int stars = 1;
            if (hpPercent >= 0.85f) stars = 3;      // Sisa HP Benteng >= 85%
            else if (hpPercent >= 0.45f) stars = 2; // Sisa HP Benteng >= 45%

            StageRecord record = CitadelLobbyManager.Instance.AccountData.GetStageRecord(currentStage.stageId);
            bool isFirstClear = !record.isCleared;

            int rewardGold = isFirstClear ? currentStage.firstClearGold : currentStage.repeatClearGold;
            int rewardExp = isFirstClear ? currentStage.firstClearExp : currentStage.repeatClearExp;
            int rewardGems = isFirstClear ? currentStage.firstClearGems : currentStage.repeatClearGems;
            EquipmentItem gearReward = isFirstClear ? currentStage.firstClearGear : null;

            // Catat ke profil Citadel & simpan
            CitadelLobbyManager.Instance.RecordStageVictory(
                currentStage.stageId,
                stars,
                rewardGold,
                rewardExp,
                rewardGems,
                gearReward
            );

            // Berikan bonus pecahan Hero Shards jika First-Clear
            if (isFirstClear && currentStage.firstClearShards > 0)
            {
                CitadelLobbyManager.Instance.AddHeroShards(currentStage.firstClearShardHero, currentStage.firstClearShards);
            }

            // Live-Ops KPI Telemetry Tracking
            FantasyDefense.Analytics.CitadelTelemetryManager.Instance?.TrackStageComplete(
                currentStage.stageId,
                stars,
                60f,
                remainingCastleHp,
                maxCastleHp
            );

            OnStageCompleted?.Invoke(currentStage, stars, isFirstClear);
            Debug.Log($"[StageManager] Kemenangan tercatat untuk {currentStage.stageCode} ({currentStage.stageTitle})! Bintang: {stars}, First Clear: {isFirstClear}");
        }

        #region Star Milestones Rewards

        /// <summary>
        /// Total akumulasi bintang yang diperoleh pemain pada Chapter 1.
        /// </summary>
        public int GetTotalChapterStars()
        {
            if (CitadelLobbyManager.Instance == null) return 0;
            return CitadelLobbyManager.Instance.AccountData.GetTotalStars();
        }

        /// <summary>
        /// Klaim hadiah peti harta milestone bintang (10 Bintang, 20 Bintang, 30 Bintang Maksimal).
        /// </summary>
        public bool TryClaimStarMilestone(int milestone, out string rewardMessage)
        {
            int totalStars = GetTotalChapterStars();
            if (totalStars < milestone)
            {
                rewardMessage = $"Bintang belum mencukupi! Butuh {milestone} Bintang (Kamu memiliki: {totalStars}★).";
                return false;
            }

            // Berikan reward milestone
            int gemsBonus = milestone * 10;
            int goldBonus = milestone * 100;
            CitadelLobbyManager.Instance.AddGems(gemsBonus);
            CitadelLobbyManager.Instance.AddGold(goldBonus);

            rewardMessage = $"Selamat! Klaim Peti Bintang {milestone}★ berhasil: +{gemsBonus} Gems & +{goldBonus} Gold!";
            OnStarMilestoneClaimed?.Invoke(milestone);
            return true;
        }

        #endregion
    }
}
