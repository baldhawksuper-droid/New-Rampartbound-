using System;
using System.Collections.Generic;
using UnityEngine;
using FantasyDefense.Combat;
using FantasyDefense.Data;
using FantasyDefense.Economy;
using FantasyDefense.GridSystem;
using FantasyDefense.Summon;
using FantasyDefense.UI;

namespace FantasyDefense.Core
{
    public enum BattleState
    {
        Initializing,
        Preparation,
        WaveActive,
        WaveInProgress = WaveActive,
        CombatActive = WaveActive,
        WaveClear,
        Victory,
        Defeat
    }

    /// <summary>
    /// Master Orchestrator untuk alur battle arena Fantasy Citadel Defense.
    /// Menginisialisasi Grid, Ekonomi Gems, Kamera Mobile, dan Sistem Pemanggilan Pahlawan.
    /// Dirancang dengan arsitektur decoupling bersih untuk kemudahan pengujian di Unity Editor maupun build Mobile.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Level Data Configuration")]
        [SerializeField] private LevelConfigSO activeLevelConfig;

        [Header("Hero Data Pool")]
        [SerializeField] private List<UnitDataSO> starterHeroPool = new List<UnitDataSO>();

        [Header("Initial Setup Options")]
        [Tooltip("Jumlah hero acak yang otomatis dipanggil saat battle dimulai")]
        [SerializeField] private int initialFreeSummons = 3;

        [Header("Current Battle State & Mode")]
        [SerializeField] private BattleState currentState = BattleState.Initializing;
        [SerializeField] private BattleMode currentBattleMode = BattleMode.PuzzlePreparation;
        [SerializeField] private int movesRemaining = 15;

        public BattleState CurrentState => currentState;
        public BattleState CurrentBattleState => currentState;
        public BattleMode CurrentBattleMode => currentBattleMode;
        public int MovesRemaining => movesRemaining;
        public LevelConfigSO ActiveLevelConfig => activeLevelConfig;

        public static event Action<BattleState> OnBattleStateChanged;
        public static event Action<BattleMode> OnBattleModeSelected;
        public static event Action<int> OnMovesRemainingChanged;
        public static event Action<int, int> OnInterWavePreparationStarted; // (nextWaveNumber, bonusMoves)

