using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FantasyDefense.Combat;
using FantasyDefense.Data;
using FantasyDefense.Economy;
using FantasyDefense.Audio;
using FantasyDefense.Mobile;

namespace FantasyDefense.GridSystem
{
    /// <summary>
    /// Mendeteksi dan mengeksekusi fusi otomatis ketika 3 unit sejenis berjejer
    /// secara horizontal atau vertikal di arena Grid (Match-3 Auto Merge).
    /// Mengosongkan 2 petak grid dan meningkatkan 1 unit ke Tier di atasnya (T1->T2, T2->T3).
    /// </summary>
    public class GridMatch3Solver : MonoBehaviour
    {
        public static GridMatch3Solver Instance { get; private set; }

        [Header("Configuration")]
        [Tooltip("Jika aktif, sistem otomatis menggabungkan 3 hero sebaris setelah summon atau perpindahan unit")]
        [SerializeField] private bool autoResolveMatch3 = true;

        [Tooltip("Bonus gems yang diberikan saat pemain berhasil membuat Match-3")]
        [SerializeField] private int match3BonusGems = 20;

        [Tooltip("Jeda waktu sebelum eksekusi fusi agar pemain melihat deretan match-3 sesaat")]
        [SerializeField] private float mergeDelay = 0.35f;

        [Header("Merge Rules & Multi-Unit Reward")]
        [Tooltip("Jumlah unit Tx+1 yang dihasilkan saat Match-5 (5 Kembar). Nilai 3 = 3 unit Tx+1.")]
        [SerializeField] private int match5PromotedCount = 3;
        public int Match5PromotedCount
        {
            get => match5PromotedCount;
            set => match5PromotedCount = Mathf.Clamp(value, 1, 3);
        }

        private bool isResolving = false;
        public bool IsResolving => isResolving;

        public static event Action<List<UnitController>, UnitController, UnitTier> OnMatch3Resolved; // (absorbedUnits, upgradedUnit, newTier)

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
            UnitController.OnUnitMoved += HandleUnitMoved;
        }

        private void OnDisable()
        {
            UnitController.OnUnitMoved -= HandleUnitMoved;
        }

        private void HandleUnitMoved(UnitController unit, TileCell cell)
        {
            if (autoResolveMatch3)
            {
                CheckAndResolveMatches(cell);
            }
        }

        [Header("Combo Juice & Audio")]
        [Tooltip("Jumlah combo berantai saat ini")]
        [SerializeField] private int currentCombo = 0;
        public int CurrentCombo => currentCombo;

        public static event Action<int> OnCascadeCombo; // (comboCount)

        // Petak target interaksi terakhir pemain (misal petak tujuan saat swipe)
        // Menjamin Tx+1 tercipta persis di petak aksi pemain, bukan meleset ke petak lain
        private TileCell lastInteractionCell = null;

        public void SetLastInteractionCell(TileCell cell)
        {
            lastInteractionCell = cell;
        }

        /// <summary>
        /// Memulai pemindaian grid untuk mencari deretan Match-3 yang valid.
        /// Dapat menerima parameter triggerCell (petak tujuan aksi pemain) untuk mengunci posisi evolusi.
        /// </summary>
        public void CheckAndResolveMatches(TileCell triggerCell = null)
        {
            if (triggerCell != null)
            {
                lastInteractionCell = triggerCell;
            }

            if (!autoResolveMatch3 || isResolving) return;

            // Pastikan solver hanya aktif pada Mode 1 (Puzzle Preparation) saat fase persiapan
            if (FantasyDefense.Core.GameManager.Instance != null &&
                (FantasyDefense.Core.GameManager.Instance.CurrentBattleMode != FantasyDefense.Data.BattleMode.PuzzlePreparation ||
                 FantasyDefense.Core.GameManager.Instance.CurrentState != FantasyDefense.Core.BattleState.Preparation))
            {
                return;
            }

            if (GridManager.Instance == null || !GridManager.Instance.IsInitialized)
                return;

            StartCoroutine(ScanAndResolveCoroutine());
        }

        /// <summary>
        /// Memicu proses cascade refill dari bawah dan pengecekan kombo secara publik
        /// (misalnya setelah fusi 2 unit manual via drag-and-drop).
        /// </summary>
        public void TriggerCascadeRefill()
        {
            if (isResolving) return;

            // Pengisian dari bawah (Cascade) HANYA berlaku untuk Mode 1 (Puzzle Preparation) dalam fase persiapan
            if (FantasyDefense.Core.GameManager.Instance != null &&
                (FantasyDefense.Core.GameManager.Instance.CurrentBattleMode != FantasyDefense.Data.BattleMode.PuzzlePreparation ||
                 FantasyDefense.Core.GameManager.Instance.CurrentState != FantasyDefense.Core.BattleState.Preparation))
            {
                return;
            }

            StartCoroutine(ManualCascadeAndResolveRoutine());
        }

