using System;
using System.Collections;
using UnityEngine;
using FantasyDefense.Metagame;

namespace FantasyDefense.Save
{
    public enum CloudSyncState
    {
        Idle,
        Synchronizing,
        Synced,
        ConflictResolved,
        Offline,
        Error
    }

    /// <summary>
    /// Layanan sinkronisasi Cloud Save cerdas (Google Play Games & Apple Game Center / Custom Cloud).
    /// Mengelola pemicu auto-sync saat pencapaian penting pemain tercapai (Stage Clear, Gacha, Promosi Star, Artefak),
    /// serta menyediakan mekanisme resolusi konflik lintas perangkat (Cross-Device Conflict Resolution).
    /// </summary>
    public class CloudSaveSyncService : MonoBehaviour
    {
        public static CloudSaveSyncService Instance { get; private set; }

        [Header("Cloud Configuration")]
        [SerializeField] private bool autoSyncOnMilestones = true;
        [SerializeField] private float autoSyncDebounceSeconds = 3.0f;

        private CloudSyncState currentState = CloudSyncState.Idle;
        private string lastStatusMessage = "Siap sinkronisasi.";
        private Coroutine debounceSyncCoroutine;

        public CloudSyncState CurrentState => currentState;
        public string LastStatusMessage => lastStatusMessage;

        public static event Action<CloudSyncState, string> OnSyncStatusChanged;
        public static event Action<CitadelAccountData> OnCloudConflictResolved;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            // Dengarkan peristiwa penting metagame untuk memicu auto-sync
            CitadelLobbyManager.OnStageProgressUpdated += HandleStageProgressUpdated;
            CitadelLobbyManager.OnHeroStarPromoted += HandleHeroPromoted;
            ArtifactManager.OnArtifactsChanged += HandleArtifactsChanged;
            LocalSaveService.OnSaveTamperedDetected += HandleLocalTamperDetected;
        }

        private void OnDisable()
        {
            CitadelLobbyManager.OnStageProgressUpdated -= HandleStageProgressUpdated;
            CitadelLobbyManager.OnHeroStarPromoted -= HandleHeroPromoted;
            ArtifactManager.OnArtifactsChanged -= HandleArtifactsChanged;
            LocalSaveService.OnSaveTamperedDetected -= HandleLocalTamperDetected;
        }

        #region Event Triggers & Debounce

        private void HandleStageProgressUpdated(int stageId, int stars, bool isFirstClear)
        {
            RequestDebouncedCloudSync("Stage Clear: " + stageId);
        }

        private void HandleHeroPromoted(HeroLobbyProfile hero)
        {
            RequestDebouncedCloudSync($"Hero Star Promoted: {hero.heroName} ({hero.starRank}★)");
        }

        private void HandleArtifactsChanged()
        {
            RequestDebouncedCloudSync("Artifact Configuration Updated");
        }

        private void HandleLocalTamperDetected()
        {
            Debug.LogWarning("[CloudSaveSyncService] Terdeteksi modifikasi lokal ilegal! Mengunduh snapshot cloud yang valid untuk restorasi...");
            TriggerCloudDownloadAndRestore();
        }

        private void RequestDebouncedCloudSync(string reason)
        {
            if (!autoSyncOnMilestones) return;

            if (debounceSyncCoroutine != null)
            {
                StopCoroutine(debounceSyncCoroutine);
            }
            debounceSyncCoroutine = StartCoroutine(DebounceSyncRoutine(reason));
        }

        private IEnumerator DebounceSyncRoutine(string reason)
        {
            yield return new WaitForSeconds(autoSyncDebounceSeconds);
            Debug.Log($"[CloudSaveSyncService] Menjalankan Auto-Sync Cloud (Alasan: {reason})...");
            TriggerManualSync();
            debounceSyncCoroutine = null;
        }

        #endregion

        #region Sync Operations

        /// <summary>
        /// Memicu sinkronisasi penuh (Unggah profil lokal ke Cloud).
        /// </summary>
        public void TriggerManualSync(Action<bool, string> onComplete = null)
        {
            if (SaveManager.Instance == null)
            {
                SetState(CloudSyncState.Error, "SaveManager tidak ditemukan.");
                onComplete?.Invoke(false, lastStatusMessage);
                return;
            }

            var provider = SaveManager.Instance.CloudProvider;
            if (provider == null || !provider.IsCloudAvailable)
            {
                SetState(CloudSyncState.Offline, "Cloud Save tidak tersedia / Offline.");
                onComplete?.Invoke(false, lastStatusMessage);
                return;
            }

            var localData = CitadelLobbyManager.Instance != null ? CitadelLobbyManager.Instance.AccountData : null;
            if (localData == null)
            {
                SetState(CloudSyncState.Error, "Profil akun lokal kosong.");
                onComplete?.Invoke(false, lastStatusMessage);
                return;
            }

            SetState(CloudSyncState.Synchronizing, "Sedang mengunggah data profil ke Cloud...");

            provider.UploadCloudSave(localData, (success, msg) =>
            {
                if (success)
                {
                    SetState(CloudSyncState.Synced, "Data profil berhasil disinkronkan ke Cloud.");
                }
                else
                {
                    SetState(CloudSyncState.Error, $"Gagal sinkronisasi: {msg}");
                }
                onComplete?.Invoke(success, lastStatusMessage);
            });
        }

        /// <summary>
        /// Mengunduh data dari Cloud dan melakukan resolusi konflik terhadap data lokal.
        /// </summary>
        public void TriggerCloudDownloadAndRestore(Action<bool, string> onComplete = null)
        {
            if (SaveManager.Instance == null) return;

            var provider = SaveManager.Instance.CloudProvider;
            if (provider == null || !provider.IsCloudAvailable) return;

            SetState(CloudSyncState.Synchronizing, "Memeriksa data simpanan terbaru dari Cloud...");

            provider.DownloadCloudSave((success, cloudData, msg) =>
            {
                if (success && cloudData != null)
                {
                    var localData = CitadelLobbyManager.Instance != null ? CitadelLobbyManager.Instance.AccountData : null;
                    var resolvedData = provider.ResolveConflict(localData, cloudData);

                    // Simpan data hasil resolusi secara lokal
                    SaveManager.Instance.LocalService.Save(resolvedData, (saved) =>
                    {
                        SetState(CloudSyncState.ConflictResolved, "Resolusi konflik selesai: data cloud termutakhir telah diterapkan.");
                        OnCloudConflictResolved?.Invoke(resolvedData);
                        onComplete?.Invoke(true, lastStatusMessage);
                    });
                }
                else
                {
                    SetState(CloudSyncState.Synced, "Data lokal sudah merupakan versi termutakhir.");
                    onComplete?.Invoke(success, msg);
                }
            });
        }

        private void SetState(CloudSyncState state, string message)
        {
            currentState = state;
            lastStatusMessage = message;
            Debug.Log($"[CloudSaveSyncService] Status: {state} — {message}");
            OnSyncStatusChanged?.Invoke(state, message);
        }

        #endregion
    }
}
