using System;
using UnityEngine;
using FantasyDefense.Data;

namespace FantasyDefense.Economy
{
    /// <summary>
    /// Mengatur ekonomi gems di dalam arena battle secara real-time.
    /// Menerapkan formula dinamis pemanggilan hero: Biaya = 40 + (n - 1) * 10 Gems.
    /// </summary>
    public class EconomyManager : MonoBehaviour
    {
        public static EconomyManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private LevelConfigSO levelConfig;

        [Header("Runtime State")]
        [SerializeField] private int currentGems = 100;
        [SerializeField] private int totalSummonsCount = 0;
        [SerializeField] private int currentBattleGold = 500;

        public int CurrentGems => currentGems;
        public int TotalSummonsCount => totalSummonsCount;
        public int CurrentBattleGold => currentBattleGold;

        public static event Action<int> OnGemsChanged;
        public static event Action<int> OnSummonCostChanged;
        public static event Action<int> OnBattleGoldChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (levelConfig != null)
            {
                Initialize(levelConfig);
            }
        }

        public void Initialize(LevelConfigSO config)
        {
            levelConfig = config;
            int startingBonus = 0;
            if (FantasyDefense.Metagame.CitadelTalentManager.Instance != null)
            {
                startingBonus = FantasyDefense.Metagame.CitadelTalentManager.Instance.GetStartingGemsBonus();
            }
            currentGems = config.startingGems + startingBonus;
            totalSummonsCount = 0;
            currentBattleGold = 500;

            OnGemsChanged?.Invoke(currentGems);
            OnSummonCostChanged?.Invoke(GetCurrentSummonCost());
            OnBattleGoldChanged?.Invoke(currentBattleGold);
            if (Combat.BattleArchetypeManager.Instance != null)
            {
                Combat.BattleArchetypeManager.Instance.ResetAllArchetypes();
            }
        }

        /// <summary>
        /// Formula biaya summon dinamis: Biaya = baseSummonCost + (totalSummonsCount * summonCostIncrement)
        /// Panggilan ke-1 = 40 gems, ke-2 = 50 gems, ke-3 = 60 gems, dst.
        /// </summary>
        public int GetCurrentSummonCost()
        {
            if (levelConfig == null) return 40;
            return levelConfig.baseSummonCost + (totalSummonsCount * levelConfig.summonCostIncrement);
        }

        public bool CanAffordSummon()
        {
            return currentGems >= GetCurrentSummonCost();
        }

        public bool SpendGemsForSummon()
        {
            int cost = GetCurrentSummonCost();
            if (currentGems < cost) return false;

            currentGems -= cost;
            totalSummonsCount++;

            OnGemsChanged?.Invoke(currentGems);
            OnSummonCostChanged?.Invoke(GetCurrentSummonCost());
            return true;
        }

        public void AddGems(int amount)
        {
            if (amount <= 0) return;
            if (Combat.BattleBlessingManager.Instance != null && Combat.BattleBlessingManager.Instance.GemBountyMultiplier > 1f)
            {
                amount = Mathf.RoundToInt(amount * Combat.BattleBlessingManager.Instance.GemBountyMultiplier);
            }
            currentGems += amount;
            OnGemsChanged?.Invoke(currentGems);
        }

        public bool SpendGems(int amount)
        {
            if (amount <= 0) return true;
            if (currentGems >= amount)
            {
                currentGems -= amount;
                OnGemsChanged?.Invoke(currentGems);
                return true;
            }
            return false;
        }

        public void AddGold(int amount)
        {
            if (amount <= 0) return;
            AddBattleGold(amount);
            if (FantasyDefense.Metagame.CitadelLobbyManager.Instance != null)
            {
                FantasyDefense.Metagame.CitadelLobbyManager.Instance.AddGold(amount);
            }
        }

        public void AddBattleGold(int amount)
        {
            if (amount <= 0) return;
            currentBattleGold += amount;
            OnBattleGoldChanged?.Invoke(currentBattleGold);
        }

        public bool SpendBattleGold(int amount)
        {
            if (amount <= 0) return true;
            if (currentBattleGold < amount) return false;
            currentBattleGold -= amount;
            OnBattleGoldChanged?.Invoke(currentBattleGold);
            return true;
        }

        public void DeductGems(int amount)
        {
            if (amount <= 0) return;
            currentGems = Mathf.Max(0, currentGems - amount);
            OnGemsChanged?.Invoke(currentGems);
        }

        /// <summary>
        /// Memberikan bonus dividen gems kepada pemain setiap menyelesaikan 1 gelombang musuh dari talenta benteng.
        /// </summary>
        public void AwardWaveDividend()
        {
            int bonus = 0;
            if (FantasyDefense.Metagame.CitadelTalentManager.Instance != null)
            {
                bonus = FantasyDefense.Metagame.CitadelTalentManager.Instance.GetWaveDividendBonus();
            }

            if (bonus > 0)
            {
                AddGems(bonus);
                Debug.Log($"[EconomyManager] Menerima dividen talenta Economic Prosperity: +{bonus} Gems!");
            }
        }
    }
}
