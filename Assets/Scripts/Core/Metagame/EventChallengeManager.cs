using System;
using System.Collections.Generic;
using UnityEngine;
using FantasyDefense.Audio;
using FantasyDefense.Data;
using FantasyDefense.Save;

namespace FantasyDefense.Metagame
{
    public enum EventChallengeType
    {
        BossRush,
        SpeedDefense,
        HeroRestriction,
        AffixMadness
    }

    [Serializable]
    public class ChallengeModifier
    {
        public string modifierName;
        public string description;
        public float monsterSpeedMultiplier = 1.0f;
        public float monsterHpMultiplier = 1.0f;
        public float heroAtkMultiplier = 1.0f;
        public bool disableMagicDamage = false;
        public bool disablePhysicalDamage = false;

        public ChallengeModifier(string name, string desc, float spd = 1f, float hp = 1f, float atk = 1f)
        {
            modifierName = name;
            description = desc;
            monsterSpeedMultiplier = spd;
            monsterHpMultiplier = hp;
            heroAtkMultiplier = atk;
        }
    }

    [Serializable]
    public class ChallengeMilestoneReward
    {
        public int requiredScore;
        public string rewardDescription;
        public int goldAmount;
        public int gemAmount;
        public int heroShardAmount;
        public bool isClaimed;

        public ChallengeMilestoneReward(int score, string desc, int gold, int gems, int shards = 0)
        {
            requiredScore = score;
            rewardDescription = desc;
            goldAmount = gold;
            gemAmount = gems;
            heroShardAmount = shards;
            isClaimed = false;
        }
    }

    [Serializable]
    public class EventChallengeData
    {
        public string eventId = "evt_abyssal_onslaught_01";
        public string eventTitle = "Abyssal Incursion Arena";
        public string description = "Hadapi gelombang monster mutasi elit dan Orc Berserker dengan batasan modifier ekstrem!";
        public EventChallengeType eventType = EventChallengeType.BossRush;
        public int remainingSeconds = 172800; // 48 Jam
        public int dailyAttemptsLeft = 3;
        public int maxDailyAttempts = 3;
        public int currentHighScore = 0;
        public int playerRank = 14;
        public List<ChallengeModifier> activeModifiers = new List<ChallengeModifier>();
        public List<ChallengeMilestoneReward> milestones = new List<ChallengeMilestoneReward>();
    }

    [Serializable]
    public class LeaderboardEntry
    {
        public int rank;
        public string playerName;
        public int score;
        public string favoriteHero;

        public LeaderboardEntry(int r, string name, int s, string hero)
        {
            rank = r;
            playerName = name;
            score = s;
            favoriteHero = hero;
        }
    }

    /// <summary>
    /// Master Manager untuk Event Arena & Limited-Time Challenge di Rampartbound.
    /// Mengelola rotasi event musiman, modifier tantangan ekstrem, progres skor, serta hadiah milestone.
    /// </summary>
    public class EventChallengeManager : MonoBehaviour
    {
        public static EventChallengeManager Instance { get; private set; }

        [Header("Event Configuration")]
        [SerializeField] private EventChallengeData currentEvent;
        [SerializeField] private List<LeaderboardEntry> simulatedLeaderboard = new List<LeaderboardEntry>();

        public EventChallengeData CurrentEvent => currentEvent;
        public IReadOnlyList<LeaderboardEntry> Leaderboard => simulatedLeaderboard;

        public static event Action OnEventChallengeUpdated;
        public static event Action<int> OnScoreUpdated; // (newHighScore)
        public static event Action<ChallengeMilestoneReward> OnRewardClaimed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeDefaultEvent();
            InitializeLeaderboard();
        }

        private void InitializeDefaultEvent()
        {
            if (currentEvent == null)
            {
                currentEvent = new EventChallengeData();
            }

            if (currentEvent.activeModifiers == null || currentEvent.activeModifiers.Count == 0)
            {
                currentEvent.activeModifiers = new List<ChallengeModifier>
                {
                    new ChallengeModifier("Frenzied Vanguard", "+30% Kecepatan Gerak Musuh, +25% HP Bos Orc", 1.3f, 1.25f, 1.0f),
                    new ChallengeModifier("Arcane Resonance", "Hero Sihir (Rabbit Mage) memperoleh +40% Attack Power", 1.0f, 1.0f, 1.4f)
                };
            }

            if (currentEvent.milestones == null || currentEvent.milestones.Count == 0)
            {
                currentEvent.milestones = new List<ChallengeMilestoneReward>
                {
                    new ChallengeMilestoneReward(1000, "1,000 Pts: Kantong Emas Benteng", 2500, 0, 0),
                    new ChallengeMilestoneReward(2500, "2,500 Pts: Kristal Abyssal & Shards", 5000, 50, 5),
                    new ChallengeMilestoneReward(5000, "5,000 Pts: Peti Harta Karun Legendaris", 12000, 150, 15),
                    new ChallengeMilestoneReward(10000, "10,000 Pts: Mahkota Penakluk Abyssal", 25000, 300, 30)
                };
            }
        }

