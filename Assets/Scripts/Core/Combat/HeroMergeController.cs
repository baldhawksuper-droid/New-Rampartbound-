using System;
using UnityEngine;
using FantasyDefense.Core;
using FantasyDefense.Data;
using FantasyDefense.GridSystem;
using FantasyDefense.Mobile;

namespace FantasyDefense.Combat
{
    /// <summary>
    /// Mengelola statistik fusi, combo merge, efek visual fusi, dan respons haptic mobile.
    /// Memastikan kepuasan game feel (juicy tactile feedback) di perangkat smartphone dan tablet.
    /// </summary>
    public class HeroMergeController : MonoBehaviour
    {
        public static HeroMergeController Instance { get; private set; }

        [Header("Combo Configuration")]
        [Tooltip("Jeda waktu maksimal antar-merge untuk mempertahankan rantai combo (detik)")]
        [SerializeField] private float comboWindowDuration = 4.0f;

        [Header("Haptic & Game Feel (Mobile / Tablet)")]
        [Tooltip("Aktifkan getaran ringan ponsel saat berhasil menggabungkan hero")]
        [SerializeField] private bool enableHapticFeedback = true;

        [Header("VFX Prefab References (Optional)")]
        [SerializeField] private GameObject mergeTier2VfxPrefab;
        [SerializeField] private GameObject mergeTier3VfxPrefab;

        [Header("Runtime Statistics")]
        [SerializeField] private int totalMergesCount = 0;
        [SerializeField] private int tier2CreatedCount = 0;
        [SerializeField] private int tier3CreatedCount = 0;
        [SerializeField] private int currentCombo = 0;

        private float comboTimer = 0f;

        public int TotalMergesCount => totalMergesCount;
        public int Tier2CreatedCount => tier2CreatedCount;
        public int Tier3CreatedCount => tier3CreatedCount;
        public int CurrentCombo => currentCombo;

        public static event Action<int, UnitTier> OnMergeSuccess; // (combo, achievedTier)
        public static event Action<int> OnComboUpdated; // (currentCombo)
        public static event Action OnComboReset;

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
            UnitController.OnUnitMerged += HandleUnitMerged;
        }

        private void OnDisable()
        {
            UnitController.OnUnitMerged -= HandleUnitMerged;
        }

        private void Update()
        {
            if (currentCombo > 0)
            {
                comboTimer -= Time.deltaTime;
                if (comboTimer <= 0f)
                {
                    currentCombo = 0;
                    OnComboReset?.Invoke();
                }
            }
        }

        private void HandleUnitMerged(UnitController source, UnitController target, UnitTier newTier)
        {
            totalMergesCount++;
            currentCombo++;
            comboTimer = comboWindowDuration;

            if (newTier == UnitTier.Tier2)
                tier2CreatedCount++;
            else if (newTier == UnitTier.Tier3)
                tier3CreatedCount++;
            else if (newTier >= UnitTier.Tier4)
                tier3CreatedCount++; // counts as ultimate tier creation

            // 1. Mobile Haptic Vibration via DevicePerformanceManager
            TriggerMobileHaptic(newTier);

            // 2. Camera Screen Shake (Point 6: subtle screen shake for Tier 2 and Tier 3+)
            if (MobileCameraAdapter.Instance != null)
            {
                float shakeDuration = newTier == UnitTier.Tier5 ? 0.35f : (newTier == UnitTier.Tier4 ? 0.28f : (newTier == UnitTier.Tier3 ? 0.24f : 0.14f));
                float shakeMagnitude = newTier == UnitTier.Tier5 ? 0.30f : (newTier == UnitTier.Tier4 ? 0.24f : (newTier == UnitTier.Tier3 ? 0.20f : 0.10f));
                MobileCameraAdapter.Instance.TriggerScreenShake(shakeDuration, shakeMagnitude);
            }

            // 3. Spawn Particle VFX jika prefab tersedia
            SpawnMergeVfx(target.transform.position, newTier);

            // Live-Ops Telemetry Tracking
            if (FantasyDefense.Analytics.CitadelTelemetryManager.Instance != null)
            {
                string heroName = (target != null && target.HeroData != null) ? target.HeroData.heroName : "Hero";
                FantasyDefense.Analytics.CitadelTelemetryManager.Instance.TrackHeroMerge(heroName, (int)newTier - 1, (int)newTier, false);
            }

            Debug.Log($"[HeroMergeController] Merge Berhasil! Unit berevolusi ke Tier {(int)newTier}. Combo: {currentCombo}x");

            OnMergeSuccess?.Invoke(currentCombo, newTier);
            OnComboUpdated?.Invoke(currentCombo);
        }

        private void TriggerMobileHaptic(UnitTier tier)
        {
            if (!enableHapticFeedback) return;

            if (DevicePerformanceManager.Instance != null)
            {
                DevicePerformanceManager.Instance.TriggerHaptic(tier >= UnitTier.Tier3 ? HapticFeedbackType.Heavy : HapticFeedbackType.Success);
                return;
            }

#if UNITY_ANDROID || UNITY_IOS
            try
            {
                // Handheld.Vibrate memberikan respon getaran haptic standar di perangkat Android/iOS
                Handheld.Vibrate();
            }
            catch (Exception)
            {
                // Fallback aman jika berjalan di emulator atau perangkat tanpa motor vibrasi
            }
#endif
        }

        private void SpawnMergeVfx(Vector3 worldPos, UnitTier tier)
        {
            GameObject vfxPrefab = tier >= UnitTier.Tier3 ? mergeTier3VfxPrefab : mergeTier2VfxPrefab;
            if (vfxPrefab != null)
            {
                GameObject vfx = Instantiate(vfxPrefab, worldPos, Quaternion.identity);
                Destroy(vfx, 2.5f);
            }
        }

        public void ResetStatistics()
        {
            totalMergesCount = 0;
            tier2CreatedCount = 0;
            tier3CreatedCount = 0;
            currentCombo = 0;
            comboTimer = 0f;
        }
    }
}
