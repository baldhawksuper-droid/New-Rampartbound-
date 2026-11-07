using UnityEngine;
using UnityEngine.EventSystems;

namespace FantasyDefense.Core
{
    /// <summary>
    /// Menjamin keandalan input layar sentuh pada perangkat Mobile (Android, iOS) dan Tablet.
    /// Otomatis memvalidasi dan melengkapi komponen Physics2DRaycaster pada kamera utama
    /// serta mencegah gangguan multitouch yang tidak disengaja (misal sentuhan telapak tangan pada tablet).
    /// </summary>
    public class MobileTouchManager : MonoBehaviour
    {
        public static MobileTouchManager Instance { get; private set; }

        [Header("Mobile Ergonomics")]
        [Tooltip("Abaikan sentuhan jari kedua/ketiga saat pemain sedang menyeret hero (mencegah konflik sentuhan di tablet)")]
        [SerializeField] private bool lockSingleTouchDuringDrag = true;

        [Header("Auto Configuration")]
        [Tooltip("Otomatis menambahkan Physics2DRaycaster ke Camera.main jika belum ada")]
        [SerializeField] private bool autoEnsurePhysics2DRaycaster = true;

        private int activeFingerId = -1;
        private bool isDragLocked = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureCameraRaycaster();
        }

        private void Start()
        {
            EnsureCameraRaycaster();

            // Set frame rate optimal untuk kelancaran visual layar sentuh mobile 60fps/90fps
            Application.targetFrameRate = 60;

            TuneEventSystemForMobile();
        }

        /// <summary>
        /// Menyesuaikan ambang batas seret piksel (pixelDragThreshold) EventSystem agar sentuhan jari mobile tidak salah dideteksi sebagai drag.
        /// </summary>
        public void TuneEventSystemForMobile()
        {
            var eventSys = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            if (eventSys != null)
            {
                eventSys.pixelDragThreshold = 12; // Nilai optimal untuk mendeteksi pergeseran/drag instan pada layar sentuh mobile
            }
        }

        /// <summary>
        /// Memastikan Camera.main memiliki Physics2DRaycaster agar interaksi IPointerDownHandler & IDragHandler
        /// pada 2D Collider (UnitController & TileCell) selalu aktif di semua perangkat Android, iOS, dan tablet.
        /// </summary>
        public void EnsureCameraRaycaster()
        {
            if (!autoEnsurePhysics2DRaycaster) return;

            Camera cam = Camera.main;
            if (cam != null)
            {
                var raycaster = cam.GetComponent<Physics2DRaycaster>();
                if (raycaster == null)
                {
                    cam.gameObject.AddComponent<Physics2DRaycaster>();
                    Debug.Log("[MobileTouchManager] Physics2DRaycaster otomatis ditambahkan ke Main Camera untuk dukungan Mobile Touch 2D.");
                }
            }

            // Pastikan EventSystem tersedia di Scene dengan StandaloneInputModule standar (stabil untuk Android Touch)
            var eventSys = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            if (eventSys == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSys = eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<StandaloneInputModule>();
                Debug.Log("[MobileTouchManager] EventSystem otomatis dibuat di Scene dengan StandaloneInputModule.");
            }
            else
            {
                // Bersihkan InputSystemUIInputModule jika ada untuk menghindari TypeInitializationException pada Process()
                var modernMod = eventSys.GetComponent("InputSystemUIInputModule");
                if (modernMod != null)
                {
                    UnityEngine.Object.Destroy(modernMod);
                    Debug.Log("[MobileTouchManager] Mengganti InputSystemUIInputModule dengan StandaloneInputModule untuk stabilitas uGUI mobile.");
                }

                if (eventSys.GetComponent<StandaloneInputModule>() == null)
                {
                    eventSys.gameObject.AddComponent<StandaloneInputModule>();
                }
            }

            if (eventSys != null)
            {
                eventSys.pixelDragThreshold = 12;
            }
        }

        public bool CanProcessTouch(int fingerId)
        {
            if (!lockSingleTouchDuringDrag || !isDragLocked) return true;
            return fingerId == activeFingerId;
        }

        public void RegisterDragStart(int fingerId)
        {
            activeFingerId = fingerId;
            isDragLocked = true;
        }

        public void RegisterDragEnd()
        {
            activeFingerId = -1;
            isDragLocked = false;
        }
    }
}
