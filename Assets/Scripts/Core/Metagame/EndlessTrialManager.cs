using System;
using UnityEngine;
using FantasyDefense.Audio;
using FantasyDefense.Core;
using FantasyDefense.Data;
using FantasyDefense.Save;

namespace FantasyDefense.Metagame
{
    /// <summary>
    /// Master Controller untuk Mode Endgame "Endless Abyssal Trial".
    /// Mengontrol siklus tantangan survival tanpa batas, scaling monster tiap lantai,
    /// pencatatan rekor tertinggi, serta sistem klaim hadiah peti mingguan.
    /// </summary>
    public class EndlessTrialManager : MonoBehaviour
    {
        public static EndlessTrialManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private EndlessTrialConfigSO config;

        [Header("Runtime State")]
        [SerializeField] private bool isEndlessModeActive = false;
        [SerializeField] private int currentFloor = 1;
        [SerializeField] private int runGoldAccumulated = 0;
        [SerializeField] private int runGemsAccumulated = 0;

        public EndlessTrialConfigSO Config => config;
        public bool IsEndlessModeActive => isEndlessModeActive;
        public int CurrentFloor => currentFloor;
        public int HighestFloorReached => CitadelLobbyManager.Instance != null ? CitadelLobbyManager.Instance.HighestAbyssalFloor : 0;

        public static event Action OnEndlessTrialStarted;
        public static event Action<int> OnEndlessFloorStarted;      // (currentFloor)
        public static event Action<int> OnEndlessFloorCleared;      // (clearedFloor)
        public static event Action<int, int, int> OnEndlessTrialEnded; // (finalFloor, totalGold, totalGems)
        public static event Action<int> OnMilestoneRewardClaimed;   // (milestoneFloor)

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureDefaultConfig();
        }

        private void Start()
        {
            CheckWeeklyReset();
        }

