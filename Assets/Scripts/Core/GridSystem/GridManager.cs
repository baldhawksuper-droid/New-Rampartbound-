using System;
using System.Collections.Generic;
using UnityEngine;
using FantasyDefense.Combat;
using FantasyDefense.Data;

namespace FantasyDefense.GridSystem
{
    /// <summary>
    /// Mengelola pembentukan, tata letak, dan status matriks petak arena (5x5 hingga 7x7).
    /// Dirancang responsif untuk orientasi layar Portrait ponsel cerdas (Android/iOS) dan tablet.
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        public static GridManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private LevelConfigSO levelConfig;
        [SerializeField] private GameObject tileCellPrefab;
        [SerializeField] private Sprite defaultTileSprite;
        [SerializeField] private Sprite hoverTileSprite;

        [Header("Dynamic Arena Layout")]
        [SerializeField] private GridLayout gridLayout;
        [SerializeField] private SpriteRenderer boardRenderer;

        private TileCell[,] gridMatrix;
        private int currentWidth;
        private int currentHeight;

        public int GridWidth => currentWidth;
        public int GridHeight => currentHeight;
        public bool IsInitialized => gridMatrix != null;
        public float CellSize => gridLayout != null ? gridLayout.CellSize : (levelConfig != null ? levelConfig.cellSize : 1.18f);
        public GridLayout Layout => gridLayout;

