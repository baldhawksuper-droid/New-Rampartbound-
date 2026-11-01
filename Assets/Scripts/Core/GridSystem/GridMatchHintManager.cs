using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FantasyDefense.Combat;
using FantasyDefense.Core;
using FantasyDefense.Data;
using FantasyDefense.Localization;
using FantasyDefense.Metagame;

namespace FantasyDefense.GridSystem
{
    /// <summary>
    /// Pengelola petunjuk otomatis (Idle Match Hint System) untuk fase persiapan (Puzzle Preparation).
    /// Jika pemain tidak melakukan sentuhan di layar selama beberapa detik (default 4 detik),
    /// sistem memindai papan 5x5 untuk mencari sepasang bidak yang dapat ditukar dan menghasilkan Match-3 atau Merge.
    /// Pasangan bidak tersebut akan berdenyut lembut (soft pulse glow) untuk membimbing pemain.
    /// Dilengkapi sistem Onboarding Sentuhan Pertama khusus Level 1-1 agar pemain baru langsung memahami alur fusi.
    /// Begitu ada sentuhan atau aksi pemain, efek petunjuk langsung berhenti seketika.
    /// </summary>
    public class GridMatchHintManager : MonoBehaviour
    {
        public static GridMatchHintManager Instance { get; private set; }

        [Header("Idle Hint Settings")]
        [Tooltip("Durasi diam tanpa input pemain sebelum petunjuk ditampilkan (detik)")]
        [SerializeField] private float idleDurationBeforeHint = 4.0f;

        [Tooltip("Kecepatan denyut visual petunjuk")]
        [SerializeField] private float hintPulseSpeed = 4.5f;

        [Tooltip("Besar pembesaran denyut maksimum (skala 1.0 s/d 1.12)")]
        [SerializeField] private float hintPulseScaleMultiplier = 1.12f;

        [Header("Level 1-1 First-Touch Onboarding")]
        [Tooltip("Bimbingan visual awal sentuhan pertama di Level 1-1 (Sprint 43)")]
        [SerializeField] private bool enableFirstTouchOnboarding = true;
        private bool hasCompletedFirstTouch = false;
        private Coroutine onboardingCheckRoutine = null;

        [Header("State Tracking")]
        [SerializeField] private bool isHintActive = false;
        private float lastActivityTime = 0f;
        private Coroutine hintRoutine = null;

        private UnitController hintedUnitA = null;
        private UnitController hintedUnitB = null;
        private Vector3 originalScaleA = Vector3.one;
        private Vector3 originalScaleB = Vector3.one;

        public bool IsHintActive => isHintActive;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            lastActivityTime = Time.time;
        }

        private void Start()
        {
            GridManager.OnGridGenerated += HandleGridGenerated;
            CheckTriggerFirstTouchOnboarding();
        }

        private void OnDestroy()
        {
            GridManager.OnGridGenerated -= HandleGridGenerated;
        }

        private void OnEnable()
        {
            UnitController.OnUnitDragStarted += HandlePlayerActivity;
            UnitController.OnUnitMoved += HandleUnitMoved;
            UnitController.OnUnitMerged += HandleUnitMerged;
            TileCell.OnTileClicked += HandleCellClicked;
            lastActivityTime = Time.time;
        }

        private void OnDisable()
        {
            UnitController.OnUnitDragStarted -= HandlePlayerActivity;
            UnitController.OnUnitMoved -= HandleUnitMoved;
            UnitController.OnUnitMerged -= HandleUnitMerged;
            TileCell.OnTileClicked -= HandleCellClicked;
            StopActiveHint();
        }

        private void HandleGridGenerated()
        {
            CheckTriggerFirstTouchOnboarding();
        }

        private void CheckTriggerFirstTouchOnboarding()
        {
            if (!enableFirstTouchOnboarding || hasCompletedFirstTouch) return;

            if (onboardingCheckRoutine != null)
            {
                StopCoroutine(onboardingCheckRoutine);
            }
            onboardingCheckRoutine = StartCoroutine(InitialOnboardingRoutine());
        }

