using System;
using UnityEngine;
using FantasyDefense.Metagame;

namespace FantasyDefense.Save
{
    /// <summary>
    /// Adapter jembatan Cloud Save untuk Android (Google Play Games Saved Games),
    /// iOS (Apple Game Center / iCloud CloudKit), serta backend kustom (Firebase/UGS/REST API).
    /// Mengelola upload, download, otentikasi akun, dan resolusi konflik data (Conflict Resolution).
    /// </summary>
    public class CloudSaveAdapter : ICloudSaveProvider
    {
        public bool IsCloudAvailable { get; private set; } = false;
        public string CloudUserId { get; private set; } = "GUEST_OFFLINE";

        [System.Serializable]
        public enum CloudPlatform
        {
            None,
            GooglePlayGames,    // Android GPGS
            AppleGameCenter,    // iOS Game Center / iCloud
            CustomBackend       // Firebase / Supabase / Unity Gaming Services
        }

        private CloudPlatform activePlatform = CloudPlatform.None;

        public CloudSaveAdapter()
        {
            DetectAndInitializePlatform();
        }

        private void DetectAndInitializePlatform()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            activePlatform = CloudPlatform.GooglePlayGames;
            Debug.Log("[CloudSaveAdapter] Target Platform: Android Google Play Games Services terdeteksi.");
#elif UNITY_IOS && !UNITY_EDITOR
            activePlatform = CloudPlatform.AppleGameCenter;
            Debug.Log("[CloudSaveAdapter] Target Platform: iOS Apple Game Center / iCloud terdeteksi.");
#else
            activePlatform = CloudPlatform.CustomBackend;
            Debug.Log("[CloudSaveAdapter] Mode Editor / Desktop: Cloud Save berjalan dalam mode simulasi cloud.");
#endif
            // Di lingkungan produksi sesungguhnya, SDK login di-trigger di sini
            IsCloudAvailable = true;
        }

        public void SetUserIdentity(string userId, bool isAuthenticated)
        {
            CloudUserId = userId;
            IsCloudAvailable = isAuthenticated;
            Debug.Log($"[CloudSaveAdapter] Akun terhubung: {userId} (Status Login: {isAuthenticated})");
        }

        /// <summary>
        /// Mengunggah data lokal ke cloud server / provider platform.
        /// </summary>
        public void UploadCloudSave(CitadelAccountData data, Action<bool, string> onComplete)
        {
            if (!IsCloudAvailable || data == null)
            {
                onComplete?.Invoke(false, "Cloud Service tidak tersedia atau data bernilai null.");
                return;
            }

            try
            {
                string jsonPayload = JsonUtility.ToJson(data);
                data.cloudUserId = CloudUserId;

                // Simulasi pengunggahan cloud (akan dihubungkan ke SDK platform spesifik GPGS / GameCenter / Firebase)
                Debug.Log($"[CloudSaveAdapter] Mengunggah payload profil ke Cloud [{activePlatform}] untuk User: {CloudUserId}...");
                
                // Pada plugin GPGS: PlayGamesPlatform.Instance.SavedGame.OpenWithAutomaticConflictResolution(...)
                // Pada Firebase: Firestore/Database.SetJson(...)
                onComplete?.Invoke(true, "Sinkronisasi cloud berhasil.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CloudSaveAdapter] Gagal mengunggah ke cloud: {ex.Message}");
                onComplete?.Invoke(false, ex.Message);
            }
        }

        /// <summary>
        /// Mengunduh data save terbaru yang tersimpan di cloud server.
        /// </summary>
        public void DownloadCloudSave(Action<bool, CitadelAccountData, string> onComplete)
        {
            if (!IsCloudAvailable)
            {
                onComplete?.Invoke(false, null, "Cloud service tidak aktif.");
                return;
            }

            try
            {
                Debug.Log($"[CloudSaveAdapter] Memeriksa snapshot save terbaru dari Cloud [{activePlatform}]...");
                
                // Di sini integrasi pembacaan dari cloud native
                // Mengembalikan null jika belum ada simpanan cloud sebelumnya
                onComplete?.Invoke(true, null, "Pengecekan cloud selesai (belum ada perbedaan snapshot).");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CloudSaveAdapter] Gagal mengunduh save cloud: {ex.Message}");
                onComplete?.Invoke(false, null, ex.Message);
            }
        }

        /// <summary>
        /// Strategi Resolusi Konflik data saat cloud dan lokal berbeda (misal bermain di 2 perangkat berbeda):
        /// Memprioritaskan data dengan progres tertinggi (Citadel Level / Team CP) atau timestamp terbaru.
        /// </summary>
        public CitadelAccountData ResolveConflict(CitadelAccountData localData, CitadelAccountData cloudData)
        {
            if (localData == null) return cloudData;
            if (cloudData == null) return localData;

            Debug.Log($"[CloudSaveAdapter] Mengevaluasi konflik data: Lokal (Lv.{localData.citadelLevel}, CP.{localData.CalculateTotalTeamCP()}) vs Cloud (Lv.{cloudData.citadelLevel}, CP.{cloudData.CalculateTotalTeamCP()})");

            // 1. Prioritas level Citadel tertinggi
            if (localData.citadelLevel > cloudData.citadelLevel)
            {
                Debug.Log("[CloudSaveAdapter] Memilih data Lokal karena level Citadel lebih tinggi.");
                return localData;
            }
            if (cloudData.citadelLevel > localData.citadelLevel)
            {
                Debug.Log("[CloudSaveAdapter] Memilih data Cloud karena level Citadel lebih tinggi.");
                return cloudData;
            }

            // 2. Jika level sama, pilih total Team CP tertinggi
            int localCP = localData.CalculateTotalTeamCP();
            int cloudCP = cloudData.CalculateTotalTeamCP();
            if (localCP > cloudCP)
            {
                return localData;
            }
            if (cloudCP > localCP)
            {
                return cloudData;
            }

            // 3. Jika identik, pilih timestamp yang paling baru (terkini)
            return localData.lastSaveTimestampUtc >= cloudData.lastSaveTimestampUtc ? localData : cloudData;
        }
    }
}
