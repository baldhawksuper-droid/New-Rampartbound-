using System;
using UnityEngine;

namespace FantasyDefense.Mobile
{
    public enum HapticFeedbackType
    {
        Light,
        Medium,
        Heavy,
        Success,
        Warning,
        Failure
    }

    /// <summary>
    /// Master Controller untuk Optimalisasi Performa Ponsel & Haptic Feedback.
    /// Mengelola pembatasan frame rate (30/60 FPS), mode Hemat Baterai (Battery Saver),
    /// serta getaran fisik perangkat (Haptic Feedback) pada Android & iOS.
    /// </summary>
    public class DevicePerformanceManager : MonoBehaviour
    {
        public static DevicePerformanceManager Instance { get; private set; }

        private const string PREF_TARGET_FPS = "Rampartbound_TargetFPS";
        private const string PREF_BATTERY_SAVER = "Rampartbound_BatterySaver";
        private const string PREF_HAPTICS = "Rampartbound_HapticsEnabled";

        [Header("Runtime Settings")]
        [SerializeField] private int currentTargetFps = 60;
        [SerializeField] private bool isBatterySaverEnabled = false;
        [SerializeField] private bool isHapticsEnabled = true;

        public int CurrentTargetFps => currentTargetFps;
        public bool IsBatterySaverEnabled => isBatterySaverEnabled;
        public bool IsHapticsEnabled => isHapticsEnabled;

        public static event Action<int> OnFpsLimitChanged;
        public static event Action<bool> OnBatterySaverToggled;
        public static event Action<bool> OnHapticsToggled;

#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject vibrator;
#endif

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadPreferences();
            InitializePlatformHaptics();
        }

        private void Start()
        {
            ApplyPerformanceSettings();
        }

        private void LoadPreferences()
        {
            currentTargetFps = PlayerPrefs.GetInt(PREF_TARGET_FPS, 60);
            isBatterySaverEnabled = PlayerPrefs.GetInt(PREF_BATTERY_SAVER, 0) == 1;
            isHapticsEnabled = PlayerPrefs.GetInt(PREF_HAPTICS, 1) == 1;
        }

        private void InitializePlatformHaptics()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DevicePerformanceManager] Inisialisasi Android Vibrator gagal: {ex.Message}");
            }
#endif
        }

        public void ApplyPerformanceSettings()
        {
            QualitySettings.vSyncCount = 0;

            if (isBatterySaverEnabled)
            {
                Application.targetFrameRate = 30;
            }
            else
            {
                Application.targetFrameRate = currentTargetFps;
            }

            OnFpsLimitChanged?.Invoke(Application.targetFrameRate);
            OnBatterySaverToggled?.Invoke(isBatterySaverEnabled);
            OnHapticsToggled?.Invoke(isHapticsEnabled);

            Debug.Log($"[DevicePerformanceManager] Pengaturan performa diterapkan: Target FPS={Application.targetFrameRate}, Battery Saver={isBatterySaverEnabled}, Haptics={isHapticsEnabled}");
        }

        #region Performance Configuration API

        public void SetTargetFps(int fps)
        {
            currentTargetFps = Mathf.Clamp(fps, 30, 60);
            PlayerPrefs.SetInt(PREF_TARGET_FPS, currentTargetFps);
            PlayerPrefs.Save();

            if (!isBatterySaverEnabled)
            {
                Application.targetFrameRate = currentTargetFps;
                OnFpsLimitChanged?.Invoke(currentTargetFps);
            }
        }

        public void ToggleBatterySaver(bool isEnabled)
        {
            isBatterySaverEnabled = isEnabled;
            PlayerPrefs.SetInt(PREF_BATTERY_SAVER, isBatterySaverEnabled ? 1 : 0);
            PlayerPrefs.Save();

            ApplyPerformanceSettings();
        }

        public void ToggleHaptics(bool isEnabled)
        {
            isHapticsEnabled = isEnabled;
            PlayerPrefs.SetInt(PREF_HAPTICS, isHapticsEnabled ? 1 : 0);
            PlayerPrefs.Save();

            OnHapticsToggled?.Invoke(isHapticsEnabled);
        }

        public void ToggleHaptics()
        {
            ToggleHaptics(!isHapticsEnabled);
            if (isHapticsEnabled)
            {
                TriggerHaptic(HapticFeedbackType.Light);
            }
        }

        public void SetHapticsEnabled(bool isEnabled)
        {
            ToggleHaptics(isEnabled);
        }

        #endregion

        #region Haptic Feedback Engine

        /// <summary>
        /// Memicu respons getaran fisik pada ponsel sesuai intensitas aksi gameplay.
        /// </summary>
        public void TriggerHaptic(HapticFeedbackType type)
        {
            if (!isHapticsEnabled) return;

#if UNITY_ANDROID && !UNITY_EDITOR
            if (vibrator != null)
            {
                long milliseconds = 25;
                switch (type)
                {
                    case HapticFeedbackType.Light: milliseconds = 20; break;
                    case HapticFeedbackType.Medium: milliseconds = 50; break;
                    case HapticFeedbackType.Heavy: milliseconds = 90; break;
                    case HapticFeedbackType.Success: milliseconds = 40; break;
                    case HapticFeedbackType.Warning: milliseconds = 80; break;
                    case HapticFeedbackType.Failure: milliseconds = 110; break;
                }
                vibrator.Call("vibrate", milliseconds);
                return;
            }
#endif

#if UNITY_IOS && !UNITY_EDITOR
            Handheld.Vibrate();
            return;
#endif

            // Fallback Editor / Standalone testing
            Debug.Log($"[DevicePerformanceManager] Haptic Vibrate Triggered: {type}");
        }

        #endregion
    }
}
