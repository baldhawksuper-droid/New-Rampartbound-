using System;
using System.Collections.Generic;
using UnityEngine;
using FantasyDefense.Audio;
using FantasyDefense.Core;
using FantasyDefense.Data;
using FantasyDefense.GridSystem;

namespace FantasyDefense.Combat
{
    /// <summary>
    /// Asisten Otomatis Fusi Benteng (Citadel Auto-Merge Assistant).
    /// Memindai papan arena secara berkala dan menggabungkan pahlawan sejenis & setara tier
    /// dengan algoritma cerdas yang memprioritaskan unit di barisan terdepan.
    /// Memberikan kenyamanan bermain (Quality of Life) untuk pemain mobile.
    /// </summary>
    public class AutoMergeAssistant : MonoBehaviour
    {
        public static AutoMergeAssistant Instance { get; private set; }

        [Header("Configuration")]
        [Tooltip("Interval pengecekan dan eksekusi fusi otomatis (detik)")]
        [SerializeField] private float mergeInterval = 1.25f;

        [Tooltip("Prioritaskan unit di barisan terdepan (koordinat Y lebih tinggi) sebagai target hasil fusi")]
        [SerializeField] private bool prioritizeFrontline = true;

        [Header("Runtime State")]
        [SerializeField] private bool isAutoMergeEnabled = false;
        private float scanTimer = 0f;

        public bool IsAutoMergeEnabled => isAutoMergeEnabled;

        public static event Action<bool> OnAutoMergeToggled;
        public static event Action<UnitController, UnitController, UnitTier> OnAutoMergeExecuted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (!isAutoMergeEnabled) return;

            // Pastikan pertempuran sedang aktif atau dalam persiapan taktis
            if (GameManager.Instance != null)
            {
                var state = GameManager.Instance.CurrentBattleState;
                if (state != BattleState.WaveInProgress && state != BattleState.Preparation)
                {
                    return;
                }
            }

            scanTimer += Time.deltaTime;
            if (scanTimer >= mergeInterval)
            {
                scanTimer = 0f;
                TryExecuteSingleAutoMerge();
            }
        }

        /// <summary>
        /// Mengaktifkan atau menonaktifkan asisten fusi otomatis.
        /// </summary>
        public void ToggleAutoMerge()
        {
            SetAutoMerge(!isAutoMergeEnabled);
        }

        public void SetAutoMerge(bool enabled)
        {
            isAutoMergeEnabled = enabled;
            scanTimer = 0f;
            Debug.Log($"[AutoMergeAssistant] Auto-Merge Assistant: {(isAutoMergeEnabled ? "AKTIF (ON)" : "NONAKTIF (OFF)")}");

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.ButtonClick);
            }

            OnAutoMergeToggled?.Invoke(isAutoMergeEnabled);
        }

        /// <summary>
        /// Memindai grid arena dan mengeksekusi satu fusi terbaik yang valid.
        /// </summary>
        public bool TryExecuteSingleAutoMerge()
        {
            if (GridManager.Instance == null || !GridManager.Instance.IsInitialized) return false;

            // Kumpulkan semua unit yang sedang menempati grid
            List<UnitController> activeUnits = new List<UnitController>();
            int width = GridManager.Instance.GridWidth;
            int height = GridManager.Instance.GridHeight;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    TileCell cell = GridManager.Instance.GetCell(x, y);
                    if (cell != null && cell.IsOccupied && cell.CurrentUnit != null)
                    {
                        UnitController unit = cell.CurrentUnit;
                        // Hanya pertimbangkan unit di bawah Tier 5 dan tidak sedang di-drag oleh pemain
                        if (unit.CurrentTier < UnitTier.Tier5 && !unit.IsDragging)
                        {
                            activeUnits.Add(unit);
                        }
                    }
                }
            }

            if (activeUnits.Count < 2) return false;

            // Cari pasangan terbaik (tipe sama, tier sama)
            UnitController bestSource = null;
            UnitController bestTarget = null;
            float highestPriority = float.MinValue;

            for (int i = 0; i < activeUnits.Count; i++)
            {
                for (int j = i + 1; j < activeUnits.Count; j++)
                {
                    UnitController uA = activeUnits[i];
                    UnitController uB = activeUnits[j];

                    if (uA.CanMergeWith(uB))
                    {
                        // Tentukan target yang berada di barisan lebih depan
                        UnitController target;
                        UnitController source;

                        if (prioritizeFrontline)
                        {
                            int yA = uA.CurrentCell != null ? uA.CurrentCell.Y : 0;
                            int yB = uB.CurrentCell != null ? uB.CurrentCell.Y : 0;

                            if (yA >= yB)
                            {
                                target = uA;
                                source = uB;
                            }
                            else
                            {
                                target = uB;
                                source = uA;
                            }
                        }
                        else
                        {
                            target = uA;
                            source = uB;
                        }

                        // Prioritaskan fusi Tier 2 -> Tier 3 terlebih dahulu, baru Tier 1 -> Tier 2
                        float priority = ((int)target.CurrentTier * 100f) + (target.CurrentCell != null ? target.CurrentCell.Y * 10f : 0f);

                        if (priority > highestPriority)
                        {
                            highestPriority = priority;
                            bestSource = source;
                            bestTarget = target;
                        }
                    }
                }
            }

            if (bestSource != null && bestTarget != null)
            {
                UnitTier resultingTier = (UnitTier)((int)bestTarget.CurrentTier + 1);
                string heroName = bestTarget.UnitData != null ? bestTarget.UnitData.heroName : "Hero";

                bool success = bestSource.MergeInto(bestTarget);
                if (success)
                {
                    Debug.Log($"[AutoMergeAssistant] Smart Auto-Merge Berhasil! {heroName} berevolusi ke Tier {(int)resultingTier}.");
                    
                    // Live-Ops Telemetry Tracking
                    Analytics.CitadelTelemetryManager.Instance?.TrackHeroMerge(heroName, (int)resultingTier - 1, (int)resultingTier, true);

                    OnAutoMergeExecuted?.Invoke(bestSource, bestTarget, resultingTier);
                    return true;
                }
            }

            return false;
        }
    }
}