        private void InitializeLeaderboard()
        {
            if (simulatedLeaderboard == null || simulatedLeaderboard.Count == 0)
            {
                simulatedLeaderboard = new List<LeaderboardEntry>
                {
                    new LeaderboardEntry(1, "Lord_Aegis", 14500, "Cedric"),
                    new LeaderboardEntry(2, "StormBringer", 13200, "Wind Ranger"),
                    new LeaderboardEntry(3, "ChronoMage", 12100, "Rabbit Mage"),
                    new LeaderboardEntry(4, "PinkValiant", 11450, "Rose Tanker"),
                    new LeaderboardEntry(5, "IronCitadel", 10800, "Cedric"),
                    new LeaderboardEntry(6, "ShadowSniper", 9950, "Wind Ranger"),
                    new LeaderboardEntry(7, "MysticRabbit", 9400, "Rabbit Mage"),
                    new LeaderboardEntry(8, "TitanWall", 8800, "Rose Tanker"),
                    new LeaderboardEntry(9, "CrimsonBlade", 8300, "Cedric"),
                    new LeaderboardEntry(10, "GaleStriker", 7900, "Wind Ranger")
                };
            }
        }

        /// <summary>
        /// Menggunakan 1 tiket percobaan harian untuk memulai pertempuran arena tantangan.
        /// </summary>
        public bool ConsumeAttempt()
        {
            if (currentEvent.dailyAttemptsLeft <= 0)
            {
                Debug.LogWarning("[EventChallengeManager] Kesempatan harian habis! Beli tiket tambahan dengan Gems.");
                return false;
            }

            currentEvent.dailyAttemptsLeft--;
            OnEventChallengeUpdated?.Invoke();
            return true;
        }

        /// <summary>
        /// Membeli 1 tiket tantangan tambahan menggunakan Gems.
        /// </summary>
        public bool PurchaseAdditionalAttempt(int gemCost = 50)
        {
            if (CitadelLobbyManager.Instance != null)
            {
                if (!CitadelLobbyManager.Instance.SpendGems(gemCost))
                {
                    Debug.LogWarning("[EventChallengeManager] Gems tidak mencukupi untuk membeli tiket tantangan.");
                    return false;
                }
            }
            else if (Economy.EconomyManager.Instance != null)
            {
                if (!Economy.EconomyManager.Instance.SpendGems(gemCost))
                {
                    Debug.LogWarning("[EventChallengeManager] Gems tidak mencukupi untuk membeli tiket tantangan.");
                    return false;
                }
            }

            currentEvent.dailyAttemptsLeft++;
            AudioManager.Instance?.PlaySfx(AudioSfxType.ButtonClick);
            OnEventChallengeUpdated?.Invoke();
            Debug.Log($"[EventChallengeManager] Berhasil membeli 1 tiket tantangan. Sisa: {currentEvent.dailyAttemptsLeft}");
            return true;
        }

        /// <summary>
        /// Merekam perolehan skor setelah menyelesaikan gelombang arena event.
        /// </summary>
        public void RecordEventScore(int finalScore)
        {
            if (finalScore > currentEvent.currentHighScore)
            {
                currentEvent.currentHighScore = finalScore;
                Debug.Log($"[EventChallengeManager] Rekor Skor Baru Tercatat: {finalScore} Pts!");
                OnScoreUpdated?.Invoke(finalScore);

                UpdateSimulatedPlayerRank(finalScore);
            }

            OnEventChallengeUpdated?.Invoke();
        }

        private void UpdateSimulatedPlayerRank(int score)
        {
            if (score >= 14000) currentEvent.playerRank = 2;
            else if (score >= 12000) currentEvent.playerRank = 4;
            else if (score >= 10000) currentEvent.playerRank = 6;
            else if (score >= 8000) currentEvent.playerRank = 9;
            else if (score >= 5000) currentEvent.playerRank = 14;
            else currentEvent.playerRank = 25;
        }

        /// <summary>
        /// Mengklaim hadiah milestone berdasarkan skor tertinggi yang telah dicapai.
        /// </summary>
        public bool ClaimMilestoneReward(int milestoneIndex)
        {
            if (milestoneIndex < 0 || milestoneIndex >= currentEvent.milestones.Count)
                return false;

            var milestone = currentEvent.milestones[milestoneIndex];
            if (milestone.isClaimed)
            {
                Debug.LogWarning("[EventChallengeManager] Hadiah milestone ini telah diklaim sebelumnya.");
                return false;
            }

            if (currentEvent.currentHighScore < milestone.requiredScore)
            {
                Debug.LogWarning($"[EventChallengeManager] Skor belum mencukupi! Butuh {milestone.requiredScore} Pts.");
                return false;
            }

            milestone.isClaimed = true;

            // Injeksi hadiah ke ekonomi pemain (Citadel Lobby profile)
            if (CitadelLobbyManager.Instance != null)
            {
                if (milestone.goldAmount > 0) CitadelLobbyManager.Instance.AddGold(milestone.goldAmount);
                if (milestone.gemAmount > 0) CitadelLobbyManager.Instance.AddGems(milestone.gemAmount);
            }
            else if (Economy.EconomyManager.Instance != null)
            {
                if (milestone.goldAmount > 0) Economy.EconomyManager.Instance.AddGold(milestone.goldAmount);
                if (milestone.gemAmount > 0) Economy.EconomyManager.Instance.AddGems(milestone.gemAmount);
            }

            AudioManager.Instance?.PlaySfx(AudioSfxType.UnitMergeTier3);
            OnRewardClaimed?.Invoke(milestone);
            OnEventChallengeUpdated?.Invoke();

            Debug.Log($"[EventChallengeManager] Berhasil mengklaim hadiah milestone: {milestone.rewardDescription}");
            return true;
        }
    }
}
