using System.Collections.Generic;
using UnityEngine;
using FantasyDefense.Combat;
using FantasyDefense.GridSystem;

namespace FantasyDefense.Wave
{
    /// <summary>
    /// Mengatur lintasan titik jalan (Waypoints) yang dilalui monster dari titik kemunculan (Spawn)
    /// menyusuri pinggiran arena Grid menuju gerbang benteng (Barricade).
    /// Dirancang adaptif untuk berbagai ukuran grid (5x5 hingga 7x7) dan rasio layar mobile portrait.
    /// </summary>
    public class MonsterPathManager : MonoBehaviour
    {
        public static MonsterPathManager Instance { get; private set; }

        [Header("Custom Waypoint Transforms (Optional)")]
        [Tooltip("Daftar transform titik jalan manual di Scene. Jika kosong, titik jalan akan dihitung otomatis.")]
        [SerializeField] private List<Transform> customWaypoints = new List<Transform>();

        [Header("Generated Path Parameters")]
        [SerializeField] private float topSpawnOffsetY = 1.8f;
        [SerializeField] private float bottomBarricadeOffsetY = -1.2f;
        [SerializeField] private float lanePaddingX = 0.8f;

        private Vector3[] cachedWaypoints;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private int lastScreenWidth;
        private int lastScreenHeight;

        private void Start()
        {
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            BuildWaypoints();
        }

        private void Update()
        {
            if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
            {
                lastScreenWidth = Screen.width;
                lastScreenHeight = Screen.height;
                BuildWaypoints();
            }
        }

        /// <summary>
        /// Membangun koordinat lintasan gerak monster.
        /// </summary>
        public void BuildWaypoints()
        {
            if (customWaypoints != null && customWaypoints.Count > 0)
            {
                cachedWaypoints = new Vector3[customWaypoints.Count];
                for (int i = 0; i < customWaypoints.Count; i++)
                {
                    cachedWaypoints[i] = customWaypoints[i].position;
                }
                return;
            }

            // Hitung lintasan otomatis melintasi sisi luar Grid
            Vector2 gridBounds = Vector2.one * 5f;
            float centerY = -0.5f;

            if (GridManager.Instance != null && GridManager.Instance.IsInitialized)
            {
                gridBounds = GridManager.Instance.GetGridWorldBounds();
            }

            float halfW = (gridBounds.x / 2f) + lanePaddingX;
            float halfH = (gridBounds.y / 2f);

            float spawnY = centerY + halfH + topSpawnOffsetY;
            float topY = centerY + halfH + 0.2f;
            // Lintasan Serbuan Tengah (Central March Path):
            // Monster muncul dari gerbang atas (Spawn Gate) dan berbaris lurus menyusuri jalan batu (Stone Road)
            // menuju gerbang benteng (Barricade), terlihat jelas di tengah layar sesuai konsep desain resmi.
            float barricadeY = -3.5f;
            if (CastleBarricade.Instance != null)
            {
                barricadeY = CastleBarricade.Instance.transform.position.y + 0.1f + bottomBarricadeOffsetY;
            }
            else
            {
                barricadeY = centerY - halfH + bottomBarricadeOffsetY;
            }

            // Hitung posisi spawn di puncak layar (Top Edge of Screen) agar monster senantiasa berbaris
            // masuk dari atas ke bawah (Top to Bottom March) melintasi arena tanpa pernah muncul tiba-tiba di tengah layar
            float topScreenY = 6.8f;
            Camera mainCam = Camera.main;
            if (mainCam != null && mainCam.orthographic)
            {
                topScreenY = mainCam.transform.position.y + mainCam.orthographicSize + 0.6f;
            }

            float upperPathY = Mathf.Max(3.8f, centerY + halfH + 0.8f);
            float midMarchY = Mathf.Max(1.8f, centerY + halfH * 0.35f);

            cachedWaypoints = new Vector3[]
            {
                new Vector3(0f, topScreenY, 0f),       // 0: Titik Kemunculan Puncak (Off-Screen Top Spawn Gate)
                new Vector3(0f, topScreenY - 1.6f, 0f), // 1: Gerbang Masuk Medan Tempur Atas
                new Vector3(0f, upperPathY, 0f),       // 2: Jalan Batu Atas Mendekati Garis Pertahanan
                new Vector3(0f, midMarchY, 0f),        // 3: Jalur Serbuan Garis Depan
                new Vector3(0f, barricadeY, 0f)        // 4: Garis Pertahanan Benteng (Castle Barricade Gate)
            };
        }

        public Vector3[] GetWaypoints()
        {
            if (cachedWaypoints == null || cachedWaypoints.Length == 0)
            {
                BuildWaypoints();
            }
            return cachedWaypoints;
        }

        public Vector3 GetSpawnPoint()
        {
            Vector3[] points = GetWaypoints();
            return points != null && points.Length > 0 ? points[0] : Vector3.up * 5f;
        }
    }
}
