using System;
using UnityEngine;
using FantasyDefense.Metagame;

namespace FantasyDefense.Save
{
    /// <summary>
    /// Orkestrator pusat penyimpanan profil pemain (Central Save Manager).
    /// Mengelola persistensi data lokal, sinkronisasi cloud Android/iOS,
    /// auto-save saat siklus hidup aplikasi mobile (Pause & Quit),
    /// serta kalkulasi regenerasi stamina saat pemain sedang offline.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        [Header("Stamina Settings")]
        [Tooltip("Interval waktu nyata (detik) untuk memulihkan 1 poin energi")]
        [SerializeField] private int energyRecoveryIntervalSeconds = 300; // 5 menit per energi
        [SerializeField] private int energyPerRecovery = 1;

        [Header("Cloud Settings")]
        [SerializeField] private bool autoSyncCloudOnSave = true;

        private ISaveService localSaveService;
        private ICloudSaveProvider cloudSaveProvider;
        private CitadelAccountData cachedData;

        public ISaveService LocalService => localSaveService;
        public ICloudSaveProvider CloudProvider => cloudSaveProvider;

        /// <summary>
        /// Mengakses data akun aktif pemain (dari CitadelLobbyManager jika ada, atau cachedData).
        /// </summary>
        public CitadelAccountData CurrentData
        {
            get
            {
                if (CitadelLobbyManager.Instance != null && CitadelLobbyManager.Instance.AccountData != null)
                {
                    return CitadelLobbyManager.Instance.AccountData;
                }
                return cachedData;
            }
            set => cachedData = value;
        }

        /// <summary>
        /// Menyimpan data profil akun aktif ke penyimpanan lokal/cloud.
        /// </summary>
        public void SaveGame()
        {
            if (CurrentData != null)
            {
                SaveProfile(CurrentData);
            }
        }

        public static event Action<bool> OnSaveCompleted;
        public static event Action<CitadelAccountData> OnDataLoaded;
        public static event Action<bool, string> OnCloudSyncStatusChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeServices();
        }

        private void InitializeServices()
        {
            localSaveService = new LocalSaveService();
            cloudSaveProvider = new CloudSaveAdapter();
            Debug.Log("[SaveManager] Layanan penyimpanan lokal dan adapter cloud berhasil diinisialisasi.");
        }

        /// <summary>
        /// Memuat profil data akun pemain. Jika belum ada di disk, akan mengembalikan null sehingga
        /// sistem dapat menggunakan konfigurasi bawaan (default profile).
        /// </summary>
        public void LoadProfile(Action<CitadelAccountData> onLoaded)
        {
            localSaveService.Load((localData) =>
            {
                if (localData != null)
                {
                    // Hitung regenerasi stamina selama pemain offline
                    ProcessOfflineEnergyRecovery(localData);
                }

                // Jika terhubung cloud, periksa apakah ada data cloud yang lebih baru
                if (cloudSaveProvider.IsCloudAvailable)
                {
                    cloudSaveProvider.DownloadCloudSave((success, cloudData, msg) =>
                    {
                        CitadelAccountData resolvedData = localData;

                        if (success && cloudData != null)
                        {
                            resolvedData = cloudSaveProvider.ResolveConflict(localData, cloudData);
                            // Simpan hasil resolusi kembali ke penyimpanan lokal
                            localSaveService.Save(resolvedData);
                        }

                        cachedData = resolvedData;
                        onLoaded?.Invoke(resolvedData);
                        OnDataLoaded?.Invoke(resolvedData);
                    });
                }
                else
                {
                    cachedData = localData;
                    onLoaded?.Invoke(localData);
                    OnDataLoaded?.Invoke(localData);
                }
            });
        }

        /// <summary>
        /// Menyimpan data profil akun ke penyimpanan lokal dan secara opsional menyinkronkan ke cloud.
        /// </summary>
        public void SaveProfile(CitadelAccountData data, bool syncToCloud = true, Action<bool> onComplete = null)
        {
            if (data == null)
            {
                onComplete?.Invoke(false);
                return;
            }

            cachedData = data;

            localSaveService.Save(data, (localSuccess) =>
            {
                OnSaveCompleted?.Invoke(localSuccess);

                if (localSuccess && syncToCloud && autoSyncCloudOnSave && cloudSaveProvider.IsCloudAvailable)
                {
                    cloudSaveProvider.UploadCloudSave(data, (cloudSuccess, msg) =>
                    {
                        OnCloudSyncStatusChanged?.Invoke(cloudSuccess, msg);
                    });
                }

                onComplete?.Invoke(localSuccess);
            });
        }

        /// <summary>
        /// Menghitung berapa banyak energi/stamina yang pulih selama aplikasi ditutup (offline).
        /// </summary>
        public int ProcessOfflineEnergyRecovery(CitadelAccountData data)
        {
            if (data == null || data.lastSaveTimestampUtc <= 0) return 0;
            if (data.currentEnergy >= data.maxEnergy) return 0;

            long currentUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long elapsedSeconds = currentUtc - data.lastSaveTimestampUtc;

            if (elapsedSeconds <= 0) return 0;

            int intervalsPassed = (int)(elapsedSeconds / energyRecoveryIntervalSeconds);
            int energyToRecover = intervalsPassed * energyPerRecovery;

            if (energyToRecover > 0)
            {
                int oldEnergy = data.currentEnergy;
                data.currentEnergy = Mathf.Min(data.maxEnergy, data.currentEnergy + energyToRecover);
                int actualRecovered = data.currentEnergy - oldEnergy;

                Debug.Log($"[SaveManager] Offline Stamina Recovery: +{actualRecovered} energi dipulihkan ({elapsedSeconds} detik offline).");
                return actualRecovered;
            }

            return 0;
        }

        /// <summary>
        /// Memicu sinkronisasi manual ke cloud server (misal saat player menekan tombol Cloud Sync di UI).
        /// </summary>
        public void SyncManualWithCloud(CitadelAccountData currentData, Action<bool, string> onComplete = null)
        {
            if (!cloudSaveProvider.IsCloudAvailable)
            {
                onComplete?.Invoke(false, "Cloud Service tidak aktif.");
                return;
            }

            cloudSaveProvider.UploadCloudSave(currentData, (success, msg) =>
            {
                OnCloudSyncStatusChanged?.Invoke(success, msg);
                onComplete?.Invoke(success, msg);
            });
        }

        // =========================================================================
        // SIKLUS HIDUP MOBILE (Android & iOS Lifecycle Handling)
        // =========================================================================
        private void OnApplicationPause(bool pauseStatus)
        {
            // Di Android & iOS, saat pemain beralih aplikasi atau menerima telepon, pauseStatus = true
            if (pauseStatus && CitadelLobbyManager.Instance != null)
            {
                Debug.Log("[SaveManager] Aplikasi masuk ke latar belakang (Background/Pause). Melakukan auto-save...");
                SaveProfile(CitadelLobbyManager.Instance.AccountData, syncToCloud: true);
            }
        }

        private void OnApplicationQuit()
        {
            if (CitadelLobbyManager.Instance != null)
            {
                Debug.Log("[SaveManager] Aplikasi ditutup (Quit). Melakukan penyimpanan data lokal final...");
                SaveProfile(CitadelLobbyManager.Instance.AccountData, syncToCloud: false);
            }
        }
    }
}