        private bool isFirstWavePreparation = true;
        public bool IsFirstWavePreparation => isFirstWavePreparation;

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
            HeroSummoner.OnHeroSummoned += HandleHeroSummonedForMove;
        }

        private void OnDisable()
        {
            HeroSummoner.OnHeroSummoned -= HandleHeroSummonedForMove;
        }

        private void HandleHeroSummonedForMove(UnitController u, TileCell c)
        {
            if (currentState == BattleState.Preparation)
            {
                TryConsumeMove();
            }
        }

        [Tooltip("Jika aktif, battle arena langsung diinisialisasi saat Start() (berguna untuk testing mandiri)")]
        [SerializeField] private bool autoStartBattleOnAwake = false;

        private void Start()
        {
            if (autoStartBattleOnAwake)
            {
                InitializeBattle();
            }
        }

        /// <summary>
        /// Memulai pertempuran arena baru dengan memilih stage tertentu (opsional).
        /// </summary>
        public void StartBattleWithStage(StageDataSO stage = null)
        {
            if (stage != null && Metagame.StageManager.Instance != null)
            {
                Metagame.StageManager.Instance.TrySelectStage(stage.stageId);
            }
            InitializeBattle();
        }

        /// <summary>
        /// Mengakhiri sesi pertempuran dan membersihkan seluruh token hero & monster dari arena.
        /// </summary>
        public void ReturnToLobby()
        {
            StopAllCoroutines();
            if (Wave.WaveSpawner.Instance != null)
            {
                Wave.WaveSpawner.Instance.StopAndClearAllWaves();
            }
            if (GridManager.Instance != null)
            {
                GridManager.Instance.ClearGrid();
            }
            SetState(BattleState.Initializing);
        }

        /// <summary>
        /// Menginisialisasi seluruh subsistem battle secara berurutan.
        /// </summary>
        [Header("Wave Progression Options")]
        [Tooltip("Otomatis memulai wave pertama setelah jeda persiapan (detik)")]
        [SerializeField] private bool autoStartWavesAfterPreparation = true;
        [SerializeField] private float preparationDuration = 4.0f;

        public void InitializeBattle()
        {
            SetState(BattleState.Initializing);

            // 1. Konsumsi energi pertempuran dari Lobby (jika aktif)
            if (Metagame.CitadelLobbyManager.Instance != null)
            {
                Metagame.CitadelLobbyManager.Instance.ConsumeBattleEnergy();
            }

            // Dapatkan konfigurasi level dari Stage aktif jika tersedia
            LevelConfigSO configToUse = activeLevelConfig;
            if (Metagame.StageManager.Instance != null)
            {
                var stage = Metagame.StageManager.Instance.GetSelectedStage();
                if (stage != null)
                {
                    configToUse = stage.GetEffectiveLevelConfig();
                    if (Combat.BattlefieldWeatherManager.Instance != null)
                    {
                        Combat.BattlefieldWeatherManager.Instance.SetWeather(stage.stageWeather);
                    }
                    if (Combat.BattlefieldEnvironmentManager.Instance != null)
                    {
                        Combat.BattlefieldEnvironmentManager.Instance.SetEnvironmentTheme(stage.environmentTheme);
                    }
                }
            }

            // Self-healing fallback: Pastikan configToUse TIDAK PERNAH NULL
            if (configToUse == null)
            {
                configToUse = Resources.Load<LevelConfigSO>("Levels/SO_Level1_Config");
                if (configToUse == null)
                {
                    configToUse = ScriptableObject.CreateInstance<LevelConfigSO>();
                    configToUse.gridWidth = 5;
                    configToUse.gridHeight = 5;
                    configToUse.battleMode = BattleMode.PuzzlePreparation;
                    configToUse.preparationMoves = 15;
                    configToUse.citadelMaxHealth = 100;
                    configToUse.startingGems = 100;
                }
                activeLevelConfig = configToUse;
                Debug.LogWarning("[GameManager] configToUse bernilai null, fallback konfigurasi Level 1 berhasil dibuat!");
            }

            if (configToUse != null)
            {
                currentBattleMode = configToUse.battleMode;
                movesRemaining = configToUse.preparationMoves;
            }

            OnBattleModeSelected?.Invoke(currentBattleMode);
            OnMovesRemainingChanged?.Invoke(movesRemaining);

            // 2. Inisialisasi Mobile Touch & Camera Adapter
            if (MobileTouchManager.Instance != null)
            {
                MobileTouchManager.Instance.EnsureCameraRaycaster();
            }

            // 3. Inisialisasi Grid Arena
            var grid = GridManager.Instance ?? FindAnyObjectByType<GridManager>(FindObjectsInactive.Include);
            if (grid != null && configToUse != null)
            {
                grid.GenerateGrid(configToUse);
            }
            else
            {
                Debug.LogWarning("[GameManager] GridManager tidak ditemukan di scene saat InitializeBattle!");
            }

            // 4. Inisialisasi Ekonomi Gems
            if (EconomyManager.Instance != null && configToUse != null)
            {
                EconomyManager.Instance.Initialize(configToUse);
            }

            // 5. Inisialisasi Castle Barricade Health
            if (CastleBarricade.Instance != null && configToUse != null)
            {
                CastleBarricade.Instance.Initialize(configToUse.castleMaxHealth);
            }

            // 6. Daftarkan Roster Hero ke HeroSummoner dan sinkronkan dengan Deployed Formation dari Lobby
            if (HeroSummoner.Instance != null && starterHeroPool != null)
            {
                foreach (var hero in starterHeroPool)
                {
                    HeroSummoner.Instance.RegisterHeroData(hero);
                }

                if (FantasyDefense.Metagame.CitadelLobbyManager.Instance != null)
                {
                    var deployed = FantasyDefense.Metagame.CitadelLobbyManager.Instance.GetDeployedFormation();
                    if (deployed != null && deployed.Count > 0)
                    {
                        HeroSummoner.Instance.SynchronizeWithFormation(deployed);
                    }
                }
            }

            // 7. Panggil Starter Heroes gratis (Mode 2) atau isi penuh arena dengan token pahlawan (Mode 1 Puzzle)
            if (currentBattleMode == BattleMode.PuzzlePreparation)
            {
                PopulateGridForPuzzleMode();
                // Self-healing guardrail: Jika ActiveUnits masih kosong, panggil RefillEmptyCells agar arena tidak pernah blank
                if (UnitController.ActiveUnits.Count == 0 && GridMatch3Solver.Instance != null)
                {
                    GridMatch3Solver.Instance.RefillEmptyCells();
                }
            }
            else
            {
                SpawnInitialHeroes();
            }

            isFirstWavePreparation = true;
            SetState(BattleState.Preparation);

            // Inisialisasi & Buka Pemilihan Berkah Roguelike Awal (Wave 1)
            if (BattleBlessingManager.Instance != null)
            {
                BattleBlessingManager.Instance.ResetBattleSession();
            }
            if (UIBattleBlessingSelectionView.Instance != null)
            {
                UIBattleBlessingSelectionView.Instance.OpenSelection(1);
            }
            Debug.Log($"[GameManager] Battle berhasil diinisialisasi untuk Mobile & Tablet dalam Mode {currentBattleMode}.");

            if (currentBattleMode == BattleMode.RealTimeDeploy && autoStartWavesAfterPreparation)
            {
                StartCoroutine(StartWaveCountdownRoutine());
            }
        }

        /// <summary>
        /// Memasuki fase persiapan taktis antar-gelombang (Inter-Wave Preparation).
        /// Memberikan langkah taktis tambahan kepada pemain untuk menata dan menaikkan level pahlawan.
        /// </summary>
        public void EnterInterWavePreparation(int nextWaveNumber, int bonusMoves)
        {
            isFirstWavePreparation = false;
            movesRemaining = bonusMoves;
            SetState(BattleState.Preparation);

            // 1. Kembalikan seluruh pahlawan yang bertahan di grid ke wujud token persiapan terlebih dahulu
            foreach (var unit in UnitController.ActiveUnits.ToArray())
            {
                if (unit != null && unit.CurrentTier > UnitTier.Tier0)
                {
                    unit.SetAsPreparationToken(true);
                }
            }

            // 2. Kembalikan batu permata T0 yang ada dari gelombang sebelumnya & pulihkan HP seluruh hero aktif
            Combat.UnitController.RestoreDormantT0Gems();
            Combat.UnitController.ReviveAllActiveHeroes();

            // 3. Refill HANYA petak yang benar-benar kosong dengan batu permata T0 baru agar grid penuh
            if (currentBattleMode == BattleMode.PuzzlePreparation && GridMatch3Solver.Instance != null)
            {
                GridMatch3Solver.Instance.RefillEmptyCells();
            }

            OnMovesRemainingChanged?.Invoke(movesRemaining);
            OnInterWavePreparationStarted?.Invoke(nextWaveNumber, bonusMoves);

            Debug.Log($"[GameManager] Fase Taktis Antar-Gelombang dimulai untuk Wave {nextWaveNumber} dengan +{bonusMoves} langkah!");
        }

        public void AddTacticalMoves(int amount)
        {
            if (currentState != BattleState.Preparation || amount <= 0) return;
            movesRemaining += amount;
            OnMovesRemainingChanged?.Invoke(movesRemaining);
            Debug.Log($"[GameManager] Bonus Langkah Taktis Combo Diberikan: +{amount} (Total: {movesRemaining})");
        }

        public void AddMoves(int amount) => AddTacticalMoves(amount);

        public bool TryConsumeMove()
        {
            if (currentState != BattleState.Preparation)
                return false;

            movesRemaining = Mathf.Max(0, movesRemaining - 1);
            OnMovesRemainingChanged?.Invoke(movesRemaining);

            if (movesRemaining <= 0)
            {
                StartBattleWavesNow();
            }
            return true;
        }

        public void StartBattleWavesNow()
        {
            if (currentState != BattleState.Preparation) return;
            StartCoroutine(StartBattleTransitionRoutine());
        }

        private System.Collections.IEnumerator StartBattleTransitionRoutine()
        {
            // Hentikan solver coroutines yang sedang berjalan agar tidak mengisi permata di tengah transisi
            if (GridSystem.GridMatch3Solver.Instance != null)
            {
                GridSystem.GridMatch3Solver.Instance.StopAllCoroutines();
                GridSystem.GridMatch3Solver.Instance.ResetResolvingState();
            }

            // Mainkan audio alert transisi tempur
            if (Audio.AudioManager.Instance != null)
            {
                Audio.AudioManager.Instance.PlaySound(Audio.SoundType.WaveIncoming);
            }

            // Beritahu HUD / VFX bahwa mode tempur dimulai
            if (Combat.SkillVfxManager.Instance != null)
            {
                Combat.SkillVfxManager.Instance.SpawnFloatingText(new Vector3(0f, 1.0f, 0f), "⚔️ PERTAHANAN DIMULAI!", new Color(1f, 0.85f, 0.2f), 1.8f, true);
            }

            // ATURAN RESMI: Permata mentah (Tier 0) larut halus menjadi lahan kosong,
            // sedangkan Pahlawan (T1, T2, T3) bertransformasi megah di petaknya masing-masing (Point 3 & 4)
            foreach (var unit in Combat.UnitController.ActiveUnits.ToArray())
            {
                if (unit != null)
                {
                    unit.TransformTokenToCombatHero();
                }
            }

            // Jeda transisi diperlambat (Point 3) agar pemain melihat perubahan wujud dan lenyapnya permata dengan jelas
            yield return new WaitForSeconds(0.85f);

            // Garansi Bersih Mode Tempur (Sprint 44): Pastikan seluruh T0 Gem tertidur (deactivated) selama perang
            if (GridSystem.GridManager.Instance != null && GridSystem.GridManager.Instance.IsInitialized)
            {
                int w = GridSystem.GridManager.Instance.GridWidth;
                int h = GridSystem.GridManager.Instance.GridHeight;
                for (int x = 0; x < w; x++)
                {
                    for (int y = 0; y < h; y++)
                    {
                        var cell = GridSystem.GridManager.Instance.GetCell(x, y);
                        if (cell != null && cell.CurrentUnit != null && cell.CurrentUnit.CurrentTier == UnitTier.Tier0)
                        {
                            var leftoverGem = cell.CurrentUnit;
                            if (leftoverGem.gameObject.activeSelf)
                            {
                                if (!Combat.UnitController.DormantT0Gems.Contains(leftoverGem))
                                {
                                    Combat.UnitController.DormantT0Gems.Add(leftoverGem);
                                }
                                leftoverGem.gameObject.SetActive(false);
                            }
                        }
                    }
                }
            }

            SetState(BattleState.WaveActive);

            if (Wave.WaveSpawner.Instance != null)
            {
                if (isFirstWavePreparation)
                {
                    isFirstWavePreparation = false;
                    Wave.WaveSpawner.Instance.StartBattleWaves();
                }
                else
                {
                    Wave.WaveSpawner.Instance.StartNextWave();
                }
            }
        }

        private System.Collections.IEnumerator StartWaveCountdownRoutine()
        {
            float remaining = preparationDuration;
            while (remaining > 0f)
            {
                if (remaining <= 3.1f && remaining > 0.1f)
                {
                    if (FantasyDefense.Mobile.DevicePerformanceManager.Instance != null && FantasyDefense.Mobile.DevicePerformanceManager.Instance.IsHapticsEnabled)
                    {
                        FantasyDefense.Mobile.DevicePerformanceManager.Instance.TriggerHaptic(FantasyDefense.Mobile.HapticFeedbackType.Selection);
                    }
                    if (FantasyDefense.Audio.AudioManager.Instance != null)
                    {
                        FantasyDefense.Audio.AudioManager.Instance.PlaySound(FantasyDefense.Audio.SoundType.ButtonClick, 1.35f);
                    }
                }
                float step = Mathf.Min(1.0f, remaining);
                yield return new WaitForSeconds(step);
                remaining -= step;
            }

            if (FantasyDefense.Mobile.DevicePerformanceManager.Instance != null && FantasyDefense.Mobile.DevicePerformanceManager.Instance.IsHapticsEnabled)
            {
                FantasyDefense.Mobile.DevicePerformanceManager.Instance.TriggerHaptic(FantasyDefense.Mobile.HapticFeedbackType.Medium);
            }

            StartBattleWavesNow();
        }

        private void SpawnInitialHeroes()
        {
            if (HeroSummoner.Instance == null || initialFreeSummons <= 0) return;

            for (int i = 0; i < initialFreeSummons; i++)
            {
                if (GridManager.Instance != null && GridManager.Instance.IsGridFull())
                    break;

                HeroSummoner.Instance.TrySummonFreeHero();
            }

            // Segarkan status tombol summon setelah pemanggilan gratis awal
            if (FantasyDefense.UI.SummonButtonUI.Instance != null)
            {
                FantasyDefense.UI.SummonButtonUI.Instance.UpdateButtonState();
            }
        }

        private void PopulateGridForPuzzleMode()
        {
            if (GridManager.Instance == null || HeroSummoner.Instance == null) return;

            HeroSummoner.Instance.EnsureHeroPoolLoaded();

            int width = GridManager.Instance.GridWidth;
            int height = GridManager.Instance.GridHeight;

            int spawnedCount = 0;
            // ATURAN RESMI: Seluruh petak arena pada awal fase persiapan diisi 100% oleh Batu Permata Mentah Tier 0.
            // PENGAMAN ANTI-MATCH AWAL (Point 1): Menjamin 100% TIDAK ADA permata kembar (3, 4, atau 5) saat awal permainan!
            var excludedTypes = new System.Collections.Generic.HashSet<UnitType>();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    TileCell cell = GridManager.Instance.GetCell(x, y);
                    if (cell != null && !cell.IsOccupied)
                    {
                        excludedTypes.Clear();

                        // Cegah kembar 3 horizontal: Cek 2 petak di sebelah kiri (x-1 dan x-2)
                        if (x >= 2)
                        {
                            var left1 = GridManager.Instance.GetCell(x - 1, y)?.CurrentUnit;
                            var left2 = GridManager.Instance.GetCell(x - 2, y)?.CurrentUnit;
                            if (left1 != null && left2 != null && left1.UnitType == left2.UnitType)
                            {
                                excludedTypes.Add(left1.UnitType);
                            }
                        }

                        // Cegah kembar 3 vertikal: Cek 2 petak di sebelah bawah (y-1 dan y-2)
                        if (y >= 2)
                        {
                            var down1 = GridManager.Instance.GetCell(x, y - 1)?.CurrentUnit;
                            var down2 = GridManager.Instance.GetCell(x, y - 2)?.CurrentUnit;
                            if (down1 != null && down2 != null && down1.UnitType == down2.UnitType)
                            {
                                excludedTypes.Add(down1.UnitType);
                            }
                        }

                        var heroData = HeroSummoner.Instance.GetRandomHeroExcluding(excludedTypes);
                        if (heroData != null)
                        {
                            HeroSummoner.Instance.SpawnUnitAtCell(heroData, cell, UnitTier.Tier0, isPreparationToken: true);
                            spawnedCount++;
                        }
                    }
                }
            }

            Debug.Log($"[GameManager] Populasi Mode 1 Puzzle selesai: {spawnedCount} permata T0 terpasang di arena (Garansi Bebas Match 3/4/5 Awal).");

            // Segarkan status tombol summon/katalis setelah populasi papan puzzle
            if (FantasyDefense.UI.SummonButtonUI.Instance != null)
            {
                FantasyDefense.UI.SummonButtonUI.Instance.UpdateButtonState();
            }
        }

        public void SetState(BattleState newState)
        {
            if (currentState == newState) return;
            currentState = newState;

            if (newState == BattleState.Defeat || newState == BattleState.Victory)
            {
                if (GridSystem.GridMatch3Solver.Instance != null)
                {
                    GridSystem.GridMatch3Solver.Instance.StopAllCoroutines();
                    GridSystem.GridMatch3Solver.Instance.ResetResolvingState();
                }

                if (Metagame.EndlessTrialManager.Instance != null && Metagame.EndlessTrialManager.Instance.IsEndlessModeActive)
                {
                    Metagame.EndlessTrialManager.Instance.ConcludeEndlessTrial();
                }

                if (newState == BattleState.Defeat)
                {
                    int stageId = Metagame.CitadelLobbyManager.Instance != null ? Metagame.CitadelLobbyManager.Instance.CurrentSelectedStageId : 101;
                    int teamCp = Metagame.CitadelLobbyManager.Instance != null ? Metagame.CitadelLobbyManager.Instance.AccountData.teamCombatPower : 500;
                    Analytics.CitadelTelemetryManager.Instance?.TrackStageFailed(stageId, 1, "Abyssal Swarm", teamCp);
                }
            }

            OnBattleStateChanged?.Invoke(currentState);
        }
    }
}
