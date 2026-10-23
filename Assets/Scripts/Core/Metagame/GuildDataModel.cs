using System;
using System.Collections.Generic;
using UnityEngine;
using FantasyDefense.Data;

namespace FantasyDefense.Metagame
{
    public enum GuildMemberRole
    {
        Member,
        Officer,
        Leader
    }

    [Serializable]
    public class GuildMember
    {
        public string memberId;
        public string memberName;
        public GuildMemberRole role;
        public int playerLevel;
        public int teamCP;
        public int weeklyDonation;
        public long lastActiveUtc;

        public GuildMember() { }

        public GuildMember(string id, string name, GuildMemberRole r, int lvl, int cp, int donation = 0)
        {
            memberId = id;
            memberName = name;
            role = r;
            playerLevel = lvl;
            teamCP = cp;
            weeklyDonation = donation;
            lastActiveUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }

    [Serializable]
    public class GuildExpedition
    {
        public string expeditionId;
        public string title;
        public string description;
        public float durationSeconds; // Durasi patroli (misal 300s / 5 menit, 1800s / 30 menit)
        public UnitType assignedHeroType;
        public long startTimeUtc;
        public int rewardGold;
        public int rewardTokens;
        public int rewardForgeDust;
        public bool isDispatched;
        public bool isClaimed;

        public GuildExpedition() { }

        public GuildExpedition(string id, string title, string desc, float duration, int gold, int tokens, int dust)
        {
            expeditionId = id;
            this.title = title;
            description = desc;
            durationSeconds = duration;
            rewardGold = gold;
            rewardTokens = tokens;
            rewardForgeDust = dust;
            isDispatched = false;
            isClaimed = false;
            startTimeUtc = 0;
        }

        public float GetRemainingSeconds()
        {
            if (!isDispatched || isClaimed) return 0f;
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long elapsed = now - startTimeUtc;
            float remaining = durationSeconds - elapsed;
            return Mathf.Max(0f, remaining);
        }

        public bool IsFinished()
        {
            if (!isDispatched) return false;
            if (isClaimed) return true;
            return GetRemainingSeconds() <= 0f;
        }

        public void Dispatch(UnitType hero)
        {
            assignedHeroType = hero;
            isDispatched = true;
            isClaimed = false;
            startTimeUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }

    [Serializable]
    public class GuildTitanRaid
    {
        public string titanName = "Colossus of the Abyss";
        public int titanLevel = 1;
        public long titanMaxHp = 5000000; // 5 Juta HP Kolosal
        public long titanCurrentHp = 5000000;
        public long guildTotalDamage = 0;
        public long personalDamageDealt = 0;
        public long seasonEndTimeUtc;
        public List<int> claimedMilestones = new List<int>(); // 20, 40, 60, 80, 100%

        public GuildTitanRaid()
        {
            seasonEndTimeUtc = DateTimeOffset.UtcNow.AddDays(7).ToUnixTimeSeconds(); // Reset mingguan
        }

        public float GetHpPercent()
        {
            if (titanMaxHp <= 0) return 0f;
            return Mathf.Clamp01((float)titanCurrentHp / titanMaxHp);
        }

        public void ApplyDamage(long damage, bool isPersonal)
        {
            titanCurrentHp = Math.Max(0, titanCurrentHp - damage);
            guildTotalDamage += damage;
            if (isPersonal)
            {
                personalDamageDealt += damage;
            }
        }

        public bool IsMilestoneClaimable(int milestonePercent)
        {
            if (claimedMilestones == null) claimedMilestones = new List<int>();
            if (claimedMilestones.Contains(milestonePercent)) return false;

            float damagePercent = (float)(titanMaxHp - titanCurrentHp) / Mathf.Max(1, titanMaxHp) * 100f;
            return damagePercent >= milestonePercent;
        }

        public bool ClaimMilestone(int milestonePercent)
        {
            if (IsMilestoneClaimable(milestonePercent))
            {
                claimedMilestones.Add(milestonePercent);
                return true;
            }
            return false;
        }
    }

    [Serializable]
    public class GuildProfile
    {
        public string guildId;
        public string guildName;
        public int guildLevel = 1;
        public int guildExp = 0;
        public string guildNotice = "Selamat datang di Benteng Aliansi! Bersama kita taklukkan Titan dan jaga Citadel!";
        public int guildTokens = 0; // Mata uang belanja fasilitas Guild

        public List<GuildMember> members = new List<GuildMember>();
        public List<GuildExpedition> expeditions = new List<GuildExpedition>();
        public GuildTitanRaid titanRaid = new GuildTitanRaid();

        public GuildProfile()
        {
            guildId = "guild_rampart_alpha";
            guildName = "Aegis Vanguard";
            guildLevel = 1;
            guildExp = 0;
            guildTokens = 150;
        }

        public int GetRequiredExpForNextLevel()
        {
            return guildLevel * 1000;
        }

        public bool AddExp(int exp, out bool leveledUp)
        {
            guildExp += exp;
            leveledUp = false;
            int req = GetRequiredExpForNextLevel();
            while (guildExp >= req && guildLevel < 20)
            {
                guildExp -= req;
                guildLevel++;
                leveledUp = true;
                req = GetRequiredExpForNextLevel();
            }
            return leveledUp;
        }

        public void AddTokens(int amount)
        {
            if (amount <= 0) return;
            guildTokens += amount;
        }

        public bool SpendTokens(int amount)
        {
            if (amount <= 0) return true;
            if (guildTokens >= amount)
            {
                guildTokens -= amount;
                return true;
            }
            return false;
        }
    }
}
