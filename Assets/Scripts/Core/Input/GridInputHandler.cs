using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using FantasyDefense.Combat;
using FantasyDefense.Core;
using FantasyDefense.Data;
using FantasyDefense.Audio;
using FantasyDefense.Mobile;

namespace FantasyDefense.GridSystem
{
    /// <summary>
    /// Pengendali input swipe & tap untuk pergeseran bidak (Swap & Merge) pada perangkat mobile & desktop.
    /// Mendukung touch (Android/iOS) dan mouse fallback (Unity Editor).
    /// Mendeteksi arah dominan sapuan (Up, Down, Left, Right) dengan ambang batas ≈ 0.3 * cellSize,
    /// serta mengeksekusi animasi swap 0.15 detik dan logging diagnostik lengkap.
    /// </summary>
    public class GridInputHandler : MonoBehaviour
    {
        public static GridInputHandler Instance { get; private set; }

        [Header("Swipe Tuning")]
        [Tooltip("Ambang batas jarak sapuan jari (dalam rasio cellSize, dikalibrasi 0.15f untuk ergonomi mobile/tablet)")]
        [SerializeField] private float swipeThresholdRatio = 0.15f;

        [Tooltip("Durasi animasi pertukaran posisi (tween swap) dalam detik")]
        [SerializeField] private float swapDuration = 0.12f;

        [Tooltip("Jika aktif, swap yang tidak menghasilkan match akan diputar balik (revert swap)")]
        [SerializeField] private bool requireMatchToSwap = false;

        [Header("Tactile Gem Lift & Drag Parameters")]
        [Tooltip("Peningkatan skala visual saat gem/unit terangkat (lifted)")]
        [SerializeField] private float liftScaleMultiplier = 1.18f;
        [Tooltip("Batas maksimum pergeseran visual gem saat ditarik tangan")]
        [SerializeField] private float maxDragDistanceMultiplier = 1.35f;
        [Tooltip("Ambang pembatalan: Jika ditarik melebihi rasio ini, swap dibatalkan dan gem memantul kembali ke petak asal")]
        [SerializeField] private float cancelDragDistanceMultiplier = 1.55f;

        [Header("State Tracking")]
        [SerializeField] private bool isSwapping = false;
        private TileCell startCell = null;
        private Vector3 startWorldPos;
        private Vector2 startScreenPos;
        private float pointerDownTime = 0f;
        private bool isPointerDown = false;
        private Camera cachedCamera;

        private UnitController draggedUnit = null;
        private Vector3 draggedUnitOriginPos = Vector3.zero;
        private Vector3 draggedUnitOriginScale = Vector3.one;
        private int draggedUnitOriginalSortingOrder = 0;
        private SpriteRenderer draggedUnitSpriteRenderer = null;
        private bool isGemLifted = false;
        private TileCell currentHighlightedNeighbor = null;
        private Coroutine snapBackCoroutine = null;

        // Ghost Tile Preview System
        private GameObject ghostTileObject = null;
        private SpriteRenderer ghostTileRenderer = null;

        public bool IsSwapping => isSwapping;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            cachedCamera = Camera.main;
        }

        private void OnDestroy()
        {
            if (ghostTileObject != null)
            {
                Destroy(ghostTileObject);
                ghostTileObject = null;
            }
        }

        private void SpawnGhostTile(UnitController unit, Vector3 originPos)
        {
            if (unit == null) return;
            if (ghostTileObject == null)
            {
                ghostTileObject = new GameObject("GhostTile_DragPreview");
                ghostTileRenderer = ghostTileObject.AddComponent<SpriteRenderer>();
            }

            ghostTileObject.transform.position = originPos;
            ghostTileObject.transform.localScale = draggedUnitOriginScale * 0.95f;

            if (draggedUnitSpriteRenderer != null)
            {
                ghostTileRenderer.sprite = draggedUnitSpriteRenderer.sprite;
                ghostTileRenderer.flipX = draggedUnitSpriteRenderer.flipX;
                ghostTileRenderer.flipY = draggedUnitSpriteRenderer.flipY;
                ghostTileRenderer.sortingOrder = Mathf.Max(0, draggedUnitOriginalSortingOrder - 1);
            }
            // Siluet transparan biru-emas (tactile ghost imprint)
            ghostTileRenderer.color = new Color(0.80f, 0.92f, 1.0f, 0.42f);
            ghostTileObject.SetActive(true);
        }