        public static event Action OnGridGenerated;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (gridLayout == null)
            {
                gridLayout = GetComponent<GridLayout>();
                if (gridLayout == null)
                {
                    gridLayout = gameObject.AddComponent<GridLayout>();
                }
            }
        }

        private Coroutine gridFadeCoroutine;

        private void OnEnable()
        {
            FantasyDefense.Core.GameManager.OnBattleStateChanged += HandleBattleStateChanged;
        }

        private void OnDisable()
        {
            FantasyDefense.Core.GameManager.OnBattleStateChanged -= HandleBattleStateChanged;
        }

        private void HandleBattleStateChanged(FantasyDefense.Core.BattleState state)
        {
            // Sesuai Instruksi Komandan (Poin 2):
            // a. Transisi Fade-Out lembut (alpha turun dari 1 ke 0 dalam 0.35 detik) bersamaan dengan transformasi hero.
            // b. Kotak kecil 100% transparan, frame papan besar disisakan bayangan tipis (alpha 10-15%).
            // c. Ketika gelombang selesai dan kembali ke fase taktis (Inter-Wave Preparation), kotak di-fade-in kembali.
            if (state == FantasyDefense.Core.BattleState.WaveActive)
            {
                FadeGridVisual(true, 0.35f);
            }
            else if (state == FantasyDefense.Core.BattleState.Preparation)
            {
                FadeGridVisual(false, 0.35f);
            }
        }

        /// <summary>
        /// Memicu transisi fade-out / fade-in lembut pada petak dan bingkai papan grid.
        /// </summary>
        public void FadeGridVisual(bool toCombat, float duration = 0.35f)
        {
            if (gridFadeCoroutine != null)
            {
                StopCoroutine(gridFadeCoroutine);
            }
            if (gameObject.activeInHierarchy)
            {
                gridFadeCoroutine = StartCoroutine(FadeGridVisualRoutine(toCombat, duration));
            }
            else
            {
                ApplyGridAlphaDirect(toCombat ? 0f : 1f, toCombat ? 0.0f : 0.95f);
            }
        }

        private System.Collections.IEnumerator FadeGridVisualRoutine(bool toCombat, float duration)
        {
            float startCellAlpha = toCombat ? 1f : 0f;
            float targetCellAlpha = toCombat ? 0f : 1f;

            float startBoardAlpha = toCombat ? 0.95f : 0.0f;
            float targetBoardAlpha = toCombat ? 0.0f : 0.95f;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                float currentCellAlpha = Mathf.Lerp(startCellAlpha, targetCellAlpha, t);
                float currentBoardAlpha = Mathf.Lerp(startBoardAlpha, targetBoardAlpha, t);

                ApplyGridAlphaDirect(currentCellAlpha, currentBoardAlpha);
                yield return null;
            }

            ApplyGridAlphaDirect(targetCellAlpha, targetBoardAlpha);
            gridFadeCoroutine = null;
        }

        private void ApplyGridAlphaDirect(float cellAlpha, float boardAlpha)
        {
            if (boardRenderer != null)
            {
                boardRenderer.enabled = boardAlpha > 0.005f;
                Color bCol = boardRenderer.color;
                bCol.a = boardAlpha;
                boardRenderer.color = bCol;
            }

            if (gridMatrix != null)
            {
                for (int x = 0; x < currentWidth; x++)
                {
                    for (int y = 0; y < currentHeight; y++)
                    {
                        TileCell cell = gridMatrix[x, y];
                        if (cell != null)
                        {
                            cell.SetVisualAlpha(cellAlpha);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Mengatur visibilitas visual kotak petak dan bingkai papan grid (backward compatibility).
        /// </summary>
        public void SetGridVisualVisibility(bool visible)
        {
            FadeGridVisual(!visible, 0.35f);
        }

        private void Start()
        {
            if (gridMatrix == null && levelConfig != null)
            {
                GenerateGrid(levelConfig);
            }
        }

        /// <summary>
        /// Membuat petak grid 2D di arena sesuai data konfigurasi level berbasis GridLayout dinamis.
        /// Mengeliminasi seluruh angka hardcode, menjamin grid berada tepat di tengah papan arena.
        /// </summary>
        public void GenerateGrid(LevelConfigSO config)
        {
            ClearGrid();

            // Self-healing fallback: Pastikan config tidak pernah null
            if (config == null)
            {
                config = levelConfig;
                if (config == null)
                {
                    config = Resources.Load<LevelConfigSO>("Levels/SO_Level1_Config");
                }
                if (config == null)
                {
                    config = ScriptableObject.CreateInstance<LevelConfigSO>();
                }
            }

            levelConfig = config;
            currentWidth = config.gridWidth > 0 ? config.gridWidth : 5;
            currentHeight = config.gridHeight > 0 ? config.gridHeight : 5;
            gridMatrix = new TileCell[currentWidth, currentHeight];

            if (gridLayout == null)
            {
                gridLayout = GetComponent<GridLayout>();
                if (gridLayout == null)
                {
                    gridLayout = gameObject.AddComponent<GridLayout>();
                }
            }

            if (boardRenderer == null)
            {
                var boardObj = GameObject.Find("BattlefieldBoard");
                if (boardObj != null)
                {
                    boardRenderer = boardObj.GetComponent<SpriteRenderer>();
                }
            }

            // Hitung tata letak matematis grid dari area papan
            if (gridLayout != null)
            {
                gridLayout.CalculateLayout(currentWidth, currentHeight, boardRenderer, config.cellSpacing);
            }

            if (defaultTileSprite == null)
            {
                defaultTileSprite = Resources.Load<Sprite>("Sprites/Grid/Tile_Empty");
            }

            float calculatedCellSize = gridLayout != null ? gridLayout.CellSize : 1.18f;

            for (int x = 0; x < currentWidth; x++)
            {
                for (int y = 0; y < currentHeight; y++)
                {
                    Vector3 worldPos = gridLayout != null ? gridLayout.GridToWorld(x, y) : new Vector3(x, y, 0);

                    GameObject cellObj;
                    if (tileCellPrefab != null)
                    {
                        cellObj = Instantiate(tileCellPrefab, worldPos, Quaternion.identity, transform);
                    }
                    else
                    {
                        cellObj = new GameObject($"Tile_[{x},{y}]");
                        cellObj.transform.position = worldPos;
                        cellObj.transform.SetParent(transform);
                    }

                    TileCell cell = cellObj.GetComponent<TileCell>();
                    if (cell == null)
                    {
                        cell = cellObj.AddComponent<TileCell>();
                    }

                    if (cell != null)
                    {
                        cell.Initialize(x, y, defaultTileSprite, hoverTileSprite, calculatedCellSize);
                        gridMatrix[x, y] = cell;
                    }
                }
            }

            try
            {
                OnGridGenerated?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GridManager] Terjadi error pada listener OnGridGenerated: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public void ClearGrid()
        {
            try
            {
                // Bersihkan daftar gem tertidur
                Combat.UnitController.DormantT0Gems.Clear();

                // 1. Bersihkan unit dan petak dari matriks
                if (gridMatrix != null)
                {
                    int w = gridMatrix.GetLength(0);
                    int h = gridMatrix.GetLength(1);
                    for (int x = 0; x < w; x++)
                    {
                        for (int y = 0; y < h; y++)
                        {
                            if (gridMatrix[x, y] != null)
                            {
                                try
                                {
                                    if (gridMatrix[x, y].CurrentUnit != null && gridMatrix[x, y].CurrentUnit.gameObject != null)
                                    {
                                        if (Application.isPlaying)
                                            Destroy(gridMatrix[x, y].CurrentUnit.gameObject);
                                        else
                                            DestroyImmediate(gridMatrix[x, y].CurrentUnit.gameObject);
                                    }
                                }
                                catch { }

                                try
                                {
                                    if (gridMatrix[x, y].gameObject != null)
                                    {
                                        if (Application.isPlaying)
                                        {
                                            gridMatrix[x, y].gameObject.SetActive(false);
                                            Destroy(gridMatrix[x, y].gameObject);
                                        }
                                        else
                                        {
                                            DestroyImmediate(gridMatrix[x, y].gameObject);
                                        }
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                    gridMatrix = null;
                }

                // 2. Sapu bersih seluruh sisa GameObject anak di bawah GridManager untuk garansi nol tumpang tindih
                for (int i = transform.childCount - 1; i >= 0; i--)
                {
                    var child = transform.GetChild(i);
                    if (child != null && child.gameObject != null)
                    {
                        try
                        {
                            if (Application.isPlaying)
                            {
                                child.gameObject.SetActive(false);
                                Destroy(child.gameObject);
                            }
                            else
                            {
                                DestroyImmediate(child.gameObject);
                            }
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GridManager] ClearGrid non-critical warning: {ex.Message}");
            }
        }

        public TileCell GetCell(int x, int y)
        {
            if (gridMatrix == null) return null;
            if (x < 0 || x >= currentWidth || y < 0 || y >= currentHeight) return null;
            return gridMatrix[x, y];
        }

        public List<TileCell> GetEmptyCells()
        {
            List<TileCell> emptyCells = new List<TileCell>();
            if (gridMatrix == null) return emptyCells;

            for (int x = 0; x < currentWidth; x++)
            {
                for (int y = 0; y < currentHeight; y++)
                {
                    if (gridMatrix[x, y] != null && !gridMatrix[x, y].IsOccupied)
                    {
                        emptyCells.Add(gridMatrix[x, y]);
                    }
                }
            }

            return emptyCells;
        }

        public TileCell GetRandomEmptyCell()
        {
            List<TileCell> emptyCells = GetEmptyCells();
            if (emptyCells.Count == 0) return null;

            int randomIndex = UnityEngine.Random.Range(0, emptyCells.Count);
            return emptyCells[randomIndex];
        }

        public bool IsGridFull()
        {
            return GetEmptyCells().Count == 0;
        }

        /// <summary>
        /// Mengambil seluruh unit hero aktif yang sedang menempati petak arena grid.
        /// Digunakan untuk evaluasi formasi taktis, auto-merge, dan highlight visual kandidat fusi.
        /// </summary>
        public List<UnitController> GetAllActiveUnits()
        {
            var units = new List<UnitController>();
            if (gridMatrix == null) return units;

            for (int x = 0; x < currentWidth; x++)
            {
                for (int y = 0; y < currentHeight; y++)
                {
                    if (gridMatrix[x, y] != null && gridMatrix[x, y].IsOccupied && gridMatrix[x, y].CurrentUnit != null)
                    {
                        units.Add(gridMatrix[x, y].CurrentUnit);
                    }
                }
            }
            return units;
        }

        /// <summary>
        /// Mengembalikan jumlah petak yang sedang terisi oleh unit hero.
        /// </summary>
        public int GetOccupiedCount()
        {
            if (gridMatrix == null) return 0;
            int count = 0;
            for (int x = 0; x < currentWidth; x++)
            {
                for (int y = 0; y < currentHeight; y++)
                {
                    if (gridMatrix[x, y] != null && gridMatrix[x, y].IsOccupied)
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        /// <summary>
        /// Fungsi tunggal konversi koordinat grid (x, y) menjadi posisi dunia (World Space).
        /// Seluruh sistem (spawn, swap, gravity fall, refill) wajib melewati fungsi ini.
        /// </summary>
        public Vector3 GridToWorld(int x, int y)
        {
            if (gridLayout == null)
            {
                gridLayout = GetComponent<GridLayout>() ?? gameObject.AddComponent<GridLayout>();
                if (levelConfig != null) gridLayout.CalculateLayout(currentWidth, currentHeight, boardRenderer, levelConfig.cellSpacing);
            }
            return gridLayout.GridToWorld(x, y);
        }

        /// <summary>
        /// Mengonversi posisi dunia (World Space) menjadi indeks koordinat grid (x, y).
        /// </summary>
        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            if (gridLayout == null)
            {
                gridLayout = GetComponent<GridLayout>() ?? gameObject.AddComponent<GridLayout>();
                if (levelConfig != null) gridLayout.CalculateLayout(currentWidth, currentHeight, boardRenderer, levelConfig.cellSpacing);
            }
            return gridLayout.WorldToGrid(worldPos);
        }

        /// <summary>
        /// Mengembalikan total ukuran dunia (panjang x lebar) dari grid untuk kalkulasi responsif kamera.
        /// </summary>
        public Vector2 GetGridWorldBounds()
        {
            if (gridLayout != null)
            {
                return gridLayout.GetGridWorldSize();
            }
            if (levelConfig == null) return new Vector2(5f, 5f);
            float stepX = levelConfig.cellSize + levelConfig.cellSpacing;
            float stepY = levelConfig.cellSize + levelConfig.cellSpacing;
            return new Vector2(currentWidth * stepX, currentHeight * stepY);
        }

        /// <summary>
        /// Mengembalikan petak grid terdekat berdasarkan koordinat dunia (World-Space Slot Snapping).
        /// Menggunakan WorldToGrid untuk deteksi instan O(1) dengan verifikasi jarak.
        /// </summary>
        public TileCell GetClosestCell(Vector3 worldPos, float maxDistance = -1f)
        {
            if (gridMatrix == null) return null;
            if (maxDistance <= 0f) maxDistance = CellSize * 0.95f;

            // 1. Coba deteksi langsung melalui WorldToGrid (O(1))
            Vector2Int gridCoord = WorldToGrid(worldPos);
            if (gridCoord.x >= 0 && gridCoord.x < currentWidth && gridCoord.y >= 0 && gridCoord.y < currentHeight)
            {
                TileCell directCell = gridMatrix[gridCoord.x, gridCoord.y];
                if (directCell != null)
                {
                    float dist = Vector3.Distance(directCell.transform.position, worldPos);
                    if (dist <= maxDistance)
                    {
                        return directCell;
                    }
                }
            }

            // 2. Fallback pencarian petak terdekat di seluruh matriks
            TileCell closest = null;
            float minSqrDist = maxDistance * maxDistance;

            for (int x = 0; x < currentWidth; x++)
            {
                for (int y = 0; y < currentHeight; y++)
                {
                    TileCell cell = gridMatrix[x, y];
                    if (cell == null) continue;

                    float sqrDist = (cell.transform.position - worldPos).sqrMagnitude;
                    if (sqrDist < minSqrDist)
                    {
                        minSqrDist = sqrDist;
                        closest = cell;
                    }
                }
            }

            return closest;
        }
    }
}
