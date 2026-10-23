using System;
using System.Collections.Generic;
using UnityEngine;

namespace FantasyDefense.Data
{
    [Serializable]
    public class AbyssalMilestoneReward
    {
        public int floorMilestone;       // Contoh: Lantai 5, 10, 15, 20, 25, 30
        public int goldReward = 500;
        public int gemsReward = 30;
        public int forgeDustReward = 40;
        public int refineStoneReward = 1;
        public string rewardDescription = "Peti Harta Abyssal";

        public AbyssalMilestoneReward() { }

        public AbyssalMilestoneReward(int floor, int gold, int gems, int dust, int stones, string desc)
        {
            floorMilestone = floor;
            goldReward = gold;
            gemsReward = gems;
            forgeDustReward = dust;
            refineStoneReward = stones;
            rewardDescription = desc;
        }
    }

    /// <summary>
    /// Konfigurasi data dan formula penskalaan eksponensial untuk Mode Endless Abyssal Trial.
    /// Mengatur pertumbuhan stat HP/ATK musuh tiap lantai, kuota spawn monster, dan hadiah peti mingguan.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_EndlessTrialConfig", menuName = "FantasyDefense/Config/Endless Trial Config", order = 5)]
    public class EndlessTrialConfigSO : ScriptableObject
    {
        [Header("Mode Identity")]
        public string modeTitle = "Endless Abyssal Trial";
        [TextArea(2, 4)]
        public string modeDescription = "Hadapi gelombang monster tanpa henti dari jurang terdalam Citadel. Bertahanlah selama mungkin untuk meraih peringkat tertinggi dan peti harta mingguan!";

        [Header("Scaling Coefficients Per Floor")]
        [Tooltip("Kenaikan HP monster per lantai (+14% per lantai)")]
        public float healthGrowthRate = 0.14f;

        [Tooltip("Kenaikan Castle Damage/ATK monster per lantai (+8% per lantai)")]
        public float damageGrowthRate = 0.08f;

        [Tooltip("Kenaikan kecepatan jalan monster (+1.5% per lantai, maksimal +40%)")]
        public float speedGrowthRate = 0.015f;
        public float maxSpeedMultiplier = 1.40f;

        [Header("Spawn Pacing")]
        public int baseMonsterCount = 8;
        public int maxMonsterCount = 28;
        public float baseSpawnInterval = 1.1f;
        public float restTimeBetweenFloors = 3.5f;

        [Header("Boss Frequency")]
        [Tooltip("Setiap berapa lantai muncul Mini-Boss atau Drake Boss (contoh: tiap 5 lantai)")]
        public int bossEncounterInterval = 5;

        [Header("Milestone Rewards")]
        public List<AbyssalMilestoneReward> milestones = new List<AbyssalMilestoneReward>()
        {
            new AbyssalMilestoneReward(5, 400, 25, 30, 1, "Peti Perunggu Abyssal (Lantai 5)"),
            new AbyssalMilestoneReward(10, 800, 50, 60, 2, "Peti Perak Abyssal (Lantai 10)"),
            new AbyssalMilestoneReward(15, 1400, 80, 100, 3, "Peti Emas Abyssal (Lantai 15)"),
            new AbyssalMilestoneReward(20, 2200, 120, 150, 5, "Peti Berlian Abyssal (Lantai 20)"),
            new AbyssalMilestoneReward(25, 3200, 180, 220, 8, "Peti Abyssal Kuno (Lantai 25)"),
            new AbyssalMilestoneReward(30, 5000, 300, 350, 12, "Peti Mahkota Abyssal (Lantai 30)")
        };

        public float GetHealthMultiplier(int floor)
        {
            return 1.0f + ((floor - 1) * healthGrowthRate);
        }

        public float GetDamageMultiplier(int floor)
        {
            return 1.0f + ((floor - 1) * damageGrowthRate);
        }

        public float GetSpeedMultiplier(int floor)
        {
            return Mathf.Min(maxSpeedMultiplier, 1.0f + ((floor - 1) * speedGrowthRate));
        }

        public int GetMonsterCount(int floor)
        {
            return Mathf.Clamp(baseMonsterCount + ((floor - 1) * 2), baseMonsterCount, maxMonsterCount);
        }

        public bool IsBossFloor(int floor)
        {
            return floor > 0 && (floor % bossEncounterInterval == 0);
        }

        public AbyssalMilestoneReward GetMilestoneReward(int floor)
        {
            return milestones.Find(m => m.floorMilestone == floor);
        }
    }
}