        private void HideGhostTile()
        {
            if (ghostTileObject != null)
            {
                ghostTileObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (cachedCamera == null)
            {
                cachedCamera = Camera.main;
                if (cachedCamera == null) return;
            }

            HandleInput();
        }

        private void HandleInput()
        {
            // 1. Deteksi Sentuhan Mobile (Touch)
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    OnPointerDown(touch.position, touch.fingerId);
                }
                else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                {
                    OnPointerDrag(touch.position, touch.fingerId);
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    OnPointerUp(touch.position, touch.fingerId);
                }
            }
            // 2. Fallback Mouse (Unity Editor & Desktop)
            else
            {
                if (Input.GetMouseButtonDown(0))
                {
                    OnPointerDown(Input.mousePosition, -1);
                }
                else if (Input.GetMouseButton(0))
                {
                    OnPointerDrag(Input.mousePosition, -1);
                }
                else if (Input.GetMouseButtonUp(0))
                {
                    OnPointerUp(Input.mousePosition, -1);
                }
            }
        }

        private bool IsPointerOverUI(Vector2 screenPos)
        {
            if (EventSystem.current == null) return false;

            PointerEventData eventData = new PointerEventData(EventSystem.current)
            {
                position = screenPos
            };

            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            foreach (var result in results)
            {
                if (result.gameObject == null) continue;

                // Hanya blokir jika raycast mengenai kontrol interaktif nyata (Tombol, Slider, Toggle, ScrollRect)
                // atau modal popup aktif. JANGAN blokir jika hanya mengenai panel latar belakang dekoratif canvas!
                if (result.module is UnityEngine.UI.GraphicRaycaster)
                {
                    var selectable = result.gameObject.GetComponentInParent<UnityEngine.UI.Selectable>();
                    if (selectable != null && selectable.interactable)
                    {
                        return true;
                    }

                    // Blokir jika berada di dalam modal overlay (Settings, Pause, Gacha, Result Dialog)
                    string objName = result.gameObject.name;
                    if (objName.Contains("Settings") || objName.Contains("Pause") || objName.Contains("Modal") || objName.Contains("Popup") || objName.Contains("LogConsole"))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void OnPointerDown(Vector2 screenPos, int pointerId)
        {
            if (UnitController.IsAnyUnitDragging)
            {
                isPointerDown = false;
                startCell = null;
                return;
            }

            // Abaikan hanya jika pointer mengenai elemen UI interaktif (Button, Slider, dsb.)
            if (IsPointerOverUI(screenPos))
            {
                Debug.Log($"[GridInputHandler] Pointer Down diabaikan: Tertutup elemen UI interaktif Canvas pada Screen: {screenPos}.");
                return;
            }

            Vector3 worldPos = cachedCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -cachedCamera.transform.position.z));
            worldPos.z = 0f;

            TileCell cell = null;

            // Deteksi tile lewat Physics2D.OverlapPointAll untuk memeriksa seluruh collider di titik sentuh
            Collider2D[] hitColliders = Physics2D.OverlapPointAll(worldPos);
            foreach (var col in hitColliders)
            {
                cell = col.GetComponent<TileCell>() ?? col.GetComponentInParent<TileCell>();
                if (cell == null)
                {
                    var unit = col.GetComponent<UnitController>() ?? col.GetComponentInParent<UnitController>();
                    if (unit != null)
                    {
                        cell = unit.CurrentCell;
                    }
                }
                if (cell != null) break;
            }

            // Fallback deteksi petak terdekat
            if (cell == null && GridManager.Instance != null)
            {
                cell = GridManager.Instance.GetClosestCell(worldPos, GridManager.Instance.CellSize * 0.65f);
            }

            if (cell != null)
            {
                startCell = cell;
                startWorldPos = worldPos;
                startScreenPos = screenPos;
                pointerDownTime = Time.unscaledTime;
                isPointerDown = true;
                HideGhostTile();

                if (snapBackCoroutine != null)
                {
                    StopCoroutine(snapBackCoroutine);
                    snapBackCoroutine = null;
                }

                draggedUnit = cell.CurrentUnit;
                if (draggedUnit != null)
                {
                    draggedUnitOriginPos = cell.transform.position;
                    draggedUnitOriginScale = draggedUnit.transform.localScale;
                    draggedUnitSpriteRenderer = draggedUnit.GetComponent<SpriteRenderer>() ?? draggedUnit.GetComponentInChildren<SpriteRenderer>();
                    if (draggedUnitSpriteRenderer != null)
                    {
                        draggedUnitOriginalSortingOrder = draggedUnitSpriteRenderer.sortingOrder;
                    }
                    isGemLifted = false;
                }
                else
                {
                    draggedUnitSpriteRenderer = null;
                    isGemLifted = false;
                }

                if (GridMatchHintManager.Instance != null)
                {
                    GridMatchHintManager.Instance.ResetHintTimer();
                }

                string occupantName = cell.CurrentUnit != null ? cell.CurrentUnit.name : "KOSONG";
                Debug.Log($"[GridInputHandler] Pointer DOWN pada Screen: {screenPos}, World: {worldPos:F2} -> Tile Terpilih: [{cell.X}, {cell.Y}] (Occupant: {occupantName})");
            }
            else
            {
                isPointerDown = false;
                startCell = null;
                draggedUnit = null;
                isGemLifted = false;
                Debug.Log($"[GridInputHandler] Pointer DOWN pada Screen: {screenPos}, World: {worldPos:F2} -> Tidak mengenai petak mana pun.");
            }
        }

        private void OnPointerDrag(Vector2 screenPos, int pointerId)
        {
            if (UnitController.IsAnyUnitDragging) return;
            if (!isPointerDown || startCell == null || draggedUnit == null) return;
            if (isSwapping) return;

            Vector3 currentWorldPos = cachedCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -cachedCamera.transform.position.z));
            currentWorldPos.z = 0f;

            Vector2 currentDelta = (Vector2)currentWorldPos - (Vector2)startWorldPos;
            float cellSize = GridManager.Instance != null ? GridManager.Instance.CellSize : 1.18f;

            // Efek terangkat (Lift Visual & Elevation) saat sentuhan mulai bergeser sedikit
            if (!isGemLifted && currentDelta.magnitude >= 0.08f * cellSize)
            {
                isGemLifted = true;
                draggedUnit.transform.localScale = draggedUnitOriginScale * liftScaleMultiplier;
                if (draggedUnitSpriteRenderer != null)
                {
                    draggedUnitSpriteRenderer.sortingOrder = draggedUnitOriginalSortingOrder + 15;
                }
                Vector3 liftedP = draggedUnit.transform.position;
                liftedP.z = -0.4f;
                draggedUnit.transform.position = liftedP;

                // Tampilkan siluet ghost tile di petak asal
                SpawnGhostTile(draggedUnit, draggedUnitOriginPos);

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySound(SoundType.TileSlideSwish, 0.6f);
                }
            }

            if (isGemLifted && draggedUnit != null)
            {
                float maxDist = maxDragDistanceMultiplier * cellSize;
                float cancelDist = cancelDragDistanceMultiplier * cellSize;
                Vector2 clampedDelta = Vector2.ClampMagnitude(currentDelta, maxDist);
                Vector3 targetPos = draggedUnitOriginPos + (Vector3)clampedDelta;
                targetPos.z = -0.4f;

                draggedUnit.transform.position = Vector3.Lerp(draggedUnit.transform.position, targetPos, Time.deltaTime * 35f);

                // Periksa highlight petak tetangga yang sedang diarahkan
                if (currentDelta.magnitude >= swipeThresholdRatio * cellSize && currentDelta.magnitude <= cancelDist)
                {
                    int dx = Mathf.Abs(currentDelta.x) > Mathf.Abs(currentDelta.y) ? (currentDelta.x > 0 ? 1 : -1) : 0;
                    int dy = Mathf.Abs(currentDelta.x) > Mathf.Abs(currentDelta.y) ? 0 : (currentDelta.y > 0 ? 1 : -1);

                    TileCell neighbor = GridManager.Instance != null ? GridManager.Instance.GetCell(startCell.X + dx, startCell.Y + dy) : null;
                    if (neighbor != currentHighlightedNeighbor)
                    {
                        if (currentHighlightedNeighbor != null)
                        {
                            currentHighlightedNeighbor.SetHighlightState(TileHighlightState.None);
                        }
                        currentHighlightedNeighbor = neighbor;
                        if (currentHighlightedNeighbor != null)
                        {
                            // Highlight taktil: Berikan warna ValidMerge jika unit sejenis/tier sama, atau ValidMove jika petak kosong/swap normal
                            if (neighbor.CurrentUnit != null && draggedUnit != null && draggedUnit.CanMergeWith(neighbor.CurrentUnit))
                            {
                                currentHighlightedNeighbor.SetHighlightState(TileHighlightState.ValidMerge);
                            }
                            else
                            {
                                currentHighlightedNeighbor.SetHighlightState(TileHighlightState.ValidMove);
                            }
                        }
                    }
                }
                else
                {
                    if (currentHighlightedNeighbor != null)
                    {
                        currentHighlightedNeighbor.SetHighlightState(TileHighlightState.None);
                        currentHighlightedNeighbor = null;
                    }
                }
            }
        }

        private void OnPointerUp(Vector2 screenPos, int pointerId)
        {
            HideGhostTile();

            if (currentHighlightedNeighbor != null)
            {
                currentHighlightedNeighbor.SetHighlightState(TileHighlightState.None);
                currentHighlightedNeighbor = null;
            }

            if (UnitController.IsAnyUnitDragging)
            {
                if (draggedUnit != null)
                {
                    if (draggedUnitSpriteRenderer != null) draggedUnitSpriteRenderer.sortingOrder = draggedUnitOriginalSortingOrder;
                    draggedUnit.transform.localScale = draggedUnitOriginScale;
                    draggedUnit.transform.position = draggedUnitOriginPos;
                    draggedUnit = null;
                    isGemLifted = false;
                }
                isPointerDown = false;
                startCell = null;
                return;
            }

            if (!isPointerDown || startCell == null)
            {
                if (draggedUnit != null)
                {
                    if (draggedUnitSpriteRenderer != null) draggedUnitSpriteRenderer.sortingOrder = draggedUnitOriginalSortingOrder;
                    draggedUnit.transform.localScale = draggedUnitOriginScale;
                    draggedUnit.transform.position = draggedUnitOriginPos;
                    draggedUnit = null;
                    isGemLifted = false;
                }
                isPointerDown = false;
                startCell = null;
                return;
            }

            isPointerDown = false;

            Vector3 endWorldPos = cachedCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -cachedCamera.transform.position.z));
            endWorldPos.z = 0f;

            Vector2 delta = (Vector2)endWorldPos - (Vector2)startWorldPos;
            float cellSize = GridManager.Instance != null ? GridManager.Instance.CellSize : 1.18f;
            float threshold = swipeThresholdRatio * cellSize;
            float cancelDist = cancelDragDistanceMultiplier * cellSize;

            // Kembalikan sorting order normal
            if (draggedUnitSpriteRenderer != null)
            {
                draggedUnitSpriteRenderer.sortingOrder = draggedUnitOriginalSortingOrder;
            }

            // ATURAN 1: PENGGESERAN KE ARAH JAUH (DISTANCE CANCELLATION)
            if (delta.magnitude > cancelDist)
            {
                Debug.Log($"[GridInputHandler] Drag Dibatalkan: Jarak geser {delta.magnitude:F2} melebihi batas toleransi {cancelDist:F2}. Gem dikembalikan ke petak asal.");
                if (draggedUnit != null)
                {
                    snapBackCoroutine = StartCoroutine(SnapBackGemRoutine(draggedUnit, draggedUnitOriginPos, draggedUnitOriginScale, 0.14f));
                    draggedUnit = null;
                    isGemLifted = false;
                }
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySound(SoundType.TileReboundThud, 0.85f);
                }
                startCell = null;
                return;
            }

            // Kalibrasi Flick & DPI Layar Sentuh Mobile/Tablet (Task 5 Pacing Fix)
            float gestureDuration = Time.unscaledTime - pointerDownTime;
            float screenDeltaPx = Vector2.Distance(screenPos, startScreenPos);
            float minScreenSwipePx = Screen.dpi > 0 ? Mathf.Clamp(Screen.dpi * 0.10f, 20f, 60f) : 28f;

            // Deteksi Fast Flick (sapuan responsif jari <= 0.35 detik) atau pergeseran terarah melebihi threshold
            bool isFlick = gestureDuration > 0.001f && gestureDuration <= 0.35f && (delta.magnitude >= threshold * 0.55f || screenDeltaPx >= minScreenSwipePx);
            bool isNormalSwipe = delta.magnitude >= threshold || (screenDeltaPx >= minScreenSwipePx && delta.magnitude >= threshold * 0.70f);

            // ATURAN 2: TAP BIASA (DI BAWAH AMBANG BATAS SWIPE & BUKAN FLICK)
            if (!isFlick && !isNormalSwipe)
            {
                Debug.Log($"[GridInputHandler] Swipe Ditolak: Jarak {delta.magnitude:F2} < {threshold:F2} (Screen: {screenDeltaPx:F1}px, Durasi: {gestureDuration:F2}s -> Terdeteksi Tap/Klik biasa).");
                if (draggedUnit != null)
                {
                    snapBackCoroutine = StartCoroutine(SnapBackGemRoutine(draggedUnit, draggedUnitOriginPos, draggedUnitOriginScale, 0.10f));
                    draggedUnit = null;
                    isGemLifted = false;
                }
                startCell = null;
                return;
            }

            // ATURAN 3: SWIPE VALID KE PETAK BERDAMPINGAN (1 LANGKAH ORTOGONAL)
            int dirX = 0;
            int dirY = 0;
            string dirName = "";

            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                dirX = delta.x > 0 ? 1 : -1;
                dirName = dirX > 0 ? "Kanan (Right)" : "Kiri (Left)";
            }
            else
            {
                dirY = delta.y > 0 ? 1 : -1;
                dirName = dirY > 0 ? "Atas (Up)" : "Bawah (Down)";
            }

            int targetX = startCell.X + dirX;
            int targetY = startCell.Y + dirY;

            Debug.Log($"[GridInputHandler] Arah Sapuan Dominan: {dirName} dari [{startCell.X}, {startCell.Y}] menuju target [{targetX}, {targetY}]");

            TileCell originCell = startCell;
            startCell = null;

            if (draggedUnit != null)
            {
                draggedUnit.transform.localScale = draggedUnitOriginScale;
                draggedUnit.transform.position = draggedUnitOriginPos;
                draggedUnit = null;
                isGemLifted = false;
            }

            // Evaluasi syarat validitas swap & logging alasan jika ditolak
            TryExecuteSwap(originCell, targetX, targetY);
        }

        private IEnumerator SnapBackGemRoutine(UnitController unit, Vector3 targetPos, Vector3 targetScale, float duration)
        {
            if (unit == null) yield break;
            Vector3 startP = unit.transform.position;
            Vector3 startS = unit.transform.localScale;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = 1f - Mathf.Pow(1f - t, 3); // Ease-out cubic
                if (unit != null)
                {
                    unit.transform.position = Vector3.Lerp(startP, targetPos, ease);
                    unit.transform.localScale = Vector3.Lerp(startS, targetScale, ease);
                }
                yield return null;
            }
            if (unit != null)
            {
                unit.transform.position = targetPos;
                unit.transform.localScale = targetScale;
            }
        }

        private void TryExecuteSwap(TileCell cellA, int targetX, int targetY)
        {
            if (GridManager.Instance == null)
            {
                Debug.LogWarning("[GridInputHandler] Swap Ditolak: GridManager belum diinisialisasi.");
                return;
            }

            TileCell cellB = GridManager.Instance.GetCell(targetX, targetY);
            if (cellB == null)
            {
                Debug.Log($"[GridInputHandler] Swap Ditolak: Petak tujuan [{targetX}, {targetY}] berada di luar batas arena grid.");
                return;
            }

            if (isSwapping)
            {
                Debug.Log("[GridInputHandler] Swap Ditolak: Animasi pertukaran sebelumnya masih berjalan.");
                return;
            }

            if (GridMatch3Solver.Instance != null && GridMatch3Solver.Instance.IsResolving)
            {
                Debug.Log("[GridInputHandler] Swap Ditolak: Grid sedang memproses fusi otomatis atau cascade refill (IsResolving = true).");
                return;
            }

            if (GameManager.Instance != null)
            {
                if (GameManager.Instance.CurrentState != BattleState.Preparation)
                {
                    Debug.Log($"[GridInputHandler] Swap Ditolak: Game tidak berada di fase Preparation (Status Saat Ini: {GameManager.Instance.CurrentState}).");
                    return;
                }

                if (GameManager.Instance.MovesRemaining <= 0)
                {
                    Debug.Log("[GridInputHandler] Swap Ditolak: Jumlah langkah taktikal telah habis (Moves = 0).");
                    return;
                }
            }

            StartCoroutine(ExecuteSwapRoutine(cellA, cellB, swapDuration));
        }

        private IEnumerator ExecuteSwapRoutine(TileCell cellA, TileCell cellB, float duration)
        {
            isSwapping = true;
            try
            {
                UnitController unitA = cellA.CurrentUnit;
                UnitController unitB = cellB.CurrentUnit;

                Debug.Log($"[GridInputHandler] Memulai animasi swap {duration:F2}s antara [{cellA.X}, {cellA.Y}] ({(unitA != null ? unitA.name : "Kosong")}) dan [{cellB.X}, {cellB.Y}] ({(unitB != null ? unitB.name : "Kosong")})");

                Vector3 posA = cellA.transform.position;
                Vector3 posB = cellB.transform.position;

                // Audio desau pergeseran ubin taktil di atas papan (Tile Slide Swish)
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySound(SoundType.TileSlideSwish, 1.0f);
                }

                // Animasi pertukaran posisi secara halus (Lerp + Ease-Out Quad)
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    float ease = t * (2f - t);

                    if (unitA != null)
                    {
                        Vector3 pA = Vector3.Lerp(posA, posB, ease);
                        pA.z = -0.1f;
                        unitA.transform.position = pA;
                    }
                    if (unitB != null)
                    {
                        Vector3 pB = Vector3.Lerp(posB, posA, ease);
                        pB.z = -0.1f;
                        unitB.transform.position = pB;
                    }
                    yield return null;
                }

                if (unitA != null)
                {
                    posB.z = -0.1f;
                    unitA.transform.position = posB;
                }
                if (unitB != null)
                {
                    posA.z = -0.1f;
                    unitB.transform.position = posA;
                }

                // Uji kecocokan Match-3 jika opsi requireMatchToSwap diaktifkan
                cellA.SetOccupantInternal(unitB);
                cellB.SetOccupantInternal(unitA);

                bool hasMatch = false;
                if (GridMatch3Solver.Instance != null)
                {
                    hasMatch = GridMatch3Solver.Instance.HasMatchAt(cellA.X, cellA.Y) ||
                               GridMatch3Solver.Instance.HasMatchAt(cellB.X, cellB.Y);
                }

                if (requireMatchToSwap && !hasMatch)
                {
                    Debug.Log("[GridInputHandler] Swap Dibatalkan: Tidak menghasilkan deretan Match-3! Memutar balik posisi (Revert Swap) dengan efek rebound...");

                    // Umpan balik audio & getaran haptik peringatan (Invalid Move Feedback)
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlaySound(SoundType.TileReboundThud, 0.75f);
                    }

                    if (DevicePerformanceManager.Instance != null && DevicePerformanceManager.Instance.IsHapticsEnabled)
                    {
                        DevicePerformanceManager.Instance.TriggerHaptic(HapticFeedbackType.Warning);
                    }

                    Vector3 dir = (posB - posA).normalized;
                    Vector3 normal = new Vector3(-dir.y, dir.x, 0f);

                    elapsed = 0f;
                    while (elapsed < duration)
                    {
                        elapsed += Time.deltaTime;
                        float t = Mathf.Clamp01(elapsed / duration);
                        float ease = t * (2f - t);

                        // Efek getaran goyang lateral (wobble shake) penanda gerakan tidak sah
                        float shake = Mathf.Sin(t * Mathf.PI * 4f) * 0.08f * (1f - t);
                        Vector3 shakeOffset = normal * shake;

                        if (unitA != null) unitA.transform.position = Vector3.Lerp(posB, posA, ease) + shakeOffset;
                        if (unitB != null) unitB.transform.position = Vector3.Lerp(posA, posB, ease) - shakeOffset;
                        yield return null;
                    }

                    if (unitA != null)
                    {
                        unitA.transform.position = posA;
                        unitA.UpdateDepthSorting();
                    }
                    if (unitB != null)
                    {
                        unitB.transform.position = posB;
                        unitB.UpdateDepthSorting();
                    }

                    cellA.SetOccupantInternal(unitA);
                    cellB.SetOccupantInternal(unitB);

                    if (GridMatchHintManager.Instance != null)
                    {
                        GridMatchHintManager.Instance.ResetHintTimer();
                    }

                    yield break;
                }

                // Swap Diterima & Sah!
                cellA.ClearOccupant();
                cellB.ClearOccupant();

                if (unitB != null)
                {
                    cellA.SetOccupant(unitB);
                    unitB.UpdateDepthSorting();
                }
                if (unitA != null)
                {
                    cellB.SetOccupant(unitA);
                    unitA.UpdateDepthSorting();
                }

                // Kurangi sisa langkah HANYA setelah swap sah
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.TryConsumeMove();
                    Debug.Log($"[GridInputHandler] Swap Sah! 1 Langkah taktikal dikonsumsi. Sisa Langkah: {GameManager.Instance.MovesRemaining}");
                }

                // Audio & Haptic Feedback Swap Sukses
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySound(SoundType.TileSlideSwish, 0.85f);
                }

                if (DevicePerformanceManager.Instance != null && DevicePerformanceManager.Instance.IsHapticsEnabled)
                {
                    DevicePerformanceManager.Instance.TriggerHaptic(HapticFeedbackType.Light);
                }

                if (GridMatchHintManager.Instance != null)
                {
                    GridMatchHintManager.Instance.ResetHintTimer();
                }

                // Picu pemindaian Match-3 solver dengan mengunci petak target aksi (cellB)
                if (GridMatch3Solver.Instance != null)
                {
                    GridMatch3Solver.Instance.CheckAndResolveMatches(cellB);
                }
            }
            finally
            {
                isSwapping = false;
            }
        }
    }
}