        private IEnumerator InitialOnboardingRoutine()
        {
            // Beri jeda 0.8s agar susunan grid stabil dan visual unit telah terpasang
            yield return new WaitForSeconds(0.85f);

            if (hasCompletedFirstTouch) yield break;

            // Pastikan ini adalah Stage 1-1 dan dalam fase persiapan Puzzle
            var currentStage = StageManager.Instance != null ? StageManager.Instance.GetSelectedStage() : null;
            bool isStage1 = currentStage == null || currentStage.stageId == 101 || currentStage.stageNumber == 1;
            bool isPrepMode = GameManager.Instance != null &&
                              GameManager.Instance.CurrentBattleMode == BattleMode.PuzzlePreparation &&
                              GameManager.Instance.CurrentState == BattleState.Preparation;

            if (isStage1 && isPrepMode && !isHintActive)
            {
                if (FindPossibleMergePair(out TileCell cellA, out TileCell cellB) || FindPossibleMatchPair(out cellA, out cellB))
                {
                    if (cellA != null && cellB != null && cellA.CurrentUnit != null && cellB.CurrentUnit != null)
                    {
                        StartHintAnimation(cellA.CurrentUnit, cellB.CurrentUnit);

                        // Tampilkan teks bimbingan mengambang lembut di atas kedua permata
                        Vector3 midPoint = (cellA.transform.position + cellB.transform.position) * 0.5f;
                        if (SkillVfxManager.Instance != null)
                        {
                            string onboardingText = CitadelLocalizationManager.Text("👆 GESER & SATUKAN PERMATA!", "👆 DRAG & MERGE GEMS!");
                            SkillVfxManager.Instance.SpawnFloatingText(midPoint + Vector3.up * 0.45f, onboardingText, new Color(1f, 0.95f, 0.35f), 3.0f, false);
                        }
                    }
                }
            }
        }

        private void HandlePlayerActivity(UnitController unit)
        {
            hasCompletedFirstTouch = true;
            ResetHintTimer();
        }

        private void HandleUnitMoved(UnitController unit, TileCell cell)
        {
            hasCompletedFirstTouch = true;
            ResetHintTimer();
        }

        private void HandleUnitMerged(UnitController source, UnitController target, UnitTier tier)
        {
            hasCompletedFirstTouch = true;
            ResetHintTimer();
        }

        private void HandleCellClicked(TileCell cell)
        {
            hasCompletedFirstTouch = true;
            ResetHintTimer();
        }

        private void Update()
        {
            // Hanya aktif di Mode Puzzle Preparation selama fase persiapan
            if (GameManager.Instance == null ||
                GameManager.Instance.CurrentBattleMode != BattleMode.PuzzlePreparation ||
                GameManager.Instance.CurrentState != BattleState.Preparation)
            {
                if (isHintActive) StopActiveHint();
                return;
            }

            // Jika papan sedang menyelesaikan fusi berantai atau cascade, tunda hitungan
            if (GridMatch3Solver.Instance != null && GridMatch3Solver.Instance.IsResolving)
            {
                ResetHintTimer();
                return;
            }

            // Jika sedang ada animasi swap berlangsung, tunda hitungan
            if (GridInputHandler.Instance != null && GridInputHandler.Instance.IsSwapping)
            {
                ResetHintTimer();
                return;
            }

            // Jika pemain sedang menyentuh layar / mouse ditekan, reset timer
            if (Input.touchCount > 0 || Input.GetMouseButton(0))
            {
                ResetHintTimer();
                return;
            }

            // Evaluasi durasi idle
            if (!isHintActive && (Time.time - lastActivityTime >= idleDurationBeforeHint))
            {
                TryTriggerHint();
            }
        }

        /// <summary>
        /// Mereset pencatat waktu aktivitas pemain dan menghentikan petunjuk visual jika sedang menyala.
        /// </summary>
        public void ResetHintTimer()
        {
            lastActivityTime = Time.time;
            if (isHintActive)
            {
                StopActiveHint();
            }
        }

