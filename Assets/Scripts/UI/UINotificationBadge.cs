using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using FantasyDefense.Metagame;

namespace FantasyDefense.UI
{
    public enum NotificationBadgeType
    {
        Custom,
        QuestsUnclaimed,
        DailySummonFree,
        EnergyFull
    }

    /// <summary>
    /// Menampilkan titik merah notifikasi (Notification Red Dot Badge) pada tombol navigasi
    /// jika ada aksi gratis yang dapat diambil (Quest siap diklaim, gacha gratis harian, atau stamina 100/100).
    /// </summary>
    public class UINotificationBadge : MonoBehaviour
    {
        [Header("Badge Configuration")]
        [SerializeField] private NotificationBadgeType badgeType = NotificationBadgeType.Custom;
        [SerializeField] private GameObject redDotObject;
        [SerializeField] private Image redDotImage;
        [SerializeField] private bool pulseAnimation = true;

        private Coroutine pulseRoutine;

        private void Awake()
        {
            if (redDotObject == null)
            {
                redDotObject = gameObject;
            }
            if (redDotImage == null && redDotObject != null)
            {
                redDotImage = redDotObject.GetComponent<Image>();
            }
        }

        private void OnEnable()
        {
            if (!gameObject.activeInHierarchy) return;

            StartCoroutine(PeriodicCheckRoutine());
            if (pulseAnimation)
            {
                pulseRoutine = StartCoroutine(PulseRoutine());
            }
        }

        private void OnDisable()
        {
            if (pulseRoutine != null)
            {
                StopCoroutine(pulseRoutine);
                pulseRoutine = null;
            }
        }

        public void SetVisible(bool isVisible)
        {
            // Jika redDotObject adalah objek ini sendiri, jangan matikan GameObject karena akan membunuh coroutine!
            // Cukup sembunyikan/tampilkan komponen Image.
            if (redDotObject != null && redDotObject != gameObject)
            {
                redDotObject.SetActive(isVisible);
            }
            else if (redDotImage != null)
            {
                redDotImage.enabled = isVisible;
            }
        }

        private IEnumerator PeriodicCheckRoutine()
        {
            // Berikan jeda 1 frame agar siklus OnEnable selesai tanpa interupsi
            yield return null;

            while (true)
            {
                UpdateBadgeState();
                yield return new WaitForSeconds(1.5f);
            }
        }

        public void UpdateBadgeState()
        {
            bool show = false;

            switch (badgeType)
            {
                case NotificationBadgeType.QuestsUnclaimed:
                    show = QuestManager.Instance != null && QuestManager.Instance.HasUnclaimedRewards();
                    break;

                case NotificationBadgeType.DailySummonFree:
                    show = CitadelSummonManager.Instance != null && CitadelSummonManager.Instance.HasFreeDailySummon();
                    break;

                case NotificationBadgeType.EnergyFull:
                    var lobby = CitadelLobbyManager.Instance;
                    show = lobby != null && lobby.AccountData != null && lobby.AccountData.currentEnergy >= lobby.AccountData.maxEnergy;
                    break;
            }

            SetVisible(show);
        }

        private IEnumerator PulseRoutine()
        {
            float timer = 0f;
            Vector3 baseScale = redDotObject != null ? redDotObject.transform.localScale : Vector3.one;

            while (true)
            {
                if (redDotObject != null && redDotObject.activeInHierarchy && (redDotImage == null || redDotImage.enabled))
                {
                    timer += Time.unscaledDeltaTime * 4.5f;
                    float wave = Mathf.Sin(timer);
                    float scaleMod = 1f + (wave * 0.08f);
                    redDotObject.transform.localScale = baseScale * scaleMod;

                    if (redDotImage != null)
                    {
                        Color c = redDotImage.color;
                        c.a = 0.8f + (wave * 0.2f);
                        redDotImage.color = c;
                    }
                }
                yield return null;
            }
        }
    }
}
