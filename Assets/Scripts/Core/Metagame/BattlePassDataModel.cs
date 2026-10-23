using System;
using System.Collections.Generic;
using UnityEngine;

namespace FantasyDefense.Metagame
{
    public enum BattlePassRewardType
    {
        Gold,
        Gems,
        Energy,
        HeroShards,
        ForgeDust,
        RefineStones,
        ExclusiveAegisFrame
    }

    [Serializable]
    public class BattlePassTier
    {
        public int tierLevel;
        public int requiredExp; // Misal 100 EXP per tier
        public BattlePassRewardType freeRewardType;
        public int freeRewardAmount;
        public bool isFreeClaimed;

        public BattlePassRewardType eliteRewardType;
        public int eliteRewardAmount;
        public bool isEliteClaimed;

        public BattlePassTier() { }

        public BattlePassTier(int level, int exp, BattlePassRewardType freeType, int freeAmt, BattlePassRewardType eliteType, int eliteAmt)
        {
            tierLevel = level;
            requiredExp = exp;
            freeRewardType = freeType;
            freeRewardAmount = freeAmt;
            isFreeClaimed = false;

            eliteRewardType = eliteType;
            eliteRewardAmount = eliteAmt;
            isEliteClaimed = false;
        }
    }

    [Serializable]
    public class BattlePassSeason
    {
        public string seasonId = "season_01_citadel_vanguard";
        public string seasonTitle = "Citadel Vanguard: Musim 1";
        public int currentPassExp = 0;
        public int currentPassLevel = 1;
        public bool isElitePassUnlocked = false;
        public long seasonEndTimeUtc;

        public List<BattlePassTier> tiers = new List<BattlePassTier>();

        public BattlePassSeason()
        {
            seasonEndTimeUtc = DateTimeOffset.UtcNow.AddDays(30).ToUnixTimeSeconds(); // 30 hari musiman
        }

        public int ExpPerTier => 100;

        public bool AddExp(int exp, out bool leveledUp)
        {
            currentPassExp += exp;
            leveledUp = false;

            while (currentPassExp >= ExpPerTier && currentPassLevel < 30)
            {
                currentPassExp -= ExpPerTier;
                currentPassLevel++;
                leveledUp = true;
            }

            return leveledUp;
        }

        public BattlePassTier GetTier(int level)
        {
            return tiers.Find(t => t.tierLevel == level);
        }
    }
}
