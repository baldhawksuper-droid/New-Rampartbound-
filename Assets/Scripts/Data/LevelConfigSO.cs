using UnityEngine;

namespace FantasyDefense.Data
{
    public enum BattleMode
    {
        RealTimeDeploy = 0,     // Mode 2: Real-time Deploy & Merge (Lucky Defense / Rush Royale style)
        PuzzlePreparation = 1   // Mode 1: Puzzle Preparation (Turn-Based Match-3 style)
    }

    [CreateAssetMenu(fileName = "NewLevelConfig", menuName = "FantasyDefense/Level Config", order = 2)]
    public class LevelConfigSO : ScriptableObject
    {
        [Header("Battle Mode Selection")]
        [Tooltip("Mode 1: Puzzle Preparation (Langkah Terbatas) atau Mode 2: Real-time Deploy & Merge")]
        public BattleMode battleMode = BattleMode.PuzzlePreparation;
        [Range(5, 30)] public int preparationMoves = 15;
        [Tooltip("Langkah taktis tambahan yang diberikan antar-gelombang (Inter-Wave Preparation)")]
        [Range(2, 15)] public int interWaveMoves = 8;

        [Header("Level Identification")]
        public int levelIndex = 1;
        public string levelName = "Citadel Outskirts - Stage 1";
        public int recommendedTeamCP = 1500;

        [Header("Grid Arena Dimensions")]
        [Range(3, 9)] public int gridWidth = 5;
        [Range(3, 9)] public int gridHeight = 5;
        public float cellSize = 0.98f;
        public float cellSpacing = 0.08f;

        [Header("Battlefield Economics")]
        public int startingGems = 100;
        public int baseSummonCost = 40;
        public int summonCostIncrement = 10;
        public int gemsPerWaveClear = 30;

        [Header("Citadel Defense")]
        public int citadelMaxHealth = 100;
        public int castleMaxHealth => citadelMaxHealth;
    }
}
