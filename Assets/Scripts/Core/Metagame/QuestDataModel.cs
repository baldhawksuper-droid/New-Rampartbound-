using System;
using UnityEngine;

namespace FantasyDefense.Metagame
{
    public enum QuestCategory
    {
        Daily,
        Achievement
    }

    public enum QuestObjectiveType
    {
        SummonHeroes,
        MergeHeroes,
        ClearStages,
        DefeatMonsters,
        UpgradeHero,
        ReachTeamCP,
        CreateTier3
    }

    /// <summary>
    /// Model data untuk satu item Misi Harian atau Pencapaian Kerajaan (Quest & Achievement).
    /// </summary>
    [Serializable]
    public class QuestItem
    {
        public string questId;
        public string title;
        public string description;
        public QuestCategory category;
        public QuestObjectiveType objectiveType;
        public int targetAmount;
        public int currentAmount;
        public int rewardGold;
        public int rewardGems;
        public int rewardEnergy;
        public int rewardShards;
        public bool isClaimed;

        public bool IsCompleted => currentAmount >= targetAmount;
        public float ProgressNormalized => Mathf.Clamp01((float)currentAmount / Mathf.Max(1, targetAmount));

        public QuestItem() { }

        public QuestItem(string id, string t, string desc, QuestCategory cat, QuestObjectiveType obj, int target, int gold, int gems, int energy = 0, int shards = 0)
        {
            questId = id;
            title = t;
            description = desc;
            category = cat;
            objectiveType = obj;
            targetAmount = target;
            currentAmount = 0;
            rewardGold = gold;
            rewardGems = gems;
            rewardEnergy = energy;
            rewardShards = shards;
            isClaimed = false;
        }
    }
}
