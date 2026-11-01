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
                stage.isBossStage = (i == 3 || i == 4 || i == 7 || i == 9);
                if (i == 9)
                {
                    stage.bossName = "Malakor, The Void Tyrant";
                }
                else if (i == 7)
                {
                    stage.bossName = "Glacial Frost Wyrm";
                }
                else if (i == 4)
                {
                    stage.bossName = "Abyssal Obsidian Maw";
                }
                else if (i == 3)
                {
                    stage.bossName = "Ancient Ironwood Golem";
                }
                else
                {
                    stage.bossName = "";
                }

                stage.firstClearGold = 350 + (i * 75);
                stage.firstClearGems = 60 + (i * 15);
                stage.firstClearExp = 120 + (i * 25);

                stage.repeatClearGold = 120 + (i * 20);
                stage.repeatClearExp = 40 + (i * 10);
                stage.repeatClearGems = 10 + (i * 2);

                if (i == 9) // Stage 1-10 Final Boss Titan Warlord
                {
                    stage.firstClearShards = 35;
                    stage.firstClearShardHero = UnitType.Cedric;
                    stage.firstClearGear = new EquipmentItem("EQ_LEG_01", "Mahkota Abyssal Purba", EquipmentSlot.Relic, EquipmentRarity.Legendary, 5, 25f, 120f, 0.08f, 0.15f);
                    stage.repeatClearShards = 4;
                    stage.repeatClearShardHero = UnitType.Cedric;
                    stage.repeatClearGems = 35;
                }
                else if (i == 7) // Stage 1-8 Mini-Boss Glacial Frost Wyrm
                {
                    stage.firstClearShards = 30;
                    stage.firstClearShardHero = UnitType.Rabbit;
                    stage.firstClearGear = new EquipmentItem("EQ_EPC_03", "Taring Kristal Frost Wyrm", EquipmentSlot.Relic, EquipmentRarity.Epic, 4, 18f, 80f, 0.08f, 0.10f);
                    stage.repeatClearShards = 3;
                    stage.repeatClearShardHero = UnitType.Rabbit;
                    stage.repeatClearGems = 25;
                }
                else if (i == 4) // Stage 1-5 Mini-boss checkpoint
                {
                    stage.firstClearShards = 25;
                    stage.firstClearShardHero = UnitType.Wind;
                    stage.firstClearGear = new EquipmentItem("EQ_EPC_02", "Pedang Komandan Penjaga", EquipmentSlot.Weapon, EquipmentRarity.Epic, 3, 20f, 0f, 0.05f);
                    stage.repeatClearShards = 2;
                    stage.repeatClearShardHero = UnitType.Wind;
                    stage.repeatClearGems = 20;
                }
                else if (i == 3) // Stage 1-4 Mini-Boss Ancient Ironwood Golem
                {
                    stage.firstClearShards = 25;
                    stage.firstClearShardHero = UnitType.Rose;
                    stage.firstClearGear = new EquipmentItem("EQ_RAR_02", "Amulet Getah Kayu Purba", EquipmentSlot.Relic, EquipmentRarity.Rare, 2, 8f, 50f, 0.04f, 0.06f);
                    stage.repeatClearShards = 2;
                    stage.repeatClearShardHero = UnitType.Rose;
                    stage.repeatClearGems = 18;
                }
                else if (i == 2) // Stage 1-3: Drop Zirah Pertama Pemain
                {
                    stage.firstClearShards = 15;
                    stage.firstClearShardHero = UnitType.Cedric;
                    stage.firstClearGear = new EquipmentItem("EQ_RAR_01", "Zirah Pelindung Citadel", EquipmentSlot.Armor, EquipmentRarity.Rare, 1, 0f, 60f, 0f, 0.08f);
                }
                else
                {
                    stage.firstClearShards = 15;
                    stage.firstClearShardHero = (UnitType)(i % 4);
                }

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

                // Tema Visual Lingkungan Pertempuran Berbasis Bioma:
                // Stage 1-4: Forest (Padang Rumput & Gerbang Luar)
                // Stage 5-8: Mountain (Punggung Bukit Karang & Batuan Granit)
                // Stage 9-10: Volcanic (Dataran Magma Vulkanik & Sarang Boss)
                if (i <= 3)
                {
                    stage.environmentTheme = Combat.BattlefieldEnvironmentTheme.Forest;
                }
                else if (i <= 7)
                {
                    stage.environmentTheme = Combat.BattlefieldEnvironmentTheme.Mountain;
                }
                else
                {
                    stage.environmentTheme = Combat.BattlefieldEnvironmentTheme.Volcanic;
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

        private MonsterDataSO CreateMonsterData(
            MonsterType type,
            string name,
            float maxHealth,
            float speed,
            int castleDamage,
            int gemsReward,
            Color themeColor,
            float visualScale)
        {
            var data = ScriptableObject.CreateInstance<MonsterDataSO>();
            data.monsterType = type;
            data.monsterName = name;
            data.maxHealth = maxHealth;
            data.moveSpeed = speed;
            data.castleDamage = castleDamage;
            data.gemsReward = gemsReward;
            data.themeColor = themeColor;
            data.visualScale = visualScale;

            string spriteName = type switch
            {
                MonsterType.Boss => "Monster_Boss",
                MonsterType.Brute => "Monster_Brute",
                _ => "Monster_Creep"
            };

            data.monsterSprite = Resources.Load<Sprite>($"Sprites/Monsters/{spriteName}");
            data.monsterWalkSprite = Resources.Load<Sprite>($"Sprites/Monsters/{spriteName}_Walk");
            data.monsterAttackSprite = Resources.Load<Sprite>($"Sprites/Monsters/{spriteName}_Attack");
            return data;
        }

        private List<WaveDataSO> GenerateDefaultWavesForStage(int stageNum, bool isBossStage, string bossName)
        {
            var list = new List<WaveDataSO>();
            int waveCount = isBossStage ? (stageNum == 10 ? 5 : 4) : 3;

            for (int w = 1; w <= waveCount; w++)
            {
                var wave = ScriptableObject.CreateInstance<WaveDataSO>();
                wave.waveNumber = w;
                wave.bonusGemsOnClear = 25 + (stageNum * 3);
                wave.restTimeAfterWave = 3.5f;
                bool isFinalWave = (w == waveCount);

                // Konfigurasi variasi monster spesifik dan unik per Stage (Anti-Membosankan)
                switch (stageNum)
                {
                    case 1: // Stage 1-1: Outpost Meadow (Padang Rumput Depan) - Mode 1 Tutorial
                        {
                            var oozeColor = new Color(0.12f, 0.78f, 0.48f); // Hijau Zamrud Slime
                            wave.waveTitle = w switch {
                                1 => "Wave 1: Tetesan Awal Gemetite Ooze",
                                2 => "Wave 2: Serbuan Koloni Slime Meadow",
                                _ => "Wave 3: Kawanan Gemetite & Pemimpin Slime"
                            };

                            var ooze = CreateMonsterData(MonsterType.Creep, "Gemetite Scout Ooze", 150f, 1.35f, 6, 4, oozeColor, 0.19f);
                            wave.spawnGroups.Add(new WaveSpawnGroup { monsterData = ooze, count = 4 + (w * 2), spawnInterval = 0.85f, initialDelay = 0.2f });

                            if (isFinalWave)
                            {
                                var leader = CreateMonsterData(MonsterType.Brute, "Gemetite Swarm Leader", 360f, 1.15f, 10, 8, new Color(0.06f, 0.65f, 0.38f), 0.26f);
                                wave.spawnGroups.Add(new WaveSpawnGroup { monsterData = leader, count = 1, spawnInterval = 1.0f, initialDelay = 2.0f });
                            }
                        }
                        break;

                    case 2: // Stage 1-2: Fortress Siege (Pengepungan Benteng Batu) - Mode 1 Fusi T2
                        {
                            var crystalOozeColor = new Color(0.08f, 0.65f, 0.42f);
                            wave.waveTitle = w switch {
                                1 => "Wave 1: Perayap Kristal Gemetite",
                                2 => "Wave 2: Serbuan Ooze Berzirah Kristal",
                                _ => "Wave 3: Pasukan Hardened Gemetite Cluster"
                            };

                            var crystalOoze = CreateMonsterData(MonsterType.Creep, "Gemetite Crystal Ooze", 210f, 1.30f, 8, 5, crystalOozeColor, 0.20f);
                            wave.spawnGroups.Add(new WaveSpawnGroup { monsterData = crystalOoze, count = 5 + (w * 2), spawnInterval = 0.80f, initialDelay = 0.2f });

                            if (w >= 2)
                            {
                                var cluster = CreateMonsterData(MonsterType.Brute, "Hardened Gemetite Cluster", 480f, 0.95f, 12, 10, new Color(0.04f, 0.52f, 0.32f), 0.27f);
                                wave.spawnGroups.Add(new WaveSpawnGroup { monsterData = cluster, count = w - 1, spawnInterval = 1.3f, initialDelay = 1.8f });
                            }
                        }
                        break;

                    case 3: // Stage 1-3: Perkemahan Mire Scrapper - Mode 2 Pengenalan Deploy Cepat!
                        {
                            var mireColor = new Color(0.55f, 0.78f, 0.18f); // Hijau Lumut / Olive
                            wave.waveTitle = w switch {
                                1 => "Wave 1: Pelari Cepat Mire Scrapper",
                                2 => "Wave 2: Barisan Pengintai Bertameng Kayu",
                                _ => "Wave 3: Penggempur Mire Chieftain"
                            };

                            var runner = CreateMonsterData(MonsterType.Creep, "Mire Runner Scrapper", 230f, 1.55f, 8, 5, mireColor, 0.21f);
                            wave.spawnGroups.Add(new WaveSpawnGroup { monsterData = runner, count = 6 + (w * 2), spawnInterval = 0.75f, initialDelay = 0.2f });

                            if (w >= 2)
                            {
                                var chief = CreateMonsterData(MonsterType.Brute, "Mire Chieftain", 680f, 1.10f, 14, 12, new Color(0.42f, 0.62f, 0.12f), 0.28f);
                                wave.spawnGroups.Add(new WaveSpawnGroup { monsterData = chief, count = w - 1, spawnInterval = 1.2f, initialDelay = 2.0f });
                            }
                        }
                        break;

                    case 4: // Stage 1-4: Menara Pantau Runtuh [MINI-BOSS: Ancient Ironwood Golem]
                        {
                            var forestColor = new Color(0.28f, 0.65f, 0.22f); // Hijau Hutan Purba
                            if (isFinalWave)
                            {
                                wave.waveTitle = $"Wave 4: MINI-BOSS - {bossName}";
                                var flailer = CreateMonsterData(MonsterType.Brute, "Mire Anchor Flailer", 780f, 1.05f, 16, 14, new Color(0.30f, 0.48f, 0.08f), 0.29f);
                                wave.spawnGroups.Add(new WaveSpawnGroup { monsterData = flailer, count = 2, spawnInterval = 1.0f, initialDelay = 0.2f });

                                var miniBoss = CreateMonsterData(MonsterType.Boss, bossName, 1850f, 0.85f, 22, 30, forestColor, 0.45f);
                                wave.spawnGroups.Add(new WaveSpawnGroup { monsterData = miniBoss, count = 1, spawnInterval = 1.0f, initialDelay = 2.0f });
                            }
                            else
                            {
                                wave.waveTitle = w switch {
                                    1 => "Wave 1: Sergapan Kilat Penyusup Rawa",
                                    2 => "Wave 2: Regu Penghancur Berpelindung",
                                    _ => "Wave 3: Barisan Pelindung Golem Hutan"
                                };

                                var horned = CreateMonsterData(MonsterType.Creep, "Mire Horned Scrapper", 270f, 1.45f, 9, 6, forestColor, 0.21f);
                                wave.spawnGroups.Add(new WaveSpawnGroup { monsterData = horned, count = 7 + (w * 2), spawnInterval = 0.75f, initialDelay = 0.2f });

                                if (w >= 2)
                                {
                                    var flailer = CreateMonsterData(MonsterType.Brute, "Mire Anchor Flailer", 780f, 1.05f, 16, 14, new Color(0.30f, 0.48f, 0.08f), 0.29f);
                                    wave.spawnGroups.Add(new WaveSpawnGroup { monsterData = flailer, count = w, spawnInterval = 1.3f, initialDelay = 1.8f });
                                }
                            }
                        }
                        break;

                    case 5: // Stage 1-5: Lembah Obsidian Maw [MINI-BOSS CHECKPOINT] - 4 Wave Magma!
                        {
                            var lavaColor = new Color(0.98f, 0.45f, 0.12f); // Oranye Magma Panas
                            if (isFinalWave)
                            {
                                wave.waveTitle = $"Wave 4: MINI-BOSS - {bossName}";
                                var brawler = CreateMonsterData(MonsterType.Brute, "Obsidian Maw Brawler", 880f, 1.00f, 16, 14, lavaColor, 0.29f);
                                wave.spawnGroups.Add(new WaveSpawnGroup { monsterData = brawler, count = 3, spawnInterval = 1.0f, initialDelay = 0.2f });

                                var miniBoss = CreateMonsterData(MonsterType.Boss, bossName, 2200f, 0.90f, 26, 35, new Color(0.92f, 0.35f, 0.08f), 0.46f);
                                wave.spawnGroups.Add(new WaveSpawnGroup { monsterData = miniBoss, count = 1, spawnInterval = 1.0f, initialDelay = 2.2f });
                            }
                            else
                            {
                                wave.waveTitle = w switch {
                                    1 => "Wave 1: Bara Pasukan Obsidian Lava Imp",
                                    2 => "Wave 2: Semburan Lahar Lembah Magma",
                                    _ => "Wave 3: Garda Pertahanan Brawler Obsidian"
                                };

                                var imp = CreateMonsterData(MonsterType.Creep, "Obsidian Lava Imp", 290f, 1.60f, 10, 6, lavaColor, 0.21f);
                                wave.spawnGroups.Add(new WaveSpawnGroup { monsterData = imp, count = 8 + (w * 2), spawnInterval = 0.70f, initialDelay = 0.2f });

                                if (w >= 2)
                                {
                                    var brawler = CreateMonsterData(MonsterType.Brute, "Obsidian Maw Brawler", 820f, 1.00f, 15, 12, new Color(0.85f, 0.38f, 0.10f), 0.28f);
                                    wave.spawnGroups.Add(new WaveSpawnGroup { monsterData = brawler, count = w - 1, spawnInterval = 1.4f, initialDelay = 2.0f });
                                }
                            }
                        }
                        break;

                    case 6: // Stage 1-6: Persimpangan Terkutuk - Mode 2 Bayangan Cepat Astral
                        {
                            var shadowColor = new Color(0.62f, 0.42f, 0.95f); // Ungu Violet Astral
                            wave.waveTitle = w switch {
                                1 => "Wave 1: Sergapan Kilat Shadow Wisp Scout",
                                2 => "Wave 2: Badai Bayangan Abyssal Wisp",
                                _ => "Wave 3: Gelombang Teror Greater Shadow Strider"
                            };

                            var wisp = CreateMonsterData(MonsterType.Creep, "Shadow Wisp Scout", 280f, 1.80f, 9, 6, shadowColor, 0.21f);
                            wave.spawnGroups.Add(new WaveSpawnGroup { monsterData = wisp, count = 9 + (w * 2), spawnInterval = 0.65f, initialDelay = 0.2f });

                            if (w >= 2)
                            {
                                var strider = CreateMonsterData(MonsterType.Brute, "Greater Shadow Strider", 920f, 1.15f, 16, 14, new Color(0.48f, 0.28f, 0.82f), 0.29f);
                                wave.spawnGroups.Add(new WaveSpawnGroup { monsterData = strider, count = w, spawnInterval = 1.2f, initialDelay = 1.8f });
                            }
                        }
                        break;

                    case 7: // Stage 1-7: Reruntuhan Kuil Kuno - Mode 1 Puzzle Kuil Berbatu
                        {
                            var templeColor = new Color(0.22f, 0.72f, 0.92f); // Biru Safir Kuno
                            wave.waveTitle = w switch {
                                1 => "Wave 1: Penjaga Sprites Kuil Kuno",
                                2 => "Wave 2: Rombongan Pengawal Kuil Runtuh",
                                _ => "Wave 3: Golem Batu Rune Golem Guardian"
                            };

                            var sprite = CreateMonsterData(MonsterType.Creep, "Ancient Temple Sprite", 320f, 1.45f, 10, 7, templeColor, 0.22f);
                            wave.spawnGroups.Add(new WaveSpawnGroup { monsterData = sprite, count = 8 + (w * 2), spawnInterval = 0.75f, initialDelay = 0.2f });

                            if (w >= 2)
                            {
                                var golem = CreateMonsterData(MonsterType.Brute, "Rune Golem Guardian", 1020f, 0.95f, 18, 15, new Color(0.15f, 0.55f, 0.78f), 0.30f);
                                wave.spawnGroups.Add(new WaveSpawnGroup { monsterData = golem, count = w, spawnInterval = 1.3f, initialDelay = 2.0f });
                            }
                        }
                        break;

                    case 8: // Stage 1-8: Punggungan Bukit Karang [MINI-BOSS: Glacial Frost Wyrm]
                        {
                            var frostColor = new Color(0.45f, 0.82f, 0.98f); // Biru Es Gletser
                            if (isFinalWave)
                            {
                                wave.waveTitle = $"Wave 4: MINI-BOSS - {bossName}";
                                var brute = CreateMonsterData(MonsterType.Brute, "Cragridge Brute", 1120f, 1.05f, 18, 15, new Color(0.70f, 0.40f, 0.08f), 0.30f);
                                wave.spawnGroups.Add(new WaveSpawnGroup { monsterData = brute, count = 3, spawnInterval = 1.0f, initialDelay = 0.2f });

                                var miniBoss = CreateMonsterData(MonsterType.Boss, bossName, 2600f, 0.95f, 32, 45, frostColor, 0.48f);
                                wave.spawnGroups.Add(new WaveSpawnGroup { monsterData = miniBoss, count = 1, spawnInterval = 1.0f, initialDelay = 2.2f });
                            }
                            else
                            {
                                wave.waveTitle = w switch {
                                    1 => "Wave 1: Perayap Karang Crag Skitterer",
                                    2 => "Wave 2: Serbuan Serempak Karang Terjal",
                                    _ => "Wave 3: Benteng Hidup Cragridge Brute"
                                };

                                var skitterer = CreateMonsterData(MonsterType.Creep, "Crag Skitterer", 350f, 1.60f, 11, 7, frostColor, 0.22f);
                                wave.spawnGroups.Add(new WaveSpawnGroup { monsterData = skitterer, count = 9 + (w * 2), spawnInterval = 0.70f, initialDelay = 0.2f });

                                if (w >= 2)
                                {
                                    var brute = CreateMonsterData(MonsterType.Brute, "Cragridge Brute", 1120f, 1.05f, 18, 15, new Color(0.70f, 0.40f, 0.08f), 0.30f);
                                    wave.spawnGroups.Add(new WaveSpawnGroup { monsterData = brute, count = w, spawnInterval = 1.2f, initialDelay = 1.8f });
                                }
                            }
                        }
                        break;

                    case 9: // Stage 1-9: Gerbang Benteng Kehampaan - Mode 2 Garda Elit Sebelum Bos
                        {
                            var voidColor = new Color(0.35f, 0.32f, 0.88f); // Indigo Kehampaan
                            wave.waveTitle = w switch {
                                1 => "Wave 1: Pasukan Penyusup Void Infiltrator",
                                2 => "Wave 2: Barisan Pelindung Gerbang Kehampaan",
                                _ => "Wave 3: Pengawal Setia Abyssal Void Guard"
                            };

                            var infiltrator = CreateMonsterData(MonsterType.Creep, "Void Gate Infiltrator", 380f, 1.55f, 12, 8, voidColor, 0.23f);
                            wave.spawnGroups.Add(new WaveSpawnGroup { monsterData = infiltrator, count = 10 + (w * 2), spawnInterval = 0.70f, initialDelay = 0.2f });

                            if (w >= 2)
                            {
                                var guard = CreateMonsterData(MonsterType.Brute, "Abyssal Void Guard", 1250f, 1.10f, 20, 16, new Color(0.25f, 0.22f, 0.75f), 0.31f);
                                wave.spawnGroups.Add(new WaveSpawnGroup { monsterData = guard, count = w, spawnInterval = 1.2f, initialDelay = 1.8f });
                            }
                        }
                        break;

                    case 10: // Stage 1-10: Sarang Malakor [FINAL BOSS CHAPTER 1] - 5 Gelombang Puncak!
                        {
                            var bossTheme = new Color(0.32f, 0.22f, 0.72f);
                            if (isFinalWave)
                            {
                                wave.waveTitle = $"Wave 5: FINAL CLIMAX - {bossName}";
                                var eliteGuard = CreateMonsterData(MonsterType.Brute, "Abyssal Void Guard", 1380f, 1.05f, 20, 18, new Color(0.25f, 0.22f, 0.75f), 0.32f);
                                wave.spawnGroups.Add(new WaveSpawnGroup { monsterData = eliteGuard, count = 4, spawnInterval = 0.9f, initialDelay = 0.2f });

                                var finalBoss = CreateMonsterData(MonsterType.Boss, bossName, 3200f, 0.78f, 40, 80, bossTheme, 0.52f);
                                wave.spawnGroups.Add(new WaveSpawnGroup { monsterData = finalBoss, count = 1, spawnInterval = 1.0f, initialDelay = 2.5f });
                            }
                            else
                            {
                                wave.waveTitle = w switch {
                                    1 => "Wave 1: Garda Depan Gabungan Slime & Goblin",
                                    2 => "Wave 2: Badai Bayangan & Api Kehampaan",
                                    3 => "Wave 3: Pasukan Inti Void Infiltrator",
                                    _ => "Wave 4: Benteng Besi Obsidian Juggernaut"
                                };

                                if (w == 1)
                                {
                                    var ooze = CreateMonsterData(MonsterType.Creep, "Gemetite Crystal Ooze", 340f, 1.45f, 10, 6, new Color(0.08f, 0.65f, 0.42f), 0.22f);
                                    wave.spawnGroups.Add(new WaveSpawnGroup { monsterData = ooze, count = 8, spawnInterval = 0.75f, initialDelay = 0.2f });
                                    var horned = CreateMonsterData(MonsterType.Creep, "Mire Horned Scrapper", 360f, 1.40f, 11, 7, new Color(0.55f, 0.78f, 0.18f), 0.22f);
                                    wave.spawnGroups.Add(new WaveSpawnGroup { monsterData = horned, count = 4, spawnInterval = 0.85f, initialDelay = 1.5f });
                                }
                                else if (w == 2)
                                {
                                    var wisp = CreateMonsterData(MonsterType.Creep, "Shadow Wisp Scout", 360f, 1.70f, 11, 7, new Color(0.62f, 0.42f, 0.95f), 0.22f);
                                    wave.spawnGroups.Add(new WaveSpawnGroup { monsterData = wisp, count = 10, spawnInterval = 0.65f, initialDelay = 0.2f });
                                    var imp = CreateMonsterData(MonsterType.Creep, "Obsidian Lava Imp", 380f, 1.60f, 12, 7, new Color(0.98f, 0.45f, 0.12f), 0.22f);
                                    wave.spawnGroups.Add(new WaveSpawnGroup { monsterData = imp, count = 4, spawnInterval = 0.75f, initialDelay = 1.6f });
                                }
                                else if (w == 3)
                                {
                                    var inf = CreateMonsterData(MonsterType.Creep, "Void Gate Infiltrator", 410f, 1.55f, 13, 8, new Color(0.35f, 0.32f, 0.88f), 0.23f);
                                    wave.spawnGroups.Add(new WaveSpawnGroup { monsterData = inf, count = 12, spawnInterval = 0.68f, initialDelay = 0.2f });
                                    var guard = CreateMonsterData(MonsterType.Brute, "Abyssal Void Guard", 1250f, 1.10f, 18, 16, new Color(0.25f, 0.22f, 0.75f), 0.31f);
                                    wave.spawnGroups.Add(new WaveSpawnGroup { monsterData = guard, count = 3, spawnInterval = 1.2f, initialDelay = 2.0f });
                                }
                                else // w == 4
                                {
                                    var inf = CreateMonsterData(MonsterType.Creep, "Void Gate Infiltrator", 440f, 1.50f, 14, 8, new Color(0.35f, 0.32f, 0.88f), 0.23f);
                                    wave.spawnGroups.Add(new WaveSpawnGroup { monsterData = inf, count = 12, spawnInterval = 0.65f, initialDelay = 0.2f });
                                    var juggernaut = CreateMonsterData(MonsterType.Brute, "Obsidian Juggernaut", 1450f, 1.00f, 22, 20, new Color(0.85f, 0.32f, 0.08f), 0.33f);
                                    wave.spawnGroups.Add(new WaveSpawnGroup { monsterData = juggernaut, count = 4, spawnInterval = 1.3f, initialDelay = 2.0f });
                                }
                            }
                        }
                        break;

                    default: // Fallback prosedur jika ada stage tambahan
                        {
                            var defColor = Color.white;
                            wave.waveTitle = $"Wave {w}: Serbuan Wilayah {stageNum}";
                            var creep = CreateMonsterData(MonsterType.Creep, $"Penyusup Wilayah {stageNum}", 240f + (stageNum * 35f), 1.45f, 8, 5, defColor, 0.20f);
                            wave.spawnGroups.Add(new WaveSpawnGroup { monsterData = creep, count = 6 + (w * 2), spawnInterval = 0.75f, initialDelay = 0.2f });

                            if (w >= 2)
                            {
                                var brute = CreateMonsterData(MonsterType.Brute, $"Pengawal Wilayah {stageNum}", 620f + (stageNum * 55f), 1.15f, 14, 12, defColor, 0.28f);
                                wave.spawnGroups.Add(new WaveSpawnGroup { monsterData = brute, count = w, spawnInterval = 1.4f, initialDelay = 2.0f });
                            }
                        }
                        break;
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

            // Berikan bonus pecahan Hero Shards jika First-Clear atau jaminan drop pada repeat clear Mini-Boss / Boss
            if (isFirstClear && currentStage.firstClearShards > 0)
            {
                CitadelLobbyManager.Instance.AddHeroShards(currentStage.firstClearShardHero, currentStage.firstClearShards);
            }
            else if (!isFirstClear && currentStage.repeatClearShards > 0)
            {
                CitadelLobbyManager.Instance.AddHeroShards(currentStage.repeatClearShardHero, currentStage.repeatClearShards);
                Debug.Log($"[StageManager] Guaranteed Repeat Drop: +{currentStage.repeatClearShards} Shards untuk {currentStage.repeatClearShardHero}!");
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
