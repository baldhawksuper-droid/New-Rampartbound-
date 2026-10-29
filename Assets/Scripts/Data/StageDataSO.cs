using System.Collections.Generic;
using UnityEngine;
using FantasyDefense.Combat;
using FantasyDefense.Metagame;

namespace FantasyDefense.Data
{
    [CreateAssetMenu(fileName = "NewStageData", menuName = "FantasyDefense/Stage Data", order = 4)]
    public class StageDataSO : ScriptableObject
    {
        [Header("Stage Identification")]
        public int stageId = 101;            // 101 = Stage 1-1, 110 = Stage 1-10
        public int chapterNumber = 1;
        public int stageNumber = 1;
        public string stageCode = "Stage 1-1";
        public string stageTitle = "Gerbang Luar Citadel";
        [TextArea(2, 3)]
        public string stageDescription = "Pertahankan gerbang terluar kerajaan dari serbuan gelombang patroli goblin.";

        [Header("Combat Requirements")]
        [Tooltip("Rekomendasi Team CP untuk stage ini")]
        public int recommendedCP = 1500;
        public int staminaCost = 5;
        public bool isBossStage = false;
        public string bossName = "";

        [Header("First-Clear Rewards (Hadiah Penaklukan Pertama)")]
        public int firstClearGold = 350;
        public int firstClearGems = 60;
        public int firstClearExp = 120;
        public int firstClearShards = 15;
        public UnitType firstClearShardHero = UnitType.Cedric;
        public EquipmentItem firstClearGear;

        [Header("Repeat Clear Rewards (Hadiah Rutin Grinding)")]
        public int repeatClearGold = 150;
        public int repeatClearExp = 50;
        public int repeatClearGems = 15;

        [Header("Battle Mode & Weather Configuration")]
        [Tooltip("Mode pertempuran untuk stage ini: Mode 1 (Puzzle Preparation) atau Mode 2 (Real-time Deploy)")]
        public BattleMode battleMode = BattleMode.RealTimeDeploy;
        [Range(5, 30)] public int preparationMoves = 15;
        [Tooltip("Kondisi cuaca medan tempur pada stage ini (Sprint 12)")]
        public WeatherType stageWeather = WeatherType.None;

        [Header("Level & Waves Setup")]
        public LevelConfigSO levelConfig;
        public List<WaveDataSO> stageWaves = new List<WaveDataSO>();

        /// <summary>
        /// Mengembalikan konfigurasi level aktif yang tersinkronisasi dengan BattleMode stage ini.
        /// </summary>
        public LevelConfigSO GetEffectiveLevelConfig()
        {
            if (levelConfig != null)
            {
                levelConfig.battleMode = battleMode;
                levelConfig.preparationMoves = preparationMoves;
                return levelConfig;
            }

            var config = ScriptableObject.CreateInstance<LevelConfigSO>();
            config.levelIndex = stageNumber;
            config.levelName = $"{stageCode} - {stageTitle}";
            config.recommendedTeamCP = recommendedCP;
            config.battleMode = battleMode;
            config.preparationMoves = preparationMoves;
            config.gridWidth = 5;
            config.gridHeight = 5;
            config.cellSize = 0.98f;
            config.cellSpacing = 0.08f;
            config.startingGems = 100;
            config.baseSummonCost = 40;
            config.summonCostIncrement = 10;
            config.gemsPerWaveClear = 30;
            config.citadelMaxHealth = 100;
            return config;
        }

        public string GetBattleModeLabel()
        {
            if (battleMode == BattleMode.PuzzlePreparation)
            {
                string movesText = FantasyDefense.Localization.CitadelLocalizationManager.Text($"{preparationMoves} Langkah", $"{preparationMoves} Moves");
                return FantasyDefense.Localization.CitadelLocalizationManager.Text(
                    $"Mode 1: Puzzle Preparation ({movesText})",
                    $"Mode 1: Puzzle Preparation ({movesText})");
            }
            return FantasyDefense.Localization.CitadelLocalizationManager.Text(
                "Mode 2: Real-time Deploy & Merge",
                "Mode 2: Real-time Deploy & Merge");
        }

        /// <summary>
        /// Mengembalikan status kesulitan berdasarkan Team CP pemain saat ini dengan dukungan bilingual.
        /// </summary>
        public string GetDifficultyLabel(int playerTeamCP)
        {
            if (playerTeamCP >= recommendedCP * 1.15f) 
                return FantasyDefense.Localization.CitadelLocalizationManager.Text("Aman (Mudah)", "Safe (Easy)");
            if (playerTeamCP >= recommendedCP) 
                return FantasyDefense.Localization.CitadelLocalizationManager.Text("Seimbang (Normal)", "Balanced (Normal)");
            if (playerTeamCP >= recommendedCP * 0.85f) 
                return FantasyDefense.Localization.CitadelLocalizationManager.Text("Menantang (Keras)", "Challenging (Hard)");
            return FantasyDefense.Localization.CitadelLocalizationManager.Text("Sangat Berbahaya (Underleveled)", "Very Dangerous (Underleveled)");
        }

        public Color GetDifficultyColor(int playerTeamCP)
        {
            if (playerTeamCP >= recommendedCP) return new Color(0.2f, 0.8f, 0.3f);     // Hijau
            if (playerTeamCP >= recommendedCP * 0.85f) return new Color(1f, 0.65f, 0f); // Oranye
            return new Color(0.9f, 0.2f, 0.2f);                                         // Merah
        }
    }
}
