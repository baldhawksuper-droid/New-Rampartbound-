using UnityEngine;
using FantasyDefense.GridSystem;

namespace FantasyDefense.Core
{
    /// <summary>
    /// Pengatur kamera adaptif untuk perangkat Mobile (Android, iOS) dan Tablet.
    /// Memastikan grid arena, jalur monster, dan benteng tetap tampil proporsional tanpa terpotong
    /// di berbagai rasio layar (9:16, 9:19.5, 9:21 ponsel berponi/notch, serta 3:4 & 4:3 tablet/iPad).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class MobileCameraAdapter : MonoBehaviour
    {
        [Header("Camera Reference")]
        [SerializeField] private Camera targetCamera;

        [Header("Framing & Safe Area Margins")]
        [Tooltip("Lebar pandangan horizontal minimum (dalam satuan Unity) agar grid tidak terpotong")]
        [SerializeField] private float minimumHorizontalWidth = 6.5f;

        [Tooltip("Ukuran ortografis minimum untuk layar vertikal standar (9:16)")]
        [SerializeField] private float baseOrthographicSize = 5.8f;

        [Tooltip("Margin aman horizontal untuk tablet dan ponsel berlayar lebar")]
        [SerializeField] private float horizontalPadding = 0.15f;

        [Header("Portrait Viewport Centering (Sprint 43)")]
        [Tooltip("Offset vertikal kamera agar barikade fisik (y=-3.5) dan waypoint gerbang atas simetris di ponsel portrait")]
        [SerializeField] private float verticalCenterOffset = -0.45f;

        private int lastScreenWidth;
        private int lastScreenHeight;

        public static MobileCameraAdapter Instance { get; private set; }

        private Vector3 originalCameraPosition;
        private Coroutine shakeCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (targetCamera == null)
                targetCamera = GetComponent<Camera>();

            Vector3 pos = transform.position;
            pos.y = verticalCenterOffset;
            transform.position = pos;

            originalCameraPosition = transform.position;
            AdjustCameraViewport();
        }

        private void Start()
        {
            // Subscribe ke event pembuatan grid agar bisa menyesuaikan ukuran otomatis jika grid membesar (misal 6x6 atau 7x7)
            GridManager.OnGridGenerated += AdjustCameraViewport;
            AdjustCameraViewport();
        }

        private void OnDestroy()
        {
            GridManager.OnGridGenerated -= AdjustCameraViewport;
        }

        /// <summary>
        /// Memicu efek getaran kamera (Screen Shake) untuk hantaman boss, merge Tier 3, atau damage benteng.
        /// Mendukung rotasi mikro (Z-angle twist) untuk benturan berskala besar.
        /// </summary>
        public void TriggerScreenShake(float duration = 0.22f, float magnitude = 0.16f, float rotationalAngle = 0f)
        {
            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
            }
            shakeCoroutine = StartCoroutine(DoScreenShake(duration, magnitude, rotationalAngle));
        }

        /// <summary>
        /// Memicu guncangan kamera khusus hantaman Boss Monster (Point 4 - Boss Impact Screen-Shake Twist).
        /// Memberikan hentakan berat dengan rotasi mikro sudut Z (0.5 - 0.7 derajat) yang dramatis.
        /// </summary>
        public void TriggerBossScreenShake(float duration = 0.38f, float magnitude = 0.30f, float maxAngle = 0.65f)
        {
            TriggerScreenShake(duration, magnitude, maxAngle);
        }

        private System.Collections.IEnumerator DoScreenShake(float duration, float magnitude, float rotationalAngle = 0f)
        {
            float elapsed = 0f;
            Vector3 basePos = originalCameraPosition;

            while (elapsed < duration)
            {
                float damping = 1f - (elapsed / duration);
                float offsetX = Random.Range(-1f, 1f) * magnitude * damping;
                float offsetY = Random.Range(-1f, 1f) * magnitude * damping;

                transform.position = new Vector3(basePos.x + offsetX, basePos.y + offsetY, basePos.z);

                if (rotationalAngle > 0.01f)
                {
                    float angleOffset = Random.Range(-1f, 1f) * rotationalAngle * damping;
                    transform.rotation = Quaternion.Euler(0f, 0f, angleOffset);
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            transform.position = basePos;
            transform.rotation = Quaternion.identity;
            shakeCoroutine = null;
        }

        private void Update()
        {
            // Deteksi perubahan orientasi layar atau resolusi runtime (misal rotasi perangkat atau split-screen tablet)
            if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
            {
                AdjustCameraViewport();
            }
        }

        /// <summary>
        /// Menyesuaikan orthographicSize kamera secara matematis berdasarkan aspect ratio layar saat ini.
        /// </summary>
        public void AdjustCameraViewport()
        {
            if (targetCamera == null || !targetCamera.orthographic) return;

            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;

            float currentAspectRatio = (float)Screen.width / Mathf.Max(1, Screen.height);

            // Jika GridManager ada di scene, ambil batas aktual grid
            float requiredWidth = minimumHorizontalWidth;
            if (GridManager.Instance != null && GridManager.Instance.IsInitialized)
            {
                Vector2 gridBounds = GridManager.Instance.GetGridWorldBounds();
                requiredWidth = Mathf.Max(requiredWidth, gridBounds.x + (horizontalPadding * 2f));
            }

            // Hitung ukuran ortografis yang dibutuhkan agar lebar minimum muat sempurna di layar
            float orthoSizeForWidth = (requiredWidth / 2f) / currentAspectRatio;

            // Pilih nilai terbesar agar seluruh konten vertikal dan horizontal aman
            targetCamera.orthographicSize = Mathf.Max(baseOrthographicSize, orthoSizeForWidth);
        }
    }
}
