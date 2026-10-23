using UnityEngine;

namespace FantasyDefense.UI
{
    /// <summary>
    /// Komponen adaptasi Safe Area untuk perangkat Mobile & Tablet (Android / iOS).
    /// Mengunci elemen UI agar berada di dalam zona aman layar, mencegah tumpang tindih
    /// dengan kamera poni/notch, Dynamic Island, punch-hole kamera depan, dan navigation/home gesture bar.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [ExecuteAlways]
    public class SafeAreaFitter : MonoBehaviour
    {
        [Header("Safe Area Edges")]
        [Tooltip("Sesuaikan margin atas (mencegah tertutup notch/kamera/status bar)")]
        [SerializeField] private bool conformTop = true;

        [Tooltip("Sesuaikan margin bawah (mencegah tertutup home bar/navigasi)")]
        [SerializeField] private bool conformBottom = true;

        [Tooltip("Sesuaikan margin kiri")]
        [SerializeField] private bool conformLeft = true;

        [Tooltip("Sesuaikan margin kanan")]
        [SerializeField] private bool conformRight = true;

        private RectTransform rectTransform;
        private Rect lastSafeArea = Rect.zero;
        private int lastScreenWidth = 0;
        private int lastScreenHeight = 0;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        private void OnEnable()
        {
            ApplySafeArea();
        }

        private void Update()
        {
            if (Screen.safeArea != lastSafeArea || Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
            {
                ApplySafeArea();
            }
        }

        /// <summary>
        /// Menerapkan perhitungan matematis safe area ke RectTransform anchor.
        /// </summary>
        public void ApplySafeArea()
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }

            if (rectTransform == null) return;

            Rect safeArea = Screen.safeArea;
            float screenW = Screen.width;
            float screenH = Screen.height;

            if (screenW <= 0 || screenH <= 0) return;

            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            anchorMin.x = conformLeft ? (anchorMin.x / screenW) : 0f;
            anchorMin.y = conformBottom ? (anchorMin.y / screenH) : 0f;
            anchorMax.x = conformRight ? (anchorMax.x / screenW) : 1f;
            anchorMax.y = conformTop ? (anchorMax.y / screenH) : 1f;

            // Pastikan nilai anchor valid dalam rentang [0, 1]
            anchorMin.x = Mathf.Clamp01(anchorMin.x);
            anchorMin.y = Mathf.Clamp01(anchorMin.y);
            anchorMax.x = Mathf.Clamp01(anchorMax.x);
            anchorMax.y = Mathf.Clamp01(anchorMax.y);

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            lastSafeArea = safeArea;
            lastScreenWidth = (int)screenW;
            lastScreenHeight = (int)screenH;
        }
    }
}