        /// <summary>
        /// Memindai papan petak 5x5 untuk mencari pasangan bidak yang dapat ditukar dan menghasilkan Match-3.
        /// </summary>
        public bool FindPossibleMatchPair(out TileCell cellA, out TileCell cellB)
        {
            cellA = null;
            cellB = null;

            if (GridManager.Instance == null || !GridManager.Instance.IsInitialized)
                return false;

            int width = GridManager.Instance.GridWidth;
            int height = GridManager.Instance.GridHeight;

            // 1. Cek seluruh pasangan horizontal bersebelahan: (x, y) dan (x + 1, y)
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width - 1; x++)
                {
                    TileCell c1 = GridManager.Instance.GetCell(x, y);
                    TileCell c2 = GridManager.Instance.GetCell(x + 1, y);

                    if (c1 != null && c2 != null && c1.IsOccupied && c2.IsOccupied)
                    {
                        if (GridMatch3Solver.Instance != null && GridMatch3Solver.Instance.SimulateSwapAndCheckMatch(c1, c2))
                        {
                            cellA = c1;
                            cellB = c2;
                            return true;
                        }
                    }
                }
            }

            // 2. Cek seluruh pasangan vertikal bersebelahan: (x, y) dan (x, y + 1)
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height - 1; y++)
                {
                    TileCell c1 = GridManager.Instance.GetCell(x, y);
                    TileCell c2 = GridManager.Instance.GetCell(x, y + 1);

                    if (c1 != null && c2 != null && c1.IsOccupied && c2.IsOccupied)
                    {
                        if (GridMatch3Solver.Instance != null && GridMatch3Solver.Instance.SimulateSwapAndCheckMatch(c1, c2))
                        {
                            cellA = c1;
                            cellB = c2;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Memindai petak untuk mencari sepasang bidak bersebelahan yang dapat digabung (merge) langsung.
        /// Prioritas utama diarahkan pada baris depan (y lebih tinggi) agar pemain melihat pertahanan aktif terdekat.
        /// </summary>
        public bool FindPossibleMergePair(out TileCell cellA, out TileCell cellB)
        {
            cellA = null;
            cellB = null;

            if (GridManager.Instance == null || !GridManager.Instance.IsInitialized)
                return false;

            int width = GridManager.Instance.GridWidth;
            int height = GridManager.Instance.GridHeight;

            for (int y = height - 1; y >= 0; y--)
            {
                for (int x = 0; x < width; x++)
                {
                    TileCell c1 = GridManager.Instance.GetCell(x, y);
                    if (c1 == null || !c1.IsOccupied || c1.CurrentUnit == null) continue;

                    // Cek tetangga kanan
                    if (x < width - 1)
                    {
                        TileCell c2 = GridManager.Instance.GetCell(x + 1, y);
                        if (c2 != null && c2.IsOccupied && c2.CurrentUnit != null)
                        {
                            if (c1.CurrentUnit.CanMergeWith(c2.CurrentUnit))
                            {
                                cellA = c1;
                                cellB = c2;
                                return true;
                            }
                        }
                    }

                    // Cek tetangga bawah
                    if (y > 0)
                    {
                        TileCell c2 = GridManager.Instance.GetCell(x, y - 1);
                        if (c2 != null && c2.IsOccupied && c2.CurrentUnit != null)
                        {
                            if (c1.CurrentUnit.CanMergeWith(c2.CurrentUnit))
                            {
                                cellA = c1;
                                cellB = c2;
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        private void TryTriggerHint()
        {
            if (FindPossibleMergePair(out TileCell cellA, out TileCell cellB) || FindPossibleMatchPair(out cellA, out cellB))
            {
                if (cellA != null && cellB != null && cellA.CurrentUnit != null && cellB.CurrentUnit != null)
                {
                    StartHintAnimation(cellA.CurrentUnit, cellB.CurrentUnit);
                }
            }
            else
            {
                // Tidak ada match atau merge yang mungkin terbentuk, tunda pengecekan kembali
                lastActivityTime = Time.time;
            }
        }

        private void StartHintAnimation(UnitController unitA, UnitController unitB)
        {
            if (unitA == null || unitB == null) return;

            StopActiveHint();

            hintedUnitA = unitA;
            hintedUnitB = unitB;
            originalScaleA = unitA.transform.localScale;
            originalScaleB = unitB.transform.localScale;

            isHintActive = true;
            hintRoutine = StartCoroutine(PulseHintRoutine(unitA, unitB, originalScaleA, originalScaleB));
            Debug.Log($"[GridMatchHintManager] Petunjuk Match Aktif pada pasangan [{unitA.CurrentCell?.X}, {unitA.CurrentCell?.Y}] dan [{unitB.CurrentCell?.X}, {unitB.CurrentCell?.Y}]");
        }

        private IEnumerator PulseHintRoutine(UnitController unitA, UnitController unitB, Vector3 baseScaleA, Vector3 baseScaleB)
        {
            while (true)
            {
                if (unitA == null || unitB == null)
                {
                    StopActiveHint();
                    yield break;
                }

                // Denyut halus taktil tanpa distorsi ukuran ekstrem (maksimum 1.025x)
                float wave = (Mathf.Sin(Time.time * hintPulseSpeed) + 1f) * 0.5f;
                float scaleMod = Mathf.Lerp(1.0f, 1.025f, wave);

                unitA.transform.localScale = baseScaleA * scaleMod;
                unitB.transform.localScale = baseScaleB * scaleMod;

                yield return null;
            }
        }

        private void StopActiveHint()
        {
            if (hintRoutine != null)
            {
                StopCoroutine(hintRoutine);
                hintRoutine = null;
            }

            if (hintedUnitA != null)
            {
                hintedUnitA.transform.localScale = originalScaleA;
                hintedUnitA = null;
            }

            if (hintedUnitB != null)
            {
                hintedUnitB.transform.localScale = originalScaleB;
                hintedUnitB = null;
            }

            isHintActive = false;
        }
    }
}