        private void EnsureDefaultConfig()
        {
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<EndlessTrialConfigSO>();
            }
        }

        private void CheckWeeklyReset()
        {
            if (CitadelLobbyManager.Instance == null) return;

            long lastReset = CitadelLobbyManager.Instance.AccountData.lastWeeklyAbyssalResetUtc;
            long nowUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long sevenDaysSeconds = 7 * 24 * 3600;

            if (lastReset == 0 || (nowUtc - lastReset) >= sevenDaysSeconds)
            {
                CitadelLobbyManager.Instance.AccountData.ResetWeeklyAbyssalTrial();
                SaveManager.Instance?.SaveProfile(CitadelLobbyManager.Instance.AccountData);
                Debug.Log("[EndlessTrialManager] Siklus mingguan Abyssal Trial telah di-reset!");
            }
        }

        /// <summary>
        /// Memulai petualangan survival Endless Abyssal Trial dari Lantai 1.
        /// </summary>
        public void StartEndlessTrial()
        {
            if (CitadelLobbyManager.Instance != null && !CitadelLobbyManager.Instance.CanStartBattle())
            {
                Debug.LogWarning("[EndlessTrialManager] Energi tidak mencukupi untuk memulai Abyssal Trial!");
                return;
            }

            if (CitadelLobbyManager.Instance != null)
            {
                CitadelLobbyManager.Instance.ConsumeBattleEnergy();
            }

            isEndlessModeActive = true;
            currentFloor = 1;
            runGoldAccumulated = 0;
            runGemsAccumulated = 0;

            OnEndlessTrialStarted?.Invoke();
            OnEndlessFloorStarted?.Invoke(currentFloor);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.ButtonClick);
            }

            Debug.Log($"[EndlessTrialManager] Memulai Endless Abyssal Trial di Lantai {currentFloor}!");
        }

        /// <summary>
        /// Menyelesaikan lantai saat ini, memberikan hadiah akumulasi, dan maju ke lantai berikutnya.
        /// </summary>
        public void NotifyFloorCleared(int clearedFloor)
        {
            if (!isEndlessModeActive) return;

            // Berikan bonus akumulasi per lantai
            int floorGold = 100 + (clearedFloor * 25);
            int floorGems = 5 + (clearedFloor / 2);
            runGoldAccumulated += floorGold;
            runGemsAccumulated += floorGems;

            // Catat progres lantai tertinggi ke akun
            if (CitadelLobbyManager.Instance != null)
            {
                CitadelLobbyManager.Instance.RecordAbyssalFloorCleared(clearedFloor);
            }

            OnEndlessFloorCleared?.Invoke(clearedFloor);

            // Persiapan ke lantai berikutnya
            currentFloor = clearedFloor + 1;

            // Rotasi afiks cuaca dinamis setiap lantai di Endless Trial (Sprint 12)
            if (Combat.BattlefieldWeatherManager.Instance != null)
            {
                Combat.WeatherType nextWeather = (Combat.WeatherType)(currentFloor % 4);
                Combat.BattlefieldWeatherManager.Instance.SetWeather(nextWeather);
            }

            OnEndlessFloorStarted?.Invoke(currentFloor);

            Debug.Log($"[EndlessTrialManager] Lantai {clearedFloor} Takluk! Bergerak ke Lantai {currentFloor} (+{floorGold} Gold, +{floorGems} Gems)");
        }

        /// <summary>
        /// Mengakhiri tantangan Endless Trial saat benteng runtuh atau pertempuran usai.
        /// </summary>
        public void ConcludeEndlessTrial()
        {
            if (!isEndlessModeActive) return;

            isEndlessModeActive = false;

            // Masukkan total hadiah yang terkumpul ke akun
            if (CitadelLobbyManager.Instance != null)
            {
                CitadelLobbyManager.Instance.AddGold(runGoldAccumulated);
                CitadelLobbyManager.Instance.AddGems(runGemsAccumulated);
                CitadelLobbyManager.Instance.RecordAbyssalFloorCleared(currentFloor - 1);
            }

            OnEndlessTrialEnded?.Invoke(currentFloor - 1, runGoldAccumulated, runGemsAccumulated);
            Debug.Log($"[EndlessTrialManager] Abyssal Trial Selesai! Capaian Terakhir: Lantai {currentFloor - 1}. Total Hadiah: {runGoldAccumulated} Gold, {runGemsAccumulated} Gems.");
        }

        /// <summary>
        /// Mengklaim hadiah peti milestone mingguan berdasarkan lantai tertinggi yang telah dicapai.
        /// </summary>
        public bool TryClaimMilestone(int milestoneFloor)
        {
            if (config == null || CitadelLobbyManager.Instance == null) return false;

            var reward = config.GetMilestoneReward(milestoneFloor);
            if (reward == null) return false;

            bool success = CitadelLobbyManager.Instance.TryClaimAbyssalMilestone(
                milestoneFloor,
                reward.goldReward,
                reward.gemsReward,
                reward.forgeDustReward,
                reward.refineStoneReward
            );

            if (success)
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySound(SoundType.MergeSuccess);
                }
                OnMilestoneRewardClaimed?.Invoke(milestoneFloor);
                Debug.Log($"[EndlessTrialManager] Hadiah Milestone Lantai {milestoneFloor} ({reward.rewardDescription}) berhasil diklaim!");
            }

            return success;
        }

        /// <summary>
        /// Menghitung sisa hari menuju reset mingguan.
        /// </summary>
        public string GetWeeklyResetTimeRemaining()
        {
            if (CitadelLobbyManager.Instance == null) return "6 Hari";

            long lastReset = CitadelLobbyManager.Instance.AccountData.lastWeeklyAbyssalResetUtc;
            long nowUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long nextReset = lastReset + (7 * 24 * 3600);
            long diff = Math.Max(0L, nextReset - nowUtc);

            int days = (int)(diff / (24 * 3600));
            int hours = (int)((diff % (24 * 3600)) / 3600);

            return $"{days}h {hours}j lagi";
        }
    }
}