        private IEnumerator ManualCascadeAndResolveRoutine()
        {
            isResolving = true;
            yield return StartCoroutine(CascadeAndRefillFromBelowCoroutine());
            yield return StartCoroutine(ScanAndResolveChainRoutine(0));
            lastInteractionCell = null;
            isResolving = false;
        }

        private IEnumerator ScanAndResolveCoroutine()
        {
            isResolving = true;
            currentCombo = 0;
            yield return StartCoroutine(ScanAndResolveChainRoutine(0));
            lastInteractionCell = null;
            isResolving = false;
        }

        private IEnumerator ScanAndResolveChainRoutine(int currentChain)
        {
            var clusters = FindAllMatchClusters();
            if (clusters == null || clusters.Count == 0)
            {
                yield break;
            }

            // Selesaikan cluster fusi pertama yang terdeteksi
            bool foundAnyMatch = false;
            foreach (var cluster in clusters)
            {
                if (cluster == null || cluster.promotedUnit == null) continue;

                currentChain++;
                currentCombo = currentChain;
                yield return new WaitForSeconds(mergeDelay);
                yield return StartCoroutine(ExecuteMatchClusterRoutine(cluster, currentChain));
                foundAnyMatch = true;
                break; // Selesaikan 1 kelompok fusi terlebih dahulu sebelum cascade
            }

            // Jika ada fusi yang terjadi, berikan jeda bernapas agar evolusi unit terlihat jelas sebelum cascade
            if (foundAnyMatch)
            {
                yield return new WaitForSeconds(0.20f);
                yield return StartCoroutine(CascadeAndRefillFromBelowCoroutine());
                yield return new WaitForSeconds(0.18f);
                // Pemindaian berulang untuk rantai kombo lanjutan
                yield return StartCoroutine(ScanAndResolveChainRoutine(currentChain));
            }
        }

        /// <summary>
        /// Mengatur ulang status resolving saat transisi antar-fase dimulai.
        /// </summary>
        public void ResetResolvingState()
        {
            isResolving = false;
            lastInteractionCell = null;
        }

