using System;
using UnityEngine;
using UnityEngine.EventSystems;
using FantasyDefense.Combat;
using FantasyDefense.Data;

namespace FantasyDefense.GridSystem
{
    public enum TileHighlightState
    {
        None,
        Hover,
        ValidMerge,
        ValidMove,
        Invalid,
        InvalidMove = Invalid
    }

    [RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
    public class TileCell : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Grid Coordinate")]
        [SerializeField] private int gridX;
        [SerializeField] private int gridY;

        [Header("Visual Feedback (Mobile Optimized)")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite defaultSprite;
        [SerializeField] private Sprite highlightSprite;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color hoverColor = new Color(1f, 0.95f, 0.4f, 1f);
        [SerializeField] private Color validMergeColor = new Color(0.20f, 1f, 0.40f, 1f); // Vibrant Emerald Green (High Luminescence)
        [SerializeField] private Color validMoveColor = new Color(0.18f, 0.78f, 1f, 1f);  // Electric Sky-Cyan (Crisp on Snow/Volcano)
        [SerializeField] private Color invalidColor = new Color(1f, 0.22f, 0.28f, 1f);    // Punchy Ruby Crimson (High Contrast on Lava & Ice)

        [Header("Occupant Status")]
        [SerializeField] private UnitController currentUnit;

        public int X => gridX;
        public int Y => gridY;
        public bool IsOccupied => currentUnit != null;
        public UnitController CurrentUnit => currentUnit;

        public static event Action<TileCell> OnTileClicked;
        public static event Action<TileCell> OnTileHoverEnter;
        public static event Action<TileCell> OnTileHoverExit;

        private BoxCollider2D boxCollider;

        private void Awake()
        {
            boxCollider = GetComponent<BoxCollider2D>();
            if (boxCollider == null)
            {
                boxCollider = gameObject.AddComponent<BoxCollider2D>();
            }

            // Cari atau inisialisasi child Background agar GameObject Cell terpisah dari Gem
            Transform bgChild = transform.Find("Background");
            if (bgChild != null)
            {
                spriteRenderer = bgChild.GetComponent<SpriteRenderer>();
            }
            else
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        public void Initialize(int x, int y, Sprite defaultTileSprite, Sprite hoverTileSprite, float cellSize = 1.18f)
        {
            gridX = x;
            gridY = y;
            defaultSprite = defaultTileSprite;
            highlightSprite = hoverTileSprite;

            // Induk Cell selalu berskala normal Vector3.one untuk mencegah double scaling pada bidak Gem
            transform.localScale = Vector3.one;

            // BoxCollider2D berukuran tepat sesuai cellSize untuk deteksi OverlapPoint yang akurat
            if (boxCollider == null) boxCollider = GetComponent<BoxCollider2D>();
            if (boxCollider != null)
            {
                boxCollider.size = new Vector2(cellSize, cellSize);
                boxCollider.isTrigger = false;
            }

            // Pisahkan GameObject: Cell (background sel) dan Gem (child, sorting order lebih tinggi)
            Transform bgChild = transform.Find("Background");
            if (bgChild == null)
            {
                var bgObj = new GameObject("Background");
                bgObj.transform.SetParent(transform, false);
                bgObj.transform.localPosition = new Vector3(0, 0, 0.05f);
                spriteRenderer = bgObj.AddComponent<SpriteRenderer>();

                // Nonaktifkan SpriteRenderer pada root jika ada agar tidak ganda
                var rootSr = GetComponent<SpriteRenderer>();
                if (rootSr != null && rootSr != spriteRenderer)
                {
                    rootSr.enabled = false;
                }
            }
            else
            {
                spriteRenderer = bgChild.GetComponent<SpriteRenderer>();
                bgChild.transform.localPosition = new Vector3(0, 0, 0.05f);
            }

            if (defaultSprite == null)
            {
                defaultSprite = Resources.Load<Sprite>("Sprites/Grid/Tile_Empty");
                if (defaultSprite == null)
                {
                    defaultSprite = GetOrCreateProceduralTileSprite();
                }
            }

            if (spriteRenderer != null && defaultSprite != null)
            {
                spriteRenderer.sprite = defaultSprite;
                spriteRenderer.color = normalColor;

                // Sorting Layer standar arsitektur: Cell (selalu di atas Board dan di bawah Gem)
                spriteRenderer.sortingLayerName = "Cell";
                spriteRenderer.sortingOrder = 5;

                float spriteW = (defaultSprite.bounds.size.x > 0.001f) ? defaultSprite.bounds.size.x : (defaultSprite.rect.width / defaultSprite.pixelsPerUnit);
                if (spriteW > 0.001f)
                {
                    spriteRenderer.transform.localScale = Vector3.one * (cellSize / spriteW);
                }
            }

            gameObject.name = $"Tile_[{x},{y}]";
        }

        public void Initialize(int x, int y, Sprite defaultTileSprite, Sprite hoverTileSprite)
        {
            float currentCellSize = GridManager.Instance != null ? GridManager.Instance.CellSize : 1.18f;
            Initialize(x, y, defaultTileSprite, hoverTileSprite, currentCellSize);
        }

        /// <summary>
        /// Mengatur visibilitas visual kotak petak (disembunyikan saat mode tempur hero agar pandangan medan perang bersih).
        /// </summary>
        public void SetVisualVisible(bool visible)
        {
            SetVisualAlpha(visible ? 1f : 0f);
        }

        /// <summary>
        /// Mengatur transparansi (alpha) visual petak dan status collider.
        /// Saat mode tempur hero (alpha = 0), collider dinonaktifkan agar raycast bebas hambatan.
        /// Saat fase persiapan (alpha = 1), collider diaktifkan kembali untuk interaksi sentuh pemain.
        /// </summary>
        public void SetVisualAlpha(float alpha)
        {
            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = alpha;
                spriteRenderer.color = c;
                spriteRenderer.enabled = alpha > 0.005f;
            }

            if (boxCollider != null)
            {
                boxCollider.enabled = alpha > 0.1f;
            }
        }

        public bool SetOccupant(UnitController unit)
        {
            if (unit == null) return false;

            // Guardrail Anti-Menumpuk: Tolak jika petak sudah ditempati unit lain
            if (currentUnit != null && currentUnit != unit)
            {
                Debug.LogWarning($"[TileCell] Petak [{gridX},{gridY}] sudah ditempati oleh {currentUnit.name}, mencegah penumpukan unit baru ({unit.name})!");
                return false;
            }

            currentUnit = unit;

            // Jadikan unit sebagai child dari TileCell sesuai arsitektur terpisah
            unit.transform.SetParent(transform, true);
            unit.transform.position = new Vector3(transform.position.x, transform.position.y, -0.1f);
            unit.SetCurrentCell(this);
            unit.UpdateVisualAppearance();
            unit.UpdateDepthSorting();
            UpdateRarityVisual();
            return true;
        }

        /// <summary>
        /// Pengisian occupant internal murni untuk simulasi swap Match-3 tanpa memicu reposisi fisik
        /// </summary>
        public void SetOccupantInternal(UnitController unit)
        {
            currentUnit = unit;
        }

        public void ClearOccupant()
        {
            currentUnit = null;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = normalColor;
            }
        }

        public void SetNormalColor(Color color)
        {
            normalColor = color;
            if (currentUnit == null && spriteRenderer != null)
            {
                spriteRenderer.color = normalColor;
            }
        }

        public void UpdateRarityVisual()
        {
            if (spriteRenderer == null || currentUnit == null) return;

            // Variasi warna rarity border (Point 5)
            switch (currentUnit.CurrentTier)
            {
                case UnitTier.Tier5:
                    spriteRenderer.color = new Color(1f, 0.95f, 0.45f, 1f); // Celestial Divine Radiant
                    break;
                case UnitTier.Tier4:
                    spriteRenderer.color = new Color(0.85f, 0.35f, 1f, 1f); // Mythic Amethyst Violet
                    break;
                case UnitTier.Tier3:
                    spriteRenderer.color = new Color(1f, 0.85f, 0.2f, 1f); // Radiant Gold
                    break;
                case UnitTier.Tier2:
                    spriteRenderer.color = new Color(0.35f, 0.75f, 1f, 1f); // Sapphire Blue
                    break;
                default:
                    spriteRenderer.color = new Color(0.85f, 0.90f, 0.95f, 1f); // Steel Silver
                    break;
            }
        }

        [Header("Tactile Fantasy Rune Bracket Overlay (Task 2)")]
        private GameObject runeBracketObj;
        private SpriteRenderer runeBracketSr;
        private Coroutine pulseGlowRoutine;
        private static Sprite goldenRuneBracketSprite;

        private void EnsureRuneBracketOverlay()
        {
            if (runeBracketObj != null) return;

            Transform child = transform.Find("RuneBracketOverlay");
            if (child != null)
            {
                runeBracketObj = child.gameObject;
                runeBracketSr = runeBracketObj.GetComponent<SpriteRenderer>();
            }
            else
            {
                runeBracketObj = new GameObject("RuneBracketOverlay");
                runeBracketObj.transform.SetParent(transform, false);
                runeBracketObj.transform.localPosition = new Vector3(0f, 0f, -0.04f);

                runeBracketSr = runeBracketObj.AddComponent<SpriteRenderer>();
                runeBracketSr.sprite = GetOrCreateRuneBracketSprite();
                runeBracketSr.sortingLayerName = spriteRenderer != null ? spriteRenderer.sortingLayerName : "Board";
                runeBracketSr.sortingOrder = spriteRenderer != null ? spriteRenderer.sortingOrder + 2 : 5;
            }
            runeBracketObj.SetActive(false);
        }

        private System.Collections.IEnumerator DoRunePulse(Color baseColor)
        {
            float t = 0f;
            Vector3 baseScale = Vector3.one;
            while (runeBracketObj != null && runeBracketObj.activeSelf)
            {
                t += Time.unscaledDeltaTime * 4.2f;
                float pulse = Mathf.PingPong(t, 1f);
                float scale = 1.0f + (pulse * 0.07f);
                runeBracketObj.transform.localScale = baseScale * scale;
                if (runeBracketSr != null)
                {
                    Color c = baseColor;
                    c.a = baseColor.a * (0.72f + (pulse * 0.28f));
                    runeBracketSr.color = c;
                }
                yield return null;
            }
        }

        private void OnDisable()
        {
            if (pulseGlowRoutine != null)
            {
                StopCoroutine(pulseGlowRoutine);
                pulseGlowRoutine = null;
            }
            if (runeBracketObj != null)
            {
                runeBracketObj.SetActive(false);
            }
        }

        /// <summary>
        /// Mengatur umpan balik visual petak dengan Tactile Golden Rune Bracket bernafas (Task 2).
        /// Ubin dasar batu tetap stabil tanpa staining kasar, sementara pendaran rune emas menyorot bidak.
        /// </summary>
        public void SetHighlightState(TileHighlightState state)
        {
            if (spriteRenderer == null) return;
            EnsureRuneBracketOverlay();

            if (state == TileHighlightState.None)
            {
                if (runeBracketObj != null) runeBracketObj.SetActive(false);
                if (pulseGlowRoutine != null)
                {
                    StopCoroutine(pulseGlowRoutine);
                    pulseGlowRoutine = null;
                }
                if (defaultSprite != null) spriteRenderer.sprite = defaultSprite;
                if (currentUnit != null) UpdateRarityVisual();
                else spriteRenderer.color = normalColor;
            }
            else
            {
                Color targetColor = hoverColor;
                switch (state)
                {
                    case TileHighlightState.Hover:
                        targetColor = new Color(1.0f, 0.86f, 0.35f, 0.95f); // Warm Golden Amber
                        break;
                    case TileHighlightState.ValidMerge:
                        targetColor = new Color(0.28f, 1.0f, 0.52f, 0.95f); // Luminescent Emerald Green
                        break;
                    case TileHighlightState.ValidMove:
                        targetColor = new Color(0.30f, 0.82f, 1.0f, 0.95f); // Sky Astral Cyan
                        break;
                    case TileHighlightState.Invalid:
                        targetColor = new Color(1.0f, 0.30f, 0.35f, 0.95f); // Ruby Crimson
                        break;
                }

                if (runeBracketObj != null)
                {
                    runeBracketObj.SetActive(true);
                    if (runeBracketSr != null) runeBracketSr.color = targetColor;

                    if (pulseGlowRoutine != null) StopCoroutine(pulseGlowRoutine);
                    pulseGlowRoutine = StartCoroutine(DoRunePulse(targetColor));
                }
            }
        }

        public void SetHighlight(bool isHighlighted)
        {
            SetHighlightState(isHighlighted ? TileHighlightState.Hover : TileHighlightState.None);
        }

        #region Pointer & Mobile Touch Event Handlers
        public void OnPointerClick(PointerEventData eventData)
        {
            OnTileClicked?.Invoke(this);

            // Dukungan Tap-to-Move ke petak kosong jika ada unit yang sedang dipilih
            if (!IsOccupied && UnitController.CurrentlySelectedUnit != null)
            {
                var unit = UnitController.CurrentlySelectedUnit;
                UnitController.DeselectCurrentUnit();
                unit.HandleDropOnCell(this);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            SetHighlight(true);
            OnTileHoverEnter?.Invoke(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetHighlight(false);
            OnTileHoverExit?.Invoke(this);
        }
        #endregion

        #region Procedural Fallback Sprite
        private static Sprite proceduralTileFallbackSprite;

        public static Sprite GetOrCreateProceduralTileSprite()
        {
            if (proceduralTileFallbackSprite != null) return proceduralTileFallbackSprite;

            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            // Standar Ashlar Fantasy Masonry: Ubin batu bertingkat lembut dengan mortar halus
            Color mortarColor = new Color(0.18f, 0.20f, 0.25f, 0.90f);
            Color baseColorTop = new Color(0.78f, 0.77f, 0.74f, 1f);
            Color baseColorBottom = new Color(0.62f, 0.60f, 0.58f, 1f);
            Color highlightBevel = new Color(0.92f, 0.90f, 0.86f, 1f);
            Color shadowBevel = new Color(0.40f, 0.38f, 0.36f, 1f);

            int margin = 5;
            Color[] colors = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                float v = (float)y / size;
                for (int x = 0; x < size; x++)
                {
                    if (x < margin || x >= size - margin || y < margin || y >= size - margin)
                    {
                        colors[y * size + x] = mortarColor;
                    }
                    else if (y >= size - margin - 3 || x < margin + 3)
                    {
                        colors[y * size + x] = highlightBevel;
                    }
                    else if (y < margin + 3 || x >= size - margin - 3)
                    {
                        colors[y * size + x] = shadowBevel;
                    }
                    else
                    {
                        colors[y * size + x] = Color.Lerp(baseColorBottom, baseColorTop, v);
                    }
                }
            }

            tex.SetPixels(colors);
            tex.Apply();
            proceduralTileFallbackSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return proceduralTileFallbackSprite;
        }

        public static Sprite GetOrCreateRuneBracketSprite()
        {
            if (goldenRuneBracketSprite != null) return goldenRuneBracketSprite;

            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            Color transparent = new Color(0f, 0f, 0f, 0f);
            Color[] colors = new Color[size * size];
            for (int i = 0; i < colors.Length; i++) colors[i] = transparent;

            int bracketLen = 32;
            int thickness = 5;
            int pad = 6;
            Color goldPrimary = new Color(1f, 0.88f, 0.40f, 1f);
            Color goldRim = new Color(0.72f, 0.55f, 0.20f, 0.85f);
            Color innerGlow = new Color(1f, 0.95f, 0.70f, 0.45f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool inCornerX = (x >= pad && x < pad + bracketLen) || (x < size - pad && x >= size - pad - bracketLen);
                    bool inCornerY = (y >= pad && y < pad + bracketLen) || (y < size - pad && y >= size - pad - bracketLen);
                    bool onEdgeX = (x >= pad && x < pad + thickness) || (x < size - pad && x >= size - pad - thickness);
                    bool onEdgeY = (y >= pad && y < pad + thickness) || (y < size - pad && y >= size - pad - thickness);

                    if ((onEdgeX && inCornerY) || (onEdgeY && inCornerX))
                    {
                        bool isBorder = (x == pad || x == size - pad - 1 || y == pad || y == size - pad - 1);
                        colors[y * size + x] = isBorder ? goldRim : goldPrimary;
                    }
                    else if (inCornerX && inCornerY && ((x >= pad + thickness && x < pad + thickness + 4) || (y >= pad + thickness && y < pad + thickness + 4)))
                    {
                        colors[y * size + x] = innerGlow;
                    }
                }
            }

            tex.SetPixels(colors);
            tex.Apply();
            goldenRuneBracketSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return goldenRuneBracketSprite;
        }
        #endregion
    }
}
