using System;
using UnityEngine;
using FantasyDefense.Wave;

namespace FantasyDefense.Combat
{
    /// <summary>
    /// CompanionManager mengelola sistem pendukung pasif kanonikal sesuai Dokumen Spesifikasi Induk:
    /// 1. Ignis, The Pyre Owl Sentinel: Burung hantu lentera api suci (+15% attack speed meriam benteng).
    /// 2. Barnaby "Barney", The Bulwark Porter: Landak pembawa amunisi (+10 HP Tembok Benteng setiap wave bersih).
    /// </summary>
    public class CompanionManager : MonoBehaviour
    {
        public static CompanionManager Instance { get; private set; }

        [Header("Ignis The Pyre Owl (Turret Attack Speed Booster)")]
        [SerializeField] private bool isIgnisUnlocked = false; // Disimpan / nonaktif sementara sesuai arahan pemain
        [SerializeField] private float ignisCannonSpeedBoostPercent = 0.15f; // +15%

        [Header("Barnaby The Bulwark Porter (Wave Barricade Repairer)")]
        [SerializeField] private bool isBarnabyUnlocked = false; // Disimpan / nonaktif sementara sesuai arahan pemain
        [SerializeField] private int barnabyWaveRepairAmount = 10; // +10 HP

        public bool IsIgnisUnlocked => isIgnisUnlocked;
        public float IgnisCannonSpeedBoostPercent => isIgnisUnlocked ? ignisCannonSpeedBoostPercent : 0f;
        public bool IsBarnabyUnlocked => isBarnabyUnlocked;
        public int BarnabyWaveRepairAmount => isBarnabyUnlocked ? barnabyWaveRepairAmount : 0;

        public static event Action<string, string> OnCompanionBuffTriggered; // (companionName, buffDescription)

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            WaveSpawner.OnWaveCompleted += HandleWaveCompleted;
        }

        private void OnDisable()
        {
            WaveSpawner.OnWaveCompleted -= HandleWaveCompleted;
        }

        private void HandleWaveCompleted(int completedWave)
        {
            if (!isBarnabyUnlocked) return;

            if (CastleBarricade.Instance != null && CastleBarricade.Instance.CurrentHealth > 0)
            {
                CastleBarricade.Instance.Repair(barnabyWaveRepairAmount);
                OnCompanionBuffTriggered?.Invoke("Barnaby", $"+{barnabyWaveRepairAmount} HP Benteng Dipulihkan");
                Debug.Log($"[CompanionManager] Barnaby memulihkan +{barnabyWaveRepairAmount} HP benteng di akhir Wave {completedWave}!");
            }
        }

        /// <summary>
        /// Menghitung interval tembak meriam setelah dipengaruhi buff Ignis.
        /// </summary>
        public float GetAdjustedCannonInterval(float baseInterval)
        {
            if (!isIgnisUnlocked) return baseInterval;
            // Interval berkurang seiring naiknya attack speed: interval / 1.15f
            return baseInterval / (1f + ignisCannonSpeedBoostPercent);
        }
    }
}
