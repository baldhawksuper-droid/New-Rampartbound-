using System;
using System.Collections.Generic;
using UnityEngine;
using FantasyDefense.Audio;
using FantasyDefense.Data;

namespace FantasyDefense.Metagame
{
    [Serializable]
    public class GachaResultItem
    {
        public UnitType unitType;
        public string heroName;
        public string rarityLabel;
        public int shardsGained;
        public bool isNewUnlock;

        public GachaResultItem(UnitType type, string name, string rarity, int shards, bool isNew)
        {
            unitType = type;
            heroName = name;
            rarityLabel = rarity;
            shardsGained = shards;
            isNewUnlock = isNew;
        }
    }

    /// <summary>
    /// Mengelola Hall Pemanggilan Citadel (Summoning Hall & Gacha Banner):
    /// Single Pull (100 Gems) & 10x Multi-Pull (900 Gems, diskon 10%).
    /// Menghadirkan Banner Spesial: Rate-Up Wind Ranger (Pemanah Angin)!
    /// Mendukung konversi hero duplikat menjadi Hero Shards untuk promosi bintang (Star Rank 1★ - 5★).
    /// </summary>
    public class CitadelSummonManager : MonoBehaviour
    {
        public static CitadelSummonManager Instance { get; private set; }

        [Header("Gacha Costs")]
        [SerializeField] private int singlePullCost = 100;
        [SerializeField] private int multiPullCost = 900; // 10x summon discount

        [Header("Drop Rates (Summon Weight)")]
        [Tooltip("Bobot kemungkinan per hero: Cedric (25%), Rose (25%), Rabbit (25%), Wind Ranger Rate-Up (25%)")]
        [SerializeField] private int cedricWeight = 25;
        [SerializeField] private int roseWeight = 25;
        [SerializeField] private int rabbitWeight = 25;
        [SerializeField] private int windRangerWeight = 25; // Featured Hero Banner

        public int SinglePullCost => singlePullCost;
        public int MultiPullCost => multiPullCost;

        public static event Action<List<GachaResultItem>> OnGachaPullsCompleted;
        public static event Action<string> OnGachaFailed;

        [Header("Daily Free Summon")]
        [SerializeField] private bool hasUsedDailyFreeSummon = false;

        public bool HasFreeDailySummon() => !hasUsedDailyFreeSummon;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public bool TryFreeDailySummon(out List<GachaResultItem> results)
        {
            results = new List<GachaResultItem>();
            if (hasUsedDailyFreeSummon)
            {
                OnGachaFailed?.Invoke("Panggilan gratis hari ini sudah digunakan!");
                return false;
            }

            hasUsedDailyFreeSummon = true;
            GachaResultItem item = PerformSinglePull();
            results.Add(item);

            PlayGachaAudio(results);
            OnGachaPullsCompleted?.Invoke(results);
            return true;
        }

        public bool TrySingleSummon(out List<GachaResultItem> results)
        {
            results = new List<GachaResultItem>();

            if (CitadelLobbyManager.Instance == null)
            {
                OnGachaFailed?.Invoke("Citadel Lobby Manager tidak aktif!");
                return false;
            }

            if (!CitadelLobbyManager.Instance.SpendAccountGems(singlePullCost))
            {
                OnGachaFailed?.Invoke($"Gems tidak cukup! Butuh {singlePullCost} Gems.");
                return false;
            }

            GachaResultItem item = PerformSinglePull();
            results.Add(item);

            PlayGachaAudio(results);
            OnGachaPullsCompleted?.Invoke(results);
            return true;
        }

        public bool TryMultiSummon(out List<GachaResultItem> results)
        {
            results = new List<GachaResultItem>();

            if (CitadelLobbyManager.Instance == null)
            {
                OnGachaFailed?.Invoke("Citadel Lobby Manager tidak aktif!");
                return false;
            }

            if (!CitadelLobbyManager.Instance.SpendAccountGems(multiPullCost))
            {
                OnGachaFailed?.Invoke($"Gems tidak cukup! Butuh {multiPullCost} Gems untuk 10x Summon.");
                return false;
            }

            for (int i = 0; i < 10; i++)
            {
                results.Add(PerformSinglePull());
            }

            PlayGachaAudio(results);
            OnGachaPullsCompleted?.Invoke(results);
            return true;
        }

        private GachaResultItem PerformSinglePull()
        {
            int totalWeight = cedricWeight + roseWeight + rabbitWeight + windRangerWeight;
            int roll = UnityEngine.Random.Range(0, totalWeight);

            UnitType pickedType;
            string heroName;
            string rarity;
            int shardsBonus;

            if (roll < cedricWeight)
            {
                pickedType = UnitType.Cedric;
                heroName = "Cedric (Ksatria Merah)";
                rarity = "Rare";
                shardsBonus = 20;
            }
            else if (roll < cedricWeight + roseWeight)
            {
                pickedType = UnitType.RoseTanker;
                heroName = "Rose Tanker (Ksatria Pink)";
                rarity = "Rare";
                shardsBonus = 20;
            }
            else if (roll < cedricWeight + roseWeight + rabbitWeight)
            {
                pickedType = UnitType.RabbitMage;
                heroName = "Rabbit Mage (Penyihir Kelinci)";
                rarity = "Epic";
                shardsBonus = 25;
            }
            else
            {
                // Featured Rate-Up: Wind Ranger
                pickedType = UnitType.WindRanger;
                heroName = "Wind Ranger (Pemanah Angin)";
                rarity = "Legendary [RATE-UP]";
                shardsBonus = 30;
            }

            // Integrasi ke Roster & Shards
            bool isNew = false;
            if (CitadelLobbyManager.Instance != null)
            {
                var profile = CitadelLobbyManager.Instance.GetHeroProfile(pickedType);
                if (profile == null)
                {
                    // Hero baru dibuka pertama kali
                    isNew = true;
                    profile = new HeroLobbyProfile(pickedType, heroName, 1, 0, 30f, 130f, 0.08f, 1, true);
                    CitadelLobbyManager.Instance.AccountData.activeRoster.Add(profile);
                    profile.CalculateCombatPower();
                }
                else if (!profile.isUnlocked)
                {
                    isNew = true;
                    profile.isUnlocked = true;
                }
                else
                {
                    // Duplikat -> konversi ke shards
                    CitadelLobbyManager.Instance.AddHeroShards(pickedType, shardsBonus);
                }
            }

            return new GachaResultItem(pickedType, heroName, rarity, shardsBonus, isNew);
        }

        private void PlayGachaAudio(List<GachaResultItem> results)
        {
            if (AudioManager.Instance == null) return;

            bool hasLegendary = results.Exists(r => r.rarityLabel.Contains("Legendary") || r.unitType == UnitType.WindRanger);
            if (hasLegendary)
            {
                AudioManager.Instance.PlaySound(SoundType.GachaLegendary);
            }
            else
            {
                AudioManager.Instance.PlaySound(SoundType.GachaPull);
            }
        }
    }
}
