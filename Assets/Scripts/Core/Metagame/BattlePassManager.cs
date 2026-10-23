using System;
using System.Collections.Generic;
using UnityEngine;
using FantasyDefense.Audio;
using FantasyDefense.Data;
using FantasyDefense.Save;

namespace FantasyDefense.Metagame
{
    /// <summary>
    /// Master Manager untuk Citadel Battle Pass (Season 1 - 30 Tiers).
    /// Mengelola progres EXP musiman, jalur hadiah gratis (Free Track) & elit (Elite Track),
    /// serta integrasi klaim hadiah pahlawan dan material benteng.
    /// </summary>
    public class BattlePassManager : MonoBehaviour
    {
        public static BattlePassManager Instance { get; private set; }

        [Header("Season Data")]
        [SerializeField] private BattlePassSeason currentSeason;

        public BattlePassSeason CurrentSeason => currentSeason;

        public static event Action OnBattlePassUpdated;
        public static event Action<int> OnPassTierUnlocked;
        public static event Action<int, bool> OnPassRewardClaimed;
        public static event Action OnElitePassUnlocked;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeDefaultSeason();
        }

        private void InitializeDefaultSeason()
        {
            if (currentSeason == null)
            {
                currentSeason = new BattlePassSeason();
            }

            if (currentSeason.tiers == null || currentSeason.tiers.Count == 0)
            {
                currentSeason.tiers = new List<BattlePassTier>();

                for (int i = 1; i <= 30; i++)
                {
                    BattlePassRewardType freeType = BattlePassRewardType.Gold;
                    int freeAmt = 250 + (i * 25);

                    if (i % 5 == 0)
                    {
                        freeType = BattlePassRewardType.Gems;
                        freeAmt = 20 + (i * 2);
                    }
                    else if (i % 3 == 0)
                    {
                        freeType = BattlePassRewardType.Energy;
                        freeAmt = 15;
                    }

                    BattlePassRewardType eliteType = BattlePassRewardType.Gems;
                    int eliteAmt = 50 + (i * 5);

                    if (i == 30)
                    {
                        eliteType = BattlePassRewardType.ExclusiveAegisFrame;
                        eliteAmt = 1;
                    }
                    else if (i % 2 == 0)
                    {
                        eliteType = BattlePassRewardType.ForgeDust;
                        eliteAmt = 25 + i;
                    }
                    else if (i % 5 == 0)
                    {
                        eliteType = BattlePassRewardType.RefineStones;
                        eliteAmt = 5 + (i / 5);
                    }

                    currentSeason.tiers.Add(new BattlePassTier(i, 100, freeType, freeAmt, eliteType, eliteAmt));
                }
            }
        }

        /// <summary>
        /// Menambahkan Pass EXP (dari penyelesaian Daily Quests atau pencapaian pertempuran).
        /// </summary>
        public void AddPassExp(int amount)
        {
            if (amount <= 0 || currentSeason == null) return;

            int oldLevel = currentSeason.currentPassLevel;
            bool leveledUp = currentSeason.AddExp(amount, out bool up);

            Debug.Log($"[BattlePassManager] +{amount} Pass EXP! Level sekarang: {currentSeason.currentPassLevel} (Tier progress: {currentSeason.currentPassExp}/{currentSeason.ExpPerTier})");

            if (up)
            {
                Debug.Log($"[BattlePassManager] Selamat! Tier Baru Terbuka: Tier {currentSeason.currentPassLevel}!");
                OnPassTierUnlocked?.Invoke(currentSeason.currentPassLevel);

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySound(SoundType.MergeSuccess);
                }
            }