        /// <summary>
        /// Mengisi lahan kosong di arena grid secara otomatis dengan urutan 2 fase:
        /// 1. Posisi Naik Dulu: Unit yang ada di bawah petak kosong bergeser naik mengisi ruang di atasnya (Point 1).
        /// 2. Baru Gem T0 Mengisi: Petak kosong yang tersisa di baris bawah diisi permata T0 baru meluncur dari bawah.
        /// </summary>
        public IEnumerator CascadeAndRefillFromBelowCoroutine()
        {
            if (GridManager.Instance == null || !GridManager.Instance.IsInitialized)
            {
                isResolving = false;
                yield break;
            }

            if (FantasyDefense.Core.GameManager.Instance != null &&
                (FantasyDefense.Core.GameManager.Instance.CurrentBattleMode != FantasyDefense.Data.BattleMode.PuzzlePreparation ||
                 FantasyDefense.Core.GameManager.Instance.CurrentState != FantasyDefense.Core.BattleState.Preparation))
            {
                isResolving = false;
                yield break;
            }

            int width = GridManager.Instance.GridWidth;
            int height = GridManager.Instance.GridHeight;

            // PENGISIAN SERENTAK DARI BAWAH (Synchronous Bottom-to-Top Unified Cascade):
            // Sesuai Instruksi Komandan (Poin 4):
            // Unit yang bergeser naik dan gem T0 yang mengisi dari bawah bergerak serentak bebarengan (simultaneous),
            // melenyapkan efek jeda atau kesan bidak jatuh.
            bool anyMovement = false;
            float slideDuration = 0.22f;

            for (int x = 0; x < width; x++)
            {
                // 1. Geser seluruh unit yang ada di kolom ini ke atas untuk mengisi petak-petak kosong di atasnya
                for (int y = height - 1; y >= 1; y--)
                {
                    TileCell targetCell = GridManager.Instance.GetCell(x, y);
                    if (targetCell == null || targetCell.IsOccupied) continue;

                    // Cari unit terdekat di bawah petak ini
                    for (int belowY = y - 1; belowY >= 0; belowY--)
                    {
                        TileCell sourceCell = GridManager.Instance.GetCell(x, belowY);
                        if (sourceCell != null && sourceCell.IsOccupied && sourceCell.CurrentUnit != null)
                        {
                            UnitController unitToShift = sourceCell.CurrentUnit;
                            if (unitToShift == null || !unitToShift.gameObject.activeInHierarchy)
                            {
                                continue;
                            }

                            sourceCell.ClearOccupant();
                            targetCell.SetOccupant(unitToShift);
                            unitToShift.SetCurrentCell(targetCell);
                            unitToShift.SmoothSnapTo(targetCell.transform.position, slideDuration);

                            anyMovement = true;
                            break;
                        }
                    }
                }

                // 2. Pada saat bersamaan (bebarengan), petak-petak kosong yang tersisa di bagian bawah
                // langsung diisi oleh batu permata T0 baru yang meluncur naik serentak dari bawah layar
                for (int y = 0; y < height; y++)
                {
                    TileCell emptyCell = GridManager.Instance.GetCell(x, y);
                    if (emptyCell != null && !emptyCell.IsOccupied)
                    {
                        var heroData = FantasyDefense.Summon.HeroSummoner.Instance != null
                            ? FantasyDefense.Summon.HeroSummoner.Instance.GetRandomHeroFromPool()
                            : null;

                        if (heroData != null)
                        {
                            Vector3 spawnPos = emptyCell.transform.position + Vector3.down * 1.05f;
                            UnitTier spawnTier = UnitTier.Tier0;
                            UnitController newUnit = FantasyDefense.Summon.HeroSummoner.Instance.SpawnUnitAtCell(heroData, emptyCell, spawnTier, isPreparationToken: true);
                            if (newUnit != null)
                            {
                                newUnit.transform.position = spawnPos;
                                if (newUnit.gameObject.activeInHierarchy)
                                {
                                    newUnit.SmoothSnapTo(emptyCell.transform.position, slideDuration);
                                }
                                else
                                {
                                    newUnit.transform.position = emptyCell.transform.position;
                                }
                                anyMovement = true;

                                // Partikel halus dan kilau sambutan kemunculan permata baru
                                if (SkillVfxManager.Instance != null)
                                {
                                    SkillVfxManager.Instance.SpawnProceduralParticleBurst(
                                        emptyCell.transform.position,
                                        new Color(1f, 1f, 1f, 0.6f),
                                        new Color(0.7f, 0.9f, 1f, 0.35f),
                                        5,
                                        0.9f
                                    );
                                }
                            }
                        }
                    }
                }
            }

            if (anyMovement)
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySound(SoundType.SummonHero);
                }
                yield return new WaitForSeconds(slideDuration + 0.04f);
            }
        }

        /// <summary>
        /// Mengisi seluruh petak kosong di grid dengan batu permata T0 baru agar grid senantiasa penuh di fase persiapan.
        /// </summary>
        public void RefillEmptyCells()
        {
            if (GridManager.Instance == null || FantasyDefense.Summon.HeroSummoner.Instance == null) return;
            var emptyCells = GridManager.Instance.GetEmptyCells();
            foreach (var cell in emptyCells)
            {
                var heroData = FantasyDefense.Summon.HeroSummoner.Instance.GetRandomHeroFromPool();
                if (heroData != null)
                {
                    UnitTier spawnTier = UnitTier.Tier0; // 100% Batu Permata Mentah T0!
                    FantasyDefense.Summon.HeroSummoner.Instance.SpawnUnitAtCell(heroData, cell, spawnTier, isPreparationToken: true);
                }
            }
        }

        /// <summary>
        /// Melakukan simulasi pertukaran dua petak bersebelahan dan mengecek apakah terbentuk barisan Match-3 (Kembar 3).
        /// Mengembalikan true jika terbentuk match 3, false jika tidak ada match (sehingga token mantul kembali/snapback).
        /// </summary>
        public bool SimulateSwapAndCheckMatch(TileCell cellA, TileCell cellB)
        {
            if (cellA == null || cellB == null || cellA.CurrentUnit == null || cellB.CurrentUnit == null)
                return false;

            UnitController unitA = cellA.CurrentUnit;
            UnitController unitB = cellB.CurrentUnit;

            // Simulasi swap internal
            cellA.SetOccupantInternal(unitB);
            cellB.SetOccupantInternal(unitA);

            bool matchFound = HasMatchAt(cellA.X, cellA.Y) || HasMatchAt(cellB.X, cellB.Y);

            // Revert swap kembali ke kondisi semula
            cellA.SetOccupantInternal(unitA);
            cellB.SetOccupantInternal(unitB);

            return matchFound;
        }

        /// <summary>
        /// Memeriksa apakah sel pada koordinat (x, y) menjadi bagian dari deretan 3 kembar horizontal atau vertikal.
        /// </summary>
        public bool HasMatchAt(int x, int y)
        {
            if (GridManager.Instance == null) return false;
            TileCell centerCell = GridManager.Instance.GetCell(x, y);
            if (centerCell == null || !centerCell.IsOccupied || centerCell.CurrentUnit == null) return false;

            UnitType type = centerCell.CurrentUnit.UnitType;
            UnitTier tier = centerCell.CurrentUnit.CurrentTier;
            if (tier >= UnitTier.Tier3) return false; // Tier 3 tidak dapat difusi lebih lanjut

            int width = GridManager.Instance.GridWidth;
            int height = GridManager.Instance.GridHeight;

            // 1. Cek deretan Horizontal
            int horizCount = 1;
            for (int cx = x - 1; cx >= 0; cx--)
            {
                TileCell c = GridManager.Instance.GetCell(cx, y);
                if (c != null && c.IsOccupied && c.CurrentUnit != null && c.CurrentUnit.UnitType == type && c.CurrentUnit.CurrentTier == tier)
                    horizCount++;
                else break;
            }
            for (int cx = x + 1; cx < width; cx++)
            {
                TileCell c = GridManager.Instance.GetCell(cx, y);
                if (c != null && c.IsOccupied && c.CurrentUnit != null && c.CurrentUnit.UnitType == type && c.CurrentUnit.CurrentTier == tier)
                    horizCount++;
                else break;
            }
            if (horizCount >= 3) return true;

            // 2. Cek deretan Vertikal
            int vertCount = 1;
            for (int cy = y - 1; cy >= 0; cy--)
            {
                TileCell c = GridManager.Instance.GetCell(x, cy);
                if (c != null && c.IsOccupied && c.CurrentUnit != null && c.CurrentUnit.UnitType == type && c.CurrentUnit.CurrentTier == tier)
                    vertCount++;
                else break;
            }
            for (int cy = y + 1; cy < height; cy++)
            {
                TileCell c = GridManager.Instance.GetCell(x, cy);
                if (c != null && c.IsOccupied && c.CurrentUnit != null && c.CurrentUnit.UnitType == type && c.CurrentUnit.CurrentTier == tier)
                    vertCount++;
                else break;
            }
            return vertCount >= 3;
        }

        /// <summary>
        /// Mengeksekusi pergeseran resmi 1 langkah antara 2 unit bersebelahan, memotong sisa langkah,
        /// dan memicu fusi Match-3 untuk melebur 3 unit menjadi 1 Tx di atasnya.
        /// </summary>
        public void ExecuteSwapAndSolve(UnitController unitA, UnitController unitB)
        {
            TileCell cellA = unitA.CurrentCell;
            TileCell cellB = unitB.CurrentCell;
            if (cellA == null || cellB == null) return;

            cellA.ClearOccupant();
            cellB.ClearOccupant();

            cellA.SetOccupant(unitB);
            unitB.SetCurrentCell(cellA);
            unitB.SmoothSnapTo(cellA.transform.position, 0.12f);

            cellB.SetOccupant(unitA);
            unitA.SetCurrentCell(cellB);
            unitA.SmoothSnapTo(cellB.transform.position, 0.12f);

            if (FantasyDefense.Core.GameManager.Instance != null)
            {
                FantasyDefense.Core.GameManager.Instance.TryConsumeMove();
            }

            CheckAndResolveMatches();
        }

        /// <summary>
        /// Representasi data kelompok fusi (Match-3, Match-4, Match-5+, bentuk L, T, atau +).
        /// </summary>
        public class MatchCluster
        {
            public UnitType unitType;
            public UnitTier unitTier;
            public HashSet<TileCell> cells = new HashSet<TileCell>();
            public List<UnitController> allUnits = new List<UnitController>();
            public List<UnitController> promotedUnits = new List<UnitController>();
            public UnitController promotedUnit => promotedUnits.Count > 0 ? promotedUnits[0] : null;
            public List<UnitController> absorbedUnits = new List<UnitController>();
            public bool isIntersection = false; // Bentuk L, T, atau +
            public int matchCount => allUnits.Count;
        }

        /// <summary>
        /// Memindai seluruh arena grid untuk menemukan seluruh formasi fusi:
        /// Garis horizontal & vertikal (3 atau lebih, bisa 4, 5+), serta persimpangan bentuk L, T, atau +.
        /// </summary>
        private List<MatchCluster> FindAllMatchClusters()
        {
            List<MatchCluster> clusters = new List<MatchCluster>();
            if (GridManager.Instance == null || !GridManager.Instance.IsInitialized)
                return clusters;

            int width = GridManager.Instance.GridWidth;
            int height = GridManager.Instance.GridHeight;

            // 1. Kumpulkan semua garis kecocokan horizontal (panjang >= 3)
            List<List<TileCell>> horizLines = new List<List<TileCell>>();
            for (int y = 0; y < height; y++)
            {
                int startX = 0;
                while (startX < width)
                {
                    TileCell startCell = GridManager.Instance.GetCell(startX, y);
                    if (startCell == null || !startCell.IsOccupied || startCell.CurrentUnit == null || startCell.CurrentUnit.CurrentTier >= UnitTier.Tier3)
                    {
                        startX++;
                        continue;
                    }

                    UnitType reqType = startCell.CurrentUnit.UnitType;
                    UnitTier reqTier = startCell.CurrentUnit.CurrentTier;
                    List<TileCell> currentLine = new List<TileCell> { startCell };

                    int nextX = startX + 1;
                    while (nextX < width)
                    {
                        TileCell nextCell = GridManager.Instance.GetCell(nextX, y);
                        if (nextCell != null && nextCell.IsOccupied && nextCell.CurrentUnit != null &&
                            nextCell.CurrentUnit.UnitType == reqType && nextCell.CurrentUnit.CurrentTier == reqTier)
                        {
                            currentLine.Add(nextCell);
                            nextX++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (currentLine.Count >= 3)
                    {
                        horizLines.Add(currentLine);
                    }
                    startX = nextX;
                }
            }

            // 2. Kumpulkan semua garis kecocokan vertikal (panjang >= 3)
            List<List<TileCell>> vertLines = new List<List<TileCell>>();
            for (int x = 0; x < width; x++)
            {
                int startY = 0;
                while (startY < height)
                {
                    TileCell startCell = GridManager.Instance.GetCell(x, startY);
                    if (startCell == null || !startCell.IsOccupied || startCell.CurrentUnit == null || startCell.CurrentUnit.CurrentTier >= UnitTier.Tier3)
                    {
                        startY++;
                        continue;
                    }

                    UnitType reqType = startCell.CurrentUnit.UnitType;
                    UnitTier reqTier = startCell.CurrentUnit.CurrentTier;
                    List<TileCell> currentLine = new List<TileCell> { startCell };

                    int nextY = startY + 1;
                    while (nextY < height)
                    {
                        TileCell nextCell = GridManager.Instance.GetCell(x, nextY);
                        if (nextCell != null && nextCell.IsOccupied && nextCell.CurrentUnit != null &&
                            nextCell.CurrentUnit.UnitType == reqType && nextCell.CurrentUnit.CurrentTier == reqTier)
                        {
                            currentLine.Add(nextCell);
                            nextY++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (currentLine.Count >= 3)
                    {
                        vertLines.Add(currentLine);
                    }
                    startY = nextY;
                }
            }

            // 3. Gabungkan garis-garis yang berpotongan (L-shape, T-shape, +-shape)
            List<List<TileCell>> allLines = new List<List<TileCell>>();
            allLines.AddRange(horizLines);
            allLines.AddRange(vertLines);

            foreach (var line in allLines)
            {
                if (line.Count == 0 || line[0].CurrentUnit == null) continue;
                UnitType lineType = line[0].CurrentUnit.UnitType;
                UnitTier lineTier = line[0].CurrentUnit.CurrentTier;

                MatchCluster targetCluster = null;
                foreach (var c in clusters)
                {
                    if (c.unitType == lineType && c.unitTier == lineTier)
                    {
                        foreach (var cell in line)
                        {
                            if (c.cells.Contains(cell))
                            {
                                targetCluster = c;
                                break;
                            }
                        }
                        if (targetCluster != null) break;
                    }
                }

                if (targetCluster == null)
                {
                    targetCluster = new MatchCluster
                    {
                        unitType = lineType,
                        unitTier = lineTier
                    };
                    clusters.Add(targetCluster);
                }

                foreach (var cell in line)
                {
                    if (!targetCluster.cells.Contains(cell) && cell.CurrentUnit != null)
                    {
                        targetCluster.cells.Add(cell);
                        targetCluster.allUnits.Add(cell.CurrentUnit);
                    }
                }
            }

            // 4. Tentukan unit yang dipromosikan (promotedUnit) dan unit yang diserap (absorbedUnits)
            foreach (var cluster in clusters)
            {
                TileCell intersectionCell = null;
                foreach (var hLine in horizLines)
                {
                    if (hLine.Count > 0 && hLine[0].CurrentUnit != null &&
                        hLine[0].CurrentUnit.UnitType == cluster.unitType &&
                        hLine[0].CurrentUnit.CurrentTier == cluster.unitTier)
                    {
                        foreach (var vLine in vertLines)
                        {
                            if (vLine.Count > 0 && vLine[0].CurrentUnit != null &&
                                vLine[0].CurrentUnit.UnitType == cluster.unitType &&
                                vLine[0].CurrentUnit.CurrentTier == cluster.unitTier)
                            {
                                foreach (var hCell in hLine)
                                {
                                    if (vLine.Contains(hCell))
                                    {
                                        intersectionCell = hCell;
                                        cluster.isIntersection = true;
                                        break;
                                    }
                                }
                                if (intersectionCell != null) break;
                            }
                        }
                    }
                    if (intersectionCell != null) break;
                }

                // Evaluasi unit yang disentuh/dipindahkan pemain secara langsung (Kotak yang sebenarnya)
                UnitController playerInteractedUnit = null;
                if (lastInteractionCell != null && lastInteractionCell.CurrentUnit != null && cluster.allUnits.Contains(lastInteractionCell.CurrentUnit))
                {
                    playerInteractedUnit = lastInteractionCell.CurrentUnit;
                }

                // Cek apakah deretan fusi merupakan garis murni vertikal (semua petak berada pada kolom X yang sama)
                bool isPureVertical = true;
                int refX = -1;
                foreach (var c in cluster.cells)
                {
                    if (refX == -1) refX = c.X;
                    else if (c.X != refX)
                    {
                        isPureVertical = false;
                        break;
                    }
                }

                if (intersectionCell != null && intersectionCell.CurrentUnit != null)
                {
                    cluster.promotedUnits.Add(intersectionCell.CurrentUnit);
                }
                else if (playerInteractedUnit != null)
                {
                    // PRIORITAS UTAMA (Point 1): Petak interaksi jari pemain ("Tx+1 udh benar sesuai jari").
                    // Bidak yang digeser/disentuh pemain langsung berevolusi menjadi Tx+1 tepat di posisi jari pemain.
                    cluster.promotedUnits.Add(playerInteractedUnit);

                    if (cluster.allUnits.Count >= 5)
                    {
                        // 5 Kembar (Supa Merge): Berikan 2 unit tambahan di sekitar petak aksi pemain
                        foreach (var u in cluster.allUnits)
                        {
                            if (cluster.promotedUnits.Count >= 3) break;
                            if (!cluster.promotedUnits.Contains(u)) cluster.promotedUnits.Add(u);
                        }
                    }
                    else if (cluster.allUnits.Count == 4)
                    {
                        // 4 Kembar: Berikan 1 unit pasangan terdekat di deretan
                        int pIdx = cluster.allUnits.IndexOf(playerInteractedUnit);
                        int partnerIdx = (pIdx == 0) ? 1 : (pIdx == 3 ? 2 : (pIdx == 1 ? 2 : 1));
                        if (partnerIdx >= 0 && partnerIdx < cluster.allUnits.Count)
                        {
                            cluster.promotedUnits.Add(cluster.allUnits[partnerIdx]);
                        }
                    }
                }
                else if (isPureVertical)
                {
                    // Fusi otomatis berantai tanpa sentuhan jari langsung (Cascade Chain Reaction):
                    // Promosikan unit paling atas (Y tertinggi) menghadap musuh
                    var sortedByY = new List<UnitController>(cluster.allUnits);
                    sortedByY.Sort((a, b) => (a.CurrentCell != null && b.CurrentCell != null) ? a.CurrentCell.Y.CompareTo(b.CurrentCell.Y) : 0);

                    if (cluster.allUnits.Count >= 5)
                    {
                        cluster.promotedUnits.Add(sortedByY[sortedByY.Count - 1]);
                        cluster.promotedUnits.Add(sortedByY[sortedByY.Count - 2]);
                        cluster.promotedUnits.Add(sortedByY[sortedByY.Count - 3]);
                    }
                    else if (cluster.allUnits.Count == 4)
                    {
                        cluster.promotedUnits.Add(sortedByY[sortedByY.Count - 1]);
                        cluster.promotedUnits.Add(sortedByY[sortedByY.Count - 2]);
                    }
                    else
                    {
                        cluster.promotedUnits.Add(sortedByY[sortedByY.Count - 1]);
                    }
                }
                else if (cluster.allUnits.Count >= 5)
                {
                    int count = cluster.allUnits.Count;
                    cluster.promotedUnits.Add(cluster.allUnits[1]);
                    cluster.promotedUnits.Add(cluster.allUnits[count / 2]);
                    cluster.promotedUnits.Add(cluster.allUnits[count - 2]);
                }
                else if (cluster.allUnits.Count == 4)
                {
                    cluster.promotedUnits.Add(cluster.allUnits[1]);
                    cluster.promotedUnits.Add(cluster.allUnits[2]);
                }
                else
                {
                    int midIdx = cluster.allUnits.Count / 2;
                    cluster.promotedUnits.Add(cluster.allUnits[midIdx]);
                }

                foreach (var unit in cluster.allUnits)
                {
                    if (!cluster.promotedUnits.Contains(unit))
                    {
                        cluster.absorbedUnits.Add(unit);
                    }
                }
            }

            return clusters;
        }

        /// <summary>
        /// Mengeksekusi fusi kelompok match: melebur 3 atau lebih unit (bisa 4, 5+, atau bentuk L/T/+)
        /// menjadi unit Tier berikutnya di arena grid dengan animasi meluncur konvergen ke atas (Point 1).
        /// Sesuai Standar Game Swap & Merge (Poin 4):
        /// Fusi bentuk L atau + (Cross Intersection 5 bidak) menghasilkan evolusi Super Merge lompat 2 Tier (Tier N+2)!
        /// </summary>
        private IEnumerator ExecuteMatchClusterRoutine(MatchCluster cluster, int comboChain = 1)
        {
            if (cluster == null || cluster.promotedUnit == null || cluster.allUnits.Count < 3) yield break;

            UnitController promotedUnit = cluster.promotedUnit;
            // Standar Game Swap & Merge: Fusi berbentuk L / T / + (Cross) adalah fusi 5-unit istimewa yang melompatkan 2 Tier (T0 -> T2 atau T1 -> T3)!
            int tierBoost = cluster.isIntersection ? 2 : 1;
            UnitTier nextTier = (UnitTier)Mathf.Min(3, (int)promotedUnit.CurrentTier + tierBoost);
            int matchCount = cluster.matchCount;
            Vector3 targetPos = promotedUnit.transform.position;

            // 1. Animasi Konvergensi: Bidak yang diserap meluncur mulus ke arah bidak yang dipromosikan (0.16s)
            float convergeDur = 0.16f;
            float elapsed = 0f;
            var startPositions = new Dictionary<UnitController, Vector3>();
            foreach (var absorbed in cluster.absorbedUnits)
            {
                if (absorbed != null)
                {
                    startPositions[absorbed] = absorbed.transform.position;
                    if (absorbed.CurrentCell != null)
                    {
                        absorbed.CurrentCell.ClearOccupant();
                    }
                }
            }

            while (elapsed < convergeDur)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / convergeDur);
                float easeT = t * t; // Percepatan konvergensi

                foreach (var absorbed in cluster.absorbedUnits)
                {
                    if (absorbed != null && startPositions.ContainsKey(absorbed))
                    {
                        Vector3 startPos = startPositions[absorbed];
                        // Sesuai Instruksi Komandan (Poin 4): Dilarang ada efek jatuh ke bawah!
                        // Bidak di atas posisi target melebur naik ke atas dan mengecil (dissolve up) tanpa pernah meluncur turun.
                        // Bidak di bawah posisi target meluncur naik ke arah target.
                        Vector3 destPos = (startPos.y > targetPos.y + 0.05f)
                            ? (startPos + Vector3.up * 0.18f)
                            : targetPos;

                        absorbed.transform.position = Vector3.Lerp(startPos, destPos, easeT);
                        absorbed.transform.localScale = Vector3.Lerp(absorbed.transform.localScale, Vector3.zero, easeT);
                    }
                }
                yield return null;
            }

            // Bersihkan dan hancurkan seluruh unit yang diserap
            foreach (var absorbed in cluster.absorbedUnits)
            {
                if (absorbed != null)
                {
                    Destroy(absorbed.gameObject);
                }
            }

            // 2. Upgrade seluruh unit yang dipromosikan ke Tier berikutnya & kunci posisi tepat di petak
            for (int i = 0; i < cluster.promotedUnits.Count; i++)
            {
                var promoted = cluster.promotedUnits[i];
                if (promoted != null)
                {
                    // Unit utama di pusat/sudut perpotongan mendapatkan promosi penuh (lompat 2 tier untuk L/+)
                    UnitTier unitTargetTier = (i == 0) ? nextTier : (UnitTier)Mathf.Min(3, (int)promoted.CurrentTier + 1);
                    promoted.UpgradeTier(unitTargetTier);
                    if (promoted.CurrentCell != null)
                    {
                        promoted.transform.position = new Vector3(promoted.CurrentCell.transform.position.x, promoted.CurrentCell.transform.position.y, -0.1f);
                    }
                }
            }

            // Audio & Pitch Feedback
            if (AudioManager.Instance != null)
            {
                float pitch = Mathf.Clamp(1.0f + (comboChain - 1) * 0.15f + (matchCount - 3) * 0.1f, 1.0f, 2.3f);
                AudioManager.Instance.PlaySound(SoundType.MergeSuccess, pitch);
            }

            // Haptic Feedback Tactile Pulse
            if (DevicePerformanceManager.Instance != null)
            {
                var hapticType = (cluster.isIntersection || matchCount >= 5 || nextTier == UnitTier.Tier3)
                    ? HapticFeedbackType.Heavy
                    : (matchCount == 4 || comboChain > 1 ? HapticFeedbackType.Success : HapticFeedbackType.Medium);
                DevicePerformanceManager.Instance.TriggerHaptic(hapticType);
            }

            // Floating Juice Text berdasarkan bentuk & jumlah formasi fusi
            if (SkillVfxManager.Instance != null && promotedUnit != null)
            {
                string comboText;
                Color txtColor = Color.white;

                if (cluster.isIntersection)
                {
                    comboText = nextTier == UnitTier.Tier3
                        ? (comboChain > 1 ? $"👑 CROSS MERGE T3 x{comboChain}!" : "👑 GODLIKE CROSS MERGE T3!")
                        : (comboChain > 1 ? $"🔥 SUPER L-MERGE T2 x{comboChain}!" : "🔥 SUPER L/T MERGE T2!");
                    txtColor = new Color(1f, 0.45f, 0.1f);
                }
                else if (matchCount >= 5)
                {
                    comboText = comboChain > 1 ? $"🌟 5-IN-A-ROW SUPA MERGE x{comboChain}!" : "🌟 5-IN-A-ROW SUPA MERGE!";
                    txtColor = new Color(1f, 0.85f, 0.1f);
                }
                else if (matchCount == 4)
                {
                    comboText = comboChain > 1 ? $"⚡ 4-MATCH MEGA MERGE x{comboChain}!" : "⚡ 4-MATCH MEGA MERGE!";
                    txtColor = new Color(0.25f, 0.9f, 1f);
                }
                else
                {
                    if (nextTier == UnitTier.Tier1)
                    {
                        comboText = comboChain > 1 ? $"⚡ HERO AWAKENED x{comboChain}!" : "HERO AWAKENED!";
                        txtColor = new Color(1f, 0.9f, 0.2f);
                    }
                    else
                    {
                        comboText = comboChain > 1 ? $"⚡ COMBO x{comboChain}!" : "TIER UP MERGE!";
                        txtColor = comboChain > 1 ? new Color(1f, 0.85f, 0.2f) : Color.white;
                    }
                }

                SkillVfxManager.Instance.SpawnFloatingText(promotedUnit.transform.position + Vector3.up * 0.6f, comboText, txtColor, 1.35f);
            }

            // Reward Langkah Taktis Combo (Sprint 44):
            // Setiap Kombo Berantai (Combo x2+) atau Super Merge (L/+ / 5-match) memberikan reward Langkah Taktis Tambahan!
            int bonusMoves = 0;
            if (comboChain >= 4)
            {
                bonusMoves = 2;
            }
            else if (comboChain >= 2 || cluster.isIntersection || matchCount >= 5)
            {
                bonusMoves = 1;
            }

            if (bonusMoves > 0 && FantasyDefense.Core.GameManager.Instance != null &&
                FantasyDefense.Core.GameManager.Instance.CurrentState == FantasyDefense.Core.BattleState.Preparation)
            {
                FantasyDefense.Core.GameManager.Instance.AddTacticalMoves(bonusMoves);
                if (SkillVfxManager.Instance != null && promotedUnit != null)
                {
                    string bonusText = bonusMoves > 1 ? $"⚡ +{bonusMoves} LANGKAH TAKTIS!" : "⚡ +1 LANGKAH TAKTIS!";
                    SkillVfxManager.Instance.SpawnFloatingText(promotedUnit.transform.position + Vector3.up * 1.15f, bonusText, new Color(1f, 0.95f, 0.2f), 1.6f, true);
                }
            }

            // Berikan bonus reward gems yang berlipat ganda sesuai combo chain dan ukuran fusi
            int sizeMultiplier = matchCount >= 5 ? 3 : (matchCount == 4 || cluster.isIntersection ? 2 : 1);
            int awardedGems = match3BonusGems * Mathf.Max(1, comboChain) * sizeMultiplier;
            if (EconomyManager.Instance != null && awardedGems > 0)
            {
                EconomyManager.Instance.AddGems(awardedGems);
            }

            string shapeDesc = cluster.isIntersection ? "Bentuk L/T/+" : $"{matchCount}-sebaris";
            Debug.Log($"[GridMatch3Solver] Match Terdeteksi ({shapeDesc})! {matchCount}x {promotedUnit.UnitType} melebur menjadi {promotedUnit.UnitType} Tier {(int)nextTier}. Combo x{comboChain}, Bonus {awardedGems} Gems!");

            OnCascadeCombo?.Invoke(comboChain);
            OnMatch3Resolved?.Invoke(cluster.absorbedUnits, promotedUnit, nextTier);
        }

        private void ExecuteMatchCluster(MatchCluster cluster, int comboChain = 1)
        {
            StartCoroutine(ExecuteMatchClusterRoutine(cluster, comboChain));
        }
    }
}
