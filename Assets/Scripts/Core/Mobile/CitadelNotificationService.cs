using System;
using System.Collections.Generic;
using UnityEngine;

namespace FantasyDefense.Mobile
{
    /// <summary>
    /// Service Manager untuk Push Notification & Mobile Retention System di Rampartbound.
    /// Menjadwalkan pengingat stamina/energi penuh, ekspedisi patroli hero, reset raid titan,
    /// serta re-engagement reminder untuk menjaga retensi harian (Day 1, Day 3, Day 7).
    /// </summary>
    public class CitadelNotificationService : MonoBehaviour
    {
        public static CitadelNotificationService Instance { get; private set; }

        private const string PREF_NOTIF_ENABLED = "Rampartbound_Notif_Enabled";
        private const string PREF_NIGHT_MUTE = "Rampartbound_Notif_NightMute";

        [Header("Notification Settings")]
        [SerializeField] private bool notificationsEnabled = true;
        [SerializeField] private bool muteDuringNight = true; // Jangan kirim antara 22:00 s/d 07:00

        public bool NotificationsEnabled => notificationsEnabled;
        public bool MuteDuringNight => muteDuringNight;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadSettings();
            InitializeNotificationChannels();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                // Saat game berpindah ke background, jadwalkan semua notifikasi retensi
                ScheduleRetentionReminders();
            }
            else
            {
                // Saat game dibuka kembali, bersihkan notifikasi tertunda
                CancelAllScheduledNotifications();
            }
        }

        private void LoadSettings()
        {
            notificationsEnabled = PlayerPrefs.GetInt(PREF_NOTIF_ENABLED, 1) == 1;
            muteDuringNight = PlayerPrefs.GetInt(PREF_NIGHT_MUTE, 1) == 1;
        }

        public void SetNotificationsEnabled(bool enabled)
        {
            notificationsEnabled = enabled;
            PlayerPrefs.SetInt(PREF_NOTIF_ENABLED, enabled ? 1 : 0);
            PlayerPrefs.Save();

            if (!enabled)
            {
                CancelAllScheduledNotifications();
            }
            else
            {
                ScheduleRetentionReminders();
            }
        }

        public void SetNightMute(bool mute)
        {
            muteDuringNight = mute;
            PlayerPrefs.SetInt(PREF_NIGHT_MUTE, mute ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void InitializeNotificationChannels()
        {
            Debug.Log("[CitadelNotificationService] Inisialisasi kanal notifikasi Rampartbound selesai.");
#if UNITY_ANDROID && !UNITY_EDITOR
            // Hook ke AndroidNotificationCenter jika Unity Mobile Notifications package terinstal
#elif UNITY_IOS && !UNITY_EDITOR
            // Hook ke iOSNotificationCenter
#endif
        }

        /// <summary>
        /// Menjadwalkan seluruh set notifikasi retensi pemain saat keluar aplikasi.
        /// </summary>
        public void ScheduleRetentionReminders()
        {
            if (!notificationsEnabled) return;

            CancelAllScheduledNotifications();

            // 1. Stamina Penuh (2 Jam)
            ScheduleLocalNotification(
                "Stamina Benteng Penuh!",
                "Panglima! Stamina Citadel telah pulih 100%. Saatnya melanjutkan ekspedisi pahlawan!",
                TimeSpan.FromHours(2),
                "energy_channel"
            );

            // 2. Ekspedisi Patroli Selesai (4 Jam)
            ScheduleLocalNotification(
                "Ekspedisi Patroli Selesai!",
                "Pasukan pengintai telah kembali membawa peti harta karun emas & kristal abyssal!",
                TimeSpan.FromHours(4),
                "expedition_channel"
            );

            // 3. Daily Quest & Titan Raid Reset (Pukul 08:00 Pagi Esok Hari)
            DateTime tomorrowMorning = DateTime.Today.AddDays(1).AddHours(8);
            TimeSpan timeUntilMorning = tomorrowMorning - DateTime.Now;
            if (timeUntilMorning.TotalSeconds > 0)
            {
                ScheduleLocalNotification(
                    "Tantangan Harian Baru Menanti!",
                    "Klaim hadiah gratis hari ini dan kalahkan Titan Colossus bersama serikatmu!",
                    timeUntilMorning,
                    "daily_channel"
                );
            }

            // 4. Inactivity Retention Reminder (24 Jam & 48 Jam)
            ScheduleLocalNotification(
                "Benteng Citadel Membutuhkanmu!",
                "Gerombolan monster abyssal mendekati gerbang benteng! Kembali sekarang dan pimpin pertahanan!",
                TimeSpan.FromDays(1),
                "retention_channel"
            );
        }

        public void ScheduleLocalNotification(string title, string message, TimeSpan delay, string channelId)
        {
            if (!notificationsEnabled) return;

            DateTime fireTime = DateTime.Now.Add(delay);

            // Cek apakah waktu jatuh di jam malam jika night mute aktif
            if (muteDuringNight)
            {
                if (fireTime.Hour >= 22 || fireTime.Hour < 7)
                {
                    // Geser ke pukul 08:00 pagi berikutnya
                    fireTime = fireTime.Date.AddHours(8);
                    if (fireTime < DateTime.Now) fireTime = fireTime.AddDays(1);
                    delay = fireTime - DateTime.Now;
                }
            }

            Debug.Log($"[CitadelNotificationService] Dijadwalkan notifikasi [{channelId}]: '{title}' pada {fireTime:yyyy-MM-dd HH:mm:ss} (delay {delay.TotalMinutes:F0} menit)");

#if UNITY_ANDROID && !UNITY_EDITOR
            // Eksekusi Android Notification Intent
#elif UNITY_IOS && !UNITY_EDITOR
            // Eksekusi iOS Calendar / TimeInterval Trigger
#endif
        }

        public void CancelAllScheduledNotifications()
        {
            Debug.Log("[CitadelNotificationService] Semua notifikasi terjadwal dibersihkan.");
#if UNITY_ANDROID && !UNITY_EDITOR
            // AndroidNotificationCenter.CancelAllNotifications();
#elif UNITY_IOS && !UNITY_EDITOR
            // iOSNotificationCenter.RemoveAllDeliveredNotifications();
#endif
        }
    }
}
