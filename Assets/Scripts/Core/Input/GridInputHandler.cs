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
        [Tooltip("Ambang batas jarak sapuan jari (dalam rasio cellSize, misal 0.3 * cellSize)")]
        [SerializeField] private float swipeThresholdRatio = 0.3f;

        [Tooltip("Durasi animasi pertukaran posisi (tween swap) dalam detik")]
        [SerializeField] private float swapDuration = 0.15f;

        [Tooltip("Jika aktif, swap yang tidak menghasilkan match akan diputar balik (revert swap)")]
        [SerializeField] private bool requireMatchToSwap = false;

        [Header("State Tracking")]
        [SerializeField] private bool isSwapping = false;
        private TileCell startCell = null;
        private Vector3 startWorldPos;
        private Vector2 startScreenPos;
        private bool isPointerDown = false;
        private Camera cachedCamera;

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

                // Hanya blokir jika raycast mengenai elemen Canvas UI (GraphicRaycaster)
                // JANGAN blokir jika hasil raycast berasal dari Physics2DRaycaster pada objek dunia 2D!
                if (result.module is UnityEngine.UI.GraphicRaycaster)
                {
                    var selectable = result.gameObject.GetComponentInParent<UnityEngine.UI.Selectable>();
                    if (selectable != null && selectable.interactable)
                    {
                        return true;
                    }

                    var graphic = result.gameObject.GetComponent<UnityEngine.UI.Graphic>();
                    if (graphic != null && graphic.raycastTarget)
                    {
                        // Abaikan gambar transparan penuh
                        if (graphic is UnityEngine.UI.Image img && img.color.a <= 0.01f) continue;
                        return true;
                    }
                }
            }

            return false;
        }

        private void OnPointerDown(Vector2 screenPos, int pointerId)
        {
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
                isPointerDown = true;

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
                Debug.Log($"[GridInputHandler] Pointer DOWN pada Screen: {screenPos}, World: {worldPos:F2} -> Tidak mengenai petak mana pun.");
            }
        }

        private void OnPointerUp(Vector2 screenPos, int pointerId)
        {
            if (!isPointerDown || startCell == null)
            {
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

            Debug.Log($"[GridInputHandler] Pointer UP pada Screen: {screenPos}, Delta World: {delta:F2} (Mag: {delta.magnitude:F2}, Threshold: {threshold:F2})");

            // Cek apakah jarak sapuan memenuhi ambang batas (Threshold)
            if (delta.magnitude < threshold)
            {
                Debug.Log($"[GridInputHandler] Swipe Ditolak: Jarak geser {delta.magnitude:F2} < ambang batas {threshold:F2} (Terdeteksi Tap/Klik biasa).");
                startCell = null;
                return;
            }

            // Tentukan arah dominan sapuan (Atas, Bawah, Kiri, Kanan)
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

            // Evaluasi syarat validitas swap & logging alasan jika ditolak
            TryExecuteSwap(originCell, targetX, targetY);
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
                        AudioManager.Instance.PlaySound(SoundType.ButtonClick, 0.65f);
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

                    if (unitA != null) unitA.transform.position = posA;
                    if (unitB != null) unitB.transform.position = posB;

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
                }
                if (unitA != null)
                {
                    cellB.SetOccupant(unitA);
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
                    AudioManager.Instance.PlaySound(SoundType.ButtonClick, 1.15f);
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