            SaveManager.Instance?.SaveProfile(SaveManager.Instance.CurrentData);
            OnBattlePassUpdated?.Invoke();
        }

        /// <summary>
        /// Mengklaim hadiah jalur gratis (Free Pass) pada tingkat tier tertentu.
        /// </summary>
        public bool ClaimFreeReward(int tierLevel, out string resultMessage)
        {
            if (currentSeason == null)
            {
                resultMessage = "Data Season Pass belum siap!";
                return false;
            }

            var tier = currentSeason.GetTier(tierLevel);
            if (tier == null)
            {
                resultMessage = "Tier tidak ditemukan!";
                return false;
            }

            if (tierLevel > currentSeason.currentPassLevel)
            {
                resultMessage = $"Tier {tierLevel} masih terkunci! (Level Anda: {currentSeason.currentPassLevel})";
                return false;
            }

            if (tier.isFreeClaimed)
            {
                resultMessage = $"Hadiah Gratis Tier {tierLevel} sudah pernah diklaim!";
                return false;
            }

            tier.isFreeClaimed = true;
            GrantReward(tier.freeRewardType, tier.freeRewardAmount);

            resultMessage = $"Hadiah Gratis Tier {tierLevel} berhasil diklaim: +{tier.freeRewardAmount} {tier.freeRewardType}!";
            Debug.Log($"[BattlePassManager] {resultMessage}");

            SaveManager.Instance?.SaveProfile(SaveManager.Instance.CurrentData, syncToCloud: true);
            OnPassRewardClaimed?.Invoke(tierLevel, false);
            OnBattlePassUpdated?.Invoke();
            return true;
        }

        /// <summary>
        /// Mengklaim hadiah jalur elit (Elite Pass) pada tingkat tier tertentu.
        /// </summary>
        public bool ClaimEliteReward(int tierLevel, out string resultMessage)
        {
            if (currentSeason == null)
            {
                resultMessage = "Data Season Pass belum siap!";
                return false;
            }

            if (!currentSeason.isElitePassUnlocked)
            {
                resultMessage = "Jalur Elite Pass belum diaktifkan! Beli Elite Pass terlebih dahulu.";
                return false;
            }

            var tier = currentSeason.GetTier(tierLevel);
            if (tier == null)
            {
                resultMessage = "Tier tidak ditemukan!";
                return false;
            }

            if (tierLevel > currentSeason.currentPassLevel)
            {
                resultMessage = $"Tier {tierLevel} masih terkunci! (Level Anda: {currentSeason.currentPassLevel})";
                return false;
            }

            if (tier.isEliteClaimed)
            {
                resultMessage = $"Hadiah Elite Tier {tierLevel} sudah pernah diklaim!";
                return false;
            }

            tier.isEliteClaimed = true;
            GrantReward(tier.eliteRewardType, tier.eliteRewardAmount);

            resultMessage = $"Hadiah Elite Tier {tierLevel} berhasil diklaim: +{tier.eliteRewardAmount} {tier.eliteRewardType}!";
            Debug.Log($"[BattlePassManager] {resultMessage}");

            SaveManager.Instance?.SaveProfile(SaveManager.Instance.CurrentData, syncToCloud: true);
            OnPassRewardClaimed?.Invoke(tierLevel, true);
            OnBattlePassUpdated?.Invoke();
            return true;
        }

        /// <summary>
        /// Mengklaim seluruh hadiah yang siap diambil sekaligus (Claim All Button).
        /// </summary>
        public int ClaimAllAvailableRewards()
        {
            if (currentSeason == null) return 0;
            int totalClaimed = 0;

            for (int i = 1; i <= currentSeason.currentPassLevel; i++)
            {
                var tier = currentSeason.GetTier(i);
                if (tier != null)
                {
                    if (!tier.isFreeClaimed)
                    {
                        tier.isFreeClaimed = true;
                        GrantReward(tier.freeRewardType, tier.freeRewardAmount);
                        totalClaimed++;
                    }

                    if (currentSeason.isElitePassUnlocked && !tier.isEliteClaimed)
                    {
                        tier.isEliteClaimed = true;
                        GrantReward(tier.eliteRewardType, tier.eliteRewardAmount);
                        totalClaimed++;
                    }
                }
            }

            if (totalClaimed > 0)
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySound(SoundType.MergeSuccess);
                }
                SaveManager.Instance?.SaveProfile(SaveManager.Instance.CurrentData, syncToCloud: true);
                OnBattlePassUpdated?.Invoke();
            }

            return totalClaimed;
        }

        /// <summary>
        /// Mengaktifkan status Elite Pass menggunakan Gems atau melalui bundle toko.
        /// </summary>
        public bool UnlockElitePass(bool spendGems, out string resultMessage)
        {
            if (currentSeason == null)
            {
                resultMessage = "Data Season Pass tidak valid!";
                return false;
            }

            if (currentSeason.isElitePassUnlocked)
            {
                resultMessage = "Elite Pass sudah aktif!";
                return false;
            }

            int gemCost = 500;
            if (spendGems)
            {
                if (CitadelLobbyManager.Instance != null && !CitadelLobbyManager.Instance.SpendGems(gemCost))
                {
                    resultMessage = $"Gems tidak mencukupi untuk membuka Elite Pass ({gemCost} Gems diperlukan)!";
                    return false;
                }
            }

            currentSeason.isElitePassUnlocked = true;
            resultMessage = "Luar biasa! Elite Pass Berhasil Diaktifkan! Semua hadiah eksklusif siap diklaim!";
            Debug.Log($"[BattlePassManager] {resultMessage}");

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.MergeSuccess);
            }

            SaveManager.Instance?.SaveProfile(SaveManager.Instance.CurrentData, syncToCloud: true);
            OnElitePassUnlocked?.Invoke();
            OnBattlePassUpdated?.Invoke();
            return true;
        }

        private void GrantReward(BattlePassRewardType type, int amount)
        {
            if (CitadelLobbyManager.Instance == null) return;

            switch (type)
            {
                case BattlePassRewardType.Gold:
                    CitadelLobbyManager.Instance.AddGold(amount);
                    break;
                case BattlePassRewardType.Gems:
                    CitadelLobbyManager.Instance.AddGems(amount);
                    break;
                case BattlePassRewardType.Energy:
                    CitadelLobbyManager.Instance.RestoreEnergy(amount);
                    break;
                case BattlePassRewardType.ForgeDust:
                    CitadelLobbyManager.Instance.AddForgeMaterials(amount, 0);
                    break;
                case BattlePassRewardType.RefineStones:
                    CitadelLobbyManager.Instance.AddForgeMaterials(0, amount);
                    break;
                case BattlePassRewardType.HeroShards:
                    if (CitadelLobbyManager.Instance.AccountData != null && CitadelLobbyManager.Instance.AccountData.activeRoster.Count > 0)
                    {
                        CitadelLobbyManager.Instance.AccountData.activeRoster[0].heroShards += amount;
                    }
                    break;
                case BattlePassRewardType.ExclusiveAegisFrame:
                    Debug.Log("[BattlePassManager] Kosmetik Eksklusif 'Aegis Frame' Terbuka!");
                    break;
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.MergeSuccess);
            }
        }
    }
}
