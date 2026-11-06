using System;
using UnityEngine;

namespace FantasyDefense.GridSystem
{
    /// <summary>
    /// Komponen pengatur tata letak grid arena 2D berbasis kalkulasi dinamis.
    /// Menghitung cellSize dan posisi sel secara matematis dari area papan (boardRenderer.bounds)
    /// tanpa angka hardcode, menjamin grid selalu presisi dan center di rasio layar 9:16 hingga 9:21.
    /// </summary>
    public class GridLayout : MonoBehaviour
    {
        [Header("Board Reference (World Space)")]
        [Tooltip("SpriteRenderer dari papan arena (Board). Menggunakan SpriteRenderer.bounds karena arena berada di 2D World Space.")]
        [SerializeField] private SpriteRenderer boardRenderer;
        [SerializeField] private Vector2 boardPadding = new Vector2(0.2f, 0.2f);

        [Header("Grid Dimensions & Spacing")]
        [SerializeField] private int columns = 5;
        [SerializeField] private int rows = 5;
        [SerializeField] private float spacing = 0.08f;

        [Header("Calculated Output Properties")]
        [SerializeField] private float calculatedCellSize = 1.18f;
        [SerializeField] private Vector2 calculatedOrigin;
        [SerializeField] private Vector2 calculatedBoardCenter;

        public float CellSize => calculatedCellSize;
        public float Spacing => spacing;
        public int Columns => columns;
        public int Rows => rows;
        public Vector2 Origin => calculatedOrigin;
        public Vector2 BoardCenter => calculatedBoardCenter;
        public SpriteRenderer BoardRenderer => boardRenderer;

        public void SetBoardRenderer(SpriteRenderer renderer)
        {
            boardRenderer = renderer;
        }

        /// <summary>
        /// Menghitung ukuran sel (cellSize) dan origin titik awal sel secara matematis.
        /// cellSize = min(boardWidth / cols, boardHeight / rows) - spacing
        /// origin = boardCenter - new Vector2((cols-1)*step/2, (rows-1)*step/2)
        /// Arah Y konsisten: Baris y=0 berada di bawah, dan y=rows-1 berada di atas (Cartesian Y-Up).
        /// </summary>
        public void CalculateLayout(int cols, int rows, SpriteRenderer board = null, float customSpacing = -1f)
        {
            this.columns = Mathf.Max(1, cols);
            this.rows = Mathf.Max(1, rows);

            if (board != null)
            {
                boardRenderer = board;
            }

            if (customSpacing >= 0f)
            {
                spacing = customSpacing;
            }

            float boardWidth = 6.0f;
            float boardHeight = 6.0f;
            // Default center papan arena dengan kalibrasi offset +0.55f (Anti-Clipping Barricade)
            calculatedBoardCenter = new Vector2(0f, -0.10f);

            // Mengambil ukuran dari SpriteRenderer.bounds papan arena (World Space)
            if (boardRenderer != null)
            {
                Bounds bounds = boardRenderer.bounds;
                calculatedBoardCenter = bounds.center;

                // Kurangi padding agar petak tidak menabrak bingkai tepi papan
                boardWidth = Mathf.Max(0.5f, bounds.size.x - (boardPadding.x * 2f));
                boardHeight = Mathf.Max(0.5f, bounds.size.y - (boardPadding.y * 2f));
            }

            // Rumus baku arsitektur:
            // cellSize = min(boardWidth / cols, boardHeight / rows) dikurangi spacing
            calculatedCellSize = Mathf.Min(boardWidth / this.columns, boardHeight / this.rows) - spacing;
            calculatedCellSize = Mathf.Max(0.2f, calculatedCellSize);

            float step = calculatedCellSize + spacing;

            // origin = boardCenter - new Vector2((cols-1)*step/2, (rows-1)*step/2)
            calculatedOrigin = calculatedBoardCenter - new Vector2(
                (this.columns - 1) * step / 2f,
                (this.rows - 1) * step / 2f
            );

            Debug.Log($"[GridLayout] Kalkulasi Layout Selesai -> Board Bounds: {boardWidth:F2}x{boardHeight:F2}, Cols: {this.columns}, Rows: {this.rows}, CellSize: {calculatedCellSize:F3}, Spacing: {spacing:F3}, Origin: {calculatedOrigin}");
        }

        /// <summary>
        /// Satu-satunya fungsi konversi koordinat grid (x, y) menjadi koordinat dunia (World Space).
        /// Seluruh sistem (spawn, swap, gravity fall, refill) wajib melewati fungsi ini.
        /// </summary>
        public Vector3 GridToWorld(int x, int y)
        {
            float step = calculatedCellSize + spacing;
            float worldX = calculatedOrigin.x + (x * step);
            float worldY = calculatedOrigin.y + (y * step);
            return new Vector3(worldX, worldY, 0f);
        }

        /// <summary>
        /// Mengonversi posisi koordinat dunia menjadi indeks petak grid (x, y).
        /// </summary>
        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            float step = calculatedCellSize + spacing;
            if (step <= 0.0001f) return new Vector2Int(0, 0);

            int x = Mathf.RoundToInt((worldPos.x - calculatedOrigin.x) / step);
            int y = Mathf.RoundToInt((worldPos.y - calculatedOrigin.y) / step);
            return new Vector2Int(x, y);
        }

        public bool IsWithinBounds(int x, int y)
        {
            return x >= 0 && x < columns && y >= 0 && y < rows;
        }

        /// <summary>
        /// Mengembalikan total batas dunia (lebar x tinggi) dari seluruh matriks grid arena.
        /// </summary>
        public Vector2 GetGridWorldSize()
        {
            float step = calculatedCellSize + spacing;
            return new Vector2(columns * step, rows * step);
        }
    }
}
